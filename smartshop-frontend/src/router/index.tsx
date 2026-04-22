import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import ProductListPage from '../pages/ProductListPage';
import ProductDetailPage from '../pages/ProductDetailPage';
import AdminDashboardPage from '../pages/AdminDashboardPage';
import AdminProductPage from '../pages/AdminProductPage';
import AdminOrderPage from '../pages/AdminOrderPage';
import AdminCouponsPage from '../pages/AdminCouponsPage';
import CartPage from '../pages/CartPage';
import CheckoutPage from '../pages/CheckoutPage';
import OrderHistoryPage from '../pages/OrderHistoryPage';
import OrderDetailPage from '../pages/OrderDetailPage';
import ProfilePage from '../pages/ProfilePage';
import { useAuthStore } from '../store/authStore';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

function AdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user } = useAuthStore();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (user?.role !== 'Admin') return <Navigate to="/products" replace />;
  return <>{children}</>;
}

const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/products', element: <ProductListPage /> },
  { path: '/products/:slug', element: <ProductDetailPage /> },
  {
    path: '/cart',
    element: (
      <PrivateRoute>
        <CartPage />
      </PrivateRoute>
    ),
  },
  {
    path: '/checkout',
    element: (
      <PrivateRoute>
        <CheckoutPage />
      </PrivateRoute>
    ),
  },
  {
    path: '/orders',
    element: (
      <PrivateRoute>
        <OrderHistoryPage />
      </PrivateRoute>
    ),
  },
  {
    path: '/orders/:id',
    element: (
      <PrivateRoute>
        <OrderDetailPage />
      </PrivateRoute>
    ),
  },
  {
    path: '/admin',
    element: (
      <AdminRoute>
        <AdminDashboardPage />
      </AdminRoute>
    ),
  },
  {
    path: '/admin/products',
    element: (
      <AdminRoute>
        <AdminProductPage />
      </AdminRoute>
    ),
  },
  {
    path: '/admin/orders',
    element: (
      <AdminRoute>
        <AdminOrderPage />
      </AdminRoute>
    ),
  },
  {
    path: '/admin/coupons',
    element: (
      <AdminRoute>
        <AdminCouponsPage />
      </AdminRoute>
    ),
  },
  {
    path: '/profile',
    element: (
      <PrivateRoute>
        <ProfilePage />
      </PrivateRoute>
    ),
  },
  { path: '/', element: <Navigate to="/products" replace /> },
  { path: '*', element: <Navigate to="/products" replace /> },
]);

export default router;
