import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { FiEdit2, FiPlus, FiX } from 'react-icons/fi';
import AdminLayout from '../../components/AdminLayout';
import { storeService } from '../../services/storeService';
import type { Store, CreateStoreRequest, UpdateStoreRequest } from '../../types/store';

// ─── Modal form state ─────────────────────────────────────────────────────────

interface FormState {
  name: string;
  address: string;
  phone: string;
  isActive: boolean;
}

interface FormErrors {
  name?: string;
  address?: string;
  phone?: string;
}

const EMPTY_FORM: FormState = { name: '', address: '', phone: '', isActive: true };

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function AdminStoresPage() {
  const [stores, setStores] = useState<Store[]>([]);
  const [loading, setLoading] = useState(true);

  // Modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [editingStore, setEditingStore] = useState<Store | null>(null);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);

  const loadStores = () => {
    setLoading(true);
    storeService
      .getStores()
      .then(setStores)
      .catch(() => toast.error('Không tải được danh sách chi nhánh.'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadStores();
  }, []);

  const openCreateModal = () => {
    setEditingStore(null);
    setForm(EMPTY_FORM);
    setErrors({});
    setModalOpen(true);
  };

  const openEditModal = (store: Store) => {
    setEditingStore(store);
    setForm({
      name: store.name,
      address: store.address,
      phone: store.phone,
      isActive: store.isActive ?? true,
    });
    setErrors({});
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditingStore(null);
    setForm(EMPTY_FORM);
    setErrors({});
  };

  const validate = (): boolean => {
    const next: FormErrors = {};
    if (!form.name.trim()) next.name = 'Tên chi nhánh không được để trống.';
    if (!form.address.trim()) next.address = 'Địa chỉ không được để trống.';
    if (!form.phone.trim()) next.phone = 'Số điện thoại không được để trống.';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;
    setSubmitting(true);
    try {
      if (editingStore) {
        const body: UpdateStoreRequest = {
          name: form.name.trim(),
          address: form.address.trim(),
          phone: form.phone.trim(),
          isActive: form.isActive,
        };
        await storeService.updateStore(editingStore.id, body);
        toast.success('Đã cập nhật chi nhánh.');
      } else {
        const body: CreateStoreRequest = {
          name: form.name.trim(),
          address: form.address.trim(),
          phone: form.phone.trim(),
        };
        await storeService.createStore(body);
        toast.success('Đã thêm chi nhánh mới.');
      }
      closeModal();
      loadStores();
    } catch (error: any) {
      toast.error(error.response?.data?.message ?? 'Có lỗi xảy ra');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AdminLayout title="Quản lý chi nhánh">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-semibold text-gray-800">Danh sách chi nhánh</h2>
        <button
          onClick={openCreateModal}
          className="flex items-center gap-2 bg-rose-600 hover:bg-rose-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
        >
          <FiPlus size={15} />
          Thêm chi nhánh
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border shadow-sm overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-400 text-sm">Đang tải danh sách chi nhánh...</div>
        ) : stores.length === 0 ? (
          <div className="p-8 text-center text-gray-400 text-sm">Chưa có chi nhánh nào.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left px-5 py-3 font-semibold text-gray-600">Tên chi nhánh</th>
                <th className="text-left px-5 py-3 font-semibold text-gray-600">Địa chỉ</th>
                <th className="text-left px-5 py-3 font-semibold text-gray-600">Số điện thoại</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Trạng thái</th>
                <th className="text-right px-5 py-3 font-semibold text-gray-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {stores.map((store) => (
                <tr key={store.id} className="hover:bg-gray-50">
                  <td className="px-5 py-3 font-medium text-gray-800">{store.name}</td>
                  <td className="px-5 py-3 text-gray-600">{store.address}</td>
                  <td className="px-5 py-3 text-gray-600">{store.phone}</td>
                  <td className="px-5 py-3 text-center">
                    {store.isActive !== false ? (
                      <span className="inline-block bg-green-100 text-green-700 text-xs px-2.5 py-0.5 rounded-full font-medium">
                        Hoạt động
                      </span>
                    ) : (
                      <span className="inline-block bg-gray-100 text-gray-500 text-xs px-2.5 py-0.5 rounded-full font-medium">
                        Tạm ngừng
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button
                      onClick={() => openEditModal(store)}
                      className="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-rose-600 transition-colors"
                      title="Chỉnh sửa chi nhánh"
                    >
                      <FiEdit2 size={14} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
            {/* Modal header */}
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h3 className="text-base font-semibold text-gray-800">
                {editingStore ? 'Chỉnh sửa chi nhánh' : 'Thêm chi nhánh mới'}
              </h3>
              <button
                onClick={closeModal}
                disabled={submitting}
                className="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors disabled:opacity-50"
              >
                <FiX size={16} />
              </button>
            </div>

            {/* Modal body */}
            <div className="px-6 py-5 space-y-4">
              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên chi nhánh <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  placeholder="Ví dụ: Chi nhánh Quận 1"
                  className={`w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400 ${
                    errors.name ? 'border-red-400' : 'border-gray-300'
                  }`}
                />
                {errors.name && (
                  <p className="mt-1 text-xs text-red-500">{errors.name}</p>
                )}
              </div>

              {/* Address */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Địa chỉ <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.address}
                  onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
                  placeholder="Ví dụ: 123 Lê Lợi, Quận 1, TP.HCM"
                  className={`w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400 ${
                    errors.address ? 'border-red-400' : 'border-gray-300'
                  }`}
                />
                {errors.address && (
                  <p className="mt-1 text-xs text-red-500">{errors.address}</p>
                )}
              </div>

              {/* Phone */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Số điện thoại <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.phone}
                  onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))}
                  placeholder="Ví dụ: 028 3823 4567"
                  className={`w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400 ${
                    errors.phone ? 'border-red-400' : 'border-gray-300'
                  }`}
                />
                {errors.phone && (
                  <p className="mt-1 text-xs text-red-500">{errors.phone}</p>
                )}
              </div>

              {/* isActive toggle — edit mode only */}
              {editingStore && (
                <div className="flex items-center gap-3 pt-1">
                  <input
                    id="isActive"
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                    className="h-4 w-4 rounded border-gray-300 text-rose-600 focus:ring-rose-400"
                  />
                  <label htmlFor="isActive" className="text-sm font-medium text-gray-700 cursor-pointer">
                    Đang hoạt động
                  </label>
                </div>
              )}
            </div>

            {/* Modal footer */}
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t">
              <button
                onClick={closeModal}
                disabled={submitting}
                className="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                onClick={handleSubmit}
                disabled={submitting}
                className="px-4 py-2 text-sm font-medium text-white bg-rose-600 hover:bg-rose-700 rounded-lg transition-colors disabled:opacity-60 flex items-center gap-2"
              >
                {submitting && (
                  <span className="inline-block h-3.5 w-3.5 rounded-full border-2 border-white border-t-transparent animate-spin" />
                )}
                Lưu
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
