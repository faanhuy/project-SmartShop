using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IPriceCampaignRepository
{
    Task<PriceCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PriceCampaign>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(PriceCampaign campaign, CancellationToken ct = default);
    void Update(PriceCampaign campaign);
    void Remove(PriceCampaign campaign);

    /// <summary>
    /// Bulk query: lấy effective price cho list (productId, sizeId?) tại storeId và thời điểm 'at'.
    /// Return: Dictionary keyed by (productId, sizeId) → (ruleType, discountValue).
    /// Priority: campaign có StartsAt lớn hơn thắng khi overlap.
    /// </summary>
    Task<Dictionary<(Guid productId, Guid? sizeId), (int ruleType, decimal discountValue)>>
        GetEffectivePriceItemsAsync(
            Guid storeId,
            IEnumerable<(Guid productId, Guid? sizeId)> keys,
            DateTime at,
            CancellationToken ct = default);

    /// <summary>Xóa items cũ rồi insert items mới — dùng khi update campaign.</summary>
    Task ReplaceItemsAsync(Guid campaignId, IEnumerable<PriceCampaignItem> newItems, CancellationToken ct = default);

    /// <summary>Đồng bộ store IDs trong join table PriceListStores.</summary>
    Task SyncStoresAsync(Guid campaignId, IEnumerable<Guid> storeIds, CancellationToken ct = default);
}
