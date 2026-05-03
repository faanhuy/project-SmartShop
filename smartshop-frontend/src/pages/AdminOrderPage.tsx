import { Fragment, useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { FiChevronDown, FiChevronRight } from 'react-icons/fi';
import AdminLayout from '../components/AdminLayout';
import { orderService } from '../services/orderService';
import type { OrderDto, OrderStatusValue, PaymentMethod, PaymentStatus } from '../types/order';
import { ORDER_STATUSES, resolveOrderStatus } from '../types/order';
import { formatPrice, formatDateTime } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';
import Pagination from '../components/common/Pagination';

const PAGE_SIZE = 20;

const PAYMENT_METHOD_LABEL: Record<string, string> = {
  COD: 'COD',
  VNPay: 'VNPay',
  BankTransfer: 'ACB',
};

const PAYMENT_STATUS_CONFIG: Record<string, { label: string; cls: string }> = {
  Pending:   { label: 'Chờ TT',    cls: 'bg-yellow-100 text-yellow-700' },
  Paid:      { label: 'Đã TT',     cls: 'bg-green-100 text-green-700'   },
  Failed:    { label: 'Thất bại',  cls: 'bg-red-100 text-red-700'       },
  Refunded:  { label: 'Hoàn tiền', cls: 'bg-gray-100 text-gray-600'     },
};

function PaymentCell({ method, status }: { method?: PaymentMethod; status?: PaymentStatus }) {
  const methodLabel = method ? (PAYMENT_METHOD_LABEL[method] ?? method) : '—';
  const st = status ? PAYMENT_STATUS_CONFIG[status] : null;
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs font-medium text-gray-700">{methodLabel}</span>
      {st && (
        <span className={`inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium w-fit ${st.cls}`}>
          {st.label}
        </span>
      )}
    </div>
  );
}

function PaymentDetail({
  method, status, paidAt, transactionId,
}: {
  method?: PaymentMethod;
  status?: PaymentStatus;
  paidAt?: string | null;
  transactionId?: string | null;
}) {
  if (!method) return null;
  const methodLabel = PAYMENT_METHOD_LABEL[method] ?? method;
  const st = status ? PAYMENT_STATUS_CONFIG[status] : null;
  return (
    <div className="mb-3 inline-flex flex-wrap gap-x-6 gap-y-1 rounded-lg border border-rose-100 bg-white px-4 py-2.5 text-xs">
      <div className="flex items-center gap-1.5 text-gray-500">
        <span className="font-medium text-gray-700">Phương thức:</span>
        <span>{methodLabel}</span>
      </div>
      {st && (
        <div className="flex items-center gap-1.5 text-gray-500">
          <span className="font-medium text-gray-700">Thanh toán:</span>
          <span className={`px-1.5 py-0.5 rounded-full text-[10px] font-medium ${st.cls}`}>{st.label}</span>
        </div>
      )}
      {paidAt && (
        <div className="flex items-center gap-1.5 text-gray-500">
          <span className="font-medium text-gray-700">Thời gian TT:</span>
          <span>{new Date(paidAt).toLocaleString('vi-VN')}</span>
        </div>
      )}
      {transactionId && (
        <div className="flex items-center gap-1.5 text-gray-500">
          <span className="font-medium text-gray-700">Mã GD:</span>
          <span className="font-mono">{transactionId}</span>
        </div>
      )}
    </div>
  );
}

export default function AdminOrderPage() {
  const [allOrders,    setAllOrders]    = useState<OrderDto[]>([]);
  const [totalCount,   setTotalCount]   = useState(0);
  const [loading,      setLoading]      = useState(true);
  const [page,         setPage]         = useState(1);
  const [statusFilter, setStatusFilter] = useState<number>(0);
  const [updatingIds,  setUpdatingIds]  = useState<Set<string>>(new Set());
  const [expandedId,   setExpandedId]   = useState<string | null>(null);
  const isBusy = updatingIds.size > 0;

  const loadOrders = async (p: number, sf: number, showLoading = true) => {
    if (showLoading) setLoading(true);
    try {
      const result = await orderService.getAllOrders(p, PAGE_SIZE, sf || undefined);
      setAllOrders(result.items);
      setTotalCount(result.totalCount);
    } catch {
      toast.error('Không thể tải danh sách đơn giao.');
    } finally {
      if (showLoading) setLoading(false);
    }
  };

  useEffect(() => { loadOrders(page, statusFilter); }, [page, statusFilter]);

  const handleFilterChange = (value: number) => {
    setStatusFilter(value);
    setPage(1);
    setExpandedId(null);
  };

  const handleStatusChange = async (orderId: string, newStatus: OrderStatusValue) => {
    if (updatingIds.has(orderId)) return;

    const optimisticStatus =
      ORDER_STATUSES.find((s) => s.value === newStatus)?.key ?? String(newStatus);

    setUpdatingIds((prev) => {
      const next = new Set(prev);
      next.add(orderId);
      return next;
    });

    // Optimistic update để admin thấy thay đổi ngay trên đúng dòng đang thao tác.
    setAllOrders((prev) =>
      prev.map((order) =>
        order.id === orderId ? { ...order, status: optimisticStatus } : order
      )
    );

    try {
      await orderService.updateOrderStatus(orderId, newStatus);
      await loadOrders(page, statusFilter, false);
      toast.success('Đã cập nhật trạng thái đơn giao.');
    } catch {
      toast.error('Cập nhật trạng thái thất bại.');
      await loadOrders(page, statusFilter, false);
    } finally {
      setUpdatingIds((prev) => {
        const next = new Set(prev);
        next.delete(orderId);
        return next;
      });
    }
  };

  const totalPages = Math.ceil(totalCount / PAGE_SIZE) || 1;

  return (
    <AdminLayout title="Quản lý đơn giao">
      {/* Bộ lọc trạng thái */}
      <div className="flex gap-1.5 flex-wrap mb-4">
        <button
          onClick={() => handleFilterChange(0)}
          disabled={isBusy}
          className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
            statusFilter === 0
              ? 'bg-gray-900 text-white border-gray-900'
              : 'bg-white text-gray-600 border-gray-200 hover:border-gray-400'
          } disabled:opacity-50 disabled:cursor-not-allowed`}
        >
          Tất cả
        </button>
        {ORDER_STATUSES.map((s) => (
          <button
            key={s.value}
            onClick={() => handleFilterChange(s.value)}
            disabled={isBusy}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
              statusFilter === s.value
                ? 'bg-gray-900 text-white border-gray-900'
                : 'bg-white text-gray-600 border-gray-200 hover:border-gray-400'
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {s.label}
            {statusFilter === s.value && !loading && (
              <span className="ml-1.5 opacity-60">({totalCount})</span>
            )}
          </button>
        ))}
      </div>

      {isBusy && (
        <div className="mb-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800 flex items-center gap-2">
          <svg className="h-3.5 w-3.5 animate-spin text-amber-700" viewBox="0 0 24 24" fill="none">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
          </svg>
          Đang xử lý {updatingIds.size} đơn hàng. Vui lòng chờ hoàn tất.
        </div>
      )}

      <p className="text-xs text-gray-400 mb-3">Tổng {totalCount} đơn giao</p>

      {/* Bảng đơn hàng */}
      {loading ? (
        <div className="flex items-center justify-center h-64 text-gray-400">Đang tải...</div>
      ) : (
        <>
          <div className="bg-white rounded-xl border shadow-sm overflow-x-auto">
            {allOrders.length === 0 ? (
              <p className="text-center text-gray-400 py-12">Không có đơn giao nào.</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50 text-gray-500 text-xs uppercase border-b">
                  <tr>
                    <th className="w-8 px-3 py-3" />
                    <th className="px-4 py-3 text-left">Mã đơn</th>
                    <th className="px-4 py-3 text-left">Người đặt</th>
                    <th className="px-4 py-3 text-left hidden sm:table-cell">Địa chỉ</th>
                    <th className="px-4 py-3 text-right">Tổng tiền</th>
                    <th className="px-4 py-3 text-left hidden lg:table-cell">Thanh toán</th>
                    <th className="px-4 py-3 text-left hidden md:table-cell">Ngày đặt</th>
                    <th className="px-4 py-3 text-left">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {allOrders.map((order) => {
                    const statusVal  = resolveOrderStatus(order.status);
                    const statusInfo = ORDER_STATUSES.find((s) => s.value === statusVal);
                    const isExpanded = expandedId === order.id;

                    return (
                      <Fragment key={order.id}>
                        <tr
                          className="hover:bg-gray-50 cursor-pointer transition-colors"
                          onClick={() => setExpandedId(isExpanded ? null : order.id)}
                        >
                          <td className="px-3 py-3 text-gray-400">
                            {isExpanded ? <FiChevronDown size={14} /> : <FiChevronRight size={14} />}
                          </td>
                          <td className="px-4 py-3 font-mono text-xs text-gray-500">
                            {order.id.slice(0, 8)}…
                          </td>
                          <td className="px-4 py-3 text-xs text-gray-700">
                            {order.userId.slice(0, 8)}…
                          </td>
                          <td className="px-4 py-3 text-gray-500 max-w-xs truncate text-xs hidden sm:table-cell">
                            {order.shippingAddress}
                          </td>
                          <td className="px-4 py-3 text-right font-semibold text-rose-600 whitespace-nowrap">
                            {formatPrice(order.totalAmount)}
                          </td>
                          <td className="px-4 py-3 hidden lg:table-cell">
                            <PaymentCell method={order.paymentMethod} status={order.paymentStatus} />
                          </td>
                          <td className="px-4 py-3 text-gray-400 whitespace-nowrap text-xs hidden md:table-cell">
                            {formatDateTime(order.createdAt)}
                          </td>
                          <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                            <div className="flex flex-col gap-1.5 min-w-[130px]">
                              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${statusInfo?.color ?? 'bg-gray-100 text-gray-700'}`}>
                                {statusInfo?.label ?? order.status}
                              </span>
                              <div className="relative">
                                <select
                                  value={statusVal}
                                  onChange={(e) =>
                                    handleStatusChange(order.id, Number(e.target.value) as OrderStatusValue)
                                  }
                                  disabled={isBusy}
                                  className="text-xs border border-gray-200 rounded-md px-1.5 py-1 focus:outline-none focus:ring-1 focus:ring-rose-300 disabled:opacity-50 disabled:cursor-not-allowed bg-white text-gray-600 cursor-pointer w-full"
                                >
                                  {ORDER_STATUSES.map((s) => (
                                    <option key={s.value} value={s.value}>{s.label}</option>
                                  ))}
                                </select>
                                {updatingIds.has(order.id) && (
                                  <div className="absolute inset-0 flex items-center justify-center bg-white/80 rounded-md">
                                    <svg className="w-3.5 h-3.5 animate-spin text-rose-500" viewBox="0 0 24 24" fill="none">
                                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                                    </svg>
                                    <span className="ml-1 text-[10px] text-rose-500 font-medium">Đang xử lý...</span>
                                  </div>
                                )}
                              </div>
                            </div>
                          </td>
                        </tr>

                        {isExpanded && (
                          <tr className="bg-rose-50/40">
                            <td colSpan={8} className="px-8 py-4">
                              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                                Chi tiết đơn · {order.items.length} món
                              </p>
                              {order.notes && (
                                <p className="text-xs text-gray-500 mb-2 italic">Ghi chú: {order.notes}</p>
                              )}
                              <PaymentDetail
                                method={order.paymentMethod}
                                status={order.paymentStatus}
                                paidAt={order.paidAt}
                                transactionId={order.vnpayTransactionId}
                              />
                              <table className="w-full text-xs">
                                <thead>
                                  <tr className="text-gray-400 text-left border-b border-rose-100">
                                    <th className="pb-1.5 font-medium pr-4">Ảnh</th>
                                    <th className="pb-1.5 font-medium pr-4">Sản phẩm</th>
                                    <th className="pb-1.5 font-medium text-right pr-4">Đơn giá</th>
                                    <th className="pb-1.5 font-medium text-center pr-4">SL</th>
                                    <th className="pb-1.5 font-medium text-right">Thành tiền</th>
                                  </tr>
                                </thead>
                                <tbody className="divide-y divide-rose-100/60">
                                  {order.items.map((item) => (
                                    <tr key={item.productId}>
                                      <td className="py-1.5 pr-4">
                                        <div className="h-10 w-10 overflow-hidden rounded-md border border-rose-100 bg-white">
                                          {item.productImageUrl ? (
                                            <img
                                              src={getImageUrl(item.productImageUrl)}
                                              alt={item.productName}
                                              className="h-full w-full object-cover"
                                            />
                                          ) : (
                                            <div className="flex h-full w-full items-center justify-center text-sm">🍔</div>
                                          )}
                                        </div>
                                      </td>
                                      <td className="py-1.5 pr-4 text-gray-700 font-medium">{item.productName}</td>
                                      <td className="py-1.5 pr-4 text-right text-gray-500">{formatPrice(item.unitPrice)}</td>
                                      <td className="py-1.5 pr-4 text-center text-gray-600">×{item.quantity}</td>
                                      <td className="py-1.5 text-right font-semibold text-rose-600">{formatPrice(item.subTotal)}</td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            </td>
                          </tr>
                        )}
                      </Fragment>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>

          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} disabled={loading || isBusy} />
        </>
      )}
    </AdminLayout>
  );
}
