import { useState, useRef } from 'react';
import { FiSearch, FiX, FiCpu } from 'react-icons/fi';
import { Link } from 'react-router-dom';
import { aiService } from '../services/aiService';
import type { SemanticSearchResultDto } from '../types/ai';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

interface AISearchBarProps {
  onClose?: () => void;
}

export default function AISearchBar({ onClose }: AISearchBarProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SemanticSearchResultDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleInput = (value: string) => {
    setQuery(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (!value.trim() || value.trim().length < 2) {
      setResults([]);
      setSearched(false);
      setErrorMsg(null);
      return;
    }
    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      setErrorMsg(null);
      try {
        const res = await aiService.semanticSearch({ query: value.trim(), topN: 8 });
        setResults(res);
        setSearched(true);
      } catch (err) {
        setResults([]);
        setSearched(true);
        setErrorMsg(aiService.extractErrorMessage(err, 'Tìm kiếm AI thất bại. Vui lòng thử lại sau.'));
      } finally {
        setLoading(false);
      }
    }, 600);
  };

  const handleClear = () => {
    setQuery('');
    setResults([]);
    setSearched(false);
    setErrorMsg(null);
  };

  return (
    <div className="relative w-full max-w-xl">
      <div className="flex items-center border border-blue-300 rounded-xl bg-white shadow-sm px-3 py-2 gap-2">
        <FiCpu className="text-blue-500 shrink-0" size={16} />
        <input
          className="flex-1 text-sm outline-none text-gray-800 placeholder-gray-400"
          placeholder="Tìm kiếm thông minh bằng AI..."
          value={query}
          onChange={(e) => handleInput(e.target.value)}
        />
        {loading && (
          <span className="text-xs text-blue-400 animate-pulse">Đang tìm...</span>
        )}
        {query && !loading && (
          <button onClick={handleClear} className="text-gray-400 hover:text-gray-600">
            <FiX size={14} />
          </button>
        )}
        <FiSearch className="text-gray-400 shrink-0" size={16} />
      </div>

      {searched && (
        <div className="absolute z-50 top-full mt-1 w-full bg-white rounded-xl shadow-lg border border-gray-100 overflow-hidden">
          {errorMsg ? (
            <p className="px-4 py-3 text-sm text-red-500">{errorMsg}</p>
          ) : results.length === 0 ? (
            <p className="px-4 py-3 text-sm text-gray-500">Không tìm thấy kết quả phù hợp.</p>
          ) : (
            <ul>
              {results.map((item) => (
                <li key={item.id}>
                  <Link
                    to={`/products/${item.slug}`}
                    onClick={onClose}
                    className="flex items-center gap-3 px-4 py-3 hover:bg-blue-50 transition-colors"
                  >
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-800 truncate">{item.name}</p>
                      <p className="text-xs text-gray-500 truncate">{item.description}</p>
                    </div>
                    <div className="text-right shrink-0">
                      <p className="text-sm font-semibold text-blue-600">{formatPrice(item.price)}</p>
                      <p className="text-xs text-gray-400">{Math.round(item.score * 100)}% phù hợp</p>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
