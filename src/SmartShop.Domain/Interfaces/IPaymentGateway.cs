namespace SmartShop.Domain.Interfaces;

public interface IPaymentGateway
{
    string CreatePaymentUrl(CreatePaymentRequest request);
    VNPayCallbackResult ProcessCallback(IDictionary<string, string> queryParams);
}

public record CreatePaymentRequest(
    string OrderId,
    long Amount,
    string OrderDescription,
    string ReturnUrl,
    string IpAddress);

public record VNPayCallbackResult(
    bool IsSuccess,
    string TransactionId,
    string OrderId,
    string ResponseCode);
