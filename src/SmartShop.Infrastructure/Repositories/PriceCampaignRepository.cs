using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;
using SmartShop.Infrastructure.Persistence.Configurations;

namespace SmartShop.Infrastructure.Repositories;

public class PriceCampaignRepository(ApplicationDbContext db) : IPriceCampaignRepository
{
    public async Task<PriceCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await db.PriceCampaigns
            .Include(c => c.ItemsNav)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (campaign is null) return null;

        // Load store IDs from join table
        var storeIds = await db.PriceListStores
            .Where(s => s.CampaignId == id)
            .Select(s => s.StoreId)
            .ToListAsync(ct);

        campaign.SetStores(storeIds);
        return campaign;
    }

    public async Task<IReadOnlyList<PriceCampaign>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var campaigns = await db.PriceCampaigns
            .AsNoTracking()
            .OrderByDescending(c => c.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.ItemsNav)
            .ToListAsync(ct);

        if (campaigns.Count == 0) return campaigns.AsReadOnly();

        var campaignIds = campaigns.Select(c => c.Id).ToList();
        var storeRows = await db.PriceListStores
            .AsNoTracking()
            .Where(s => campaignIds.Contains(s.CampaignId))
            .ToListAsync(ct);

        var storeMap = storeRows
            .GroupBy(s => s.CampaignId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.StoreId).ToList());

        foreach (var campaign in campaigns)
        {
            if (storeMap.TryGetValue(campaign.Id, out var ids))
                campaign.SetStores(ids);
        }

        return campaigns.AsReadOnly();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await db.PriceCampaigns.CountAsync(ct);

    public async Task AddAsync(PriceCampaign campaign, CancellationToken ct = default)
    {
        await db.PriceCampaigns.AddAsync(campaign, ct);
    }

    public void Update(PriceCampaign campaign)
        => db.PriceCampaigns.Update(campaign);

    public void Remove(PriceCampaign campaign)
        => db.PriceCampaigns.Remove(campaign);

    public async Task<Dictionary<(Guid productId, Guid? sizeId), (int ruleType, decimal discountValue)>>
        GetEffectivePriceItemsAsync(
            Guid storeId,
            IEnumerable<(Guid productId, Guid? sizeId)> keys,
            DateTime at,
            CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return new Dictionary<(Guid, Guid?), (int, decimal)>();

        var productIds = keyList.Select(k => k.productId).Distinct().ToList();

        // Load all candidate items from active campaigns at this time/store
        var candidateItems = await (
            from ci in db.PriceCampaignItems
            join c in db.PriceCampaigns on ci.CampaignId equals c.Id
            where c.IsActive
               && c.StartsAt <= at && c.EndsAt > at
               && productIds.Contains(ci.ProductId)
            select new
            {
                ci.ProductId,
                ci.SizeId,
                ci.RuleType,
                ci.DiscountValue,
                c.StartsAt,
                c.AppliesToAll,
                c.Id
            }
        ).ToListAsync(ct);

        if (candidateItems.Count == 0)
            return new Dictionary<(Guid, Guid?), (int, decimal)>();

        // Filter by store scope — load store assignments for relevant campaigns
        var campaignIds = candidateItems.Select(x => x.Id).Distinct().ToList();
        var storeAssignments = await db.PriceListStores
            .AsNoTracking()
            .Where(s => campaignIds.Contains(s.CampaignId) && s.StoreId == storeId)
            .Select(s => s.CampaignId)
            .ToListAsync(ct);

        var storeAssignmentSet = new HashSet<Guid>(storeAssignments);

        // Filter by store scope + matching keys, then take winning campaign per (productId, sizeId)
        var keySet = new HashSet<(Guid, Guid?)>(keyList);

        var result = candidateItems
            .Where(x => (x.AppliesToAll || storeAssignmentSet.Contains(x.Id))
                        && keySet.Contains((x.ProductId, x.SizeId)))
            .GroupBy(x => (x.ProductId, x.SizeId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var winner = g.OrderByDescending(x => x.StartsAt).First();
                    return ((int)winner.RuleType, winner.DiscountValue);
                });

        return result;
    }

    public async Task ReplaceItemsAsync(Guid campaignId, IEnumerable<PriceCampaignItem> newItems, CancellationToken ct)
    {
        var existing = await db.PriceCampaignItems
            .Where(i => i.CampaignId == campaignId)
            .ToListAsync(ct);

        db.PriceCampaignItems.RemoveRange(existing);
        await db.PriceCampaignItems.AddRangeAsync(newItems, ct);
    }

    public async Task SyncStoresAsync(Guid campaignId, IEnumerable<Guid> storeIds, CancellationToken ct)
    {
        var existing = await db.PriceListStores
            .Where(s => s.CampaignId == campaignId)
            .ToListAsync(ct);

        db.PriceListStores.RemoveRange(existing);

        var newRows = storeIds.Select(sid => new PriceListStoreRow
        {
            CampaignId = campaignId,
            StoreId = sid
        });
        await db.PriceListStores.AddRangeAsync(newRows, ct);
    }
}
