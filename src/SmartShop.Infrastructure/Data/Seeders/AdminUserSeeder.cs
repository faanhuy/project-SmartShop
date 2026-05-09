using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Seeders;

internal sealed class AdminUserSeeder(
    ApplicationDbContext context,
    IPasswordHasher hasher,
    ILogger<AdminUserSeeder> logger) : IDataSeeder
{
    public int Order => 2;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(u => u.Role == "Admin", cancellationToken))
            return;

        var admin = User.Create("admin@fastfood.vn", hasher.Hash("Admin@123"), "Admin", "FastFood");
        admin.PromoteToAdmin();
        await context.Users.AddAsync(admin, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded admin user: admin@fastfood.vn / Admin@123");
    }
}
