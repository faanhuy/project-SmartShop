import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FiShoppingCart, FiPackage, FiLogOut, FiGrid, FiUser, FiHeart } from 'react-icons/fi';
import { useAuthStore } from '../store/authStore';
import { cartService } from '../services/cartService';
import { orderService } from '../services/orderService';
import { wishlistService } from '../services/wishlistService';
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

  useEffect(() => {
    if (!isAuthenticated) {
      setCartCount(0);
      setOrderCount(0);
      return;
    }
    cartService.getCart()
      .then((cart) => setCartCount(cart.items.reduce((s, i) => s + i.quantity, 0)))
      .catch(() => setCartCount(0));
  }, [isAuthenticated, cartVersion]); // re-fetch khi cartVersion thay đổi

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
          {/* H for Huy — two soft rounded legs, crossbar = mini burger */}
          <svg width="40" height="40" viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <linearGradient id="hLogoGrad" x1="0" y1="0" x2="40" y2="40" gradientUnits="userSpaceOnUse">
                <stop offset="0%" stopColor="#fb7185" />
                <stop offset="100%" stopColor="#f97316" />
              </linearGradient>
            </defs>
            {/* Background */}
            <rect width="40" height="40" rx="14" fill="url(#hLogoGrad)" />

            {/* H — left leg */}
            <line x1="11" y1="9" x2="11" y2="31" stroke="white" strokeWidth="5" strokeLinecap="round" strokeOpacity="0.95" />
            {/* H — right leg */}
            <line x1="29" y1="9" x2="29" y2="31" stroke="white" strokeWidth="5" strokeLinecap="round" strokeOpacity="0.95" />

            {/* Crossbar = mini burger */}
            {/* top bun — soft dome arc */}
            <path d="M13.5 20 Q20 13.5 26.5 20" stroke="white" strokeWidth="3" strokeLinecap="round" fill="none" strokeOpacity="0.95" />
            {/* lettuce — wavy green */}
            <path d="M13 21.5 Q16 20 19.5 21.5 Q23 20 26.5 21.5" stroke="#86efac" strokeWidth="1.5" strokeLinecap="round" fill="none" opacity="0.85" />
            {/* patty */}
            <line x1="13" y1="23.5" x2="27" y2="23.5" stroke="#fcd34d" strokeWidth="3" strokeLinecap="round" />
            {/* bottom bun */}
            <line x1="13.5" y1="26" x2="26.5" y2="26" stroke="white" strokeWidth="2.5" strokeLinecap="round" strokeOpacity="0.9" />
          </svg>
          <div className="flex flex-col">
            <span className="text-lg font-extrabold text-rose-600 leading-tight tracking-tight">FastFood</span>
            <span className="hidden sm:block text-[10px] text-gray-400 leading-none">Đồ ăn nhanh — giao tận nơi</span>
          </div>
        </Link>

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
