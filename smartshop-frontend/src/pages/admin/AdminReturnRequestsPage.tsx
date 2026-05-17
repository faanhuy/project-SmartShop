import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { FiX } from 'react-icons/fi';
import returnRequestService from '../../services/returnRequestService';
import type { ReturnRequestDto } from '../../types/returnRequest';
import { ReturnStatus } from '../../types/returnRequest';
import {
  RETURN_REASON_LABELS,
  RETURN_STATUS_LABELS,
  RETURN_STATUS_COLORS,
} from '../../types/returnRequest';
import { formatPrice, formatDateTime } from '../../utils/formatters';
import { getApiError } from '../../utils/errorHandler';
import AdminLayout from '../../components/AdminLayout';

type FilterStatus = 'all' | ReturnStatus;

interface ActionModalState {
  type: 'approve' | 'reject' | 'view' | null;
  returnRequestId: string | null;
}

export default function AdminReturnRequestsPage() {
  const [returnRequests, setReturnRequests] = useState<ReturnRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<FilterStatus>('all');
  const [actionModal, setActionModal] = useState<ActionModalState>({
    type: null,
    returnRequestId: null,
  });
  const [adminNote, setAdminNote] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    loadData();
  }, [filter]);

  const loadData = async () => {
    setLoading(true);
    try {
      const statusParam = filter === 'all' ? undefined : (filter as ReturnStatus);
      const data = await returnRequestService.getAll(statusParam);
      setReturnRequests(data);
    } catch (err) {
      toast.error(getApiError(err, 'Không thể tải dữ liệu'));
    } finally {
      setLoading(false);
    }
  };

  const openApproveModal = (id: string) => {
    setActionModal({ type: 'approve', returnRequestId: id });
    setAdminNote('');
  };

  const openRejectModal = (id: string) => {
    setActionModal({ type: 'reject', returnRequestId: id });
    setAdminNote('');
  };

  const handleApprove = async () => {
    if (!actionModal.returnRequestId) return;
    setActionLoading(true);
    try {
      const updated = await returnRequestService.approve(
        actionModal.returnRequestId,
        { adminNote: adminNote || undefined }
      );
      setReturnRequests((prev) =>
        prev.map((r) => (r.id === updated.id ? updated : r))
      );
      toast.success('Đã duyệt yêu cầu trả hàng');
      setActionModal({ type: null, returnRequestId: null });
    } catch (err) {
      toast.error(getApiError(err, 'Duyệt yêu cầu thất bại'));
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async () => {
    if (!actionModal.returnRequestId) return;
    if (!adminNote.trim()) {
      toast.error('Vui lòng nhập lý do từ chối');
      return;
    }
    setActionLoading(true);
    try {
      const updated = await returnRequestService.reject(
        actionModal.returnRequestId,
        { adminNote }
      );
      setReturnRequests((prev) =>
        prev.map((r) => (r.id === updated.id ? updated : r))
      );
      toast.success('Đã từ chối yêu cầu trả hàng');
      setActionModal({ type: null, returnRequestId: null });
    } catch (err) {
      toast.error(getApiError(err, 'Từ chối yêu cầu thất bại'));
    } finally {
      setActionLoading(false);
    }
  };

  const closeActionModal = () => {
    if (!actionLoading) {
      setActionModal({ type: null, returnRequestId: null });
      setAdminNote('');
    }
  };

  if (loading && returnRequests.length === 0) {
    return (
      <AdminLayout title="">
        <div className="p-8 text-center text-gray-400">Đang tải...</div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout title="">
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-2xl font-bold">Yêu cầu trả hàng</h1>
        </div>

        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Lọc theo trạng thái
          </label>
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as FilterStatus)}
            className="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">Tất cả</option>
            <option value={ReturnStatus.Pending}>Chờ xử lý</option>
            <option value={ReturnStatus.Approved}>Đã duyệt</option>
            <option value={ReturnStatus.Rejected}>Bị từ chối</option>
          </select>
        </div>

        {returnRequests.length === 0 ? (
          <div className="bg-white rounded-lg p-8 text-center text-gray-500">
            <p>Không có yêu cầu trả hàng nào.</p>
          </div>
        ) : (
          <div className="bg-white rounded-lg overflow-hidden border">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b">
                  <tr>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Đơn hàng
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Email
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Lý do
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Hoàn tiền
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Trạng thái
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Ngày tạo
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-semibold text-gray-700">
                      Hành động
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {returnRequests.map((request) => (
                    <tr key={request.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm font-medium">
                        {request.orderNumber || request.orderId.slice(0, 8).toUpperCase()}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {request.userEmail || '-'}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {RETURN_REASON_LABELS[request.reason]}
                      </td>
                      <td className="px-4 py-3 text-sm font-medium text-rose-600">
                        {formatPrice(request.refundAmount)}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        <span
                          className={`px-2 py-1 rounded-full text-xs font-medium ${
                            RETURN_STATUS_COLORS[request.status]
                          }`}
                        >
                          {RETURN_STATUS_LABELS[request.status]}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-500">
                        {formatDateTime(request.createdAt)}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {request.status === ReturnStatus.Pending ? (
                          <div className="flex gap-2">
                            <button
                              onClick={() => openApproveModal(request.id)}
                              className="px-3 py-1 bg-green-100 text-green-700 rounded hover:bg-green-200 text-xs font-medium"
                            >
                              Duyệt
                            </button>
                            <button
                              onClick={() => openRejectModal(request.id)}
                              className="px-3 py-1 bg-red-100 text-red-700 rounded hover:bg-red-200 text-xs font-medium"
                            >
                              Từ chối
                            </button>
                          </div>
                        ) : request.adminNote ? (
                          <button
                            onClick={() => {
                              setAdminNote(request.adminNote || '');
                              setActionModal({ type: 'view', returnRequestId: request.id });
                            }}
                            className="text-xs text-blue-600 hover:underline"
                          >
                            Xem ghi chú
                          </button>
                        ) : (
                          <span className="text-xs text-gray-400">-</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>

      {/* View Note Modal */}
      {actionModal.type === 'view' && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-md w-full">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-lg font-bold">Ghi chú admin</h2>
              <button onClick={closeActionModal} className="text-gray-400 hover:text-gray-600">
                <FiX size={20} />
              </button>
            </div>
            <div className="p-6">
              <p className="text-sm text-gray-700 whitespace-pre-wrap">{adminNote}</p>
              <button
                onClick={closeActionModal}
                className="mt-4 w-full px-4 py-2 bg-gray-100 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-200"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Action Modal */}
      {(actionModal.type === 'approve' || actionModal.type === 'reject') && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-md w-full">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-lg font-bold">
                {actionModal.type === 'approve'
                  ? 'Duyệt yêu cầu trả hàng'
                  : 'Từ chối yêu cầu trả hàng'}
              </h2>
              <button
                onClick={closeActionModal}
                disabled={actionLoading}
                className="text-gray-400 hover:text-gray-600"
              >
                <FiX size={20} />
              </button>
            </div>

            <form
              onSubmit={(e) => {
                e.preventDefault();
                if (actionModal.type === 'approve') {
                  handleApprove();
                } else {
                  handleReject();
                }
              }}
              className="p-6 space-y-4"
            >
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  {actionModal.type === 'approve'
                    ? 'Ghi chú (tùy chọn)'
                    : 'Lý do từ chối *'}
                </label>
                <textarea
                  value={adminNote}
                  onChange={(e) => setAdminNote(e.target.value)}
                  placeholder={
                    actionModal.type === 'approve'
                      ? 'Nhập ghi chú...'
                      : 'Nhập lý do từ chối...'
                  }
                  required={actionModal.type === 'reject'}
                  className="w-full border rounded-lg px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-blue-500"
                  rows={3}
                />
              </div>

              <div className="flex gap-2 pt-2">
                <button
                  type="button"
                  onClick={closeActionModal}
                  disabled={actionLoading}
                  className="flex-1 px-4 py-2 border rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={
                    actionLoading ||
                    (actionModal.type === 'reject' && !adminNote.trim())
                  }
                  className={`flex-1 px-4 py-2 rounded-lg text-sm font-medium text-white ${
                    actionModal.type === 'approve'
                      ? 'bg-green-600 hover:bg-green-700 disabled:opacity-60'
                      : 'bg-red-600 hover:bg-red-700 disabled:opacity-60'
                  }`}
                >
                  {actionLoading
                    ? 'Đang xử lý...'
                    : actionModal.type === 'approve'
                      ? 'Duyệt'
                      : 'Từ chối'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
