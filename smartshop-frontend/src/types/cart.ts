export interface CartItemDto {
  productId: string;
  productName: string;
  productImageUrl: string | null;
  quantity: number;
  unitPrice: number;
  originalUnitPrice: number | null;
  subTotal: number;
  sizeId: string | null;
  sizeLabel: string | null;
}

export interface CartDto {
  id: string;
  userId: string;
  items: CartItemDto[];
  totalAmount: number;
}
