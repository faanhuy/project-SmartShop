using MediatR;

namespace SmartShop.Application.Features.Returns.Commands.RejectReturn;

public record RejectReturnCommand(
    Guid ReturnRequestId,
    string AdminNote) : IRequest<ReturnRequestDto>;
