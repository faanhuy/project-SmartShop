namespace SmartShop.Domain.Entities;

public class Province
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public ICollection<Ward> Wards { get; private set; } = [];

    private Province() { }

    public static Province Create(int id, string name, string code) =>
        new() { Id = id, Name = name, Code = code };
}
