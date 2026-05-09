import api from '@/services/api';
import type { Province, Ward } from '@/types/geography';

export const geographyService = {
  getProvinces: async (): Promise<Province[]> => {
    const response = await api.get<{ data: Province[] }>('/geography/provinces');
    return response.data.data;
  },

  getWardsByProvince: async (provinceId: number): Promise<Ward[]> => {
    const response = await api.get<{ data: Ward[] }>(`/geography/provinces/${provinceId}/wards`);
    return response.data.data;
  },
};
