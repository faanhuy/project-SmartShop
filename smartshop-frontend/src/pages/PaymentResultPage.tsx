import { useSearchParams, Link } from 'react-router-dom';
import { FiCheckCircle, FiXCircle, FiShoppingBag, FiRefreshCw } from 'react-icons/fi';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';

export default function PaymentResultPage() {
  const [params] = useSearchParams();

  const successParam = params.get('success');
  const orderId = params.get('orderId') ?? params.get('vnp_TxnRef');
  const transactionNo = params.get('vnp_TransactionNo');
  const isSuccess = successParam?.toLowerCase() === 'true';

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-lg mx-auto px-4 py-16 text-center">
        {isSuccess ? (
          <>
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center">
                <FiCheckCircle size={40} className="text-green-500" />
              </div>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Thanh toán thành công!</h1>
            <p className="text-gray-500 mb-1 text-sm">Đơn hàng của bạn đã được đặt và thanh toán thành công.</p>
            {transactionNo && (
              <p className="text-xs text-gray-400 mb-6">
                Mã giao dịch VNPay: <span className="font-medium text-gray-600">{transactionNo}</span>
              </p>
            )}
            <div className="bg-white rounded-2xl shadow-sm p-6 mb-6 text-left space-y-2">
              {orderId && (
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Mã đơn hàng</span>
                  <span className="font-medium text-gray-800 font-mono text-xs">{orderId}</span>
                </div>
              )}
              <div className="flex justify-between text-sm">
                <span className="text-gray-500">Trạng thái</span>
                <span className="font-medium text-green-600">Đã thanh toán</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-500">Phương thức</span>
                <span className="font-medium text-gray-800">VNPay</span>
              </div>
            </div>
            <div className="flex gap-3 justify-center">
              {orderId && (
                <Link
                  to={`/orders/${orderId}`}
                  className="flex items-center gap-2 bg-rose-600 text-white px-5 py-2.5 rounded-xl font-medium text-sm hover:bg-rose-700 transition-colors"
                >
                  <FiShoppingBag size={16} />
                  Xem đơn hàng
                </Link>
              )}
              <Link
                to="/orders"
                className="flex items-center gap-2 border border-gray-300 text-gray-700 px-5 py-2.5 rounded-xl font-medium text-sm hover:bg-gray-50 transition-colors"
              >
                Lịch sử đơn hàng
              </Link>
            </div>
          </>
        ) : (
          <>
            <div className="flex justify-center mb-6">
              <div className="w-20 h-20 bg-red-100 rounded-full flex items-center justify-center">
                <FiXCircle size={40} className="text-red-500" />
              </div>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Thanh toán thất bại</h1>
            <p className="text-gray-500 mb-1 text-sm">Giao dịch không thành công. Vui lòng thử lại.</p>
            {orderId && (
              <p className="text-xs text-gray-400 mb-6">
                Mã đơn: <span className="font-medium text-gray-600 font-mono">{orderId}</span>
              </p>
            )}
            <div className="bg-white rounded-2xl shadow-sm p-6 mb-6 text-left">
              <p className="text-sm text-gray-600">
                Đơn hàng của bạn có thể đã được tạo nhưng chưa được thanh toán.
                Bạn có thể kiểm tra lại trong{' '}
                <Link to="/orders" className="text-rose-600 hover:underline">lịch sử đơn hàng</Link>{' '}
                và thử thanh toán lại hoặc chọn thanh toán COD khi liên hệ cửa hàng.
              </p>
            </div>
            <div className="flex gap-3 justify-center">
              <Link
                to="/checkout"
                className="flex items-center gap-2 bg-rose-600 text-white px-5 py-2.5 rounded-xl font-medium text-sm hover:bg-rose-700 transition-colors"
              >
                <FiRefreshCw size={16} />
                Thử lại
              </Link>
              <Link
                to="/orders"
                className="flex items-center gap-2 border border-gray-300 text-gray-700 px-5 py-2.5 rounded-xl font-medium text-sm hover:bg-gray-50 transition-colors"
              >
                Lịch sử đơn hàng
              </Link>
            </div>
          </>
        )}
      </div>
      <Footer />
    </div>
  );
}
