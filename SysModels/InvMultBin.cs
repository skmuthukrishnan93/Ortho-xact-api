namespace Ortho_xact_api.SysModels
{
    public partial class InvMultBin
    {
        public string StockCode { get; set; } = null!;

        public string Warehouse { get; set; } = null!;

        public string Bin { get; set; } = null!;

        public decimal QtyOnHand1 { get; set; }

        public decimal QtyOnHand2 { get; set; }

        public decimal QtyOnHand3 { get; set; }

        public DateTime? LastReceiptDate { get; set; }

        public DateTime? LastIssueDate { get; set; }

        public decimal SoQtyToShip { get; set; }

        public string Note { get; set; } = null!;

        public decimal QtyDispatched { get; set; }

        public string OnHold { get; set; } = null!;

        public string OnHoldReason { get; set; } = null!;

        public byte[]? TimeStamp { get; set; }
    }
}
