export interface ProductDto {
  id: string;
  name: string;
  description: string;
  price: number;
  originalPrice: number;
  slug: string;
  imageUrl: string | null;
  isActive: boolean;
  categoryId: string;
  createdAt: string;
}

export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  imageUrl: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  categoryId: string;
  slug: string;
  imageUrl?: string | null;
}

export interface UpdateProductRequest {
  name: string;
  description: string;
  price: number;
  originalPrice?: number | null;
  imageUrl: string | null;
}
