-- Read saved patient and stock details together (no schema change required).
SELECT p.SalesOrder, p.ProcedureNumber, p.PatientNumber, p.PatientName,
       p.SurgeonName, p.ProcedureDate, p.CgSetNumber,
       j.LineNumber, d.[Set] AS SetsCode, d.MstockCode, d.MstockDes,
       d.Mwarehouse, d.MorderQty, d.MshipQty, d.RepUsageQty,
       d.RetQty, d.Usage, d.Variance
FROM dbo.PatientProcedures p
CROSS APPLY OPENJSON(p.LineNumbersJson) WITH (LineNumber int '$') j
LEFT JOIN dbo.DeliveryOrderDetails d
  ON d.SalesOrder = p.SalesOrder AND d.Line = j.LineNumber
ORDER BY p.SalesOrder, p.ProcedureNumber, j.LineNumber;
