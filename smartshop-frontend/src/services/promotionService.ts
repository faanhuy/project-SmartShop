import api from './api';
import type { ApiResponse } from '../types/auth';
import type { PagedResult } from '../types/product';
import type {
  ComboPromotion,
  CreateComboRequest,
  CreatePriceCampaignRequest,
  PriceCampaignDto,
  PriceCampaignSummaryDto,
} from '../types/promotion';

export const promotionService = {
  // Public
  getActiveCombos: async (storeId: string): Promise<ComboPromotion[]> => {
    const { data } = await api.get<ApiResponse<ComboPromotion[]>>(
      '/promotions/combos/active',
      { params: { storeId } },
    );
    return data.data ?? [];
  },

  // Admin — Price Campaigns
  getPriceCampaigns: async (): Promise<PagedResult<PriceCampaignSummaryDto>> => {
    const { data } = await api.get<ApiResponse<PagedResult<PriceCampaignSummaryDto>>>(
      '/admin/price-campaigns',
    );
    return data.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },

  getPriceCampaignById: async (id: string): Promise<PriceCampaignDto> => {
    const { data } = await api.get<ApiResponse<PriceCampaignDto>>(`/admin/price-campaigns/${id}`);
    return data.data!;
  },

  createPriceCampaign: async (body: CreatePriceCampaignRequest): Promise<PriceCampaignDto> => {
    const { data } = await api.post<ApiResponse<PriceCampaignDto>>('/admin/price-campaigns', body);
    return data.data!;
  },

  updatePriceCampaign: async (
    id: string,
    body: CreatePriceCampaignRequest,
  ): Promise<PriceCampaignDto> => {
    const { data } = await api.put<ApiResponse<PriceCampaignDto>>(
      `/admin/price-campaigns/${id}`,
      body,
    );
    return data.data!;
  },

  deletePriceCampaign: async (id: string): Promise<void> => {
    await api.delete(`/admin/price-campaigns/${id}`);
  },

  // Admin — Combos
  getCombos: async (): Promise<PagedResult<ComboPromotion>> => {
    const { data } = await api.get<ApiResponse<PagedResult<ComboPromotion>>>(
      '/admin/promotions/combos',
    );
    return data.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },

  getComboById: async (id: string): Promise<ComboPromotion> => {
    const { data } = await api.get<ApiResponse<ComboPromotion>>(
      `/admin/promotions/combos/${id}`,
    );
    return data.data!;
  },

  createCombo: async (body: CreateComboRequest): Promise<ComboPromotion> => {
    const { data } = await api.post<ApiResponse<ComboPromotion>>(
      '/admin/promotions/combos',
      body,
    );
    return data.data!;
  },

  updateCombo: async (id: string, body: CreateComboRequest): Promise<ComboPromotion> => {
    const { data } = await api.put<ApiResponse<ComboPromotion>>(
      `/admin/promotions/combos/${id}`,
      body,
    );
    return data.data!;
  },

  deleteCombo: async (id: string): Promise<void> => {
    await api.delete(`/admin/promotions/combos/${id}`);
  },
};
