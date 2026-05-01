import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { orderService } from '../services/orderService';
import type { OrderDto } from '../types/order';
import { ORDER_STATUSES } from '../types/order';
import { formatPrice, formatDate } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';
import Pagination from '../components/common/Pagination';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';

export default function OrderHistoryPage() {
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const navigate = useNavigate();

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

  if (loading) return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="p-8 text-center text-gray-400">Đang tải...</div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-3xl mx-auto p-6">
        <h1 className="text-2xl font-bold mb-6">Lịch sử đơn giao</h1>

        {orders.length === 0 ? (
          <div className="text-center py-16 text-gray-500">
            <p className="mb-4">Bạn chưa có đơn giao nào.</p>
            <button
              onClick={() => navigate('/products')}
              className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
            >
              Đặt món ngay
            </button>
          </div>
        ) : (
          <>
            <div className="space-y-4">
              {orders.map((order) => {
                const statusInfo = ORDER_STATUSES.find((s) => s.key === order.status);
                const previewItems = order.items.slice(0, 3);
                return (
                  <div
                    key={order.id}
                    onClick={() => navigate(`/orders/${order.id}`)}
                    className="border rounded-lg p-4 hover:shadow-md cursor-pointer transition"
                  >
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm text-gray-500">
                        #{order.id.slice(0, 8).toUpperCase()}
                      </span>
                      <span className={`text-xs px-2 py-1 rounded-full font-medium ${statusInfo?.color ?? 'bg-gray-100 text-gray-700'}`}>
                        {statusInfo?.label ?? order.status}
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 mb-1">
                      {order.items.length} món · {formatDate(order.createdAt)}
                    </p>
                    <div className="mb-3 flex items-center gap-2">
                      {previewItems.map((item) => (
                        <div
                          key={`${order.id}-${item.productId}`}
                          className="h-11 w-11 shrink-0 overflow-hidden rounded-lg border border-gray-100 bg-gray-100"
                        >
                          {item.productImageUrl ? (
                            <img
                              src={getImageUrl(item.productImageUrl)}
                              alt={item.productName}
                              className="h-full w-full object-cover"
                            />
                          ) : (
                            <div className="flex h-full w-full items-center justify-center text-lg">🍔</div>
                          )}
                        </div>
                      ))}
                      {order.items.length > previewItems.length && (
                        <span className="text-xs text-gray-400">+{order.items.length - previewItems.length} món</span>
                      )}
                    </div>
                    <p className="font-semibold text-blue-700">{formatPrice(order.totalAmount)}</p>
                  </div>
                );
              })}
            </div>

            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
          </>
        )}
      </div>
      <Footer />
    </div>
  );
}
