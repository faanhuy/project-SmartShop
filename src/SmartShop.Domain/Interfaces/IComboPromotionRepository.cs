using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IComboPromotionRepository
{
    Task<ComboPromotion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ComboPromotion>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Lấy combo đang active tại thời điểm 'at', áp dụng cho storeId.</summary>
    Task<IReadOnlyList<ComboPromotion>> GetActiveForStoreAsync(Guid storeId, DateTime at, CancellationToken ct = default);

    Task AddAsync(ComboPromotion combo, CancellationToken ct = default);
    void Update(ComboPromotion combo);
    void Remove(ComboPromotion combo);
}
