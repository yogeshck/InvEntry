using System.Reflection;
using DataAccess.Workflows;
using InvEntry.Contracts.StockAdjustments;

namespace InvEntry.Test;

[TestFixture]
public sealed class StockAdjustmentReasonValidationTests
{
    [TestCase(StockAdjustmentTypes.Increase, StockAdjustmentReasonCodes.PhysicalExcess)]
    [TestCase(StockAdjustmentTypes.Decrease, StockAdjustmentReasonCodes.PhysicalShortage)]
    [TestCase(StockAdjustmentTypes.Reallocation, StockAdjustmentReasonCodes.ComponentTransfer)]
    public void ApprovedModeSpecificReason_IsAccepted(string adjustmentType, string reasonCode)
    {
        Assert.That(
            StockAdjustmentReasonCodes.IsValidForAdjustmentType(adjustmentType, reasonCode),
            Is.True);
    }

    [TestCase(StockAdjustmentTypes.Increase, StockAdjustmentReasonCodes.Loss)]
    [TestCase(StockAdjustmentTypes.Decrease, StockAdjustmentReasonCodes.PhysicalExcess)]
    [TestCase(StockAdjustmentTypes.Reallocation, StockAdjustmentReasonCodes.PhysicalShortage)]
    public void ReasonFromAnotherMode_IsRejected(string adjustmentType, string reasonCode)
    {
        Assert.That(
            StockAdjustmentReasonCodes.IsValidForAdjustmentType(adjustmentType, reasonCode),
            Is.False);
    }

    [TestCase(StockAdjustmentTypes.Increase)]
    [TestCase(StockAdjustmentTypes.Decrease)]
    [TestCase(StockAdjustmentTypes.Reallocation)]
    public void Other_WithRemarks_IsAcceptedByBackendHeaderValidation(string adjustmentType)
    {
        var request = CreateRequest(adjustmentType, StockAdjustmentReasonCodes.Other, "Count note");

        Assert.That(() => InvokeValidateHeader(request), Throws.Nothing);
    }

    [Test]
    public void Other_WithoutRemarks_IsRejectedByBackendHeaderValidation()
    {
        var request = CreateRequest(
            StockAdjustmentTypes.Increase,
            StockAdjustmentReasonCodes.Other,
            "   ");

        var exception = Assert.Throws<InvalidOperationException>(
            () => InvokeValidateHeader(request));

        Assert.That(exception!.Message, Is.EqualTo("Remarks are required when reason is OTHER."));
    }

    [Test]
    public void InvalidModeReason_IsRejectedByBackendHeaderValidation()
    {
        var request = CreateRequest(
            StockAdjustmentTypes.Increase,
            StockAdjustmentReasonCodes.Loss,
            null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => InvokeValidateHeader(request));

        Assert.That(
            exception!.Message,
            Is.EqualTo("The selected reason is not valid for an Increase stock adjustment."));
    }

    private static CreateStockAdjustmentRequest CreateRequest(
        string adjustmentType,
        string reasonCode,
        string? remarks)
    {
        return new CreateStockAdjustmentRequest
        {
            AdjustmentDate = DateTime.Today,
            AdjustmentType = adjustmentType,
            ReasonCode = reasonCode,
            Remarks = remarks,
            Lines =
            [
                new CreateStockAdjustmentLineRequest
                {
                    LineNbr = 1
                }
            ]
        };
    }

    private static void InvokeValidateHeader(CreateStockAdjustmentRequest request)
    {
        var method = typeof(StockAdjustmentWorkflow).GetMethod(
            "ValidateHeader",
            BindingFlags.NonPublic | BindingFlags.Static);

        try
        {
            method!.Invoke(null, [request]);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }
}
