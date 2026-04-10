import api from './api';
import type { ProductDto } from '../types/product';
import type { GenerateDescriptionRequest, SemanticSearchRequest, SemanticSearchResultDto } from '../types/ai';
import axios from 'axios';

interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: string[];
}

function extractErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err) && err.response?.data) {
    const body = err.response.data as ApiResponse<unknown>;
    if (body.errors?.length) return body.errors[0];
    if (body.message) return body.message;
  }
  return fallback;
}

export const aiService = {
  semanticSearch: async (request: SemanticSearchRequest): Promise<SemanticSearchResultDto[]> => {
    const { data } = await api.post<ApiResponse<SemanticSearchResultDto[]>>('/ai/search', request);
    return data.data ?? [];
  },

  getRecommendations: async (productId: string, count = 5): Promise<ProductDto[]> => {
    const { data } = await api.get<ApiResponse<ProductDto[]>>(
      `/ai/recommendations/${productId}`,
      { params: { count } }
    );
    return data.data ?? [];
  },

  generateDescription: async (request: GenerateDescriptionRequest): Promise<string> => {
    const { data } = await api.post<ApiResponse<string>>('/ai/generate-description', request);
    return data.data ?? '';
  },

  extractErrorMessage,
};
