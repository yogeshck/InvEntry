using InvEntry.Gst.Core.Helpers;
using InvEntry.Gst.Core.Models;
using InvEntry.Gst.Core.Rules;
using System;

namespace InvEntry.Gst.Core.Classification
{
    public class GstClassificationService
        : IGstClassificationService
    {
        private readonly IGstRuleProvider _ruleProvider;

        public GstClassificationService(
            IGstRuleProvider ruleProvider)
        {
            _ruleProvider = ruleProvider;
        }

        public GstClassificationResult Classify(
            GstClassificationRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = new GstClassificationResult();

            // -----------------------------------------------------
            // 1. BASIC VALIDATION
            // -----------------------------------------------------

            ValidateBasicData(request, result);

            if (result.HasErrors)
            {
                result.IsValid = false;
                result.IsReportable = false;
                return result;
            }

            // -----------------------------------------------------
            // 2. RECIPIENT REGISTRATION
            // -----------------------------------------------------

            result.IsRecipientRegistered =
                !string.IsNullOrWhiteSpace(request.RecipientGstin);

            // -----------------------------------------------------
            // 3. SPECIAL DOCUMENT TYPES
            // -----------------------------------------------------

            if (request.DocumentType == GstDocumentType.Estimate)
            {
                ClassifyEstimate(request, result);
                Finish(result);
                return result;
            }

            // -----------------------------------------------------
            // 4. EXPORT
            // -----------------------------------------------------

            if (request.IsExport)
            {
                result.SupplyType = GstSupplyType.Export;
                result.TaxType =
                    request.IgstAmount > 0
                        ? GstTaxType.Igst
                        : GstTaxType.None;

                result.ReturnCategory = GstReturnCategory.Export;
                result.Gstr1Table = "6A";
                result.IsReportable = true;

                Finish(result);
                return result;
            }

            // -----------------------------------------------------
            // 5. SUPPLY TYPE
            // -----------------------------------------------------

            DetermineSupplyType(request, result);

            // -----------------------------------------------------
            // 6. EXPECTED TAX TYPE
            // -----------------------------------------------------

            DetermineTaxType(result);

            // -----------------------------------------------------
            // 7. VALIDATE ACTUAL TAX
            // -----------------------------------------------------

            ValidateTaxCombination(request, result);

            // -----------------------------------------------------
            // 8. GSTR-1 CLASSIFICATION
            // -----------------------------------------------------

            DetermineReturnCategory(request, result);

            // -----------------------------------------------------
            // 9. FINAL RESULT
            // -----------------------------------------------------

            Finish(result);

            return result;
        }

        private static void ValidateBasicData(
            GstClassificationRequest request,
            GstClassificationResult result)
        {
            if (request.DocumentDate == default)
            {
                result.Errors.Add(
                    "Document date is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.SupplierGstin))
            {
                result.Errors.Add(
                    "Supplier GSTIN is required.");
            }
            else if (!GstinHelper.IsValid(
                         request.SupplierGstin))
            {
                result.Errors.Add(
                    $"Supplier GSTIN '{request.SupplierGstin}' is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(request.RecipientGstin) &&
                !GstinHelper.IsValid(request.RecipientGstin))
            {
                result.Errors.Add(
                    $"Recipient GSTIN '{request.RecipientGstin}' is invalid.");
            }

            var gstinStateCode =
                    GstinHelper.GetStateCode(
                        request.SupplierGstin);

            if (!string.IsNullOrWhiteSpace(gstinStateCode) &&
                !string.Equals(
                    gstinStateCode,
                    request.SupplierStateCode?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(
                    $"Supplier GSTIN state code '{gstinStateCode}' " +
                    $"does not match seller state code " +
                    $"'{request.SupplierStateCode}'.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.SupplierStateCode))
            {
                result.Errors.Add(
                    "Supplier GST state code is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.PlaceOfSupplyCode))
            {
                result.Errors.Add(
                    "Place of Supply is required.");
            }

            if (request.InvoiceValue < 0)
            {
                result.Errors.Add(
                    "Invoice value cannot be negative.");
            }

            if (request.TaxableValue < 0)
            {
                result.Errors.Add(
                    "Taxable value cannot be negative.");
            }
        }

        private static void DetermineSupplyType(
            GstClassificationRequest request,
            GstClassificationResult result)
        {
            var supplierState =
                NormalizeStateCode(
                    request.SupplierStateCode);

            var placeOfSupply =
                NormalizeStateCode(
                    request.PlaceOfSupplyCode);

            if (supplierState == placeOfSupply)
            {
                result.SupplyType =
                    GstSupplyType.IntraState;
            }
            else
            {
                result.SupplyType =
                    GstSupplyType.InterState;
            }
        }

        private static void DetermineTaxType(
            GstClassificationResult result)
        {
            result.TaxType =
                result.SupplyType switch
                {
                    GstSupplyType.IntraState
                        => GstTaxType.CgstSgst,

                    GstSupplyType.InterState
                        => GstTaxType.Igst,

                    GstSupplyType.Export
                        => GstTaxType.Igst,

                    _ => GstTaxType.None
                };
        }

        private static void ValidateTaxCombination(
            GstClassificationRequest request,
            GstClassificationResult result)
        {
            const decimal tolerance = 0.01M;

            var hasCgst =
                request.CgstAmount > tolerance;

            var hasSgst =
                request.SgstAmount > tolerance;

            var hasIgst =
                request.IgstAmount > tolerance;

            if (result.SupplyType ==
                GstSupplyType.IntraState)
            {
                if (hasIgst)
                {
                    result.Errors.Add(
                        "IGST has been applied to an " +
                        "intra-state supply. Expected CGST/SGST.");
                }

                if (hasCgst != hasSgst)
                {
                    result.Errors.Add(
                        "For an intra-state taxable supply, " +
                        "CGST and SGST must both be present.");
                }
            }

            if (result.SupplyType ==
                GstSupplyType.InterState)
            {
                if (hasCgst || hasSgst)
                {
                    result.Errors.Add(
                        "CGST/SGST has been applied to an " +
                        "inter-state supply. Expected IGST.");
                }
            }
        }

        private void DetermineReturnCategory(
            GstClassificationRequest request,
            GstClassificationResult result)
        {
            // -----------------------------------------------------
            // Registered recipient
            // -----------------------------------------------------

            if (result.IsRecipientRegistered)
            {
                result.ReturnCategory =
                    GstReturnCategory.B2B;

                result.Gstr1Table = "4A";
                result.IsReportable = true;

                return;
            }

            // -----------------------------------------------------
            // Unregistered recipient
            // -----------------------------------------------------

            if (result.SupplyType ==
                GstSupplyType.InterState)
            {
                var threshold =
                    _ruleProvider.GetB2ClThreshold(
                        request.DocumentDate);

                if (request.InvoiceValue > threshold)
                {
                    result.ReturnCategory =
                        GstReturnCategory.B2CL;

                    result.Gstr1Table = "5";
                    result.IsReportable = true;

                    return;
                }
            }

            result.ReturnCategory =
                GstReturnCategory.B2CS;

            result.Gstr1Table = "7";
            result.IsReportable = true;
        }

        private static void ClassifyEstimate(
            GstClassificationRequest request,
            GstClassificationResult result)
        {
            DetermineSupplyType(request, result);

            DetermineTaxType(result);

            result.ReturnCategory =
                GstReturnCategory.NotApplicable;

            result.Gstr1Table = null;

            // Important:
            // Estimate can use GST classification/calculation
            // but must not enter GSTR-1.
            result.IsReportable = false;
        }

        private static void Finish(
            GstClassificationResult result)
        {
            result.IsValid =
                result.Errors.Count == 0;

            if (!result.IsValid)
            {
                result.IsReportable = false;
            }
        }

        private static string NormalizeStateCode(
            string? value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}