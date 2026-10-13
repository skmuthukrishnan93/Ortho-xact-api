-- Run in the APPLICATION database used by the deployed API.
-- Read-only checks; no data or schema changes.
SELECT DB_NAME() AS ApplicationDatabase;
SELECT
 CASE WHEN COL_LENGTH('dbo.PatientProcedures', 'LineQuantitiesJson') IS NOT NULL
      THEN 'Present' ELSE 'MISSING - apply migration 003' END AS LineQuantitiesJson;
SELECT kc.name AS PrimaryKeyName, c.name AS KeyColumn, ic.key_ordinal AS KeyPosition
FROM sys.key_constraints kc
JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id
 AND ic.index_id = kc.unique_index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID('dbo.SORDETAIL')
 AND kc.type = 'PK' AND ic.key_ordinal > 0
ORDER BY ic.key_ordinal;
-- Expected key columns: SalesOrder, ProcedureNumber, Line.
