using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Addresses.Commands.DeleteAddress;

public class DeleteAddressCommandHandler(
    IUserAddressRepository addressRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteAddressCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteAddressCommand command, CancellationToken cancellationToken)
    {
        var address = await addressRepository.GetByIdAsync(command.AddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAddress), command.AddressId);

        if (address.UserId != command.UserId)
            throw new UnauthorizedException("Không có quyền xóa địa chỉ này.");

        // If deleting the default address, promote the oldest remaining address
        if (address.IsDefault)
        {
            var remaining = await addressRepository.GetByUserIdAsync(command.UserId, cancellationToken);
            var oldest = remaining
                .Where(a => a.Id != command.AddressId)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefault();

            if (oldest is not null)
                oldest.SetAsDefault();
        }

        addressRepository.Remove(address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Đã xóa địa chỉ giao hàng.");
    }
}
