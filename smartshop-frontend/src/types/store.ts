export interface Store {
  id: string;
  name: string;
  address: string;
  phone: string;
  isActive?: boolean;
}

export interface CreateStoreRequest {
  name: string;
  address: string;
  phone: string;
}

export interface UpdateStoreRequest {
  name: string;
  address: string;
  phone: string;
  isActive: boolean;
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
