using MediatR;

namespace SmartShop.Application.Features.AI.Commands.GenerateDescription;

public record GenerateDescriptionCommand(
    string ProductName,
    string CategoryName
) : IRequest<string>;
