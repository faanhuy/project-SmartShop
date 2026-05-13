import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { categoryService, productService } from '../services/productService';
import { sizeService } from '../services/sizeService';
import { getApiError } from '../utils/errorHandler';
import { formatPrice } from '../utils/formatters';
import { slugify } from '../utils/slugify';
import type { CategoryDto, CreateProductRequest, ProductDto, UpdateProductRequest } from '../types/product';
import type { ProductSize, SizeDto, SizeCategory } from '../types/size';
import { SIZE_CATEGORY_LABELS } from '../types/size';
import GenerateDescriptionButton from '../components/GenerateDescriptionButton';
import AdminLayout from '../components/AdminLayout';
import ImageUploadField from '../components/common/ImageUploadField';
import Pagination from '../components/common/Pagination';
import { getImageUrl } from '../utils/imageUrl';

const EMPTY_CREATE: CreateProductRequest = {
  name: '', description: '', price: 0, originalPrice: undefined,
  categoryId: '', slug: '', imageUrl: '', hasSizes: false, sizeType: null,
};

const INPUT_CLS = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-rose-400 focus:outline-none';

export default function AdminProductPage() {
  const navigate = useNavigate();
  const [products,    setProducts]    = useState<ProductDto[]>([]);
  const [categories,  setCategories]  = useState<CategoryDto[]>([]);
  const [loading,     setLoading]     = useState(true);
  const [page,        setPage]        = useState(1);
  const [totalPages,  setTotalPages]  = useState(1);

  // Create modal
  const [showCreate,  setShowCreate]  = useState(false);
  const [createForm,  setCreateForm]  = useState<CreateProductRequest>(EMPTY_CREATE);
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating,    setCreating]    = useState(false);
  const [createKey,   setCreateKey]   = useState(0);

  // Edit modal
  const [editProduct, setEditProduct] = useState<ProductDto | null>(null);
  const [editForm,    setEditForm]    = useState<UpdateProductRequest>({ name: '', description: '', price: 0, originalPrice: null, imageUrl: null });
  const [editError,   setEditError]   = useState<string | null>(null);
  const [editing,     setEditing]     = useState(false);

  // Size management modal
  const [sizeProduct,    setSizeProduct]    = useState<ProductDto | null>(null);
  const [masterSizes,    setMasterSizes]    = useState<SizeDto[]>([]);
  const [checkedSizeIds, setCheckedSizeIds] = useState<Set<string>>(new Set());
  const [sizesLoading,   setSizesLoading]   = useState(false);
  const [savingSize,     setSavingSize]     = useState(false);

  const loadProducts = async (p: number) => {
    setLoading(true);
    try {
      const result = await productService.getProducts({ page: p, pageSize: 15 });
      setProducts(result.items);
      setTotalPages(result.totalPages);
    } catch { /* ignore */ } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    categoryService.getCategories().then(setCategories).catch(console.error);
    loadProducts(page);
  }, [page]);

  /* ── Create ── */
  const handleCreateNameChange = (name: string) =>
    setCreateForm((f) => ({ ...f, name, slug: slugify(name) }));

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);
    setCreating(true);
    try {
      await productService.createProduct({
        ...createForm,
        originalPrice: createForm.originalPrice || undefined,
        imageUrl: createForm.imageUrl || null,
      });
      setShowCreate(false);
      setCreateForm(EMPTY_CREATE);
      setCreateKey((k) => k + 1);
      await loadProducts(1);
      setPage(1);
      toast.success('Đã tạo món mới.');
    } catch (err) {
      setCreateError(getApiError(err, 'Tạo món thất bại.'));
    } finally {
      setCreating(false);
    }
  };

  /* ── Edit ── */
  const openEdit = (p: ProductDto) => {
    setEditProduct(p);
    setEditForm({
      name: p.name,
      description: p.description,
      price: p.price,
      originalPrice: p.originalPrice !== p.price ? p.originalPrice : null,
      imageUrl: p.imageUrl || '',
      hasSizes: p.hasSizes,
      sizeType: p.sizeType,
    });
    setEditError(null);
  };

  /* ── Size Management ── */
  const openSizeModal = async (p: ProductDto) => {
    setSizeProduct(p);
    setSizesLoading(true);
    try {
      const category = p.sizeType as SizeCategory | null;
      const [masters, current] = await Promise.all([
        category ? sizeService.getByCategory(category) : sizeService.getAllAdmin(),
        sizeService.getProductSizes(p.id),
      ]);
      setMasterSizes(masters);
      const checked = new Set(current.filter((ps) => ps.sizeId).map((ps) => ps.sizeId as string));
      setCheckedSizeIds(checked);
    } finally {
      setSizesLoading(false);
    }
  };

  const toggleSizeCheck = (sizeId: string) => {
    setCheckedSizeIds((prev) => {
      const next = new Set(prev);
      if (next.has(sizeId)) next.delete(sizeId);
      else next.add(sizeId);
      return next;
    });
  };

  const handleSaveSizes = async () => {
    if (!sizeProduct) return;
    setSavingSize(true);
    try {
      await sizeService.setProductSizes(sizeProduct.id, Array.from(checkedSizeIds));
      toast.success('Đã lưu kích cỡ.');
      setSizeProduct(null);
    } catch (err) {
      toast.error(getApiError(err, 'Lưu kích cỡ thất bại.'));
    } finally {
      setSavingSize(false);
    }
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editProduct) return;
    setEditError(null);
    setEditing(true);
    try {
      await productService.updateProduct(editProduct.id, {
        ...editForm,
        originalPrice: editForm.originalPrice || null,
        imageUrl: editForm.imageUrl || null,
      });
      setEditProduct(null);
      await loadProducts(page);
      toast.success('Đã cập nhật món ăn.');
    } catch (err) {
      setEditError(getApiError(err, 'Cập nhật món thất bại.'));
    } finally {
      setEditing(false);
    }
  };

  /* ── Delete ── */
  const handleDelete = async (id: string, name: string) => {
    if (!confirm(`Gỡ món "${name}" khỏi thực đơn?`)) return;
    try {
      await productService.deleteProduct(id);
      await loadProducts(page);
      toast.success('Đã gỡ món khỏi thực đơn.');
    } catch {
      toast.error('Gỡ món thất bại.');
    }
  };

  /* ── Shared description field ── */
  const DescField = ({ value, onChange, categoryName, productName }: {
    value: string; onChange: (v: string) => void;
    categoryName: string; productName: string;
  }) => (
    <div>
      <div className="flex items-center justify-between mb-1">
        <label className="block text-sm font-medium text-gray-700">Mô tả món</label>
        <GenerateDescriptionButton
          productName={productName}
          categoryName={categoryName}
          onGenerated={onChange}
        />
      </div>
      <textarea required rows={3} className={INPUT_CLS}
        value={value} onChange={(e) => onChange(e.target.value)} />
    </div>
  );

  return (
    <AdminLayout title="Quản lý món ăn">
      <div className="flex justify-end mb-4">
        <button
          onClick={() => { setShowCreate(true); setCreateError(null); setCreateForm(EMPTY_CREATE); setCreateKey((k) => k + 1); }}
          className="bg-rose-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-rose-700"
        >
          + Thêm món
        </button>
      </div>

      {/* Create Modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg p-6 my-4">
            <h2 className="text-lg font-semibold mb-4">Thêm món mới</h2>
            <form onSubmit={handleCreate} className="space-y-3">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tên món</label>
                <input required className={INPUT_CLS} value={createForm.name}
                  onChange={(e) => handleCreateNameChange(e.target.value)} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
                <input required className={`${INPUT_CLS} bg-gray-50`} value={createForm.slug}
                  onChange={(e) => setCreateForm((f) => ({ ...f, slug: e.target.value }))} />
              </div>
              <DescField
                value={createForm.description}
                onChange={(v) => setCreateForm((f) => ({ ...f, description: v }))}
                productName={createForm.name}
                categoryName={categories.find((c) => c.id === createForm.categoryId)?.name ?? ''}
              />
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Giá bán (VND)</label>
                  <input required type="number" min={1} className={INPUT_CLS}
                    value={createForm.price || ''}
                    onChange={(e) => setCreateForm((f) => ({ ...f, price: Number(e.target.value) }))} />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Giá niêm yết (VND)</label>
                  <input type="number" min={0} placeholder="Để trống = giá bán" className={INPUT_CLS}
                    value={createForm.originalPrice || ''}
                    onChange={(e) => setCreateForm((f) => ({ ...f, originalPrice: Number(e.target.value) || undefined }))} />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Nhóm món</label>
                <select required className={INPUT_CLS} value={createForm.categoryId}
                  onChange={(e) => setCreateForm((f) => ({ ...f, categoryId: e.target.value }))}>
                  <option value="">-- Chọn nhóm món --</option>
                  {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <div className="flex items-center gap-2">
                <input type="checkbox" id="create-has-sizes" className="w-4 h-4 rounded border-gray-300"
                  checked={createForm.hasSizes || false}
                  onChange={(e) => setCreateForm((f) => ({ ...f, hasSizes: e.target.checked, sizeType: e.target.checked ? f.sizeType : null }))} />
                <label htmlFor="create-has-sizes" className="text-sm font-medium text-gray-700">Sản phẩm có kích cỡ</label>
              </div>
              {createForm.hasSizes && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Loại kích cỡ</label>
                  <select className={INPUT_CLS} value={createForm.sizeType || ''}
                    onChange={(e) => setCreateForm((f) => ({ ...f, sizeType: e.target.value || null }))}>
                    <option value="">-- Chọn loại --</option>
                    <option value="DrinkSize">Đồ uống</option>
                    <option value="FoodPortion">Khẩu phần ăn</option>
                    <option value="MealSize">Suất ăn / Combo</option>
                    <option value="Custom">Tùy chỉnh</option>
                  </select>
                </div>
              )}
              <ImageUploadField
                key={createKey}
                currentUrl={createForm.imageUrl}
                onUploaded={(url) => setCreateForm((f) => ({ ...f, imageUrl: url }))}
              />
              {createError && <p className="text-sm text-red-500">{createError}</p>}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" onClick={() => setShowCreate(false)}
                  className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Hủy</button>
                <button type="submit" disabled={creating}
                  className="px-4 py-2 text-sm bg-rose-600 text-white rounded-lg hover:bg-rose-700 disabled:opacity-60">
                  {creating ? 'Đang lưu...' : 'Lưu'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Modal */}
      {editProduct && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg p-6 my-4">
            <h2 className="text-lg font-semibold mb-1">Chỉnh sửa món ăn</h2>
            <p className="text-xs text-gray-400 mb-4 font-mono">{editProduct.slug}</p>
            <form onSubmit={handleEdit} className="space-y-3">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tên món</label>
                <input required className={INPUT_CLS} value={editForm.name}
                  onChange={(e) => setEditForm((f) => ({ ...f, name: e.target.value }))} />
              </div>
              <DescField
                value={editForm.description}
                onChange={(v) => setEditForm((f) => ({ ...f, description: v }))}
                productName={editForm.name}
                categoryName={categories.find((c) => c.id === editProduct.categoryId)?.name ?? ''}
              />
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Giá bán (VND)</label>
                  <input required type="number" min={1} className={INPUT_CLS}
                    value={editForm.price || ''}
                    onChange={(e) => setEditForm((f) => ({ ...f, price: Number(e.target.value) }))} />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Giá niêm yết (VND)</label>
                  <input type="number" min={0} placeholder="Để trống = giữ nguyên" className={INPUT_CLS}
                    value={editForm.originalPrice ?? ''}
                    onChange={(e) => setEditForm((f) => ({ ...f, originalPrice: Number(e.target.value) || null }))} />
                </div>
              </div>
              <div className="flex items-center gap-2">
                <input type="checkbox" id="edit-has-sizes" className="w-4 h-4 rounded border-gray-300"
                  checked={editForm.hasSizes || false}
                  onChange={(e) => setEditForm((f) => ({ ...f, hasSizes: e.target.checked, sizeType: e.target.checked ? f.sizeType : null }))} />
                <label htmlFor="edit-has-sizes" className="text-sm font-medium text-gray-700">Sản phẩm có kích cỡ</label>
              </div>
              {editForm.hasSizes && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Loại kích cỡ</label>
                  <select className={INPUT_CLS} value={editForm.sizeType || ''}
                    onChange={(e) => setEditForm((f) => ({ ...f, sizeType: e.target.value || null }))}>
                    <option value="">-- Chọn loại --</option>
                    <option value="DrinkSize">Đồ uống</option>
                    <option value="FoodPortion">Khẩu phần ăn</option>
                    <option value="MealSize">Suất ăn / Combo</option>
                    <option value="Custom">Tùy chỉnh</option>
                  </select>
                </div>
              )}
              <ImageUploadField
                key={editProduct?.id}
                currentUrl={editForm.imageUrl}
                onUploaded={(url) => setEditForm((f) => ({ ...f, imageUrl: url }))}
              />
              {editError && <p className="text-sm text-red-500">{editError}</p>}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" onClick={() => setEditProduct(null)}
                  className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Hủy</button>
                <button type="submit" disabled={editing}
                  className="px-4 py-2 text-sm bg-rose-600 text-white rounded-lg hover:bg-rose-700 disabled:opacity-60">
                  {editing ? 'Đang lưu...' : 'Cập nhật'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Products Table */}
      {loading ? (
        <div className="flex items-center justify-center h-64 text-gray-400">Đang tải...</div>
      ) : (
        <>
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left px-4 py-3 font-medium text-gray-600">Ảnh</th>
                  <th className="text-left px-4 py-3 font-medium text-gray-600">Món ăn</th>
                  <th className="text-left px-4 py-3 font-medium text-gray-600">Giá bán</th>
                  <th className="text-left px-4 py-3 font-medium text-gray-600 hidden sm:table-cell">Giá niêm yết</th>
                  <th className="text-left px-4 py-3 font-medium text-gray-600">Trạng thái</th>
                  <th className="text-right px-4 py-3 font-medium text-gray-600">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {products.map((p) => (
                  <tr key={p.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <div className="h-14 w-14 overflow-hidden rounded-xl bg-gray-100">
                        {p.imageUrl ? (
                          <img src={getImageUrl(p.imageUrl)} alt={p.name} className="h-full w-full object-cover" />
                        ) : (
                          <div className="flex h-full w-full items-center justify-center text-lg text-gray-300">🍔</div>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-medium text-gray-800 line-clamp-1">{p.name}</div>
                      <div className="text-xs text-gray-400 font-mono">{p.slug}</div>
                    </td>
                    <td className="px-4 py-3 text-rose-600 font-medium">{formatPrice(p.price)}</td>
                    <td className="px-4 py-3 text-gray-400 text-xs hidden sm:table-cell">
                      {p.originalPrice > p.price ? (
                        <span className="line-through">{formatPrice(p.originalPrice)}</span>
                      ) : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${p.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'}`}>
                        {p.isActive ? 'Đang phục vụ' : 'Đã ẩn'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right space-x-3">
                      <button onClick={() => navigate(`/products/${p.slug}`)}
                        className="text-rose-600 hover:underline text-xs">Xem</button>
                      <button onClick={() => openEdit(p)}
                        className="text-yellow-600 hover:underline text-xs">Sửa</button>
                      {p.hasSizes && (
                        <button onClick={() => openSizeModal(p)}
                          className="text-blue-600 hover:underline text-xs">Kích cỡ</button>
                      )}
                      <button onClick={() => handleDelete(p.id, p.name)}
                        className="text-red-500 hover:underline text-xs">Gỡ</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}

      {/* Size Management Modal */}
      {sizeProduct && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            <div className="flex items-center justify-between mb-1">
              <div>
                <h2 className="text-lg font-semibold">Chọn kích cỡ</h2>
                <p className="text-xs text-gray-400">{sizeProduct.name}</p>
              </div>
              <button onClick={() => setSizeProduct(null)} className="text-gray-400 hover:text-gray-600">✕</button>
            </div>
            {sizeProduct.sizeType && (
              <p className="text-xs text-rose-500 mb-3">
                Loại: {SIZE_CATEGORY_LABELS[sizeProduct.sizeType as SizeCategory] ?? sizeProduct.sizeType}
              </p>
            )}

            {sizesLoading ? (
              <p className="text-sm text-gray-400 text-center py-6">Đang tải kích cỡ...</p>
            ) : masterSizes.length === 0 ? (
              <div className="py-6 text-center">
                <p className="text-sm text-gray-400">Chưa có kích cỡ nào trong danh mục này.</p>
                <a href="/admin/sizes" className="text-xs text-rose-600 hover:underline mt-1 block">
                  Quản lý kích cỡ →
                </a>
              </div>
            ) : (
              <ul className="space-y-2 mb-5 max-h-64 overflow-y-auto">
                {masterSizes.map((s) => (
                  <li key={s.id}
                    className="flex items-center gap-3 px-3 py-2 border rounded-lg hover:bg-gray-50 cursor-pointer"
                    onClick={() => toggleSizeCheck(s.id)}
                  >
                    <input
                      type="checkbox"
                      readOnly
                      checked={checkedSizeIds.has(s.id)}
                      className="w-4 h-4 rounded border-gray-300 text-rose-600 cursor-pointer"
                    />
                    <span className="text-sm font-medium flex-1">{s.label}</span>
                    <span className="text-xs text-gray-400">#{s.displayOrder}</span>
                  </li>
                ))}
              </ul>
            )}

            <div className="flex justify-end gap-2 pt-2 border-t">
              <button type="button" onClick={() => setSizeProduct(null)}
                className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Hủy</button>
              <button onClick={handleSaveSizes} disabled={savingSize || masterSizes.length === 0}
                className="px-4 py-2 text-sm bg-rose-600 text-white rounded-lg hover:bg-rose-700 disabled:opacity-60">
                {savingSize ? 'Đang lưu...' : `Lưu (${checkedSizeIds.size})`}
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
