using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Ortho_xact_api.Models;

public partial class OrthoxactContext : DbContext
{
    public OrthoxactContext()
    {
    }

    public OrthoxactContext(DbContextOptions<OrthoxactContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AreaMapping> AreaMappings { get; set; }

    public virtual DbSet<DeliveryOrderDetail> DeliveryOrderDetails { get; set; }

    public virtual DbSet<DocumentDetail> DocumentDetails { get; set; }

    public virtual DbSet<EmailSetting> EmailSettings { get; set; }

    public virtual DbSet<SysproPostLog> SysproPostLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AreaMapping>(entity =>
        {
            entity.ToTable("AreaMapping");

            entity.Property(e => e.Area).HasMaxLength(100);
            entity.Property(e => e.MappedBy).HasMaxLength(50);
            entity.Property(e => e.MappedDate).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(500);
        });

        modelBuilder.Entity<DeliveryOrderDetail>(entity =>
        {
            entity.HasKey(e => new { e.SalesOrder, e.Line });

            entity.Property(e => e.SalesOrder).HasMaxLength(50);
            entity.Property(e => e.AdminVerNumber).HasMaxLength(50);
            entity.Property(e => e.ClerkDate).HasColumnType("datetime");
            entity.Property(e => e.ClerkName).HasMaxLength(50);
            entity.Property(e => e.ClerkVerNumber).HasMaxLength(50);
            entity.Property(e => e.Customer).HasMaxLength(50);
            entity.Property(e => e.CustomerName).HasMaxLength(500);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MorderQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MshipQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MstockCode).HasMaxLength(50);
            entity.Property(e => e.MstockDes).HasMaxLength(500);
            entity.Property(e => e.Mwarehouse).HasMaxLength(50);
            entity.Property(e => e.PostedBy).HasMaxLength(100);
            entity.Property(e => e.PostedDate).HasColumnType("datetime");
            entity.Property(e => e.RepEntertedDate).HasColumnType("datetime");
            entity.Property(e => e.RepName).HasMaxLength(50);
            entity.Property(e => e.RepUsageQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RepVerNumber).HasMaxLength(50);
            entity.Property(e => e.RetQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoutedClerk).HasMaxLength(50);
            entity.Property(e => e.Set).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Usage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Variance).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<DocumentDetail>(entity =>
        {
            entity.HasKey(e => e.DocId);

            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.DocNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<EmailSetting>(entity =>
        {
            entity.Property(e => e.Bcc).HasColumnName("BCC");
            entity.Property(e => e.Cc).HasColumnName("CC");
            entity.Property(e => e.FromAddress).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.PortNumber).HasMaxLength(50);
            entity.Property(e => e.SmtpServer).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<SysproPostLog>(entity =>
        {
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.SalesOrder).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.DefaultRouteClerk).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Firstname).HasMaxLength(100);
            entity.Property(e => e.Lastname).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Roles).HasMaxLength(50);
            entity.Property(e => e.Salesperson).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
