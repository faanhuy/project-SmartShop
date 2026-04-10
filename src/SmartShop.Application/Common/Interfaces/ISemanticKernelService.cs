namespace SmartShop.Application.Common.Interfaces;

public interface ISemanticKernelService
{
    /// <summary>
    /// Dùng Claude để xếp hạng sản phẩm theo độ liên quan với query.
    /// Trả về danh sách (Id, Score) sắp xếp theo score giảm dần.
    /// </summary>
    Task<IReadOnlyList<(Guid Id, double Score)>> SemanticSearchAsync(
        string query,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int topN,
        CancellationToken ct = default);

    /// <summary>
    /// Dùng Claude để tìm sản phẩm tương tự với sản phẩm gốc.
    /// Trả về danh sách (Id, Score) sắp xếp theo độ tương tự giảm dần.
    /// </summary>
    Task<IReadOnlyList<(Guid Id, double Score)>> GetRecommendationsAsync(
        (Guid Id, string Name, string Description) source,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int count,
        CancellationToken ct = default);

    Task<string> GenerateProductDescriptionAsync(string productName, string categoryName, CancellationToken ct = default);
}
