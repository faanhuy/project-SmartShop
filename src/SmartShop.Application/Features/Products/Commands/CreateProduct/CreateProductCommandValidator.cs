using FluentValidation;

namespace SmartShop.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả không được để trống.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug không được để trống.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug chỉ được chứa chữ thường, số và dấu gạch ngang.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId không được để trống.");
    }
}
