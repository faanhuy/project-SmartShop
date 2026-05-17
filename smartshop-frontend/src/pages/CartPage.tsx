import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { cartService } from '../services/cartService';
import { productService } from '../services/productService';
import { sizeService } from '../services/sizeService';
import { getApiError } from '../utils/errorHandler';
import { formatPrice } from '../utils/formatters';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';
import type { CartDto, CartItemDto } from '../types/cart';
import type { ProductDto } from '../types/product';
import type { EffectivePriceItem } from '../types/size';
import { FiMinus, FiPlus, FiShoppingCart } from 'react-icons/fi';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import CouponInput from '../components/CouponInput';
import type { ValidateCouponResult } from '../services/couponService';
import { couponSession } from '../utils/couponSession';
import { couponService } from '../services/couponService';
import { getImageUrl } from '../utils/imageUrl';

export default function CartPage() {
  const [cart, setCart] = useState<CartDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [appliedCoupon, setAppliedCoupon] = useState<ValidateCouponResult | null>(null);
  const [appliedCode, setAppliedCode] = useState('');
  const [suggestions, setSuggestions] = useState<ProductDto[]>([]);
  const [addingId, setAddingId] = useState<string | null>(null);
  const [expandedComponents, setExpandedComponents] = useState<Set<string>>(new Set());
  const [effectivePrices, setEffectivePrices] = useState<Map<string, EffectivePriceItem>>(new Map());
  const navigate = useNavigate();
  const { selectedStore } = useStoreSelectionStore();

  useEffect(() => {
    const saved = couponSession.load();
    if (saved) {
      setAppliedCode(saved.code);
      setAppliedCoupon(saved.result);
    }
  }, []);

  const loadEffectivePrices = async (cartData: CartDto, storeId: string) => {
    const productItems = cartData.items.filter((i) => i.itemType === 'Product' && i.productId);
    if (productItems.length === 0) {
      setEffectivePrices(new Map());
      return;
    }
    try {
      const items = productItems.map((i) => ({ productId: i.productId!, sizeId: i.sizeId }));
      const prices = await sizeService.getBulkEffectivePrices(storeId, items);
      const map = new Map<string, EffectivePriceItem>();
      prices.forEach((p) => map.set(`${p.productId}:${p.sizeId ?? ''}`, p));
      setEffectivePrices(map);
    } catch {
      setEffectivePrices(new Map());
    }
  };

  const getEffectiveUnitPrice = (item: CartItemDto): number => {
    if (item.itemType !== 'Product' || !item.productId) return item.unitPrice;
    const key = `${item.productId}:${item.sizeId ?? ''}`;
    return effectivePrices.get(key)?.effectivePrice ?? item.unitPrice;
  };

  const revalidateCoupon = async (updatedCart: CartDto, code: string) => {
    const newTotal = updatedCart.items.reduce((sum, i) => sum + getEffectiveUnitPrice(i) * i.quantity, 0);
    try {
      const result = await couponService.validate(code, newTotal);
      setAppliedCoupon(result);
      couponSession.save(code, result);
    } catch {
      setAppliedCoupon(null);
      setAppliedCode('');
      couponSession.clear();
      toast('Mã giảm giá đã bị gỡ vì giỏ món không còn đủ điều kiện.', { icon: 'ℹ️' });
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

  useEffect(() => {
    if (!cart || !selectedStore) {
      setEffectivePrices(new Map());
      return;
    }
    loadEffectivePrices(cart, selectedStore.id);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cart, selectedStore]);

  const loadSuggestions = async (cartData: CartDto | null) => {
    if (!cartData || cartData.items.length === 0) return;
    const firstProduct = cartData.items.find((i) => i.itemType === 'Product');
    if (!firstProduct?.productId) return;
    try {
      const product = await productService.getProductById(firstProduct.productId);
      if (!product) return;
      const cartProductIds = new Set(
        cartData.items.filter((i) => i.itemType === 'Product').map((i) => i.productId!)
      );
      const result = await productService.getProducts({
        categoryId: product.categoryId,
        sortBy: 5,
        pageSize: 8,
        page: 1,
      });
      const filtered = (result?.items ?? []).filter((p) => !cartProductIds.has(p.id));
      setSuggestions(filtered.slice(0, 6));
    } catch {
      // không hiện lỗi
    }
  };

  useEffect(() => {
    loadCart().then((data) => loadSuggestions(data));
  }, []);

  const handleUpdateQuantity = async (item: CartItemDto, quantity: number) => {
    if (quantity <= 0) return handleRemove(item);
    try {
      const updated = await cartService.updateItemByLineId(item.id, quantity);
      setCart(updated);
      if (appliedCode) await revalidateCoupon(updated, appliedCode);
    } catch (err) {
      toast.error(getApiError(err, 'Cập nhật thất bại.'));
    }
  };

  const handleRemove = async (item: CartItemDto) => {
    try {
      const updated = await cartService.removeItemByLineId(item.id);
      setCart(updated);
      if (appliedCode) await revalidateCoupon(updated, appliedCode);
    } catch {
      toast.error('Gỡ món thất bại.');
    }
  };

  const handleClear = async () => {
    if (!confirm('Xóa toàn bộ giỏ món?')) return;
    try {
      await cartService.clearCart();
      setCart(null);
      setAppliedCoupon(null);
      setAppliedCode('');
      couponSession.clear();
    } catch {
      toast.error('Xóa giỏ món thất bại.');
    }
  };

  const toggleComponents = (itemId: string) => {
    setExpandedComponents((prev) => {
      const next = new Set(prev);
      if (next.has(itemId)) next.delete(itemId);
      else next.add(itemId);
      return next;
    });
  };

  const originalTotal = cart ? cart.items.reduce((sum, i) => sum + getEffectiveUnitPrice(i) * i.quantity, 0) : 0;
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
      toast.success(`Đã thêm "${product.name}" vào giỏ món`);
    } catch (err) {
      toast.error(getApiError(err, 'Thêm món vào giỏ thất bại.'));
    } finally {
      setAddingId(null);
    }
  };

  if (loading)
    return (
      <div className="min-h-screen bg-gray-50">
        <Navbar />
        <div className="p-8 text-center text-gray-400">Đang tải...</div>
      </div>
    );

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div
        id="cart-layout"
        className="max-w-7xl mx-auto p-6 flex flex-col md:flex-row gap-8 justify-center items-start"
      >
        {/* Cột trái: Giỏ hàng */}
        <div id="cart-main" className="flex-1 min-w-[350px] max-w-xl flex flex-col items-center">
          <div className="flex items-center justify-between gap-4 mb-6 w-full">
            <h1 id="cart-title" className="text-xl font-bold ml-8">
              Giỏ món của bạn:
            </h1>
            {cart && cart.items.length > 0 && (
              <button
                onClick={handleClear}
                className="rounded-full border border-gray-300 px-4 py-2 text-xs font-semibold text-gray-600 transition-colors hover:border-red-300 hover:text-red-500"
              >
                Xóa giỏ
              </button>
            )}
          </div>
          {error && <p className="text-red-500 mb-4">{error}</p>}
          {!cart || cart.items.length === 0 ? (
            <div className="text-center py-16 text-gray-500">
              <p className="text-lg mb-4">Giỏ món đang trống</p>
              <button
                onClick={() => navigate('/products')}
                className="bg-rose-600 text-white px-6 py-2 rounded hover:bg-rose-700 flex items-center gap-2 mx-auto"
                title="Tiếp tục đặt món"
              >
                <FiPlus size={18} />
                Tiếp tục đặt món
              </button>
            </div>
          ) : (
            <div id="cart-content" className="max-w-lg w-full">
              <div id="cart-items" className="space-y-4 mb-4 w-full">
                {cart.items.map((item) => (
                  <div
                    key={item.id}
                    className="border rounded-lg bg-white overflow-hidden"
                    id={`cart-item-${item.id}`}
                  >
                    <div className="flex gap-4 p-4 items-start">
                      <div className="w-20 h-20 bg-gray-100 rounded flex-shrink-0 overflow-hidden">
                        {item.imageUrl ? (
                          <img
                            src={getImageUrl(item.imageUrl)}
                            alt={item.displayName}
                            className="w-full h-full object-cover"
                          />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-gray-400 text-xs">
                            No img
                          </div>
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          {item.itemType === 'Combo' && (
                            <span className="bg-orange-100 text-orange-600 text-xs font-bold px-2 py-0.5 rounded-full">
                              COMBO
                            </span>
                          )}
                          <div className="font-bold text-base">{item.displayName}</div>
                        </div>
                        {item.sizeLabel && (
                          <div className="text-xs text-gray-500 mb-1">
                            Size: <span className="font-medium text-gray-700">{item.sizeLabel}</span>
                          </div>
                        )}
                        <div className="flex gap-2 text-amber-600 font-bold items-center mb-1">
                          <span className="text-xl">{(getEffectiveUnitPrice(item) * item.quantity).toLocaleString('vi-VN')} đ</span>
                          {item.itemType === 'Product' && effectivePrices.get(`${item.productId}:${item.sizeId ?? ''}`)?.hasPromotion && (
                            <span className="text-gray-400 line-through text-sm font-normal">
                              {item.subTotal.toLocaleString('vi-VN')} đ
                            </span>
                          )}
                        </div>
                        <div className="flex gap-4 text-sm mt-1">
                          <button
                            className="text-amber-600 hover:underline"
                            onClick={() => handleRemove(item)}
                          >
                            Gỡ món
                          </button>
                          {item.itemType === 'Combo' && item.components.length > 0 && (
                            <button
                              className="text-gray-500 hover:underline flex items-center gap-1"
                              onClick={() => toggleComponents(item.id)}
                            >
                              {expandedComponents.has(item.id) ? 'Ẩn' : 'Xem'} chi tiết
                            </button>
                          )}
                        </div>
                      </div>
                      <div className="flex flex-col items-center gap-2">
                        <div className="flex items-center gap-2 bg-gray-100 rounded-full px-2 py-1">
                          <button
                            className="w-7 h-7 rounded-full flex items-center justify-center hover:bg-gray-200"
                            onClick={() => handleUpdateQuantity(item, item.quantity - 1)}
                          >
                            <FiMinus size={14} />
                          </button>
                          <span className="w-6 text-center">{item.quantity}</span>
                          <button
                            className="w-7 h-7 rounded-full flex items-center justify-center hover:bg-gray-200"
                            onClick={() => handleUpdateQuantity(item, item.quantity + 1)}
                          >
                            <FiPlus size={14} />
                          </button>
                        </div>
                      </div>
                    </div>

                    {/* Combo components expandable */}
                    {item.itemType === 'Combo' &&
                      expandedComponents.has(item.id) &&
                      item.components.length > 0 && (
                        <div className="border-t bg-orange-50 px-4 py-3">
                          <p className="text-xs font-semibold text-orange-700 mb-2">
                            Gồm các món:
                          </p>
                          <ul className="space-y-1">
                            {item.components.map((c, idx) => (
                              <li key={idx} className="flex justify-between text-xs text-gray-600">
                                <span>
                                  {c.productName}
                                  {c.sizeLabel && (
                                    <span className="text-gray-400"> ({c.sizeLabel})</span>
                                  )}
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
                  <span className="text-rose-600 font-semibold">
                    {originalTotal.toLocaleString('vi-VN')} <span className="underline">đ</span>
                  </span>
                </div>
                {discountAmount > 0 && (
                  <div className="flex justify-between">
                    <span className="font-semibold">Giảm giá ({appliedCode})</span>
                    <span className="text-green-600 font-semibold">
                      -{discountAmount.toLocaleString('vi-VN')} <span className="underline">đ</span>
                    </span>
                  </div>
                )}
                <div className="flex justify-between font-bold text-xl border-t pt-2">
                  <span>Tổng cộng</span>
                  <span className="text-rose-600">
                    {finalTotal.toLocaleString('vi-VN')} <span className="underline">đ</span>
                  </span>
                </div>
              </div>

              <button
                onClick={() => navigate('/checkout')}
                className="w-full bg-rose-600 text-white py-3 rounded-full font-bold text-lg hover:bg-rose-700 transition-colors mt-2"
              >
                Xác nhận đặt món
              </button>
            </div>
          )}
        </div>

        {/* Cột phải: Món đề xuất */}
        {suggestions.length > 0 && (
          <div
            id="cart-suggestion"
            className="w-full md:w-[380px] bg-white rounded-3xl shadow p-6 flex-shrink-0 border"
          >
            <h2 id="suggestion-title" className="text-lg font-semibold mb-1">
              Món gợi ý thêm
            </h2>
            <p className="text-xs text-gray-400 mb-4">Bán chạy cùng nhóm món hôm nay</p>
            <div
              id="suggestion-list"
              className="divide-y max-h-[calc(100vh-240px)] overflow-y-auto"
            >
              {suggestions.map((p) => (
                <div key={p.id} className="flex gap-3 py-4 items-center">
                  <button
                    onClick={() => navigate(`/products/${p.slug}`)}
                    className="w-16 h-16 bg-gray-100 rounded-lg flex-shrink-0 overflow-hidden hover:opacity-80 transition-opacity"
                  >
                    {p.imageUrl ? (
                      <img
                        src={getImageUrl(p.imageUrl)}
                        alt={p.name}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-gray-300 text-xs">
                        No img
                      </div>
                    )}
                  </button>
                  <div className="flex-1 min-w-0">
                    <button
                      onClick={() => navigate(`/products/${p.slug}`)}
                      className="font-semibold text-sm text-gray-800 leading-tight line-clamp-2 text-left hover:text-rose-600 transition-colors w-full"
                    >
                      {p.name}
                    </button>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="text-rose-600 font-bold text-sm">{formatPrice(p.price)}</span>
                      {p.originalPrice > p.price && (
                        <span className="text-gray-400 text-xs line-through">
                          {formatPrice(p.originalPrice)}
                        </span>
                      )}
                    </div>
                    <button
                      disabled={addingId === p.id}
                      onClick={() => handleAddSuggestion(p)}
                      className="mt-2 flex items-center gap-1.5 border border-rose-500 text-rose-600 rounded-full px-3 py-1 text-xs font-semibold hover:bg-rose-50 transition-colors disabled:opacity-50"
                    >
                      <FiShoppingCart size={12} />
                      {addingId === p.id ? 'Đang thêm...' : 'Thêm món'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
      <Footer />
    </div>
  );
}
