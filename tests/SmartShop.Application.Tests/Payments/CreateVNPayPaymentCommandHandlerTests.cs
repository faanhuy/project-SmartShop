using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Payments.Commands.CreateVNPayPayment;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Payments;

public class CreateVNPayPaymentCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();

    private CreateVNPayPaymentCommandHandler CreateHandler() =>
        new(_orderRepo.Object, _paymentGateway.Object);

    private static Order CreateVNPayOrder(Guid userId)
    {
        var order = Order.Create(userId, "123 Đường Lê Lợi");
        order.SetPaymentMethod(PaymentMethod.VNPay);
        return order;
    }

    [Fact]
    public async Task CreateVNPayPayment_ValidOrder_ReturnsPaymentUrl()
    {
        var userId = Guid.NewGuid();
        var order = CreateVNPayOrder(userId);
        var expectedUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=100000";

        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);
        _paymentGateway.Setup(g => g.CreatePaymentUrl(It.IsAny<CreatePaymentRequest>()))
                       .Returns(expectedUrl);

        var result = await CreateHandler().Handle(
            new CreateVNPayPaymentCommand(order.Id, userId.ToString(), "https://return.url", "127.0.0.1"),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task CreateVNPayPayment_OrderNotFound_ThrowsNotFoundException()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, default)).ReturnsAsync((Order?)null);

        var act = () => CreateHandler().Handle(
            new CreateVNPayPaymentCommand(orderId, userId, "https://return.url", "127.0.0.1"),
            default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateVNPayPayment_WrongOwner_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var order = CreateVNPayOrder(userId);

        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        var act = () => CreateHandler().Handle(
            new CreateVNPayPaymentCommand(order.Id, otherUserId.ToString(), "https://return.url", "127.0.0.1"),
            default);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task CreateVNPayPayment_NonVNPayOrder_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var order = Order.Create(userId, "123 Đường Lê Lợi");
        // PaymentMethod defaults to COD — no SetPaymentMethod(VNPay)

        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        var act = () => CreateHandler().Handle(
            new CreateVNPayPaymentCommand(order.Id, userId.ToString(), "https://return.url", "127.0.0.1"),
            default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateVNPayPayment_AlreadyPaidOrder_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var order = CreateVNPayOrder(userId);
        order.MarkAsPaid("TXN001");

        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        var act = () => CreateHandler().Handle(
            new CreateVNPayPaymentCommand(order.Id, userId.ToString(), "https://return.url", "127.0.0.1"),
            default);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
