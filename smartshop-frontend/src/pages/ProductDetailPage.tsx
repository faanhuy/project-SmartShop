import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { productService } from '../services/productService';
import { cartService } from '../services/cartService';
import { useAuthStore } from '../store/authStore';
import type { ProductDto } from '../types/product';
import { FiLogOut, FiArrowLeft, FiHome } from 'react-icons/fi';
import RecommendationCarousel from '../components/RecommendationCarousel';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

export default function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { isAuthenticated, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);
  const [cartMessage, setCartMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    productService
      .getProductBySlug(slug)
      .then(setProduct)
      .catch(() => setError('Không tìm thấy sản phẩm.'))
      .finally(() => setLoading(false));
  }, [slug]);

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
        <Link to="/products" className="text-blue-600 hover:text-blue-800" title="Quay lại danh sách">
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
    setAddingToCart(true);
    setCartMessage(null);
    try {
      await cartService.addToCart(product!.id, quantity);
      setCartMessage({ type: 'success', text: `Đã thêm ${quantity} sản phẩm vào giỏ hàng!` });
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      setCartMessage({ type: 'error', text: msg ?? 'Thêm vào giỏ hàng thất bại.' });
    } finally {
      setAddingToCart(false);
    }
  };

  const discountPercent =
    product.originalPrice > product.price
      ? Math.round(((product.originalPrice - product.price) / product.originalPrice) * 100)
      : 0;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-5xl mx-auto px-4 py-3 flex items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <Link to="/products" className="text-gray-500 hover:text-blue-600" title="Quay lại">
              <FiArrowLeft size={20} />
            </Link>
            <span className="text-gray-300">|</span>
            <Link to="/" className="text-gray-500 hover:text-blue-600" title="Trang chủ">
              <FiHome size={20} />
            </Link>
          </div>
          {isAuthenticated && (
            <button
              onClick={handleLogout}
              className="text-red-500 hover:text-red-700"
              title="Đăng xuất"
            >
              <FiLogOut size={20} />
            </button>
          )}
        </div>
      </header>

      <div className="max-w-5xl mx-auto px-4 py-8">
        <div className="bg-white rounded-2xl shadow-sm p-6 flex flex-col md:flex-row gap-8">
          {/* Image */}
          <div className="md:w-2/5">
            <div className="bg-gray-100 rounded-xl h-64 md:h-80 flex items-center justify-center overflow-hidden">
              {product.imageUrl ? (
                <img src={product.imageUrl} alt={product.name} className="h-full w-full object-contain" />
              ) : (
                <span className="text-gray-300 text-7xl">📦</span>
              )}
            </div>
          </div>

          {/* Info */}
          <div className="flex-1 flex flex-col">
            <h1 className="text-xl font-bold text-gray-900">{product.name}</h1>

            <div className="mt-3 flex items-center gap-3">
              <span className="text-2xl font-bold text-blue-600">{formatPrice(product.price)}</span>
              {discountPercent > 0 && (
                <>
                  <span className="text-gray-400 line-through text-sm">{formatPrice(product.originalPrice)}</span>
                  <span className="bg-red-100 text-red-600 text-xs px-2 py-0.5 rounded-full font-medium">
                    -{discountPercent}%
                  </span>
                </>
              )}
            </div>

            <p className="mt-1 text-sm text-gray-500">
              {product.stock > 0 ? (
                product.stock < 10 ? (
                  <span className="text-orange-500">Còn {product.stock} sản phẩm</span>
                ) : (
                  <span className="text-green-600">Còn hàng ({product.stock})</span>
                )
              ) : (
                <span className="text-red-500">Hết hàng</span>
              )}
            </p>

            <p className="mt-4 text-sm text-gray-600 leading-relaxed">{product.description}</p>

            {/* Quantity + Add to Cart */}
            {product.stock > 0 ? (
              <div className="mt-6 space-y-3">
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
                      onClick={() => setQuantity((q) => Math.min(product.stock, q + 1))}
                      className="px-3 py-2 hover:bg-gray-100 text-gray-600 text-lg"
                    >
                      +
                    </button>
                  </div>
                  <button
                    onClick={handleAddToCart}
                    disabled={addingToCart}
                    className="flex-1 bg-blue-600 text-white rounded-lg py-2 text-sm font-medium hover:bg-blue-700 transition-colors disabled:opacity-50"
                  >
                    {addingToCart ? 'Đang thêm...' : 'Thêm vào giỏ hàng'}
                  </button>
                </div>

                {cartMessage && (
                  <div className={`flex items-center justify-between rounded-lg px-4 py-2 text-sm ${
                    cartMessage.type === 'success'
                      ? 'bg-green-50 text-green-700 border border-green-200'
                      : 'bg-red-50 text-red-700 border border-red-200'
                  }`}>
                    <span>{cartMessage.text}</span>
                    {cartMessage.type === 'success' && (
                      <button
                        onClick={() => navigate('/cart')}
                        className="ml-3 font-medium underline hover:no-underline whitespace-nowrap"
                      >
                        Xem giỏ hàng →
                      </button>
                    )}
                  </div>
                )}
              </div>
            ) : null}
          </div>
        </div>

        <RecommendationCarousel productId={product.id} />
      </div>
    </div>
  );
}
