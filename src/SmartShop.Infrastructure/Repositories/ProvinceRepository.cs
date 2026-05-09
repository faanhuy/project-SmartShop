using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class ProvinceRepository(ApplicationDbContext context) : IProvinceRepository
{
    public async Task<IEnumerable<Province>> GetAllAsync() =>
        await context.Provinces.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

    public async Task<Province?> GetByIdAsync(int id) =>
        await context.Provinces.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
}
