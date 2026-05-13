import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { FiEdit2, FiTrash2, FiPlus, FiX, FiCheck, FiToggleLeft, FiToggleRight } from 'react-icons/fi';
import AdminLayout from '../../components/AdminLayout';
import { promotionService } from '../../services/promotionService';
import { productService } from '../../services/productService';
import { sizeService } from '../../services/sizeService';
import type { ComboPromotion, CreateComboRequest } from '../../types/promotion';
import type { ProductDto } from '../../types/product';
import type { ProductSize } from '../../types/size';

const REWARD_TYPES = [
  { value: 0, label: 'Tặng sản phẩm miễn phí' },
  { value: 1, label: 'Giảm số tiền cố định' },
];

const defaultForm = (): {
  name: string;
  triggerProductId: string;
  triggerSizeId: string;
  triggerMinQuantity: string;
  rewardType: number;
  rewardProductId: string;
  rewardSizeId: string;
  rewardQuantity: string;
  rewardAmount: string;
  storeId: string;
  startsAt: string;
  endsAt: string;
} => ({
  name: '',
  triggerProductId: '',
  triggerSizeId: '',
  triggerMinQuantity: '1',
  rewardType: 0,
  rewardProductId: '',
  rewardSizeId: '',
  rewardQuantity: '1',
  rewardAmount: '',
  storeId: '',
  startsAt: '',
  endsAt: '',
});

export default function AdminComboPage() {
  const [combos, setCombos] = useState<ComboPromotion[]>([]);
  const [loading, setLoading] = useState(true);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState(defaultForm());

  const [products, setProducts] = useState<ProductDto[]>([]);
  const [triggerSizes, setTriggerSizes] = useState<ProductSize[]>([]);
  const [rewardSizes, setRewardSizes] = useState<ProductSize[]>([]);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const loadCombos = async () => {
    setLoading(true);
    try {
      const result = await promotionService.getCombos();
      setCombos(result.items);
    } catch {
      toast.error('Không tải được danh sách combo.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCombos();
    productService.getProducts({ pageSize: 200 }).then((r) => setProducts(r?.items ?? [])).catch(() => {});
  }, []);

  const loadTriggerSizes = async (productId: string) => {
    if (!productId) { setTriggerSizes([]); return; }
    const p = products.find((pr) => pr.id === productId);
    if (!p?.hasSizes) { setTriggerSizes([]); return; }
    try {
      const sizes = await sizeService.getProductSizes(productId);
      setTriggerSizes(sizes.filter((s) => s.isActive));
    } catch {
      setTriggerSizes([]);
    }
  };

  const loadRewardSizes = async (productId: string) => {
    if (!productId) { setRewardSizes([]); return; }
    const p = products.find((pr) => pr.id === productId);
    if (!p?.hasSizes) { setRewardSizes([]); return; }
    try {
      const sizes = await sizeService.getProductSizes(productId);
      setRewardSizes(sizes.filter((s) => s.isActive));
    } catch {
      setRewardSizes([]);
    }
  };

  const openCreate = () => {
    setEditingId(null);
    const f = defaultForm();
    if (products.length > 0) {
      f.triggerProductId = products[0].id;
      f.rewardProductId = products[0].id;
    }
    setForm(f);
    setTriggerSizes([]);
    setRewardSizes([]);
    setShowForm(true);
  };

  const openEdit = async (id: string) => {
    try {
      const combo = await promotionService.getComboById(id);
      setEditingId(id);
      setForm({
        name: combo.name,
        triggerProductId: combo.triggerProductId,
        triggerSizeId: combo.triggerSizeId ?? '',
        triggerMinQuantity: String(combo.triggerMinQuantity),
        rewardType: combo.rewardType,
        rewardProductId: combo.rewardProductId ?? '',
        rewardSizeId: combo.rewardSizeId ?? '',
        rewardQuantity: String(combo.rewardQuantity ?? 1),
        rewardAmount: String(combo.rewardAmount ?? ''),
        storeId: combo.storeId ?? '',
        startsAt: combo.startsAt ? combo.startsAt.slice(0, 16) : '',
        endsAt: combo.endsAt ? combo.endsAt.slice(0, 16) : '',
      });
      await Promise.all([
        loadTriggerSizes(combo.triggerProductId),
        loadRewardSizes(combo.rewardProductId ?? ''),
      ]);
      setShowForm(true);
    } catch {
      toast.error('Không tải được combo.');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Xóa combo này?')) return;
    setDeletingId(id);
    try {
      await promotionService.deleteCombo(id);
      setCombos((prev) => prev.filter((c) => c.id !== id));
      toast.success('Đã xóa combo.');
    } catch {
      toast.error('Xóa thất bại.');
    } finally {
      setDeletingId(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) { toast.error('Vui lòng nhập tên combo.'); return; }
    if (!form.triggerProductId) { toast.error('Vui lòng chọn sản phẩm trigger.'); return; }
    const body: CreateComboRequest = {
      name: form.name.trim(),
      triggerProductId: form.triggerProductId,
      triggerSizeId: form.triggerSizeId || null,
      triggerMinQuantity: parseInt(form.triggerMinQuantity) || 1,
      rewardType: form.rewardType,
      rewardProductId: form.rewardType === 0 ? (form.rewardProductId || null) : null,
      rewardSizeId: form.rewardType === 0 ? (form.rewardSizeId || null) : null,
      rewardQuantity: form.rewardType === 0 ? (parseInt(form.rewardQuantity) || 1) : null,
      rewardAmount: form.rewardType === 1 ? (parseFloat(form.rewardAmount) || 0) : null,
      storeId: form.storeId || null,
      startsAt: form.startsAt ? new Date(form.startsAt).toISOString() : null,
      endsAt: form.endsAt ? new Date(form.endsAt).toISOString() : null,
    };
    setSaving(true);
    try {
      if (editingId) {
        await promotionService.updateCombo(editingId, body);
        toast.success('Đã cập nhật combo.');
      } else {
        await promotionService.createCombo(body);
        toast.success('Đã tạo combo mới.');
      }
      setShowForm(false);
      await loadCombos();
    } catch (e: any) {
      toast.error(e.response?.data?.message ?? 'Lưu thất bại.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <AdminLayout title="Quản lý Combo Khuyến mãi">
      <div className="flex items-center justify-between mb-6">
        <p className="text-sm text-gray-500">Tạo combo tặng quà hoặc giảm giá khi mua đủ số lượng.</p>
        <button
          onClick={openCreate}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-white bg-rose-600 hover:bg-rose-700 rounded-lg transition-colors"
        >
          <FiPlus size={14} />
          Tạo combo
        </button>
      </div>

      {/* Combo list */}
      <div className="bg-white rounded-xl border shadow-sm overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-400 text-sm">Đang tải...</div>
        ) : combos.length === 0 ? (
          <div className="p-8 text-center text-gray-400 text-sm">Chưa có combo nào.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left px-5 py-3 font-semibold text-gray-600">Tên combo</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Trigger (SP / Qty)</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Phần thưởng</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Trạng thái</th>
                <th className="text-right px-5 py-3 font-semibold text-gray-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {combos.map((c) => {
                const triggerProduct = products.find((p) => p.id === c.triggerProductId);
                const rewardProduct = products.find((p) => p.id === c.rewardProductId);
                return (
                  <tr key={c.id} className="hover:bg-gray-50">
                    <td className="px-5 py-3 font-medium text-gray-800">{c.name}</td>
                    <td className="px-5 py-3 text-center text-gray-600 text-xs">
                      {triggerProduct?.name ?? c.triggerProductId} x{c.triggerMinQuantity}
                    </td>
                    <td className="px-5 py-3 text-center text-xs">
                      {c.rewardType === 0 ? (
                        <span className="text-green-700">
                          Tặng: {rewardProduct?.name ?? c.rewardProductId} x{c.rewardQuantity}
                        </span>
                      ) : (
                        <span className="text-rose-700">
                          Giảm: {c.rewardAmount?.toLocaleString('vi-VN')} đ
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-3 text-center">
                      {c.isActive ? (
                        <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium flex items-center gap-1 justify-center w-fit mx-auto">
                          <FiToggleRight size={12} />
                          Bật
                        </span>
                      ) : (
                        <span className="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full flex items-center gap-1 justify-center w-fit mx-auto">
                          <FiToggleLeft size={12} />
                          Tắt
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => openEdit(c.id)}
                          className="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-rose-600 transition-colors"
                          title="Sửa"
                        >
                          <FiEdit2 size={14} />
                        </button>
                        <button
                          onClick={() => handleDelete(c.id)}
                          disabled={deletingId === c.id}
                          className="p-1.5 rounded-lg text-gray-500 hover:bg-red-50 hover:text-red-600 transition-colors disabled:opacity-50"
                          title="Xóa"
                        >
                          <FiTrash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* Form modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-start justify-center p-4 bg-black/50 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-xl my-8">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h3 className="text-base font-semibold text-gray-800">
                {editingId ? 'Sửa combo' : 'Tạo combo mới'}
              </h3>
              <button onClick={() => setShowForm(false)} className="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100">
                <FiX size={16} />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên combo <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                  placeholder="VD: Mua 2 tặng 1"
                />
              </div>

              {/* Trigger */}
              <div className="p-4 bg-amber-50 rounded-xl space-y-3">
                <p className="text-xs font-semibold uppercase tracking-wide text-amber-700">Điều kiện kích hoạt</p>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Sản phẩm</label>
                    <select
                      value={form.triggerProductId}
                      onChange={(e) => {
                        setForm((p) => ({ ...p, triggerProductId: e.target.value, triggerSizeId: '' }));
                        loadTriggerSizes(e.target.value);
                      }}
                      className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                    >
                      <option value="">-- Chọn sản phẩm --</option>
                      {products.map((p) => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Số lượng tối thiểu</label>
                    <input
                      type="number"
                      min="1"
                      value={form.triggerMinQuantity}
                      onChange={(e) => setForm((p) => ({ ...p, triggerMinQuantity: e.target.value }))}
                      className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400"
                    />
                  </div>
                </div>
                {triggerSizes.length > 0 && (
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Size (tùy chọn)</label>
                    <select
                      value={form.triggerSizeId}
                      onChange={(e) => setForm((p) => ({ ...p, triggerSizeId: e.target.value }))}
                      className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                    >
                      <option value="">Tất cả size</option>
                      {triggerSizes.map((s) => (
                        <option key={s.id} value={s.id}>{s.sizeLabel}</option>
                      ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Reward type */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Loại phần thưởng</label>
                <div className="flex gap-4">
                  {REWARD_TYPES.map((rt) => (
                    <label key={rt.value} className="flex items-center gap-2 cursor-pointer text-sm text-gray-700">
                      <input
                        type="radio"
                        name="rewardType"
                        value={rt.value}
                        checked={form.rewardType === rt.value}
                        onChange={() => setForm((p) => ({ ...p, rewardType: rt.value }))}
                        className="accent-rose-600"
                      />
                      {rt.label}
                    </label>
                  ))}
                </div>
              </div>

              {/* Reward fields */}
              {form.rewardType === 0 ? (
                <div className="p-4 bg-green-50 rounded-xl space-y-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-green-700">Sản phẩm tặng</p>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Sản phẩm tặng</label>
                      <select
                        value={form.rewardProductId}
                        onChange={(e) => {
                          setForm((p) => ({ ...p, rewardProductId: e.target.value, rewardSizeId: '' }));
                          loadRewardSizes(e.target.value);
                        }}
                        className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                      >
                        <option value="">-- Chọn sản phẩm --</option>
                        {products.map((p) => (
                          <option key={p.id} value={p.id}>{p.name}</option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Số lượng tặng</label>
                      <input
                        type="number"
                        min="1"
                        value={form.rewardQuantity}
                        onChange={(e) => setForm((p) => ({ ...p, rewardQuantity: e.target.value }))}
                        className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400"
                      />
                    </div>
                  </div>
                  {rewardSizes.length > 0 && (
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Size sản phẩm tặng</label>
                      <select
                        value={form.rewardSizeId}
                        onChange={(e) => setForm((p) => ({ ...p, rewardSizeId: e.target.value }))}
                        className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                      >
                        <option value="">Tất cả size</option>
                        {rewardSizes.map((s) => (
                          <option key={s.id} value={s.id}>{s.sizeLabel}</option>
                        ))}
                      </select>
                    </div>
                  )}
                </div>
              ) : (
                <div className="p-4 bg-rose-50 rounded-xl">
                  <label className="block text-xs font-semibold uppercase tracking-wide text-rose-700 mb-2">Số tiền giảm (VND)</label>
                  <input
                    type="number"
                    min="0"
                    value={form.rewardAmount}
                    onChange={(e) => setForm((p) => ({ ...p, rewardAmount: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                    placeholder="VD: 50000"
                  />
                </div>
              )}

              {/* Optional fields */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">Ngày bắt đầu (tùy chọn)</label>
                  <input
                    type="datetime-local"
                    value={form.startsAt}
                    onChange={(e) => setForm((p) => ({ ...p, startsAt: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">Ngày kết thúc (tùy chọn)</label>
                  <input
                    type="datetime-local"
                    value={form.endsAt}
                    onChange={(e) => setForm((p) => ({ ...p, endsAt: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-2 border-t">
                <button
                  type="button"
                  onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-lg"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="px-4 py-2 text-sm font-semibold text-white bg-rose-600 hover:bg-rose-700 rounded-lg disabled:opacity-50 flex items-center gap-2"
                >
                  {saving ? (
                    <span className="inline-block h-3.5 w-3.5 rounded-full border-2 border-white border-t-transparent animate-spin" />
                  ) : (
                    <FiCheck size={14} />
                  )}
                  {editingId ? 'Cập nhật' : 'Tạo mới'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
