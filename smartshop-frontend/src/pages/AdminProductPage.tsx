import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { categoryService, productService } from '../services/productService';
import { useAuthStore } from '../store/authStore';
import type { CategoryDto, CreateProductRequest, ProductDto } from '../types/product';
import { FiLogOut, FiArrowLeft } from 'react-icons/fi';
import GenerateDescriptionButton from '../components/GenerateDescriptionButton';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

const slugify = (text: string) =>
  text
    .toLowerCase()
    .replace(/[àáâãäå]/g, 'a')
    .replace(/[èéêë]/g, 'e')
    .replace(/[ìíîï]/g, 'i')
    .replace(/[òóôõö]/g, 'o')
    .replace(/[ùúûü]/g, 'u')
    .replace(/[ý]/g, 'y')
    .replace(/[đ]/g, 'd')
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-');

const EMPTY_FORM: CreateProductRequest = {
  name: '',
  description: '',
  price: 0,
  stock: 0,
  categoryId: '',
  slug: '',
};

export default function AdminProductPage() {
  const navigate = useNavigate();
  const logout = useAuthStore((s) => s.logout);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateProductRequest>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  const loadProducts = async (p: number) => {
    setLoading(true);
    try {
      const result = await productService.getProducts({ page: p, pageSize: 15 });
      setProducts(result.items);
      setTotalPages(result.totalPages);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    categoryService.getCategories().then(setCategories).catch(console.error);
    loadProducts(page);
  }, [page]);

  const handleNameChange = (name: string) => {
    setForm((f) => ({ ...f, name, slug: slugify(name) }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setSaving(true);
    try {
      await productService.createProduct(form);
      setShowForm(false);
      setForm(EMPTY_FORM);
      await loadProducts(1);
      setPage(1);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      setFormError(msg ?? 'Tạo sản phẩm thất bại.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string, name: string) => {
    if (!confirm(`Xoá sản phẩm "${name}"?`)) return;
    try {
      await productService.deleteProduct(id);
      await loadProducts(page);
    } catch {
      alert('Xoá thất bại.');
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link to="/products" className="text-gray-500 hover:text-blue-600" title="Storefront">
              <FiArrowLeft size={20} />
            </Link>
            <h1 className="text-lg font-semibold text-gray-800">Quản lý sản phẩm</h1>
          </div>
          <div className="flex items-center gap-3">
            <button
              onClick={() => { setShowForm(true); setFormError(null); setForm(EMPTY_FORM); }}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-blue-700"
            >
              + Thêm sản phẩm
            </button>
            <button
              onClick={handleLogout}
              className="text-red-500 hover:text-red-700"
              title="Đăng xuất"
            >
              <FiLogOut size={20} />
            </button>
          </div>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Create Form Modal */}
        {showForm && (
          <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg p-6">
              <h2 className="text-lg font-semibold mb-4">Thêm sản phẩm mới</h2>
              <form onSubmit={handleSubmit} className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Tên sản phẩm</label>
                  <input
                    required
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:outline-none"
                    value={form.name}
                    onChange={(e) => handleNameChange(e.target.value)}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
                  <input
                    required
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:outline-none bg-gray-50"
                    value={form.slug}
                    onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value }))}
                  />
                </div>
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <label className="block text-sm font-medium text-gray-700">Mô tả</label>
                    <GenerateDescriptionButton
                      productName={form.name}
                      categoryName={categories.find((c) => c.id === form.categoryId)?.name ?? ''}
                      onGenerated={(desc) => setForm((f) => ({ ...f, description: desc }))}
                    />
                  </div>
                  <textarea
                    required
                    rows={3}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:outline-none"
                    value={form.description}
                    onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Giá (VNĐ)</label>
                    <input
                      required
                      type="number"
                      min={1}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:outline-none"
                      value={form.price || ''}
                      onChange={(e) => setForm((f) => ({ ...f, price: Number(e.target.value) }))}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Tồn kho</label>
                    <input
                      required
                      type="number"
                      min={0}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:outline-none"
                      value={form.stock || ''}
                      onChange={(e) => setForm((f) => ({ ...f, stock: Number(e.target.value) }))}
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Danh mục</label>
                  <select
                    required
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:outline-none"
                    value={form.categoryId}
                    onChange={(e) => setForm((f) => ({ ...f, categoryId: e.target.value }))}
                  >
                    <option value="">-- Chọn danh mục --</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                </div>
                {formError && <p className="text-sm text-red-500">{formError}</p>}
                <div className="flex justify-end gap-2 pt-2">
                  <button
                    type="button"
                    onClick={() => setShowForm(false)}
                    className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50"
                  >
                    Huỷ
                  </button>
                  <button
                    type="submit"
                    disabled={saving}
                    className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-60"
                  >
                    {saving ? 'Đang lưu...' : 'Lưu'}
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
                    <th className="text-left px-4 py-3 font-medium text-gray-600">Sản phẩm</th>
                    <th className="text-left px-4 py-3 font-medium text-gray-600">Giá</th>
                    <th className="text-left px-4 py-3 font-medium text-gray-600">Tồn kho</th>
                    <th className="text-left px-4 py-3 font-medium text-gray-600">Trạng thái</th>
                    <th className="text-right px-4 py-3 font-medium text-gray-600">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {products.map((p) => (
                    <tr key={p.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3">
                        <div className="font-medium text-gray-800 line-clamp-1">{p.name}</div>
                        <div className="text-xs text-gray-400 font-mono">{p.slug}</div>
                      </td>
                      <td className="px-4 py-3 text-blue-600 font-medium">{formatPrice(p.price)}</td>
                      <td className="px-4 py-3">
                        <span className={p.stock < 5 ? 'text-orange-500 font-medium' : 'text-gray-700'}>
                          {p.stock}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${p.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'}`}>
                          {p.isActive ? 'Đang bán' : 'Đã ẩn'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          onClick={() => navigate(`/products/${p.id}`)}
                          className="text-blue-600 hover:underline text-xs mr-3"
                        >
                          Xem
                        </button>
                        <button
                          onClick={() => handleDelete(p.id, p.name)}
                          className="text-red-500 hover:underline text-xs"
                        >
                          Xoá
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-4 flex justify-center gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="px-3 py-1.5 rounded border text-sm disabled:opacity-40 hover:bg-gray-100"
                >
                  ← Trước
                </button>
                <span className="px-3 py-1.5 text-sm text-gray-600">
                  Trang {page} / {totalPages}
                </span>
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="px-3 py-1.5 rounded border text-sm disabled:opacity-40 hover:bg-gray-100"
                >
                  Sau →
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
