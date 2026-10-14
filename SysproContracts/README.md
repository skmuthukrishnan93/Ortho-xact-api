# SYSPRO posting mappings and entity readback

## 14 October 2026 - Patient details through COMTFM

This supersedes the direct SQL patient update. After SORTOI creates a replacement and its first entity verification passes, COMTFM posts the client's PostCustomForm document through the existing SYSPRO session. FormType is ORD; KeyFields contain the returned replacement SalesOrder (normalized to the same company order format as entity readback) and a blank InvoiceNumber. Fields map PatientNumber, PatientName, SurgeonName to DoctorsName, ProcedureDate and numeric CgSetNumber from the current procedure. OriginalInvoice is omitted; no procedure number is posted. No patient SQL writer or CusSorMaster+ UPDATE permission check remains.

Only PatientNumber, PatientName, DoctorsName, ProcedureDate and CgSetNumber are sent under Fields. RateOfExchange, CustomerHospitalAc and OriginalInvoice are omitted at the client's request, including when source values or old custom-field configuration entries exist. The InvoiceNumber key remains blank. Patient text must fit 200 characters, dates must fit datetime, and CgSetNumber must be a whole numeric value fitting decimal(12,0). All fields are rendered and validated before releasing the original order.

Deploy COMTFM.parameters.xml to the effective ContractsDirectory. Defaults use Function U, ValidateOnly N and ApplyIfEntireDocumentValid N, matching the supplied COMTFM parameters. Parameter overrides cannot switch to add, delete or validation-only processing. Request overrides must match the verified replacement key and all mapped procedure fields. The SYSPRO operator must be permitted to use COMTFM and maintain the ORD custom form. Existing entity readback remains read-only; it requires the normal SorMaster, SorDetail and vw_FetchSORDetails access.

COMTFM requests, parameters and raw responses are recorded as a COMTFM step per procedure in the existing posting audit. The default response contract requires a PostCustomForm document, RecordsRead 1, RecordsInvalid 0 and RecordsWritten 1 for exactly one item, with no explicit errors, rejected counts, failed status or validation-only markers. The returned key must match ORD, the replacement order and the blank invoice; keyvalue is also checked when present. Lowercase item/key/formtype/keyfields are supported. An optional COMTFM.success.xpath can define the installed-version positive acknowledgement; explicit errors and wrong returned keys still fail. The supplied successful SYSPRO 8.0.008 response is retained in examples/COMTFM.success.xml and used by regression checks. Live posting from this workspace remains untested. A missing or uncertain acknowledgement stops posting for review with no automatic replay. Earlier committed replacements and custom-form steps may remain if a later operation fails.

Updated 1 October 2026.

SORTOS now releases the original order for invoicing (status 8), as requested, before creating replacements. It does not mark the original complete. Deploy the updated SORTOS.success.xpath to the effective ContractsDirectory (for example C:\OrthoX\SysproContracts). Any custom SORTOS.request.xslt must also request status 8; the backend rejects other target statuses.

SORQRY web-service calls have been replaced by SysproEntityOrderReader using the existing SysproContext and ConnectionStrings:SysproConnection. SORQRY.request.xslt, SORQRY.result.xslt and SORQRY.success.xpath are no longer loaded or required. Older files in this directory are retained only as historical reference.

## Entity readback

- SorMaster supplies the returned order identity and customer.
- SorDetail supplies actual line numbers, stock codes, warehouses, order quantities, kit flags and component sequences.
- vw_FetchSORDetails supplies the component SetsCode used to match an actual parent stock code. This is the same source set identity used by procedure staging.
- Read queries use AsNoTracking and a short serializable read transaction. They perform no database writes.
- Both pre-SORTSU and final readback remain audited as SYSPRO_ENTITY_READ. The saved XML is an application representation of database results, not a SOAP response.
- Before component changes, verify line counts/numbers, stock codes, customer and parent/component identities. After changes, also verify actual order quantities and warehouses.
- Missing/duplicate view references, multiple possible parents, missing sequences, wrong customers and mismatches stop processing. Parent relationships are never inferred from adjacency.

The SQL connection must point to the same company database used by the business-object login and have read access to SorMaster, SorDetail and vw_FetchSORDetails. The view must expose replacement orders in their newly created status (normally 1), not only warehouse-status orders. Its definition is not included in this repository and has not been verified on the client server. No status-4 filter is added by the reader.

## Required mapping files

- SORTOS.success.xpath (included)

SORTSU.success.xpath is no longer required or evaluated, as requested. SORTSU responses must still be valid XML with the expected document root and no explicit errors, invalid-item counts, nonzero error codes or failed validation statuses. The audit step is recorded as ResponseChecked; it is not a positive acknowledgement. Final entity readback must pass before the overall posting attempt is marked successful.

Optional overrides remain available for SORTOS, SORTOI and SORTSU requests/parameters; standard mappings are built into the backend. Included SORTOS.parameters.xml and SORTSU.parameters.xml use actual posting, reject warnings and validate the entire document.

Custom-form posting remains excluded at the user's request. No patient custom-form mappings or CustomFormBusinessObject setting are required.

## Deployment

Deploy the latest backend, then configure the existing SysproConnection and business-object credentials for the same company. ContractsDirectory remains C:\OrthoX\SysproContracts in development settings; production requires its own effective setting. The API identity needs read access to the active mapping files.

Both the SORQRY mapping dependency and SORTSU success-XPath dependency are removed. Final database verification and partial-failure safeguards remain active. UAT against the installed database/view and SYSPRO business objects remains required.

## SORTOI replacement import parameters

Deploy SORTOI.parameters.xml to the effective ContractsDirectory; an existing file overrides backend defaults. Earlier requested ship dates are accepted, minimum shipping lead days are zero, and AllowDuplicateOrderNumbers A permits reuse of the original customer PO even when duplicate checking is configured. CheckForCustomerPoNumbers N allows a missing PO; it is not the duplicate-check switch. New sales order numbers remain automatically allocated.

AlwaysUsePriceEntered Y and AllowZeroPrice Y preserve supplied prices including explicit zero prices. They do not supply missing prices or guarantee prices for BOM-generated components. Inspect the failed StockCode and actual request Price if pricing errors persist. AllowChangeToZeroPrice is set to Y at the user\'s request. It permits an explicit zero price when changing an existing merchandise line; it does not provide missing prices when adding new orders. IgnoreWarnings is Y for SORTOI at the user request; explicit errors and rejected-item responses still fail validation. Credit and product-class restrictions are not changed by this file.

Reference: https://help.syspro.com/business-object-library/G_Objects/sor/sortoi/sortoi.htm?TocPath=Business+Objects%7CSales+Orders%7CTransaction%7C_____31

## Explicit component validation trial

Admin POST /Rep/validate-sortoi-components with JSON {"salesOrderNumber":"57189"} reads the staged procedures and resolves BOM sequences through SORRSL. It sends every parent and child as a StockLine with its saved price and units. The built-in trial mapping is used even if a request XSLT override exists. Configured SORTOI parameters are used with Process forcibly set to Validate. The response contains per-procedure requestXml, parametersXml and responseXml. No SORTOS, import, SORTSU, staging updates or attempt reset is performed.

This is an experiment, not a production kit mapping. Explicit stock lines do not establish parent/component identity, and automatic parent expansion may duplicate components. A successful validation does not prove correct kit relationships. Synthetic example XML is generated by the regression checks under .artifacts/sortoi-component-trial. Live validation has not been executed from the development workspace.

## Normal posting includes explicit children

The built-in SORTOI mapping now sends every planned parent and child StockLine with saved prices and units through the existing Post To Syspro button. Parent-only SORTOI.request.xslt overrides are rejected before SORTOS. Automatic BOM expansion may still duplicate components or fail pricing. Entity readback continues to reject unexpected line counts or kit relationships. Live posting has not been verified. The separate validation endpoint remains available.

## 9 October 2026 - Parent-only normal SORTOI import

This supersedes the previous all-lines import change. Normal SORTOI now sends only kit parents (KitFlag P, no ParentLine). Child and standalone stock lines are excluded. Parents retain their required quantity, warehouse, price and units; the request does not consist of stock codes alone. Overrides must contain exactly these parent lines or preflight fails. SORRSL, child staging, SORTSU and full relationship/quantity readback remain active. The separate explicit-component validation trial still includes all lines. Client SYSPRO acceptance, including zero-quantity SORTSU updates and component pricing, remains unverified. Orders containing standalone lines need a separately agreed handling policy because final readback still expects the complete checked plan.

## Zero-quantity components: SORTOX completion

After parent-only SORTOI succeeds, entity readback verifies the generated kit lines. SORTOX marks only child lines whose checked quantity is zero as complete, using MarkLineComplete Y, an empty CancelReasonCode and eSignature, and a nonzero verified replacement line number. Every request must return matching SalesOrder/SalesOrderLine, Completed Y and Cancelled N. Failures or uncertain outcomes stop posting for admin review; no automatic retry occurs.

Confirmed completed lines remain in the plan with Completed Y. Entity readback checks every retained line identity and relationship before SORTSU updates only positive-quantity children. Final readback checks all warehouses and active line quantities; completed rows may retain their original order quantity. Completion is confirmed by the SORTOX acknowledgement.

SORTOX uses built-in parameters when SORTOX.parameters.xml is absent. Overrides must use ValidateOnly N and ApplyIfEntireDocumentValid Y. Request overrides must mark lines complete and leave cancellation reasons empty. SysproPosting:CancelReasonCode is not used for completion. No database migration is required. Existing posting logs retain requests and responses.

This does not bypass SORTOI pricing failures. Live kit-component completion still requires client verification.
