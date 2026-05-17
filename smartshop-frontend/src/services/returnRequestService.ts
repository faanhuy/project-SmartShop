import api from '@/services/api';
import type {
  ReturnRequestDto,
  CreateReturnRequestRequest,
  ApproveReturnRequest,
  RejectReturnRequest,
} from '@/types/returnRequest';
import { ReturnStatus, RETURN_REASON_INT, RETURN_STATUS_INT } from '@/types/returnRequest';
import type { ApiResponse } from '@/types/auth';

const returnRequestService = {
  create: async (orderId: string, data: CreateReturnRequestRequest): Promise<ReturnRequestDto> => {
    const { data: response } = await api.post<ApiResponse<ReturnRequestDto>>(
      `/orders/${orderId}/return-request`,
      { ...data, reason: RETURN_REASON_INT[data.reason] }
    );
    return response.data;
  },

  getMyReturnRequests: async (): Promise<ReturnRequestDto[]> => {
    const { data } = await api.get<ApiResponse<ReturnRequestDto[]>>('/orders/return-requests');
    return data.data;
  },

  // Admin
  getAll: async (status?: ReturnStatus): Promise<ReturnRequestDto[]> => {
    const statusInt = status ? RETURN_STATUS_INT[status] : undefined;
    const { data } = await api.get<ApiResponse<ReturnRequestDto[]>>('/admin/return-requests', {
      params: { ...(statusInt ? { status: statusInt } : {}) },
    });
    return data.data;
  },

  approve: async (id: string, payload: ApproveReturnRequest): Promise<ReturnRequestDto> => {
    const { data } = await api.post<ApiResponse<ReturnRequestDto>>(
      `/admin/return-requests/${id}/approve`,
      payload
    );
    return data.data;
  },

  reject: async (id: string, payload: RejectReturnRequest): Promise<ReturnRequestDto> => {
    const { data } = await api.post<ApiResponse<ReturnRequestDto>>(
      `/admin/return-requests/${id}/reject`,
      payload
    );
    return data.data;
  },
};

export default returnRequestService;
