using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data;

internal sealed record CategorySeed(string Name, string Slug, string Description);

internal sealed record ProductSeed(
    string Name,
    string Description,
    decimal Price,
    decimal? OriginalPrice,
    string CategorySlug,
    string Slug,
    string? ImageUrl = null);

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache   = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var hasher  = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger  = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            // Seed FAQ documents nếu chưa có
            await SeedFaqDocumentsAsync(context, logger);

            // Seed AppSettings nếu chưa có
            if (!await context.AppSettings.AnyAsync())
            {
                var defaults = new[]
                {
                    AppSetting.Create("AI:Search:MinScore",          "0.3",       "number",  "Điểm tối thiểu để hiển thị kết quả tìm kiếm AI (0.0 - 1.0)"),
                    AppSetting.Create("AI:Search:TopN",              "8",         "number",  "Số kết quả tối đa trả về khi tìm kiếm AI"),
                    AppSetting.Create("AI:Recommendations:Count",    "5",         "number",  "Số sản phẩm gợi ý tối đa"),
                    AppSetting.Create("AI:Recommendations:MinScore", "0.4",       "number",  "Điểm tối thiểu để hiển thị gợi ý sản phẩm"),
                    AppSetting.Create("Site:Name",                   "FastFood",  "text",    "Tên hiển thị của website"),
                };
                await context.AppSettings.AddRangeAsync(defaults);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} AppSettings.", defaults.Length);
            }
            else
            {
                var siteNameSetting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == "Site:Name");
                if (siteNameSetting is not null && siteNameSetting.Value != "FastFood")
                {
                    siteNameSetting.SetValue("FastFood");
                    await context.SaveChangesAsync();
                }
            }

            // Seed Admin user nếu chưa có
            if (!await context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                var admin = User.Create("admin@fastfood.vn", hasher.Hash("Admin@123"), "Admin", "FastFood");
                admin.PromoteToAdmin();
                await context.Users.AddAsync(admin);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded admin user: admin@fastfood.vn / Admin@123");
            }

            logger.LogInformation("Synchronizing food delivery catalog...");

            var categorySeeds = GetCategorySeeds();
            var productSeeds = GetProductSeeds();

            await SyncFoodDeliveryCatalogAsync(context, categorySeeds, productSeeds);
            await InvalidateCatalogCachesAsync(cache);

            logger.LogInformation(
                "Catalog synchronized: {CategoryCount} food categories, {ProductCount} menu items",
                categorySeeds.Length,
                productSeeds.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SyncFoodDeliveryCatalogAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<CategorySeed> categorySeeds,
        IReadOnlyCollection<ProductSeed> productSeeds)
    {
        var categorySeedMap = categorySeeds.ToDictionary(seed => seed.Slug, StringComparer.OrdinalIgnoreCase);
        var productSeedMap = productSeeds.ToDictionary(seed => seed.Slug, StringComparer.OrdinalIgnoreCase);

        var existingCategories = await context.Categories.ToListAsync();
        foreach (var category in existingCategories.Where(c => c.IsActive && !categorySeedMap.ContainsKey(c.Slug)))
        {
            category.Deactivate();
        }

        foreach (var seed in categorySeeds)
        {
            var existing = existingCategories.FirstOrDefault(c => c.Slug == seed.Slug);
            if (existing is null)
            {
                await context.Categories.AddAsync(Category.Create(seed.Name, seed.Slug, seed.Description));
                continue;
            }

            existing.Update(seed.Name, seed.Slug, seed.Description, existing.ImageUrl);
            existing.Activate();
        }

        await context.SaveChangesAsync();

        var targetCategories = await context.Categories
            .Where(c => categorySeedMap.Keys.Contains(c.Slug))
            .ToListAsync();
        var categoryIdsBySlug = targetCategories.ToDictionary(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var existingProducts = await context.Products.ToListAsync();
        foreach (var product in existingProducts.Where(p => p.IsActive && !productSeedMap.ContainsKey(p.Slug)))
        {
            product.Deactivate();
        }

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
                    seed.OriginalPrice));
                continue;
            }

            existing.Update(seed.Name, seed.Description, seed.Price, seed.ImageUrl, seed.OriginalPrice ?? seed.Price);
            existing.Activate();
        }

        await context.SaveChangesAsync();
    }

    private static async Task InvalidateCatalogCachesAsync(ICacheService cache)
    {
        await cache.RemoveAsync("categories:all");
        await cache.RemoveAsync("categories:all:v2");
        await cache.RemoveByPrefixAsync("products:list:");
        await cache.RemoveByPrefixAsync("products:id:");
        await cache.RemoveByPrefixAsync("products:slug:");
    }

    private static CategorySeed[] GetCategorySeeds() =>
    [
        new("Burger", "burger", "Burger bò, gà rán và burger đặc biệt cho bữa ăn nhanh gọn."),
        new("Gà Rán", "ga-ran", "Gà rán giòn, combo nhóm và món ăn kèm đang hot."),
        new("Pizza", "pizza", "Pizza đế mỏng, đế dày và pizza chia sẻ cho nhiều người."),
        new("Mì Ý & Nui", "mi-y-nui", "Mì Ý sốt bò, nui đặc biệt và món no bụng giao nhanh."),
        new("Đồ Uống & Tráng Miệng", "do-uong-trang-mieng", "Trà sữa, nước ngọt, kem và món tráng miệng mát lạnh."),
    ];

    private static ProductSeed[] GetProductSeeds()
        =>
        [
            new("Burger Bò Phô Mai Kép", "Hai lớp bò nướng, hai lớp phô mai cheddar, sốt burger đặc trưng và dưa chua giòn.", 89000m, 109000m, "burger", "burger-bo-phomai-kep", LocalCatalogImage("burger-bo-phomai-kep.svg")),
            new("Burger Gà Sốt Hàn", "Gà giòn sốt Hàn cay nhẹ, bắp cải tím và sốt mayonnaise vị rong biển.", 75000m, 92000m, "burger", "burger-ga-sot-han", LocalCatalogImage("burger-ga-sot-han.svg")),
            new("Burger Tôm Giòn", "Tôm tẩm bột giòn rụm, rau sống, sốt thousand island và bánh brioche mềm.", 79000m, 95000m, "burger", "burger-tom-gion", LocalCatalogImage("burger-tom-gion.svg")),
            new("Combo Burger Duo", "1 burger bò phô mai kép, 1 khoai tây lớn và 2 ly nước ngăn đá.", 159000m, 189000m, "burger", "combo-burger-duo", LocalCatalogImage("combo-burger-duo.svg")),
            new("Gà Rán Truyền Thống 3 Miếng", "Ba miếng gà giòn rụm, ướp muối tiêu tỏi và bột chiên siêu giòn.", 99000m, 119000m, "ga-ran", "ga-ran-truyen-thong-3-mieng", LocalCatalogImage("ga-ran-truyen-thong-3-mieng.svg")),
            new("Combo Gà Gia Đình", "6 miếng gà rán, 2 khoai tây vừa, 1 salad bắp cải và 4 ly nước.", 269000m, 315000m, "ga-ran", "combo-ga-gia-dinh", LocalCatalogImage("combo-ga-gia-dinh.svg")),
            new("Cánh Gà BBQ", "Cánh gà sốt BBQ ngọt khói, phủ mè rang và lá parsley.", 69000m, 82000m, "ga-ran", "canh-ga-bbq", LocalCatalogImage("canh-ga-bbq.svg")),
            new("Khoai Tây Lắc Phô Mai", "Khoai tây chiên giòn, phủ bột phô mai cheddar và rau mùi sấy.", 39000m, null, "ga-ran", "khoai-tay-lac-pho-mai", LocalCatalogImage("khoai-tay-lac-pho-mai.svg")),
            new("Pizza Hải Sản Sốt Pesto", "Tôm, mực, pesto basil, olive đen và mozzarella nướng nóng.", 179000m, 219000m, "pizza", "pizza-hai-san-sot-pesto", LocalCatalogImage("pizza-hai-san-sot-pesto.svg")),
            new("Pizza Bò Bằm Xúc Xích", "Bò bằm xào hành, xúc xích hun khói và phô mai phủ đầy mặt.", 169000m, 205000m, "pizza", "pizza-bo-bam-xuc-xich", LocalCatalogImage("pizza-bo-bam-xuc-xich.svg")),
            new("Pizza 4 Loại Phô Mai", "Mozzarella, cheddar, parmesan, blue cheese và mật ong cay nhẹ.", 189000m, 229000m, "pizza", "pizza-4-loai-pho-mai", LocalCatalogImage("pizza-4-loai-pho-mai.svg")),
            new("Combo Pizza Tiệc Nhỏ", "1 pizza size L, 4 cánh gà BBQ và 1 chai cola 1.5L.", 259000m, 309000m, "pizza", "combo-pizza-tiec-nho", LocalCatalogImage("combo-pizza-tiec-nho.svg")),
            new("Mì Ý Bò Bằm Sốt Cà Chua", "Mì Ý al dente, sốt cà chua nấu chậm, bò bằm và phô mai parmesan.", 85000m, 99000m, "mi-y-nui", "mi-y-bo-bam-sot-ca-chua", LocalCatalogImage("mi-y-bo-bam-sot-ca-chua.svg")),
            new("Mì Ý Sốt Kem Nấm", "Sốt kem béo nhẹ, nấm mỡ, thịt xông khói và rau mùi tây.", 89000m, 105000m, "mi-y-nui", "mi-y-sot-kem-nam", LocalCatalogImage("mi-y-sot-kem-nam.svg")),
            new("Nui Bò Lúc Lắc", "Nui xào bò lúc lắc, ớt chuông, hành tây và tiêu đen xay mới.", 92000m, null, "mi-y-nui", "nui-bo-luc-lac", LocalCatalogImage("nui-bo-luc-lac.svg")),
            new("Combo Mì Trưa Văn Phòng", "1 mì Ý bò bằm, 1 soup ngô và 1 trà đào mát lạnh.", 119000m, 145000m, "mi-y-nui", "combo-mi-trua-van-phong", LocalCatalogImage("combo-mi-trua-van-phong.svg")),
            new("Trà Sữa Trân Châu Đường Đen", "Trà sữa Đài Loan đậm vị, trân châu đường đen nấu mới mỗi ngày.", 42000m, 49000m, "do-uong-trang-mieng", "tra-sua-tran-chau-duong-den", LocalCatalogImage("tra-sua-tran-chau-duong-den.svg")),
            new("Soda Chanh Dây", "Soda mát lạnh với syrup chanh dây, hạt chia và lát cam tươi.", 35000m, null, "do-uong-trang-mieng", "soda-chanh-day", LocalCatalogImage("soda-chanh-day.svg")),
            new("Cola Không Đường", "Lon cola lạnh giao cùng đá viên, hợp với burger và gà rán.", 19000m, null, "do-uong-trang-mieng", "cola-khong-duong", LocalCatalogImage("cola-khong-duong.svg")),
            new("Kem Sundae Sô Cô La", "Kem vanilla mềm mịn, sốt sô cô la đậm và hạt hạnh nhân rang.", 29000m, null, "do-uong-trang-mieng", "kem-sundae-socola", LocalCatalogImage("kem-sundae-socola.svg")),
            new("Tiramisu Hộp", "Tiramisu mềm, vị cà phê nhẹ, lớp kem mascarpone phủ cocoa.", 45000m, 52000m, "do-uong-trang-mieng", "tiramisu-hop", LocalCatalogImage("tiramisu-hop.svg")),
            new("Combo Ăn Đêm", "1 burger gà sốt Hàn, 1 cánh gà BBQ và 1 soda chanh dây.", 129000m, 154000m, "burger", "combo-an-dem", LocalCatalogImage("combo-an-dem.svg")),
            new("Combo Gà Cay Nhẹ", "2 miếng gà rán, 1 khoai tây lắc phô mai và 1 trà sữa size M.", 115000m, 139000m, "ga-ran", "combo-ga-cay-nhe", LocalCatalogImage("combo-ga-cay-nhe.svg")),
            new("Pizza Pepperoni Mini", "Pizza size mini đế mỏng, pepperoni hun khói và phô mai mozzarella.", 99000m, 119000m, "pizza", "pizza-pepperoni-mini", LocalCatalogImage("pizza-pepperoni-mini.svg")),
        ];

    private static string LocalCatalogImage(string fileName) => $"local:{fileName}";

    private static async Task SeedFaqDocumentsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.FaqDocuments.AnyAsync()) return;

        var faqs = new List<FaqDocument>
        {
            FaqDocument.Create("shipping",  "Thời gian giao hàng bao lâu?",
                "FastFood giao hàng trong vòng 30-45 phút trong khu vực nội thành. Đơn hàng ngoại thành có thể mất 60-90 phút."),
            FaqDocument.Create("shipping",  "Phí giao hàng là bao nhiêu?",
                "Miễn phí giao hàng cho đơn từ 150.000đ. Đơn dưới 150.000đ phí 15.000đ."),
            FaqDocument.Create("shipping",  "FastFood giao hàng những khu vực nào?",
                "Hiện tại FastFood giao hàng tại TP.HCM và Hà Nội. Chúng tôi đang mở rộng thêm tỉnh thành khác."),
            FaqDocument.Create("returns",   "Chính sách đổi trả như thế nào?",
                "FastFood hỗ trợ đổi/hoàn tiền trong vòng 2 giờ kể từ khi nhận hàng nếu sản phẩm bị lỗi hoặc sai đơn. Liên hệ hotline 1900-xxxx."),
            FaqDocument.Create("payment",   "Những phương thức thanh toán nào được hỗ trợ?",
                "FastFood hỗ trợ: tiền mặt khi nhận hàng (COD), chuyển khoản ngân hàng, ví điện tử MoMo, ZaloPay, thẻ Visa/Mastercard."),
            FaqDocument.Create("payment",   "Làm thế nào để sử dụng mã giảm giá?",
                "Nhập mã giảm giá vào ô 'Mã khuyến mãi' ở trang thanh toán trước khi xác nhận đơn hàng."),
            FaqDocument.Create("general",   "Làm thế nào để theo dõi đơn hàng?",
                "Đăng nhập vào tài khoản → Đơn hàng của tôi → Chọn đơn cần theo dõi. Bạn cũng nhận được thông báo qua email khi trạng thái đơn thay đổi."),
            FaqDocument.Create("general",   "Tôi có thể hủy đơn hàng không?",
                "Bạn có thể hủy đơn trong vòng 5 phút sau khi đặt, miễn là đơn chưa được xác nhận. Vào Đơn hàng của tôi → Chọn đơn → Hủy đơn."),
            FaqDocument.Create("general",   "Cách tạo tài khoản mới?",
                "Nhấn Đăng ký ở góc phải → Nhập email, mật khẩu, họ tên → Xác nhận email → Đăng nhập."),
            FaqDocument.Create("general",   "Giờ làm việc của FastFood?",
                "FastFood hoạt động từ 8:00 - 22:00 hàng ngày, kể cả cuối tuần và ngày lễ."),
        };

        context.FaqDocuments.AddRange(faqs);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} FAQ documents.", faqs.Count);
    }
}
