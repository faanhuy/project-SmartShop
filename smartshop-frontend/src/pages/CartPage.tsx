import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { cartService } from '../services/cartService';
import { productService } from '../services/productService';
import { getApiError } from '../utils/errorHandler';
import { formatPrice } from '../utils/formatters';
import type { CartDto } from '../types/cart';
import type { ProductDto } from '../types/product';
import { FiMinus, FiPlus, FiShoppingCart } from 'react-icons/fi';
import Navbar from '../components/Navbar';
import CouponInput from '../components/CouponInput';
import type { ValidateCouponResult } from '../services/couponService';
import { couponSession } from '../utils/couponSession';
import { couponService } from '../services/couponService';

export default function CartPage() {
  const [cart, setCart] = useState<CartDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [appliedCoupon, setAppliedCoupon] = useState<ValidateCouponResult | null>(null);
  const [appliedCode, setAppliedCode] = useState('');
  const [suggestions, setSuggestions] = useState<ProductDto[]>([]);
  const [addingId, setAddingId] = useState<string | null>(null);
  const navigate = useNavigate();

  // Khôi phục coupon đã áp dụng từ sessionStorage sau khi refresh
  useEffect(() => {
    const saved = couponSession.load();
    if (saved) {
      setAppliedCode(saved.code);
      setAppliedCoupon(saved.result);
    }
  }, []);

  // Sau mỗi thay đổi giỏ hàng, re-validate coupon với tổng tiền mới
  // để tránh discount stale dẫn đến hiển thị số âm
  const revalidateCoupon = async (updatedCart: CartDto, code: string) => {
    const newTotal = updatedCart.items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0);
    try {
      const result = await couponService.validate(code, newTotal);
      setAppliedCoupon(result);
      couponSession.save(code, result);
    } catch {
      // Coupon không còn hợp lệ (vd: tổng tiền giờ dưới MinOrderValue)
      setAppliedCoupon(null);
      setAppliedCode('');
      couponSession.clear();
      toast('Mã giảm giá đã bị gỡ vì đơn hàng không còn đủ điều kiện.', { icon: 'ℹ️' });
    }
  };

  const loadCart = async () => {
    try {
      const data = await cartService.getCart();
      setCart(data);
      return data;
    } catch {
      setError('Không thể tải giỏ hàng.');
      return null;
    } finally {
      setLoading(false);
    }
  };

  const loadSuggestions = async (cartData: CartDto | null) => {
    if (!cartData || cartData.items.length === 0) return;
    try {
      // Lấy categoryId từ sản phẩm đầu tiên trong giỏ
      const firstProduct = await productService.getProductById(cartData.items[0].productId);
      if (!firstProduct) return;

      const cartProductIds = new Set(cartData.items.map((i) => i.productId));
      const result = await productService.getProducts({
        categoryId: firstProduct.categoryId,
        sortBy: 5, // BestSelling
        pageSize: 8,
        page: 1,
      });
      // Lọc bỏ sản phẩm đã có trong giỏ
      const filtered = (result?.items ?? []).filter((p) => !cartProductIds.has(p.id));
      setSuggestions(filtered.slice(0, 6));
    } catch {
      // Không hiện lỗi nếu suggestions tải thất bại
    }
  };

  useEffect(() => {
    loadCart().then((data) => loadSuggestions(data));
  }, []);

  const handleUpdateQuantity = async (productId: string, quantity: number) => {
    if (quantity <= 0) return handleRemove(productId);
    try {
      const updated = await cartService.updateItem(productId, quantity);
      setCart(updated);
      if (appliedCode) await revalidateCoupon(updated, appliedCode);
    } catch (err) {
      toast.error(getApiError(err, 'Cập nhật thất bại.'));
    }
  };

  const handleRemove = async (productId: string) => {
    try {
      const updated = await cartService.removeItem(productId);
      setCart(updated);
      if (appliedCode) await revalidateCoupon(updated, appliedCode);
    } catch {
      toast.error('Xoá sản phẩm thất bại.');
    }
  };

  const handleClear = async () => {
    if (!confirm('Xoá toàn bộ giỏ hàng?')) return;
    try {
      await cartService.clearCart();
      setCart(null);
      setAppliedCoupon(null);
      setAppliedCode('');
      couponSession.clear();
    } catch {
      toast.error('Xoá giỏ hàng thất bại.');
    }
  };

  const originalTotal = cart ? cart.items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0) : 0;
  const discountAmount = appliedCoupon?.discountAmount ?? 0;
  const finalTotal = Math.max(0, appliedCoupon?.finalAmount ?? originalTotal);

  const handleCouponApplied = (result: ValidateCouponResult, code: string) => {
    setAppliedCoupon(result);
    setAppliedCode(code);
    couponSession.save(code, result);
    toast.success(`Áp dụng mã ${code} thành công!`);
  };

  const handleCouponClear = () => {
    setAppliedCoupon(null);
    setAppliedCode('');
    couponSession.clear();
  };

  const handleAddSuggestion = async (product: ProductDto) => {
    setAddingId(product.id);
    try {
      const updated = await cartService.addToCart(product.id, 1);
      setCart(updated);
      setSuggestions((prev) => prev.filter((p) => p.id !== product.id));
      if (appliedCode) await revalidateCoupon(updated, appliedCode);
      toast.success(`Đã thêm "${product.name}" vào giỏ hàng`);
    } catch (err) {
      toast.error(getApiError(err, 'Thêm vào giỏ thất bại.'));
    } finally {
      setAddingId(null);
    }
  };
  if (loading) return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="p-8 text-center text-gray-400">Đang tải...</div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div id="cart-layout" className="max-w-7xl mx-auto p-6 flex flex-col md:flex-row gap-8 justify-center items-start">
        {/* Cột trái: Giỏ hàng */}
        <div id="cart-main" className="flex-1 min-w-[350px] max-w-xl flex flex-col items-center">
          <div className="flex items-center gap-4 mb-6 w-full">
            <h1 id="cart-title" className="text-xl font-bold ml-8">Giỏ hàng của bạn:</h1>
          </div>
          {error && <p className="text-red-500 mb-4">{error}</p>}
          {!cart || cart.items.length === 0 ? (
            <div className="text-center py-16 text-gray-500">
              <p className="text-lg mb-4">Giỏ hàng trống</p>
              <button
                onClick={() => navigate('/products')}
                className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700 flex items-center gap-2 mx-auto"
                title="Tiếp tục mua sắm"
              >
                <FiPlus size={18} />
                Tiếp tục mua sắm
              </button>
            </div>
          ) : (
            <div id="cart-content" className="max-w-lg w-full">
              {/* Sản phẩm trong giỏ */}
              <div id="cart-items" className="space-y-4 mb-4 w-full">
                {cart.items.map((item) => (
                  <div key={item.productId} className="flex gap-4 border rounded-lg p-4 items-start bg-white" id={`cart-item-${item.productId}`}>
                    <div className="w-20 h-20 bg-gray-100 rounded flex-shrink-0 overflow-hidden">
                      {item.productImageUrl ? (
                        <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-cover" />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-gray-400 text-xs">No img</div>
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="font-bold text-base mb-1">{item.productName}</div>
                      <div className="text-gray-500 text-sm mb-1">2x Gà Charsiu (New)<br />2x Gà giòn không cay<br />2x Coca</div>
                      <div className="flex gap-2 text-orange-600 font-bold items-center mb-1">
                        <span className="line-through text-gray-400 font-normal mr-2">{item.unitPrice.toLocaleString('vi-VN')} đ</span>
                        <span className="text-xl ml-2">{item.subTotal.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <div className="flex gap-4 text-sm mt-1">
                        <button className="text-orange-500 hover:underline" onClick={() => handleRemove(item.productId)}>Xoá</button>
                      </div>
                    </div>
                    <div className="flex flex-col items-center gap-2">
                      <div className="flex items-center gap-2 bg-gray-100 rounded-full px-2 py-1">
                        <button className="w-7 h-7 rounded-full flex items-center justify-center hover:bg-gray-200" onClick={() => handleUpdateQuantity(item.productId, item.quantity - 1)}><FiMinus size={14} /></button>
                        <span className="w-6 text-center">{item.quantity}</span>
                        <button className="w-7 h-7 rounded-full flex items-center justify-center hover:bg-gray-200" onClick={() => handleUpdateQuantity(item.productId, item.quantity + 1)}><FiPlus size={14} /></button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
              {/* Mã giảm giá */}
              <div className="mt-6">
                <CouponInput
                  orderTotal={originalTotal}
                  onApplied={handleCouponApplied}
                  onClear={handleCouponClear}
                  appliedCode={appliedCode}
                />
              </div>
              {/* Bảng giá */}
              <div className="space-y-2 text-lg mb-4">
                <div className="flex justify-between">
                  <span className="font-semibold">Tổng</span>
                  <span className="text-orange-500 font-semibold">{originalTotal.toLocaleString('vi-VN')} <span className="underline">đ</span></span>
                </div>
                {discountAmount > 0 && (
                  <div className="flex justify-between">
                    <span className="font-semibold">Giảm giá ({appliedCode})</span>
                    <span className="text-green-600 font-semibold">-{discountAmount.toLocaleString('vi-VN')} <span className="underline">đ</span></span>
                  </div>
                )}
                <div className="flex justify-between font-bold text-xl border-t pt-2">
                  <span>Tổng cộng</span>
                  <span className="text-orange-500">{finalTotal.toLocaleString('vi-VN')} <span className="underline">đ</span></span>
                </div>
              </div>
              {/* Nút thanh toán */}
              <button
                onClick={() => navigate('/checkout')}
                className="w-full bg-orange-500 text-white py-3 rounded-full font-bold text-lg 
                hover:bg-orange-600 transition-colors mt-2">
                Thanh toán
              </button>
            </div>
          )}
        </div>
        {/* Cột phải: Sản phẩm đề xuất */}
        {suggestions.length > 0 && (
          <div id="cart-suggestion" className="w-full md:w-[380px] bg-white rounded-3xl shadow p-6 flex-shrink-0 border">
            <h2 id="suggestion-title" className="text-lg font-semibold mb-1">Sản phẩm đề xuất</h2>
            <p className="text-xs text-gray-400 mb-4">Bán chạy cùng danh mục hôm nay</p>
            <div id="suggestion-list" className="divide-y max-h-[calc(100vh-240px)] overflow-y-auto">
              {suggestions.map((p) => (
                <div key={p.id} className="flex gap-3 py-4 items-center">
                  <button
                    onClick={() => navigate(`/products/${p.slug}`)}
                    className="w-16 h-16 bg-gray-100 rounded-lg flex-shrink-0 overflow-hidden hover:opacity-80 transition-opacity"
                  >
                    {p.imageUrl ? (
                      <img src={p.imageUrl} alt={p.name} className="w-full h-full object-cover" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-gray-300 text-xs">No img</div>
                    )}
                  </button>
                  <div className="flex-1 min-w-0">
                    <button
                      onClick={() => navigate(`/products/${p.slug}`)}
                      className="font-semibold text-sm text-gray-800 leading-tight line-clamp-2 text-left hover:text-orange-500 transition-colors w-full"
                    >
                      {p.name}
                    </button>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="text-orange-500 font-bold text-sm">{formatPrice(p.price)}</span>
                      {p.originalPrice > p.price && (
                        <span className="text-gray-400 text-xs line-through">{formatPrice(p.originalPrice)}</span>
                      )}
                    </div>
                    <button
                      disabled={addingId === p.id}
                      onClick={() => handleAddSuggestion(p)}
                      className="mt-2 flex items-center gap-1.5 border border-orange-500 text-orange-500 rounded-full px-3 py-1 text-xs font-semibold hover:bg-orange-50 transition-colors disabled:opacity-50"
                    >
                      <FiShoppingCart size={12} />
                      {addingId === p.id ? 'Đang thêm...' : 'Thêm vào giỏ'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}