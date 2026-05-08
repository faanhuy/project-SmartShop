using FluentValidation;

namespace SmartShop.Application.Features.Stores.Commands.UpdateStore;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id chi nhánh không hợp lệ.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chi nhánh không được để trống.")
            .MaximumLength(100).WithMessage("Tên chi nhánh tối đa 100 ký tự.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Địa chỉ không được để trống.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.");
    }
}
