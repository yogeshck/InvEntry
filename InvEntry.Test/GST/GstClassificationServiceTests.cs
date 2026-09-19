using InvEntry.Gst.Core.Classification;
using InvEntry.Gst.Core.Models;
using InvEntry.Gst.Core.Rules;

namespace InvEntry.Tests.GST
{
    [TestFixture]
    public class GstClassificationServiceTests
    {
        private GstClassificationService _service = null!;

        [SetUp]
        public void Setup()
        {
            IGstRuleProvider rules =
                new GstRuleProvider();

            _service =
                new GstClassificationService(rules);
        }

        // =========================================================
        // 1. REGISTERED BUYER - SAME STATE
        // =========================================================

        [Test]
        public void RegisteredBuyer_SameState_ShouldBeB2B()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin =
                "33BBBBB0000B1Z5";

            request.RecipientStateCode = "33";
            request.PlaceOfSupplyCode = "33";

            request.InvoiceValue = 51500M;
            request.TaxableValue = 50000M;

            request.CgstAmount = 750M;
            request.SgstAmount = 750M;
            request.IgstAmount = 0M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.IsReportable,
                    Is.True);

                Assert.That(
                    result.IsRecipientRegistered,
                    Is.True);

                Assert.That(
                    result.SupplyType,
                    Is.EqualTo(
                        GstSupplyType.IntraState));

                Assert.That(
                    result.TaxType,
                    Is.EqualTo(
                        GstTaxType.CgstSgst));

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.B2B));

                Assert.That(
                    result.Gstr1Table,
                    Is.EqualTo("4A"));

                Assert.That(
                    result.Errors,
                    Is.Empty);
            });
        }

        // =========================================================
        // 2. REGISTERED BUYER - INTERSTATE
        // =========================================================

        [Test]
        public void RegisteredBuyer_InterState_ShouldBeB2B()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin =
                "29BBBBB0000B1Z5";

            request.RecipientStateCode = "29";
            request.PlaceOfSupplyCode = "29";

            request.InvoiceValue = 59000M;
            request.TaxableValue = 50000M;

            request.CgstAmount = 0M;
            request.SgstAmount = 0M;
            request.IgstAmount = 9000M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.IsReportable,
                    Is.True);

                Assert.That(
                    result.SupplyType,
                    Is.EqualTo(
                        GstSupplyType.InterState));

                Assert.That(
                    result.TaxType,
                    Is.EqualTo(
                        GstTaxType.Igst));

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.B2B));

                Assert.That(
                    result.Gstr1Table,
                    Is.EqualTo("4A"));
            });
        }

        // =========================================================
        // 3. UNREGISTERED BUYER - SAME STATE
        // =========================================================

        [Test]
        public void UnregisteredBuyer_SameState_ShouldBeB2CS()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin = null;
            request.RecipientStateCode = "33";
            request.PlaceOfSupplyCode = "33";

            request.InvoiceValue = 51500M;
            request.TaxableValue = 50000M;

            request.CgstAmount = 750M;
            request.SgstAmount = 750M;
            request.IgstAmount = 0M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.IsRecipientRegistered,
                    Is.False);

                Assert.That(
                    result.SupplyType,
                    Is.EqualTo(
                        GstSupplyType.IntraState));

                Assert.That(
                    result.TaxType,
                    Is.EqualTo(
                        GstTaxType.CgstSgst));

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.B2CS));

                Assert.That(
                    result.Gstr1Table,
                    Is.EqualTo("7"));
            });
        }

        // =========================================================
        // 4. UNREGISTERED BUYER - INTERSTATE BELOW THRESHOLD
        // =========================================================

        [Test]
        public void UnregisteredBuyer_InterState_BelowThreshold_ShouldBeB2CS()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin = null;
            request.RecipientStateCode = "29";
            request.PlaceOfSupplyCode = "29";

            request.InvoiceValue = 50000M;
            request.TaxableValue = 45000M;

            request.CgstAmount = 0M;
            request.SgstAmount = 0M;
            request.IgstAmount = 5000M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.SupplyType,
                    Is.EqualTo(
                        GstSupplyType.InterState));

                Assert.That(
                    result.TaxType,
                    Is.EqualTo(
                        GstTaxType.Igst));

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.B2CS));

                Assert.That(
                    result.Gstr1Table,
                    Is.EqualTo("7"));
            });
        }

        // =========================================================
        // 5. UNREGISTERED BUYER - INTERSTATE ABOVE THRESHOLD
        // =========================================================

        [Test]
        public void UnregisteredBuyer_InterState_AboveThreshold_ShouldBeB2CL()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin = null;
            request.RecipientStateCode = "29";
            request.PlaceOfSupplyCode = "29";

            request.InvoiceValue = 150000M;
            request.TaxableValue = 140000M;

            request.CgstAmount = 0M;
            request.SgstAmount = 0M;
            request.IgstAmount = 10000M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.IsRecipientRegistered,
                    Is.False);

                Assert.That(
                    result.SupplyType,
                    Is.EqualTo(
                        GstSupplyType.InterState));

                Assert.That(
                    result.TaxType,
                    Is.EqualTo(
                        GstTaxType.Igst));

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.B2CL));

                Assert.That(
                    result.Gstr1Table,
                    Is.EqualTo("5"));
            });
        }

        // =========================================================
        // 6. REGISTERED BUYER ABOVE B2CL THRESHOLD
        // MUST STILL BE B2B
        // =========================================================

        [Test]
        public void RegisteredBuyer_AboveThreshold_ShouldRemainB2B()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin =
                "29BBBBB0000B1Z5";

            request.RecipientStateCode = "29";
            request.PlaceOfSupplyCode = "29";

            request.InvoiceValue = 150000M;
            request.TaxableValue = 140000M;

            request.CgstAmount = 0M;
            request.SgstAmount = 0M;
            request.IgstAmount = 10000M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.IsRecipientRegistered,
                    Is.True);

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.B2B));

                Assert.That(
                    result.Gstr1Table,
                    Is.EqualTo("4A"));
            });
        }

        // =========================================================
        // 7. INTRA-STATE BUT IGST APPLIED
        // =========================================================

        [Test]
        public void IntraState_WithIgst_ShouldFail()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin =
                "33BBBBB0000B1Z5";

            request.RecipientStateCode = "33";
            request.PlaceOfSupplyCode = "33";

            request.CgstAmount = 0M;
            request.SgstAmount = 0M;
            request.IgstAmount = 1500M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.False);

                Assert.That(
                    result.IsReportable,
                    Is.False);

                Assert.That(
                    result.Errors.Any(x =>
                        x.Contains(
                            "IGST",
                            StringComparison.OrdinalIgnoreCase)),
                    Is.True);
            });
        }

        // =========================================================
        // 8. INTERSTATE BUT CGST/SGST APPLIED
        // =========================================================

        [Test]
        public void InterState_WithCgstSgst_ShouldFail()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin =
                "29BBBBB0000B1Z5";

            request.RecipientStateCode = "29";
            request.PlaceOfSupplyCode = "29";

            request.CgstAmount = 750M;
            request.SgstAmount = 750M;
            request.IgstAmount = 0M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.False);

                Assert.That(
                    result.IsReportable,
                    Is.False);

                Assert.That(
                    result.Errors.Any(x =>
                        x.Contains(
                            "CGST/SGST",
                            StringComparison.OrdinalIgnoreCase)),
                    Is.True);
            });
        }

        // =========================================================
        // 9. INVALID RECIPIENT GSTIN
        // =========================================================

        [Test]
        public void InvalidRecipientGstin_ShouldFail()
        {
            var request = CreateBaseRequest();

            request.RecipientGstin =
                "INVALIDGSTIN";

            request.RecipientStateCode = "33";
            request.PlaceOfSupplyCode = "33";

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.False);

                Assert.That(
                    result.IsReportable,
                    Is.False);

                Assert.That(
                    result.Errors.Any(x =>
                        x.Contains(
                            "Recipient GSTIN",
                            StringComparison.OrdinalIgnoreCase)),
                    Is.True);
            });
        }

        // =========================================================
        // 10. ESTIMATE - CLASSIFIABLE BUT NOT REPORTABLE
        // =========================================================

        [Test]
        public void Estimate_ShouldNotBeGstr1Reportable()
        {
            var request = CreateBaseRequest();

            request.DocumentType =
                GstDocumentType.Estimate;

            request.RecipientGstin =
                "33BBBBB0000B1Z5";

            request.RecipientStateCode = "33";
            request.PlaceOfSupplyCode = "33";

            request.CgstAmount = 750M;
            request.SgstAmount = 750M;
            request.IgstAmount = 0M;

            var result =
                _service.Classify(request);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsValid,
                    Is.True);

                Assert.That(
                    result.IsReportable,
                    Is.False);

                Assert.That(
                    result.ReturnCategory,
                    Is.EqualTo(
                        GstReturnCategory.NotApplicable));

                Assert.That(
                    result.Gstr1Table,
                    Is.Null);

                Assert.That(
                    result.SupplyType,
                    Is.EqualTo(
                        GstSupplyType.IntraState));

                Assert.That(
                    result.TaxType,
                    Is.EqualTo(
                        GstTaxType.CgstSgst));
            });
        }

        // =========================================================
        // COMMON REQUEST
        // =========================================================

        private static GstClassificationRequest
            CreateBaseRequest()
        {
            return new GstClassificationRequest
            {
                DocumentType =
                    GstDocumentType.SalesInvoice,

                DocumentNumber =
                    "TEST-001",

                // Fixed date intentionally used.
                // GST rules can be effective-date dependent.
                DocumentDate =
                    new DateTime(2026, 9, 16),

                // Tamil Nadu seller
                SupplierGstin =
                    "33AAAAA0000A1Z5",

                SupplierStateCode =
                    "33",

                RecipientStateCode =
                    "33",

                PlaceOfSupplyCode =
                    "33",

                TaxableValue =
                    50000M,

                InvoiceValue =
                    51500M,

                CgstAmount =
                    750M,

                SgstAmount =
                    750M,

                IgstAmount =
                    0M,

                CessAmount =
                    0M,

                TaxTreatment =
                    GstTaxTreatment.Taxable
            };
        }
    }
}