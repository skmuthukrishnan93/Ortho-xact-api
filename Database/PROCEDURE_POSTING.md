> Update 2026-09-30: SORTSU positive success-XPath validation was removed at the user's request. Explicit response errors still stop processing, and final entity readback is required before overall success. SORTSU.success.xpath is no longer required; earlier requirements below are superseded.

> Update 2026-09-30: SORQRY readback is now performed through read-only SysproContext entities (SorMaster, SorDetail and vw_FetchSORDetails). SORQRY mappings are no longer required. Query-related requirements below are historical. See ../SysproContracts/README.md for current deployment prerequisites.

> Update 2026-09-30: custom-form posting and its required configuration were removed at the user's request. Custom-field requirements below are historical and superseded. See ../SysproContracts/README.md for the current mapping package and remaining response-mapping requirements.

# RepClerk and replacement-order integration

Status: application workflow implemented; **live posting is disabled until company XML contracts and quotation settings are supplied**. No migration or live SYSPRO call was run by the implementation.

## Business objects and quotation configuration

The user clarified SORSTU to **SORTSU**. SORTSU maintains existing kit component lines; it cannot create a sales order or quotation. The flow uses SORTOS to complete the source, SORTOI to create replacements, and SORTSU to maintain their components. Quotation order-type settings remain to be supplied by LiquidERP; this implementation does not claim that replacements are quotations.

SORTOS uses status 9 (Complete) for the original order. LiquidERP must confirm that the customer's installed version permits the warehouse-to-complete transition. Public documentation describes status 9 but its displayed schema enumeration omits it. Never fall back to direct database updates.

References:
- https://help.syspro.com/business-object-library/G_Objects/sor/sortos/sortos.htm
- https://help.syspro.com/business-object-library/G_Objects/sor/sortoi/sortoi.htm
- https://help.syspro.com/business-object-library/G_Objects/sor/sorrsl/sorrsl.htm
- https://help.syspro.com/business-object-library/G_Objects/sor/sortsu/sortsu.htm

## Application data and validation

Apply 001_PatientProcedures.sql (if not installed), then 002_ProcedurePosting.sql to the APPLICATION database. Reconcile existing SORHEADER/SORDETAIL schemas before deployment; these names were not mapped in the repository. The script rejects detected incompatible tables rather than replacing them.

Rep final save captures SORHEADER per procedure and SORDETAIL per source line in the same app transaction. Original header/line fields are retained as JSON, together with explicit procedure and kit parent associations. Ambiguous or split kit relationships fail capture. Older captures are staged on first warehouse load and must be checked again. TEMP data here means these persistent app staging tables, not SQL session-local temporary tables.

Warehouse views read these tables. Every non-parent line needs an explicit saved return check. Usage and variance are computed on the server. The server validates all procedures from persisted records; submitted flags, quantities and subsets cannot bypass posting checks.

## Posting sequence

1. Load and compile all configured XML mappings before any remote changes.
2. Persist one posting attempt per original order under a unique database constraint. Persist a source snapshot.
3. SORRSL Build obtains kit component identities; preserve returned sequence strings, including leading zeros. Reject missing, ambiguous or unchecked BOM components.
4. Prepare one replacement plan per procedure, in procedure order. Sort parent stock codes, then components numerically by BOM sequence. Each replacement starts at line 1.
5. SORTOS Post completes the original order. A failed acknowledgement stops generation.
6. SORTOI Post currently adds a new order per procedure. OrderActionType must be A; the original number cannot be reused.
7. SORQRY reads the created order. A supplied mapping must expose actual component/parent identities; comparing only adjacent line positions is not sufficient. Reject mismatched counts, identities or line numbers.
8. SORTSU maintains verified existing component lines. The parent relationship already exists in SYSPRO; do not add components as unrelated stock lines.
9. Post custom forms using LiquidERP's specified object, taking values from the staged procedure header.
10. Read the replacement again with SORQRY after component and custom-form posting. Require the planned quantities and warehouses as well as identities.
11. Mark PostedToSyspro and show ?your order is now successfully posted to Syspro? only after every procedure and custom-form operation succeeds.

A source order may be complete while a replacement fails. This is not an atomic transaction across SYSPRO calls. Successful steps must be reconciled individually before any recovery.

## Company XML contract interface

Configure through environment variables or the deployment configuration (credentials must not be committed):

- SysproPosting:Enabled (default false)
- SysproPosting:ServiceUrl (base SYSPRO8WebServices URL)
- SysproPosting:Operator, Password, Company, CompanyPassword
- SysproPosting:ContractsDirectory (deployment-owned directory)
- SysproPosting:CustomFormBusinessObject
- SysproPosting:EmailEnableSsl (default true)

SORTOI now has built-in request, parameter, response and success mappings in Services/SortoiMapping.cs. No external SORTOI files are required. It adds normal sales orders (ORD) using automatic numbering. It maps saved customer/PO, dates, currency, shipping address/instructions, discounts, salesperson/branch/area and tax exemptions; detail mapping includes stock, warehouse, checked quantities, original units, prices, discounts and tax codes. Only kit parents and standalone stocked lines are imported; components are generated by the BOM and maintained through SORTSU. Missing units, invalid quantities or unsupported non-merchandise lines are rejected before closing the original order. The returned order number must be accompanied by a successful add acknowledgement and one processed item.

Required request files remain SORQRY and CustomFields (SORTOS, SORTOI and SORTSU have built-in request/parameter builders, optionally overridden by the same files):
- <step>.request.xslt: transform the app Posting XML into the installed business object's input document.
- <step>.parameters.xml: validated parameters; not required for SORQRY.
- <step>.success.xpath: boolean XPath on the raw response proving positive success, not merely absence of errors.

A <step>.success.xpath remains required for SORTOS, SORTSU, SORQRY and CustomFields. SORTOI has a built-in success check. Response mappings:
- SORTOI.result.xslt is optional; the built-in parser reads SalesOrders/Order/SalesOrder after validating its acknowledgement.
- SORQRY.result.xslt: raw response -> <Result><SalesOrder>new number</SalesOrder><Lines><Line><LineNumber>1</LineNumber><StockCode>KIT</StockCode><ParentLine/><ComponentSequence/><Warehouse>WH</Warehouse><Quantity>1</Quantity></Line>...</Lines></Result>.

The built-in SORTSU document uses existing SalesOrder and SalesOrderLine, plus Customer, StockCode, checked OrderQty and QuantityType T. It does not send unsupported ParentLine, ShipQty or ComponentSequence elements; the relationship is verified against SORQRY before posting. Customer-specific missing files are intentionally not fabricated. The SORQRY mapping must expose the final order quantity in the same order units as the plan, plus warehouse, for every line. It must use actual returned values, never constants or planned quantities. Validation-only SORTOI/SORTOS/SORTSU parameters and non-atomic SORTOS/SORTSU parameters are rejected at preflight. SORTOS is restricted to exactly one original order; replacement order numbers cannot repeat across procedures. XSLT scripting, external document access and DTDs are disabled. Check all request/response mappings against installed XSDs and UAT responses before enabling.

Header design supplied to the request transform:

<Posting>
  <OriginalSalesOrder>original order</OriginalSalesOrder>
  <SalesOrder>empty until import returns a new number</SalesOrder>
  <SourceHeader>original SorMaster properties</SourceHeader>
  <Header>
    <ProcedureNumber>1</ProcedureNumber><Customer>customer</Customer>
    <PatientNumber>patient identifier</PatientNumber><PatientName>patient</PatientName>
    <SurgeonName>surgeon</SurgeonName><ProcedureDate>yyyy-MM-dd</ProcedureDate><CgSetNumber>set</CgSetNumber>
  </Header>
  <Lines>...</Lines>
</Posting>

Detail design under Lines:

<Line>
  <SourceValues>original SorDetail properties, including prices and units</SourceValues>
  <SourceLine>original line</SourceLine><LineNumber>replacement line</LineNumber>
  <ParentLine>replacement parent line, empty for parents/standalone items</ParentLine>
  <KitFlag>P or C or original standalone flag</KitFlag>
  <ComponentSequence>unmodified SORRSL sequence</ComponentSequence>
  <StockCode>part</StockCode><Warehouse>warehouse</Warehouse><Bin>bin</Bin>
  <Description>description</Description><Quantity>parent order quantity or checked component usage</Quantity>
</Line>

This is the internal canonical design passed to generation/custom-form mappings. LiquidERP must supply custom-form field names/types, header mapping (ship-to, terms, prices, tax, dates), manual/automatic numbering policy, quotation rules, zero-usage handling, optional component policy, and allocation/serial/lot/bin requirements. Kit imports must explode the parent according to SYSPRO rules; the SORTOI mapper must not create duplicate component stock lines.

## Errors and admin review

ProcedurePostSteps records intent before each remote call and raw responses before validation. UnsuccessfulSOMaster and UnsuccessfulSODetail retain failed attempts and their XML. XML parse errors, returned errors, invalid counts, missing positive acknowledgements and network errors stop posting. An empty response is Unknown, not assumed unsuccessful remotely.

A background worker emails configured admin users using Email Settings; SMTP failures retain a pending notification and retry every minute. Email is at-least-once (a crash after SMTP accepts may cause a duplicate). XML/patient details stay in the authenticated admin screen, not the email body.

Admin > Unsuccessful orders shows incomplete attempts, saved errors, each successful/failed/pending step and raw XML. Restart/crash attempts remain visible as Posting; there is no automatic remote replay. The original specification ends at ?They can?, so retry/edit/resolution actions await the rest of the admin recovery requirements. Do not delete attempt rows to force a repost.

## Verification

API: dotnet build Ortho-xact-api/Ortho-xact-api.csproj --no-restore -p:UseAppHost=false -o .artifacts/api-check

Backend checks: dotnet run --project tests/ProcedurePosting.Checks -p:UseAppHost=false

Frontend: node node_modules/react-scripts/bin/react-scripts.js test --watchAll=false --runInBand --runTestsByPath src/RepClerk.test.js src/clerkProcedureWorkflow.test.js src/clerkPosting.test.js src/UnsuccessfulOrders.test.js

Before release: apply schema in UAT, test representative single/multi-procedure kits with real XML, confirm original closure and quotation/replacement type, inject failures after each remote commit, verify duplicate requests cannot replay, test SMTP failure/recovery and unauthorized admin access.
