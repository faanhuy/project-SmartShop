using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class UserAddressRepository(ApplicationDbContext context) : IUserAddressRepository
{
    public async Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await context.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<UserAddress?> GetDefaultByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await context.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault, ct);
    }

    public async Task AddAsync(UserAddress address, CancellationToken ct = default)
    {
        await context.UserAddresses.AddAsync(address, ct);
    }

    public void Update(UserAddress address)
    {
        context.UserAddresses.Update(address);
    }

    public void Remove(UserAddress address)
    {
        context.UserAddresses.Remove(address);
    }
}
