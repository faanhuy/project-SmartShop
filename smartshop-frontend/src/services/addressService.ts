import api from '@/services/api';
import type { AddressDto } from '@/types/order';
import type { ApiResponse } from '@/types/auth';

type CreateAddressRequest = Omit<AddressDto, 'id' | 'isDefault' | 'createdAt'>;

export const addressService = {
  getAll: async (): Promise<AddressDto[]> => {
    const { data } = await api.get<ApiResponse<AddressDto[]>>('/users/me/addresses');
    return data.data ?? [];
  },

  add: async (payload: CreateAddressRequest): Promise<AddressDto> => {
    const { data } = await api.post<ApiResponse<AddressDto>>('/users/me/addresses', payload);
    return data.data!;
  },

  update: async (id: string, payload: Partial<AddressDto>): Promise<AddressDto> => {
    const { data } = await api.put<ApiResponse<AddressDto>>(`/users/me/addresses/${id}`, payload);
    return data.data!;
  },

  remove: async (id: string): Promise<void> => {
    await api.delete(`/users/me/addresses/${id}`);
  },

  setDefault: async (id: string): Promise<void> => {
    await api.patch(`/users/me/addresses/${id}/default`);
  },
};
