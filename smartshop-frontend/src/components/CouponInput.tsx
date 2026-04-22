import { useState } from 'react';
import { FiX, FiTag } from 'react-icons/fi';
import { couponService, type ValidateCouponResult } from '../services/couponService';
import { getApiError } from '../utils/errorHandler';

interface Props {
  orderTotal: number;
  onApplied: (result: ValidateCouponResult, code: string) => void;
  onClear: () => void;
  appliedCode?: string;
}

export default function CouponInput({ orderTotal, onApplied, onClear, appliedCode }: Props) {
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleApply = async () => {
    const trimmed = code.trim();
    if (!trimmed) return;
    setError('');
    setLoading(true);
    try {
      const result = await couponService.validate(trimmed, orderTotal);
      onApplied(result, trimmed.toUpperCase());
      setCode('');
    } catch (err) {
      setError(getApiError(err, 'Mã voucher không hợp lệ hoặc đã hết hạn.'));
    } finally {
      setLoading(false);
    }
  };

  const handleClear = () => {
    setCode('');
    setError('');
    onClear();
  };

  if (appliedCode) {
    return (
      <div className="flex items-center justify-between bg-green-50 border border-green-200 rounded-full px-4 py-2 mb-3">
        <div className="flex items-center gap-2 text-green-700 font-semibold">
          <FiTag size={16} />
          <span>{appliedCode}</span>
        </div>
        <button
          onClick={handleClear}
          className="text-green-600 hover:text-red-500 transition-colors"
          title="Xoá mã giảm giá"
        >
          <FiX size={18} />
        </button>
      </div>
    );
  }

  return (
    <div className="mb-3">
      <div className="flex gap-2">
        <input
          type="text"
          className="flex-1 border rounded-full px-4 py-2 text-base focus:outline-none focus:ring-2 focus:ring-orange-400"
          placeholder="Nhập mã giảm giá"
          value={code}
          onChange={e => { setCode(e.target.value); setError(''); }}
          onKeyDown={e => { if (e.key === 'Enter') handleApply(); }}
          disabled={loading}
        />
        <button
          className="bg-orange-500 text-white px-8 py-2 rounded-full font-semibold text-base hover:bg-orange-600 disabled:opacity-50 transition-colors"
          onClick={handleApply}
          disabled={loading || !code.trim()}
        >
          {loading ? 'Đang kiểm tra...' : 'Áp dụng'}
        </button>
      </div>
      {error && (
        <div className="flex items-start gap-2 text-red-500 text-base mt-2">
          <FiX className="flex-shrink-0 mt-0.5" size={20} />
          <span>{error}</span>
        </div>
      )}
    </div>
  );
}
