using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class WardRepository(ApplicationDbContext context) : IWardRepository
{
    public async Task<IEnumerable<Ward>> GetByProvinceAsync(int provinceId) =>
        await context.Wards
            .AsNoTracking()
            .Where(w => w.ProvinceId == provinceId)
            .OrderBy(w => w.Name)
            .ToListAsync();

    public async Task<Ward?> GetByIdAsync(int id) =>
        await context.Wards.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
}
