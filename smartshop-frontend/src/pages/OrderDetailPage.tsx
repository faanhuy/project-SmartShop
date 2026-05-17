import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { FiArrowLeft, FiPackage } from 'react-icons/fi';
import { orderService } from '../services/orderService';
import { paymentService } from '../services/paymentService';
import returnRequestService from '../services/returnRequestService';
import type { OrderDto } from '../types/order';
import { ORDER_STATUSES } from '../types/order';
import type { ReturnRequestDto } from '../types/returnRequest';
import { ReturnStatus, RETURN_STATUS_LABELS, RETURN_STATUS_COLORS } from '../types/returnRequest';
import { getApiError } from '../utils/errorHandler';
import { formatPrice, formatDateTime } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import CreateReturnRequestModal from '../components/returns/CreateReturnRequestModal';

const PAYMENT_METHOD_LABELS: Record<string, string> = {
  COD: 'Thanh toán khi nhận hàng (COD)',
  VNPay: 'VNPay',
  BankTransfer: 'Chuyển khoản ngân hàng',
};

export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [cancelling, setCancelling] = useState(false);
  const [retrying, setRetrying] = useState(false);
  const [expandedComponents, setExpandedComponents] = useState<Set<number>>(new Set());
  const [showReturnModal, setShowReturnModal] = useState(false);
  const [existingReturnRequest, setExistingReturnRequest] = useState<ReturnRequestDto | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!id) return;
    const loadData = async () => {
      try {
        const orderData = await orderService.getOrderById(id);
        setOrder(orderData);

        // Check if there's an existing return request for this order
        const returnRequests = await returnRequestService.getMyReturnRequests();
        const existingReturn = returnRequests.find((r) => r.orderId === id);
        if (existingReturn) {
          setExistingReturnRequest(existingReturn);
        }
      } catch {
        setError('Không tìm thấy đơn giao.');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [id]);

  const handleRetryPayment = async () => {
    if (!order) return;
    setRetrying(true);
    try {
      const url = await paymentService.createVNPayUrl(order.id);
      window.location.href = url;
    } catch (err: any) {
      toast.error(err.response?.data?.message ?? 'Không thể tạo link thanh toán');
    } finally {
      setRetrying(false);
    }
  };

  const handleCancel = async () => {
    if (!order) return;
    if (!confirm('Bạn có chắc muốn huỷ đơn giao này?')) return;
    setCancelling(true);
    try {
      await orderService.cancelOrder(order.id);
      setOrder({ ...order, status: 'Cancelled' });
      toast.success('Đã huỷ đơn giao.');
    } catch (err) {
      toast.error(getApiError(err, 'Huỷ đơn giao thất bại.'));
    } finally {
      setCancelling(false);
    }
  };

  const canRequestReturn = (): boolean => {
    if (!order) return false;
    // Block only if there's a Pending or Approved request; Rejected allows re-submission
    if (existingReturnRequest && existingReturnRequest.status !== ReturnStatus.Rejected) return false;

    const preShipStatuses = ['Pending', 'Confirmed', 'Processing'];
    const isPaidBeforeShip =
      order.paymentStatus === 'Paid' && preShipStatuses.includes(order.status);

    if (isPaidBeforeShip) return true;

    if (order.status === 'Delivered') {
      const deliveredAt = order.createdAt;
      const daysDiff = Math.floor(
        (Date.now() - new Date(deliveredAt).getTime()) / (1000 * 60 * 60 * 24)
      );
      return daysDiff <= 7;
    }

    return false;
  };

  const handleReturnSuccess = async () => {
    if (!id) return;
    try {
      const returnRequests = await returnRequestService.getMyReturnRequests();
      const existingReturn = returnRequests.find((r) => r.orderId === id);
      if (existingReturn) {
        setExistingReturnRequest(existingReturn);
      }
    } catch (err) {
      toast.error(getApiError(err, 'Không thể cập nhật dữ liệu.'));
    }
  };

  if (loading) return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="p-8 text-center text-gray-400">Đang tải...</div>
    </div>
  );
  if (error || !order) return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="p-8 text-center text-red-500">{error || 'Không tìm thấy đơn giao.'}</div>
    </div>
  );

  const toggleComponents = (idx: number) =>
    setExpandedComponents((prev) => {
      const next = new Set(prev);
      next.has(idx) ? next.delete(idx) : next.add(idx);
      return next;
    });

  const statusInfo = ORDER_STATUSES.find((s) => s.key === order.status);

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-2xl mx-auto p-6">
        <button onClick={() => navigate('/orders')} className="text-rose-600 hover:text-rose-800 mb-4 flex items-center gap-1.5 text-sm" title="Quay lại danh sách đơn giao">
          <FiArrowLeft size={16} /> Lịch sử đơn giao
        </button>

        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl font-bold">
            Đơn #{order.id.slice(0, 8).toUpperCase()}
          </h1>
          <span className={`text-sm px-3 py-1 rounded-full font-medium ${statusInfo?.color ?? 'bg-gray-100 text-gray-700'}`}>
            {statusInfo?.label ?? order.status}
          </span>
        </div>

        <div className="bg-gray-50 rounded-lg p-4 mb-6 space-y-1 text-sm text-gray-600">
          <p><span className="font-medium text-gray-800">Ngày đặt:</span> {formatDateTime(order.createdAt)}</p>
          <p>
            <span className="font-medium text-gray-800">Địa chỉ giao:</span>{' '}
            {order.shippingWardName
              ? `${order.shippingWardName}, ${order.shippingProvinceName ?? ''}`.replace(/,\s*$/, '')
              : order.shippingAddress}
          </p>
          {order.notes && <p><span className="font-medium text-gray-800">Ghi chú:</span> {order.notes}</p>}
        </div>

        <div className="bg-gray-50 rounded-lg p-4 mb-6 space-y-2 text-sm text-gray-600">
          <p className="font-medium text-gray-800 mb-1">Thông tin thanh toán</p>
          {order.paymentMethod && (
            <p>
              <span className="font-medium text-gray-800">Phương thức:</span>{' '}
              {PAYMENT_METHOD_LABELS[order.paymentMethod] ?? order.paymentMethod}
            </p>
          )}
          {order.paymentStatus && (
            <p className="flex items-center gap-2">
              <span className="font-medium text-gray-800">Trạng thái:</span>
              {order.paymentStatus === 'Paid' && (
                <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-green-100 text-green-700">Đã thanh toán</span>
              )}
              {order.paymentStatus === 'Failed' && (
                <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-red-100 text-red-700">Thanh toán thất bại</span>
              )}
              {order.paymentStatus === 'Pending' && (order.paymentMethod === 'VNPay' || order.paymentMethod === 'BankTransfer') && (
                <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-yellow-100 text-yellow-700">Chờ thanh toán</span>
              )}
              {order.paymentStatus === 'Pending' && order.paymentMethod === 'COD' && (
                <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-gray-100 text-gray-600">Thanh toán khi nhận</span>
              )}
              {order.paymentStatus === 'Refunded' && (
                <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-gray-100 text-gray-700">Đã hoàn tiền</span>
              )}
            </p>
          )}
          {order.paidAt && (
            <p><span className="font-medium text-gray-800">Ngày thanh toán:</span> {formatDateTime(order.paidAt)}</p>
          )}
        </div>

        <div className="space-y-2 mb-6">
          {order.items.map((item, idx) => (
            <div key={idx} className="border rounded-lg overflow-hidden bg-white">
              <div className="flex items-center justify-between gap-3 p-3">
                <div className="flex min-w-0 items-center gap-3">
                  <div className="h-14 w-14 shrink-0 overflow-hidden rounded-lg border border-gray-100 bg-gray-100">
                    {item.productImageUrl ? (
                      <img
                        src={getImageUrl(item.productImageUrl)}
                        alt={item.productName}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center text-2xl">
                        {item.itemType === 'Combo' ? '📦' : '🍔'}
                      </div>
                    )}
                  </div>
                  <div className="min-w-0">
                    <div className="flex items-center gap-1.5 mb-0.5">
                      {item.itemType === 'Combo' && (
                        <span className="bg-orange-100 text-orange-600 text-[10px] font-bold px-1.5 py-0.5 rounded-full">
                          COMBO
                        </span>
                      )}
                      <p className="truncate font-medium">{item.productName}</p>
                    </div>
                    <p className="text-sm text-gray-500">
                      {formatPrice(item.unitPrice)} × {item.quantity}
                    </p>
                    {item.itemType === 'Combo' && item.components.length > 0 && (
                      <button
                        onClick={() => toggleComponents(idx)}
                        className="text-xs text-gray-400 hover:text-gray-600 flex items-center gap-1 mt-0.5"
                      >
                        <FiPackage size={10} />
                        {expandedComponents.has(idx) ? 'Ẩn' : 'Xem'} chi tiết
                      </button>
                    )}
                  </div>
                </div>
                <p className="shrink-0 font-semibold">{formatPrice(item.subTotal)}</p>
              </div>

              {item.itemType === 'Combo' && expandedComponents.has(idx) && item.components.length > 0 && (
                <div className="border-t bg-orange-50 px-4 py-2">
                  <ul className="space-y-1">
                    {item.components.map((c, ci) => (
                      <li key={ci} className="flex justify-between text-xs text-gray-600">
                        <span>
                          {c.productName}
                          {c.sizeLabel && <span className="text-gray-400"> ({c.sizeLabel})</span>}
                          {' '}× {c.quantityPerCombo}
                        </span>
                        <span className="text-gray-400">
                          {c.unitPriceSnapshot.toLocaleString('vi-VN')} đ/cái
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          ))}
        </div>

        <div className="flex items-center justify-between flex-wrap gap-3">
          <div className="flex items-center gap-3">
            {order.status === 'Pending' && (
              <button
                onClick={handleCancel}
                disabled={cancelling}
                className="text-sm text-red-500 border border-red-300 px-4 py-2 rounded-lg hover:bg-red-50 disabled:opacity-50 transition-colors"
              >
                {cancelling ? 'Đang huỷ...' : 'Huỷ đơn'}
              </button>
            )}
            {order.paymentMethod === 'VNPay' &&
              (order.paymentStatus === 'Pending' || order.paymentStatus === 'Failed') &&
              order.status !== 'Cancelled' && (
                <button
                  onClick={handleRetryPayment}
                  disabled={retrying}
                  className="text-sm px-4 py-2 rounded-lg bg-amber-500 text-white hover:bg-amber-600 disabled:opacity-60 transition-colors"
                >
                  {retrying ? 'Đang xử lý...' : 'Thanh toán lại'}
                </button>
              )}
            {canRequestReturn() && (
              <button
                onClick={() => setShowReturnModal(true)}
                className="text-sm px-4 py-2 rounded-lg bg-blue-500 text-white hover:bg-blue-600 transition-colors"
              >
                Yêu cầu trả hàng
              </button>
            )}
            {existingReturnRequest && existingReturnRequest.status !== ReturnStatus.Rejected && (
              <span
                className={`text-sm px-3 py-1.5 rounded-lg font-medium ${RETURN_STATUS_COLORS[existingReturnRequest.status]}`}
              >
                Yêu cầu hoàn tiền: {RETURN_STATUS_LABELS[existingReturnRequest.status]}
              </span>
            )}
          </div>
          <p className="text-xl font-bold text-rose-700">
            Tổng cộng: {formatPrice(order.totalAmount)}
          </p>
        </div>

        {existingReturnRequest?.status === ReturnStatus.Rejected && (
          <div className="mt-4 bg-red-50 border border-red-200 rounded-lg p-4 text-sm">
            <p className="font-semibold text-red-700 mb-1">Yêu cầu hoàn tiền bị từ chối</p>
            {existingReturnRequest.adminNote && (
              <p className="text-red-600">Lý do: {existingReturnRequest.adminNote}</p>
            )}
            <p className="text-gray-500 mt-1">Bạn có thể gửi yêu cầu hoàn tiền mới.</p>
          </div>
        )}

        <CreateReturnRequestModal
          isOpen={showReturnModal}
          onClose={() => setShowReturnModal(false)}
          orderId={order.id}
          orderTotal={order.totalAmount}
          onSuccess={handleReturnSuccess}
        />
      </div>
      <Footer />
    </div>
  );
}
