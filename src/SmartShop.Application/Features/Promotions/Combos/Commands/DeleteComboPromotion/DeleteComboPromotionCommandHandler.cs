using MediatR;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Promotions.Combos.Commands.DeleteComboPromotion;

public class DeleteComboPromotionCommandHandler(
    IComboPromotionRepository repo,
    IUnitOfWork uow
) : IRequestHandler<DeleteComboPromotionCommand>
{
    public async Task Handle(DeleteComboPromotionCommand cmd, CancellationToken ct)
    {
        var combo = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(ComboPromotion), cmd.Id);

        repo.Remove(combo);
        await uow.SaveChangesAsync(ct);
    }
}
