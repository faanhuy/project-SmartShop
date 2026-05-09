using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Seeders;

internal sealed record ProvinceSeed(int Id, string Name, string Code);
internal sealed record WardSeed(int Id, int ProvinceId, string Name, string Code);
internal sealed record GeographySeedData(ProvinceSeed[] Provinces, WardSeed[] Wards);

internal sealed class GeographySeeder(
    ApplicationDbContext context,
    ILogger<GeographySeeder> logger) : IDataSeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Provinces.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Geography data already seeded, skipping.");
            return;
        }

        var seedData = LoadSeedData();
        if (seedData is null)
        {
            logger.LogWarning("Could not load vietnam-geography.json. Skipping geography seed.");
            return;
        }

        var provinces = seedData.Provinces
            .Select(p => Province.Create(p.Id, p.Name, p.Code))
            .ToList();

        await context.Provinces.AddRangeAsync(provinces, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var wards = seedData.Wards
            .Select(w => Ward.Create(w.Id, w.ProvinceId, w.Name, w.Code))
            .ToList();

        await context.Wards.AddRangeAsync(wards, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Geography seeded: {ProvinceCount} provinces, {WardCount} wards.",
            provinces.Count,
            wards.Count);
    }

    private static GeographySeedData? LoadSeedData()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "vietnam-geography.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "vietnam-geography.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedData", "vietnam-geography.json")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<GeographySeedData>(json, options);
        }

        return null;
    }
}
