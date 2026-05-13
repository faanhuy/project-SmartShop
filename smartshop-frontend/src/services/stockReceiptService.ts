import api from './api';
import type { ApiResponse } from '../types/auth';
import type {
  StockReceiptDto,
  StockReceiptDetailDto,
  CreateStockReceiptRequest,
  UpdateStockReceiptRequest,
  ReceiptStatus,
} from '../types/stockReceipt';
import type { PagedResult } from '../types/product';

export const stockReceiptService = {
  create: async (req: CreateStockReceiptRequest): Promise<StockReceiptDto> => {
    const { data } = await api.post<ApiResponse<StockReceiptDto>>('/admin/stock-receipts', req);
    return data.data!;
  },

  getList: async (
    storeId: string,
    page = 1,
    pageSize = 20,
    status?: ReceiptStatus,
  ): Promise<PagedResult<StockReceiptDto>> => {
    const params: Record<string, any> = { storeId, page, pageSize };
    if (status) params.status = status;
    const { data } = await api.get<ApiResponse<PagedResult<StockReceiptDto>>>('/admin/stock-receipts', { params });
    return data.data!;
  },

  getById: async (id: string): Promise<StockReceiptDetailDto> => {
    const { data } = await api.get<ApiResponse<StockReceiptDetailDto>>(`/admin/stock-receipts/${id}`);
    return data.data!;
  },

  complete: async (id: string): Promise<StockReceiptDto> => {
    const { data } = await api.post<ApiResponse<StockReceiptDto>>(`/admin/stock-receipts/${id}/complete`);
    return data.data!;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/admin/stock-receipts/${id}`);
  },

  cancel: async (id: string): Promise<StockReceiptDto> => {
    const { data } = await api.post<ApiResponse<StockReceiptDto>>(`/admin/stock-receipts/${id}/cancel`);
    return data.data!;
  },

  update: async (id: string, req: UpdateStockReceiptRequest): Promise<StockReceiptDetailDto> => {
    const { data } = await api.put<ApiResponse<StockReceiptDetailDto>>(`/admin/stock-receipts/${id}`, req);
    return data.data!;
  },
};
