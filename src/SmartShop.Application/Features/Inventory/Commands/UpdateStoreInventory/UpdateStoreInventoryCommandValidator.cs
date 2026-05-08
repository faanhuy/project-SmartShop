using FluentValidation;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateStoreInventory;

public class UpdateStoreInventoryCommandValidator : AbstractValidator<UpdateStoreInventoryCommand>
{
    public UpdateStoreInventoryCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("StoreId không hợp lệ.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId không hợp lệ.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng tồn kho không được âm.");
    }
}
