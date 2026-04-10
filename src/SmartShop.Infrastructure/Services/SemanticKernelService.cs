#pragma warning disable CS0618 // ITextEmbeddingGenerationService obsolete warning - stable API, safe to use
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;

namespace SmartShop.Infrastructure.Services;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<SemanticKernelService> _logger;

    public SemanticKernelService(IConfiguration configuration, ILogger<SemanticKernelService> logger)
    {
        _logger = logger;

        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
        var embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-ada-002";
        var chatModel = configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAITextEmbeddingGeneration(embeddingModel, apiKey);
        builder.AddOpenAIChatCompletion(chatModel, apiKey);
        _kernel = builder.Build();

        _embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _chatService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var result = await _embeddingService.GenerateEmbeddingAsync(text, _kernel, ct);
            return result.ToArray();
        }
        catch (HttpOperationException ex) when (ex.Message.Contains("429") || ex.Message.Contains("insufficient_quota"))
        {
            _logger.LogWarning("OpenAI quota exceeded when generating embedding.");
            throw new ServiceUnavailableException("Dịch vụ AI tạm thời không khả dụng. Vui lòng nạp credits tại platform.openai.com.");
        }
    }

    public Task<IReadOnlyList<(Guid Id, double Score)>> SemanticSearchAsync(
        string query,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int topN,
        CancellationToken ct = default)
        => throw new NotSupportedException("SemanticKernelService is not the active AI provider.");

    public Task<IReadOnlyList<(Guid Id, double Score)>> GetRecommendationsAsync(
        (Guid Id, string Name, string Description) source,
        IEnumerable<(Guid Id, string Name, string Description)> candidates,
        int count,
        CancellationToken ct = default)
        => throw new NotSupportedException("SemanticKernelService is not the active AI provider.");

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

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        try
        {
            var result = await _chatService.GetChatMessageContentAsync(history, cancellationToken: ct);
            return result.Content ?? string.Empty;
        }
        catch (HttpOperationException ex) when (ex.Message.Contains("429") || ex.Message.Contains("insufficient_quota"))
        {
            _logger.LogWarning("OpenAI quota exceeded when generating description.");
            throw new ServiceUnavailableException("Dịch vụ AI tạm thời không khả dụng. Vui lòng nạp credits tại platform.openai.com.");
        }
    }
}
#pragma warning restore CS0618
