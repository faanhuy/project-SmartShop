export enum ReturnStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

export enum ReturnReason {
  Defective = 'Defective',
  NotAsDescribed = 'NotAsDescribed',
  WrongSize = 'WrongSize',
  ChangedMind = 'ChangedMind',
}

// Numeric values for sending to API (controller expects int)
export const RETURN_REASON_INT: Record<ReturnReason, number> = {
  [ReturnReason.Defective]: 1,
  [ReturnReason.NotAsDescribed]: 2,
  [ReturnReason.WrongSize]: 3,
  [ReturnReason.ChangedMind]: 4,
};

export const RETURN_STATUS_INT: Record<ReturnStatus, number> = {
  [ReturnStatus.Pending]: 1,
  [ReturnStatus.Approved]: 2,
  [ReturnStatus.Rejected]: 3,
};

export interface ReturnRequestDto {
  id: string;
  orderId: string;
  orderNumber: string;
  reason: ReturnReason;
  description?: string;
  evidenceImageUrl?: string;
  status: ReturnStatus;
  adminNote?: string;
  refundAmount: number;
  createdAt: string;
  userEmail?: string; // chỉ có trong admin view
}

export interface CreateReturnRequestRequest {
  reason: ReturnReason;
  description?: string;
  evidenceImageUrl?: string;
}

export interface ApproveReturnRequest {
  adminNote?: string;
}

export interface RejectReturnRequest {
  adminNote: string;
}

export const RETURN_REASON_LABELS: Record<ReturnReason, string> = {
  [ReturnReason.Defective]: 'Sản phẩm bị lỗi',
  [ReturnReason.NotAsDescribed]: 'Không đúng mô tả',
  [ReturnReason.WrongSize]: 'Kích cỡ không phù hợp',
  [ReturnReason.ChangedMind]: 'Thay đổi ý định',
};

export const RETURN_STATUS_LABELS: Record<ReturnStatus, string> = {
  [ReturnStatus.Pending]: 'Chờ xử lý',
  [ReturnStatus.Approved]: 'Đã duyệt',
  [ReturnStatus.Rejected]: 'Bị từ chối',
};

export const RETURN_STATUS_COLORS: Record<ReturnStatus, string> = {
  [ReturnStatus.Pending]: 'bg-yellow-100 text-yellow-700',
  [ReturnStatus.Approved]: 'bg-green-100 text-green-700',
  [ReturnStatus.Rejected]: 'bg-red-100 text-red-700',
};
