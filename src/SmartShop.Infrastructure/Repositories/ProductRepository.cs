using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Product?> GetByIdWithSizesAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Sizes.OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Slug == slug, ct);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? categoryId = null, string? search = null,
        string sortBy = "newest", CancellationToken ct = default)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(p =>
                EF.Functions.Like(EF.Functions.Collate(p.Name, "Latin1_General_CI_AI"), pattern) ||
                EF.Functions.Like(EF.Functions.Collate(p.Description, "Latin1_General_CI_AI"), pattern));
        }

        var totalCount = await query.CountAsync(ct);

        List<Product> items;

        if (sortBy == "best_selling")
        {
            // Sort by total units sold across all completed orders
            items = await query
                .Select(p => new
                {
                    Product = p,
                    TotalSold = _context.OrderItems
                        .Where(oi => oi.ProductId == p.Id)
                        .Sum(oi => (int?)oi.Quantity) ?? 0
                })
                .OrderByDescending(x => x.TotalSold)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Product)
                .ToListAsync(ct);
        }
        else
        {
            var sorted = sortBy switch
            {
                "price_asc"  => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc"   => query.OrderBy(p => p.Name),
                "name_desc"  => query.OrderByDescending(p => p.Name),
                _            => query.OrderByDescending(p => p.CreatedAt),
            };

            items = await sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        return (items, totalCount);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public void Delete(Product product)
    {
        _context.Products.Remove(product);
    }
}
