import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  FiShoppingCart, FiPackage, FiLogOut, FiGrid,
  FiUser, FiHeart, FiMapPin, FiChevronDown, FiCheck,
} from 'react-icons/fi';
import { useAuthStore } from '../store/authStore';
import { cartService } from '../services/cartService';
import { orderService } from '../services/orderService';
import { wishlistService } from '../services/wishlistService';
import { useStoreSelectionStore } from '../store/useStoreSelectionStore';
import NotificationBell from './NotificationBell';

interface NavbarProps {
  children?: React.ReactNode; // slot cho search bar (ProductListPage)
}

export default function Navbar({ children }: NavbarProps) {
  const navigate = useNavigate();
  const { isAuthenticated, user, logout, cartVersion, wishlistVersion } = useAuthStore();
  const [cartCount, setCartCount] = useState(0);
  const [orderCount, setOrderCount] = useState(0);
  const [wishlistCount, setWishlistCount] = useState(0);

  // Store selection
  const { stores, selectedStore, setSelectedStore, fetchStores } = useStoreSelectionStore();
  const [storeDropdownOpen, setStoreDropdownOpen] = useState(false);
  const storeDropdownRef = useRef<HTMLDivElement>(null);

  const activeStores = stores.filter((s) => s.isActive !== false);

  useEffect(() => {
    if (stores.length === 0) fetchStores().catch(() => {});
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Đóng dropdown khi click ngoài
  useEffect(() => {
    if (!storeDropdownOpen) return;
    const handler = (e: MouseEvent) => {
      if (storeDropdownRef.current && !storeDropdownRef.current.contains(e.target as Node)) {
        setStoreDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [storeDropdownOpen]);

  // Đóng dropdown khi nhấn Escape
  useEffect(() => {
    if (!storeDropdownOpen) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setStoreDropdownOpen(false);
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [storeDropdownOpen]);

  useEffect(() => {
    if (!isAuthenticated) {
      setCartCount(0);
      setOrderCount(0);
      return;
    }
    cartService.getCart()
      .then((cart) => setCartCount(cart.items.reduce((s, i) => s + i.quantity, 0)))
      .catch(() => setCartCount(0));
  }, [isAuthenticated, cartVersion]);

  useEffect(() => {
    if (!isAuthenticated) { setOrderCount(0); return; }
    orderService.getMyOrders(1, 1)
      .then((r) => setOrderCount(r.totalCount))
      .catch(() => setOrderCount(0));
  }, [isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated) { setWishlistCount(0); return; }
    wishlistService.getWishlist()
      .then((items) => setWishlistCount(items.length))
      .catch(() => setWishlistCount(0));
  }, [isAuthenticated, wishlistVersion]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="bg-white/90 backdrop-blur-md border-b border-gray-100 shadow-sm sticky top-0 z-10 border-t-4 border-t-rose-500">
      <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between gap-4">
        {/* Logo */}
        <Link to="/products" className="shrink-0 flex items-center gap-2.5">
          <svg width="40" height="40" viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <linearGradient id="hLogoGrad" x1="0" y1="0" x2="40" y2="40" gradientUnits="userSpaceOnUse">
                <stop offset="0%" stopColor="#fb7185" />
                <stop offset="100%" stopColor="#f97316" />
              </linearGradient>
            </defs>
            <rect width="40" height="40" rx="14" fill="url(#hLogoGrad)" />
            <line x1="11" y1="9" x2="11" y2="31" stroke="white" strokeWidth="5" strokeLinecap="round" strokeOpacity="0.95" />
            <line x1="29" y1="9" x2="29" y2="31" stroke="white" strokeWidth="5" strokeLinecap="round" strokeOpacity="0.95" />
            <path d="M13.5 20 Q20 13.5 26.5 20" stroke="white" strokeWidth="3" strokeLinecap="round" fill="none" strokeOpacity="0.95" />
            <path d="M13 21.5 Q16 20 19.5 21.5 Q23 20 26.5 21.5" stroke="#86efac" strokeWidth="1.5" strokeLinecap="round" fill="none" opacity="0.85" />
            <line x1="13" y1="23.5" x2="27" y2="23.5" stroke="#fcd34d" strokeWidth="3" strokeLinecap="round" />
            <line x1="13.5" y1="26" x2="26.5" y2="26" stroke="white" strokeWidth="2.5" strokeLinecap="round" strokeOpacity="0.9" />
          </svg>
          <div className="flex flex-col">
            <span className="text-lg font-extrabold text-rose-600 leading-tight tracking-tight">FastFood</span>
            <span className="hidden sm:block text-[10px] text-gray-400 leading-none">Đồ ăn nhanh — giao tận nơi</span>
          </div>
        </Link>

        {/* Store picker */}
        <div ref={storeDropdownRef} className="relative shrink-0">
          <button
            onClick={() => setStoreDropdownOpen((o) => !o)}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full border text-sm transition-colors ${
              selectedStore
                ? 'border-rose-200 bg-rose-50 text-rose-700 hover:bg-rose-100'
                : 'border-gray-200 bg-gray-50 text-gray-400 hover:bg-gray-100'
            }`}
          >
            <FiMapPin size={13} className={selectedStore ? 'text-rose-500' : 'text-gray-400'} />
            <span className="max-w-[120px] truncate font-medium">
              {selectedStore ? selectedStore.name : 'Chọn chi nhánh'}
            </span>
            <FiChevronDown
              size={13}
              className={`transition-transform ${storeDropdownOpen ? 'rotate-180' : ''}`}
            />
          </button>

          {storeDropdownOpen && (
            <div className="absolute left-0 top-full mt-2 w-64 bg-white rounded-xl shadow-lg border border-gray-100 py-1 z-50">
              <p className="px-3 py-1.5 text-[11px] font-semibold text-gray-400 uppercase tracking-wide">
                Chọn chi nhánh
              </p>
              {activeStores.length === 0 ? (
                <p className="px-3 py-3 text-sm text-gray-400 text-center">Không có chi nhánh nào.</p>
              ) : (
                activeStores.map((store) => {
                  const isSelected = selectedStore?.id === store.id;
                  return (
                    <button
                      key={store.id}
                      onClick={() => {
                        setSelectedStore(store);
                        setStoreDropdownOpen(false);
                      }}
                      className={`w-full flex items-start gap-2.5 px-3 py-2.5 text-left hover:bg-gray-50 transition-colors ${
                        isSelected ? 'bg-rose-50' : ''
                      }`}
                    >
                      <FiMapPin
                        size={14}
                        className={`mt-0.5 shrink-0 ${isSelected ? 'text-rose-500' : 'text-gray-300'}`}
                      />
                      <div className="flex-1 min-w-0">
                        <p className={`text-sm font-medium truncate ${isSelected ? 'text-rose-700' : 'text-gray-800'}`}>
                          {store.name}
                        </p>
                        {(store.street || store.address) && (
                          <p className="text-xs text-gray-400 truncate mt-0.5">
                            {store.street
                              ? `${store.street}, ${store.wardName ?? ''}, ${store.provinceName ?? ''}`
                              : store.address}
                          </p>
                        )}
                      </div>
                      {isSelected && <FiCheck size={14} className="text-rose-500 shrink-0 mt-0.5" />}
                    </button>
                  );
                })
              )}
            </div>
          )}
        </div>

        {/* Center slot (search bar, etc.) */}
        {children && <div className="flex-1 max-w-xl">{children}</div>}

        {/* Right — auth actions */}
        <div className="flex items-center gap-1 shrink-0">
          {isAuthenticated ? (
            <>
              <Link
                to="/profile"
                className="hidden sm:flex items-center gap-1.5 text-xs font-semibold text-rose-700 bg-rose-100 hover:bg-rose-200 px-2.5 py-1 rounded-full transition-colors mr-1"
                title="Trang cá nhân"
              >
                <span className="w-5 h-5 rounded-full bg-rose-500 text-white flex items-center justify-center text-[10px] font-bold">
                  {user?.firstName?.charAt(0)?.toUpperCase() ?? <FiUser size={10} />}
                </span>
                {user?.firstName}
              </Link>

              <Link to="/wishlist" className="relative p-2 rounded-full text-gray-500 hover:text-rose-600 hover:bg-rose-50 transition-colors" title="Yêu thích">
                <FiHeart size={19} />
                {wishlistCount > 0 && (
                  <span className="absolute top-0.5 right-0.5 bg-rose-500 text-white text-[9px] font-bold min-w-4 h-4 px-0.5 rounded-full flex items-center justify-center leading-none">
                    {wishlistCount > 99 ? '99+' : wishlistCount}
                  </span>
                )}
              </Link>

              <NotificationBell />

              <Link to="/cart" className="relative p-2 rounded-full text-gray-500 hover:text-rose-600 hover:bg-rose-50 transition-colors" title="Giỏ hàng">
                <FiShoppingCart size={19} />
                {cartCount > 0 && (
                  <span className="absolute top-0.5 right-0.5 bg-red-500 text-white text-[9px] font-bold min-w-4 h-4 px-0.5 rounded-full flex items-center justify-center leading-none">
                    {cartCount > 99 ? '99+' : cartCount}
                  </span>
                )}
              </Link>

              <Link to="/orders" className="relative p-2 rounded-full text-gray-500 hover:text-rose-600 hover:bg-rose-50 transition-colors" title="Đơn hàng">
                <FiPackage size={19} />
                {orderCount > 0 && (
                  <span className="absolute top-0.5 right-0.5 bg-red-500 text-white text-[9px] font-bold min-w-4 h-4 px-0.5 rounded-full flex items-center justify-center leading-none">
                    {orderCount > 99 ? '99+' : orderCount}
                  </span>
                )}
              </Link>

              {user?.role === 'Admin' && (
                <Link to="/admin" className="p-2 rounded-full text-gray-500 hover:text-rose-600 hover:bg-rose-50 transition-colors" title="Admin Panel">
                  <FiGrid size={18} />
                </Link>
              )}

              <button
                onClick={handleLogout}
                className="p-2 rounded-full text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                title="Đăng xuất"
              >
                <FiLogOut size={19} />
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-sm text-gray-500 hover:text-rose-600 px-3 py-1.5 rounded-full hover:bg-rose-50 transition-colors">Đăng nhập</Link>
              <Link to="/register" className="text-sm bg-rose-600 text-white px-4 py-1.5 rounded-full hover:bg-rose-700 transition-colors font-medium shadow-sm">
                Đăng ký
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
