using FluentValidation;

namespace SmartShop.Application.Features.Orders.Commands.PlaceOrder;

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId không hợp lệ.");

        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Vui lòng chọn chi nhánh.");

        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("Vui lòng chọn địa chỉ giao hàng.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("Mã giảm giá tối đa 50 ký tự.")
            .When(x => x.CouponCode is not null);
    }
}
