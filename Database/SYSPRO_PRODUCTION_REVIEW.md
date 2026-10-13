> Update 2026-09-30: SORTSU positive success-XPath validation was removed at the user's request. Explicit response errors still stop processing, and final entity readback is required before overall success. SORTSU.success.xpath is no longer required; earlier requirements below are superseded.

> Update 2026-09-30: SORQRY readback is now performed through read-only SysproContext entities (SorMaster, SorDetail and vw_FetchSORDetails). SORQRY mappings are no longer required. Query-related requirements below are historical. See ../SysproContracts/README.md for current deployment prerequisites.

> Update 2026-09-30: custom-form posting and its required configuration were removed at the user's request. Custom-field requirements below are historical and superseded. See ../SysproContracts/README.md for the current mapping package and remaining response-mapping requirements.

# SYSPRO production readiness review ? 2026-09-25

**Decision: not ready for production posting.** This was a repository review with local checks, not a live SYSPRO/company certification. No orders were closed or created and no database migration or email was sent.

## Changes made during this review

- Require authentication on RepController, administrator access on the legacy SYSPRO login endpoint and raw SYSPRO log endpoint.
- Retain a received SOAP response when channel Close fails, so successful remote work is not misclassified solely because cleanup failed.
- Restrict SORTOS input to exactly one Item for the original order and status 9.
- Reject validation-only SORTOI/SORTOS/SORTSU settings; require whole-document validation for SORTOS/SORTSU.
- Reject duplicate replacement order numbers within an attempt.
- Reject ItemsRejectedWithWarnings in the shared error parser.
- Query each replacement after component/custom-field posting and verify quantities and warehouses as well as line numbers and kit identities.
- Add disabled example deployment settings. Update the SORQRY result contract to require Warehouse and Quantity.

## Object-by-object assessment

| Object | Current implementation | Release dependency |
| --- | --- | --- |
| SORRSL | Builds each kit; matches stock and actual component sequence; rejects ambiguous or changed component membership | Installed-version samples, optional/repeated components, units and current BOM behavior |
| SORTOS | Completes exactly the original order with status 9; persisted intent and response | Confirm eligible original statuses, outstanding shipment/allocation implications, installed XSD and positive acknowledgement |
| SORTOI | Creates one normal order per procedure using automatic numbering; BOM expands components; parses created order number | Prices, taxes, units, dates, zero usage, numbering, allocations and quotation requirements validated in UAT |
| SORQRY | Required company request/result transforms; verifies returned order and kit identities; final quantity/warehouse readback | Real parent and sequence fields plus Quantity in matching order units and Warehouse; do not invent relationships from adjacency |
| SORTSU | Updates verified existing component lines to total checked usage | Validate zero/reduced quantities and shipped/reserved stock restrictions in the installed company |
| Custom forms | Required configured business object, request/parameters and positive acknowledgement | LiquidERP field names/types and installed business object; patient data readback still needs UAT |

## Blocking release work

1. **Contracts and deployment configuration:** production company mappings are not checked into the repository. Test-generated transforms are synthetic fixtures and must never be deployed. Configure secrets outside source control and use reviewed positive-success XPath expressions. Confirm effective deployment settings independently of local appsettings.
2. **Database:** install and verify 001/002 against an application UAT database, including existing SORHEADER/SORDETAIL compatibility and unique attempt constraint. Current checks inspect the EF model; they do not exercise real SQL transactions.
3. **Distributed failure recovery:** completing the old order and creating replacements are separate remote transactions. A failed later call may leave the old order complete and only some replacements present. Attempts block automatic replay and preserve step XML, but there is no approved admin resume/reconcile operation. Define and rehearse a manual recovery runbook before release; never delete attempts to force a retry.
4. **Crash handling:** a process crash or database outage can leave an attempt in Posting with a pending step and an unknown remote outcome. Such attempts are visible to admins but are not automatically converted into failure notification records. Establish monitoring and recovery for these cases.
5. **Source freshness:** staging retains the source snapshot but does not revalidate every current source field/status immediately before closure. Concurrent external changes must be covered by source-order ownership rules or a version-checked pre-close validation.
6. **Business semantics:** built-in SORTOI uses ORD, not a quotation contract. Bin/lot/serial allocations, optional BOM components, kit pricing and zero-use behavior require company decisions and real order validation. Custom-field acknowledgement alone is not a field-by-field readback.
7. **Security/operations:** the legacy admin login helper still uses a fixed HTTP endpoint and returns a session token. The production flow uses configured endpoints. Retire the legacy diagnostic route or replace it with a configured, non-token-returning health operation before deployment. Use HTTPS for credentials or document the approved protected network path. Restrict the SYSPRO SQL identity to read-only access.
8. **Build/dependency hygiene:** the legacy net5 SysproWcfClient project shares its project directory/obj path with the net8 API. Build the explicit API project; isolate or retire the old project before building both in deployment. Review dependency advisories and pin floating WCF versions as part of release dependency management.

## Verification

- 26 backend checks passed, including XML validation, quantities, BOM identity, duplicate-attempt EF constraint, parameter safeguards and authorization attributes. This run compiles the API.
- Backend build initially failed because the system drive was full; it succeeded after redirecting TEMP/TMP to .artifacts/tool-temp.
- 15 frontend tests passed across 4 suites: clerk workflow, posting popup, procedure navigation and unsuccessful-order review.
- git diff --check passed (line-ending normalization warnings only).
- No live SOAP, SQL migration, SMTP, HTTP authorization integration, concurrency or crash-injection tests were performed.

## UAT acceptance cases

Run single and multiple procedures, two kits, duplicate component stock codes with distinct sequences, optional/changed BOM, zero and fractional usage, and a standalone stock line. Verify old status, every new order number, actual line relationships, units, quantities, prices/taxes and patient fields.

Reject unchecked/variance lines and unauthorized callers. Submit the same order concurrently and after success. Inject failures before and after every remote commit, including a timeout where SYSPRO committed, loss of database persistence, process restart, and SMTP failure/recovery. Confirm that no automatic replay creates duplicates and that admins can reconcile all remote work.
