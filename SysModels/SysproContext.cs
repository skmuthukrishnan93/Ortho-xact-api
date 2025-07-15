using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Ortho_xact_api.SysModels;

public partial class SysproContext : DbContext
{
    public SysproContext()
    {
    }

    public SysproContext(DbContextOptions<SysproContext> options)
        : base(options)
    {
    }

    public virtual DbSet<SalSalesperson> SalSalespeople { get; set; }

    public virtual DbSet<SorDetail> SorDetails { get; set; }

    public virtual DbSet<SorMaster> SorMasters { get; set; }

    public virtual DbSet<VwFetchSordetail> VwFetchSordetails { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_BIN");

        modelBuilder.Entity<SalSalesperson>(entity =>
        {
            entity.HasKey(e => new { e.Branch, e.Salesperson }).HasName("SalSalespersonKey");

            entity.ToTable("SalSalesperson");

            entity.Property(e => e.Branch)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Salesperson)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.CommissionPct).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.SalesActual1).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual10).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual11).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual12).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual13).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual2).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual3).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual4).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual5).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual6).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual7).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual8).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesActual9).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SalesBudget1).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget10).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget11).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget12).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget13).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget2).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget3).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget4).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget5).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget6).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget7).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget8).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalesBudget9).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.SalespersonColl)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<SorDetail>(entity =>
        {
            entity.HasKey(e => new { e.SalesOrder, e.SalesOrderLine }).HasName("SorDetailKey");

            entity.ToTable("SorDetail");

            entity.HasIndex(e => new { e.McreditOrderNo, e.McreditOrderLine, e.SalesOrder, e.SalesOrderLine }, "SorDetailIdxCredited").IsUnique();

            entity.HasIndex(e => new { e.SalesOrderDetStat, e.SalesOrder, e.SalesOrderLine }, "SorDetailIdxDetStat").IsUnique();

            entity.HasIndex(e => new { e.MhierarchyJob, e.SalesOrder, e.SalesOrderLine }, "SorDetailIdxHier").IsUnique();

            entity.HasIndex(e => new { e.SalesOrder, e.SalesOrderInitLine, e.SalesOrderLine }, "SorDetailIdxInit").IsUnique();

            entity.HasIndex(e => new { e.MstockCode, e.SalesOrder, e.SalesOrderLine }, "SorDetailIdxStk").IsUnique();

            entity.Property(e => e.SalesOrder)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.SalesOrderLine).HasColumnType("decimal(4, 0)");
            entity.Property(e => e.CreditReason)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.FixedQtyPer).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.FixedQtyPerFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.IncludeInMrp)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.IntrastatExempt)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.LibraryCode)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.LineType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MabcApplied)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MAbcApplied");
            entity.Property(e => e.MallocStatSched)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MAllocStatSched");
            entity.Property(e => e.MaltUomUnitQ)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MAltUomUnitQ");
            entity.Property(e => e.MaterialAllocLine)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MbackOrderQty)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MBackOrderQty");
            entity.Property(e => e.Mbin)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MBin");
            entity.Property(e => e.MbomFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MBomFlag");
            entity.Property(e => e.MbuyingGroup)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MBuyingGroup");
            entity.Property(e => e.McommissionCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MCommissionCode");
            entity.Property(e => e.McommitDate)
                .HasColumnType("datetime")
                .HasColumnName("MCommitDate");
            entity.Property(e => e.McomponentSeq)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MComponentSeq");
            entity.Property(e => e.Mcontract)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MContract");
            entity.Property(e => e.MconvFactAlloc)
                .HasColumnType("decimal(12, 6)")
                .HasColumnName("MConvFactAlloc");
            entity.Property(e => e.MconvFactOrdUm)
                .HasColumnType("decimal(12, 6)")
                .HasColumnName("MConvFactOrdUm");
            entity.Property(e => e.MconvFactUnitQ)
                .HasColumnType("decimal(6, 0)")
                .HasColumnName("MConvFactUnitQ");
            entity.Property(e => e.McreditOrderLine)
                .HasColumnType("decimal(4, 0)")
                .HasColumnName("MCreditOrderLine");
            entity.Property(e => e.McreditOrderNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MCreditOrderNo");
            entity.Property(e => e.McusRetailPrice)
                .HasColumnType("decimal(15, 5)")
                .HasColumnName("MCusRetailPrice");
            entity.Property(e => e.McusSupStkCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MCusSupStkCode");
            entity.Property(e => e.McustRequestDat)
                .HasColumnType("datetime")
                .HasColumnName("MCustRequestDat");
            entity.Property(e => e.Mdecimals)
                .HasColumnType("decimal(1, 0)")
                .HasColumnName("MDecimals");
            entity.Property(e => e.MdecimalsUnitQ)
                .HasColumnType("decimal(1, 0)")
                .HasColumnName("MDecimalsUnitQ");
            entity.Property(e => e.MdelNotePrinted)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MDelNotePrinted");
            entity.Property(e => e.MdiscChanged)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MDiscChanged");
            entity.Property(e => e.MdiscPct1)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("MDiscPct1");
            entity.Property(e => e.MdiscPct2)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("MDiscPct2");
            entity.Property(e => e.MdiscPct3)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("MDiscPct3");
            entity.Property(e => e.MdiscValFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MDiscValFlag");
            entity.Property(e => e.MdiscValue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("MDiscValue");
            entity.Property(e => e.MeccFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MEccFlag");
            entity.Property(e => e.MfstTaxCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MFstTaxCode");
            entity.Property(e => e.MhierarchyJob)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MHierarchyJob");
            entity.Property(e => e.MinvoicePrinted)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MInvoicePrinted");
            entity.Property(e => e.MlastDelNote)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MLastDelNote");
            entity.Property(e => e.MleadTime)
                .HasColumnType("decimal(10, 0)")
                .HasColumnName("MLeadTime");
            entity.Property(e => e.MlineReceiptDat)
                .HasColumnType("datetime")
                .HasColumnName("MLineReceiptDat");
            entity.Property(e => e.MlineShipDate)
                .HasColumnType("datetime")
                .HasColumnName("MLineShipDate");
            entity.Property(e => e.MmovementReqd)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MMovementReqd");
            entity.Property(e => e.MmpsFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MMpsFlag");
            entity.Property(e => e.MmpsGrossReqd)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MMpsGrossReqd");
            entity.Property(e => e.MmulDivPrcFct)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MMulDivPrcFct");
            entity.Property(e => e.MmulDivQtyFct)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MMulDivQtyFct");
            entity.Property(e => e.MoptionalFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MOptionalFlag");
            entity.Property(e => e.MordAckPrinted)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MOrdAckPrinted");
            entity.Property(e => e.MorderQty)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MOrderQty");
            entity.Property(e => e.MorderUom)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MOrderUom");
            entity.Property(e => e.MparentKitType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MParentKitType");
            entity.Property(e => e.MpickingSlip)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MPickingSlip");
            entity.Property(e => e.Mprice)
                .HasColumnType("decimal(15, 5)")
                .HasColumnName("MPrice");
            entity.Property(e => e.MpriceCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MPriceCode");
            entity.Property(e => e.MpriceUom)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MPriceUom");
            entity.Property(e => e.MprintComponent)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MPrintComponent");
            entity.Property(e => e.MproductClass)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MProductClass");
            entity.Property(e => e.MqtyChangesFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MQtyChangesFlag");
            entity.Property(e => e.MqtyDispatched)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MQtyDispatched");
            entity.Property(e => e.MqtyPer)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MQtyPer");
            entity.Property(e => e.Mrelease)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MRelease");
            entity.Property(e => e.MreviewFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MReviewFlag");
            entity.Property(e => e.MreviewStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MReviewStatus");
            entity.Property(e => e.MscrapPercentage)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("MScrapPercentage");
            entity.Property(e => e.MserialMethod)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MSerialMethod");
            entity.Property(e => e.MshipQty)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MShipQty");
            entity.Property(e => e.MstockCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MStockCode");
            entity.Property(e => e.MstockDes)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MStockDes");
            entity.Property(e => e.MstockQtyToShp)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MStockQtyToShp");
            entity.Property(e => e.MstockUnitMass)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MStockUnitMass");
            entity.Property(e => e.MstockUnitVol)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MStockUnitVol");
            entity.Property(e => e.MstockingUom)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MStockingUom");
            entity.Property(e => e.MsupplementaryUn)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MSupplementaryUn");
            entity.Property(e => e.MtariffCode)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MTariffCode");
            entity.Property(e => e.MtaxCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MTaxCode");
            entity.Property(e => e.MtraceableType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MTraceableType");
            entity.Property(e => e.MtrfCostMult)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("MTrfCostMult");
            entity.Property(e => e.MultiShipCode)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MunitCost)
                .HasColumnType("decimal(15, 5)")
                .HasColumnName("MUnitCost");
            entity.Property(e => e.MunitQuantity)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MUnitQuantity");
            entity.Property(e => e.MuserDef)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MUserDef");
            entity.Property(e => e.Mversion)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MVersion");
            entity.Property(e => e.Mwarehouse)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("MWarehouse");
            entity.Property(e => e.MzeroQtyCrNote)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("MZeroQtyCrNote");
            entity.Property(e => e.NchargeCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("NChargeCode");
            entity.Property(e => e.Ncomment)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("NComment");
            entity.Property(e => e.NcommentFromLin)
                .HasColumnType("decimal(4, 0)")
                .HasColumnName("NCommentFromLin");
            entity.Property(e => e.NcommentTextTyp)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NCommentTextTyp");
            entity.Property(e => e.NcommentType)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NCommentType");
            entity.Property(e => e.NdepRetFlagProj)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NDepRetFlagProj");
            entity.Property(e => e.NmscChargeCost)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("NMscChargeCost");
            entity.Property(e => e.NmscChargeQty)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("NMscChargeQty");
            entity.Property(e => e.NmscChargeValue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("NMscChargeValue");
            entity.Property(e => e.NmscFstCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NMscFstCode");
            entity.Property(e => e.NmscInvCharge)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NMscInvCharge");
            entity.Property(e => e.NmscProductCls)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("NMscProductCls");
            entity.Property(e => e.NmscTaxCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NMscTaxCode");
            entity.Property(e => e.NprtOnAck)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NPrtOnAck");
            entity.Property(e => e.NprtOnDel)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NPrtOnDel");
            entity.Property(e => e.NprtOnInv)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NPrtOnInv");
            entity.Property(e => e.NretentionJob)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .HasColumnName("NRetentionJob");
            entity.Property(e => e.NsrvApplyFactor)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NSrvApplyFactor");
            entity.Property(e => e.NsrvChargeType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NSrvChargeType");
            entity.Property(e => e.NsrvDecRndFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NSrvDecRndFlag");
            entity.Property(e => e.NsrvDecimalRnd)
                .HasColumnType("decimal(1, 0)")
                .HasColumnName("NSrvDecimalRnd");
            entity.Property(e => e.NsrvIncTotal)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NSrvIncTotal");
            entity.Property(e => e.NsrvMaxValue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("NSrvMaxValue");
            entity.Property(e => e.NsrvMinQuantity)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("NSrvMinQuantity");
            entity.Property(e => e.NsrvMinValue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("NSrvMinValue");
            entity.Property(e => e.NsrvMulDiv)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NSrvMulDiv");
            entity.Property(e => e.NsrvParentLine)
                .HasColumnType("decimal(4, 0)")
                .HasColumnName("NSrvParentLine");
            entity.Property(e => e.NsrvQtyFactor)
                .HasColumnType("decimal(12, 6)")
                .HasColumnName("NSrvQtyFactor");
            entity.Property(e => e.NsrvSummary)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NSrvSummary");
            entity.Property(e => e.NsrvUnitCost)
                .HasColumnType("decimal(15, 5)")
                .HasColumnName("NSrvUnitCost");
            entity.Property(e => e.NsrvUnitPrice)
                .HasColumnType("decimal(15, 5)")
                .HasColumnName("NSrvUnitPrice");
            entity.Property(e => e.NtaxAmountFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("NTaxAmountFlag");
            entity.Property(e => e.OrigCountry)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.OrigShipDateAps).HasColumnType("datetime");
            entity.Property(e => e.PickNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.PreactorPriority).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.PriceGroupRule).HasColumnType("decimal(4, 0)");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.PromotionCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.QtyReleasedToPick).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.QtyReserved).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.QtyReservedShip).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.SalesOrderDetStat)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.SalesOrderInitLine).HasColumnType("decimal(4, 0)");
            entity.Property(e => e.SalesOrderResStat)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.ScrapQuantity).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.TimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TpmSequence).HasColumnType("decimal(6, 0)");
            entity.Property(e => e.TpmUsageFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TriangDestState)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TriangDispState)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TriangulationRole)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.User1)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();

            entity.HasOne(d => d.SalesOrderNavigation).WithMany(p => p.SorDetails)
                .HasForeignKey(d => d.SalesOrder)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Syspro_FK_SorDetail_SorMaster");
        });

        modelBuilder.Entity<SorMaster>(entity =>
        {
            entity.HasKey(e => e.SalesOrder).HasName("SorMasterKey");

            entity.ToTable("SorMaster");

            entity.HasIndex(e => new { e.ActiveFlag, e.SalesOrder }, "SorMasterIdxActive").IsUnique();

            entity.HasIndex(e => new { e.CancelledFlag, e.AlternateKey, e.SalesOrder }, "SorMasterIdxAlt").IsUnique();

            entity.HasIndex(e => new { e.CancelledFlag, e.CustomerPoNumber, e.SalesOrder }, "SorMasterIdxCspo").IsUnique();

            entity.HasIndex(e => new { e.CancelledFlag, e.Customer, e.SalesOrder }, "SorMasterIdxCus").IsUnique();

            entity.HasIndex(e => new { e.CancelledFlag, e.DetailStatus, e.SalesOrder }, "SorMasterIdxDetStat").IsUnique();

            entity.HasIndex(e => new { e.SalesOrderSource, e.SalesOrderSrcDesc, e.SalesOrder }, "SorMasterIdxSource").IsUnique();

            entity.HasIndex(e => new { e.CancelledFlag, e.InterWhSale, e.SalesOrder }, "SorMasterIdxWhSale").IsUnique();

            entity.Property(e => e.SalesOrder)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ActiveFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.AltShipAddrFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.AlternateKey)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Area)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Branch)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.CancelledFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.CaptureHh).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.CaptureMm).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.CashCredit)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.CommissionSales1).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.CommissionSales2).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.CommissionSales3).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.CommissionSales4).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.CompanyTaxNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ConsolidatedOrder)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.CounterSalesFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.CountyZip)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.CreditAuthority)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.CreditedInvDate).HasColumnType("datetime");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.Customer)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.CustomerPoNumber)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.DateLastDocPrt).HasColumnType("datetime");
            entity.Property(e => e.DateLastInvPrt).HasColumnType("datetime");
            entity.Property(e => e.DeliveryNote)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.DeliveryTerms)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DepositFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DetCustMvmtReqd)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DetailStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DiscPct1).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiscPct2).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiscPct3).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DispatchesMade)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DocumentFormat)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DocumentType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.EdiSource)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.EntInvoice)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.EntInvoiceDate).HasColumnType("datetime");
            entity.Property(e => e.EntrySystemDate).HasColumnType("datetime");
            entity.Property(e => e.ExchangeRate).HasColumnType("decimal(12, 6)");
            entity.Property(e => e.ExtendedTaxCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.FaxInvInBatch)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.FixExchangeRate)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.GstDeduction)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.GstExemptFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.GstExemptNum)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.GstExemptOride)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasColumnName("GstExemptORide");
            entity.Property(e => e.GtrReference)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.HierarchyFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.IbtFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.IncludeInMrp)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.InterWhSale)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.InvTermsOverride)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.InvoiceCount).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.Job)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.JobsExistFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.LanguageCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.LastDelNote).HasColumnType("datetime");
            entity.Property(e => e.LastInvoice)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.LastOperator)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.LineComp)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.LiveDispExist)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MulDiv)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MultiShipCode)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.Nationality)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.NextDetailLine).HasColumnType("decimal(4, 0)");
            entity.Property(e => e.NonMerchFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.NumDispatches).HasColumnType("decimal(4, 0)");
            entity.Property(e => e.Operator)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.OrdAcknwPrinted)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.OrderStatusFail)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.OrderType)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.PortDispatch)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.PriceGroup)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.PriceGroupLevel)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.ProcessFlag).HasColumnType("decimal(1, 0)");
            entity.Property(e => e.QuickQuote)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Quote)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.QuoteVersion).HasColumnType("decimal(3, 0)");
            entity.Property(e => e.RegimeCode)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.ReqShipDate).HasColumnType("datetime");
            entity.Property(e => e.SalesOrderSource)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.SalesOrderSrcDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Salesperson)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Salesperson2)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Salesperson3)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.Salesperson4)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ScheduledOrdFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.SerialisedFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.ShipAddress1)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipAddress2)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipAddress3)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipAddress3Loc)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipAddress4)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipAddress5)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipPostalCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShipToGpsLat).HasColumnType("decimal(8, 6)");
            entity.Property(e => e.ShipToGpsLong).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.ShippingInstrs)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShippingInstrsCod)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.ShippingLocation)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.SourceWarehouse)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.SpecialInstrs)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.StandardComment)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.State)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TargetWarehouse)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.TaxExemptFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TaxExemptNumber)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.TaxExemptOverride)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TimeDelPrtedHh).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.TimeDelPrtedMm).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.TimeInvPrtedHh).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.TimeInvPrtedMm).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.TimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TimeTakenToAdd).HasColumnType("decimal(3, 0)");
            entity.Property(e => e.TimeTakenToChg).HasColumnType("decimal(3, 0)");
            entity.Property(e => e.TpmEvaluatedFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TpmPickupFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.TransactionNature).HasColumnType("decimal(3, 0)");
            entity.Property(e => e.TransportMode).HasColumnType("decimal(2, 0)");
            entity.Property(e => e.User1)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.Warehouse)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(" ");
            entity.Property(e => e.WebCreated)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue(" ")
                .IsFixedLength();

            entity.HasOne(d => d.SalSalesperson).WithMany(p => p.SorMasters)
                .HasForeignKey(d => new { d.Branch, d.Salesperson })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Syspro_FK_SorMaster_SalSalesperson");
        });

        modelBuilder.Entity<VwFetchSordetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_FetchSORDetails");

            entity.Property(e => e.Customer)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CustomerName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MbomFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MBomFlag");
            entity.Property(e => e.MorderQty)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MOrderQty");
            entity.Property(e => e.MshipQty)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("MShipQty");
            entity.Property(e => e.MstockCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MStockCode");
            entity.Property(e => e.MstockDes)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MStockDes");
            entity.Property(e => e.Mwarehouse)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MWarehouse");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.RepUsageQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RetQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SalesOrder)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SalesOrderLine).HasColumnType("decimal(4, 0)");
            entity.Property(e => e.Salesperson)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SetsCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Setsdec)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .UseCollation("Latin1_General_CI_AS");
            entity.Property(e => e.Usage).HasColumnType("decimal(23, 6)");
            entity.Property(e => e.Variance)
                .HasColumnType("decimal(19, 2)")
                .HasColumnName("variance");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
