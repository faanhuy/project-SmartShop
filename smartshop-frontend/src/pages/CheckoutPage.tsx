import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { orderService } from '../services/orderService';
import { cartService } from '../services/cartService';
import { addressService } from '../services/addressService';
import { paymentService } from '../services/paymentService';
import type { CartDto } from '../types/cart';
import type { AddressDto, PaymentMethod } from '../types/order';
import { FiArrowLeft, FiShoppingBag, FiMapPin, FiRepeat } from 'react-icons/fi';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import { couponSession } from '../utils/couponSession';
import { getImageUrl } from '../utils/imageUrl';
import StoreSelectorModal from '../components/StoreSelectorModal';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

export default function CheckoutPage() {
  const [shippingAddress, setShippingAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [cart, setCart] = useState<CartDto | null>(null);
  const [cartLoading, setCartLoading] = useState(true);

  // Address picker
  const [addresses, setAddresses] = useState<AddressDto[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<string>('');
  const [addrLoading, setAddrLoading] = useState(true);

  // Payment method
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('COD');

  // Store selection
  const { selectedStore, fetchStores } = useStoreSelectionStore();
  const [storeModalOpen, setStoreModalOpen] = useState(false);

  const navigate = useNavigate();

  const savedCoupon = couponSession.load();
  const couponCode = savedCoupon?.code ?? null;
  const discountAmount = savedCoupon?.result.discountAmount ?? 0;

  useEffect(() => {
    cartService.getCart()
      .then(setCart)
      .catch(() => {})
      .finally(() => setCartLoading(false));

    addressService.getAll()
      .then((list) => {
        setAddresses(list);
        const def = list.find((a) => a.isDefault) ?? list[0];
        if (def) setSelectedAddressId(def.id);
      })
      .catch(() => {})
      .finally(() => setAddrLoading(false));

    fetchStores().catch(() => {});
  }, [fetchStores]);

  // Auto-open store modal if no store selected
  useEffect(() => {
    if (!selectedStore) {
      setStoreModalOpen(true);
    }
  }, [selectedStore]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedStore) {
      toast.error('Vui lòng chọn chi nhánh trước khi đặt hàng.');
      setStoreModalOpen(true);
      return;
    }
    if (!selectedAddressId) {
      setError('Vui lòng chọn địa chỉ giao hàng.');
      return;
    }
    setLoading(true);
    setError('');

    try {
      const order = await orderService.placeOrder({
        addressId: selectedAddressId,
        notes: notes.trim() || undefined,
        couponCode: couponCode ?? '',
        paymentMethod,
        storeId: selectedStore.id,
      });
      couponSession.clear();

      if (paymentMethod === 'VNPay') {
        const paymentUrl = await paymentService.createVNPayUrl(order.id);
        window.location.href = paymentUrl;
      } else if (paymentMethod === 'BankTransfer') {
        toast.success('Đặt món thành công! Vui lòng chuyển khoản để xác nhận đơn.');
        navigate(`/orders/${order.id}`, { replace: true, state: { showBankInfo: true } });
      } else {
        toast.success('Đặt món thành công!');
        navigate(`/orders/${order.id}`, { replace: true });
      }
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string; errors?: string[] } } })
        ?.response?.data?.errors?.[0]
        ?? (e as { response?: { data?: { message?: string } } })?.response?.data?.message
        ?? 'Đặt món thất bại, vui lòng thử lại.';
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const hasAddresses = addresses.length > 0;

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-4xl mx-auto px-4 py-8">
        <h1 className="text-2xl font-bold mb-6">Xác nhận đơn giao</h1>

        <StoreSelectorModal
          isOpen={storeModalOpen}
          onClose={() => setStoreModalOpen(false)}
          required={!selectedStore}
        />

        <div className="flex flex-col lg:flex-row gap-6">
          {/* Left — Form */}
          <div className="flex-1 space-y-4">
            {/* Store selector section */}
            <div className="bg-white rounded-2xl shadow-sm p-6">
              <div className="flex items-center justify-between mb-2">
                <h2 className="text-base font-semibold text-gray-800">Chi nhánh lấy hàng</h2>
                <button
                  type="button"
                  onClick={() => setStoreModalOpen(true)}
                  className="text-xs text-rose-600 hover:text-rose-700 flex items-center gap-1"
                >
                  <FiRepeat size={12} />
                  Đổi chi nhánh
                </button>
              </div>
              {selectedStore ? (
                <div className="flex items-start gap-2 text-sm text-gray-700">
                  <FiMapPin size={14} className="shrink-0 mt-0.5 text-rose-500" />
                  <div>
                    <p className="font-medium">{selectedStore.name}</p>
                    <p className="text-xs text-gray-500">{selectedStore.address}</p>
                    <p className="text-xs text-gray-500">{selectedStore.phone}</p>
                  </div>
                </div>
              ) : (
                <p className="text-sm text-amber-600">
                  Chưa chọn chi nhánh.{' '}
                  <button
                    type="button"
                    onClick={() => setStoreModalOpen(true)}
                    className="underline hover:text-amber-700"
                  >
                    Chọn ngay
                  </button>
                </p>
              )}
            </div>

            {/* Address section */}
            <div className="bg-white rounded-2xl shadow-sm p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-base font-semibold text-gray-800">Địa chỉ giao hàng</h2>
                <Link
                  to="/profile"
                  state={{ tab: 'addresses' }}
                  className="text-xs text-rose-600 hover:text-rose-700 flex items-center gap-1"
                >
                  <FiMapPin size={12} />
                  Quản lý địa chỉ
                </Link>
              </div>

              {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
                  {error}
                </div>
              )}

              {addrLoading ? (
                <p className="text-sm text-gray-400">Đang tải địa chỉ...</p>
              ) : hasAddresses ? (
                <div className="space-y-2">
                  {addresses.map((addr) => (
                    <label
                      key={addr.id}
                      className={`flex items-start gap-3 p-3 rounded-xl border-2 cursor-pointer transition-colors ${
                        selectedAddressId === addr.id
                          ? 'border-rose-400 bg-rose-50'
                          : 'border-gray-200 hover:border-gray-300'
                      }`}
                    >
                      <input
                        type="radio"
                        name="address"
                        value={addr.id}
                        checked={selectedAddressId === addr.id}
                        onChange={() => setSelectedAddressId(addr.id)}
                        className="mt-0.5 accent-rose-600"
                      />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="text-sm font-medium text-gray-800">{addr.recipientName}</span>
                          <span className="text-sm text-gray-500">{addr.phone}</span>
                          {addr.label && (
                            <span className="text-xs bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded-full">
                              {addr.label}
                            </span>
                          )}
                          {addr.isDefault && (
                            <span className="text-xs bg-rose-100 text-rose-600 px-1.5 py-0.5 rounded-full">
                              Mặc định
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-gray-500 mt-0.5">
                          {addr.street && addr.wardName && addr.provinceName
                            ? `${addr.street}, ${addr.wardName}, ${addr.provinceName}`
                            : [addr.street, addr.ward, addr.district, addr.city].filter(Boolean).join(', ')}
                        </p>
                      </div>
                    </label>
                  ))}
                </div>
              ) : (
                <div>
                  <p className="text-sm text-gray-500 mb-3">
                    Bạn chưa có địa chỉ đã lưu.{' '}
                    <Link to="/profile" className="text-rose-600 hover:underline">
                      Thêm địa chỉ
                    </Link>
                  </p>
                  <textarea
                    value={shippingAddress}
                    onChange={(e) => setShippingAddress(e.target.value)}
                    rows={3}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                    placeholder="Số nhà, đường, phường/xã, quận/huyện, thành phố"
                  />
                </div>
              )}
            </div>

            {/* Payment method */}
            <div className="bg-white rounded-2xl shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-800 mb-4">Phương thức thanh toán</h2>
              <div className="space-y-2">
                <label
                  className={`flex items-center gap-3 p-3 rounded-xl border-2 cursor-pointer transition-colors ${
                    paymentMethod === 'COD'
                      ? 'border-rose-400 bg-rose-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="payment"
                    value="COD"
                    checked={paymentMethod === 'COD'}
                    onChange={() => setPaymentMethod('COD')}
                    className="accent-rose-600"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-800">Thanh toán khi nhận hàng (COD)</p>
                    <p className="text-xs text-gray-500">Trả tiền mặt khi nhận món</p>
                  </div>
                </label>

                <label
                  className={`flex items-center gap-3 p-3 rounded-xl border-2 cursor-pointer transition-colors ${
                    paymentMethod === 'VNPay'
                      ? 'border-rose-400 bg-rose-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="payment"
                    value="VNPay"
                    checked={paymentMethod === 'VNPay'}
                    onChange={() => setPaymentMethod('VNPay')}
                    className="accent-rose-600"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-800">Thanh toán qua VNPay</p>
                    <p className="text-xs text-gray-500">QR Code / Thẻ ATM / Thẻ tín dụng</p>
                  </div>
                </label>

                <label
                  className={`flex items-center gap-3 p-3 rounded-xl border-2 cursor-pointer transition-colors ${
                    paymentMethod === 'BankTransfer'
                      ? 'border-rose-400 bg-rose-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="payment"
                    value="BankTransfer"
                    checked={paymentMethod === 'BankTransfer'}
                    onChange={() => setPaymentMethod('BankTransfer')}
                    className="accent-rose-600"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-800">Chuyển khoản ngân hàng (ACB)</p>
                    <p className="text-xs text-gray-500">Chuyển khoản sau khi đặt — đơn xác nhận sau khi nhận tiền</p>
                  </div>
                </label>

                {paymentMethod === 'BankTransfer' && (
                  <div className="mt-2 p-4 bg-amber-50 border border-amber-200 rounded-xl text-sm space-y-1.5">
                    <p className="font-semibold text-amber-800">Thông tin chuyển khoản</p>
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-amber-900">
                      <span className="text-gray-500">Ngân hàng</span><span className="font-medium">ACB</span>
                      <span className="text-gray-500">Số tài khoản</span>
                      <span className="font-bold tracking-widest select-all">24817827</span>
                      <span className="text-gray-500">Chủ tài khoản</span><span className="font-medium">SMARTSHOP</span>
                    </div>
                    <p className="text-xs text-amber-700 mt-1">
                      Nội dung CK: <span className="font-semibold">SmartShop + SĐT của bạn</span>
                    </p>
                  </div>
                )}
              </div>
            </div>

            {/* Notes & Actions */}
            <div className="bg-white rounded-2xl shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-800 mb-4">Thông tin thêm</h2>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Ghi chú (tuỳ chọn)
                  </label>
                  <textarea
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    rows={2}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                    placeholder="Ít cay, thêm tương, gọi trước khi giao..."
                  />
                </div>

                <div className="flex gap-3">
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
                    className="flex-1 bg-rose-600 text-white py-2 rounded-lg hover:bg-rose-700 disabled:opacity-50 font-semibold text-sm flex items-center justify-center gap-2"
                  >
                    <FiShoppingBag size={16} />
                    {loading
                      ? paymentMethod === 'VNPay' ? 'Đang chuyển đến VNPay...' : 'Đang xử lý...'
                      : paymentMethod === 'VNPay' ? 'Thanh toán qua VNPay'
                      : paymentMethod === 'BankTransfer' ? 'Đặt món & Chuyển khoản'
                      : 'Xác nhận đặt món'}
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
                      <div key={`${item.productId}-${item.sizeId ?? ''}`} className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-gray-100 rounded-lg shrink-0 overflow-hidden">
                          {item.productImageUrl ? (
                            <img src={getImageUrl(item.productImageUrl)} alt={item.productName} className="w-full h-full object-cover" />
                          ) : (
                            <div className="w-full h-full flex items-center justify-center text-gray-300 text-lg">📦</div>
                          )}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-800 truncate">{item.productName}</p>
                          <p className="text-xs text-gray-500">
                            x{item.quantity} phần{item.sizeLabel ? ` · ${item.sizeLabel}` : ''}
                          </p>
                        </div>
                        <p className="text-sm font-semibold text-rose-600 shrink-0">
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
                      <span className="text-rose-600">
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
