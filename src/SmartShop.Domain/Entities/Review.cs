using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class Review : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; }

    public User? User { get; private set; }
    public Product? Product { get; private set; }

    private Review() { }

    public static Review Create(Guid userId, Guid productId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Đánh giá phải từ 1 đến 5 sao.");

        return new Review
        {
            UserId = userId,
            ProductId = productId,
            Rating = rating,
            Comment = comment
        };
    }

    public void Approve()
    {
        IsApproved = true;
    }

    public void Update(int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Đánh giá phải từ 1 đến 5 sao.");

        Rating = rating;
        Comment = comment;
    }
}
