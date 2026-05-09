namespace SmartShop.Domain.Entities;

public class Ward
{
    public int Id { get; private set; }
    public int ProvinceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public Province Province { get; private set; } = null!;

    private Ward() { }

    public static Ward Create(int id, int provinceId, string name, string code) =>
        new() { Id = id, ProvinceId = provinceId, Name = name, Code = code };
}
