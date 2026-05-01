import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { orderService } from '../services/orderService';
import { cartService } from '../services/cartService';
import type { CartDto } from '../types/cart';
import { FiArrowLeft, FiShoppingBag } from 'react-icons/fi';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import { couponSession } from '../utils/couponSession';
import { getImageUrl } from '../utils/imageUrl';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

export default function CheckoutPage() {
  const [shippingAddress, setShippingAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [cart, setCart] = useState<CartDto | null>(null);
  const [cartLoading, setCartLoading] = useState(true);
  const navigate = useNavigate();

  // Đọc coupon đã áp dụng từ sessionStorage
  const savedCoupon = couponSession.load();
  const couponCode     = savedCoupon?.code ?? null;
  const discountAmount = savedCoupon?.result.discountAmount ?? 0;

  useEffect(() => {
    cartService.getCart()
      .then(setCart)
      .catch(() => {})
      .finally(() => setCartLoading(false));
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!shippingAddress.trim()) {
      setError('Vui lòng nhập địa chỉ giao món.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      const order = await orderService.placeOrder({
        shippingAddress: shippingAddress.trim(),
        notes: notes.trim() || undefined,
        couponCode: couponCode ?? "",
      });
      couponSession.clear();
      navigate(`/orders/${order.id}`, { replace: true });
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      setError(msg ?? 'Đặt món thất bại, vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-4xl mx-auto px-4 py-8">
        <h1 className="text-2xl font-bold mb-6">Xác nhận đơn giao</h1>

        <div className="flex flex-col lg:flex-row gap-6">
          {/* Left — Form */}
          <div className="flex-1">
            <div className="bg-white rounded-2xl shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-800 mb-4">Thông tin giao món</h2>

              {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
                  {error}
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Địa chỉ giao món <span className="text-red-500">*</span>
                  </label>
                  <textarea
                    value={shippingAddress}
                    onChange={(e) => setShippingAddress(e.target.value)}
                    rows={3}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Số nhà, đường, phường/xã, quận/huyện, thành phố"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Ghi chú (tuỳ chọn)
                  </label>
                  <textarea
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    rows={2}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Ít cay, thêm tương, gọi trước khi giao..."
                  />
                </div>

                <div className="flex gap-3 pt-2">
                  <button
                    type="button"
                    onClick={() => navigate('/cart')}
                    className="flex-1 border border-gray-300 text-gray-700 py-2 rounded-lg hover:bg-gray-50 flex items-center justify-center gap-2 text-sm"
                  >
                    <FiArrowLeft size={16} />
                    Giỏ món
                  </button>
                  <button
                    type="submit"
                    disabled={loading || cartLoading || !cart || cart.items.length === 0}
                    className="flex-1 bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 font-semibold text-sm flex items-center justify-center gap-2"
                  >
                    <FiShoppingBag size={16} />
                    {loading ? 'Đang xử lý...' : 'Xác nhận đặt món'}
                  </button>
                </div>
              </form>
            </div>
          </div>

          {/* Right — Cart Summary */}
          <div className="lg:w-80">
            <div className="bg-white rounded-2xl shadow-sm p-6 sticky top-20">
              <h2 className="text-base font-semibold text-gray-800 mb-4">
                Đơn giao của bạn
                {cart && <span className="ml-2 text-xs text-gray-400 font-normal">({cart.items.length} món)</span>}
              </h2>

              {cartLoading ? (
                <p className="text-sm text-gray-400 text-center py-4">Đang tải...</p>
              ) : !cart || cart.items.length === 0 ? (
                <p className="text-sm text-gray-400 text-center py-4">Giỏ món đang trống.</p>
              ) : (
                <>
                  <div className="space-y-3 mb-4 max-h-64 overflow-y-auto pr-1">
                    {cart.items.map((item) => (
                      <div key={item.productId} className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-gray-100 rounded-lg shrink-0 overflow-hidden">
                          {item.productImageUrl ? (
                            <img src={getImageUrl(item.productImageUrl)} alt={item.productName} className="w-full h-full object-cover" />
                          ) : (
                            <div className="w-full h-full flex items-center justify-center text-gray-300 text-lg">📦</div>
                          )}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-800 truncate">{item.productName}</p>
                          <p className="text-xs text-gray-500">x{item.quantity} phần</p>
                        </div>
                        <p className="text-sm font-semibold text-blue-600 shrink-0">
                          {formatPrice(item.subTotal)}
                        </p>
                      </div>
                    ))}
                  </div>

                  <div className="border-t pt-3 space-y-1.5">
                    <div className="flex justify-between text-sm text-gray-600">
                      <span>Tạm tính</span>
                      <span>{formatPrice(cart.totalAmount)}</span>
                    </div>
                    {couponCode && discountAmount > 0 && (
                      <div className="flex justify-between text-sm text-green-600">
                        <span>Mã giảm giá ({couponCode})</span>
                        <span>-{formatPrice(discountAmount)}</span>
                      </div>
                    )}
                    <div className="flex justify-between text-sm text-gray-600">
                      <span>Phí giao hàng</span>
                      <span className="text-green-600">Miễn phí</span>
                    </div>
                    <div className="flex justify-between text-base font-bold text-gray-900 pt-1 border-t">
                      <span>Tổng cộng</span>
                      <span className="text-blue-600">
                        {formatPrice(Math.max(0, cart.totalAmount - discountAmount))}
                      </span>
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      </div>
      <Footer />
    </div>
  );
}
