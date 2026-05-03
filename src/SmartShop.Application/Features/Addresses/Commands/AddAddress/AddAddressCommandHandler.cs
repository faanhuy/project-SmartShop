using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Addresses.Commands.AddAddress;

public class AddAddressCommandHandler(
    IUserAddressRepository addressRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddAddressCommand, ApiResponse<AddressDto>>
{
    public async Task<ApiResponse<AddressDto>> Handle(AddAddressCommand command, CancellationToken cancellationToken)
    {
        var address = UserAddress.Create(
            command.UserId,
            command.Label,
            command.RecipientName,
            command.Phone,
            command.Street,
            command.Ward,
            command.District,
            command.City);

        // If user has no addresses yet, make this one the default
        var existing = await addressRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (existing.Count == 0)
            address.SetAsDefault();

        await addressRepository.AddAsync(address, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AddressDto>.Ok(AddressDto.From(address), "Đã thêm địa chỉ giao hàng.");
    }
}
