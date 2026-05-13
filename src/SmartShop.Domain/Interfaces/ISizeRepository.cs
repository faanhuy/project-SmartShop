using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Interfaces;

public interface ISizeRepository
{
    Task<List<Size>> GetByCategoryAsync(SizeType category, CancellationToken ct = default);
    Task<List<Size>> GetAllAsync(CancellationToken ct = default);
    Task<Size?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByLabelAndCategoryAsync(string label, SizeType category, CancellationToken ct = default);
    Task AddAsync(Size size, CancellationToken ct = default);
    void Update(Size size);
}
