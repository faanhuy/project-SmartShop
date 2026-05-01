import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FiShoppingCart, FiPackage, FiLogOut, FiGrid, FiUser } from 'react-icons/fi';
import { useAuthStore } from '../store/authStore';
import { cartService } from '../services/cartService';
import { orderService } from '../services/orderService';

interface NavbarProps {
  children?: React.ReactNode; // slot cho search bar (ProductListPage)
}

export default function Navbar({ children }: NavbarProps) {
  const navigate = useNavigate();
  const { isAuthenticated, user, logout, cartVersion } = useAuthStore();
  const [cartCount, setCartCount] = useState(0);
  const [orderCount, setOrderCount] = useState(0);

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

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="bg-white shadow-sm sticky top-0 z-10">
      <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between gap-4">
        {/* Logo */}
        <Link to="/products" className="shrink-0">
          <span className="block text-xl font-bold text-orange-500">FastFood</span>
          <span className="hidden sm:block text-[11px] text-gray-400">Giao mon nhanh trong ngay</span>
        </Link>

        {/* Center slot (search bar, etc.) */}
        {children && <div className="flex-1 max-w-xl">{children}</div>}

        {/* Right — auth actions */}
        <div className="flex items-center gap-3 shrink-0">
          {isAuthenticated ? (
            <>
              <Link to="/profile" className="hidden sm:flex items-center gap-1.5 text-sm text-gray-600 hover:text-blue-600" title="Trang cá nhân">
                <FiUser size={14} />
                <strong>{user?.firstName}</strong>
              </Link>

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
                <Link to="/admin" className="text-gray-500 hover:text-blue-600" title="Admin Panel">
                  <FiGrid size={18} />
                </Link>
              )}

              <button onClick={handleLogout} className="text-red-500 hover:text-red-700" title="Đăng xuất">
                <FiLogOut size={20} />
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-sm text-gray-500 hover:text-blue-600">Đăng nhập</Link>
              <Link to="/register" className="text-sm bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700">
                Đăng ký
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
