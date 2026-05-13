using MediatR;
using SmartShop.Application.Features.Promotions.Combos.Commands.CreateComboPromotion;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Promotions.Combos.Commands.UpdateComboPromotion;

public class UpdateComboPromotionCommandHandler(
    IComboPromotionRepository repo,
    IProductRepository productRepo,
    IUnitOfWork uow
) : IRequestHandler<UpdateComboPromotionCommand, ComboPromotionDto>
{
    public async Task<ComboPromotionDto> Handle(UpdateComboPromotionCommand cmd, CancellationToken ct)
    {
        var combo = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(ComboPromotion), cmd.Id);

        var triggerProduct = await productRepo.GetByIdAsync(cmd.TriggerProductId, ct)
            ?? throw new NotFoundException(nameof(Product), cmd.TriggerProductId);

        Product? rewardProduct = null;
        if (cmd.RewardProductId.HasValue)
        {
            rewardProduct = await productRepo.GetByIdAsync(cmd.RewardProductId.Value, ct)
                ?? throw new NotFoundException(nameof(Product), cmd.RewardProductId.Value);
        }

        combo.Update(
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

        repo.Update(combo);
        await uow.SaveChangesAsync(ct);

        return CreateComboPromotionCommandHandler.MapToDto(combo, triggerProduct.Name, null, rewardProduct?.Name, null);
    }
}
