import { Link } from 'react-router-dom';
import { FiShoppingBag, FiMail, FiPhone, FiMapPin, FiGithub } from 'react-icons/fi';

export default function Footer() {
  return (
    <footer className="bg-[#2A1F1A] text-gray-300 mt-auto">
      <div className="max-w-7xl mx-auto px-4 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">

          {/* Brand */}
          <div className="md:col-span-1">
            <div className="flex items-center gap-2 mb-3">
              <FiShoppingBag className="text-orange-400" size={22} />
              <span className="text-white text-xl font-bold">FastFood</span>
            </div>
            <p className="text-sm text-gray-400 leading-relaxed">
              Web đặt đồ ăn nhanh với AI gợi ý món, combo bán chạy và quy trình đặt hàng gọn nhẹ.
            </p>
          </div>

          {/* Shop */}
          <div>
            <h4 className="text-white font-semibold mb-4 text-sm uppercase tracking-wider">Thực đơn</h4>
            <ul className="space-y-2.5 text-sm">
              <li><Link to="/products" className="hover:text-orange-300 transition-colors">Toàn bộ món</Link></li>
              <li><Link to="/products?sort=newest" className="hover:text-orange-300 transition-colors">Combo mới lên sóng</Link></li>
              <li><Link to="/cart" className="hover:text-orange-300 transition-colors">Giỏ món</Link></li>
              <li><Link to="/checkout" className="hover:text-orange-300 transition-colors">Xác nhận đơn</Link></li>
            </ul>
          </div>

          {/* Account */}
          <div>
            <h4 className="text-white font-semibold mb-4 text-sm uppercase tracking-wider">Tài khoản</h4>
            <ul className="space-y-2.5 text-sm">
              <li><Link to="/profile" className="hover:text-orange-300 transition-colors">Hồ sơ giao nhận</Link></li>
              <li><Link to="/orders" className="hover:text-orange-300 transition-colors">Đơn giao của tôi</Link></li>
              <li><Link to="/login" className="hover:text-orange-300 transition-colors">Đăng nhập</Link></li>
              <li><Link to="/register" className="hover:text-orange-300 transition-colors">Đăng ký</Link></li>
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h4 className="text-white font-semibold mb-4 text-sm uppercase tracking-wider">Liên hệ</h4>
            <ul className="space-y-2.5 text-sm">
              <li className="flex items-center gap-2">
                <FiMail size={14} className="text-orange-300 shrink-0" />
                <span>huyp18062000@gmail.com</span>
              </li>
              <li className="flex items-center gap-2">
                <FiPhone size={14} className="text-orange-300 shrink-0" />
                <span>0355 609 145</span>
              </li>
              <li className="flex items-start gap-2">
                <FiMapPin size={14} className="text-orange-300 shrink-0 mt-0.5" />
                <span>TP. Hồ Chí Minh, giao nội thành mỗi ngày</span>
              </li>
              <li className="flex items-center gap-2 mt-1">
                <FiGithub size={14} className="text-orange-300 shrink-0" />
                <a
                  href="https://github.com/faanhuy/project-SmartShop"
                  target="_blank"
                  rel="noreferrer"
                  className="hover:text-orange-300 transition-colors"
                >
                  GitHub
                </a>
              </li>
            </ul>
          </div>
        </div>

        {/* Divider + Bottom bar */}
        <div className="border-t border-gray-700 mt-10 pt-6 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-gray-500">
          <span>© {new Date().getFullYear()} FastFood. Giao đồ ăn nhanh mỗi ngày.</span>
          <div className="flex items-center gap-1.5">
            <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
            <span>Powered by .NET 8 + React 19 + HuyPD</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
