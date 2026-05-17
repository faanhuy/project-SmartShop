using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Interfaces;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReturnRequest?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<List<ReturnRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<ReturnRequest>> GetAllAsync(ReturnStatus? status, CancellationToken ct = default);
    Task AddAsync(ReturnRequest returnRequest, CancellationToken ct = default);
    void Update(ReturnRequest returnRequest);
}
