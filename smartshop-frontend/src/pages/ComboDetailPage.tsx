import { useEffect, useState } from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { FiArrowLeft } from 'react-icons/fi';
import { comboService } from '../services/comboService';
import { cartService } from '../services/cartService';
import { useAuthStore } from '../store/authStore';
import { formatPrice, formatDate } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';
import { getApiError } from '../utils/errorHandler';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import type { ComboDto } from '../types/promotion';

export default function ComboDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user, refreshCartCount } = useAuthStore();

  const [combo, setCombo] = useState<ComboDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [descExpanded, setDescExpanded] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    comboService
      .getComboById(id)
      .then(setCombo)
      .catch(() => setError('Không tìm thấy combo.'))
      .finally(() => setLoading(false));
  }, [id]);

  const handleAddToCart = async () => {
    if (!combo) return;
    setAdding(true);
    try {
      await cartService.addComboToCart(combo.id, 1);
      refreshCartCount();
      toast.success(`Đã thêm combo "${combo.name}" vào giỏ hàng`);
    } catch (err) {
      toast.error(getApiError(err, 'Thêm combo thất bại.'));
    } finally {
      setAdding(false);
    }
  };

  if (!user) return <Navigate to="/login" />;

  if (loading) {
    return (
      <>
        <Navbar>
          <Link to="/products" className="flex items-center gap-1 text-blue-600 hover:text-blue-700 text-sm">
            <FiArrowLeft size={16} />
            Quay lại
          </Link>
        </Navbar>
        <div className="flex-1 max-w-4xl mx-auto px-4 py-8 flex items-center justify-center">
          <div className="text-gray-500">Đang tải...</div>
        </div>
        <Footer />
      </>
    );
  }

  if (error || !combo) {
    return (
      <>
        <Navbar>
          <Link to="/products" className="flex items-center gap-1 text-blue-600 hover:text-blue-700 text-sm">
            <FiArrowLeft size={16} />
            Quay lại
          </Link>
        </Navbar>
        <div className="flex-1 max-w-4xl mx-auto px-4 py-8">
          <div className="bg-white rounded-lg shadow p-6 text-center">
            <p className="text-red-600 mb-4">{error || 'Không tìm thấy combo.'}</p>
            <Link to="/products" className="text-blue-600 hover:text-blue-700">
              Quay lại trang sản phẩm
            </Link>
          </div>
        </div>
        <Footer />
      </>
    );
  }

  const realtimeOriginal = combo.currentOriginalPrice > 0 ? combo.currentOriginalPrice : combo.originalPrice;
  const discountPercent = realtimeOriginal > combo.salePrice
    ? Math.round(((realtimeOriginal - combo.salePrice) / realtimeOriginal) * 100)
    : 0;

  const endDate = combo.endsAt ? new Date(combo.endsAt) : null;
  const now = new Date();
  const isActive = combo.isCurrentlyActive && combo.isActive;

  let validityText = '';
  if (combo.startsAt) {
    validityText = `Từ ${formatDate(combo.startsAt)}`;
    if (endDate) {
      validityText += ` đến ${formatDate(combo.endsAt!)}`;
    } else {
      validityText += ' (Không hạn)';
    }
  }

  const daysLeft = endDate ? Math.ceil((endDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)) : 0;
  const daysLeftText = daysLeft > 0 ? `Còn ${daysLeft} ngày` : '';

  return (
    <div className="flex flex-col min-h-screen bg-gray-50">
      <Navbar>
        <Link to="/products" className="flex items-center gap-1 text-blue-600 hover:text-blue-700 text-sm">
          <FiArrowLeft size={16} />
          Quay lại
        </Link>
      </Navbar>

      <main className="flex-1 max-w-4xl mx-auto px-4 py-8 w-full">
        <div className="bg-white rounded-2xl shadow p-6">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-8">
            {/* Image Section */}
            <div className="md:col-span-2 flex items-start justify-center">
              <div className="bg-gray-100 rounded-lg w-full aspect-square flex items-center justify-center overflow-hidden">
                {combo.imageUrl ? (
                  <img
                    src={getImageUrl(combo.imageUrl)}
                    alt={combo.name}
                    className="w-full h-full object-contain"
                  />
                ) : (
                  <span className="text-6xl text-gray-300">📦</span>
                )}
              </div>
            </div>

            {/* Info Section */}
            <div className="md:col-span-3 flex flex-col justify-between">
              {/* Status Badge */}
              <div className="flex items-center gap-2 mb-2">
                {isActive ? (
                  <span className="inline-block bg-green-100 text-green-700 text-xs font-semibold px-3 py-1 rounded-full">
                    Đang chạy
                  </span>
                ) : (
                  <span className="inline-block bg-gray-100 text-gray-600 text-xs font-semibold px-3 py-1 rounded-full">
                    Không hoạt động
                  </span>
                )}
              </div>

              {/* Title & Name */}
              <div className="mb-4">
                <h1 className="text-3xl font-bold text-gray-900">{combo.title}</h1>
                <p className="text-gray-500 text-lg mt-1">{combo.name}</p>
              </div>

              {/* Price Section */}
              <div className="mb-6">
                <div className="flex items-baseline gap-3 flex-wrap">
                  <span className="text-4xl font-bold text-rose-600">
                    {formatPrice(combo.salePrice)}
                  </span>
                  {discountPercent > 0 && (
                    <span className="inline-block bg-red-100 text-red-700 font-semibold px-3 py-1 rounded-full text-sm">
                      -{discountPercent}%
                    </span>
                  )}
                </div>
                {realtimeOriginal > combo.salePrice && (
                  <p className="mt-1 text-gray-400 line-through text-base">
                    {formatPrice(realtimeOriginal)}
                  </p>
                )}
                {discountPercent > 0 && (
                  <p className="mt-1 text-sm text-green-600 font-medium">
                    Tiết kiệm {formatPrice(realtimeOriginal - combo.salePrice)}
                  </p>
                )}
              </div>

              {/* Validity & Days Left */}
              <div className="mb-6">
                <p className="text-sm text-gray-600 mb-1">{validityText}</p>
                {daysLeftText && (
                  <p className="text-sm font-medium text-orange-600">{daysLeftText}</p>
                )}
              </div>

              {/* Description */}
              {combo.description && (
                <div className="mb-6">
                  <div
                    className={`text-gray-700 text-sm leading-relaxed whitespace-pre-line overflow-hidden transition-all duration-300 ${
                      descExpanded ? '' : 'line-clamp-4'
                    }`}
                  >
                    {combo.description}
                  </div>
                  {combo.description.length > 200 && (
                    <button
                      onClick={() => setDescExpanded(v => !v)}
                      className="mt-1 text-xs text-orange-500 hover:text-orange-600 font-medium"
                    >
                      {descExpanded ? 'Thu gọn' : 'Xem thêm'}
                    </button>
                  )}
                </div>
              )}

              {/* Add to Cart Button */}
              <button
                onClick={handleAddToCart}
                disabled={adding || !isActive}
                className="w-full bg-orange-500 hover:bg-orange-600 disabled:bg-gray-300 disabled:text-gray-500 text-white font-semibold py-3 rounded-lg transition-colors"
              >
                {adding ? 'Đang thêm...' : !isActive ? 'Combo không hoạt động' : 'Thêm combo vào giỏ hàng'}
              </button>
            </div>
          </div>
        </div>

        {/* Items in Combo */}
        {combo.items && combo.items.length > 0 && (
          <div className="mt-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Các món trong combo</h2>
            <div className="space-y-3">
              {combo.items.map((item) => (
                <div
                  key={item.id}
                  className="bg-white rounded-xl border border-gray-200 hover:border-orange-200 px-4 py-3 flex items-center justify-between transition-colors"
                >
                  <div className="flex-1">
                    <p className="text-gray-900 font-medium">{item.productName}</p>
                    {item.sizeLabel && (
                      <p className="text-xs text-gray-500 mt-0.5">Size: {item.sizeLabel}</p>
                    )}
                  </div>
                  <div className="flex items-center gap-4 ml-4 shrink-0">
                    <span className="text-sm text-gray-500">x{item.quantity}</span>
                    <div className="text-right min-w-[110px]">
                      <p className="text-sm font-medium text-gray-900">
                        {formatPrice(item.currentUnitPrice)}
                      </p>
                      {item.currentUnitPrice !== item.unitPriceSnapshot && (
                        <p className="text-xs text-gray-400 line-through">
                          {formatPrice(item.unitPriceSnapshot)}
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}
