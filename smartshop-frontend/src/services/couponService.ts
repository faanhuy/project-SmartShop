import api from './api';
import type { ApiResponse } from '../types/auth';

export interface ValidateCouponResult {
  discountAmount: number;
  finalAmount: number;
  originalAmount: number;
}

// DiscountType: 1 = Percentage, 2 = FixedAmount
export interface CouponDto {
  id: string;
  code: string;
  discountType: 1 | 2;
  discountValue: number;
  minOrderValue: number;
  maxUsage: number;
  usedQuantity: number;
  expiresAt: string;
  description: string | null;
}

export interface CreateCouponRequest {
  code: string;
  discountType: 1 | 2;
  discountValue: number;
  minOrderValue: number;
  maxUsage: number;
  expiresAt: string;
  description?: string;
}

export const couponService = {
  async validate(code: string, orderTotal: number): Promise<ValidateCouponResult> {
    const response = await api.post<ApiResponse<ValidateCouponResult>>('/coupons/validate', {
      code,
      orderTotal,
    });
    return response.data.data!;
  },

  async getAll(): Promise<CouponDto[]> {
    const response = await api.get<ApiResponse<CouponDto[]>>('/coupons');
    return response.data.data!;
  },

  async create(req: CreateCouponRequest): Promise<CouponDto> {
    const response = await api.post<ApiResponse<CouponDto>>('/coupons', req);
    return response.data.data!;
  },

  async remove(code: string): Promise<void> {
    await api.delete(`/coupons/${code}`);
  },
};
