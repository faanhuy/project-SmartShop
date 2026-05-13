using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.PriceCampaigns.Commands.CreatePriceCampaign;

public class CreatePriceCampaignCommandHandler(
    IPriceCampaignRepository priceCampaignRepo,
    IProductRepository productRepo,
    ICacheService cache,
    IUnitOfWork uow
) : IRequestHandler<CreatePriceCampaignCommand, ApiResponse<PriceCampaignDto>>
{
    public async Task<ApiResponse<PriceCampaignDto>> Handle(
        CreatePriceCampaignCommand cmd, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd.Name);

        if (cmd.EndsAt <= cmd.StartsAt)
            throw new ArgumentException("EndsAt phải sau StartsAt.");

        if (!cmd.AppliesToAll && (cmd.StoreIds is null || cmd.StoreIds.Count == 0))
            throw new ArgumentException("Phải chọn ít nhất một chi nhánh khi AppliesToAll = false.");

        // Validate items: products with HasSizes must have entries for all active sizes
        await ValidateItemsAsync(cmd.Items, ct);

        var campaign = PriceCampaign.Create(
            cmd.Name, cmd.StartsAt, cmd.EndsAt,
            cmd.AppliesToAll,
            cmd.AppliesToAll ? null : cmd.StoreIds);

        // Build and attach items via ItemsNav
        foreach (var input in cmd.Items)
        {
            var ruleType = (PriceRuleType)input.RuleType;
            var item = PriceCampaignItem.Create(
                campaign.Id, input.ProductId, input.SizeId, ruleType, input.DiscountValue);
            campaign.ItemsNav.Add(item);
        }

        await priceCampaignRepo.AddAsync(campaign, ct);
        await uow.SaveChangesAsync(ct);

        // Sync store join rows after save (campaign.Id is needed)
        if (!cmd.AppliesToAll && cmd.StoreIds.Count > 0)
        {
            await priceCampaignRepo.SyncStoresAsync(campaign.Id, cmd.StoreIds, ct);
            await uow.SaveChangesAsync(ct);
        }

        await cache.RemoveByPrefixAsync("price:effective:", ct);

        return ApiResponse<PriceCampaignDto>.Ok(MapToDto(campaign));
    }

    private async Task ValidateItemsAsync(
        List<CreatePriceCampaignItemInput> items, CancellationToken ct)
    {
        var groupedByProduct = items.GroupBy(i => i.ProductId);

        foreach (var group in groupedByProduct)
        {
            var product = await productRepo.GetByIdWithSizesAsync(group.Key, ct)
                ?? throw new NotFoundException(nameof(Product), group.Key);

            if (product.HasSizes)
            {
                var activeSizeIds = product.Sizes
                    .Where(s => s.IsActive)
                    .Select(s => s.Id)
                    .ToHashSet();

                var providedSizeIds = group
                    .Where(i => i.SizeId.HasValue)
                    .Select(i => i.SizeId!.Value)
                    .ToHashSet();

                var missing = activeSizeIds.Except(providedSizeIds).ToList();
                if (missing.Count > 0)
                    throw new ArgumentException(
                        $"Sản phẩm '{product.Name}' có sizes — phải cung cấp giá cho tất cả active sizes.");
            }
        }
    }

    private static PriceCampaignDto MapToDto(PriceCampaign c) => new(
        c.Id, c.Name, c.StartsAt, c.EndsAt, c.AppliesToAll, c.IsActive,
        c.StoreIds.ToList(),
        c.ItemsNav.Select(i => new PriceCampaignItemDto(
            i.Id, i.ProductId, string.Empty, i.SizeId, null, (int)i.RuleType, i.DiscountValue
        )).ToList()
    );
}
