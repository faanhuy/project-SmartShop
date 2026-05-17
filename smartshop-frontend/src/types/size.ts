export interface ProductSize {
  id: string;
  sizeLabel: string;
  displayOrder: number;
  isActive: boolean;
  sizeId?: string | null;
}

export interface EffectivePriceItem {
  productId: string;
  sizeId: string | null;
  basePrice: number;
  effectivePrice: number;
  hasPromotion: boolean;
}

// Size Master Management types
export type SizeCategory = 'DrinkSize' | 'FoodPortion' | 'MealSize' | 'Custom';

export const SIZE_CATEGORY_LABELS: Record<SizeCategory, string> = {
  DrinkSize: 'Đồ uống',
  FoodPortion: 'Khẩu phần ăn',
  MealSize: 'Suất ăn / Combo',
  Custom: 'Tùy chỉnh',
};

export interface SizeDto {
  id: string;
  category: SizeCategory;
  label: string;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateSizeRequest {
  category: SizeCategory;
  label: string;
  displayOrder: number;
}

export interface UpdateSizeRequest {
  label: string;
  displayOrder: number;
}
