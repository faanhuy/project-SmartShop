using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartShop.Infrastructure.Data.Seeders;

namespace SmartShop.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope  = serviceProvider.CreateScope();
        var context      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger       = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            var seeders = scope.ServiceProvider
                .GetServices<IDataSeeder>()
                .OrderBy(s => s.Order);

            foreach (var seeder in seeders)
                await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding the database.");
            throw;
        }
    }
}
