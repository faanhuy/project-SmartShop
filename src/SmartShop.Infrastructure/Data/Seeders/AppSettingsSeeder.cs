using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Seeders;

internal sealed class AppSettingsSeeder(
    ApplicationDbContext context,
    ILogger<AppSettingsSeeder> logger) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await context.AppSettings.AnyAsync(cancellationToken))
        {
            var defaults = new[]
            {
                AppSetting.Create("AI:Search:MinScore",          "0.3",      "number", "Điểm tối thiểu để hiển thị kết quả tìm kiếm AI (0.0 - 1.0)"),
                AppSetting.Create("AI:Search:TopN",              "8",         "number", "Số kết quả tối đa trả về khi tìm kiếm AI"),
                AppSetting.Create("AI:Recommendations:Count",    "5",         "number", "Số sản phẩm gợi ý tối đa"),
                AppSetting.Create("AI:Recommendations:MinScore", "0.4",       "number", "Điểm tối thiểu để hiển thị gợi ý sản phẩm"),
                AppSetting.Create("Site:Name",                   "FastFood",  "text",   "Tên hiển thị của website"),
            };
            await context.AppSettings.AddRangeAsync(defaults, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} AppSettings.", defaults.Length);
            return;
        }

        var siteNameSetting = await context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == "Site:Name", cancellationToken);
        if (siteNameSetting is not null && siteNameSetting.Value != "FastFood")
        {
            siteNameSetting.SetValue("FastFood");
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
