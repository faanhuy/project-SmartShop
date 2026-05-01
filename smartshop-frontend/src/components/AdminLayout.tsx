import { useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { FiGrid, FiPackage, FiShoppingBag, FiTag, FiLogOut, FiMenu, FiExternalLink } from 'react-icons/fi';
import { useAuthStore } from '../store/authStore';

interface AdminLayoutProps {
  children: React.ReactNode;
  title: string;
}

const NAV_ITEMS = [
  { to: '/admin',          label: 'Tổng quan',  icon: FiGrid,        end: true  },
  { to: '/admin/products', label: 'Món ăn',     icon: FiPackage,     end: false },
  { to: '/admin/orders',   label: 'Đơn giao',   icon: FiShoppingBag, end: false },
  { to: '/admin/coupons',  label: 'Mã giảm giá', icon: FiTag,        end: false },
];

function SidebarContent({ onClose }: { onClose?: () => void }) {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();

  const handleLogout = () => { logout(); navigate('/login'); };

  return (
    <div className="flex flex-col h-full bg-gray-900 text-white">
      <div className="px-5 py-4 border-b border-gray-700/60">
        <p className="text-lg font-bold text-white">FastFood</p>
        <span className="inline-block mt-1 text-[10px] font-semibold tracking-widest uppercase bg-blue-600 text-white px-2 py-0.5 rounded">
          Admin
        </span>
      </div>

      <nav className="flex-1 py-3 px-2 space-y-0.5">
        {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            onClick={onClose}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-400 hover:bg-gray-800 hover:text-white'
              }`
            }
          >
            <Icon size={16} />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="px-4 py-4 border-t border-gray-700/60 space-y-3">
        <Link
          to="/products"
          className="flex items-center gap-2 text-xs text-gray-400 hover:text-white"
        >
          <FiExternalLink size={13} /> Về trang đặt món
        </Link>
        <div className="flex items-center justify-between">
          <div className="min-w-0">
            <p className="text-[11px] text-gray-500">Đăng nhập với</p>
            <p className="text-xs font-medium text-gray-300 truncate">{user?.email}</p>
          </div>
          <button
            onClick={handleLogout}
            title="Đăng xuất"
            className="text-red-400 hover:text-red-300 ml-2 shrink-0"
          >
            <FiLogOut size={16} />
          </button>
        </div>
      </div>
    </div>
  );
}

export default function AdminLayout({ children, title }: AdminLayoutProps) {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <div className="flex h-screen bg-gray-50 overflow-hidden">
      {/* Desktop sidebar */}
      <aside className="hidden md:flex flex-col w-56 shrink-0">
        <SidebarContent />
      </aside>

      {/* Mobile sidebar overlay */}
      {mobileOpen && (
        <div className="fixed inset-0 z-40 md:hidden">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setMobileOpen(false)}
          />
          <aside className="absolute left-0 top-0 h-full w-56 z-50">
            <SidebarContent onClose={() => setMobileOpen(false)} />
          </aside>
        </div>
      )}

      {/* Main content area */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <header className="bg-white border-b px-4 py-3 flex items-center gap-3 shrink-0">
          <button
            className="md:hidden text-gray-500 hover:text-gray-800"
            onClick={() => setMobileOpen(true)}
          >
            <FiMenu size={20} />
          </button>
          <h1 className="text-base font-semibold text-gray-800">{title}</h1>
        </header>

        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          {children}
        </main>
      </div>
    </div>
  );
}
