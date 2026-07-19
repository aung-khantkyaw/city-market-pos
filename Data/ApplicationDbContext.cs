using CityMarketPOS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Category> Categories { get; set; }
    public DbSet<UOM> UOMs { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
    public DbSet<GRN> GRNs { get; set; }
    public DbSet<GRNDetail> GRNDetails { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<StockAdjustment> StockAdjustments { get; set; }
    public DbSet<Counter> Counters { get; set; }
    public DbSet<POSSession> POSSessions { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleDetail> SaleDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal precision for financial fields
        modelBuilder.Entity<POSSession>()
            .Property(p => p.OpeningBalance)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<POSSession>()
            .Property(p => p.ClosingBalance)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Sale>()
            .Property(p => p.Subtotal)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<Sale>()
            .Property(p => p.Tax)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<Sale>()
            .Property(p => p.Total)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<Sale>()
            .Property(p => p.Discount)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<Sale>()
            .Property(p => p.GrandTotal)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<SaleDetail>()
            .Property(p => p.UnitPrice)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<SaleDetail>()
            .Property(p => p.TotalPrice)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<SaleDetail>()
            .Property(p => p.Discount)
            .HasColumnType("decimal(18,2)");
        
        modelBuilder.Entity<SaleDetail>()
            .Property(p => p.LineTotal)
            .HasColumnType("decimal(18,2)");

        // Fix cascade delete cycle - remove cascade from Sale.POSSessionId relationship
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.POSSession)
            .WithMany()
            .HasForeignKey(s => s.POSSessionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Fix cascade delete cycle - remove cascade from SaleDetail relationships
        modelBuilder.Entity<SaleDetail>()
            .HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SaleDetail>()
            .HasOne(d => d.GRNDetail)
            .WithMany()
            .HasForeignKey(d => d.GRNDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UOM>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Counter>().HasQueryFilter(e => !e.IsDeleted);
    }
}