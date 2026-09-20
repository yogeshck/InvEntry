using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Contracts.Estimates;

namespace DataAccess.Workflows;

public sealed class EstimateWorkflow : IEstimateWorkflow
{
    private readonly IRepositoryBase<EstimateHeader> _headers;
    private readonly IRepositoryBase<EstimateLine> _lines;
    private readonly IRepositoryBase<VoucherType> _voucherTypes;
    private readonly IUnitOfWork _unitOfWork;

    public EstimateWorkflow(
        IRepositoryBase<EstimateHeader> headers,
        IRepositoryBase<EstimateLine> lines,
        IRepositoryBase<VoucherType> voucherTypes,
        IUnitOfWork unitOfWork)
    {
        _headers = headers;
        _lines = lines;
        _voucherTypes = voucherTypes;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveEstimateResponse> SaveAsync(
        SaveEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Header);
        if (request.Lines is null || request.Lines.Count == 0)
            throw new ArgumentException("At least one estimate line is required.", nameof(request));

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var voucherType = _voucherTypes.Get(x => x.DocumentType == "Estimate")
                ?? throw new InvalidOperationException("Estimate voucher type was not found.");
            var nextNumber = voucherType.LastUsedNumber.GetValueOrDefault() + 1;
            voucherType.LastUsedNumber = nextNumber;
            _voucherTypes.Update(voucherType);

            var estimateNumber = string.Format(
                "{0}{1}",
                voucherType.DocNbrPrefix,
                nextNumber.ToString($"D{voucherType.DocNbrLength}"));

            var header = MapHeader(request.Header);
            header.EstNbr = estimateNumber;
            _headers.Add(header);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var lines = request.Lines.Select((source, index) =>
                MapLine(source, header.Gkey, estimateNumber, header.TenantGkey, index + 1)).ToList();
            _lines.AddRange(lines);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SaveEstimateResponse
            {
                Gkey = header.Gkey,
                EstNbr = estimateNumber,
                TenantGkey = header.TenantGkey
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static EstimateHeader MapHeader(EstimateHeaderSaveModel x) => new()
    {
        EstDate=x.EstDate, PaymentMode=x.PaymentMode, CustGkey=x.CustGkey, CustMobile=x.CustMobile,
        EstRefund=x.EstRefund, EstTaxableAmount=x.EstTaxableAmount, IsTaxApplicable=x.IsTaxApplicable,
        GrossRcbAmount=x.GrossRcbAmount, OldGoldAmount=x.OldGoldAmount, OldSilverAmount=x.OldSilverAmount,
        TaxType=x.TaxType, GstLocSeller=x.GstLocSeller, GstLocBuyer=x.GstLocBuyer,
        CgstPercent=x.CgstPercent, CgstAmount=x.CgstAmount, SgstPercent=x.SgstPercent,
        SgstAmount=x.SgstAmount, IgstPercent=x.IgstPercent, IgstAmount=x.IgstAmount,
        AmountPayable=x.AmountPayable, DiscountPercent=x.DiscountPercent, DiscountAmount=x.DiscountAmount,
        AdvanceAdj=x.AdvanceAdj, PaymentDueDate=x.PaymentDueDate, RdAmountAdj=x.RdAmountAdj,
        EstBalance=x.EstBalance, RecdAmount=x.RecdAmount, RoundOff=x.RoundOff, EstNotes=x.EstNotes,
        DeliveryMethod=x.DeliveryMethod, DeliveryRef=x.DeliveryRef, OrderNbr=x.OrderNbr,
        OrderDate=x.OrderDate, EstlTaxTotal=x.EstlTaxTotal, CreatedBy=x.CreatedBy,
        CreatedOn=x.CreatedOn, ModifiedBy=x.ModifiedBy, ModifiedOn=x.ModifiedOn, TenantGkey=x.TenantGkey
    };

    private static EstimateLine MapLine(
        EstimateLineSaveModel x,
        int headerGkey,
        string estimateNumber,
        int? tenantGkey,
        int number) => new()
    {
        HsnCode=x.HsnCode, EstLineNbr=number, EstlBilledPrice=x.EstlBilledPrice,
        EstlGrossAmt=x.EstlGrossAmt, EstlMakingCharges=x.EstlMakingCharges,
        EstlOtherCharges=x.EstlOtherCharges, EstlPayableAmt=x.EstlPayableAmt,
        EstlStoneAmount=x.EstlStoneAmount, EstlTaxableAmount=x.EstlTaxableAmount,
        EstlWastageAmt=x.EstlWastageAmt, ProdCategory=x.ProdCategory,
        ProdGrossWeight=x.ProdGrossWeight, ProdNetWeight=x.ProdNetWeight, ProdQty=x.ProdQty,
        ProdStoneWeight=x.ProdStoneWeight, ProductDesc=x.ProductDesc, ProductGkey=x.ProductGkey,
        ProductName=x.ProductName, ProdPackCode=x.ProdPackCode, ProductPurity=x.ProductPurity,
        IsTaxable=x.IsTaxable, ItemNotes=x.ItemNotes, ItemPacked=x.ItemPacked,
        EstlCgstPercent=x.EstlCgstPercent, EstlCgstAmount=x.EstlCgstAmount,
        EstlIgstPercent=x.EstlIgstPercent, EstlIgstAmount=x.EstlIgstAmount,
        EstlTotal=x.EstlTotal, EstlSgstAmount=x.EstlSgstAmount, EstlSgstPercent=x.EstlSgstPercent,
        ProductId=x.ProductId, Metal=x.Metal, TaxAmount=x.TaxAmount, TaxPercent=x.TaxPercent,
        TaxType=x.TaxType, VaAmount=x.VaAmount, VaPercent=x.VaPercent, EstNote=x.EstNote,
        EstimateHdrGkey=headerGkey, EstimateId=estimateNumber, CreatedBy=x.CreatedBy,
        CreatedOn=x.CreatedOn, ModifiedBy=x.ModifiedBy, ModifiedOn=x.ModifiedOn,
        TenantGkey=tenantGkey, ProductSku=x.ProductSku
    };
}
