using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Payments.Commands.ProcessVNPayCallback;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Payments;

public class ProcessVNPayCallbackCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ProcessVNPayCallbackCommandHandler CreateHandler() =>
        new(_orderRepo.Object, _paymentGateway.Object, _uow.Object);

    private static ProcessVNPayCallbackCommand AnyCallback() =>
        new(new Dictionary<string, string> { ["vnp_ResponseCode"] = "00" });

    private static Order CreatePendingVNPayOrder(Guid userId)
    {
        var order = Order.Create(userId, "123 Đường Lê Lợi");
        order.SetPaymentMethod(PaymentMethod.VNPay);
        return order;
    }

    [Fact]
    public async Task ProcessCallback_SuccessResponse_MarksOrderPaid()
    {
        var userId = Guid.NewGuid();
        var order = CreatePendingVNPayOrder(userId);
        var callbackResult = new VNPayCallbackResult(true, "TXN_SUCCESS_001", order.Id.ToString(), "00");

        _paymentGateway.Setup(g => g.ProcessCallback(It.IsAny<IDictionary<string, string>>()))
                       .Returns(callbackResult);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(AnyCallback(), default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        order.VnpayTransactionId.Should().Be("TXN_SUCCESS_001");
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessCallback_FailedResponse_MarksOrderFailed()
    {
        var userId = Guid.NewGuid();
        var order = CreatePendingVNPayOrder(userId);
        var callbackResult = new VNPayCallbackResult(false, string.Empty, order.Id.ToString(), "24");

        _paymentGateway.Setup(g => g.ProcessCallback(It.IsAny<IDictionary<string, string>>()))
                       .Returns(callbackResult);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(AnyCallback(), default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessCallback_AlreadyProcessed_Idempotent_DoesNotSave()
    {
        var userId = Guid.NewGuid();
        var order = CreatePendingVNPayOrder(userId);
        order.MarkAsPaid("TXN_EXISTING"); // already Paid
        var callbackResult = new VNPayCallbackResult(true, "TXN_DUPLICATE", order.Id.ToString(), "00");

        _paymentGateway.Setup(g => g.ProcessCallback(It.IsAny<IDictionary<string, string>>()))
                       .Returns(callbackResult);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        var result = await CreateHandler().Handle(AnyCallback(), default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        // SaveChanges should NOT be called — idempotent early return
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ProcessCallback_AlreadyFailed_Idempotent_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var order = CreatePendingVNPayOrder(userId);
        order.MarkPaymentFailed(); // already Failed
        var callbackResult = new VNPayCallbackResult(false, string.Empty, order.Id.ToString(), "99");

        _paymentGateway.Setup(g => g.ProcessCallback(It.IsAny<IDictionary<string, string>>()))
                       .Returns(callbackResult);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        var result = await CreateHandler().Handle(AnyCallback(), default);

        result.Data.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ProcessCallback_InvalidOrderId_ThrowsNotFoundException()
    {
        // Gateway returns a non-GUID order ID (simulates tampered/invalid callback)
        var callbackResult = new VNPayCallbackResult(true, "TXN_INVALID", "not-a-valid-guid", "00");

        _paymentGateway.Setup(g => g.ProcessCallback(It.IsAny<IDictionary<string, string>>()))
                       .Returns(callbackResult);

        var act = () => CreateHandler().Handle(AnyCallback(), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ProcessCallback_OrderNotFound_ThrowsNotFoundException()
    {
        var missingOrderId = Guid.NewGuid();
        var callbackResult = new VNPayCallbackResult(true, "TXN_001", missingOrderId.ToString(), "00");

        _paymentGateway.Setup(g => g.ProcessCallback(It.IsAny<IDictionary<string, string>>()))
                       .Returns(callbackResult);
        _orderRepo.Setup(r => r.GetByIdAsync(missingOrderId, default)).ReturnsAsync((Order?)null);

        var act = () => CreateHandler().Handle(AnyCallback(), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
