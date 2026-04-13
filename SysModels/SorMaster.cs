using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class SorMaster
{
    public string SalesOrder { get; set; } = null!;

    public decimal NextDetailLine { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string ActiveFlag { get; set; } = null!;

    public string CancelledFlag { get; set; } = null!;

    public string DocumentType { get; set; } = null!;

    public string Customer { get; set; } = null!;

    public string Salesperson { get; set; } = null!;

    public string CustomerPoNumber { get; set; } = null!;

    public DateTime? OrderDate { get; set; }

    public DateTime? EntrySystemDate { get; set; }

    public DateTime? ReqShipDate { get; set; }

    public DateTime? DateLastDocPrt { get; set; }

    public string ShippingInstrs { get; set; } = null!;

    public string ShippingInstrsCod { get; set; } = null!;

    public string AltShipAddrFlag { get; set; } = null!;

    public decimal InvoiceCount { get; set; }

    public string InvTermsOverride { get; set; } = null!;

    public string CreditAuthority { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public string SpecialInstrs { get; set; } = null!;

    public string EntInvoice { get; set; } = null!;

    public DateTime? EntInvoiceDate { get; set; }

    public decimal DiscPct1 { get; set; }

    public decimal DiscPct2 { get; set; }

    public decimal DiscPct3 { get; set; }

    public string OrderType { get; set; } = null!;

    public string TaxExemptFlag { get; set; } = null!;

    public string Area { get; set; } = null!;

    public string TaxExemptNumber { get; set; } = null!;

    public string TaxExemptOverride { get; set; } = null!;

    public string CashCredit { get; set; } = null!;

    public string Warehouse { get; set; } = null!;

    public string LastInvoice { get; set; } = null!;

    public string ScheduledOrdFlag { get; set; } = null!;

    public string GstExemptFlag { get; set; } = null!;

    public string GstExemptNum { get; set; } = null!;

    public string GstExemptOride { get; set; } = null!;

    public string IbtFlag { get; set; } = null!;

    public string OrdAcknwPrinted { get; set; } = null!;

    public string DetCustMvmtReqd { get; set; } = null!;

    public string DocumentFormat { get; set; } = null!;

    public string FixExchangeRate { get; set; } = null!;

    public decimal ExchangeRate { get; set; }

    public string MulDiv { get; set; } = null!;

    public string Currency { get; set; } = null!;

    public string GstDeduction { get; set; } = null!;

    public string OrderStatusFail { get; set; } = null!;

    public string ConsolidatedOrder { get; set; } = null!;

    public DateTime? CreditedInvDate { get; set; }

    public string Job { get; set; } = null!;

    public string SerialisedFlag { get; set; } = null!;

    public string CounterSalesFlag { get; set; } = null!;

    public string Nationality { get; set; } = null!;

    public string DeliveryTerms { get; set; } = null!;

    public decimal TransactionNature { get; set; }

    public decimal TransportMode { get; set; }

    public decimal ProcessFlag { get; set; }

    public string JobsExistFlag { get; set; } = null!;

    public string AlternateKey { get; set; } = null!;

    public string LastOperator { get; set; } = null!;

    public string HierarchyFlag { get; set; } = null!;

    public string DepositFlag { get; set; } = null!;

    public string EdiSource { get; set; } = null!;

    public string DeliveryNote { get; set; } = null!;

    public string Operator { get; set; } = null!;

    public string LineComp { get; set; } = null!;

    public decimal CaptureHh { get; set; }

    public decimal CaptureMm { get; set; }

    public DateTime? LastDelNote { get; set; }

    public decimal TimeDelPrtedHh { get; set; }

    public decimal TimeDelPrtedMm { get; set; }

    public decimal TimeInvPrtedHh { get; set; }

    public decimal TimeInvPrtedMm { get; set; }

    public DateTime? DateLastInvPrt { get; set; }

    public string Salesperson2 { get; set; } = null!;

    public string Salesperson3 { get; set; } = null!;

    public string Salesperson4 { get; set; } = null!;

    public decimal CommissionSales1 { get; set; }

    public decimal CommissionSales2 { get; set; }

    public decimal CommissionSales3 { get; set; }

    public decimal CommissionSales4 { get; set; }

    public decimal TimeTakenToAdd { get; set; }

    public decimal TimeTakenToChg { get; set; }

    public string FaxInvInBatch { get; set; } = null!;

    public string InterWhSale { get; set; } = null!;

    public string SourceWarehouse { get; set; } = null!;

    public string TargetWarehouse { get; set; } = null!;

    public string DispatchesMade { get; set; } = null!;

    public string LiveDispExist { get; set; } = null!;

    public decimal NumDispatches { get; set; }

    public string CustomerName { get; set; } = null!;

    public string ShipAddress1 { get; set; } = null!;

    public string ShipAddress2 { get; set; } = null!;

    public string ShipAddress3 { get; set; } = null!;

    public string ShipAddress3Loc { get; set; } = null!;

    public string ShipAddress4 { get; set; } = null!;

    public string ShipAddress5 { get; set; } = null!;

    public string ShipPostalCode { get; set; } = null!;

    public decimal ShipToGpsLat { get; set; }

    public decimal ShipToGpsLong { get; set; }

    public string State { get; set; } = null!;

    public string CountyZip { get; set; } = null!;

    public string ExtendedTaxCode { get; set; } = null!;

    public string MultiShipCode { get; set; } = null!;

    public string WebCreated { get; set; } = null!;

    public string Quote { get; set; } = null!;

    public decimal QuoteVersion { get; set; }

    public string GtrReference { get; set; } = null!;

    public string NonMerchFlag { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string User1 { get; set; } = null!;

    public string CompanyTaxNo { get; set; } = null!;

    public string TpmPickupFlag { get; set; } = null!;

    public string TpmEvaluatedFlag { get; set; } = null!;

    public string StandardComment { get; set; } = null!;

    public string DetailStatus { get; set; } = null!;

    public string SalesOrderSource { get; set; } = null!;

    public string SalesOrderSrcDesc { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public string ShippingLocation { get; set; } = null!;

    public string IncludeInMrp { get; set; } = null!;

    public byte[]? TimeStamp { get; set; }

    public string QuickQuote { get; set; } = null!;

    public string RegimeCode { get; set; } = null!;

    public string PortDispatch { get; set; } = null!;

    public string PriceGroup { get; set; } = null!;

    public string PriceGroupLevel { get; set; } = null!;

    public string LastOpInvPrt { get; set; } = null!;

    public string DispatchWholeSo { get; set; } = null!;

    public string EntityDimensions { get; set; } = null!;

    public virtual SalArea AreaNavigation { get; set; } = null!;

    public virtual SalSalesperson SalSalesperson { get; set; } = null!;

    public virtual ICollection<SorDetailBin> SorDetailBins { get; set; } = new List<SorDetailBin>();

    public virtual ICollection<SorDetail> SorDetails { get; set; } = new List<SorDetail>();
}
