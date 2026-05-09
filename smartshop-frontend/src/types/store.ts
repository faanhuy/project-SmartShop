export interface Store {
  id: string;
  name: string;
  address: string;
  phone: string;
  isActive?: boolean;
  provinceId?: number;
  wardId?: number;
  provinceName?: string;
  wardName?: string;
  street?: string;
}

export interface CreateStoreRequest {
  name: string;
  address: string;
  phone: string;
  provinceId?: number;
  wardId?: number;
  provinceName?: string;
  wardName?: string;
  street?: string;
}

export interface UpdateStoreRequest {
  name: string;
  address: string;
  phone: string;
  isActive: boolean;
  provinceId?: number;
  wardId?: number;
  provinceName?: string;
  wardName?: string;
  street?: string;
}

export interface StoreInventory {
  productId: string;
  productName: string;
  quantity: number;
  lowStockThreshold: number;
}

export interface StockInfo {
  productId: string;
  storeId: string;
  quantity: number;
}
