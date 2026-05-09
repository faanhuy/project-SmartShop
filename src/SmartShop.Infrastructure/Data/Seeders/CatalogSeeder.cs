using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Seeders;

internal sealed record CategorySeed(string Name, string Slug, string Description);

internal sealed record ProductSeed(
    string Name,
    string Description,
    decimal Price,
    decimal? OriginalPrice,
    string CategorySlug,
    string Slug,
    string? ImageUrl = null);

internal sealed class CatalogSeeder(
    ApplicationDbContext context,
    ICacheService cache,
    ILogger<CatalogSeeder> logger) : IDataSeeder
{
    public int Order => 4;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Synchronizing food delivery catalog...");

        var categorySeeds = GetCategorySeeds();
        var productSeeds  = GetProductSeeds();

        await SyncCatalogAsync(categorySeeds, productSeeds, cancellationToken);
        await InvalidateCatalogCachesAsync();

        logger.LogInformation(
            "Catalog synchronized: {CategoryCount} categories, {ProductCount} products.",
            categorySeeds.Length,
            productSeeds.Length);
    }

    private async Task SyncCatalogAsync(
        IReadOnlyCollection<CategorySeed> categorySeeds,
        IReadOnlyCollection<ProductSeed> productSeeds,
        CancellationToken cancellationToken)
    {
        var categorySeedMap = categorySeeds.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
        var productSeedMap  = productSeeds.ToDictionary(s => s.Slug,  StringComparer.OrdinalIgnoreCase);

        var existingCategories = await context.Categories.ToListAsync(cancellationToken);

        foreach (var category in existingCategories.Where(c => c.IsActive && !categorySeedMap.ContainsKey(c.Slug)))
            category.Deactivate();

        foreach (var seed in categorySeeds)
        {
            var existing = existingCategories.FirstOrDefault(c => c.Slug == seed.Slug);
            if (existing is null)
            {
                await context.Categories.AddAsync(Category.Create(seed.Name, seed.Slug, seed.Description), cancellationToken);
                continue;
            }
            existing.Update(seed.Name, seed.Slug, seed.Description, existing.ImageUrl);
            existing.Activate();
        }

        await context.SaveChangesAsync(cancellationToken);

        var targetCategories = await context.Categories
            .Where(c => categorySeedMap.Keys.Contains(c.Slug))
            .ToListAsync(cancellationToken);
        var categoryIdsBySlug = targetCategories.ToDictionary(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var existingProducts = await context.Products.ToListAsync(cancellationToken);

        foreach (var product in existingProducts.Where(p => p.IsActive && !productSeedMap.ContainsKey(p.Slug)))
            product.Deactivate();

        foreach (var seed in productSeeds)
        {
            var existing = existingProducts.FirstOrDefault(p => p.Slug == seed.Slug);
            if (existing is null)
            {
                await context.Products.AddAsync(Product.Create(
                    seed.Name,
                    seed.Description,
                    seed.Price,
                    categoryIdsBySlug[seed.CategorySlug],
                    seed.Slug,
                    seed.ImageUrl,
                    seed.OriginalPrice), cancellationToken);
                continue;
            }
            existing.Update(seed.Name, seed.Description, seed.Price, seed.ImageUrl, seed.OriginalPrice ?? seed.Price);
            existing.Activate();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task InvalidateCatalogCachesAsync()
    {
        await cache.RemoveAsync("categories:all");
        await cache.RemoveAsync("categories:all:v2");
        await cache.RemoveByPrefixAsync("products:list:");
        await cache.RemoveByPrefixAsync("products:id:");
        await cache.RemoveByPrefixAsync("products:slug:");
    }

    private static CategorySeed[] GetCategorySeeds() =>
    [
        new("Burger",                "burger",              "Burger bò, gà rán và burger đặc biệt cho bữa ăn nhanh gọn."),
        new("Gà Rán",                "ga-ran",              "Gà rán giòn, combo nhóm và món ăn kèm đang hot."),
        new("Pizza",                 "pizza",               "Pizza đế mỏng, đế dày và pizza chia sẻ cho nhiều người."),
        new("Mì Ý & Nui",            "mi-y-nui",            "Mì Ý sốt bò, nui đặc biệt và món no bụng giao nhanh."),
        new("Đồ Uống & Tráng Miệng", "do-uong-trang-mieng", "Trà sữa, nước ngọt, kem và món tráng miệng mát lạnh."),
    ];

    private static ProductSeed[] GetProductSeeds() =>
    [
        new("Burger Bò Phô Mai Kép",       "Hai lớp bò nướng, hai lớp phô mai cheddar, sốt burger đặc trưng và dưa chua giòn.",   89000m,  109000m, "burger",              "burger-bo-phomai-kep",        Image("burger-bo-phomai-kep.svg")),
        new("Burger Gà Sốt Hàn",           "Gà giòn sốt Hàn cay nhẹ, bắp cải tím và sốt mayonnaise vị rong biển.",               75000m,   92000m, "burger",              "burger-ga-sot-han",           Image("burger-ga-sot-han.svg")),
        new("Burger Tôm Giòn",             "Tôm tẩm bột giòn rụm, rau sống, sốt thousand island và bánh brioche mềm.",            79000m,   95000m, "burger",              "burger-tom-gion",             Image("burger-tom-gion.svg")),
        new("Combo Burger Duo",            "1 burger bò phô mai kép, 1 khoai tây lớn và 2 ly nước ngăn đá.",                     159000m,  189000m, "burger",              "combo-burger-duo",            Image("combo-burger-duo.svg")),
        new("Gà Rán Truyền Thống 3 Miếng", "Ba miếng gà giòn rụm, ướp muối tiêu tỏi và bột chiên siêu giòn.",                    99000m,  119000m, "ga-ran",              "ga-ran-truyen-thong-3-mieng", Image("ga-ran-truyen-thong-3-mieng.svg")),
        new("Combo Gà Gia Đình",           "6 miếng gà rán, 2 khoai tây vừa, 1 salad bắp cải và 4 ly nước.",                    269000m,  315000m, "ga-ran",              "combo-ga-gia-dinh",           Image("combo-ga-gia-dinh.svg")),
        new("Cánh Gà BBQ",                "Cánh gà sốt BBQ ngọt khói, phủ mè rang và lá parsley.",                               69000m,   82000m, "ga-ran",              "canh-ga-bbq",                 Image("canh-ga-bbq.svg")),
        new("Khoai Tây Lắc Phô Mai",       "Khoai tây chiên giòn, phủ bột phô mai cheddar và rau mùi sấy.",                      39000m,     null, "ga-ran",              "khoai-tay-lac-pho-mai",       Image("khoai-tay-lac-pho-mai.svg")),
        new("Pizza Hải Sản Sốt Pesto",     "Tôm, mực, pesto basil, olive đen và mozzarella nướng nóng.",                        179000m,  219000m, "pizza",               "pizza-hai-san-sot-pesto",     Image("pizza-hai-san-sot-pesto.svg")),
        new("Pizza Bò Bằm Xúc Xích",       "Bò bằm xào hành, xúc xích hun khói và phô mai phủ đầy mặt.",                       169000m,  205000m, "pizza",               "pizza-bo-bam-xuc-xich",       Image("pizza-bo-bam-xuc-xich.svg")),
        new("Pizza 4 Loại Phô Mai",        "Mozzarella, cheddar, parmesan, blue cheese và mật ong cay nhẹ.",                    189000m,  229000m, "pizza",               "pizza-4-loai-pho-mai",        Image("pizza-4-loai-pho-mai.svg")),
        new("Combo Pizza Tiệc Nhỏ",        "1 pizza size L, 4 cánh gà BBQ và 1 chai cola 1.5L.",                                259000m,  309000m, "pizza",               "combo-pizza-tiec-nho",        Image("combo-pizza-tiec-nho.svg")),
        new("Mì Ý Bò Bằm Sốt Cà Chua",    "Mì Ý al dente, sốt cà chua nấu chậm, bò bằm và phô mai parmesan.",                  85000m,   99000m, "mi-y-nui",            "mi-y-bo-bam-sot-ca-chua",     Image("mi-y-bo-bam-sot-ca-chua.svg")),
        new("Mì Ý Sốt Kem Nấm",           "Sốt kem béo nhẹ, nấm mỡ, thịt xông khói và rau mùi tây.",                           89000m,  105000m, "mi-y-nui",            "mi-y-sot-kem-nam",            Image("mi-y-sot-kem-nam.svg")),
        new("Nui Bò Lúc Lắc",             "Nui xào bò lúc lắc, ớt chuông, hành tây và tiêu đen xay mới.",                      92000m,     null, "mi-y-nui",            "nui-bo-luc-lac",              Image("nui-bo-luc-lac.svg")),
        new("Combo Mì Trưa Văn Phòng",     "1 mì Ý bò bằm, 1 soup ngô và 1 trà đào mát lạnh.",                                119000m,  145000m, "mi-y-nui",            "combo-mi-trua-van-phong",     Image("combo-mi-trua-van-phong.svg")),
        new("Trà Sữa Trân Châu Đường Đen", "Trà sữa Đài Loan đậm vị, trân châu đường đen nấu mới mỗi ngày.",                    42000m,   49000m, "do-uong-trang-mieng", "tra-sua-tran-chau-duong-den", Image("tra-sua-tran-chau-duong-den.svg")),
        new("Soda Chanh Dây",             "Soda mát lạnh với syrup chanh dây, hạt chia và lát cam tươi.",                       35000m,     null, "do-uong-trang-mieng", "soda-chanh-day",              Image("soda-chanh-day.svg")),
        new("Cola Không Đường",           "Lon cola lạnh giao cùng đá viên, hợp với burger và gà rán.",                         19000m,     null, "do-uong-trang-mieng", "cola-khong-duong",            Image("cola-khong-duong.svg")),
        new("Kem Sundae Sô Cô La",        "Kem vanilla mềm mịn, sốt sô cô la đậm và hạt hạnh nhân rang.",                      29000m,     null, "do-uong-trang-mieng", "kem-sundae-socola",           Image("kem-sundae-socola.svg")),
        new("Tiramisu Hộp",               "Tiramisu mềm, vị cà phê nhẹ, lớp kem mascarpone phủ cocoa.",                         45000m,   52000m, "do-uong-trang-mieng", "tiramisu-hop",                Image("tiramisu-hop.svg")),
        new("Combo Ăn Đêm",               "1 burger gà sốt Hàn, 1 cánh gà BBQ và 1 soda chanh dây.",                          129000m,  154000m, "burger",              "combo-an-dem",                Image("combo-an-dem.svg")),
        new("Combo Gà Cay Nhẹ",           "2 miếng gà rán, 1 khoai tây lắc phô mai và 1 trà sữa size M.",                     115000m,  139000m, "ga-ran",              "combo-ga-cay-nhe",            Image("combo-ga-cay-nhe.svg")),
        new("Pizza Pepperoni Mini",        "Pizza size mini đế mỏng, pepperoni hun khói và phô mai mozzarella.",                99000m,  119000m, "pizza",               "pizza-pepperoni-mini",        Image("pizza-pepperoni-mini.svg")),
    ];

    private static string Image(string fileName) => $"local:{fileName}";
}
