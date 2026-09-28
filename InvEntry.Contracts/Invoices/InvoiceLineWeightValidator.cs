namespace InvEntry.Contracts.Invoices;

using System.Globalization;

public sealed record InvoiceLineWeightValidationResult(
    int RowNumber,
    string Product,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public string Message =>
        $"Row {RowNumber} ({Product}): {string.Join(" ", Errors)}";
}

public static class InvoiceLineWeightValidator
{
    private const decimal WeightTolerance = 0.001M;

    public static bool IsWeightBased(string? metal) =>
        !string.IsNullOrWhiteSpace(metal);

    public static (bool IsValid, string Message) ValidateGrossCell(
        object? proposedValue,
        decimal? stoneWeight)
    {
        if (!TryGetDecimal(proposedValue, out var grossWeight) || grossWeight <= 0M)
            return (false, "Gross Weight must be greater than zero.");

        if (stoneWeight.GetValueOrDefault() > grossWeight)
            return (false, "Stone Weight cannot exceed Gross Weight.");

        return (true, string.Empty);
    }

    public static (bool IsValid, string Message) ValidateStoneCell(
        object? proposedValue,
        decimal? grossWeight)
    {
        if (proposedValue is null || proposedValue is string text && string.IsNullOrWhiteSpace(text))
            return (true, string.Empty);

        if (!TryGetDecimal(proposedValue, out var stoneWeight) || stoneWeight < 0M)
            return (false, "Stone Weight cannot be negative.");

        if (grossWeight.HasValue && stoneWeight > grossWeight.Value)
            return (false, "Stone Weight cannot exceed Gross Weight.");

        return (true, string.Empty);
    }

    private static bool TryGetDecimal(object? value, out decimal result)
    {
        if (value is decimal decimalValue)
        {
            result = decimalValue;
            return true;
        }

        if (value is IConvertible)
        {
            try
            {
                result = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
            }
        }

        result = default;
        return false;
    }

    public static InvoiceLineWeightValidationResult Validate(
        int rowNumber,
        string? productName,
        string? productId,
        string? metal,
        decimal? grossWeight,
        decimal? stoneWeight,
        decimal? netWeight)
    {
        var errors = new List<string>();
        var product = !string.IsNullOrWhiteSpace(productName)
            ? productName.Trim()
            : !string.IsNullOrWhiteSpace(productId)
                ? productId.Trim()
                : "Unnamed product";

        if (!IsWeightBased(metal))
            return new(rowNumber, product, errors);

        if (!grossWeight.HasValue || grossWeight.Value <= 0M)
            errors.Add("Gross Weight must be greater than zero.");

        var effectiveStoneWeight = stoneWeight.GetValueOrDefault();
        if (effectiveStoneWeight < 0M)
            errors.Add("Stone Weight cannot be negative.");

        if (grossWeight.HasValue && effectiveStoneWeight > grossWeight.Value)
            errors.Add("Stone Weight cannot exceed Gross Weight.");

        if (grossWeight.HasValue && grossWeight.Value > 0M &&
            effectiveStoneWeight >= 0M && effectiveStoneWeight <= grossWeight.Value)
        {
            var expectedNetWeight = grossWeight.Value - effectiveStoneWeight;
            if (!netWeight.HasValue ||
                Math.Abs(netWeight.Value - expectedNetWeight) > WeightTolerance)
            {
                errors.Add("Net Weight must equal Gross Weight minus Stone Weight.");
            }
        }

        return new(rowNumber, product, errors);
    }
}
