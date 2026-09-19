using InvEntry.Gst.Core.Models;
using InvEntry.Models;
using System;

namespace InvEntry.GST.Classification
{
    public static class InvoiceGstMapper
    {
        public static GstClassificationRequest Create(
            InvoiceHeader invoice,
            Customer customer,
            OrgThisCompanyView company)
        {
            ArgumentNullException.ThrowIfNull(invoice);
            ArgumentNullException.ThrowIfNull(customer);
            ArgumentNullException.ThrowIfNull(company);

            return new GstClassificationRequest
            {
                DocumentType =
                    GstDocumentType.SalesInvoice,

                DocumentNumber =
                    invoice.InvNbr,

                DocumentDate =
                    invoice.InvDate ?? DateTime.Today,

                // =====================================================
                // SELLER
                // =====================================================

                SupplierGstin = company.GstNbr,

                SupplierStateCode = company.GstCode,

                // =====================================================
                // RECIPIENT
                // =====================================================

                RecipientGstin =
                    customer.GstinNbr,

                RecipientStateCode =
                    customer.GstStateCode,

                // =====================================================
                // PLACE OF SUPPLY
                // =====================================================

                PlaceOfSupplyCode =
                    invoice.PlaceOfSupply,

                // =====================================================
                // VALUES
                // =====================================================

                TaxableValue =
                    invoice.InvlTaxableAmount.GetValueOrDefault(),

                InvoiceValue =
                    invoice.GrossRcbAmount.GetValueOrDefault(),

                CgstAmount =
                    invoice.CgstAmount.GetValueOrDefault(),

                SgstAmount =
                    invoice.SgstAmount.GetValueOrDefault(),

                IgstAmount =
                    invoice.IgstAmount.GetValueOrDefault(),

                CessAmount = 0M,

                // =====================================================
                // GST CHARACTERISTICS
                // =====================================================

                TaxTreatment =
                    invoice.IsTaxApplicable
                        ? GstTaxTreatment.Taxable
                        : GstTaxTreatment.NonGst,

                IsExport = false,
                IsSez = false,
                IsReverseCharge = false,
                IsDeemedExport = false,
                IsEcommerceSupply = false
            };
        }
    }
}