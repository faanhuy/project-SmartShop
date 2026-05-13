import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { categoryService, productService } from '../services/productService';
import { cartService } from '../services/cartService';
import { storeService } from '../services/storeService';
import { useAuthStore } from '../store/authStore';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';
import type { CategoryDto, PagedResult, ProductDto } from '../types/product';
import { FiSearch, FiCpu } from 'react-icons/fi';
import AISearchBar from '../components/AISearchBar';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import WishlistButton from '../components/WishlistButton';

import { formatPrice } from '../utils/formatters';
import { getImageUrl } from '../utils/imageUrl';

export default function ProductListPage() {
  const navigate = useNavigate();
  const { isAuthenticated, refreshCartCount } = useAuthStore();
  const { selectedStore } = useStoreSelectionStore();

  const [products, setProducts] = useState<PagedResult<ProductDto> | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [aiMode, setAiMode] = useState(false);
  const [categoryId, setCategoryId] = useState<string | undefined>(undefined);
  const [sortBy, setSortBy] = useState<number>(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [stockLoading, setStockLoading] = useState(false);
  const [stockByProductId, setStockByProductId] = useState<Record<string, number>>({});
  const [addingId, setAddingId] = useState<string | null>(null);

  useEffect(() => {
    categoryService.getCategories().then(setCategories).catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    productService
      .getProducts({ page, pageSize: 12, categoryId, search: search || undefined, sortBy })
      .then(setProducts)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [page, categoryId, search, sortBy]);

  useEffect(() => {
    const visibleProducts = products?.items ?? [];
    if (!selectedStore || visibleProducts.length === 0) {
      setStockByProductId({});
      return;
    }

    let cancelled = false;
    setStockLoading(true);
    Promise.all(
      visibleProducts.map((product) =>
        storeService
          .getProductStock(selectedStore.id, product.id)
          .then((stock) => [product.id, stock.quantity] as const)
          .catch(() => [product.id, 0] as const),
      ),
    )
      .then((entries) => {
        if (!cancelled) {
          setStockByProductId(Object.fromEntries(entries));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setStockLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [products, selectedStore]);

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
    if (product.hasSizes) {
      toast('Vui lòng chọn size trước khi thêm vào giỏ.');
      navigate(`/products/${product.slug}`);
      return;
    }
    if (selectedStore && stockByProductId[product.id] === 0) {
      toast.error('Sản phẩm đang hết hàng tại chi nhánh đã chọn.');
      return;
    }
    setAddingId(product.id);
    try {
      await cartService.addToCart(product.id, 1);
      refreshCartCount();
      toast.success(`Đã thêm "${product.name}" vào giỏ món!`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(msg ?? 'Thêm món vào giỏ thất bại.');
    } finally {
      setAddingId(null);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar>
        <div className="flex items-center gap-2">
          {aiMode ? (
            <AISearchBar onClose={() => setAiMode(false)} />
          ) : (
            <form onSubmit={handleSearch} className="flex-1 relative">
              <input
                className="w-full border border-gray-300 rounded-lg pl-4 pr-10 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                placeholder="Tìm burger, pizza, gà rán, trà sữa..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
              />
              <button
                type="submit"
                className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-rose-600"
              >
                <FiSearch size={18} />
              </button>
            </form>
          )}
          <button
            onClick={() => setAiMode(!aiMode)}
            title={aiMode ? 'Tìm món thường' : 'Tìm món bằng AI'}
            className={`shrink-0 flex items-center gap-1 px-2.5 py-2 rounded-lg text-xs font-medium border transition-colors ${
              aiMode
                ? 'bg-rose-600 text-white border-rose-600'
                : 'bg-white text-rose-600 border-rose-300 hover:bg-rose-50'
            }`}
          >
            <FiCpu size={14} />
            AI
          </button>
        </div>
      </Navbar>

      <div className="max-w-7xl mx-auto px-4 py-6 flex gap-6">
        {/* Sidebar Categories */}
        <aside className="w-48 shrink-0">
          <h3 className="font-semibold text-gray-700 mb-3">Nhóm món</h3>
          <ul className="space-y-1">
            <li>
              <button
                onClick={() => handleCategoryChange(undefined)}
                className={`w-full text-left px-3 py-1.5 rounded text-sm ${!categoryId ? 'bg-rose-100 text-rose-700 font-medium' : 'text-gray-600 hover:bg-gray-100'}`}
              >
                Tất cả món
              </button>
            </li>
            {categories.map((cat) => (
              <li key={cat.id}>
                <button
                  onClick={() => handleCategoryChange(cat.id)}
                  className={`w-full text-left px-3 py-1.5 rounded text-sm ${categoryId === cat.id ? 'bg-rose-100 text-rose-700 font-medium' : 'text-gray-600 hover:bg-gray-100'}`}
                >
                  {cat.name}
                </button>
              </li>
            ))}
          </ul>
        </aside>

        {/* Main */}
        <main className="flex-1">
          <div className="mb-6 rounded-3xl bg-gradient-to-r from-rose-600 via-red-500 to-amber-500 px-6 py-7 text-white shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-rose-100">Fast delivery menu</p>
            <h1 className="mt-2 text-2xl font-bold sm:text-3xl">Đặt đồ ăn nhanh, giao nóng hổi trong vài chạm</h1>
            <p className="mt-2 max-w-2xl text-sm text-rose-50 sm:text-base">
              Chọn burger, gà rán, pizza, mì Ý và đồ uống cho bữa trưa văn phòng, bữa tối nhẹ nhàng hoặc combo xem phim cuối tuần.
            </p>
          </div>

          {/* Toolbar: result count + sort */}
          <div className="mb-4 flex items-center justify-between gap-4 flex-wrap">
            <span className="text-sm text-gray-500">
              {products && `Hiển thị ${products.items.length} / ${products.totalCount} món ăn`}
            </span>
            <div className="flex items-center gap-1.5 flex-wrap justify-end">
              <span className="text-sm text-gray-500 hidden sm:inline mr-1">Sắp xếp:</span>

              {/* Pill: Mới */}
              <button
                onClick={() => handleSortChange(0)}
                className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors
                  ${sortBy === 0
                    ? 'bg-rose-600 text-white border-rose-600'
                    : 'bg-white text-gray-600 border-gray-300 hover:border-rose-400 hover:text-rose-600'}`}
              >
                Mới
              </button>

              {/* Pill: Bán chạy */}
              <button
                onClick={() => handleSortChange(5)}
                className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors
                  ${sortBy === 5
                    ? 'bg-rose-600 text-white border-rose-600'
                    : 'bg-white text-gray-600 border-gray-300 hover:border-rose-400 hover:text-rose-600'}`}
              >
                Bán chạy
              </button>

              {/* Combo: Giá */}
              <select
                value={[1, 2].includes(sortBy) ? sortBy : ''}
                onChange={(e) => e.target.value !== '' && handleSortChange(Number(e.target.value))}
                className={`px-2.5 py-1 rounded-full text-xs font-medium border transition-colors cursor-pointer
                  ${[1, 2].includes(sortBy)
                    ? 'border-rose-600 text-rose-600 bg-rose-50'
                    : 'border-gray-300 text-gray-600 bg-white hover:border-rose-400 hover:text-rose-600'}`}
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
                    ? 'border-rose-600 text-rose-600 bg-rose-50'
                    : 'border-gray-300 text-gray-600 bg-white hover:border-rose-400 hover:text-rose-600'}`}
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
                {products?.items.map((product) => {
                  const stock = selectedStore ? stockByProductId[product.id] : null;
                  const outOfStock = stock === 0;
                  const quickAddDisabled = addingId === product.id || outOfStock || stockLoading;

                  return (
                  <Link
                    key={product.id}
                    to={`/products/${product.slug}`}
                    className={`relative bg-white rounded-xl shadow-sm hover:shadow-lg hover:-translate-y-1 hover:border-rose-200 border border-transparent transition-all duration-200 p-3 flex flex-col group cursor-pointer ${
                      outOfStock ? 'opacity-75' : ''
                    }`}
                  >
                    <div className="absolute top-2 right-2">
                      <WishlistButton productId={product.id} />
                    </div>
                    {outOfStock && (
                      <span className="absolute left-2 top-2 rounded-full bg-gray-900/80 px-2 py-0.5 text-[11px] font-medium text-white">
                        Hết hàng
                      </span>
                    )}
                    <div className="bg-gray-100 rounded-lg h-36 flex items-center justify-center mb-3 overflow-hidden">
                      {product.imageUrl ? (
                        <img src={getImageUrl(product.imageUrl)} alt={product.name} className="h-full w-full object-contain" />
                      ) : (
                        <span className="text-gray-300 text-4xl">🍔</span>
                      )}
                    </div>
                    <p className="text-sm font-medium text-gray-800 line-clamp-2 flex-1">{product.name}</p>
                    <div className="mt-2">
                      <p className="text-rose-600 font-bold text-sm">{formatPrice(product.price)}</p>
                      {product.originalPrice > product.price && (
                        <p className="text-gray-400 text-xs line-through">{formatPrice(product.originalPrice)}</p>
                      )}
                      {selectedStore && (
                        <p className={`mt-1 text-xs ${outOfStock ? 'text-red-500' : 'text-green-600'}`}>
                          {stockLoading && stock === undefined
                            ? 'Đang kiểm tra tồn kho...'
                            : outOfStock
                            ? 'Hết hàng tại chi nhánh'
                            : `Còn ${stock ?? 0} sản phẩm`}
                        </p>
                      )}
                    </div>
                    <button
                      onClick={(e) => handleQuickAdd(e, product)}
                      disabled={quickAddDisabled}
                      className="mt-2 w-full text-xs bg-rose-600 text-white rounded-lg py-1.5 hover:bg-rose-700 disabled:opacity-50 transition-colors"
                    >
                      {addingId === product.id
                        ? 'Đang thêm...'
                        : outOfStock
                        ? 'Hết hàng'
                        : product.hasSizes
                        ? 'Chọn size'
                        : '+ Thêm món'}
                    </button>
                  </Link>
                  );
                })}
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
                      className={`px-3 py-1.5 rounded border text-sm ${p === page ? 'bg-rose-600 text-white border-rose-600' : 'hover:bg-gray-100'}`}
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
      <Footer />
    </div>
  );
}
