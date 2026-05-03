import api from '@/services/api';
import type { ApiResponse } from '@/types/auth';

export const paymentService = {
  createVNPayUrl: async (orderId: string): Promise<string> => {
    const { data } = await api.post<ApiResponse<string>>('/payments/vnpay/create', { orderId });
    return data.data!;
  },
};
