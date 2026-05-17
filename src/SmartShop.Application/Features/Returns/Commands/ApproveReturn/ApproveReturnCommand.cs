using MediatR;

namespace SmartShop.Application.Features.Returns.Commands.ApproveReturn;

public record ApproveReturnCommand(
    Guid ReturnRequestId,
    string? AdminNote) : IRequest<ReturnRequestDto>;
