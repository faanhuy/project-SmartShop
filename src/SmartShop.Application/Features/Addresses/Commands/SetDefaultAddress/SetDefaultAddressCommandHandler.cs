using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Addresses.Commands.SetDefaultAddress;

public class SetDefaultAddressCommandHandler(
    IUserAddressRepository addressRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SetDefaultAddressCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(SetDefaultAddressCommand command, CancellationToken cancellationToken)
    {
        var target = await addressRepository.GetByIdAsync(command.AddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAddress), command.AddressId);

        if (target.UserId != command.UserId)
            throw new UnauthorizedException("Không có quyền thay đổi địa chỉ này.");

        var allAddresses = await addressRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        foreach (var addr in allAddresses)
            addr.UnsetDefault();

        target.SetAsDefault();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã đặt địa chỉ mặc định.");
    }
}
