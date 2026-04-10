using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;

namespace SmartShop.Infrastructure.Services;

public class ClaudeAIService : ISemanticKernelService
{
    private readonly HttpClient _claudeHttp;
    private readonly string _claudeModel;
    private readonly ILogger<ClaudeAIService> _logger;

    public ClaudeAIService(IConfiguration configuration, ILogger<ClaudeAIService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;

        var anthropicKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
        _claudeModel = configuration["Anthropic:Model"] ?? "claude-sonnet-4-6";

        _claudeHttp = httpClientFactory.CreateClient("Anthropic");
        _claudeHttp.BaseAddress = new Uri("https://api.anthropic.com");
        _claudeHttp.DefaultRequestHeaders.Add("x-api-key", anthropicKey);
        _claudeHttp.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    /// <summary>
    /// Gọi Claude để xếp hạng sản phẩm theo độ liên quan với query.
    /// Claude trả về JSON array: [{"id":"...","score":0.95}, ...]
    /// </summary>
    public async Task<IReadOnlyList<(Guid Id, double Score)>> SemanticSearchAsync(
        string query,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int topN,
        CancellationToken ct = default)
    {
        var productList = candidates
            .Select(p => new { id = p.Id.ToString(), name = p.Name, description = p.Description })
            .ToList();

        if (productList.Count == 0)
            return [];

        var productJson = JsonSerializer.Serialize(productList);
        var prompt = $"Bạn là hệ thống tìm kiếm sản phẩm thương mại điện tử.\n" +
            $"Dựa vào từ khóa tìm kiếm, hãy chọn tối đa {topN} sản phẩm phù hợp nhất từ danh sách bên dưới.\n\n" +
            $"Từ khóa: \"{query}\"\n\n" +
            $"Danh sách sản phẩm:\n{productJson}\n\n" +
            "Yêu cầu:\n" +
            "- Chỉ chọn sản phẩm thực sự liên quan đến từ khóa\n" +
            "- Sắp xếp theo độ liên quan giảm dần\n" +
            "- Trả về ĐÚNG một JSON array, mỗi phần tử có \"id\" và \"score\" (0.0 đến 1.0)\n" +
            "- Nếu không có sản phẩm nào phù hợp, trả về []\n" +
            "- Chỉ trả về JSON array, không kèm bất kỳ nội dung nào khác\n\n" +
            "Ví dụ: [{\"id\":\"abc123\",\"score\":0.95},{\"id\":\"def456\",\"score\":0.80}]";

        var body = new
        {
            model = _claudeModel,
            max_tokens = 1024,
            messages = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            var response = await _claudeHttp.PostAsJsonAsync("/v1/messages", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                if ((int)response.StatusCode == 429)
                {
                    _logger.LogWarning("Claude API rate limit exceeded on semantic search.");
                    throw new ServiceUnavailableException("Dịch vụ AI tạm thời không khả dụng (rate limit). Vui lòng thử lại sau.");
                }
                if (IsLowCreditError(errorText))
                {
                    _logger.LogError("Claude API credit balance too low.");
                    throw new ServiceUnavailableException("Dịch vụ AI không khả dụng do tài khoản hết credit. Vui lòng liên hệ quản trị viên.");
                }
                _logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, errorText);
                throw new ServiceUnavailableException($"Dịch vụ AI lỗi: {response.StatusCode}");
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var rawText = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "[]";

            using var resultDoc = JsonDocument.Parse(rawText.Trim());
            var ranked = new List<(Guid Id, double Score)>();
            foreach (var item in resultDoc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idProp)) continue;
                var idStr = idProp.GetString();
                var score = item.TryGetProperty("score", out var scoreProp) ? scoreProp.GetDouble() : 1.0;
                if (Guid.TryParse(idStr, out var guid))
                    ranked.Add((guid, score));
            }
            return ranked;
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Claude semantic search.");
            throw new ServiceUnavailableException("Không thể kết nối đến dịch vụ AI. Vui lòng thử lại sau.");
        }
    }

    /// <summary>
    /// Gọi Claude để tìm sản phẩm tương tự với sản phẩm gốc.
    /// </summary>
    public async Task<IReadOnlyList<(Guid Id, double Score)>> GetRecommendationsAsync(
        (Guid Id, string Name, string Description) source,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int count,
        CancellationToken ct = default)
    {
        var candidateList = candidates
            .Select(p => new { id = p.Id.ToString(), name = p.Name, description = p.Description })
            .ToList();

        if (candidateList.Count == 0)
            return [];

        var sourceJson = JsonSerializer.Serialize(new { id = source.Id.ToString(), name = source.Name, description = source.Description });
        var candidatesJson = JsonSerializer.Serialize(candidateList);

        var prompt = $"Bạn là hệ thống gợi ý sản phẩm thương mại điện tử.\n" +
            $"Dựa vào sản phẩm gốc, hãy chọn tối đa {count} sản phẩm tương tự hoặc liên quan nhất từ danh sách.\n\n" +
            $"Sản phẩm gốc:\n{sourceJson}\n\n" +
            $"Danh sách sản phẩm khác:\n{candidatesJson}\n\n" +
            "Yêu cầu:\n" +
            "- Chọn sản phẩm có cùng loại, cùng danh mục hoặc là phụ kiện liên quan\n" +
            "- Sắp xếp theo độ tương tự giảm dần\n" +
            "- Trả về ĐÚNG một JSON array, mỗi phần tử có \"id\" và \"score\" (0.0 đến 1.0)\n" +
            "- Nếu không có sản phẩm nào phù hợp, trả về []\n" +
            "- Chỉ trả về JSON array, không kèm bất kỳ nội dung nào khác\n\n" +
            "Ví dụ: [{\"id\":\"abc123\",\"score\":0.92},{\"id\":\"def456\",\"score\":0.75}]";

        var body = new
        {
            model = _claudeModel,
            max_tokens = 1024,
            messages = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            var response = await _claudeHttp.PostAsJsonAsync("/v1/messages", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                if ((int)response.StatusCode == 429)
                {
                    _logger.LogWarning("Claude API rate limit exceeded on recommendations.");
                    throw new ServiceUnavailableException("Dịch vụ AI tạm thời không khả dụng (rate limit). Vui lòng thử lại sau.");
                }
                if (IsLowCreditError(errorText))
                {
                    _logger.LogError("Claude API credit balance too low on recommendations.");
                    throw new ServiceUnavailableException("Dịch vụ AI không khả dụng do tài khoản hết credit. Vui lòng liên hệ quản trị viên.");
                }
                _logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, errorText);
                throw new ServiceUnavailableException($"Dịch vụ AI lỗi: {response.StatusCode}");
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var rawText = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "[]";

            using var resultDoc = JsonDocument.Parse(rawText.Trim());
            var ranked = new List<(Guid Id, double Score)>();
            foreach (var item in resultDoc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idProp)) continue;
                var idStr = idProp.GetString();
                var score = item.TryGetProperty("score", out var scoreProp) ? scoreProp.GetDouble() : 1.0;
                if (Guid.TryParse(idStr, out var guid))
                    ranked.Add((guid, score));
            }
            return ranked;
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Claude recommendations.");
            throw new ServiceUnavailableException("Không thể kết nối đến dịch vụ AI. Vui lòng thử lại sau.");
        }
    }

    /// <summary>
    /// Gọi Claude API để tạo mô tả sản phẩm tiếng Việt.
    /// </summary>
    public async Task<string> GenerateProductDescriptionAsync(
        string productName, string categoryName, CancellationToken ct = default)
    {
        var prompt = $"""
            Bạn là chuyên gia viết nội dung thương mại điện tử. Hãy viết mô tả sản phẩm hấp dẫn bằng tiếng Việt
            cho sản phẩm sau. Mô tả cần 2-3 câu, làm nổi bật lợi ích chính và phù hợp với cửa hàng trực tuyến.

            Tên sản phẩm: {productName}
            Danh mục: {categoryName}

            Chỉ trả về nội dung mô tả, không kèm theo bình luận hoặc tiêu đề.
            """;

        var body = new
        {
            model = _claudeModel,
            max_tokens = 512,
            messages = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            var response = await _claudeHttp.PostAsJsonAsync("/v1/messages", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                if ((int)response.StatusCode == 429)
                {
                    _logger.LogWarning("Claude API rate limit exceeded.");
                    throw new ServiceUnavailableException("Dịch vụ AI tạm thời không khả dụng (rate limit). Vui lòng thử lại sau.");
                }
                if (IsLowCreditError(errorText))
                {
                    _logger.LogError("Claude API credit balance too low.");
                    throw new ServiceUnavailableException("Dịch vụ AI không khả dụng do tài khoản hết credit. Vui lòng liên hệ quản trị viên.");
                }
                _logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, errorText);
                throw new ServiceUnavailableException($"Dịch vụ AI lỗi: {response.StatusCode}");
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            return content ?? string.Empty;
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Claude API.");
            throw new ServiceUnavailableException("Không thể kết nối đến dịch vụ AI. Vui lòng thử lại sau.");
        }
    }

    private static bool IsLowCreditError(string errorBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var errorObj)
                && errorObj.TryGetProperty("message", out var msg))
            {
                var message = msg.GetString() ?? string.Empty;
                return message.Contains("credit balance", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("billing", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { /* not valid JSON — ignore */ }
        return false;
    }
}
