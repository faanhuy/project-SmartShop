using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Wishlist.Commands.AddToWishlist;

public class AddToWishlistCommandHandler(
    IWishlistRepository wishlistRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<AddToWishlistCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var alreadyExists = await wishlistRepository.ExistsAsync(userId, request.ProductId, cancellationToken);
        if (alreadyExists)
            throw new ConflictException("Sản phẩm đã có trong danh sách yêu thích.");

        var item = WishlistItem.Create(userId, request.ProductId);
        await wishlistRepository.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã thêm vào danh sách yêu thích.");
    }
}
