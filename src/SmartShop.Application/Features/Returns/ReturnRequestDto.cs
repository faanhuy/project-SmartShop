using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Returns;

public record ReturnRequestDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    ReturnReason Reason,
    string? Description,
    string? EvidenceImageUrl,
    ReturnStatus Status,
    string? AdminNote,
    decimal RefundAmount,
    DateTime CreatedAt);

public static class ReturnRequestMapper
{
    public static ReturnRequestDto ToDto(ReturnRequest entity, string orderNumber)
    {
        return new ReturnRequestDto(
            Id: entity.Id,
            OrderId: entity.OrderId,
            OrderNumber: orderNumber,
            Reason: entity.Reason,
            Description: entity.Description,
            EvidenceImageUrl: entity.EvidenceImageUrl,
            Status: entity.Status,
            AdminNote: entity.AdminNote,
            RefundAmount: entity.RefundAmount,
            CreatedAt: entity.CreatedAt);
    }
}
