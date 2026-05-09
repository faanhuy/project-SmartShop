using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Addresses.Commands.UpdateAddress;

public class UpdateAddressCommandHandler(
    IUserAddressRepository addressRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAddressCommand, ApiResponse<AddressDto>>
{
    public async Task<ApiResponse<AddressDto>> Handle(UpdateAddressCommand command, CancellationToken cancellationToken)
    {
        var address = await addressRepository.GetByIdAsync(command.AddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAddress), command.AddressId);

        if (address.UserId != command.UserId)
            throw new UnauthorizedException("Không có quyền chỉnh sửa địa chỉ này.");

        address.Update(
            command.Label,
            command.RecipientName,
            command.Phone,
            command.Street,
            command.Ward,
            command.District,
            command.City,
            command.ProvinceId,
            command.WardId);

        addressRepository.Update(address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AddressDto>.Ok(AddressDto.From(address), "Đã cập nhật địa chỉ giao hàng.");
    }
}
