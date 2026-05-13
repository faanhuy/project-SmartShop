using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class PriceCampaignItem
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? SizeId { get; private set; }
    public PriceRuleType RuleType { get; private set; }
    public decimal DiscountValue { get; private set; }

    private PriceCampaignItem() { }

    public static PriceCampaignItem Create(
        Guid campaignId, Guid productId, Guid? sizeId,
        PriceRuleType ruleType, decimal discountValue)
    {
        if (ruleType == PriceRuleType.Coefficient && (discountValue <= 0 || discountValue > 10))
            throw new ArgumentException("Coefficient phải nằm trong khoảng (0, 10].", nameof(discountValue));

        if (ruleType == PriceRuleType.FixedPrice && discountValue < 0)
            throw new ArgumentException("FixedPrice không được âm.", nameof(discountValue));

        return new PriceCampaignItem
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ProductId = productId,
            SizeId = sizeId,
            RuleType = ruleType,
            DiscountValue = discountValue
        };
    }

    public decimal ComputePrice(decimal basePrice) => RuleType switch
    {
        PriceRuleType.Coefficient => basePrice * DiscountValue,
        PriceRuleType.FixedPrice => DiscountValue,
        _ => basePrice
    };
}
