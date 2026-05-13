import api from './api';
import type { CartDto } from '../types/cart';
import type { ApiResponse } from '../types/auth';

export const cartService = {
  getCart: async (): Promise<CartDto> => {
    const { data } = await api.get<ApiResponse<CartDto>>('/cart');
    return data.data;
  },

  addToCart: async (productId: string, quantity: number, sizeId?: string): Promise<CartDto> => {
    const { data } = await api.post<ApiResponse<CartDto>>('/cart/items', {
      productId,
      quantity,
      ...(sizeId ? { sizeId } : {}),
    });
    return data.data;
  },

  updateItem: async (productId: string, quantity: number, sizeId?: string): Promise<CartDto> => {
    const { data } = await api.put<ApiResponse<CartDto>>(`/cart/items/${productId}`, {
      quantity,
      ...(sizeId ? { sizeId } : {}),
    });
    return data.data;
  },

  removeItem: async (productId: string, sizeId?: string): Promise<CartDto> => {
    const { data } = await api.delete<ApiResponse<CartDto>>(`/cart/items/${productId}`, {
      params: sizeId ? { sizeId } : undefined,
    });
    return data.data;
  },

  clearCart: async (): Promise<void> => {
    await api.delete('/cart');
  },
};
