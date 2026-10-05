# InvEntry Historical Invoice to GSTR-1 Migration Process

## Purpose

This procedure is intended for use whenever an existing InvEntry installation, branch, or customer database needs to bring historical invoices into the current GSTR-1 processing workflow.

The process ensures that:

- genuine completed invoices are identified correctly;
- incomplete, cancelled, or test invoices are excluded;
- historical invoice status is corrected where necessary;
- financial and GST values are validated;
- invoices are classified into the correct GSTR-1 categories;
- duplicate GST staging is prevented;
- GSTR-1 data is reconciled before JSON export.

The process should be performed in two separate phases:

**Phase A — Historical Invoice Normalisation**

Historical invoice → Validate → Correct status → Reconcile financial values

**Phase B — GST Backfill**

FINALISED invoice → GST classification → Staging → Validation → Export

---

# PART 1 — USER / OPERATIONS PROCESS

## 1. Select the Migration Period

First identify the date from which the branch should start using the new GST workflow.

Example:

**GST Cutover Date: 01-Sep-2026**

This means:

- invoices before the cutover date normally belong to already-filed historical GST periods;
- invoices from the cutover date onward must be processed through the current GSTR-1 workflow.

For example:

```text
Up to 31-Aug-2026
Historical / already-filed period

From 01-Sep-2026
Current GSTR-1 workflow
```

Historical periods that have already been filed should normally not be recreated unless there is a specific correction requirement.

---

## 2. Load Historical Invoices

Run the Historical Invoice Audit for the required period.

Example:

```text
From Date: 01-Sep-2026
To Date:   30-Sep-2026
```

The audit should list invoices belonging to that period.

At this point the system is only reviewing the invoices.

No GST data should be created or modified automatically.

---

## 3. Identify Genuine Sales Invoices

Each historical invoice should be reviewed to determine whether it represents a genuine completed sale.

Typical classifications are:

```text
Confirmed Production
Test Transaction
Needs Investigation
Unreviewed
```

Only invoices classified as:

```text
Confirmed Production
```

should proceed through the migration.

A record should not proceed when it is:

- a test invoice;
- cancelled;
- partially entered;
- abandoned;
- incomplete;
- duplicated;
- otherwise doubtful.

---

## 4. Correct Historical Invoice Status

Some genuine historical invoices may still show:

```text
DRAFT
```

because they were created under an older version of InvEntry.

Once the operator confirms that the invoice represents a completed sale, its status may be corrected to:

```text
FINALISED
```

The rule is:

```text
Confirmed genuine sale
+ complete invoice
+ not cancelled
+ not test data
+ currently DRAFT
        ↓
Change to FINALISED
```

Changing the status to `FINALISED` does not automatically mean that the invoice has been included in GSTR-1.

Invoice finalisation and GST processing are separate activities.

---

## 5. Run the Audit Again

After historical invoice status has been corrected, run the Historical Invoice Audit again.

The system should verify:

- invoice number;
- invoice date;
- taxable value;
- CGST;
- SGST;
- IGST;
- invoice value;
- amount payable;
- round-off;
- invoice lines;
- customer information;
- HSN details.

Any invoice showing inconsistent totals should be marked:

```text
Needs Investigation
```

and should not proceed to GST processing.

---

## 6. Check Taxable Value and Tax Amount

The audit should confirm that taxable value and tax amounts are reasonable.

The basic tax relationship is:

```text
Tax Total
=
CGST + SGST + IGST
```

The approximate invoice relationship is:

```text
Taxable Value
+ GST
+ Other Charges
+/- Round Off
- Discount
=
Invoice Value
```

The exact calculation should follow the InvEntry invoice calculation rules.

If the values do not reconcile, the invoice must be investigated before continuing.

---

## 7. Validate Customer GST Information

The system should identify whether the customer is:

```text
Registered Customer
```

or:

```text
Unregistered Customer
```

A registered customer should have a valid GSTIN.

The customer's state / place of supply should also be correct.

For transactions within the same state:

```text
CGST + SGST
```

normally applies.

For transactions between different states:

```text
IGST
```

normally applies.

Any mismatch should be reviewed before continuing.

---

## 8. Run GSTR-1 Dry-Run

After the invoices have passed the historical audit, run the GSTR-1 Backfill Dry-Run.

The dry-run should show what would happen without modifying the live GST staging data.

Typical output should include:

- invoice number;
- invoice date;
- customer GSTIN where applicable;
- taxable value;
- invoice value;
- CGST;
- SGST;
- IGST;
- GSTR-1 category;
- validation result;
- HSN information;
- processing status.

The dry-run provides an opportunity for the operator to review the migration before committing anything.

---

## 9. Review GSTR-1 Classification

Each invoice should be classified according to GST rules.

Typical examples include:

```text
B2B
B2C
HSN
Credit / Debit Note
Export
```

For normal retail invoices:

```text
Valid customer GSTIN
        ↓
B2B
```

and generally:

```text
No customer GSTIN
        ↓
B2C
```

The GST classification should be reviewed before staging.

---

## 10. Validate HSN Information

All invoice items should have valid HSN details.

Typical required values include:

- HSN code;
- description;
- quantity;
- UQC / unit;
- taxable value;
- GST rate;
- tax amount.

If any product is missing HSN information, the invoice should not proceed automatically.

---

## 11. Approve Records for GSTR-1 Staging

An invoice should proceed to staging only when all required checks have passed.

The basic rule is:

```text
Confirmed Production
AND
FINALISED
AND
Financial totals correct
AND
GST details valid
AND
GST classification successful
AND
HSN valid
AND
Not already staged
        ↓
Eligible for GSTR-1 staging
```

---

## 12. Create GSTR-1 Staging Records

Once approved, the system creates GSTR-1 staging records.

The staging area represents the GST return data that will eventually be exported.

The original sales invoice remains unchanged apart from any approved historical status correction.

---

## 13. Reconcile Staged Data

After staging, compare the source invoices and the GSTR-1 staging records.

The following totals should reconcile:

```text
Invoice Count
Taxable Value
CGST
SGST
IGST
Invoice Value
```

Category totals should also be checked.

Example:

```text
B2B Total
+
B2C Total
+
Other applicable categories
=
Eligible Sales Total
```

Differences should be investigated before JSON export.

---

## 14. Mark Records Ready for Export

Only successfully validated staging records should become exportable.

The processing flow should therefore be:

```text
Historical Invoice
        ↓
FINALISED
        ↓
GST Classified
        ↓
Staged
        ↓
Validated
        ↓
Export Ready
```

---

## 15. Generate GSTR-1 JSON

Once reconciliation is complete, generate the GSTR-1 JSON file.

Before uploading to the GST portal, verify:

- return period;
- supplier GSTIN;
- invoice count;
- B2B totals;
- B2C totals;
- HSN totals;
- taxable value;
- CGST;
- SGST;
- IGST.

The generated JSON can then proceed through the normal GST filing process.

---

# PART 2 — TECHNICAL PROCESS

## 1. Define the GST Cutover

The application should have an explicit GST cutover date.

Example:

```csharp
var gstCutoverDate = new DateTime(2026, 9, 1);
```

The migration scope becomes:

```csharp
invoice.InvoiceDate >= gstCutoverDate
```

For a specific migration period:

```csharp
invoice.InvoiceDate >= fromDate
&& invoice.InvoiceDate <= toDate
```

Periods already filed should normally be excluded.

---

## 2. Historical Candidate Selection

Historical records should first be selected without modifying them.

Conceptual eligibility rule:

```csharp
bool IsHistoricalGstr1Candidate(Invoice invoice)
{
    return invoice.InvoiceDate >= fromDate
        && invoice.InvoiceDate <= toDate
        && invoice.DocumentType == "SalesInvoice"
        && !invoice.IsCancelled;
}
```

At this stage the operation must remain read-only.

---

## 3. Determine Whether the Invoice Represents a Completed Sale

A historical invoice should satisfy basic completeness rules.

Example:

```csharp
bool LooksLikeCompletedSale(Invoice invoice)
{
    return invoice.Lines.Any()
        && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
        && invoice.AmountPayable > 0;
}
```

Additional project-specific checks may include:

```text
Customer exists
Invoice lines exist
Document number exists
Quantity / weight exists
Financial amount exists
Not cancelled
Not test data
```

---

## 4. Historical Status Normalisation

Historical completion should be handled independently of GST processing.

Logical statement:

```text
IF
    Historical GST candidate
AND ConfirmedProduction
AND Complete
AND NotCancelled
AND CurrentStatus = DRAFT
THEN
    Status = FINALISED
```

Conceptual implementation:

```csharp
if (
    audit.IsHistoricalCandidate &&
    audit.IsConfirmedProduction &&
    audit.IsComplete &&
    invoice.Status == "DRAFT")
{
    invoice.Status = "FINALISED";
}
```

Do not trigger GSTR-1 staging automatically from the status change.

---

## 5. Resolve Historical Taxable Value

Legacy data may have incomplete header values.

A fallback should be used where appropriate.

Conceptually:

```csharp
decimal resolvedTaxableValue =
    invoice.HeaderTaxableValue > 0
        ? invoice.HeaderTaxableValue
        : invoice.Lines.Sum(x => x.TaxableAmount);
```

This should be treated as a migration-time resolved value unless business rules explicitly require source-data correction.

---

## 6. Resolve Tax Total

GST tax must be derived from its GST components.

Correct formula:

```csharp
decimal taxTotal =
      invoice.CgstAmount
    + invoice.SgstAmount
    + invoice.IgstAmount;
```

The application must not assume that a legacy field named similar to `TaxTotal` necessarily represents GST tax without validating its historical meaning.

---

## 7. Financial Reconciliation

Resolve expected invoice value using the current invoice rules.

Conceptually:

```csharp
var expectedValue =
      resolvedTaxableValue
    + invoice.CgstAmount
    + invoice.SgstAmount
    + invoice.IgstAmount
    + invoice.OtherCharges
    + invoice.RoundOff
    - invoice.DiscountAmount;
```

Then:

```csharp
var difference =
    Math.Abs(expectedValue - invoice.AmountPayable);
```

Use a defined tolerance:

```csharp
var reconciled = difference <= 0.01m;
```

Records outside tolerance should become:

```text
NeedsInvestigation
```

and should not stage automatically.

---

## 8. Validate Company GSTIN

The supplier GSTIN passed into the migration should match the configured company.

Conceptually:

```csharp
if (request.SupplierGstin != company.Gstin)
{
    throw new ValidationException(
        "Supplier GSTIN does not match the configured company.");
}
```

This prevents processing another company's records by mistake.

---

## 9. Validate Date Range

The migration endpoint should validate:

```text
FromDate <= ToDate
```

and enforce a reasonable maximum interval.

Example:

```text
Maximum interval = 366 days
```

This prevents accidental bulk processing of the entire database.

---

## 10. Determine Registered / Unregistered Customer

Conceptually:

```csharp
bool isRegistered =
    IsValidGstin(invoice.CustomerGstin);
```

Then:

```csharp
if (isRegistered)
{
    classification.CustomerType = "Registered";
}
else
{
    classification.CustomerType = "Unregistered";
}
```

---

## 11. Determine Intrastate / Interstate Supply

Conceptually:

```csharp
bool isIntrastate =
    supplierStateCode == placeOfSupplyStateCode;
```

Validation rules:

```text
Intrastate:
CGST >= 0
SGST >= 0
IGST = 0
```

```text
Interstate:
CGST = 0
SGST = 0
IGST >= 0
```

Example validation:

```csharp
if (isIntrastate && invoice.IgstAmount > 0)
{
    errors.Add("IGST present for intrastate supply.");
}

if (!isIntrastate &&
    (invoice.CgstAmount > 0 || invoice.SgstAmount > 0))
{
    errors.Add(
        "CGST/SGST present for interstate supply.");
}
```

---

## 12. GST Classification

Create a GST classification request from resolved invoice values.

Conceptual model:

```csharp
var request = new GstClassificationRequest
{
    DocumentType = GstDocumentType.SalesInvoice,

    InvoiceNumber = invoice.InvoiceNumber,
    InvoiceDate = invoice.InvoiceDate,

    SupplierStateCode = company.StateCode,
    PlaceOfSupplyStateCode = placeOfSupply,

    RecipientGstin = customer.Gstin,

    TaxableValue = resolvedTaxableValue,
    InvoiceValue = invoice.AmountPayable,

    Cgst = invoice.CgstAmount,
    Sgst = invoice.SgstAmount,
    Igst = invoice.IgstAmount
};
```

The classification service should return values such as:

```text
ReturnCategory = B2B
Gstr1Table = B2B
```

or:

```text
ReturnCategory = B2C
Gstr1Table = appropriate B2C table
```

The detailed B2C classification should remain inside the GST classification service rather than being duplicated in the migration utility.

---

## 13. Validate HSN

Every GST-relevant invoice line should have a valid HSN.

Example:

```csharp
foreach (var line in invoice.Lines)
{
    if (string.IsNullOrWhiteSpace(line.HsnCode))
    {
        errors.Add(
            $"Missing HSN for product {line.ProductName}");
    }
}
```

HSN summary should normally group compatible rows by:

```text
HSN
GST Rate
UQC
```

and calculate:

```text
Quantity
Taxable Value
CGST
SGST
IGST
```

---

## 14. Dry-Run Model

Before mutation, create an in-memory dry-run result.

Example:

```csharp
public class HistoricalGstr1DryRunResult
{
    public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }

    public decimal TaxableValue { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal InvoiceValue { get; set; }

    public string ReturnCategory { get; set; }
    public string Gstr1Table { get; set; }

    public bool IsValid { get; set; }

    public IList<string> ValidationMessages { get; set; }
}
```

Internal database identifiers should remain internal and should not be exposed to operators in:

- UI;
- CSV exports;
- reports;
- messages;
- evidence files.

---

## 15. Staging Eligibility Predicate

Use a single explicit predicate before mutation.

Conceptually:

```csharp
bool CanStage(HistoricalInvoiceAuditItem item)
{
    return
        item.OperatorReview == "ConfirmedProduction"
        && item.InvoiceStatus == "FINALISED"
        && item.FinancialsReconciled
        && item.GstClassificationValid
        && item.HsnValid
        && !item.AlreadyStaged;
}
```

No record should bypass this gate.

---

## 16. Idempotency / Duplicate Protection

Migration commands must be safe to execute more than once.

Logical rule:

```text
IF same invoice already exists in GSTR-1 staging
THEN skip
ELSE insert
```

Application-level duplicate checking should be backed by a database constraint where practical.

Conceptual unique key:

```text
Supplier GSTIN
Return Period
Document Type
Document Number
```

Example:

```sql
CREATE UNIQUE INDEX UX_GSTR1_STAGING_DOCUMENT
ON GSTR1_STAGING
(
    SUPPLIER_GSTIN,
    RETURN_PERIOD,
    DOCUMENT_TYPE,
    DOCUMENT_NUMBER
);
```

Actual table and column names should match the production schema.

---

## 17. Create Staging Snapshot

Once staging eligibility is confirmed, create immutable or controlled GST snapshot data.

Conceptual relationship:

```text
Invoice Header
        ↓
GSTR-1 Document Staging

Invoice Lines
        ↓
GSTR-1 Item / HSN Staging
```

The staging snapshot should contain the values intended for filing rather than relying indefinitely on mutable invoice data.

---

## 18. Post-Staging Reconciliation

After staging, compare source and staging totals.

Count reconciliation:

```text
Eligible source invoice count
=
Staged invoice count
```

Financial reconciliation:

```text
SUM(Source Taxable)
=
SUM(Staging Taxable)
```

```text
SUM(Source CGST)
=
SUM(Staging CGST)
```

```text
SUM(Source SGST)
=
SUM(Staging SGST)
```

```text
SUM(Source IGST)
=
SUM(Staging IGST)
```

```text
SUM(Source Invoice Value)
=
SUM(Staging Invoice Value)
```

Any mismatch blocks export readiness.

---

## 19. Category Reconciliation

Reconcile GSTR-1 totals by category.

Example:

```text
B2B
Invoice Count
Taxable
CGST
SGST
IGST
Invoice Value
```

```text
B2C
Invoice Count
Taxable
CGST
SGST
IGST
Invoice Value
```

Then:

```text
B2B
+ B2C
+ Other Applicable Categories
=
Eligible September Sales
```

with documented exclusions for cancelled/test/invalid records.

---

## 20. Keep Invoice Status and GST Status Separate

Do not overload `InvoiceHeader.Status` with GST workflow states.

Invoice business status should remain something like:

```text
DRAFT
FINALISED
CANCELLED
```

GST processing should maintain a separate lifecycle such as:

```text
NOT_PROCESSED
AUDITED
CLASSIFIED
STAGED
VALIDATED
EXPORT_READY
EXPORTED
ERROR
```

Therefore a valid invoice may temporarily be:

```text
Invoice Status = FINALISED
GST Status     = NOT_PROCESSED
```

Later:

```text
Invoice Status = FINALISED
GST Status     = STAGED
```

and finally:

```text
Invoice Status = FINALISED
GST Status     = EXPORTED
```

---

## 21. Recommended Migration State Machine

The historical process can be represented as:

```text
Historical Invoice
        ↓
Candidate
        ↓
Operator Reviewed
        ↓
Confirmed Production
        ↓
FINALISED
        ↓
Financially Reconciled
        ↓
GST Classified
        ↓
HSN Validated
        ↓
Staged
        ↓
Staging Reconciled
        ↓
Export Ready
        ↓
JSON Exported
```

A record should never move to the next stage when the previous stage has failed.

---

## 22. Recommended Processing Algorithm

Conceptually:

```csharp
foreach (var invoice in historicalInvoices)
{
    if (!IsHistoricalGstr1Candidate(invoice))
        continue;

    var audit = Audit(invoice);

    if (!audit.IsConfirmedProduction)
        continue;

    if (invoice.Status == "DRAFT")
    {
        FinaliseHistoricalInvoice(invoice);
    }

    var normalized =
        NormalizeFinancialAndGstValues(invoice);

    if (!normalized.IsReconciled)
    {
        MarkNeedsInvestigation(invoice);
        continue;
    }

    var classification =
        ClassifyForGstr1(normalized);

    if (!classification.IsValid)
    {
        MarkNeedsInvestigation(invoice);
        continue;
    }

    if (!ValidateHsn(invoice))
    {
        MarkNeedsInvestigation(invoice);
        continue;
    }

    if (AlreadyStaged(invoice))
        continue;

    StageForGstr1(
        normalized,
        classification);
}
```

After the batch completes:

```csharp
ValidateStagedPeriod(returnPeriod);
```

Only after successful validation:

```csharp
MarkExportReady(returnPeriod);
```

Then:

```csharp
GenerateGstr1Json(returnPeriod);
```

---

# STANDARD BRANCH INSTALLATION CHECKLIST

Whenever this process is used for another branch or customer installation, record the following before beginning.

```text
Company:
Branch:
Supplier GSTIN:
Database:
GST Cutover Date:
Historical Period From:
Historical Period To:
Periods Already Filed:
Operator:
Migration Date:
Application Version:
Backend Version:
```

Then execute:

```text
1. Backup database
2. Confirm company GSTIN
3. Confirm GST cutover date
4. Identify historical period
5. Run historical audit
6. Classify test / production / investigation records
7. Correct genuine historical DRAFT invoices to FINALISED
8. Re-run audit
9. Reconcile taxable and GST values
10. Validate GSTIN / state / place of supply
11. Validate HSN
12. Run GSTR-1 dry-run
13. Review B2B/B2C classification
14. Check for existing staging records
15. Stage eligible records
16. Reconcile staging totals
17. Review category summaries
18. Mark validated records Export Ready
19. Generate JSON
20. Verify JSON totals
21. Preserve CSV / audit evidence
22. Proceed with GST portal filing process
```

---

# CORE SAFETY RULES

The following rules should remain mandatory for every branch migration.

### Rule 1 — Never blindly finalise all DRAFT invoices

```text
DRAFT
≠
Completed Sale
```

Only confirmed historical completed transactions should be corrected.

### Rule 2 — Finalisation does not equal GST filing

```text
FINALISED
≠
GSTR-1 Processed
```

GST classification and staging happen separately.

### Rule 3 — Never stage unreconciled financial data

```text
Financial mismatch
        ↓
Needs Investigation
```

### Rule 4 — Migration must be repeatable

Running the utility more than once must not create duplicate staging records.

### Rule 5 — Do not alter already-filed historical periods unnecessarily

Previously filed periods should remain unchanged unless a specific correction process has been approved.

### Rule 6 — Preserve evidence

Retain audit and reconciliation evidence for every migration.

### Rule 7 — Keep internal identifiers internal

Internal database keys must not be exposed in operator-facing migration screens, CSV files, reports, snapshots, or messages.

---

# FINAL PROCESS SUMMARY

## User View

```text
Find historical invoices
        ↓
Confirm genuine sales
        ↓
Correct old DRAFT invoices
        ↓
Check amounts and GST
        ↓
Run GSTR-1 dry-run
        ↓
Review GST classification
        ↓
Approve
        ↓
Stage
        ↓
Reconcile
        ↓
Generate JSON
```

## Technical View

```text
Candidate Selection
        ↓
Status Normalisation
        ↓
Financial Normalisation
        ↓
GST Validation
        ↓
GST Classification
        ↓
HSN Validation
        ↓
Idempotency Check
        ↓
GSTR-1 Staging
        ↓
Post-Staging Reconciliation
        ↓
Export Readiness
        ↓
JSON Generation
```

The two most important design principles are:

```text
Invoice Business Status
        and
GST Processing Status
```

must remain independent.

And:

```text
Historical Correction
        and
GST Backfill
```

must be separate controlled operations.