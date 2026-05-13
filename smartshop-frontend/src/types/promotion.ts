export interface PriceCampaignItem {
  id: string;
  productId: string;
  productName: string;
  sizeId: string | null;
  sizeLabel: string | null;
  ruleType: number; // 1=Coefficient, 2=FixedPrice
  discountValue: number;
}

export interface PriceCampaignDto {
  id: string;
  name: string;
  startsAt: string;
  endsAt: string;
  appliesToAll: boolean;
  isActive: boolean;
  stores: { id: string; name: string }[];
  items: PriceCampaignItem[];
}

export interface PriceCampaignSummaryDto {
  id: string;
  name: string;
  startsAt: string;
  endsAt: string;
  appliesToAll: boolean;
  isActive: boolean;
  storeCount: number;
  itemCount: number;
}

export interface CreatePriceCampaignRequest {
  name: string;
  startsAt: string;
  endsAt: string;
  appliesToAll: boolean;
  storeIds: string[];
  items: {
    productId: string;
    sizeId: string | null;
    ruleType: number;
    discountValue: number;
  }[];
}

export interface ComboPromotion {
  id: string;
  name: string;
  triggerProductId: string;
  triggerSizeId: string | null;
  triggerMinQuantity: number;
  rewardType: number; // 0=FreeProduct, 1=DiscountAmount
  rewardProductId: string | null;
  rewardSizeId: string | null;
  rewardQuantity: number | null;
  rewardAmount: number | null;
  storeId: string | null;
  startsAt: string | null;
  endsAt: string | null;
  isActive: boolean;
}

export interface CreateComboRequest {
  name: string;
  triggerProductId: string;
  triggerSizeId: string | null;
  triggerMinQuantity: number;
  rewardType: number;
  rewardProductId: string | null;
  rewardSizeId: string | null;
  rewardQuantity: number | null;
  rewardAmount: number | null;
  storeId: string | null;
  startsAt: string | null;
  endsAt: string | null;
}
