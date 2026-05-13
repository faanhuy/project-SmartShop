using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Products.Commands.DeleteProductSize;

public record DeleteProductSizeCommand(Guid SizeId) : IRequest<ApiResponse<bool>>;
