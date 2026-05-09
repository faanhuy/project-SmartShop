using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Wishlist.Queries.GetWishlist;

public class GetWishlistQueryHandler(
    IWishlistRepository wishlistRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetWishlistQuery, ApiResponse<List<WishlistItemDto>>>
{
    public async Task<ApiResponse<List<WishlistItemDto>>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var items = await wishlistRepository.GetByUserIdAsync(userId, cancellationToken);

        var dtos = items.Select(i => new WishlistItemDto(
            ProductId: i.ProductId,
            ProductName: i.Product?.Name ?? string.Empty,
            Price: i.Product?.Price ?? 0,
            ImageUrl: i.Product?.ImageUrl,
            IsInStock: i.Product?.IsActive ?? false,
            Slug: i.Product?.Slug ?? string.Empty
        )).ToList();

        return ApiResponse<List<WishlistItemDto>>.Ok(dtos);
    }
}
