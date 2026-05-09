using MediatR;
using SmartShop.Application.Products.Queries.GetProducts;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Orders.Queries.GetMyOrders;

public class GetMyOrdersQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetMyOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await orderRepository.GetPagedByUserIdAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            ShippingAddressId = order.ShippingAddressId,
            ShippingStreet = order.ShippingStreet,
            ShippingWardId = order.ShippingWardId,
            ShippingProvinceId = order.ShippingProvinceId,
            ShippingWardName = order.ShippingWard?.Name,
            ShippingProvinceName = order.ShippingProvince?.Name,
            Notes = order.Notes,
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PaidAt = order.PaidAt,
            VnpayTransactionId = order.VnpayTransactionId,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImageUrl = i.Product?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList(),
            CreatedAt = order.CreatedAt
        }).ToList();

        return new PagedResult<OrderDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
