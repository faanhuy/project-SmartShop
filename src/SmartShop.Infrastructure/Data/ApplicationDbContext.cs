using Microsoft.EntityFrameworkCore;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Common;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ProductEmbedding> ProductEmbeddings { get; set; }

    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<CouponUsage> CouponUsages { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<FaqDocument> FaqDocuments => Set<FaqDocument>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<StoreInventory> StoreInventories { get; set; }
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Ward> Wards => Set<Ward>();

    private static readonly TimeZoneInfo _vnTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static DateTime NowVn() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        string? currentUserId = null;
        try { currentUserId = _currentUserService?.UserId; }
        catch { /* unauthenticated context (background jobs, migrations) */ }

        var now = NowVn();

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUserId;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUserId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discover all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
