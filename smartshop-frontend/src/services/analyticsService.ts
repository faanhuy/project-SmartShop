import api from './api';
import type { ApiResponse } from '../types/auth';

export interface RevenueSummaryDto {
  totalRevenue: number;
  totalOrders: number;
  newCustomers: number;
  averageOrderValue: number;
  revenueGrowthPercent: number;
}

export interface RevenueByDateDto {
  date: string;
  revenue: number;
  orderCount: number;
}

export interface TopProductDto {
  productId: string;
  productName: string;
  totalQuantity: number;
  totalRevenue: number;
}

export interface OrderStatusBreakdownDto {
  status: string;
  count: number;
}

export const analyticsService = {
  getSummary: async (from: string, to: string): Promise<RevenueSummaryDto> => {
    const { data } = await api.get<ApiResponse<RevenueSummaryDto>>('/admin/analytics/summary', {
      params: { from, to },
    });
    return data.data;
  },

  getRevenueByDate: async (from: string, to: string): Promise<RevenueByDateDto[]> => {
    const { data } = await api.get<ApiResponse<RevenueByDateDto[]>>('/admin/analytics/revenue', {
      params: { from, to },
    });
    return data.data;
  },

  getTopProducts: async (from: string, to: string, limit = 5): Promise<TopProductDto[]> => {
    const { data } = await api.get<ApiResponse<TopProductDto[]>>('/admin/analytics/top-products', {
      params: { from, to, limit },
    });
    return data.data;
  },

  getOrderStatusBreakdown: async (): Promise<OrderStatusBreakdownDto[]> => {
    const { data } = await api.get<ApiResponse<OrderStatusBreakdownDto[]>>(
      '/admin/analytics/order-status',
    );
    return data.data;
  },
};
