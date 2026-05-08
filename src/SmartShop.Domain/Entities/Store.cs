using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class Store : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Store() { }

    public static Store Create(string name, string address, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        return new Store
        {
            Name = name,
            Address = address,
            Phone = phone,
            IsActive = true
        };
    }

    public void Update(string name, string address, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Name = name;
        Address = address;
        Phone = phone;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
