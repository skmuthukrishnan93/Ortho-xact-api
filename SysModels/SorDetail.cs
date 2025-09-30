using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class SorDetail
{
    public string SalesOrder { get; set; } = null!;

    public decimal SalesOrderLine { get; set; }

    public string LineType { get; set; } = null!;

    public string MstockCode { get; set; } = null!;

    public string MstockDes { get; set; } = null!;

    public string Mwarehouse { get; set; } = null!;

    public string Mbin { get; set; } = null!;

    public decimal MorderQty { get; set; }

    public decimal MshipQty { get; set; }

    public decimal MbackOrderQty { get; set; }

    public decimal MunitCost { get; set; }

    public string MbomFlag { get; set; } = null!;

    public string MparentKitType { get; set; } = null!;

    public decimal MqtyPer { get; set; }

    public decimal MscrapPercentage { get; set; }

    public string MprintComponent { get; set; } = null!;

    public string McomponentSeq { get; set; } = null!;

    public string MqtyChangesFlag { get; set; } = null!;

    public string MoptionalFlag { get; set; } = null!;

    public decimal Mdecimals { get; set; }

    public string MorderUom { get; set; } = null!;

    public decimal MstockQtyToShp { get; set; }

    public string MstockingUom { get; set; } = null!;

    public decimal MconvFactOrdUm { get; set; }

    public string MmulDivPrcFct { get; set; } = null!;

    public decimal Mprice { get; set; }

    public string MpriceUom { get; set; } = null!;

    public string McommissionCode { get; set; } = null!;

    public decimal MdiscPct1 { get; set; }

    public decimal MdiscPct2 { get; set; }

    public decimal MdiscPct3 { get; set; }

    public string MdiscValFlag { get; set; } = null!;

    public decimal MdiscValue { get; set; }

    public string MproductClass { get; set; } = null!;

    public string MtaxCode { get; set; } = null!;

    public DateTime? MlineShipDate { get; set; }

    public string MallocStatSched { get; set; } = null!;

    public string MfstTaxCode { get; set; } = null!;

    public decimal MstockUnitMass { get; set; }

    public decimal MstockUnitVol { get; set; }

    public string MpriceCode { get; set; } = null!;

    public decimal MconvFactAlloc { get; set; }

    public string MmulDivQtyFct { get; set; } = null!;

    public string MtraceableType { get; set; } = null!;

    public string MmpsFlag { get; set; } = null!;

    public string MpickingSlip { get; set; } = null!;

    public string MmovementReqd { get; set; } = null!;

    public string MserialMethod { get; set; } = null!;

    public string MzeroQtyCrNote { get; set; } = null!;

    public string MabcApplied { get; set; } = null!;

    public string MmpsGrossReqd { get; set; } = null!;

    public string Mcontract { get; set; } = null!;

    public string MbuyingGroup { get; set; } = null!;

    public string McusSupStkCode { get; set; } = null!;

    public decimal McusRetailPrice { get; set; }

    public string MtariffCode { get; set; } = null!;

    public DateTime? MlineReceiptDat { get; set; }

    public decimal MleadTime { get; set; }

    public decimal MtrfCostMult { get; set; }

    public string MsupplementaryUn { get; set; } = null!;

    public string MreviewFlag { get; set; } = null!;

    public string MreviewStatus { get; set; } = null!;

    public string MinvoicePrinted { get; set; } = null!;

    public string MdelNotePrinted { get; set; } = null!;

    public string MordAckPrinted { get; set; } = null!;

    public string MhierarchyJob { get; set; } = null!;

    public DateTime? McustRequestDat { get; set; }

    public string MlastDelNote { get; set; } = null!;

    public string MuserDef { get; set; } = null!;

    public decimal MqtyDispatched { get; set; }

    public string MdiscChanged { get; set; } = null!;

    public string McreditOrderNo { get; set; } = null!;

    public decimal McreditOrderLine { get; set; }

    public string MunitQuantity { get; set; } = null!;

    public decimal MconvFactUnitQ { get; set; }

    public string MaltUomUnitQ { get; set; } = null!;

    public decimal MdecimalsUnitQ { get; set; }

    public string MeccFlag { get; set; } = null!;

    public string Mversion { get; set; } = null!;

    public string Mrelease { get; set; } = null!;

    public DateTime? McommitDate { get; set; }

    public decimal QtyReserved { get; set; }

    public string Ncomment { get; set; } = null!;

    public decimal NcommentFromLin { get; set; }

    public decimal NmscChargeValue { get; set; }

    public string NmscProductCls { get; set; } = null!;

    public decimal NmscChargeCost { get; set; }

    public string NmscInvCharge { get; set; } = null!;

    public string NcommentType { get; set; } = null!;

    public string NmscTaxCode { get; set; } = null!;

    public string NmscFstCode { get; set; } = null!;

    public string NcommentTextTyp { get; set; } = null!;

    public decimal NmscChargeQty { get; set; }

    public string NsrvIncTotal { get; set; } = null!;

    public string NsrvSummary { get; set; } = null!;

    public string NsrvChargeType { get; set; } = null!;

    public decimal NsrvParentLine { get; set; }

    public decimal NsrvUnitPrice { get; set; }

    public decimal NsrvUnitCost { get; set; }

    public decimal NsrvQtyFactor { get; set; }

    public string NsrvApplyFactor { get; set; } = null!;

    public decimal NsrvDecimalRnd { get; set; }

    public string NsrvDecRndFlag { get; set; } = null!;

    public decimal NsrvMinValue { get; set; }

    public decimal NsrvMaxValue { get; set; }

    public string NsrvMulDiv { get; set; } = null!;

    public string NprtOnInv { get; set; } = null!;

    public string NprtOnDel { get; set; } = null!;

    public string NprtOnAck { get; set; } = null!;

    public string NtaxAmountFlag { get; set; } = null!;

    public string NdepRetFlagProj { get; set; } = null!;

    public string NretentionJob { get; set; } = null!;

    public decimal NsrvMinQuantity { get; set; }

    public string NchargeCode { get; set; } = null!;

    public string IncludeInMrp { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string LibraryCode { get; set; } = null!;

    public string MaterialAllocLine { get; set; } = null!;

    public decimal ScrapQuantity { get; set; }

    public string FixedQtyPerFlag { get; set; } = null!;

    public decimal FixedQtyPer { get; set; }

    public string MultiShipCode { get; set; } = null!;

    public string User1 { get; set; } = null!;

    public string CreditReason { get; set; } = null!;

    public DateTime? OrigShipDateAps { get; set; }

    public string TpmUsageFlag { get; set; } = null!;

    public string PromotionCode { get; set; } = null!;

    public decimal TpmSequence { get; set; }

    public decimal SalesOrderInitLine { get; set; }

    public decimal PreactorPriority { get; set; }

    public string SalesOrderDetStat { get; set; } = null!;

    public string SalesOrderResStat { get; set; } = null!;

    public decimal QtyReservedShip { get; set; }

    public byte[]? TimeStamp { get; set; }

    public decimal QtyReleasedToPick { get; set; }

    public string PickNumber { get; set; } = null!;

    public string IntrastatExempt { get; set; } = null!;

    public string TriangulationRole { get; set; } = null!;

    public string TriangDispState { get; set; } = null!;

    public string TriangDestState { get; set; } = null!;

    public string OrigCountry { get; set; } = null!;

    public decimal PriceGroupRule { get; set; }

    public string Catalogue { get; set; } = null!;

    public decimal CatLineNumber { get; set; }

    public virtual SorMaster SalesOrderNavigation { get; set; } = null!;

    public virtual ICollection<SorDetailBin> SorDetailBins { get; set; } = new List<SorDetailBin>();
}
