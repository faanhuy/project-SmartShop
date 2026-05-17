import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { productService } from '../services/productService';
import { cartService } from '../services/cartService';
import { storeService } from '../services/storeService';
import { useAuthStore } from '../store/authStore';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';
import type { ProductDetailDto } from '../types/product';
import type { StockInfo } from '../types/store';
import { FiArrowLeft, FiMapPin } from 'react-icons/fi';
import RecommendationCarousel from '../components/RecommendationCarousel';
import ProductReviews from '../components/ProductReviews';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import StoreSelectorModal from '../components/StoreSelectorModal';
import { getImageUrl } from '../utils/imageUrl';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

export default function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { isAuthenticated, refreshCartCount } = useAuthStore();
  const { selectedStore, fetchStores } = useStoreSelectionStore();

  const [product, setProduct] = useState<ProductDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);

  const [stockInfo, setStockInfo] = useState<StockInfo | null>(null);
  const [stockLoading, setStockLoading] = useState(false);

  const [storeModalOpen, setStoreModalOpen] = useState(false);

  const [selectedSizeId, setSelectedSizeId] = useState<string | null>(null);
  const [sizeStockById, setSizeStockById] = useState<Record<string, number>>({});
  const [sizeStockLoading, setSizeStockLoading] = useState(false);

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    productService
      .getProductBySlug(slug, selectedStore?.id)
      .then(setProduct)
      .catch(() => setError('Không tìm thấy sản phẩm.'))
      .finally(() => setLoading(false));

    fetchStores().catch(() => {});
  }, [slug, selectedStore?.id, fetchStores]);

  useEffect(() => {
    if (!product || !selectedStore) {
      setStockInfo(null);
      setSizeStockById({});
      return;
    }
    setStockLoading(true);
    storeService
      .getProductStock(selectedStore.id, product.id)
      .then(setStockInfo)
      .catch(() => setStockInfo(null))
      .finally(() => setStockLoading(false));

    if (product.hasSizes) {
      setSizeStockLoading(true);
      storeService
        .getProductSizeStock(selectedStore.id, product.id)
        .then((items) => {
          setSizeStockById(
            items.reduce<Record<string, number>>((acc, item) => {
              acc[item.sizeId] = item.quantity;
              return acc;
            }, {}),
          );
        })
        .catch(() => setSizeStockById({}))
        .finally(() => setSizeStockLoading(false));
    } else {
      setSizeStockById({});
    }
  }, [product, selectedStore]);

  const selectedSizeStock = selectedSizeId ? sizeStockById[selectedSizeId] ?? 0 : null;
  const availableQuantity = product?.hasSizes && selectedSizeId
    ? selectedSizeStock ?? 0
    : stockInfo?.quantity ?? null;
  const isOutOfStock = availableQuantity !== null && availableQuantity === 0;

  useEffect(() => {
    if (availableQuantity !== null && availableQuantity > 0) {
      setQuantity((q) => Math.min(q, availableQuantity));
    }
  }, [availableQuantity]);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center text-gray-400">
        Đang tải...
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center gap-4">
        <p className="text-gray-500">{error ?? 'Không tìm thấy sản phẩm.'}</p>
        <Link to="/products" className="text-rose-600 hover:text-rose-800" title="Quay lại danh sách">
          <FiArrowLeft size={20} />
        </Link>
      </div>
    );
  }

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (product.hasSizes && !selectedSizeId) {
      toast.error('Vui lòng chọn size trước khi thêm vào giỏ.');
      return;
    }
    if (isOutOfStock) {
      toast.error('Size đã chọn đang hết hàng tại chi nhánh này.');
      return;
    }
    setAddingToCart(true);
    try {
      await cartService.addToCart(product.id, quantity, selectedSizeId ?? undefined);
      refreshCartCount();
      toast.success(`Đã thêm ${quantity} vào giỏ hàng!`);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(msg ?? 'Thêm vào giỏ thất bại.');
    } finally {
      setAddingToCart(false);
    }
  };

  const handleBuyNow = async () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (product.hasSizes && !selectedSizeId) {
      toast.error('Vui lòng chọn size trước khi đặt hàng.');
      return;
    }
    if (isOutOfStock) {
      toast.error('Size đã chọn đang hết hàng tại chi nhánh này.');
      return;
    }
    setAddingToCart(true);
    try {
      await cartService.addToCart(product.id, quantity, selectedSizeId ?? undefined);
      refreshCartCount();
      navigate('/checkout');
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(msg ?? 'Thêm vào giỏ thất bại.');
      setAddingToCart(false);
    }
  };

  const selectedSize = selectedSizeId && product?.sizes
    ? product.sizes.find((sz) => sz.id === selectedSizeId)
    : null;

  const displayPrice = product?.hasSizes && selectedSize
    ? selectedSize.effectivePrice ?? product.price
    : product?.effectivePrice ?? product?.price ?? 0;

  const originalDisplayPrice = displayPrice < product!.price ? product!.price : null;
  const hasDiscount = originalDisplayPrice !== null;
  const discountPercent = hasDiscount
    ? Math.round(((originalDisplayPrice - displayPrice) / originalDisplayPrice) * 100)
    : 0;

  return (
    <div className="min-h-screen bg-gray-50">
      <StoreSelectorModal
        isOpen={storeModalOpen}
        onClose={() => setStoreModalOpen(false)}
      />
      <Navbar>
        <Link to="/products" className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-rose-600">
          <FiArrowLeft size={16} />
          Quay lại
        </Link>
      </Navbar>

      <div className="max-w-5xl mx-auto px-4 py-8">
        <div className="bg-white rounded-2xl shadow-sm p-6 flex flex-col md:flex-row gap-8">
          <div className="md:w-2/5 self-start">
            <div className="bg-gray-100 rounded-xl h-64 md:h-80 flex items-center justify-center overflow-hidden">
              {product.imageUrl ? (
                <img src={getImageUrl(product.imageUrl)} alt={product.name} className="h-full w-full object-contain" />
              ) : (
                <span className="text-gray-400 text-sm">Không có ảnh</span>
              )}
            </div>
          </div>

          <div className="flex-1 flex flex-col">
            <h1 className="text-xl font-bold text-gray-900">{product.name}</h1>

            <div className="mt-3 flex items-center gap-3">
              <span className="text-2xl font-bold text-rose-600">{formatPrice(displayPrice)}</span>
              {hasDiscount && originalDisplayPrice && (
                <>
                  <span className="text-gray-400 line-through text-sm">{formatPrice(originalDisplayPrice)}</span>
                  {discountPercent > 0 && (
                    <span className="bg-red-100 text-red-600 text-xs px-2 py-0.5 rounded-full font-medium">
                      -{discountPercent}%
                    </span>
                  )}
                </>
              )}
            </div>

            <div className="mt-1 text-sm">
              {!selectedStore ? (
                <button
                  type="button"
                  onClick={() => setStoreModalOpen(true)}
                  className="text-amber-600 hover:text-amber-700 flex items-center gap-1"
                >
                  <FiMapPin size={13} />
                  Chọn chi nhánh để xem tồn kho
                </button>
              ) : stockLoading ? (
                <span className="text-gray-400">Đang kiểm tra tồn kho...</span>
              ) : stockInfo ? (
                stockInfo.quantity === 0 ? (
                  <span className="text-red-500 font-medium">Hết hàng tại chi nhánh này</span>
                ) : (
                  <span className="text-green-600">
                    Còn hàng tại {selectedStore.name} ({stockInfo.quantity} sản phẩm)
                  </span>
                )
              ) : (
                <span className="text-gray-400">Không có thông tin tồn kho</span>
              )}
            </div>

            <p className="mt-4 text-sm text-gray-600 leading-relaxed">{product.description}</p>

            {product.hasSizes && (
              <div className="mt-4">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-gray-400 mb-2">
                  Chọn size
                  <span className="text-red-500 ml-1">*</span>
                </p>
                {product.sizes.length === 0 ? (
                  <p className="text-sm text-gray-400">Không có size khả dụng.</p>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    {product.sizes.map((sz) => {
                      const sizeStock = sizeStockById[sz.id] ?? 0;
                      return (
                        <button
                          key={sz.id}
                          onClick={() => setSelectedSizeId(sz.id === selectedSizeId ? null : sz.id)}
                          disabled={!sz.isActive}
                          className={`min-w-[76px] px-3 py-1.5 rounded-lg border text-sm font-medium transition-colors ${
                            selectedSizeId === sz.id
                              ? 'border-rose-600 bg-rose-600 text-white'
                              : sz.isActive
                              ? 'border-gray-300 text-gray-700 hover:border-rose-400 hover:text-rose-600'
                              : 'border-gray-200 text-gray-300 cursor-not-allowed bg-gray-50'
                          }`}
                        >
                          <span className="block">{sz.label}</span>
                          {selectedStore && (
                            <span className={`block text-[11px] ${selectedSizeId === sz.id ? 'text-white/80' : 'text-gray-400'}`}>
                              {sizeStockLoading ? '...' : sizeStock > 0 ? `Còn ${sizeStock}` : 'Hết hàng'}
                            </span>
                          )}
                        </button>
                      );
                    })}
                  </div>
                )}
                {product.hasSizes && !selectedSizeId && (
                  <p className="mt-1 text-xs text-amber-600">Vui lòng chọn size trước khi thêm vào giỏ</p>
                )}
                {product.hasSizes && selectedSizeId && (
                  <p className={`mt-1 text-xs ${selectedSizeStock && selectedSizeStock > 0 ? 'text-green-600' : 'text-red-500'}`}>
                    {selectedSizeStock && selectedSizeStock > 0
                      ? `Size đã chọn còn ${selectedSizeStock} sản phẩm tại ${selectedStore?.name ?? 'chi nhánh này'}`
                      : 'Size đã chọn đang hết hàng tại chi nhánh này'}
                  </p>
                )}
              </div>
            )}

            {availableQuantity !== null && availableQuantity > 0 && availableQuantity <= 5 && (
              <span className="inline-block mt-2 bg-orange-100 text-orange-700 text-xs font-medium px-2.5 py-1 rounded-full">
                Còn ít hàng
              </span>
            )}

            {availableQuantity === null || availableQuantity > 0 ? (
              <div className="mt-6 space-y-3">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-gray-400">Số lượng</p>
                <div className="flex items-center gap-3">
                  <div className="flex items-center border rounded-lg overflow-hidden">
                    <button
                      onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                      className="px-3 py-2 hover:bg-gray-100 text-gray-600 text-lg"
                    >
                      -
                    </button>
                    <span className="px-4 py-2 font-medium text-sm">{quantity}</span>
                    <button
                      onClick={() => setQuantity((q) => Math.min(availableQuantity ?? 99, q + 1))}
                      className="px-3 py-2 hover:bg-gray-100 text-gray-600 text-lg"
                    >
                      +
                    </button>
                  </div>
                  <button
                    onClick={handleAddToCart}
                    disabled={addingToCart || isOutOfStock}
                    className="flex-1 border border-rose-600 text-rose-600 rounded-lg py-2 text-sm font-medium hover:bg-rose-50 transition-colors disabled:opacity-50"
                  >
                    {addingToCart ? 'Đang thêm...' : 'Thêm vào giỏ'}
                  </button>
                  <button
                    onClick={handleBuyNow}
                    disabled={addingToCart || isOutOfStock}
                    className="flex-1 bg-rose-600 text-white rounded-lg py-2 text-sm font-medium hover:bg-rose-700 transition-colors disabled:opacity-50"
                  >
                    Mua ngay
                  </button>
                </div>
              </div>
            ) : (
              <div className="mt-6">
                <button
                  disabled
                  className="w-full bg-gray-100 text-gray-400 rounded-lg py-2.5 text-sm font-medium cursor-not-allowed"
                >
                  Hết hàng tại chi nhánh này
                </button>
              </div>
            )}
          </div>
        </div>

        <ProductReviews productId={product.id} />
        <RecommendationCarousel productId={product.id} />
      </div>
      <Footer />
    </div>
  );
}
