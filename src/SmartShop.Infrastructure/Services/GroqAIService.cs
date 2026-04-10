using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;

namespace SmartShop.Infrastructure.Services;

/// <summary>
/// AI service dùng Groq API (free tier: 6000 req/min, 500K tokens/day).
/// Model: llama-3.3-70b-versatile — hiểu tiếng Việt tốt, phản hồi nhanh.
/// </summary>
public class GroqAIService : ISemanticKernelService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<GroqAIService> _logger;

    public GroqAIService(
        IConfiguration configuration,
        ILogger<GroqAIService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        var key = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");
        _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

        _http = httpClientFactory.CreateClient("Groq");
        _http.BaseAddress = new Uri("https://api.groq.com");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
    }

    // ── Core: gọi Groq Chat Completions API ──────────────────────────────
    private async Task<string> CallGroqAsync(
        string systemPrompt, string userPrompt, int maxTokens, CancellationToken ct)
    {
        var body = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   }
            },
            temperature = 0.0,   // deterministic — quan trọng cho ranking
            max_tokens  = maxTokens
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/openai/v1/chat/completions", body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network error calling Groq API.");
            throw new ServiceUnavailableException("Không thể kết nối đến dịch vụ AI. Vui lòng thử lại sau.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody  = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Groq API HTTP {Status}: {Body}", statusCode, errorBody);

            if (statusCode == 429)
                throw new ServiceUnavailableException("Dịch vụ AI tạm thời không khả dụng (rate limit). Vui lòng thử lại sau.");

            throw new ServiceUnavailableException($"Dịch vụ AI lỗi: HTTP {statusCode}");
        }

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    // ── Parse JSON array [{id, score}] từ response ────────────────────────
    private IReadOnlyList<(Guid Id, double Score)> ParseRankedJson(string rawText)
    {
        _logger.LogDebug("Groq raw response: {Text}", rawText);

        var start = rawText.IndexOf('[');
        var end   = rawText.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            _logger.LogWarning("Groq response has no JSON array. Raw: {Text}", rawText);
            return [];
        }

        try
        {
            using var doc    = JsonDocument.Parse(rawText[start..(end + 1)]);
            var ranked = new List<(Guid Id, double Score)>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idProp)) continue;
                var score = item.TryGetProperty("score", out var s) ? s.GetDouble() : 1.0;
                if (Guid.TryParse(idProp.GetString(), out var guid))
                    ranked.Add((guid, score));
            }
            _logger.LogInformation("Groq returned {Count} ranked items.", ranked.Count);
            return ranked;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Groq JSON.");
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
        var productList = candidates
            .Select(p => new
            {
                id   = p.Id.ToString(),
                name = p.Name,
                desc = p.Description.Length > 100 ? p.Description[..100] : p.Description
            })
            .ToList();

        if (productList.Count == 0) return [];

        const string system =
            "You are a Vietnamese e-commerce semantic search engine with deep product knowledge.\n\n" +
            "BRAND RELATIONSHIPS (apply when user searches by brand):\n" +
            "- Xiaomi brand family: Xiaomi, Redmi, POCO, Black Shark\n" +
            "- Samsung brand family: Samsung, Galaxy\n" +
            "- Apple brand family: Apple, iPhone, MacBook, AirPods, iPad\n" +
            "- ASUS brand family: ASUS, ROG, VivoBook, ZenBook\n" +
            "- Lenovo brand family: Lenovo, ThinkPad, IdeaPad, Legion\n\n" +
            "VIETNAMESE SYNONYMS:\n" +
            "- điện thoại / điện thoại thông minh / smartphone → phone products\n" +
            "- quần áo / thời trang / trang phục → fashion/clothing\n" +
            "- tai nghe / headphones / earphones / earbuds → audio\n" +
            "- máy tính / laptop / notebook → computer products\n" +
            "- phụ kiện → accessories\n\n" +
            "Always respond with a valid JSON array only, no explanation.";

        var user =
            $"Search query: \"{query}\"\n\n" +
            $"Product catalog:\n{JsonSerializer.Serialize(productList)}\n\n" +
            $"Find the top {topN} most relevant products. Apply brand knowledge and synonyms.\n" +
            "Return JSON array: [{\"id\":\"<exact-guid>\",\"score\":0.95}, ...]\n" +
            "Rules:\n" +
            "- Copy IDs EXACTLY as provided\n" +
            "- score 0.0-1.0: brand match=0.9+, category match=0.6+, partial=0.3+\n" +
            "- Sort by score descending\n" +
            "- Include all products with score >= 0.3\n" +
            "- Return [] only if truly nothing is relevant\n" +
            "Output JSON array only, no text before or after.";

        try
        {
            var rawText = await CallGroqAsync(system, user, 1024, ct);
            return ParseRankedJson(rawText);
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Groq SemanticSearch.");
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
                id   = p.Id.ToString(),
                name = p.Name,
                desc = p.Description.Length > 100 ? p.Description[..100] : p.Description
            })
            .ToList();

        if (candidateList.Count == 0) return [];

        const string system =
            "You are a Vietnamese e-commerce recommendation engine. " +
            "Find products that are similar, complementary, or in the same category. " +
            "Always respond with a valid JSON array only, no explanation.";

        var user =
            $"Source product: {JsonSerializer.Serialize(new { id = source.Id.ToString(), name = source.Name })}\n\n" +
            $"Candidates:\n{JsonSerializer.Serialize(candidateList)}\n\n" +
            $"Return the top {count} most similar/related products as JSON array:\n" +
            "[{\"id\":\"<exact-guid>\",\"score\":0.92}, ...]\n" +
            "Rules:\n" +
            "- Copy IDs exactly as given\n" +
            "- score: 0.0-1.0 based on similarity\n" +
            "- Sort by score descending\n" +
            "Output JSON array only.";

        try
        {
            var rawText = await CallGroqAsync(system, user, 1024, ct);
            return ParseRankedJson(rawText);
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Groq GetRecommendations.");
            throw new ServiceUnavailableException("Gợi ý sản phẩm thất bại. Vui lòng thử lại sau.");
        }
    }

    // ── GenerateDescription ───────────────────────────────────────────────
    public async Task<string> GenerateProductDescriptionAsync(
        string productName, string categoryName, CancellationToken ct = default)
    {
        const string system =
            "You are a Vietnamese e-commerce copywriter. " +
            "Write compelling 2-3 sentence product descriptions in Vietnamese. " +
            "Highlight key benefits. Be concise and persuasive. " +
            "Return only the description text, no titles or extra commentary.";

        var user = $"Product: {productName}\nCategory: {categoryName}";

        return await CallGroqAsync(system, user, 512, ct);
    }
}
