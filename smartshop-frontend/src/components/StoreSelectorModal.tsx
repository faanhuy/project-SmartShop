import { useEffect, useState } from 'react';
import { FiMapPin, FiPhone, FiCheck, FiX } from 'react-icons/fi';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';
import type { Store } from '../types/store';

interface StoreSelectorModalProps {
  isOpen: boolean;
  onClose: () => void;
  required?: boolean;
}

export default function StoreSelectorModal({ isOpen, onClose, required = false }: StoreSelectorModalProps) {
  const { stores, selectedStore, setSelectedStore, fetchStores } = useStoreSelectionStore();
  const [pending, setPending] = useState<Store | null>(selectedStore);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen && stores.length === 0) {
      setLoading(true);
      fetchStores().finally(() => setLoading(false));
    }
  }, [isOpen, stores.length, fetchStores]);

  useEffect(() => {
    if (isOpen) {
      setPending(selectedStore);
    }
  }, [isOpen, selectedStore]);

  const handleConfirm = () => {
    if (!pending) return;
    setSelectedStore(pending);
    onClose();
  };

  const handleClose = () => {
    if (required && !selectedStore) return;
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/50"
        onClick={handleClose}
      />

      {/* Modal */}
      <div id="store-selector-modal" className="relative bg-white rounded-2xl shadow-xl w-full max-w-md mx-4 overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-base font-semibold text-gray-800">Chọn chi nhánh</h2>
          <button
            onClick={handleClose}
            disabled={required && !selectedStore}
            className="p-1.5 rounded-lg hover:bg-gray-100 text-gray-500 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            title={required && !selectedStore ? 'Vui lòng chọn chi nhánh trước' : 'Đóng'}
          >
            <FiX size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-4 max-h-96 overflow-y-auto">
          {loading ? (
            <div className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-20 bg-gray-100 rounded-xl animate-pulse" />
              ))}
            </div>
          ) : stores.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-8">Không có chi nhánh nào.</p>
          ) : (
            <div className="space-y-2">
              {stores.map((store) => {
                const isSelected = pending?.id === store.id;
                return (
                  <button
                    key={store.id}
                    type="button"
                    onClick={() => setPending(store)}
                    className={`w-full text-left p-4 rounded-xl border-2 transition-colors ${
                      isSelected
                        ? 'border-rose-400 bg-rose-50'
                        : 'border-gray-200 hover:border-gray-300'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-gray-800">{store.name}</p>
                        <p className="text-xs text-gray-500 mt-1 flex items-center gap-1">
                          <FiMapPin size={11} className="shrink-0" />
                          <span className="truncate">{store.address}</span>
                        </p>
                        <p className="text-xs text-gray-500 mt-0.5 flex items-center gap-1">
                          <FiPhone size={11} className="shrink-0" />
                          {store.phone}
                        </p>
                      </div>
                      {isSelected && (
                        <div className="shrink-0 w-5 h-5 rounded-full bg-rose-500 flex items-center justify-center mt-0.5">
                          <FiCheck size={11} className="text-white" />
                        </div>
                      )}
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t bg-gray-50">
          {required && !selectedStore && (
            <p className="text-xs text-amber-600 mb-3">
              Vui lòng chọn chi nhánh để tiếp tục đặt hàng.
            </p>
          )}
          <button
            onClick={handleConfirm}
            disabled={!pending}
            className="w-full bg-rose-600 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-rose-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Chọn chi nhánh này
          </button>
        </div>
      </div>
    </div>
  );
}
