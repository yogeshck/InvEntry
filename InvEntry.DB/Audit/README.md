# InvEntry deployment prerequisite and readiness audit

Audit date: 2026-09-27  
Scope: repository source and SQL project artifacts only; no live database was available.  
Method: static source/schema inspection and `dotnet build InvEntry.sln --no-restore`. No source code or database data was changed.

## Executive finding

Deployment is **not ready without site-specific validation**. The principal blockers are:

1. There is no seed/post-deployment data script for required voucher types, company/address, references, products, or GST data.
2. Two divergent database projects exist. `InvEntry.DB` has customer-order/reporting objects but lacks stock-transfer tables; `InvEntry.Database` has stock-transfer tables but lacks customer-order and several reporting objects.
3. The EF model expects objects absent from the checked-in SQL projects, including `GST_GSTR1_DOCUMENT`, `GST_GSTR1_DOCUMENT_LINE`, `DAILY_REP_STOCK_MOVEMENT`, `DAILY_REP_STOCK_SUMRY_MOVEMENT`, `ITEM_RCT_GRN_DBVIEW`, and `PRODUCT_STOCK_TMP` (`DataAccess/Models/MijmsContext.cs:488`, `:538`, `:1177`, `:1289`, `:1743`, `:2817`).
4. Production API address, report output folders, log folders, and barcode printer name are hardcoded.
5. A JWT signing key and a DevExpress feed credential are committed in configuration. They must be rotated and supplied securely.
6. The SQL project requires Visual Studio/SSDT and fails under `dotnet build` because `Microsoft.Data.Tools.Schema.SqlTasks.targets` is unavailable.
7. Workshop transactions are not deployment-ready: `WorkshopIssue` and `WorkshopReceipt` are enum values only, with no active posting workflow found, and the centralized movement service rejects every stock-in direction.
8. Material Receipt/GRN uses a separate legacy client-side sequence of API calls rather than the centralized atomic stock workflow. Partial saves and incomplete movement history are therefore possible.

The authoritative row-level checklist is [prerequisites.csv](prerequisites.csv). Run [validate-prerequisites.sql](validate-prerequisites.sql) against the intended database and retain its result sets as deployment evidence.

## Document and voucher inventory

| Document / transaction | Application expectation | Numbering dependency | Classification |
|---|---|---|---|
| Sale Invoice | Exact `VOUCHER_TYPES.DOCUMENT_TYPE = 'Sale Invoice'`; states `DRAFT`, `FINAL`, `CANCELLED` | Prefix, length, last-used number | Mandatory for billing |
| Estimate | Exact `DOCUMENT_TYPE = 'Estimate'` | Prefix, length, last-used number | Feature-specific |
| GRN | Exact `DOCUMENT_TYPE = 'GRN'`; open lookup uses status `Open` | Prefix and last-used number; implementation fixes numeric width at 4 | Feature-specific |
| Customer Order | Exact `DOCUMENT_TYPE = 'Customer Order'` | Prefix, optional length, last-used number | Feature-specific |
| Stock Transfer | Exact `DOCUMENT_TYPE = 'Stock Transfer'`; types `ORNAMENT`/`OLD_METAL`; status `POSTED` | Active row, positive length, nonnegative last-used number | Feature-specific |
| OM Transfer | Exact `DOCUMENT_TYPE = 'OM Transfer'` | Active row, positive length, nonnegative last-used number | Required only for `OLD_METAL` stock transfer |
| OM Purchase | Exact `DOCUMENT_TYPE = 'OM Purchase'` | Prefix, length (fallback 4), last-used number | Required when invoice contains old metal |
| Legacy old-metal variants | `OG Purchase`, `OS Purchase`, `DIA Purchase`, `OG Advance`, `OS Advance`, `DIA Advance`; transfer variants require investigation | Lookup by generated transaction type in legacy paths | Conditional/legacy |
| Receipt/payment voucher | `TRANS_TYPE = 'RECEIPT'`; voucher type comes from request (examples: Cash/UPI/Bank); legacy list filters `Advance Receipt` | A matching `VOUCHER_TYPES.DOCUMENT_TYPE` for every allowed payment mode; daily `SEQ_NBR` per voucher type | Conditional |
| GST documents | Enum includes `SalesInvoice`, `Estimate`, `BranchTransfer`, `CreditNote`, `DebitNote`; staging currently proves sales invoices | GST staging tables | Conditional on GST/GSTR-1 |
| Repair | EF models/controllers exist, but no corresponding checked-in table definition was verified | Requires investigation | Feature-specific |
| Stock adjustment | Hardcoded transaction/document/voucher values `Adjustment` / `SAN`; no configured document sequence | None; literal `SAN` is reused | Feature-specific and operationally risky |
| Workshop issue/receipt | Only movement-purpose labels `WORKSHOP_ISSUE` and `WORKSHOP_RECEIPT` were found | No document workflow or numbering verified | Not deployment-ready / requires investigation |

Evidence: `DataAccess/Controllers/InvoiceController.cs:493`, `DataAccess/Workflows/EstimateWorkflow.cs:38`, `DataAccess/Controllers/GrnController.cs:54-77`, `DataAccess/Workflows/CustomerOrderWorkflow.cs:11`, `DataAccess/Workflows/StockTransferWorkflow.cs:47-60`, `DataAccess/Workflows/OldMetalTransferPostingService.cs:9-13`, `DataAccess/Workflows/InvoiceWorkFlow.cs:2222-2297`, `InvEntry.Models/Extensions/OldMetalTransactionExtension.cs:76-100`, `InvEntry.Gst.Core/Models/GstEnums.cs:34-41`.

## Required master-data inventory

| Master | Proven requirement | Classification |
|---|---|---|
| Voucher types | One exact row for every enabled document/payment type; active/length validation is enforced by centralized numbering, but older paths dereference rows without equivalent checks | Mandatory per enabled workflow |
| This company | Exactly one usable `ORG_COMPANY` row with `THIS_COMPANY = 1`, nonblank `NAME`, and a valid `ADDRESS_GKEY` | Mandatory |
| Company address | Address joined by the company view; `GST_STATE_CODE` is used as seller/default buyer state | Mandatory; GST code mandatory for GST |
| GST registration | `ORG_COMPANY.GST_NBR`, valid seller state code, product HSN/UOM/taxability and invoice tax fields | Mandatory for GST invoices/GSTR-1 |
| Stock-transfer destinations | Active `MTBL_REFERENCES` rows with `REF_NAME = 'STOCK_TRANSFER'`, nonblank code/value | Conditional |
| Destination customer | For old-metal transfer, destination `REF_VALUE` must match `ORG_CUSTOMER.MOBILE_NBR` | Conditional |
| Daily-rate definitions | Active `MTBL_REFERENCES` rows with `REF_NAME = 'DAILY_RATE'` | Conditional on daily-rate UI |
| Products | Active products used by stock workflows need ID, metal, purity, UOM; GST staging additionally requires HSN and currently only maps `UOM = 'Grams'` to `GMS` | Mandatory for inventory; GST fields conditional |
| Product categories/groups/metals | Values are surfaced as masters; no universal required row names were proven | Requires investigation per enabled catalog |
| Customers | Required by invoices/orders; old-metal transfer specifically requires mobile-number resolution | Mandatory per transaction |
| Company bank accounts | `ORG_BANK_DETAILS` is consumed for bank settlements; IFSC is schema-NOT-NULL | Conditional on noncash settlement |
| Ledger masters | Ledger tables are consumed by accounting workflows, but required ledger names could not be proven safely | Requires investigation |
| Supplier references | Material Receipt UI loads `MTBL_REFERENCES.REF_NAME = 'SUPPLIERS'` and stores `REF_VALUE` as supplier ID | Conditional on Material Receipt |
| Stock-adjustment reasons | UI loads active values using reference name `STOCK_ADJUSTMENTS`; selected value is written to transaction notes | Conditional on Stock Adjustment |

Company-view evidence: `InvEntry.DB/dbo/Views/ORG_THIS_COMPANY_VIEW.sql:2-25`. Product columns: `InvEntry.DB/dbo/Tables/PRODUCT.sql:1-26`. GST UQC rule: `DataAccess/Services/Gstr1StagingEnrichmentService.cs:230-269`.

## Technical checklist

- Install .NET 9 runtime for framework-dependent API publishing and .NET 9 Windows Desktop Runtime for WPF (`InvEntry/InvEntry.csproj:3-7`; publish profiles are not self-contained).
- Install Visual Studio build tools with SSDT to build the `.sqlproj`; the no-restore solution build failed on the missing SSDT targets.
- Configure SQL Server database `mijms` or override `ConnectionStrings:DefaultConnection`; current source defaults to local `SQLEXPRESS` with Windows authentication (`DataAccess/appsettings.json:7-9`). The generated context also contains the same fallback (`DataAccess/Models/MijmsContext.cs:124-126`).
- Make the WPF API endpoint deployable. It is compile-time hardcoded to `https://localhost:7001/` in Debug and `http://localhost:8500/` in Release (`InvEntry/Bootstrapper.cs:224-230`); `appsettings.json` does not control it.
- Supply a strong external `Jwt:Key`, rotate the committed key, and require HTTPS. A missing key reaches `Encoding.UTF8.GetBytes(jwtKey)` (`DataAccess/Program.cs:15-40`).
- Ensure write access to `C:\Madrone\Logs` and `C:\Madrone\InvEntry-.log`; invoice PDF output uses `C:\Madrone\Invoice\`, while estimate output uses `D:\Madrone\Invoice\` (`InvEntry/ViewModels/InvoiceViewModel.cs:2221`; `InvEntry/ViewModels/EstimateViewModel.cs:1017`).
- For barcode printing, install a Windows printer queue named exactly `Bar Code Printer TT065-50` and a RAW-ZPL-compatible driver/device (`InvEntry.Utils/BarCodePrint.cs:20-66`; `RawPrinterHelper.cs:9-28`). If labels are not enabled, set `LabelPrinting:Mode` exactly to `Simulation`; every other/missing value selects the physical printer (`InvEntry/Services/Printing/LabelPrinterFactory.cs:7-19`).
- For OCR/import, deploy `tessdata/eng.traineddata`, Ghostscript native prerequisites, and `Config/*-fields.json`/`*-mapping.json` relative to the working directory (`InvEntry/InvEntry.csproj:81-86`; `InvEntry.Utils/PdfOcrReader.cs:13-27`; `DocumentParser.cs:53-57`).
- Tally integration is conditional and defaults to `http://localhost:9000/` (`InvEntry/appsettings.json:11-13`). Verify Tally is listening and company name matches.
- Session automation is conditional and expects Node/npx at `C:\Program Files\nodejs\npx.cmd` plus writable `browser-data` (`DataAccess/Controllers/SessionController.cs:19-54`).
- DevExpress 24.2.14 restore requires the configured private feed; rotate the credential embedded in `Nuget.config` (`Directory.Build.props:3-5`; `Nuget.config:18-21`). Crystal Reports reference path is machine-specific (`InvEntry.Reports/InvEntry.Reports.csproj:7`).

## Numbering and concurrency risks

`VoucherNumberService` requires an active row, positive `DOC_NBR_LENGTH`, and nonnegative `LAST_USED_NUMBER` (`DataAccess/Services/VoucherNumberService.cs:68-135`). Stock transfer wraps generation in a serializable transaction (`StockTransferWorkflow.cs:144-154`), but estimate, GRN, legacy voucher, customer-order, invoice, and old-metal paths directly read/increment the same row. The schema defines only a primary key on `GKEY`; it does not enforce uniqueness of `DOCUMENT_TYPE` (`InvEntry.DB/dbo/Tables/VOUCHER_TYPES.sql:1-16`). Before go-live:

1. Ensure exactly one row per case-sensitive application literal.
2. Reconcile `LAST_USED_NUMBER` with existing maximum document numbers.
3. Load-test concurrent creation; several legacy paths do not visibly use serializable locking.
4. Note inconsistent widths: GRN and legacy vouchers force four digits; other workflows use `DOC_NBR_LENGTH` or fall back differently.

No financial-year-specific sequence column or reset logic was found in `VOUCHER_TYPES`; financial-year numbering is therefore **requires investigation**. GSTR-1 derives financial year from report dates rather than a company setting.

## Material Receipt, stock adjustment, and workshop readiness

Material Receipt is implemented through `GRNViewModel`, not as one server-side transaction. It creates the GRN header, then line summaries, then updates/creates product summaries, creates temporary tagged stock, and launches product transaction updates (`InvEntry/ViewModels/GRNViewModel.cs:210-231`, `:298-349`, `:487-578`). This creates several prerequisites and risks:

- `MTBL_REFERENCES` must contain the intended `SUPPLIERS` values (`GRNViewModel.cs:122-133`). No supplier-table relationship is enforced by this path.
- New receipts start with status `Open`; the supplier query also depends on exact `Open` casing (`GRNViewModel.cs:109-112`; `DataAccess/Controllers/GrnController.cs:51-55`).
- Movement literals are `GRN` and `Stock Receipt` (`GRNViewModel.cs:382-386`). These are stored transaction values, not rows proven to be validated against a master.
- Product stock summaries are located by category and then changed client-side. Product/category consistency and uniqueness must be validated at the site.
- Product transaction creation is `async void` and invoked without awaiting it (`GRNViewModel.cs:355`, `:575`), so the UI can report success while history updates are still running or have failed.
- The centralized `StockMovementService` rejects `Direction.In` with `NotSupportedException` until GRN/material receipt integration exists (`DataAccess/Inventory/StockMovementService.cs:183-207`). Consequently the enum purpose `MaterialReceipt` does not prove integrated stock-in posting.

Stock Adjustment loads reasons from exact reference name `STOCK_ADJUSTMENTS` (`InvEntry/ViewModels/StockAdjustmentViewModel.cs:217`), directly updates tagged stock, and writes a product transaction with hardcoded `DocumentNbr`, `DocumentType`, and `VoucherType` all equal to `SAN` (`:342-428`). No voucher-number row or unique adjustment document number is used, and the comments acknowledge summary handling needs study (`:351-353`). Treat the feature as requiring business reconciliation and concurrency testing before enablement.

Workshop support is incomplete. `StockMovementPurpose` declares `WorkshopIssue` and `WorkshopReceipt` (`DataAccess/Inventory/StockMovementPurpose.cs:18-19`) and maps them to stored transaction strings (`StockMovementService.cs:1183-1187`), but no caller/workflow was found. `RepairHeader`, `RepairDetail`, `GoldLedger`, and `Charge` model classes exist but are absent from `MijmsContext` DbSets/mappings and no matching SQL-project DDL was found. They are orphan models, not evidence of a deployable repair/workshop module. Workshop must remain disabled until its authoritative schema, masters, numbering, statuses, and posting workflow are specified and implemented.

## Voucher and payment-processing vocabulary

Invoice settlement does not validate ordinary payment modes against a database master. Any nonblank mode other than the special values is treated as real money received. The exact special literals are `Advance Adj`, `RD Adj`, and `Credit` (`DataAccess/Workflows/InvoiceWorkFlow.cs:2396-2428`). Voucher transaction types become `Receipt`, `Payment`, or `Journal`; refund voucher type becomes `Refund` (`:438-500`). Receivable statuses/types include `Outstanding`, `Credit`, and `Refund` (`:690-809`). The contract comments suggest `CASH`, `UPI`, `CARD`, `NEFT`, `IMPS`, `RTGS`, `CHEQUE`, and `DD`, but comments are not enforcement (`InvEntry.Contracts/Invoices/InvoiceSettlementSaveModel.cs:6-40`).

Therefore the approved payment-mode list, instrument/reference requirements, bank-account requirements, and casing must be agreed per site. `Advance Adj` and `RD Adj` are journal adjustments rather than cash receipts. Receipt references are derived from the invoice number: an invoice beginning with `B` becomes `R` plus the remainder; all others become `R-<invoice>` (`InvoiceWorkFlow.cs:624-639`). This derived value is not a separately configured sequence.

## Missing seed-data analysis

No `INSERT` or `MERGE` seed script for business configuration was found in either database project. Existing scripts are DDL, views, procedures, and security principals. Consequently, neither project provisions voucher types, company/address, stock-transfer/daily-rate references, products, customers, bank/ledger masters, or GST staging baseline data.

Do not use `InvEntry.Database/Security/mijms_1.sql` as deployment seed: it contains a committed SQL login password and `MUST_CHANGE`. Security principals must be environment-managed.

## Schema discrepancies

| Discrepancy | Impact |
|---|---|
| `InvEntry.DB` lacks `STOCK_TRANSFER_HEADER/LINE`; `InvEntry.Database` contains them | Choosing the newer-looking `InvEntry.DB` alone breaks stock transfer |
| `InvEntry.Database` lacks `CUSTOMER_ORDER`, `CUSTOMER_ORDER_LINES`, and `CUSTOMER_ORDER_DB_VIEW`; `InvEntry.DB` contains them | Choosing `InvEntry.Database` alone breaks customer orders |
| EF expects GST staging tables absent from both projects | Invoice finalisation/GSTR-1 staging can fail |
| EF expects multiple temp/report tables/views absent from one or both projects | Runtime failures in reporting/inventory paths |
| `MTBL_REFERENCES` declares a self-FK from `GKEY` to itself | Every row trivially references itself; likely unintended and provides no parent relationship (`MTBL_REFERENCES.sql:10-11`) |
| Absolute developer paths are embedded in `DataAccess.csproj` | Nonportable project evaluation |
| Reports contain hardcoded GST rate captions (1.5%/3%) and at least one design-time GSTIN | Printed text can disagree with data/rates; verify all layouts before production |
| Repair/workshop model classes are not registered in `MijmsContext`, and matching DDL is absent | Workshop/repair cannot be treated as a deployable database feature |
| `SupplierMetalTransaction` controller depends on an unmapped model and defines three conflicting single-segment GET routes | Supplier-metal/workshop calls can fail through DI/model mapping or ambiguous routing |
| Central stock-in throws `NotSupportedException`, while GRN uses legacy client orchestration | Material receipts do not share the atomic/idempotent stock movement path |

## Recommended deployment verification procedure

1. Designate one SQL project/schema baseline and reconcile every EF object before deployment.
2. Rotate committed JWT, NuGet, and SQL-login credentials; inject environment secrets.
3. Build Release on a clean Windows agent with .NET 9, Windows Desktop Runtime tooling, DevExpress access, and SSDT.
4. Publish API and WPF; verify all content assets exist in publish output.
5. Restore/deploy a clean candidate database, then run `validate-prerequisites.sql` read-only.
6. Resolve every `FAIL` and explicitly accept each `INVESTIGATE`; archive output.
7. Smoke-test API health/authentication from the WPF host using the actual production URL.
8. Test one draft/final/cancel invoice, estimate, GRN, order, ornament transfer, and old-metal transfer as enabled; verify sequence uniqueness and rollback behavior.
9. Test intra/interstate GST, B2B/B2C, HSN/UQC, documents-issued, and export files for the intended return period.
10. Test every report against production-like data; inspect company/GST/address, rates, totals, paper, and printer selection.
11. Run barcode printing first in Simulation, then submit a physical ZPL label and verify scanability. “Submitted” only proves Windows accepted the job (`BarCodePrint.cs:15-17`).
12. Verify log/PDF directory permissions, disk retention, backup/restore, TLS, firewall, service identity, startup/restart, and monitoring.
