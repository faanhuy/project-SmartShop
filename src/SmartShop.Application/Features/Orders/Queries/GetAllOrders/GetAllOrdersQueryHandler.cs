using MediatR;
using SmartShop.Application.Products.Queries.GetProducts;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Orders.Queries.GetAllOrders;

public class GetCouponsQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetAllOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(
        GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await orderRepository.GetAllPagedAsync(
            request.Page, request.PageSize, request.StatusFilter, cancellationToken);

        var dtos = items.Select(o => new OrderDto
        {
            Id              = o.Id,
            UserId          = o.UserId,
            Status          = o.Status.ToString(),
            TotalAmount     = o.TotalAmount,
            ShippingAddress = o.ShippingAddress,
            Notes           = o.Notes,
            Items           = o.Items.Select(i => new OrderItemDto
            {
                ProductId   = i.ProductId,
                ProductName = i.ProductName,
                ProductImageUrl = i.Product?.ImageUrl,
                Quantity    = i.Quantity,
                UnitPrice   = i.UnitPrice,
                SubTotal    = i.SubTotal
            }).ToList(),
            CreatedAt = o.CreatedAt
        });

        return new PagedResult<OrderDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
