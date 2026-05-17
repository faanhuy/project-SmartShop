using SmartShop.Domain.Common;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class ReturnRequest : BaseAuditableEntity
{
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public ReturnReason Reason { get; private set; }
    public string? Description { get; private set; }
    public string? EvidenceImageUrl { get; private set; }
    public ReturnStatus Status { get; private set; }
    public string? AdminNote { get; private set; }
    public decimal RefundAmount { get; private set; }

    public Order Order { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ReturnRequest() { }

    public static ReturnRequest Create(
        Guid orderId,
        Guid userId,
        ReturnReason reason,
        string? description,
        string? evidenceImageUrl,
        decimal refundAmount)
    {
        return new ReturnRequest
        {
            OrderId = orderId,
            UserId = userId,
            Reason = reason,
            Description = description,
            EvidenceImageUrl = evidenceImageUrl,
            Status = ReturnStatus.Pending,
            RefundAmount = refundAmount
        };
    }

    public void Approve(string? adminNote)
    {
        Status = ReturnStatus.Approved;
        AdminNote = adminNote;
    }

    public void Reject(string adminNote)
    {
        Status = ReturnStatus.Rejected;
        AdminNote = adminNote;
    }

    public void Resubmit(ReturnReason reason, string? description, string? evidenceImageUrl, decimal refundAmount)
    {
        Reason = reason;
        Description = description;
        EvidenceImageUrl = evidenceImageUrl;
        RefundAmount = refundAmount;
        Status = ReturnStatus.Pending;
        AdminNote = null;
    }
}
