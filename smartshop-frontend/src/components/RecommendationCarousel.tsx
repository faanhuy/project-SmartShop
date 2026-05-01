import { useEffect, useState, useRef } from 'react';
import { Link } from 'react-router-dom';
import { FiChevronLeft, FiChevronRight, FiAlertCircle } from 'react-icons/fi';
import { aiService } from '../services/aiService';
import type { ProductDto } from '../types/product';
import { formatPrice } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';

interface RecommendationCarouselProps {
  productId: string;
}

export default function RecommendationCarousel({ productId }: RecommendationCarouselProps) {
  const [recommendations, setRecommendations] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setLoading(true);
    setErrorMsg(null);
    aiService
      .getRecommendations(productId, 5)
      .then(setRecommendations)
      .catch((err) => {
        setRecommendations([]);
        setErrorMsg(aiService.extractErrorMessage(err, 'Không thể tải gợi ý sản phẩm.'));
      })
      .finally(() => setLoading(false));
  }, [productId]);

  const scroll = (dir: 'left' | 'right') => {
    if (!scrollRef.current) return;
    scrollRef.current.scrollBy({ left: dir === 'left' ? -240 : 240, behavior: 'smooth' });
  };

  if (loading) {
    return (
      <div className="mt-8">
        <h2 className="text-base font-semibold text-gray-800 mb-3">Món gợi ý cùng vị</h2>
        <div className="flex gap-4 overflow-hidden">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="w-48 shrink-0 rounded-xl bg-gray-100 animate-pulse h-56" />
          ))}
        </div>
      </div>
    );
  }

  if (errorMsg) {
    return (
      <div className="mt-8">
        <h2 className="text-base font-semibold text-gray-800 mb-3">Món gợi ý cùng vị</h2>
        <div className="flex items-center gap-2 text-sm text-amber-600 bg-amber-50 border border-amber-200 rounded-lg px-4 py-3">
          <FiAlertCircle size={15} className="shrink-0" />
          <span>{errorMsg}</span>
        </div>
      </div>
    );
  }

  if (recommendations.length === 0) return null;

  return (
    <div className="mt-8">
      <div className="flex items-center justify-between mb-3">
        <h2 className="text-base font-semibold text-gray-800">Món gợi ý cùng vị</h2>
        <div className="flex gap-1">
          <button
            onClick={() => scroll('left')}
            className="p-1.5 rounded-lg border hover:bg-gray-50 text-gray-500"
          >
            <FiChevronLeft size={16} />
          </button>
          <button
            onClick={() => scroll('right')}
            className="p-1.5 rounded-lg border hover:bg-gray-50 text-gray-500"
          >
            <FiChevronRight size={16} />
          </button>
        </div>
      </div>

      <div
        ref={scrollRef}
        className="flex gap-4 overflow-x-auto pb-2"
        style={{ scrollbarWidth: 'none' }}
      >
        {recommendations.map((product) => (
          <Link
            key={product.id}
            to={`/products/${product.slug}`}
            className="w-48 shrink-0 bg-white rounded-xl shadow-sm border border-gray-100 p-3 hover:shadow-md transition-shadow"
          >
            <div className="h-28 bg-gray-100 rounded-lg flex items-center justify-center mb-2 overflow-hidden">
              {product.imageUrl ? (
                <img
                  src={getImageUrl(product.imageUrl)}
                  alt={product.name}
                  className="h-full w-full object-contain"
                />
              ) : (
                <span className="text-4xl">🍔</span>
              )}
            </div>
            <p className="text-xs font-medium text-gray-800 line-clamp-2 leading-snug">
              {product.name}
            </p>
            <p className="mt-1 text-sm font-bold text-blue-600">{formatPrice(product.price)}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
