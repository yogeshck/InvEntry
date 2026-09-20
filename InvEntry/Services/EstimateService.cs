using InvEntry.Models;
using InvEntry.Contracts.Estimates;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IEstimateService
    {
        Task<EstimateHeader> GetHeader(string estNbr);

        Task<EstimateHeader> CreateHeader(EstimateHeader estHdr);

        Task<SaveEstimateResponse> SaveAsync(EstimateHeader header, IEnumerable<EstimateLine> lines);

        Task UpdateHeader(EstimateHeader estHdr);

        Task<IEnumerable<EstimateHeader>> GetAll(DateSearchOption options);

        Task CreateEstimateLine(EstimateLine line);

        Task CreateEstimateLine(IEnumerable<EstimateLine> lines);
    }

    public class EstimateService : IEstimateService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public EstimateService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<EstimateHeader> GetHeader(string estNbr)
        {
            return await _mijmsApiService.Get<EstimateHeader>($"api/estimate/{estNbr}");
        }

        public async Task<EstimateHeader> CreateHeader(EstimateHeader estHdr)
        {
            return await _mijmsApiService.Post($"api/estimate/", estHdr);
        }

        public async Task<SaveEstimateResponse> SaveAsync(EstimateHeader header, IEnumerable<EstimateLine> lines)
        {
            var request = new SaveEstimateRequest
            {
                Header = new EstimateHeaderSaveModel
                {
                    EstDate = header.EstDate, PaymentMode = header.PaymentMode,
                    CustGkey = header.CustGkey, CustMobile = header.CustMobile,
                    EstRefund = header.EstRefund, EstTaxableAmount = header.EstlTaxableAmount,
                    IsTaxApplicable = header.IsTaxApplicable, GrossRcbAmount = header.GrossRcbAmount,
                    OldGoldAmount = header.OldGoldAmount, OldSilverAmount = header.OldSilverAmount,
                    TaxType = header.TaxType, GstLocSeller = header.GstLocSeller,
                    GstLocBuyer = header.GstLocBuyer, CgstPercent = header.CgstPercent,
                    CgstAmount = header.CgstAmount, SgstPercent = header.SgstPercent,
                    SgstAmount = header.SgstAmount, IgstPercent = header.IgstPercent,
                    IgstAmount = header.IgstAmount, AmountPayable = header.AmountPayable,
                    DiscountPercent = header.DiscountPercent, DiscountAmount = header.DiscountAmount,
                    AdvanceAdj = header.AdvanceAdj, PaymentDueDate = header.PaymentDueDate,
                    RdAmountAdj = header.RdAmountAdj, EstBalance = header.EstBalance,
                    RecdAmount = header.RecdAmount, RoundOff = header.RoundOff,
                    EstNotes = header.EstNotes, EstlTaxTotal = header.EstlTaxTotal,
                    CreatedBy = header.CreatedBy, CreatedOn = header.CreatedOn,
                    ModifiedBy = header.ModifiedBy, ModifiedOn = header.ModifiedOn,
                    TenantGkey = header.TenantGkey
                },
                Lines = lines.Select(MapLine).ToList()
            };

            return await _mijmsApiService.Post<SaveEstimateRequest, SaveEstimateResponse>(
                "api/estimate/save",
                request);
        }

        private static EstimateLineSaveModel MapLine(EstimateLine line) => new()
        {
            HsnCode=line.HsnCode, EstlBilledPrice=line.EstlBilledPrice,
            EstlGrossAmt=line.EstlGrossAmt, EstlMakingCharges=line.EstlMakingCharges,
            EstlOtherCharges=line.EstlOtherCharges, EstlPayableAmt=line.EstlPayableAmt,
            EstlStoneAmount=line.EstlStoneAmount, EstlTaxableAmount=line.EstlTaxableAmount,
            EstlWastageAmt=line.EstlWastageAmt, ProdCategory=line.ProdCategory,
            ProdGrossWeight=line.ProdGrossWeight, ProdNetWeight=line.ProdNetWeight,
            ProdQty=line.ProdQty, ProdStoneWeight=line.ProdStoneWeight,
            ProductDesc=line.ProductDesc, ProductGkey=line.ProductGkey,
            ProductName=line.ProductName, ProdPackCode=line.ProdPackCode,
            ProductPurity=line.ProductPurity, IsTaxable=line.IsTaxable,
            ItemNotes=line.ItemNotes, ItemPacked=line.ItemPacked,
            EstlCgstPercent=line.EstlCgstPercent, EstlCgstAmount=line.EstlCgstAmount,
            EstlIgstPercent=line.EstlIgstPercent, EstlIgstAmount=line.EstlIgstAmount,
            EstlTotal=line.EstlTotal, EstlSgstAmount=line.EstlSgstAmount,
            EstlSgstPercent=line.EstlSgstPercent, ProductId=line.ProductId,
            Metal=line.Metal, TaxAmount=line.TaxAmount, TaxPercent=line.TaxPercent,
            TaxType=line.TaxType, VaAmount=line.VaAmount, VaPercent=line.VaPercent,
            EstNote=line.EstNote, CreatedBy=line.CreatedBy, CreatedOn=line.CreatedOn,
            ModifiedBy=line.ModifiedBy, ModifiedOn=line.ModifiedOn,
            TenantGkey=line.TenantGkey, ProductSku=line.ProductSku
        };
        public async Task UpdateHeader(EstimateHeader estHdr)
        {
            await _mijmsApiService.Put($"api/estimate/{estHdr.EstNbr}", estHdr);
        }

        public async Task CreateEstimateLine(EstimateLine line)
        {
            await _mijmsApiService.Post($"api/estimateline/", line);
        }

        public async Task CreateEstimateLine(IEnumerable<EstimateLine> lines)
        {
            var list = new List<Task>();

            foreach (var line in lines)
                list.Add(CreateEstimateLine(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<EstimateHeader>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<EstimateHeader, DateSearchOption>($"api/estimate/filter", options);


        }
    }
}
