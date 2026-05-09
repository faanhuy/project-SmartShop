namespace SmartShop.Infrastructure.Data.Seeders;

internal interface IDataSeeder
{
    int Order { get; }
    Task SeedAsync(CancellationToken cancellationToken = default);
}
