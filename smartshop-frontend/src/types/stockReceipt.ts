export type ReceiptStatus = 'Pending' | 'Completed' | 'Cancelled';

export interface StockReceiptItemDto {
  id: string;
  productId: string;
  productName: string;
  sizeId: string | null;
  sizeLabel: string | null;
  quantity: number;
  notes: string | null;
}

export interface StockReceiptDto {
  id: string;
  receiptNumber: string;
  storeId: string;
  receiptDate: string;
  notes: string | null;
  status: ReceiptStatus;
  createdAt: string;
}

export interface StockReceiptDetailDto extends StockReceiptDto {
  items: StockReceiptItemDto[];
}

export interface CreateStockReceiptItemRequest {
  productId: string;
  sizeId?: string | null;
  quantity: number;
  notes?: string | null;
}

export interface CreateStockReceiptRequest {
  storeId: string;
  receiptDate: string;
  notes?: string | null;
  items: CreateStockReceiptItemRequest[];
}

export interface UpdateStockReceiptRequest {
  receiptDate: string;
  notes?: string | null;
  items: CreateStockReceiptItemRequest[];
}
