using MediatR;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Returns.Commands.CreateReturnRequest;

public record CreateReturnRequestCommand(
    Guid OrderId,
    ReturnReason Reason,
    string? Description,
    string? EvidenceImageUrl) : IRequest<ReturnRequestDto>;
