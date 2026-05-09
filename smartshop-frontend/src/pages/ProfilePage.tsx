import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import {
  FiUser, FiMail, FiCalendar, FiSave,
  FiMapPin, FiPlus, FiEdit2, FiTrash2, FiCheck,
} from 'react-icons/fi';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import { userService, type UserProfileDto } from '../services/userService';
import { addressService } from '../services/addressService';
import { useAuthStore } from '../store/authStore';
import type { AddressDto } from '../types/order';
import { formatDate } from '@/utils/formatters';
import { AddressSelector, type AddressSelection } from '@/components/AddressSelector';

type Tab = 'profile' | 'addresses';

// ---- Address Form Modal ----
interface AddressFormData {
  label: string;
  recipientName: string;
  phone: string;
  street: string;
  ward: string;
  district: string;
  city: string;
  provinceId?: number;
  wardId?: number;
  provinceName?: string;
  wardName?: string;
}

const EMPTY_FORM: AddressFormData = {
  label: '',
  recipientName: '',
  phone: '',
  street: '',
  ward: '',
  district: '',
  city: '',
};

interface AddressModalProps {
  initial?: AddressDto | null;
  onClose: () => void;
  onSaved: () => void;
}

function AddressModal({ initial, onClose, onSaved }: AddressModalProps) {
  const [form, setForm] = useState<AddressFormData>(
    initial
      ? {
          label: initial.label ?? '',
          recipientName: initial.recipientName,
          phone: initial.phone,
          street: initial.street,
          ward: initial.wardName ?? initial.ward ?? '',
          district: initial.district,
          city: initial.provinceName ?? initial.city,
          provinceId: initial.provinceId,
          wardId: initial.wardId,
          provinceName: initial.provinceName,
          wardName: initial.wardName,
        }
      : EMPTY_FORM
  );
  const [saving, setSaving] = useState(false);

  const set = (field: keyof AddressFormData) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [field]: e.target.value }));

  const handleAddressSelection = (selection: AddressSelection) => {
    setForm((f) => ({
      ...f,
      street: selection.street,
      ward: selection.wardName,
      city: selection.provinceName,
      provinceId: selection.provinceId,
      wardId: selection.wardId,
      provinceName: selection.provinceName,
      wardName: selection.wardName,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.recipientName.trim() || !form.phone.trim()) {
      toast.error('Vui lòng điền đầy đủ họ tên người nhận và số điện thoại.');
      return;
    }
    if (!form.provinceId || !form.wardId || !form.street.trim()) {
      toast.error('Vui lòng chọn tỉnh/thành phố, phường/xã và nhập số nhà, đường.');
      return;
    }
    setSaving(true);
    try {
      const payload = {
        label: form.label.trim(),
        recipientName: form.recipientName.trim(),
        phone: form.phone.trim(),
        street: form.street.trim(),
        ward: form.wardName ?? (form.ward.trim() || undefined),
        district: form.district.trim() || '',
        city: form.provinceName ?? form.city.trim(),
        provinceId: form.provinceId,
        wardId: form.wardId,
        provinceName: form.provinceName,
        wardName: form.wardName,
      };
      if (initial) {
        await addressService.update(initial.id, payload);
        toast.success('Đã cập nhật địa chỉ.');
      } else {
        await addressService.add(payload);
        toast.success('Đã thêm địa chỉ mới.');
      }
      onSaved();
      onClose();
    } catch (err: any) {
      toast.error(err.response?.data?.message ?? 'Lưu địa chỉ thất bại.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6 max-h-[90vh] overflow-y-auto">
        <h3 className="text-base font-semibold text-gray-800 mb-4">
          {initial ? 'Sửa địa chỉ' : 'Thêm địa chỉ mới'}
        </h3>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Nhãn (tuỳ chọn)</label>
            <input
              value={form.label}
              onChange={set('label')}
              placeholder="Nhà, Công ty..."
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Người nhận <span className="text-red-500">*</span>
              </label>
              <input
                required
                value={form.recipientName}
                onChange={set('recipientName')}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Điện thoại <span className="text-red-500">*</span>
              </label>
              <input
                required
                value={form.phone}
                onChange={set('phone')}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
              />
            </div>
          </div>

          <div className="border border-gray-200 rounded-lg p-3">
            <p className="text-sm font-medium text-gray-700 mb-2">
              Địa chỉ <span className="text-red-500">*</span>
            </p>
            <AddressSelector
              value={{
                provinceId: form.provinceId,
                wardId: form.wardId,
                street: form.street,
              }}
              onChange={handleAddressSelection}
            />
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 border border-gray-300 text-gray-700 py-2 rounded-lg text-sm hover:bg-gray-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={saving}
              className="flex-1 bg-rose-600 text-white py-2 rounded-lg text-sm font-medium hover:bg-rose-700 disabled:opacity-50"
            >
              {saving ? 'Đang lưu...' : 'Lưu địa chỉ'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ---- Main Page ----
export default function ProfilePage() {
  const { updateUser } = useAuthStore();
  const [activeTab, setActiveTab] = useState<Tab>('profile');

  // Profile state
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');

  // Address state
  const [addresses, setAddresses] = useState<AddressDto[]>([]);
  const [addrLoading, setAddrLoading] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [editTarget, setEditTarget] = useState<AddressDto | null>(null);

  useEffect(() => {
    userService.getMyProfile()
      .then((p) => {
        setProfile(p);
        setFirstName(p.firstName);
        setLastName(p.lastName);
      })
      .catch(() => toast.error('Không thể tải thông tin profile.'))
      .finally(() => setLoading(false));
  }, []);

  const fetchAddresses = () => {
    setAddrLoading(true);
    addressService.getAll()
      .then(setAddresses)
      .catch(() => toast.error('Không thể tải danh sách địa chỉ.'))
      .finally(() => setAddrLoading(false));
  };

  useEffect(() => {
    if (activeTab === 'addresses') fetchAddresses();
  }, [activeTab]);

  const handleProfileSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!firstName.trim() || !lastName.trim()) return;
    setSaving(true);
    try {
      const updated = await userService.updateMyProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
      });
      setProfile(updated);
      updateUser({ firstName: updated.firstName, lastName: updated.lastName });
      toast.success('Đã cập nhật thông tin.');
    } catch {
      toast.error('Cập nhật thất bại.');
    } finally {
      setSaving(false);
    }
  };

  const handleSetDefault = async (id: string) => {
    try {
      await addressService.setDefault(id);
      toast.success('Đã đặt làm địa chỉ mặc định.');
      fetchAddresses();
    } catch (err: any) {
      toast.error(err.response?.data?.message ?? 'Có lỗi xảy ra.');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Xác nhận xóa địa chỉ này?')) return;
    try {
      await addressService.remove(id);
      toast.success('Đã xóa địa chỉ.');
      fetchAddresses();
    } catch (err: any) {
      toast.error(err.response?.data?.message ?? 'Xóa thất bại.');
    }
  };

  if (loading) return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="p-8 text-center text-gray-400">Đang tải...</div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-2xl mx-auto px-4 py-8">
        <h1 className="text-2xl font-bold mb-6">Tài khoản của tôi</h1>

        {/* Tabs */}
        <div className="flex gap-1 bg-gray-100 rounded-xl p-1 mb-6">
          <button
            onClick={() => setActiveTab('profile')}
            className={`flex-1 py-2 text-sm font-medium rounded-lg transition-colors ${
              activeTab === 'profile'
                ? 'bg-white text-rose-600 shadow-sm'
                : 'text-gray-600 hover:text-gray-800'
            }`}
          >
            Thông tin cá nhân
          </button>
          <button
            onClick={() => setActiveTab('addresses')}
            className={`flex-1 py-2 text-sm font-medium rounded-lg transition-colors flex items-center justify-center gap-1.5 ${
              activeTab === 'addresses'
                ? 'bg-white text-rose-600 shadow-sm'
                : 'text-gray-600 hover:text-gray-800'
            }`}
          >
            <FiMapPin size={14} />
            Địa chỉ giao hàng
          </button>
        </div>

        {/* Tab: Profile */}
        {activeTab === 'profile' && (
          <>
            <div className="bg-white rounded-2xl shadow-sm p-6 mb-6 space-y-3">
              <div className="flex items-center gap-3 text-sm text-gray-600">
                <FiMail size={16} className="text-gray-400 shrink-0" />
                <span>{profile?.email}</span>
              </div>
              <div className="flex items-center gap-3 text-sm text-gray-600">
                <FiUser size={16} className="text-gray-400 shrink-0" />
                <span className="capitalize">{profile?.role === 'Admin' ? 'Quản trị viên' : 'Khách hàng'}</span>
              </div>
              <div className="flex items-center gap-3 text-sm text-gray-600">
                <FiCalendar size={16} className="text-gray-400 shrink-0" />
                <span>Tham gia {profile ? formatDate(profile.createdAt) : '—'}</span>
              </div>
            </div>

            <div className="bg-white rounded-2xl shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-800 mb-4">Chỉnh sửa thông tin</h2>
              <form onSubmit={handleProfileSubmit} className="space-y-4">
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Họ</label>
                    <input
                      required
                      value={lastName}
                      onChange={(e) => setLastName(e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Tên</label>
                    <input
                      required
                      value={firstName}
                      onChange={(e) => setFirstName(e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400"
                    />
                  </div>
                </div>
                <button
                  type="submit"
                  disabled={saving}
                  className="w-full bg-rose-600 text-white py-2 rounded-lg text-sm font-medium hover:bg-rose-700 disabled:opacity-50 transition-colors flex items-center justify-center gap-2"
                >
                  <FiSave size={15} />
                  {saving ? 'Đang lưu...' : 'Lưu thay đổi'}
                </button>
              </form>
            </div>
          </>
        )}

        {/* Tab: Addresses */}
        {activeTab === 'addresses' && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-semibold text-gray-800">Danh sách địa chỉ</h2>
              <button
                onClick={() => { setEditTarget(null); setShowModal(true); }}
                className="flex items-center gap-1.5 bg-rose-600 text-white px-3 py-1.5 rounded-lg text-sm font-medium hover:bg-rose-700 transition-colors"
              >
                <FiPlus size={14} />
                Thêm địa chỉ
              </button>
            </div>

            {addrLoading ? (
              <p className="text-sm text-gray-400 text-center py-8">Đang tải...</p>
            ) : addresses.length === 0 ? (
              <div className="bg-white rounded-2xl shadow-sm p-8 text-center">
                <FiMapPin size={32} className="mx-auto text-gray-300 mb-3" />
                <p className="text-sm text-gray-500">Bạn chưa có địa chỉ nào.</p>
                <p className="text-xs text-gray-400 mt-1">Thêm địa chỉ để thanh toán nhanh hơn.</p>
              </div>
            ) : (
              addresses.map((addr) => (
                <div
                  key={addr.id}
                  className={`bg-white rounded-2xl shadow-sm p-4 border-2 transition-colors ${
                    addr.isDefault ? 'border-rose-300' : 'border-transparent'
                  }`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap mb-1">
                        {addr.label && (
                          <span className="text-xs font-semibold text-gray-700 bg-gray-100 px-2 py-0.5 rounded-full">
                            {addr.label}
                          </span>
                        )}
                        {addr.isDefault && (
                          <span className="text-xs font-semibold text-rose-600 bg-rose-50 px-2 py-0.5 rounded-full flex items-center gap-1">
                            <FiCheck size={10} />
                            Mặc định
                          </span>
                        )}
                      </div>
                      <p className="text-sm font-medium text-gray-800">{addr.recipientName}</p>
                      <p className="text-sm text-gray-500">{addr.phone}</p>
                      <p className="text-sm text-gray-500 mt-0.5">
                        {addr.street && addr.wardName && addr.provinceName
                          ? `${addr.street}, ${addr.wardName}, ${addr.provinceName}`
                          : [addr.street, addr.ward, addr.district, addr.city].filter(Boolean).join(', ')}
                      </p>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      {!addr.isDefault && (
                        <button
                          onClick={() => handleSetDefault(addr.id)}
                          title="Đặt mặc định"
                          className="p-1.5 text-gray-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors text-xs"
                        >
                          <FiCheck size={15} />
                        </button>
                      )}
                      <button
                        onClick={() => { setEditTarget(addr); setShowModal(true); }}
                        title="Sửa"
                        className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                      >
                        <FiEdit2 size={15} />
                      </button>
                      <button
                        onClick={() => handleDelete(addr.id)}
                        title="Xóa"
                        className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                      >
                        <FiTrash2 size={15} />
                      </button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </div>
      <Footer />

      {showModal && (
        <AddressModal
          initial={editTarget}
          onClose={() => setShowModal(false)}
          onSaved={fetchAddresses}
        />
      )}
    </div>
  );
}
