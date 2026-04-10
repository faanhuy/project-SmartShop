using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            // Seed AppSettings nếu chưa có
            if (!await context.AppSettings.AnyAsync())
            {
                var defaults = new[]
                {
                    AppSetting.Create("AI:Search:MinScore",          "0.3",       "number",  "Điểm tối thiểu để hiển thị kết quả tìm kiếm AI (0.0 - 1.0)"),
                    AppSetting.Create("AI:Search:TopN",              "8",         "number",  "Số kết quả tối đa trả về khi tìm kiếm AI"),
                    AppSetting.Create("AI:Recommendations:Count",    "5",         "number",  "Số sản phẩm gợi ý tối đa"),
                    AppSetting.Create("AI:Recommendations:MinScore", "0.4",       "number",  "Điểm tối thiểu để hiển thị gợi ý sản phẩm"),
                    AppSetting.Create("Site:Name",                   "SmartShop", "text",    "Tên hiển thị của website"),
                };
                await context.AppSettings.AddRangeAsync(defaults);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} AppSettings.", defaults.Length);
            }

            if (await context.Categories.AnyAsync()) return; // Already seeded

            logger.LogInformation("Seeding database...");

            // --- Categories ---
            var categories = new[]
            {
                Category.Create("Điện thoại & Máy tính bảng", "dien-thoai-may-tinh-bang", "Smartphone, tablet và phụ kiện"),
                Category.Create("Laptop & Máy tính", "laptop-may-tinh", "Laptop, PC, linh kiện máy tính"),
                Category.Create("Âm thanh & Tai nghe", "am-thanh-tai-nghe", "Loa, tai nghe, thiết bị âm thanh"),
                Category.Create("Thời trang", "thoi-trang", "Quần áo, giày dép, phụ kiện thời trang"),
                Category.Create("Nhà cửa & Đời sống", "nha-cua-doi-song", "Đồ gia dụng, nội thất, trang trí nhà"),
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            var phone = categories[0];
            var laptop = categories[1];
            var audio = categories[2];
            var fashion = categories[3];
            var home = categories[4];

            // --- Products ---
            var products = new[]
            {
                // Điện thoại
                Product.Create("iPhone 15 Pro Max 256GB", "Điện thoại Apple iPhone 15 Pro Max với chip A17 Pro, camera 48MP, màn hình 6.7 inch ProMotion 120Hz", 34990000m, 20, phone.Id, "iphone-15-pro-max-256gb"),
                Product.Create("Samsung Galaxy S24 Ultra", "Samsung Galaxy S24 Ultra với AI Galaxy, bút S Pen tích hợp, camera 200MP, RAM 12GB", 31990000m, 15, phone.Id, "samsung-galaxy-s24-ultra"),
                Product.Create("Xiaomi 14 Pro", "Xiaomi 14 Pro với Snapdragon 8 Gen 3, camera Leica 50MP, sạc nhanh 120W", 22990000m, 25, phone.Id, "xiaomi-14-pro"),
                Product.Create("OPPO Find X7 Ultra", "OPPO Find X7 Ultra camera Hasselblad kép, chip Dimensity 9300, sạc 100W", 26990000m, 10, phone.Id, "oppo-find-x7-ultra"),
                Product.Create("Vivo X100 Pro", "Vivo X100 Pro camera ZEISS, chip Dimensity 9300, pin 5400mAh", 21990000m, 18, phone.Id, "vivo-x100-pro"),
                Product.Create("Google Pixel 8 Pro", "Google Pixel 8 Pro với chip Tensor G3, AI tích hợp sâu, camera 50MP", 23990000m, 12, phone.Id, "google-pixel-8-pro"),
                Product.Create("iPhone 15 128GB", "iPhone 15 chip A16 Bionic, camera 48MP, Dynamic Island, USB-C", 22990000m, 30, phone.Id, "iphone-15-128gb"),
                Product.Create("Samsung Galaxy A55", "Samsung Galaxy A55 5G, chip Exynos 1480, màn hình AMOLED 6.6 inch", 9990000m, 40, phone.Id, "samsung-galaxy-a55"),
                Product.Create("Redmi Note 13 Pro+", "Redmi Note 13 Pro+ camera 200MP, sạc 120W, màn hình AMOLED 120Hz", 8490000m, 50, phone.Id, "redmi-note-13-pro-plus"),
                Product.Create("realme GT 6", "realme GT 6 Snapdragon 8s Gen 3, sạc siêu nhanh 120W, màn hình 6.78 inch", 12990000m, 20, phone.Id, "realme-gt-6"),

                // Laptop
                Product.Create("MacBook Pro 14 M3 Pro", "MacBook Pro 14 inch chip M3 Pro, 18GB RAM, 512GB SSD, màn hình Liquid Retina XDR", 52990000m, 10, laptop.Id, "macbook-pro-14-m3-pro"),
                Product.Create("Dell XPS 15 9530", "Dell XPS 15 Intel Core i7-13700H, RTX 4060, 16GB RAM, 512GB SSD, OLED 3.5K", 46990000m, 8, laptop.Id, "dell-xps-15-9530"),
                Product.Create("ASUS ROG Zephyrus G14", "ASUS ROG Zephyrus G14 Ryzen 9 8945HS, RTX 4070, 32GB RAM, màn hình 2.8K 120Hz", 42990000m, 12, laptop.Id, "asus-rog-zephyrus-g14"),
                Product.Create("Lenovo ThinkPad X1 Carbon Gen 12", "ThinkPad X1 Carbon Intel Ultra 7, 32GB RAM, 1TB SSD, màn hình 2.8K OLED", 49990000m, 6, laptop.Id, "lenovo-thinkpad-x1-carbon-gen-12"),
                Product.Create("HP Spectre x360 14", "HP Spectre x360 14 2-in-1 Intel Ultra 5, 16GB RAM, 512GB SSD, OLED touch", 38990000m, 9, laptop.Id, "hp-spectre-x360-14"),
                Product.Create("MacBook Air 13 M3", "MacBook Air 13 inch chip M3, 8GB RAM, 256GB SSD, màn hình Liquid Retina, siêu nhẹ 1.24kg", 28990000m, 20, laptop.Id, "macbook-air-13-m3"),
                Product.Create("Acer Aspire 5 A515", "Acer Aspire 5 Intel Core i5-1335U, 8GB RAM, 512GB SSD, màn hình FHD IPS", 14990000m, 25, laptop.Id, "acer-aspire-5-a515"),
                Product.Create("Lenovo IdeaPad 5 Pro", "Lenovo IdeaPad 5 Pro AMD Ryzen 7, 16GB RAM, 512GB SSD, màn hình 2.8K OLED", 22990000m, 15, laptop.Id, "lenovo-ideapad-5-pro"),
                Product.Create("MSI Katana 15", "MSI Katana 15 Gaming Intel Core i7, RTX 4060, 16GB RAM, 144Hz FHD", 26990000m, 11, laptop.Id, "msi-katana-15"),
                Product.Create("ASUS VivoBook 15 OLED", "ASUS VivoBook 15 OLED Intel Core i5, 16GB RAM, 512GB SSD, màn hình OLED FHD", 19990000m, 22, laptop.Id, "asus-vivobook-15-oled"),

                // Âm thanh
                Product.Create("Sony WH-1000XM5", "Tai nghe Sony WH-1000XM5 chống ồn hàng đầu, 30 giờ pin, Multipoint connection", 8490000m, 30, audio.Id, "sony-wh-1000xm5"),
                Product.Create("Apple AirPods Pro 2", "AirPods Pro thế hệ 2 với H2 chip, chống ồn ANC, Lossless Audio, tích hợp Vision Pro", 6490000m, 35, audio.Id, "apple-airpods-pro-2"),
                Product.Create("Bose QuietComfort Ultra", "Bose QuietComfort Ultra Headphones với Immersive Audio, chống ồn thế giới thực", 9990000m, 15, audio.Id, "bose-quietcomfort-ultra"),
                Product.Create("JBL Flip 6", "Loa Bluetooth JBL Flip 6 chống nước IP67, 12 giờ pin, âm thanh PartyBoost", 2490000m, 50, audio.Id, "jbl-flip-6"),
                Product.Create("Sony WF-1000XM5", "Tai nghe true wireless Sony WF-1000XM5, chống ồn tốt nhất, 8 giờ pin", 5990000m, 25, audio.Id, "sony-wf-1000xm5"),
                Product.Create("Samsung Galaxy Buds3 Pro", "Galaxy Buds3 Pro ANC thông minh, chất lượng âm thanh Hi-Fi, thiết kế mở", 4490000m, 20, audio.Id, "samsung-galaxy-buds3-pro"),
                Product.Create("Harman Kardon Onyx Studio 8", "Loa Bluetooth Harman Kardon Onyx Studio 8, thiết kế sang trọng, 8 giờ pin", 5990000m, 18, audio.Id, "harman-kardon-onyx-studio-8"),
                Product.Create("Xiaomi Buds 5 Pro", "Xiaomi Buds 5 Pro ANC 52dB, LDAC, 10 giờ pin, driver 10.5mm", 2290000m, 30, audio.Id, "xiaomi-buds-5-pro"),
                Product.Create("Sennheiser Momentum 4 Wireless", "Sennheiser Momentum 4 60 giờ pin, chống ồn adaptable, âm thanh tự nhiên", 8490000m, 12, audio.Id, "sennheiser-momentum-4-wireless"),
                Product.Create("Bang & Olufsen Beosound A1", "Loa B&O Beosound A1 Gen 2, thiết kế Scandinavian, chống nước IP67, 18 giờ", 5990000m, 10, audio.Id, "bang-olufsen-beosound-a1"),

                // Thời trang
                Product.Create("Áo Polo Nam Ralph Lauren", "Áo Polo Ralph Lauren chất liệu cotton Pima cao cấp, logo thêu nổi bật, form slimfit", 1890000m, 100, fashion.Id, "ao-polo-nam-ralph-lauren"),
                Product.Create("Giày Nike Air Max 270", "Nike Air Max 270 đế khí lớn nhất trong lịch sử Air Max, đệm êm ái vượt trội", 3490000m, 60, fashion.Id, "giay-nike-air-max-270"),
                Product.Create("Giày Adidas Ultraboost 22", "Adidas Ultraboost 22 đế Boost trả lực cao, upper Primeknit+, siêu nhẹ", 4290000m, 45, fashion.Id, "giay-adidas-ultraboost-22"),
                Product.Create("Túi Xách Nữ Coach", "Túi xách Coach Tabby Shoulder Bag da bò nguyên chất, khóa C logo, màu beige", 9990000m, 20, fashion.Id, "tui-xach-nu-coach"),
                Product.Create("Áo Khoác Uniqlo Ultra Light Down", "Áo khoác lông vũ Uniqlo Ultra Light Down, siêu nhẹ, gấp gọn, ấm áp mùa đông", 1490000m, 80, fashion.Id, "ao-khoac-uniqlo-ultra-light-down"),
                Product.Create("Quần Jeans Levi's 511", "Quần jeans Levi's 511 Slim Fit vải denim 100% cotton, form chuẩn, bền màu", 1290000m, 70, fashion.Id, "quan-jeans-levis-511"),
                Product.Create("Đồng hồ Casio G-Shock GA-2100", "Casio G-Shock GA-2100 chống va đập, chống nước 200m, pin 3 năm, mặt số 8 cạnh", 2490000m, 40, fashion.Id, "dong-ho-casio-g-shock-ga-2100"),
                Product.Create("Kính mắt Ray-Ban Wayfarer", "Ray-Ban Original Wayfarer Classic tròng kính CR-39, gọng acetate, UV400", 3990000m, 25, fashion.Id, "kinh-mat-ray-ban-wayfarer"),
                Product.Create("Balo Laptop Samsonite 15.6\"", "Balo Samsonite Openroad 15.6 inch chất liệu cao cấp, nhiều ngăn tiện dụng", 2290000m, 35, fashion.Id, "balo-laptop-samsonite-156"),
                Product.Create("Mũ Bucket Vải Kaki", "Mũ bucket unisex vải kaki cao cấp, nhiều màu sắc, phù hợp mọi khuôn mặt", 290000m, 150, fashion.Id, "mu-bucket-vai-kaki"),

                // Nhà cửa
                Product.Create("Nồi Chiên Không Khí Philips HD9200", "Nồi chiên không khí Philips 4L, công nghệ Rapid Air, ít dầu mỡ 90%", 2890000m, 40, home.Id, "noi-chien-khong-khi-philips-hd9200"),
                Product.Create("Robot Hút Bụi Xiaomi S10+", "Robot hút bụi Xiaomi Robot Vacuum S10+ lực hút 4000Pa, tự động giặt giẻ lau", 8490000m, 15, home.Id, "robot-hut-bui-xiaomi-s10-plus"),
                Product.Create("Đèn Bàn LED Xiaomi", "Đèn bàn học Xiaomi Mi LED Desk Lamp Pro, điều chỉnh màu sắc và độ sáng qua app", 890000m, 60, home.Id, "den-ban-led-xiaomi"),
                Product.Create("Máy Lọc Không Khí Dyson TP07", "Dyson Purifier Cool Formaldehyde TP09 lọc sạch formaldehyde, HEPA+Carbon, quạt không cánh", 19990000m, 8, home.Id, "may-loc-khong-khi-dyson-tp09"),
                Product.Create("Bộ Chăn Ga Gối Cotton Lụa", "Bộ chăn ga gối 100% cotton lụa cao cấp, mềm mịn, thoáng khí, set 4 món", 1490000m, 50, home.Id, "bo-chan-ga-goi-cotton-lua"),
                Product.Create("Máy Pha Cà Phê Delonghi Dedica", "Máy pha espresso De'Longhi Dedica EC685 15 bar, 1.1L, hệ thống tạo bọt sữa", 6490000m, 12, home.Id, "may-pha-ca-phe-delonghi-dedica"),
                Product.Create("Bàn Làm Việc Gỗ Tự Nhiên", "Bàn làm việc gỗ cao su tự nhiên 120x60cm, chân sắt sơn tĩnh điện, tải trọng 50kg", 1890000m, 20, home.Id, "ban-lam-viec-go-tu-nhien"),
                Product.Create("Ghế Gaming DXRacer Formula", "Ghế gaming DXRacer Formula F08 da PU cao cấp, lưng ngả 135°, gối đầu & thắt lưng", 5990000m, 18, home.Id, "ghe-gaming-dxracer-formula"),
                Product.Create("Nồi Cơm Điện Tử Tiger JKT-S18V", "Nồi cơm điện tử Tiger JKT-S18V 1.8L, nấu áp suất, giữ ấm 24h, nội địa Nhật", 3890000m, 25, home.Id, "noi-com-dien-tu-tiger-jkt-s18v"),
                Product.Create("Giá Đỡ Điện Thoại Ugreen", "Giá đỡ điện thoại Ugreen điều chỉnh 360°, tương thích mọi thiết bị, chất liệu nhôm", 390000m, 100, home.Id, "gia-do-dien-thoai-ugreen"),
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeding completed: {CategoryCount} categories, {ProductCount} products",
                categories.Length, products.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding the database");
            throw;
        }
    }
}
