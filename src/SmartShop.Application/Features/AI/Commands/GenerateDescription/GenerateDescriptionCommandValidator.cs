using FluentValidation;

namespace SmartShop.Application.Features.AI.Commands.GenerateDescription;

public class GenerateDescriptionCommandValidator : AbstractValidator<GenerateDescriptionCommand>
{
    public GenerateDescriptionCommandValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CategoryName).NotEmpty().MaximumLength(100);
    }
}
