using FluentValidation;

namespace SmartShop.Application.Features.AI.Queries.SemanticSearch;

public class SemanticSearchQueryValidator : AbstractValidator<SemanticSearchQuery>
{
    public SemanticSearchQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Search query cannot be empty.")
            .MinimumLength(2).WithMessage("Query must be at least 2 characters.")
            .MaximumLength(500).WithMessage("Query must not exceed 500 characters.");

        RuleFor(x => x.TopN)
            .InclusiveBetween(1, 50).WithMessage("TopN must be between 1 and 50.");
    }
}
