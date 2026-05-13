using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Promotions.Combos.Commands.CreateComboPromotion;

public class CreateComboPromotionCommandHandler(
    IComboPromotionRepository repo,
    IProductRepository productRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateComboPromotionCommand, ComboPromotionDto>
{
    public async Task<ComboPromotionDto> Handle(CreateComboPromotionCommand cmd, CancellationToken ct)
    {
        // Verify trigger product exists
        var triggerProduct = await productRepo.GetByIdAsync(cmd.TriggerProductId, ct)
            ?? throw new NotFoundException(nameof(Product), cmd.TriggerProductId);

        Product? rewardProduct = null;
        if (cmd.RewardProductId.HasValue)
        {
            rewardProduct = await productRepo.GetByIdAsync(cmd.RewardProductId.Value, ct)
                ?? throw new NotFoundException(nameof(Product), cmd.RewardProductId.Value);
        }

        var combo = ComboPromotion.Create(
            cmd.Name,
            cmd.TriggerProductId,
            cmd.TriggerSizeId,
            cmd.TriggerMinQuantity,
            cmd.RewardType,
            cmd.RewardProductId,
            cmd.RewardSizeId,
            cmd.RewardQuantity,
            cmd.RewardAmount,
            cmd.StoreId,
            cmd.StartsAt,
            cmd.EndsAt);

        await repo.AddAsync(combo, ct);
        await uow.SaveChangesAsync(ct);

        return MapToDto(combo, triggerProduct.Name, null, rewardProduct?.Name, null);
    }

    internal static ComboPromotionDto MapToDto(
        ComboPromotion c,
        string triggerProductName,
        string? triggerSizeLabel,
        string? rewardProductName,
        string? rewardSizeLabel) => new(
            c.Id,
            c.Name,
            c.TriggerProductId,
            triggerProductName,
            c.TriggerSizeId,
            triggerSizeLabel,
            c.TriggerMinQuantity,
            (int)c.RewardType,
            c.RewardProductId,
            rewardProductName,
            c.RewardSizeId,
            rewardSizeLabel,
            c.RewardQuantity,
            c.RewardAmount,
            c.StoreId,
            c.StartsAt,
            c.EndsAt,
            c.IsActive
        );
}
