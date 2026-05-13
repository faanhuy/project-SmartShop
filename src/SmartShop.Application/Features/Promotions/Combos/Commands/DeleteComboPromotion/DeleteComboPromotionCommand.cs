using MediatR;

namespace SmartShop.Application.Features.Promotions.Combos.Commands.DeleteComboPromotion;

public record DeleteComboPromotionCommand(Guid Id) : IRequest;
