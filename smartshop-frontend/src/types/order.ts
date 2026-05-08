export interface OrderItemDto {
  productId: string;
  productName: string;
  productImageUrl: string | null;
  quantity: number;
  unitPrice: number;
  subTotal: number;
}

export type PaymentMethod = 'COD' | 'VNPay' | 'BankTransfer';
export type PaymentStatus = 'Pending' | 'Paid' | 'Failed' | 'Refunded';

export interface OrderDto {
  id: string;
  userId: string;
  userName: string;
  status: string;
  totalAmount: number;
  shippingAddress: string;
  notes: string | null;
  items: OrderItemDto[];
  createdAt: string;
  paymentMethod?: PaymentMethod;
  paymentStatus?: PaymentStatus;
  paidAt?: string | null;
  vnpayTransactionId?: string | null;
}

export interface PlaceOrderRequest {
  shippingAddress: string;
  notes?: string;
  couponCode?: string;
  paymentMethod?: PaymentMethod;
  storeId?: string;
}

export interface AddressDto {
  id: string;
  label: string;
  recipientName: string;
  phone: string;
  street: string;
  ward?: string;
  district: string;
  city: string;
  isDefault: boolean;
  createdAt: string;
}

export const ORDER_STATUSES = [
  { value: 1, label: 'Chờ xác nhận', key: 'Pending',    color: 'bg-yellow-100 text-yellow-700' },
  { value: 2, label: 'Đã xác nhận',  key: 'Confirmed',  color: 'bg-blue-100 text-blue-700'    },
  { value: 3, label: 'Đang xử lý',   key: 'Processing', color: 'bg-purple-100 text-purple-700'},
  { value: 4, label: 'Đang giao',    key: 'Shipped',    color: 'bg-indigo-100 text-indigo-700'},
  { value: 5, label: 'Đã giao',      key: 'Delivered',  color: 'bg-green-100 text-green-700'  },
  { value: 6, label: 'Đã hủy',       key: 'Cancelled',  color: 'bg-red-100 text-red-700'      },
  { value: 7, label: 'Hoàn tiền',    key: 'Refunded',   color: 'bg-gray-100 text-gray-700'    },
] as const;

export type OrderStatusValue = (typeof ORDER_STATUSES)[number]['value'];

/** Chuẩn hoá status từ bất kỳ format nào backend trả về:
 *  - C# enum name : "Pending", "Confirmed", ...
 *  - Số nguyên    : 1, 2, ...
 *  - Numeric string: "1", "2", ...
 *  - Label tiếng Việt: "Chờ xác nhận", ...
 */
export function resolveOrderStatus(status: string | number): OrderStatusValue {
  if (typeof status === 'number') return status as OrderStatusValue;
  const n = Number(status);
  return (
    ORDER_STATUSES.find((s) => s.key   === status)?.value ??
    ORDER_STATUSES.find((s) => s.label === status)?.value ??
    (!Number.isNaN(n) ? ORDER_STATUSES.find((s) => s.value === n)?.value : undefined) ??
    1
  ) as OrderStatusValue;
}
