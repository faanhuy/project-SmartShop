import api from './api';
import type { ApiResponse } from '../types/auth';
import type {
  ProductSize,
  EffectivePriceItem,
  BulkEffectivePricesResponse,
  SizeCategory,
  SizeDto,
  CreateSizeRequest,
  UpdateSizeRequest,
} from '../types/size';

export const sizeService = {
  // Product-specific sizes (for product management)
  getProductSizes: async (productId: string): Promise<ProductSize[]> => {
    const { data } = await api.get<ApiResponse<ProductSize[]>>(`/products/${productId}/sizes`);
    return data.data ?? [];
  },

  getBulkEffectivePrices: async (
    storeId: string,
    items: { productId: string; sizeId: string | null }[],
  ): Promise<EffectivePriceItem[]> => {
    const { data } = await api.post<ApiResponse<BulkEffectivePricesResponse>>(
      '/price-campaigns/bulk-effective-prices',
      { storeId, items },
    );
    return data.data?.prices ?? [];
  },

  addSize: async (productId: string, sizeLabel: string, displayOrder: number): Promise<ProductSize> => {
    const { data } = await api.post<ApiResponse<ProductSize>>(
      `/admin/products/${productId}/sizes`,
      { sizeLabel, displayOrder },
    );
    return data.data!;
  },

  updateSize: async (productId: string, sizeId: string, sizeLabel: string, displayOrder: number): Promise<ProductSize> => {
    const { data } = await api.put<ApiResponse<ProductSize>>(
      `/admin/products/${productId}/sizes/${sizeId}`,
      { sizeLabel, displayOrder },
    );
    return data.data!;
  },

  deleteSize: async (productId: string, sizeId: string): Promise<void> => {
    await api.delete(`/admin/products/${productId}/sizes/${sizeId}`);
  },

  setProductSizes: async (productId: string, sizeIds: string[]): Promise<ProductSize[]> => {
    const { data } = await api.put<ApiResponse<ProductSize[]>>(
      `/admin/products/${productId}/sizes`,
      { sizeIds },
    );
    return data.data ?? [];
  },

  // Size Master Management (admin)
  getByCategory: async (category?: SizeCategory): Promise<SizeDto[]> => {
    const params = category ? { category } : {};
    const { data } = await api.get<ApiResponse<SizeDto[]>>('/sizes', { params });
    return data.data ?? [];
  },

  getAllAdmin: async (category?: SizeCategory): Promise<SizeDto[]> => {
    const params = category ? { category } : {};
    const { data } = await api.get<ApiResponse<SizeDto[]>>('/admin/sizes', { params });
    return data.data ?? [];
  },

  createSize: async (req: CreateSizeRequest): Promise<SizeDto> => {
    const { data } = await api.post<ApiResponse<SizeDto>>('/admin/sizes', req);
    return data.data!;
  },

  updateMasterSize: async (id: string, req: UpdateSizeRequest): Promise<SizeDto> => {
    const { data } = await api.put<ApiResponse<SizeDto>>(`/admin/sizes/${id}`, req);
    return data.data!;
  },

  deleteMasterSize: async (id: string): Promise<void> => {
    await api.delete(`/admin/sizes/${id}`);
  },
};
