using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartShop.Infrastructure.Services;

/// <summary>
/// Embedding background service đã được thay thế bởi Claude direct ranking.
/// Không cần lưu vector vào DB nữa — tìm kiếm gọi trực tiếp Claude API.
/// </summary>
public class EmbeddingBackgroundService(
    ILogger<EmbeddingBackgroundService> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmbeddingBackgroundService: disabled — using Claude direct ranking.");
        return Task.CompletedTask;
    }
}

