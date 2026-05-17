import { useState } from 'react';
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import {
  FiGrid, FiPackage, FiShoppingBag, FiTag, FiLogOut, FiMenu,
  FiExternalLink, FiMapPin, FiArchive, FiPercent, FiSliders,
  FiLayers, FiRotateCcw, FiChevronDown, FiChevronRight,
} from 'react-icons/fi';
import { useAuthStore } from '../store/authStore';

interface AdminLayoutProps {
  children: React.ReactNode;
  title: string;
}

interface NavChild {
  to: string;
  label: string;
  icon: React.ElementType;
}

interface NavGroup {
  label: string;
  icon: React.ElementType;
  children: NavChild[];
}

interface NavSingle {
  to: string;
  label: string;
  icon: React.ElementType;
  end?: boolean;
}

type NavItem = NavSingle | NavGroup;

const isGroup = (item: NavItem): item is NavGroup => 'children' in item;

const NAV_ITEMS: NavItem[] = [
  { to: '/admin', label: 'Tổng quan', icon: FiGrid, end: true },
  {
    label: 'Sản phẩm',
    icon: FiPackage,
    children: [
      { to: '/admin/products',          label: 'Món ăn',          icon: FiPackage  },
      { to: '/admin/sizes',             label: 'Kích cỡ',         icon: FiSliders  },
      { to: '/admin/combos',            label: 'Combo',           icon: FiLayers   },
      { to: '/admin/promotional-prices',label: 'Giá khuyến mãi',  icon: FiPercent  },
    ],
  },
  {
    label: 'Vận hành',
    icon: FiShoppingBag,
    children: [
      { to: '/admin/orders',          label: 'Đơn giao',  icon: FiShoppingBag },
      { to: '/admin/return-requests', label: 'Trả hàng',  icon: FiRotateCcw   },
    ],
  },
  {
    label: 'Kinh doanh',
    icon: FiMapPin,
    children: [
      { to: '/admin/stores',    label: 'Chi nhánh',      icon: FiMapPin   },
      { to: '/admin/inventory', label: 'Tồn kho',        icon: FiArchive  },
      { to: '/admin/coupons',   label: 'Mã giảm giá',   icon: FiTag      },
    ],
  },
];

function NavGroupItem({ group, onClose }: { group: NavGroup; onClose?: () => void }) {
  const location = useLocation();
  const isAnyChildActive = group.children.some(c => location.pathname.startsWith(c.to));
  const [open, setOpen] = useState(isAnyChildActive);

  return (
    <div>
      <button
        onClick={() => setOpen(v => !v)}
        className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
          isAnyChildActive
            ? 'text-white bg-gray-800'
            : 'text-gray-400 hover:bg-gray-800 hover:text-white'
        }`}
      >
        <group.icon size={16} />
        <span className="flex-1 text-left">{group.label}</span>
        {open ? <FiChevronDown size={13} /> : <FiChevronRight size={13} />}
      </button>

      {open && (
        <div className="mt-0.5 ml-3 pl-3 border-l border-gray-700 space-y-0.5">
          {group.children.map(child => (
            <NavLink
              key={child.to}
              to={child.to}
              onClick={onClose}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                  isActive
                    ? 'bg-rose-600 text-white font-medium'
                    : 'text-gray-400 hover:bg-gray-800 hover:text-white'
                }`
              }
            >
              <child.icon size={14} />
              {child.label}
            </NavLink>
          ))}
        </div>
      )}
    </div>
  );
}

function SidebarContent({ onClose }: { onClose?: () => void }) {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();

  const handleLogout = () => { logout(); navigate('/login'); };

  return (
    <div className="flex flex-col h-full bg-gray-900 text-white">
      <div className="px-5 py-4 border-b border-gray-700/60">
        <p className="text-lg font-bold text-white">FastFood</p>
        <span className="inline-block mt-1 text-[10px] font-semibold tracking-widest uppercase bg-rose-600 text-white px-2 py-0.5 rounded">
          Admin
        </span>
      </div>

      <nav className="flex-1 py-3 px-2 space-y-0.5 overflow-y-auto">
        {NAV_ITEMS.map((item, i) =>
          isGroup(item) ? (
            <NavGroupItem key={i} group={item} onClose={onClose} />
          ) : (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              onClick={onClose}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-rose-600 text-white'
                    : 'text-gray-400 hover:bg-gray-800 hover:text-white'
                }`
              }
            >
              <item.icon size={16} />
              {item.label}
            </NavLink>
          )
        )}
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
      <aside className="hidden md:flex flex-col w-56 shrink-0">
        <SidebarContent />
      </aside>

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
