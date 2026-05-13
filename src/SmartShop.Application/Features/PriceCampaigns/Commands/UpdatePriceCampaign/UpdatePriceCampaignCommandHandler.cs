using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.PriceCampaigns.Commands.CreatePriceCampaign;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.PriceCampaigns.Commands.UpdatePriceCampaign;

public class UpdatePriceCampaignCommandHandler(
    IPriceCampaignRepository priceCampaignRepo,
    IProductRepository productRepo,
    ICacheService cache,
    IUnitOfWork uow
) : IRequestHandler<UpdatePriceCampaignCommand, ApiResponse<PriceCampaignDto>>
{
    public async Task<ApiResponse<PriceCampaignDto>> Handle(
        UpdatePriceCampaignCommand cmd, CancellationToken ct)
    {
        var campaign = await priceCampaignRepo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(PriceCampaign), cmd.Id);

        ArgumentException.ThrowIfNullOrWhiteSpace(cmd.Name);

        if (cmd.EndsAt <= cmd.StartsAt)
            throw new ArgumentException("EndsAt phải sau StartsAt.");

        if (!cmd.AppliesToAll && (cmd.StoreIds is null || cmd.StoreIds.Count == 0))
            throw new ArgumentException("Phải chọn ít nhất một chi nhánh khi AppliesToAll = false.");

        await ValidateItemsAsync(cmd.Items, productRepo, ct);

        campaign.UpdateHeader(cmd.Name, cmd.StartsAt, cmd.EndsAt, cmd.AppliesToAll);

        var newStoreIds = cmd.AppliesToAll ? [] : cmd.StoreIds;
        campaign.SetStores(newStoreIds);
        await priceCampaignRepo.SyncStoresAsync(campaign.Id, newStoreIds, ct);

        // Replace items: delete old rows, insert new
        var newItems = cmd.Items.Select(input =>
            PriceCampaignItem.Create(
                campaign.Id, input.ProductId, input.SizeId,
                (PriceRuleType)input.RuleType, input.DiscountValue)
        ).ToList();

        await priceCampaignRepo.ReplaceItemsAsync(campaign.Id, newItems, ct);

        priceCampaignRepo.Update(campaign);
        await uow.SaveChangesAsync(ct);

        await cache.RemoveByPrefixAsync("price:effective:", ct);

        return ApiResponse<PriceCampaignDto>.Ok(MapToDto(campaign));
    }

    private static async Task ValidateItemsAsync(
        List<CreatePriceCampaignItemInput> items,
        IProductRepository productRepo,
        CancellationToken ct)
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
