using MediatR;
using SmartShop.Application.Common.Interfaces;

namespace SmartShop.Application.Features.AI.Commands.GenerateDescription;

public class GenerateDescriptionCommandHandler(
    ISemanticKernelService semanticKernel
) : IRequestHandler<GenerateDescriptionCommand, string>
{
    public async Task<string> Handle(GenerateDescriptionCommand request, CancellationToken cancellationToken)
    {
        return await semanticKernel.GenerateProductDescriptionAsync(
            request.ProductName, request.CategoryName, cancellationToken);
    }
}
