using DataAccess.Models;
using DataAccess.Services.Advances;
using InvEntry.Contracts.Schemes;

namespace DataAccess.Services.Schemes;

/// <summary>
/// Spreads a scheme receipt over the installment schedule.
///
/// Fixed-amount plans: the payment pays the oldest unpaid installments first,
/// may pay part of one, and may not exceed what is still due on the schedule.
///
/// Flexible plans: the payment pays one month, the earliest not yet paid; it
/// must cover that month's outstanding minimum and may not exceed the plan's
/// maximum installment.
/// </summary>
public static class SchemeInstallmentAllocator
{
    public sealed record Allocation(SchemeInstallment Installment, decimal Amount, decimal Weight);

    public static IReadOnlyList<Allocation> Allocate(
        SchemePlanDto terms,
        IReadOnlyCollection<SchemeInstallment> installments,
        decimal amount,
        decimal weight,
        DateOnly paidOn)
    {
        if (amount <= 0M)
            throw new AdvanceAccountBusinessException("Receipt amount must be greater than zero.");

        var open = installments
            .Where(x => x.Status != SchemeTerms.InstallmentWaived &&
                        x.Status != SchemeTerms.InstallmentPaid)
            .OrderBy(x => x.SeqNbr)
            .ToList();

        if (open.Count == 0)
            throw new AdvanceAccountBusinessException("All installments of this scheme are already paid.");

        var amounts = terms.AmountFixed
            ? SpreadOverInstallments(open, amount)
            : SingleMonth(terms, open[0], amount);

        var allocations = SplitWeight(amounts, amount, weight);

        foreach (var allocation in allocations)
        {
            var installment = allocation.Installment;
            installment.AmountPaid += allocation.Amount;
            installment.WeightCredited += allocation.Weight;

            if (installment.AmountPaid >= installment.AmountDue)
            {
                installment.Status = SchemeTerms.InstallmentPaid;
                installment.PaidOn = paidOn;
            }
            else
            {
                installment.Status = SchemeTerms.InstallmentPartial;
            }
        }

        return allocations;
    }

    private static List<(SchemeInstallment Installment, decimal Amount)> SpreadOverInstallments(
        IReadOnlyList<SchemeInstallment> open,
        decimal amount)
    {
        var outstanding = open.Sum(x => Math.Max(0M, x.AmountDue - x.AmountPaid));

        if (amount > outstanding)
            throw new AdvanceAccountBusinessException(
                $"Only ₹{outstanding:N2} is still due on this scheme; ₹{amount:N2} was entered.");

        var result = new List<(SchemeInstallment, decimal)>();
        var remaining = amount;

        foreach (var installment in open)
        {
            if (remaining <= 0M)
                break;

            var due = Math.Max(0M, installment.AmountDue - installment.AmountPaid);
            var part = Math.Min(due, remaining);

            if (part <= 0M)
                continue;

            result.Add((installment, part));
            remaining -= part;
        }

        return result;
    }

    private static List<(SchemeInstallment Installment, decimal Amount)> SingleMonth(
        SchemePlanDto terms,
        SchemeInstallment installment,
        decimal amount)
    {
        var due = Math.Max(0M, installment.AmountDue - installment.AmountPaid);

        if (amount < due)
            throw new AdvanceAccountBusinessException(
                $"Installment {installment.SeqNbr} needs at least ₹{due:N2}; ₹{amount:N2} was entered.");

        if (terms.MaxInstallmentAmount is decimal max &&
            installment.AmountPaid + amount > max)
            throw new AdvanceAccountBusinessException(
                $"An installment of this scheme cannot exceed ₹{max:N2}.");

        return new List<(SchemeInstallment, decimal)> { (installment, amount) };
    }

    /// <summary>
    /// Weight follows the money; truncated per installment, with the last
    /// installment taking the remainder so the parts add up exactly.
    /// </summary>
    private static List<Allocation> SplitWeight(
        List<(SchemeInstallment Installment, decimal Amount)> amounts,
        decimal totalAmount,
        decimal totalWeight)
    {
        var allocations = new List<Allocation>();
        var weightLeft = totalWeight;

        for (var i = 0; i < amounts.Count; i++)
        {
            var (installment, part) = amounts[i];

            var weight = i == amounts.Count - 1
                ? weightLeft
                : decimal.Floor(totalWeight * part / totalAmount * 1000M) / 1000M;

            weightLeft -= weight;
            allocations.Add(new Allocation(installment, part, weight));
        }

        return allocations;
    }
}
