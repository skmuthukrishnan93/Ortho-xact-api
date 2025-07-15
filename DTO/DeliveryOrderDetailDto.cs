namespace Ortho_xact_api.DTO
{
    public class DeliveryOrderDetailDto
    {
        public string MbomFlag { get; set; } = null!;
        public long Id { get; set; }
        public string SalesOrder { get; set; } = null!;
        public int SalesOrderLine { get; set; }
        public decimal? RetQty { get; set; }

        public decimal? Usage { get; set; }

        public decimal? Variance { get; set; }
        public string? Status { get; set; }
        public int? Sysprostatus { get; set; }
        public string? SetsCode { get; set; }
        public string? Mwarehouse { get; set; }
        public string? MstockCode { get; set; }
        public string? MstockDes { get; set; }
        public decimal? MorderQty { get; set; }
        public decimal? MshipQty { get; set; }
        public decimal? RepUsageQty { get; set; }

        public string? Customer { get; set; }
        public string? CustomerName { get; set; }
    }

}
