import api from './api';
import type { ApiResponse } from '../types/auth';
import type { Store, StoreInventory, StockInfo, CreateStoreRequest, UpdateStoreRequest } from '../types/store';

export const storeService = {
  getStores: async (): Promise<Store[]> => {
    const { data } = await api.get<ApiResponse<Store[]>>('/stores');
    return data.data ?? [];
  },

  getStoreById: async (id: string): Promise<Store> => {
    const { data } = await api.get<ApiResponse<Store>>(`/stores/${id}`);
    return data.data!;
  },

  getProductStock: async (storeId: string, productId: string): Promise<StockInfo> => {
    const { data } = await api.get<ApiResponse<StockInfo>>(
      `/stores/${storeId}/products/${productId}/stock`,
    );
    return data.data!;
  },

  // Admin endpoints
  getStoreInventory: async (storeId: string): Promise<StoreInventory[]> => {
    const { data } = await api.get<ApiResponse<StoreInventory[]>>(
      `/admin/stores/${storeId}/inventory`,
    );
    return data.data ?? [];
  },

  getLowStockProducts: async (storeId: string): Promise<StoreInventory[]> => {
    const { data } = await api.get<ApiResponse<StoreInventory[]>>(
      `/admin/stores/${storeId}/inventory/low-stock`,
    );
    return data.data ?? [];
  },

  updateStoreInventory: async (
    storeId: string,
    productId: string,
    quantity: number,
  ): Promise<StoreInventory> => {
    const { data } = await api.patch<ApiResponse<StoreInventory>>(
      `/admin/stores/${storeId}/inventory/${productId}`,
      { quantity },
    );
    return data.data!;
  },

  createStore: async (body: CreateStoreRequest): Promise<Store> => {
    const { data } = await api.post<ApiResponse<Store>>('/admin/stores', body);
    return data.data!;
  },

  updateStore: async (id: string, body: UpdateStoreRequest): Promise<Store> => {
    const { data } = await api.put<ApiResponse<Store>>(`/admin/stores/${id}`, body);
    return data.data!;
  },
};
