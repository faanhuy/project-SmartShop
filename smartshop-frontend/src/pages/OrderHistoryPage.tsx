import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  FiArrowRight,
  FiCalendar,
  FiCheckCircle,
  FiClock,
  FiCreditCard,
  FiMapPin,
  FiPackage,
  FiRefreshCw,
  FiRotateCcw,
  FiShoppingBag,
} from 'react-icons/fi';
import { orderService } from '../services/orderService';
import { paymentService } from '../services/paymentService';
import type { OrderDto, PaymentMethod, PaymentStatus } from '../types/order';
import { ORDER_STATUSES } from '../types/order';
import { formatPrice, formatDate } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';
import Pagination from '../components/common/Pagination';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';

const paymentMethodLabels: Record<PaymentMethod, string> = {
  COD: 'COD',
  VNPay: 'VNPay',
  BankTransfer: 'Chuyển khoản',
};

const paymentStatusStyles: Record<PaymentStatus, { label: string; className: string }> = {
  Pending: {
    label: 'Chờ thanh toán',
    className: 'bg-amber-50 text-amber-700 border-amber-200',
  },
  Paid: {
    label: 'Đã thanh toán',
    className: 'bg-green-50 text-green-700 border-green-200',
  },
  Failed: {
    label: 'Thanh toán lỗi',
    className: 'bg-red-50 text-red-700 border-red-200',
  },
  Refunded: {
    label: 'Đã hoàn tiền',
    className: 'bg-gray-100 text-gray-700 border-gray-200',
  },
};

const getDisplayAddress = (order: OrderDto) => {
  if (order.shippingWardName || order.shippingProvinceName) {
    return [order.shippingWardName, order.shippingProvinceName].filter(Boolean).join(', ');
  }

  return order.shippingAddress || 'Chưa có địa chỉ giao hàng';
};

const getPaymentLabel = (order: OrderDto) => {
  if (!order.paymentMethod) return 'Chưa chọn';
  return paymentMethodLabels[order.paymentMethod] ?? order.paymentMethod;
};

const canRetryPayment = (order: OrderDto) =>
  order.paymentMethod === 'VNPay' &&
  (order.paymentStatus === 'Pending' || order.paymentStatus === 'Failed') &&
  order.status !== 'Cancelled';

export default function OrderHistoryPage() {
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [retryingOrderId, setRetryingOrderId] = useState<string | null>(null);
  const navigate = useNavigate();

  const stats = useMemo(() => {
    const activeOrders = orders.filter((order) => !['Delivered', 'Cancelled', 'Refunded'].includes(order.status)).length;
    const deliveredOrders = orders.filter((order) => order.status === 'Delivered').length;

    return {
      activeOrders,
      deliveredOrders,
      totalAmount: orders.reduce((sum, order) => sum + order.totalAmount, 0),
    };
  }, [orders]);

  const handleRetryPayment = async (e: React.MouseEvent, orderId: string) => {
    e.stopPropagation();
    setRetryingOrderId(orderId);
    try {
      const url = await paymentService.createVNPayUrl(orderId);
      window.location.href = url;
    } catch (error: any) {
      toast.error(error.response?.data?.message ?? 'Không thể tạo link thanh toán');
    } finally {
      setRetryingOrderId(null);
    }
  };

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const result = await orderService.getMyOrders(page);
        setOrders(result.items);
        setTotalPages(result.totalPages);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [page]);

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <main className="mx-auto max-w-5xl px-4 py-6 sm:px-6 lg:py-8">
        <section className="mb-6 overflow-hidden rounded-2xl border border-rose-100 bg-white shadow-sm">
          <div className="border-b border-rose-100 bg-rose-50/70 px-5 py-5 sm:px-6">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <p className="mb-1 text-sm font-medium text-rose-600">Theo dõi đơn giao</p>
                <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl">Đơn hàng của tôi</h1>
                <p className="mt-2 max-w-2xl text-sm text-gray-600">
                  Xem trạng thái giao hàng, thanh toán lại đơn VNPay và mở chi tiết từng đơn.
                </p>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => navigate('/orders/return-requests')}
                  className="inline-flex h-11 items-center justify-center gap-2 rounded-xl border border-gray-300 px-4 text-sm font-semibold text-gray-700 shadow-sm transition-colors hover:bg-gray-50"
                >
                  <FiRotateCcw size={17} />
                  Trả hàng
                </button>
                <button
                  onClick={() => navigate('/products')}
                  className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-rose-600 px-4 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-rose-700"
                >
                  <FiShoppingBag size={17} />
                  Đặt món mới
                </button>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 divide-y divide-gray-100 sm:grid-cols-3 sm:divide-x sm:divide-y-0">
            <div className="flex items-center gap-3 px-5 py-4 sm:px-6">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-50 text-amber-600">
                <FiClock size={20} />
              </div>
              <div>
                <p className="text-xs font-medium uppercase text-gray-400">Đang xử lý</p>
                <p className="text-lg font-bold text-gray-900">{stats.activeOrders}</p>
              </div>
            </div>
            <div className="flex items-center gap-3 px-5 py-4 sm:px-6">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-green-50 text-green-600">
                <FiCheckCircle size={20} />
              </div>
              <div>
                <p className="text-xs font-medium uppercase text-gray-400">Đã giao</p>
                <p className="text-lg font-bold text-gray-900">{stats.deliveredOrders}</p>
              </div>
            </div>
            <div className="flex items-center gap-3 px-5 py-4 sm:px-6">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-rose-50 text-rose-600">
                <FiCreditCard size={20} />
              </div>
              <div>
                <p className="text-xs font-medium uppercase text-gray-400">Trang hiện tại</p>
                <p className="text-lg font-bold text-gray-900">{formatPrice(stats.totalAmount)}</p>
              </div>
            </div>
          </div>
        </section>

        {loading ? (
          <div className="space-y-4">
            {[1, 2, 3].map((item) => (
              <div key={item} className="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
                <div className="mb-5 flex items-center justify-between gap-4">
                  <div className="h-5 w-36 animate-pulse rounded bg-gray-100" />
                  <div className="h-7 w-28 animate-pulse rounded-full bg-gray-100" />
                </div>
                <div className="flex gap-3">
                  <div className="h-16 w-16 animate-pulse rounded-xl bg-gray-100" />
                  <div className="flex-1 space-y-3">
                    <div className="h-4 w-full animate-pulse rounded bg-gray-100" />
                    <div className="h-4 w-2/3 animate-pulse rounded bg-gray-100" />
                  </div>
                </div>
              </div>
            ))}
          </div>
        ) : orders.length === 0 ? (
          <section className="rounded-2xl border border-dashed border-rose-200 bg-white px-6 py-14 text-center shadow-sm">
            <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-2xl bg-rose-50 text-rose-600">
              <FiPackage size={30} />
            </div>
            <h2 className="text-xl font-bold text-gray-900">Bạn chưa có đơn giao nào</h2>
            <p className="mx-auto mt-2 max-w-md text-sm text-gray-500">
              Khi bạn đặt món, toàn bộ lịch sử đơn hàng và trạng thái giao sẽ xuất hiện tại đây.
            </p>
            <button
              onClick={() => navigate('/products')}
              className="mt-6 inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-rose-600 px-5 text-sm font-semibold text-white transition-colors hover:bg-rose-700"
            >
              <FiShoppingBag size={17} />
              Đặt món ngay
            </button>
          </section>
        ) : (
          <>
            <div className="space-y-4">
              {orders.map((order) => {
                const statusInfo = ORDER_STATUSES.find((s) => s.key === order.status);
                const previewItems = order.items.slice(0, 4);
                const paymentInfo = order.paymentStatus ? paymentStatusStyles[order.paymentStatus] : null;

                return (
                  <article
                    key={order.id}
                    onClick={() => navigate(`/orders/${order.id}`)}
                    className="group cursor-pointer overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm transition-all hover:-translate-y-0.5 hover:border-rose-200 hover:shadow-md"
                  >
                    <div className="flex flex-col gap-4 border-b border-gray-100 px-5 py-4 sm:flex-row sm:items-start sm:justify-between">
                      <div>
                        <div className="mb-2 flex flex-wrap items-center gap-2">
                          <span className="font-mono text-sm font-semibold text-gray-900">
                            #{order.id.slice(0, 8).toUpperCase()}
                          </span>
                          <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusInfo?.color ?? 'bg-gray-100 text-gray-700'}`}>
                            {statusInfo?.label ?? order.status}
                          </span>
                          {paymentInfo && (
                            <span className={`rounded-full border px-3 py-1 text-xs font-semibold ${paymentInfo.className}`}>
                              {paymentInfo.label}
                            </span>
                          )}
                        </div>
                        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-gray-500">
                          <span className="inline-flex items-center gap-1.5">
                            <FiCalendar size={15} />
                            {formatDate(order.createdAt)}
                          </span>
                          <span className="inline-flex items-center gap-1.5">
                            <FiPackage size={15} />
                            {order.items.length} món
                          </span>
                          <span className="inline-flex items-center gap-1.5">
                            <FiCreditCard size={15} />
                            {getPaymentLabel(order)}
                          </span>
                        </div>
                      </div>

                      <div className="text-left sm:text-right">
                        <p className="text-xs font-medium uppercase text-gray-400">Tổng cộng</p>
                        <p className="text-xl font-bold text-rose-700">{formatPrice(order.totalAmount)}</p>
                      </div>
                    </div>

                    <div className="px-5 py-4">
                      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                        <div className="min-w-0 flex-1">
                          <div className="mb-3 flex items-center gap-2 overflow-hidden">
                            {previewItems.map((item) => (
                              <div
                                key={`${order.id}-${item.productId}`}
                                className="h-14 w-14 shrink-0 overflow-hidden rounded-xl border border-gray-100 bg-gray-100"
                                title={item.productName}
                              >
                                {item.productImageUrl ? (
                                  <img
                                    src={getImageUrl(item.productImageUrl)}
                                    alt={item.productName}
                                    className="h-full w-full object-cover"
                                  />
                                ) : (
                                  <div className="flex h-full w-full items-center justify-center text-2xl">🍽️</div>
                                )}
                              </div>
                            ))}
                            {order.items.length > previewItems.length && (
                              <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl border border-dashed border-gray-200 bg-gray-50 text-sm font-semibold text-gray-500">
                                +{order.items.length - previewItems.length}
                              </span>
                            )}
                          </div>
                          <p className="flex items-start gap-2 text-sm text-gray-500">
                            <FiMapPin className="mt-0.5 shrink-0 text-rose-500" size={16} />
                            <span className="line-clamp-2">{getDisplayAddress(order)}</span>
                          </p>
                        </div>

                        <div className="flex flex-wrap items-center gap-2 lg:justify-end">
                          {canRetryPayment(order) && (
                            <button
                              onClick={(e) => handleRetryPayment(e, order.id)}
                              disabled={retryingOrderId === order.id}
                              className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-amber-500 px-4 text-sm font-semibold text-white transition-colors hover:bg-amber-600 disabled:opacity-60"
                            >
                              <FiRefreshCw className={retryingOrderId === order.id ? 'animate-spin' : ''} size={16} />
                              {retryingOrderId === order.id ? 'Đang xử lý' : 'Thanh toán lại'}
                            </button>
                          )}
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              navigate(`/orders/${order.id}`);
                            }}
                            className="inline-flex h-10 items-center justify-center gap-2 rounded-xl border border-gray-200 px-4 text-sm font-semibold text-gray-700 transition-colors hover:border-rose-200 hover:bg-rose-50 hover:text-rose-700"
                          >
                            Chi tiết
                            <FiArrowRight className="transition-transform group-hover:translate-x-0.5" size={16} />
                          </button>
                        </div>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>

            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
          </>
        )}
      </main>
      <Footer />
    </div>
  );
}
