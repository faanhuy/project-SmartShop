using System.Text.Json;

namespace SmartShop.Domain.Entities;

public class ProductEmbedding
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string EmbeddingJson { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }

    public Product? Product { get; private set; }

    private ProductEmbedding() { }

    public static ProductEmbedding Create(Guid productId, float[] embedding)
    {
        return new ProductEmbedding
        {
            ProductId = productId,
            EmbeddingJson = JsonSerializer.Serialize(embedding),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public float[] GetEmbedding()
        => JsonSerializer.Deserialize<float[]>(EmbeddingJson)!;

    public void UpdateEmbedding(float[] embedding)
    {
        EmbeddingJson = JsonSerializer.Serialize(embedding);
        GeneratedAt = DateTime.UtcNow;
    }
}
