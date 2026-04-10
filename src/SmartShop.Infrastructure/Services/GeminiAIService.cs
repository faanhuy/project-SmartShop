using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;

namespace SmartShop.Infrastructure.Services;

public class GeminiAIService : ISemanticKernelService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string[] _models; // fallback chain
    private readonly ILogger<GeminiAIService> _logger;

    // Delay khi gặp 429/503: 3s → 8s → throw
    private static readonly int[] RetryDelaysMs = [3000, 8000];

    public GeminiAIService(
        IConfiguration configuration,
        ILogger<GeminiAIService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        var primary = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        // Fallback chain: thử lần lượt khi model trước bị 503
        _models = [primary, "gemini-2.0-flash-001", "gemini-2.0-flash-lite-001"];

        _http = httpClientFactory.CreateClient("Gemini");
        _http.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
    }

    // ── Helper: gọi Gemini với model fallback chain + smart retry ─────────
    // Vòng ngoài: thử từng model trong _models
    // Vòng trong: retry 2 lần nếu gặp 429/503 tạm thời
    private async Task<string> CallGeminiAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = maxTokens, temperature = 0.1 }
        };

        string? lastError = null;

        foreach (var model in _models)
        {
            var url = $"/v1beta/models/{model}:generateContent?key={_apiKey}";
            int maxAttempts = RetryDelaysMs.Length + 1;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                HttpResponseMessage response;
                try
                {
                    response = await _http.PostAsJsonAsync(url, body, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Network error calling Gemini [{Model}].", model);
                    lastError = "network";
                    break; // thử model tiếp theo
                }

                if (response.IsSuccessStatusCode)
                {
                    if (model != _models[0])
                        _logger.LogInformation("Gemini fallback succeeded with [{Model}].", model);
                    return await ExtractTextFromResponse(response, ct);
                }

                var statusCode = (int)response.StatusCode;
                var errorBody  = await response.Content.ReadAsStringAsync(ct);

                if (statusCode is 429 or 503)
                {
                    // Hết quota ngày → skip model này, thử model tiếp theo
                    if (IsQuotaExhausted(errorBody))
                    {
                        _logger.LogWarning("Gemini [{Model}] daily quota exhausted, trying next model.", model);
                        lastError = "quota";
                        break;
                    }

                    // 503 tạm thời → retry trong cùng model
                    if (attempt < maxAttempts)
                    {
                        var delay = ParseRetryDelayMs(errorBody) ?? RetryDelaysMs[attempt - 1];
                        _logger.LogWarning("Gemini [{Model}] {Status} attempt {A}/{Max}, retry in {D}ms.",
                            model, statusCode, attempt, maxAttempts, delay);
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    // Hết retry trong model này → thử model tiếp theo
                    _logger.LogWarning("Gemini [{Model}] {Status} after {Max} attempts, trying next model.",
                        model, statusCode, maxAttempts);
                    lastError = "overload";
                    break;
                }

                // Lỗi không thể retry (400, 401...) → throw ngay
                _logger.LogError("Gemini [{Model}] HTTP {Status}: {Body}", model, statusCode, errorBody);
                throw new ServiceUnavailableException($"Dịch vụ AI lỗi: HTTP {statusCode}");
            }
        }

        // Tất cả models đều thất bại
        var msg = lastError switch
        {
            "quota"   => "Dịch vụ AI đã đạt giới hạn sử dụng hôm nay. Vui lòng thử lại vào ngày mai.",
            "overload" => "Dịch vụ AI đang quá tải. Vui lòng thử lại sau vài giây.",
            _          => "Không thể kết nối đến dịch vụ AI. Vui lòng thử lại sau."
        };
        throw new ServiceUnavailableException(msg);
    }

    /// Kiểm tra lỗi hết quota ngày (limit đã cạn kiệt, retry vô ích)
    private static bool IsQuotaExhausted(string errorBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (!doc.RootElement.TryGetProperty("error", out var err)) return false;

            // Quota per-day violation → không retry
            if (err.TryGetProperty("details", out var details))
                foreach (var detail in details.EnumerateArray())
                    if (detail.TryGetProperty("violations", out var violations))
                        foreach (var v in violations.EnumerateArray())
                            if (v.TryGetProperty("quotaId", out var qid) &&
                                qid.GetString()?.Contains("PerDay", StringComparison.OrdinalIgnoreCase) == true)
                                return true;
        }
        catch { /* ignore parse errors */ }
        return false;
    }

    /// Đọc retryDelay từ Gemini error response, trả về milliseconds
    private static int? ParseRetryDelayMs(string errorBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (!doc.RootElement.TryGetProperty("error", out var err)) return null;
            if (!err.TryGetProperty("details", out var details)) return null;

            foreach (var detail in details.EnumerateArray())
                if (detail.TryGetProperty("retryDelay", out var rd))
                {
                    // Format: "17s" hoặc "17.5s"
                    var raw = rd.GetString() ?? "";
                    var seconds = raw.TrimEnd('s');
                    if (double.TryParse(seconds, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var s))
                        return (int)(s * 1000);
                }
        }
        catch { /* ignore */ }
        return null;
    }

    // ── Helper: trích text từ response (bỏ qua thinking parts) ───────────
    private static async Task<string> ExtractTextFromResponse(HttpResponseMessage response, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var parts = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts");

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            // Gemini 2.5 thinking model có part {thought: true} → bỏ qua
            if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                continue;
            if (part.TryGetProperty("text", out var textProp))
                sb.Append(textProp.GetString());
        }
        return sb.ToString();
    }

    // ── Helper: tìm JSON array [...] đầu tiên trong text ─────────────────
    private IReadOnlyList<(Guid Id, double Score)> ParseRankedJson(string rawText)
    {
        _logger.LogDebug("Gemini raw response: {Text}", rawText);

        var start = rawText.IndexOf('[');
        var end = rawText.LastIndexOf(']');

        if (start < 0 || end <= start)
        {
            _logger.LogWarning("Gemini response contains no JSON array. Raw: {Text}", rawText);
            return [];
        }

        try
        {
            var json = rawText[start..(end + 1)];
            using var doc = JsonDocument.Parse(json);
            var ranked = new List<(Guid Id, double Score)>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idProp)) continue;
                var score = item.TryGetProperty("score", out var s) ? s.GetDouble() : 1.0;
                if (Guid.TryParse(idProp.GetString(), out var guid))
                    ranked.Add((guid, score));
            }
            _logger.LogInformation("Gemini returned {Count} ranked items.", ranked.Count);
            return ranked;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini JSON. Extracted: {Json}", rawText[start..(end + 1)]);
            return [];
        }
    }

    // ── SemanticSearch ────────────────────────────────────────────────────
    public async Task<IReadOnlyList<(Guid Id, double Score)>> SemanticSearchAsync(
        string query,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int topN,
        CancellationToken ct = default)
    {
        // Truncate description để giảm kích thước prompt
        var productList = candidates
            .Select(p => new
            {
                id = p.Id.ToString(),
                name = p.Name,
                desc = p.Description.Length > 80 ? p.Description[..80] : p.Description
            })
            .ToList();

        if (productList.Count == 0) return [];

        var productJson = JsonSerializer.Serialize(productList);
        var prompt =
            $"E-commerce product search. Query: \"{query}\"\n\n" +
            $"Products:\n{productJson}\n\n" +
            $"Return a JSON array of up to {topN} most relevant products.\n" +
            "Format: [{\"id\":\"<exact-id>\",\"score\":0.95},...]\n" +
            "Rules: copy IDs exactly, sort by relevance descending, return [] if none match.\n" +
            "Output JSON array only, no markdown, no explanation.";

        try
        {
            var rawText = await CallGeminiAsync(prompt, 1024, ct);
            return ParseRankedJson(rawText);
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SemanticSearch.");
            throw new ServiceUnavailableException("Tìm kiếm AI thất bại. Vui lòng thử lại sau.");
        }
    }

    // ── GetRecommendations ────────────────────────────────────────────────
    public async Task<IReadOnlyList<(Guid Id, double Score)>> GetRecommendationsAsync(
        (Guid Id, string Name, string Description) source,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int count,
        CancellationToken ct = default)
    {
        var candidateList = candidates
            .Select(p => new
            {
                id = p.Id.ToString(),
                name = p.Name,
                desc = p.Description.Length > 80 ? p.Description[..80] : p.Description
            })
            .ToList();

        if (candidateList.Count == 0) return [];

        var sourceObj = new { id = source.Id.ToString(), name = source.Name };
        var prompt =
            $"E-commerce recommendation. Source product: {JsonSerializer.Serialize(sourceObj)}\n\n" +
            $"Candidate products:\n{JsonSerializer.Serialize(candidateList)}\n\n" +
            $"Return a JSON array of up to {count} most similar products.\n" +
            "Format: [{\"id\":\"<exact-id>\",\"score\":0.92},...]\n" +
            "Rules: copy IDs exactly, sort by similarity descending, return [] if none match.\n" +
            "Output JSON array only, no markdown, no explanation.";

        try
        {
            var rawText = await CallGeminiAsync(prompt, 1024, ct);
            return ParseRankedJson(rawText);
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetRecommendations.");
            throw new ServiceUnavailableException("Gợi ý sản phẩm thất bại. Vui lòng thử lại sau.");
        }
    }

    // ── GenerateDescription ───────────────────────────────────────────────
    public async Task<string> GenerateProductDescriptionAsync(
        string productName, string categoryName, CancellationToken ct = default)
    {
        var prompt =
            $"Viết mô tả sản phẩm thương mại điện tử bằng tiếng Việt, 2-3 câu, nêu bật lợi ích chính.\n" +
            $"Sản phẩm: {productName}\nDanh mục: {categoryName}\n" +
            "Chỉ trả về nội dung mô tả, không có tiêu đề hay ghi chú thêm.";

        return await CallGeminiAsync(prompt, 512, ct);
    }
}
