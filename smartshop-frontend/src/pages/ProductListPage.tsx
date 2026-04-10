import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { categoryService, productService } from '../services/productService';
import { cartService } from '../services/cartService';
import { orderService } from '../services/orderService';
import { useAuthStore } from '../store/authStore';
import type { CategoryDto, PagedResult, ProductDto } from '../types/product';
import { FiShoppingCart, FiPackage, FiLogOut, FiSearch, FiCpu } from 'react-icons/fi';
import AISearchBar from '../components/AISearchBar';

const formatPrice = (price: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

export default function ProductListPage() {
  const navigate = useNavigate();
  const { isAuthenticated, user, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };
  const [products, setProducts] = useState<PagedResult<ProductDto> | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [aiMode, setAiMode] = useState(false);
  const [categoryId, setCategoryId] = useState<string | undefined>(undefined);
  const [sortBy, setSortBy] = useState<number>(0); // 0 = Newest
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [addingId, setAddingId] = useState<string | null>(null);
  const [cartCount, setCartCount] = useState(0);
  const [orderCount, setOrderCount] = useState(0);

  useEffect(() => {
    categoryService.getCategories().then(setCategories).catch(console.error);
  }, []);

  useEffect(() => {
    if (!isAuthenticated) return;
    cartService.getCart()
      .then((cart) => setCartCount(cart.items.reduce((sum, i) => sum + i.quantity, 0)))
      .catch(() => setCartCount(0));
    orderService.getMyOrders(1, 1)
      .then((result) => setOrderCount(result.totalCount))
      .catch(() => setOrderCount(0));
  }, [isAuthenticated]);

  useEffect(() => {
    setLoading(true);
    productService
      .getProducts({ page, pageSize: 12, categoryId, search: search || undefined, sortBy })
      .then(setProducts)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [page, categoryId, search, sortBy]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  };

  const handleCategoryChange = (id: string | undefined) => {
    setCategoryId(id);
    setPage(1);
  };

  const handleSortChange = (value: number) => {
    setSortBy(value);
    setPage(1);
  };

  const handleQuickAdd = async (e: React.MouseEvent, product: ProductDto) => {
    e.preventDefault(); // không navigate vào detail page
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    setAddingId(product.id);
    try {
      await cartService.addToCart(product.id, 1);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      alert(msg ?? 'Thêm vào giỏ hàng thất bại.');
    } finally {
      setAddingId(null);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between gap-4">
          <Link to="/" className="text-xl font-bold text-blue-600">SmartShop</Link>
          <div className="flex-1 max-w-xl flex items-center gap-2">
            {aiMode ? (
              <AISearchBar onClose={() => setAiMode(false)} />
            ) : (
              <form onSubmit={handleSearch} className="flex-1 relative">
                <input
                  className="w-full border border-gray-300 rounded-lg pl-4 pr-10 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
                  placeholder="Tìm kiếm sản phẩm..."
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                />
                <button
                  type="submit"
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-blue-600"
                >
                  <FiSearch size={18} />
                </button>
              </form>
            )}
            <button
              onClick={() => setAiMode(!aiMode)}
              title={aiMode ? 'Tìm kiếm thường' : 'Tìm kiếm AI'}
              className={`shrink-0 flex items-center gap-1 px-2.5 py-2 rounded-lg text-xs font-medium border transition-colors ${
                aiMode
                  ? 'bg-blue-600 text-white border-blue-600'
                  : 'bg-white text-blue-600 border-blue-300 hover:bg-blue-50'
              }`}
            >
              <FiCpu size={14} />
              AI
            </button>
          </div>
          <div className="flex items-center gap-3 shrink-0">
            {isAuthenticated ? (
              <>
                <span className="text-sm text-gray-600 hidden sm:block">
                  Xin chào, <strong>{user?.firstName}</strong>
                </span>
                <Link to="/cart" className="relative text-gray-500 hover:text-blue-600" title="Giỏ hàng">
                  <FiShoppingCart size={20} />
                  {cartCount > 0 && (
                    <span className="absolute -top-1.5 -right-1.5 bg-red-500 text-white text-[10px] font-bold min-w-4 h-4 px-0.5 rounded-full flex items-center justify-center leading-none">
                      {cartCount > 99 ? '99+' : cartCount}
                    </span>
                  )}
                </Link>
                <Link to="/orders" className="relative text-gray-500 hover:text-blue-600" title="Đơn hàng">
                  <FiPackage size={20} />
                  {orderCount > 0 && (
                    <span className="absolute -top-1.5 -right-1.5 bg-red-500 text-white text-[10px] font-bold min-w-4 h-4 px-0.5 rounded-full flex items-center justify-center leading-none">
                      {orderCount > 99 ? '99+' : orderCount}
                    </span>
                  )}
                </Link>
                {user?.role === 'Admin' && (
                  <Link to="/admin/products" className="text-sm text-gray-500 hover:text-blue-600">Admin</Link>
                )}
                <button
                  onClick={handleLogout}
                  className="text-red-500 hover:text-red-700"
                  title="Đăng xuất"
                >
                  <FiLogOut size={20} />
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-sm text-gray-500 hover:text-blue-600">Đăng nhập</Link>
                <Link to="/register" className="text-sm bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700">Đăng ký</Link>
              </>
            )}
          </div>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 py-6 flex gap-6">
        {/* Sidebar Categories */}
        <aside className="w-48 shrink-0">
          <h3 className="font-semibold text-gray-700 mb-3">Danh mục</h3>
          <ul className="space-y-1">
            <li>
              <button
                onClick={() => handleCategoryChange(undefined)}
                className={`w-full text-left px-3 py-1.5 rounded text-sm ${!categoryId ? 'bg-blue-100 text-blue-700 font-medium' : 'text-gray-600 hover:bg-gray-100'}`}
              >
                Tất cả
              </button>
            </li>
            {categories.map((cat) => (
              <li key={cat.id}>
                <button
                  onClick={() => handleCategoryChange(cat.id)}
                  className={`w-full text-left px-3 py-1.5 rounded text-sm ${categoryId === cat.id ? 'bg-blue-100 text-blue-700 font-medium' : 'text-gray-600 hover:bg-gray-100'}`}
                >
                  {cat.name}
                </button>
              </li>
            ))}
          </ul>
        </aside>

        {/* Main */}
        <main className="flex-1">
          {/* Toolbar: result count + sort */}
          <div className="mb-4 flex items-center justify-between gap-4 flex-wrap">
            <span className="text-sm text-gray-500">
              {products && `Hiển thị ${products.items.length} / ${products.totalCount} sản phẩm`}
            </span>
            <div className="flex items-center gap-1.5 flex-wrap justify-end">
              <span className="text-sm text-gray-500 hidden sm:inline mr-1">Sắp xếp:</span>

              {/* Pill: Mới */}
              <button
                onClick={() => handleSortChange(0)}
                className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors
                  ${sortBy === 0
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'bg-white text-gray-600 border-gray-300 hover:border-blue-400 hover:text-blue-600'}`}
              >
                Mới
              </button>

              {/* Pill: Bán chạy */}
              <button
                onClick={() => handleSortChange(5)}
                className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors
                  ${sortBy === 5
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'bg-white text-gray-600 border-gray-300 hover:border-blue-400 hover:text-blue-600'}`}
              >
                Bán chạy
              </button>

              {/* Combo: Giá */}
              <select
                value={[1, 2].includes(sortBy) ? sortBy : ''}
                onChange={(e) => e.target.value !== '' && handleSortChange(Number(e.target.value))}
                className={`px-2.5 py-1 rounded-full text-xs font-medium border transition-colors cursor-pointer
                  ${[1, 2].includes(sortBy)
                    ? 'border-blue-600 text-blue-600 bg-blue-50'
                    : 'border-gray-300 text-gray-600 bg-white hover:border-blue-400 hover:text-blue-600'}`}
              >
                <option value="" disabled>Giá ↕</option>
                <option value={1}>Giá tăng dần</option>
                <option value={2}>Giá giảm dần</option>
              </select>

              {/* Combo: Tên */}
              <select
                value={[3, 4].includes(sortBy) ? sortBy : ''}
                onChange={(e) => e.target.value !== '' && handleSortChange(Number(e.target.value))}
                className={`px-2.5 py-1 rounded-full text-xs font-medium border transition-colors cursor-pointer
                  ${[3, 4].includes(sortBy)
                    ? 'border-blue-600 text-blue-600 bg-blue-50'
                    : 'border-gray-300 text-gray-600 bg-white hover:border-blue-400 hover:text-blue-600'}`}
              >
                <option value="" disabled>Tên ↕</option>
                <option value={3}>Tên A → Z</option>
                <option value={4}>Tên Z → A</option>
              </select>
            </div>
          </div>

          {loading ? (
            <div className="flex items-center justify-center h-64 text-gray-400">Đang tải...</div>
          ) : (
            <>
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                {products?.items.map((product) => (
                  <Link
                    key={product.id}
                    to={`/products/${product.slug}`}
                    className="bg-white rounded-xl shadow-sm hover:shadow-lg hover:-translate-y-1 hover:border-blue-200 border border-transparent transition-all duration-200 p-3 flex flex-col group cursor-pointer"
                  >
                    <div className="bg-gray-100 rounded-lg h-36 flex items-center justify-center mb-3 overflow-hidden">
                      {product.imageUrl ? (
                        <img src={product.imageUrl} alt={product.name} className="h-full w-full object-contain" />
                      ) : (
                        <span className="text-gray-300 text-4xl">📦</span>
                      )}
                    </div>
                    <p className="text-sm font-medium text-gray-800 line-clamp-2 flex-1">{product.name}</p>
                    <div className="mt-2">
                      <p className="text-blue-600 font-bold text-sm">{formatPrice(product.price)}</p>
                      {product.originalPrice > product.price && (
                        <p className="text-gray-400 text-xs line-through">{formatPrice(product.originalPrice)}</p>
                      )}
                    </div>
                    {product.stock < 5 && product.stock > 0 && (
                      <p className="text-orange-500 text-xs mt-1">Còn {product.stock} sản phẩm</p>
                    )}
                    {product.stock === 0 ? (
                      <p className="mt-2 text-xs text-center text-gray-400 py-1">Hết hàng</p>
                    ) : (
                      <button
                        onClick={(e) => handleQuickAdd(e, product)}
                        disabled={addingId === product.id}
                        className="mt-2 w-full text-xs bg-blue-600 text-white rounded-lg py-1.5 hover:bg-blue-700 disabled:opacity-50 transition-colors"
                      >
                        {addingId === product.id ? 'Đang thêm...' : '+ Giỏ hàng'}
                      </button>
                    )}
                  </Link>
                ))}
              </div>

              {/* Pagination */}
              {products && products.totalPages > 1 && (
                <div className="mt-8 flex justify-center gap-2">
                  <button
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="px-3 py-1.5 rounded border text-sm disabled:opacity-40 hover:bg-gray-100"
                  >
                    ← Trước
                  </button>
                  {Array.from({ length: products.totalPages }, (_, i) => i + 1).map((p) => (
                    <button
                      key={p}
                      onClick={() => setPage(p)}
                      className={`px-3 py-1.5 rounded border text-sm ${p === page ? 'bg-blue-600 text-white border-blue-600' : 'hover:bg-gray-100'}`}
                    >
                      {p}
                    </button>
                  ))}
                  <button
                    onClick={() => setPage((p) => Math.min(products.totalPages, p + 1))}
                    disabled={page === products.totalPages}
                    className="px-3 py-1.5 rounded border text-sm disabled:opacity-40 hover:bg-gray-100"
                  >
                    Sau →
                  </button>
                </div>
              )}
            </>
          )}
        </main>
      </div>
    </div>
  );
}
