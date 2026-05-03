using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Infrastructure.Payment;

public class VNPayGateway(IConfiguration configuration) : IPaymentGateway
{
    private readonly string _tmnCode = configuration["VNPay:TmnCode"]!;
    private readonly string _hashSecret = configuration["VNPay:HashSecret"]!;
    private readonly string _payUrl = configuration["VNPay:PayUrl"]!;
    private const string Version = "2.1.0";

    public string CreatePaymentUrl(CreatePaymentRequest request)
    {
        var now = DateTime.UtcNow.AddHours(7); // VNPay requires Vietnam time (UTC+7)
        var expireDate = now.AddMinutes(15);

        var vnpParams = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _tmnCode,
            ["vnp_Amount"] = (request.Amount * 100).ToString(),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = request.OrderDescription,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = request.ReturnUrl,
            ["vnp_TxnRef"] = request.OrderId,
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss")
        };

        var queryString = BuildQueryString(vnpParams);        // dùng cho cả URL lẫn hash
        var secureHash = ComputeHmacSha512(_hashSecret, queryString);

        return $"{_payUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    public VNPayCallbackResult ProcessCallback(IDictionary<string, string> queryParams)
    {
        var receivedHash = queryParams.TryGetValue("vnp_SecureHash", out var h) ? h : string.Empty;
        var responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var rc) ? rc : string.Empty;
        var transactionId = queryParams.TryGetValue("vnp_TransactionNo", out var tid) ? tid : string.Empty;
        var orderId = queryParams.TryGetValue("vnp_TxnRef", out var oid) ? oid : string.Empty;

        var vnpParams = new SortedDictionary<string, string>();
        foreach (var (key, value) in queryParams)
        {
            if (!string.IsNullOrEmpty(key)
                && key != "vnp_SecureHash"
                && key != "vnp_SecureHashType")
            {
                vnpParams[key] = value;
            }
        }

        var hashData = BuildQueryString(vnpParams);
        var computedHash = ComputeHmacSha512(_hashSecret, hashData);

        var isValidHash = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
        var isSuccess = isValidHash && responseCode == "00";

        return new VNPayCallbackResult(
            IsSuccess: isSuccess,
            TransactionId: transactionId,
            OrderId: orderId,
            ResponseCode: responseCode);
    }

    // Dùng cho cả URL query string lẫn HMAC hash data — WebUtility.UrlEncode (VNPay spec)
    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in parameters)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(WebUtility.UrlEncode(key));
                sb.Append('=');
                sb.Append(WebUtility.UrlEncode(value));
            }
        }
        return sb.ToString();
    }

    private static string ComputeHmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
