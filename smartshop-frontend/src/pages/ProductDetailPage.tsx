import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { productService } from '../services/productService';
import { cartService } from '../services/cartService';
import { storeService } from '../services/storeService';
import { useAuthStore } from '../store/authStore';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';
import type { ProductDto } from '../types/product';
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

  const [product, setProduct] = useState<ProductDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);

  // Stock info from selected store
  const [stockInfo, setStockInfo] = useState<StockInfo | null>(null);
  const [stockLoading, setStockLoading] = useState(false);

  // Store selector modal
  const [storeModalOpen, setStoreModalOpen] = useState(false);

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    productService
      .getProductBySlug(slug)
      .then(setProduct)
      .catch(() => setError('Không tìm thấy món ăn.'))
      .finally(() => setLoading(false));

    fetchStores().catch(() => {});
  }, [slug, fetchStores]);

  // Fetch stock whenever product or selectedStore changes
  useEffect(() => {
    if (!product || !selectedStore) {
      setStockInfo(null);
      return;
    }
    setStockLoading(true);
    storeService
      .getProductStock(selectedStore.id, product.id)
      .then(setStockInfo)
      .catch(() => setStockInfo(null))
      .finally(() => setStockLoading(false));
  }, [product, selectedStore]);

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
        <p className="text-gray-500">{error ?? 'Không tìm thấy món ăn.'}</p>
        <Link to="/products" className="text-rose-600 hover:text-rose-800" title="Quay lại danh sách">
          <FiArrowLeft size={20} />
        </Link>
      </div>
    );
  }

  const handleAddToCart = async () => {
    if (!isAuthenticated) { navigate('/login'); return; }
    setAddingToCart(true);
    try {
      await cartService.addToCart(product!.id, quantity);
      refreshCartCount();
      toast.success(`Đã thêm ${quantity} phần vào giỏ món!`);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(msg ?? 'Thêm món vào giỏ thất bại.');
    } finally {
      setAddingToCart(false);
    }
  };

  const handleBuyNow = async () => {
    if (!isAuthenticated) { navigate('/login'); return; }
    setAddingToCart(true);
    try {
      await cartService.addToCart(product!.id, quantity);
      refreshCartCount();
      navigate('/checkout');
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(msg ?? 'Thêm món vào giỏ thất bại.');
      setAddingToCart(false);
    }
  };

  const discountPercent =
    product.originalPrice > product.price
      ? Math.round(((product.originalPrice - product.price) / product.originalPrice) * 100)
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
          {/* Image */}
          <div className="md:w-2/5">
            <div className="bg-gray-100 rounded-xl h-64 md:h-80 flex items-center justify-center overflow-hidden">
              {product.imageUrl ? (
                <img src={getImageUrl(product.imageUrl)} alt={product.name} className="h-full w-full object-contain" />
              ) : (
                <span className="text-gray-300 text-7xl">🍔</span>
              )}
            </div>
          </div>

          {/* Info */}
          <div className="flex-1 flex flex-col">
            <h1 className="text-xl font-bold text-gray-900">{product.name}</h1>

            <div className="mt-3 flex items-center gap-3">
              <span className="text-2xl font-bold text-rose-600">{formatPrice(product.price)}</span>
              {discountPercent > 0 && (
                <>
                  <span className="text-gray-400 line-through text-sm">{formatPrice(product.originalPrice)}</span>
                  <span className="bg-red-100 text-red-600 text-xs px-2 py-0.5 rounded-full font-medium">
                    -{discountPercent}%
                  </span>
                </>
              )}
            </div>

            {/* Stock info by selected store */}
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
                    Còn hàng tại {selectedStore.name} ({stockInfo.quantity} phần)
                  </span>
                )
              ) : (
                <span className="text-gray-400">Không có thông tin tồn kho</span>
              )}
            </div>

            <p className="mt-4 text-sm text-gray-600 leading-relaxed">{product.description}</p>

            <div className="mt-4 flex flex-wrap gap-2 text-xs">
              <span className="rounded-full bg-amber-100 px-3 py-1 font-medium text-amber-700">Chuẩn bị 15-20 phút</span>
              <span className="rounded-full bg-emerald-100 px-3 py-1 font-medium text-emerald-700">Giao nhanh nội thành</span>
              <span className="rounded-full bg-rose-100 px-3 py-1 font-medium text-rose-700">Ăn nóng ngon hơn</span>
            </div>

            {/* Low stock badge */}
            {stockInfo && stockInfo.quantity > 0 && stockInfo.quantity <= 5 && (
              <span className="inline-block mt-2 bg-orange-100 text-orange-700 text-xs font-medium px-2.5 py-1 rounded-full">
                Còn ít hàng
              </span>
            )}

            {/* Quantity + Add to Cart */}
            {stockInfo === null || stockInfo.quantity > 0 ? (
              <div className="mt-6 space-y-3">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-gray-400">Số phần</p>
                <div className="flex items-center gap-3">
                  <div className="flex items-center border rounded-lg overflow-hidden">
                    <button
                      onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                      className="px-3 py-2 hover:bg-gray-100 text-gray-600 text-lg"
                    >
                      −
                    </button>
                    <span className="px-4 py-2 font-medium text-sm">{quantity}</span>
                    <button
                      onClick={() => setQuantity((q) => Math.min(stockInfo?.quantity ?? 99, q + 1))}
                      className="px-3 py-2 hover:bg-gray-100 text-gray-600 text-lg"
                    >
                      +
                    </button>
                  </div>
                  <button
                    onClick={handleAddToCart}
                    disabled={addingToCart || (stockInfo !== null && stockInfo.quantity === 0)}
                    className="flex-1 border border-rose-600 text-rose-600 rounded-lg py-2 text-sm font-medium hover:bg-rose-50 transition-colors disabled:opacity-50"
                  >
                    {addingToCart ? 'Đang thêm...' : 'Thêm món'}
                  </button>
                  <button
                    onClick={handleBuyNow}
                    disabled={addingToCart || (stockInfo !== null && stockInfo.quantity === 0)}
                    className="flex-1 bg-rose-600 text-white rounded-lg py-2 text-sm font-medium hover:bg-rose-700 transition-colors disabled:opacity-50"
                  >
                    Đặt ngay
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
