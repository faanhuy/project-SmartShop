import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { FiEdit2, FiTrash2, FiPlus, FiX, FiCheck } from 'react-icons/fi';
import AdminLayout from '../../components/AdminLayout';
import { promotionService } from '../../services/promotionService';
import { productService } from '../../services/productService';
import { sizeService } from '../../services/sizeService';
import type { PriceCampaignSummaryDto, PriceCampaignDto, CreatePriceCampaignRequest } from '../../types/promotion';
import type { ProductDto } from '../../types/product';
import type { ProductSize } from '../../types/size';

const RULE_TYPES = [
  { value: 1, label: 'Giảm theo hệ số (Coefficient %)' },
  { value: 2, label: 'Giá cố định (Fixed Price)' },
];

interface CampaignItemForm {
  productId: string;
  sizeId: string | null;
  ruleType: number;
  discountValue: string;
}

const defaultForm = (): {
  name: string;
  startsAt: string;
  endsAt: string;
  appliesToAll: boolean;
  storeIds: string[];
  items: CampaignItemForm[];
} => ({
  name: '',
  startsAt: '',
  endsAt: '',
  appliesToAll: true,
  storeIds: [],
  items: [],
});

export default function AdminPromotionalPricePage() {
  const [campaigns, setCampaigns] = useState<PriceCampaignSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState(defaultForm());

  const [products, setProducts] = useState<ProductDto[]>([]);
  const [itemSizes, setItemSizes] = useState<Record<number, ProductSize[]>>({});
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const loadCampaigns = async () => {
    setLoading(true);
    try {
      const result = await promotionService.getPriceCampaigns();
      setCampaigns(result.items);
    } catch {
      toast.error('Không tải được danh sách campaign.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCampaigns();
    productService.getProducts({ pageSize: 200 }).then((r) => setProducts(r?.items ?? [])).catch(() => {});
  }, []);

  const openCreate = () => {
    setEditingId(null);
    setForm(defaultForm());
    setItemSizes({});
    setShowForm(true);
  };

  const openEdit = async (id: string) => {
    try {
      const dto: PriceCampaignDto = await promotionService.getPriceCampaignById(id);
      setEditingId(id);
      setForm({
        name: dto.name,
        startsAt: dto.startsAt.slice(0, 16),
        endsAt: dto.endsAt.slice(0, 16),
        appliesToAll: dto.appliesToAll,
        storeIds: dto.stores.map((s) => s.id),
        items: dto.items.map((it) => ({
          productId: it.productId,
          sizeId: it.sizeId,
          ruleType: it.ruleType,
          discountValue: String(it.discountValue),
        })),
      });
      setItemSizes({});
      setShowForm(true);
    } catch {
      toast.error('Không tải được campaign.');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Xóa campaign này?')) return;
    setDeletingId(id);
    try {
      await promotionService.deletePriceCampaign(id);
      setCampaigns((prev) => prev.filter((c) => c.id !== id));
      toast.success('Đã xóa campaign.');
    } catch {
      toast.error('Xóa thất bại.');
    } finally {
      setDeletingId(null);
    }
  };

  const addItem = () => {
    setForm((prev) => ({
      ...prev,
      items: [...prev.items, { productId: products[0]?.id ?? '', sizeId: null, ruleType: 1, discountValue: '' }],
    }));
  };

  const removeItem = (idx: number) => {
    setForm((prev) => ({ ...prev, items: prev.items.filter((_, i) => i !== idx) }));
    setItemSizes((prev) => {
      const next = { ...prev };
      delete next[idx];
      return next;
    });
  };

  const updateItem = (idx: number, patch: Partial<CampaignItemForm>) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.map((it, i) => i === idx ? { ...it, ...patch } : it),
    }));
  };

  const handleProductChange = async (idx: number, productId: string) => {
    updateItem(idx, { productId, sizeId: null });
    const p = products.find((pr) => pr.id === productId);
    if (p?.hasSizes) {
      try {
        const sizes = await sizeService.getProductSizes(productId);
        setItemSizes((prev) => ({ ...prev, [idx]: sizes.filter((s) => s.isActive) }));
      } catch {
        setItemSizes((prev) => ({ ...prev, [idx]: [] }));
      }
    } else {
      setItemSizes((prev) => ({ ...prev, [idx]: [] }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim() || !form.startsAt || !form.endsAt) {
      toast.error('Vui lòng điền đầy đủ tên, ngày bắt đầu và ngày kết thúc.');
      return;
    }
    const body: CreatePriceCampaignRequest = {
      name: form.name.trim(),
      startsAt: new Date(form.startsAt).toISOString(),
      endsAt: new Date(form.endsAt).toISOString(),
      appliesToAll: form.appliesToAll,
      storeIds: form.appliesToAll ? [] : form.storeIds,
      items: form.items.map((it) => ({
        productId: it.productId,
        sizeId: it.sizeId || null,
        ruleType: it.ruleType,
        discountValue: parseFloat(it.discountValue) || 0,
      })),
    };
    setSaving(true);
    try {
      if (editingId) {
        await promotionService.updatePriceCampaign(editingId, body);
        toast.success('Đã cập nhật campaign.');
      } else {
        await promotionService.createPriceCampaign(body);
        toast.success('Đã tạo campaign mới.');
      }
      setShowForm(false);
      await loadCampaigns();
    } catch (e: any) {
      toast.error(e.response?.data?.message ?? 'Lưu thất bại.');
    } finally {
      setSaving(false);
    }
  };

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });

  return (
    <AdminLayout title="Quản lý giá khuyến mãi">
      <div className="flex items-center justify-between mb-6">
        <p className="text-sm text-gray-500">Quản lý các chiến dịch giá (Price Campaign) theo sản phẩm và size.</p>
        <button
          onClick={openCreate}
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-white bg-rose-600 hover:bg-rose-700 rounded-lg transition-colors"
        >
          <FiPlus size={14} />
          Tạo campaign
        </button>
      </div>

      {/* Campaign list */}
      <div className="bg-white rounded-xl border shadow-sm overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-400 text-sm">Đang tải...</div>
        ) : campaigns.length === 0 ? (
          <div className="p-8 text-center text-gray-400 text-sm">Chưa có campaign nào.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left px-5 py-3 font-semibold text-gray-600">Tên</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Thời gian</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Phạm vi</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Số item</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Trạng thái</th>
                <th className="text-right px-5 py-3 font-semibold text-gray-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {campaigns.map((c) => (
                <tr key={c.id} className="hover:bg-gray-50">
                  <td className="px-5 py-3 font-medium text-gray-800">{c.name}</td>
                  <td className="px-5 py-3 text-center text-gray-600 text-xs">
                    {formatDate(c.startsAt)} — {formatDate(c.endsAt)}
                  </td>
                  <td className="px-5 py-3 text-center">
                    {c.appliesToAll ? (
                      <span className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full">Tất cả</span>
                    ) : (
                      <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full">{c.storeCount} chi nhánh</span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-center text-gray-600">{c.itemCount}</td>
                  <td className="px-5 py-3 text-center">
                    {c.isActive ? (
                      <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium">Hoạt động</span>
                    ) : (
                      <span className="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full">Không hoạt động</span>
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
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Form modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-start justify-center p-4 bg-black/50 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl my-8">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h3 className="text-base font-semibold text-gray-800">
                {editingId ? 'Sửa campaign' : 'Tạo campaign mới'}
              </h3>
              <button onClick={() => setShowForm(false)} className="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100">
                <FiX size={16} />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên campaign <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                  placeholder="VD: Khuyến mãi hè 2026"
                />
              </div>

              {/* Dates */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Bắt đầu <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="datetime-local"
                    value={form.startsAt}
                    onChange={(e) => setForm((p) => ({ ...p, startsAt: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Kết thúc <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="datetime-local"
                    value={form.endsAt}
                    onChange={(e) => setForm((p) => ({ ...p, endsAt: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                  />
                </div>
              </div>

              {/* Scope */}
              <div>
                <label className="flex items-center gap-2 text-sm font-medium text-gray-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.appliesToAll}
                    onChange={(e) => setForm((p) => ({ ...p, appliesToAll: e.target.checked }))}
                    className="accent-rose-600"
                  />
                  Áp dụng cho tất cả chi nhánh
                </label>
              </div>

              {/* Items */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-sm font-medium text-gray-700">Sản phẩm áp dụng</label>
                  <button
                    type="button"
                    onClick={addItem}
                    className="flex items-center gap-1 text-xs text-rose-600 hover:text-rose-700 font-medium"
                  >
                    <FiPlus size={12} />
                    Thêm dòng
                  </button>
                </div>
                {form.items.length === 0 ? (
                  <p className="text-sm text-gray-400 py-2">Chưa có item nào. Nhấn "Thêm dòng" để thêm.</p>
                ) : (
                  <div className="space-y-2">
                    {form.items.map((it, idx) => {
                      const sizes = itemSizes[idx] ?? [];
                      const selectedProduct = products.find((p) => p.id === it.productId);
                      return (
                        <div key={idx} className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                          <select
                            value={it.productId}
                            onChange={(e) => handleProductChange(idx, e.target.value)}
                            className="flex-1 border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                          >
                            {products.map((p) => (
                              <option key={p.id} value={p.id}>{p.name}</option>
                            ))}
                          </select>
                          {(selectedProduct?.hasSizes || sizes.length > 0) && (
                            <select
                              value={it.sizeId ?? ''}
                              onChange={(e) => updateItem(idx, { sizeId: e.target.value || null })}
                              className="w-24 border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                            >
                              <option value="">Tất cả size</option>
                              {sizes.map((s) => (
                                <option key={s.id} value={s.id}>{s.sizeLabel}</option>
                              ))}
                            </select>
                          )}
                          <select
                            value={it.ruleType}
                            onChange={(e) => updateItem(idx, { ruleType: Number(e.target.value) })}
                            className="w-36 border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400 bg-white"
                          >
                            {RULE_TYPES.map((r) => (
                              <option key={r.value} value={r.value}>{r.label}</option>
                            ))}
                          </select>
                          <input
                            type="number"
                            step="0.01"
                            min="0"
                            value={it.discountValue}
                            onChange={(e) => updateItem(idx, { discountValue: e.target.value })}
                            className="w-24 border border-gray-300 rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-rose-400"
                            placeholder={it.ruleType === 1 ? 'Hệ số (vd: 0.8)' : 'Giá (vd: 50000)'}
                          />
                          <button
                            type="button"
                            onClick={() => removeItem(idx)}
                            className="p-1 rounded text-gray-400 hover:text-red-500"
                          >
                            <FiX size={14} />
                          </button>
                        </div>
                      );
                    })}
                  </div>
                )}
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
