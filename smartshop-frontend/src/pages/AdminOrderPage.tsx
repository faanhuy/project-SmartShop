import { Fragment, useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { FiChevronDown, FiChevronRight } from 'react-icons/fi';
import AdminLayout from '../components/AdminLayout';
import { orderService } from '../services/orderService';
import type { OrderDto, OrderStatusValue } from '../types/order';
import { ORDER_STATUSES, resolveOrderStatus } from '../types/order';
import { formatPrice, formatDateTime } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';
import Pagination from '../components/common/Pagination';

const PAGE_SIZE = 20;

export default function AdminOrderPage() {
  const [allOrders,    setAllOrders]    = useState<OrderDto[]>([]);
  const [totalCount,   setTotalCount]   = useState(0);
  const [loading,      setLoading]      = useState(true);
  const [page,         setPage]         = useState(1);
  const [statusFilter, setStatusFilter] = useState<number>(0);
  const [updatingId,   setUpdatingId]   = useState<string | null>(null);
  const [expandedId,   setExpandedId]   = useState<string | null>(null);

  const loadOrders = async (p: number, sf: number) => {
    setLoading(true);
    try {
      const result = await orderService.getAllOrders(p, PAGE_SIZE, sf || undefined);
      setAllOrders(result.items);
      setTotalCount(result.totalCount);
    } catch {
      toast.error('Không thể tải danh sách đơn giao.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadOrders(page, statusFilter); }, [page, statusFilter]);

  const handleFilterChange = (value: number) => {
    setStatusFilter(value);
    setPage(1);
    setExpandedId(null);
  };

  const handleStatusChange = async (orderId: string, newStatus: OrderStatusValue) => {
    setUpdatingId(orderId);
    try {
      await orderService.updateOrderStatus(orderId, newStatus);
      await loadOrders(page, statusFilter);
      toast.success('Đã cập nhật trạng thái đơn giao.');
    } catch {
      toast.error('Cập nhật trạng thái thất bại.');
    } finally {
      setUpdatingId(null);
    }
  };

  const totalPages = Math.ceil(totalCount / PAGE_SIZE) || 1;

  return (
    <AdminLayout title="Quản lý đơn giao">
      {/* Bộ lọc trạng thái */}
      <div className="flex gap-1.5 flex-wrap mb-4">
        <button
          onClick={() => handleFilterChange(0)}
          className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
            statusFilter === 0
              ? 'bg-gray-900 text-white border-gray-900'
              : 'bg-white text-gray-600 border-gray-200 hover:border-gray-400'
          }`}
        >
          Tất cả
        </button>
        {ORDER_STATUSES.map((s) => (
          <button
            key={s.value}
            onClick={() => handleFilterChange(s.value)}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
              statusFilter === s.value
                ? 'bg-gray-900 text-white border-gray-900'
                : 'bg-white text-gray-600 border-gray-200 hover:border-gray-400'
            }`}
          >
            {s.label}
            {statusFilter === s.value && !loading && (
              <span className="ml-1.5 opacity-60">({totalCount})</span>
            )}
          </button>
        ))}
      </div>

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
                          <td className="px-4 py-3 text-right font-semibold text-blue-600 whitespace-nowrap">
                            {formatPrice(order.totalAmount)}
                          </td>
                          <td className="px-4 py-3 text-gray-400 whitespace-nowrap text-xs hidden md:table-cell">
                            {formatDateTime(order.createdAt)}
                          </td>
                          <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                            <div className="flex flex-col gap-1.5 min-w-[130px]">
                              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${statusInfo?.color ?? 'bg-gray-100 text-gray-700'}`}>
                                {statusInfo?.label ?? order.status}
                              </span>
                              <select
                                value={statusVal}
                                onChange={(e) =>
                                  handleStatusChange(order.id, Number(e.target.value) as OrderStatusValue)
                                }
                                disabled={updatingId === order.id}
                                className="text-xs border border-gray-200 rounded-md px-1.5 py-1 focus:outline-none focus:ring-1 focus:ring-blue-300 disabled:opacity-50 bg-white text-gray-600 cursor-pointer"
                              >
                                {ORDER_STATUSES.map((s) => (
                                  <option key={s.value} value={s.value}>{s.label}</option>
                                ))}
                              </select>
                            </div>
                          </td>
                        </tr>

                        {isExpanded && (
                          <tr className="bg-blue-50/40">
                            <td colSpan={7} className="px-8 py-4">
                              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                                Chi tiết đơn · {order.items.length} món
                              </p>
                              {order.notes && (
                                <p className="text-xs text-gray-500 mb-3 italic">Ghi chú: {order.notes}</p>
                              )}
                              <table className="w-full text-xs">
                                <thead>
                                  <tr className="text-gray-400 text-left border-b border-blue-100">
                                    <th className="pb-1.5 font-medium pr-4">Ảnh</th>
                                    <th className="pb-1.5 font-medium pr-4">Sản phẩm</th>
                                    <th className="pb-1.5 font-medium text-right pr-4">Đơn giá</th>
                                    <th className="pb-1.5 font-medium text-center pr-4">SL</th>
                                    <th className="pb-1.5 font-medium text-right">Thành tiền</th>
                                  </tr>
                                </thead>
                                <tbody className="divide-y divide-blue-100/60">
                                  {order.items.map((item) => (
                                    <tr key={item.productId}>
                                      <td className="py-1.5 pr-4">
                                        <div className="h-10 w-10 overflow-hidden rounded-md border border-blue-100 bg-white">
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
                                      <td className="py-1.5 text-right font-semibold text-blue-600">{formatPrice(item.subTotal)}</td>
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

          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} disabled={loading} />
        </>
      )}
    </AdminLayout>
  );
}
