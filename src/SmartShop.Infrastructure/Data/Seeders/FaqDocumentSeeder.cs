using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShop.Domain.Entities;

namespace SmartShop.Infrastructure.Data.Seeders;

internal sealed class FaqDocumentSeeder(
    ApplicationDbContext context,
    ILogger<FaqDocumentSeeder> logger) : IDataSeeder
{
    public int Order => 3;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.FaqDocuments.AnyAsync(cancellationToken))
            return;

        var faqs = new List<FaqDocument>
        {
            FaqDocument.Create("shipping", "Thời gian giao hàng bao lâu?",
                "FastFood giao hàng trong vòng 30-45 phút trong khu vực nội thành. Đơn hàng ngoại thành có thể mất 60-90 phút."),
            FaqDocument.Create("shipping", "Phí giao hàng là bao nhiêu?",
                "Miễn phí giao hàng cho đơn từ 150.000đ. Đơn dưới 150.000đ phí 15.000đ."),
            FaqDocument.Create("shipping", "FastFood giao hàng những khu vực nào?",
                "Hiện tại FastFood giao hàng tại TP.HCM và Hà Nội. Chúng tôi đang mở rộng thêm tỉnh thành khác."),
            FaqDocument.Create("returns", "Chính sách đổi trả như thế nào?",
                "FastFood hỗ trợ đổi/hoàn tiền trong vòng 2 giờ kể từ khi nhận hàng nếu sản phẩm bị lỗi hoặc sai đơn. Liên hệ hotline 1900-xxxx."),
            FaqDocument.Create("payment", "Những phương thức thanh toán nào được hỗ trợ?",
                "FastFood hỗ trợ: tiền mặt khi nhận hàng (COD), chuyển khoản ngân hàng, ví điện tử MoMo, ZaloPay, thẻ Visa/Mastercard."),
            FaqDocument.Create("payment", "Làm thế nào để sử dụng mã giảm giá?",
                "Nhập mã giảm giá vào ô 'Mã khuyến mãi' ở trang thanh toán trước khi xác nhận đơn hàng."),
            FaqDocument.Create("general", "Làm thế nào để theo dõi đơn hàng?",
                "Đăng nhập vào tài khoản → Đơn hàng của tôi → Chọn đơn cần theo dõi. Bạn cũng nhận được thông báo qua email khi trạng thái đơn thay đổi."),
            FaqDocument.Create("general", "Tôi có thể hủy đơn hàng không?",
                "Bạn có thể hủy đơn trong vòng 5 phút sau khi đặt, miễn là đơn chưa được xác nhận. Vào Đơn hàng của tôi → Chọn đơn → Hủy đơn."),
            FaqDocument.Create("general", "Cách tạo tài khoản mới?",
                "Nhấn Đăng ký ở góc phải → Nhập email, mật khẩu, họ tên → Xác nhận email → Đăng nhập."),
            FaqDocument.Create("general", "Giờ làm việc của FastFood?",
                "FastFood hoạt động từ 8:00 - 22:00 hàng ngày, kể cả cuối tuần và ngày lễ."),
        };

        context.FaqDocuments.AddRange(faqs);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} FAQ documents.", faqs.Count);
    }
}
