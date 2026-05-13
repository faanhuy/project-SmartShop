namespace SmartShop.Application.Features.PriceCampaigns;

public record PriceCampaignItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid? SizeId,
    string? SizeLabel,
    int RuleType,
    decimal DiscountValue
);

public record PriceCampaignDto(
    Guid Id,
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    bool AppliesToAll,
    bool IsActive,
    List<Guid> StoreIds,
    List<PriceCampaignItemDto> Items
);

public record PriceCampaignSummaryDto(
    Guid Id,
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    bool AppliesToAll,
    bool IsActive,
    int ItemCount
);
