import { useEffect, useState, useCallback } from 'react';
import toast from 'react-hot-toast';
import { FiAlertTriangle, FiEdit2, FiCheck, FiX, FiPlusCircle, FiSearch, FiPlus } from 'react-icons/fi';
import AdminLayout from '../../components/AdminLayout';
import { storeService } from '../../services/storeService';
import { productService } from '../../services/productService';
import type { Store, StoreInventory } from '../../types/store';
import type { ProductDto } from '../../types/product';
import { getImageUrl } from '../../utils/imageUrl';

type TabKey = 'all' | 'lowStock';

export default function InventoryManagementPage() {
  const [stores, setStores] = useState<Store[]>([]);
  const [selectedStoreId, setSelectedStoreId] = useState<string>('');
  const [storesLoading, setStoresLoading] = useState(true);

  const [inventory, setInventory] = useState<StoreInventory[]>([]);
  const [inventoryLoading, setInventoryLoading] = useState(false);

  const [tab, setTab] = useState<TabKey>('all');

  // Inline edit state
  const [editingProductId, setEditingProductId] = useState<string | null>(null);
  const [editQuantity, setEditQuantity] = useState<string>('');
  const [saving, setSaving] = useState(false);

  // Import stock modal state
  const [importItem, setImportItem] = useState<StoreInventory | null>(null);
  const [importQty, setImportQty] = useState<string>('');
  const [importing, setImporting] = useState(false);

  // Add product to inventory modal state
  const [addModalOpen, setAddModalOpen] = useState(false);
  const [allProducts, setAllProducts] = useState<ProductDto[]>([]);
  const [addProductSearch, setAddProductSearch] = useState('');
  const [addSelectedProduct, setAddSelectedProduct] = useState<ProductDto | null>(null);
  const [addQty, setAddQty] = useState('');
  const [adding, setAdding] = useState(false);

  // Load stores on mount
  useEffect(() => {
    setStoresLoading(true);
    storeService
      .getStores()
      .then((list) => {
        setStores(list);
        if (list.length > 0) setSelectedStoreId(list[0].id);
      })
      .catch(() => toast.error('Không tải được danh sách chi nhánh.'))
      .finally(() => setStoresLoading(false));
  }, []);

  // Load inventory for selected store
  const loadInventory = useCallback(async (storeId: string) => {
    if (!storeId) return;
    setInventoryLoading(true);
    try {
      const data = await storeService.getStoreInventory(storeId);
      setInventory(data);
    } catch {
      toast.error('Không tải được tồn kho.');
    } finally {
      setInventoryLoading(false);
    }
  }, []);

  useEffect(() => {
    if (selectedStoreId) loadInventory(selectedStoreId);
  }, [selectedStoreId, loadInventory]);

  // Inline edit handlers
  const startEdit = (item: StoreInventory) => {
    setEditingProductId(item.productId);
    setEditQuantity(String(item.quantity));
  };

  const cancelEdit = () => {
    setEditingProductId(null);
    setEditQuantity('');
  };

  const saveEdit = async (productId: string) => {
    const qty = parseInt(editQuantity, 10);
    if (isNaN(qty) || qty < 0) { toast.error('Số lượng không hợp lệ.'); return; }
    setSaving(true);
    try {
      const updated = await storeService.updateStoreInventory(selectedStoreId, productId, qty);
      setInventory((prev) =>
        prev.map((item) => item.productId === productId ? { ...item, quantity: updated.quantity } : item),
      );
      toast.success('Đã cập nhật tồn kho.');
      cancelEdit();
    } catch (e: any) {
      toast.error(e.response?.data?.message ?? 'Cập nhật thất bại.');
    } finally {
      setSaving(false);
    }
  };

  // Add product to inventory handlers
  const openAddModal = async () => {
    setAddModalOpen(true);
    setAddProductSearch('');
    setAddSelectedProduct(null);
    setAddQty('');
    if (allProducts.length === 0) {
      try {
        const result = await productService.getProducts({ pageSize: 200 });
        setAllProducts(result?.items ?? []);
      } catch {
        toast.error('Không tải được danh sách sản phẩm.');
      }
    }
  };

  const closeAddModal = () => {
    setAddModalOpen(false);
    setAddProductSearch('');
    setAddSelectedProduct(null);
    setAddQty('');
  };

  const confirmAdd = async () => {
    if (!addSelectedProduct) { toast.error('Vui lòng chọn sản phẩm.'); return; }
    const qty = parseInt(addQty, 10);
    if (isNaN(qty) || qty < 0) { toast.error('Số lượng không hợp lệ.'); return; }
    setAdding(true);
    try {
      const updated = await storeService.updateStoreInventory(selectedStoreId, addSelectedProduct.id, qty);
      setInventory((prev) => {
        const exists = prev.find((i) => i.productId === addSelectedProduct.id);
        if (exists) return prev.map((i) => i.productId === addSelectedProduct.id ? { ...i, quantity: updated.quantity } : i);
        return [...prev, { productId: addSelectedProduct.id, productName: addSelectedProduct.name, quantity: updated.quantity, lowStockThreshold: 10 }];
      });
      toast.success(`Đã thêm "${addSelectedProduct.name}" vào kho.`);
      closeAddModal();
    } catch (e: any) {
      toast.error(e.response?.data?.message ?? 'Thêm vào kho thất bại.');
    } finally {
      setAdding(false);
    }
  };

  const existingProductIds = new Set(inventory.map((i) => i.productId));
  const filteredAddProducts = allProducts.filter(
    (p) => !existingProductIds.has(p.id) &&
      (!addProductSearch.trim() || p.name.toLowerCase().includes(addProductSearch.toLowerCase()))
  );

  // Import stock handlers
  const openImport = (item: StoreInventory) => {
    setImportItem(item);
    setImportQty('');
  };

  const closeImport = () => {
    setImportItem(null);
    setImportQty('');
  };

  const confirmImport = async () => {
    if (!importItem) return;
    const qty = parseInt(importQty, 10);
    if (isNaN(qty) || qty <= 0) { toast.error('Số lượng nhập phải lớn hơn 0.'); return; }
    setImporting(true);
    try {
      const newQty = importItem.quantity + qty;
      const updated = await storeService.updateStoreInventory(selectedStoreId, importItem.productId, newQty);
      setInventory((prev) =>
        prev.map((i) => i.productId === importItem.productId ? { ...i, quantity: updated.quantity } : i),
      );
      toast.success(`Đã nhập ${qty} sản phẩm vào kho.`);
      closeImport();
    } catch (e: any) {
      toast.error(e.response?.data?.message ?? 'Nhập hàng thất bại.');
    } finally {
      setImporting(false);
    }
  };

  const displayedItems = tab === 'lowStock'
    ? inventory.filter((i) => i.quantity <= i.lowStockThreshold)
    : inventory;

  const lowStockCount = inventory.filter((i) => i.quantity <= i.lowStockThreshold).length;

  return (
    <AdminLayout title="Quản lý tồn kho">
      {/* Store selector */}
      <div className="flex items-center gap-3 mb-6">
        <label className="text-sm font-medium text-gray-700 shrink-0">Chi nhánh:</label>
        {storesLoading ? (
          <div className="h-9 w-48 bg-gray-100 rounded-lg animate-pulse" />
        ) : (
          <select
            value={selectedStoreId}
            onChange={(e) => setSelectedStoreId(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-400 bg-white"
          >
            {stores.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        )}
      </div>

      {/* Tabs + Add button */}
      <div className="flex items-end justify-between mb-4 border-b">
        <div className="flex gap-2">
          <button
            onClick={() => setTab('all')}
            className={`pb-2.5 px-1 text-sm font-medium border-b-2 transition-colors ${
              tab === 'all' ? 'border-rose-600 text-rose-600' : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            Tất cả ({inventory.length})
          </button>
          <button
            onClick={() => setTab('lowStock')}
            className={`pb-2.5 px-1 text-sm font-medium border-b-2 transition-colors flex items-center gap-1.5 ${
              tab === 'lowStock' ? 'border-rose-600 text-rose-600' : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            <FiAlertTriangle size={13} />
            Sắp hết hàng
            {lowStockCount > 0 && (
              <span className="bg-red-100 text-red-600 text-xs px-1.5 py-0.5 rounded-full font-semibold">
                {lowStockCount}
              </span>
            )}
          </button>
        </div>

        <button
          onClick={openAddModal}
          className="mb-2 flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-white bg-rose-600 hover:bg-rose-700 rounded-lg transition-colors shrink-0"
        >
          <FiPlus size={14} />
          Thêm sản phẩm
        </button>
      </div>

      {/* Inventory table */}
      <div className="bg-white rounded-xl border shadow-sm overflow-hidden">
        {inventoryLoading ? (
          <div className="p-8 text-center text-gray-400 text-sm">Đang tải tồn kho...</div>
        ) : displayedItems.length === 0 ? (
          <div className="p-8 text-center text-gray-400 text-sm">
            {tab === 'lowStock' ? 'Không có sản phẩm sắp hết hàng.' : 'Không có dữ liệu tồn kho.'}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left px-5 py-3 font-semibold text-gray-600">Sản phẩm</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Tồn kho</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Ngưỡng cảnh báo</th>
                <th className="text-center px-5 py-3 font-semibold text-gray-600">Trạng thái</th>
                <th className="text-right px-5 py-3 font-semibold text-gray-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {displayedItems.map((item) => {
                const isLow = item.quantity <= item.lowStockThreshold;
                const isEditing = editingProductId === item.productId;
                return (
                  <tr key={item.productId} className={isLow ? 'bg-red-50/60' : 'hover:bg-gray-50'}>
                    <td className="px-5 py-3 font-medium text-gray-800">{item.productName}</td>

                    <td className="px-5 py-3 text-center">
                      {isEditing ? (
                        <input
                          type="number"
                          min={0}
                          value={editQuantity}
                          onChange={(e) => setEditQuantity(e.target.value)}
                          className="w-20 border border-rose-400 rounded px-2 py-1 text-center text-sm focus:outline-none focus:ring-1 focus:ring-rose-400"
                          autoFocus
                        />
                      ) : (
                        <span className={`font-semibold ${isLow ? 'text-red-600' : 'text-gray-800'}`}>
                          {item.quantity}
                        </span>
                      )}
                    </td>

                    <td className="px-5 py-3 text-center text-gray-500">{item.lowStockThreshold}</td>

                    <td className="px-5 py-3 text-center">
                      {isLow ? (
                        <span className="inline-flex items-center gap-1 bg-red-100 text-red-700 text-xs px-2 py-0.5 rounded-full font-medium">
                          <FiAlertTriangle size={10} />
                          Sắp hết
                        </span>
                      ) : (
                        <span className="inline-block bg-green-100 text-green-700 text-xs px-2 py-0.5 rounded-full font-medium">
                          Bình thường
                        </span>
                      )}
                    </td>

                    <td className="px-5 py-3 text-right">
                      {isEditing ? (
                        <div className="flex items-center justify-end gap-2">
                          <button
                            onClick={() => saveEdit(item.productId)}
                            disabled={saving}
                            className="p-1.5 rounded-lg bg-green-50 text-green-600 hover:bg-green-100 disabled:opacity-50"
                            title="Lưu"
                          >
                            <FiCheck size={14} />
                          </button>
                          <button
                            onClick={cancelEdit}
                            disabled={saving}
                            className="p-1.5 rounded-lg bg-gray-50 text-gray-500 hover:bg-gray-100 disabled:opacity-50"
                            title="Hủy"
                          >
                            <FiX size={14} />
                          </button>
                        </div>
                      ) : (
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => openImport(item)}
                            className="p-1.5 rounded-lg text-gray-500 hover:bg-blue-50 hover:text-blue-600 transition-colors"
                            title="Nhập hàng vào kho"
                          >
                            <FiPlusCircle size={14} />
                          </button>
                          <button
                            onClick={() => startEdit(item)}
                            className="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-rose-600 transition-colors"
                            title="Sửa tồn kho"
                          >
                            <FiEdit2 size={14} />
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* Add product to inventory modal */}
      {addModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-lg overflow-hidden">
            {/* Header */}
            <div className="relative bg-gradient-to-r from-rose-500 to-rose-600 px-6 pt-6 pb-10">
              <button
                onClick={closeAddModal}
                disabled={adding}
                className="absolute top-4 right-4 p-1.5 rounded-full bg-white/20 hover:bg-white/30 text-white transition-colors disabled:opacity-50"
              >
                <FiX size={15} />
              </button>
              <p className="text-xs font-semibold uppercase tracking-widest text-rose-200 mb-1">Tồn kho</p>
              <h3 className="text-xl font-bold text-white">Thêm sản phẩm vào kho</h3>
            </div>

            <div className="px-6 -mt-5 space-y-4 pb-6">
              {/* Search box — floats over gradient */}
              <div className="relative bg-white rounded-2xl shadow-lg ring-1 ring-gray-200">
                <FiSearch size={15} className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  value={addProductSearch}
                  onChange={(e) => { setAddProductSearch(e.target.value); setAddSelectedProduct(null); }}
                  placeholder="Tìm tên sản phẩm..."
                  className="w-full pl-10 pr-4 py-3.5 text-sm rounded-2xl focus:outline-none focus:ring-2 focus:ring-rose-400 bg-transparent"
                  autoFocus
                />
              </div>

              {/* Fixed-height area: dropdown OR selected card */}
              <div className="h-52">
                {addSelectedProduct ? (
                  <div className="flex items-center gap-4 bg-rose-50 rounded-2xl px-4 py-3 ring-1 ring-rose-200">
                    <div className="h-14 w-14 rounded-xl overflow-hidden shrink-0 bg-white shadow-sm">
                      {addSelectedProduct.imageUrl ? (
                        <img src={getImageUrl(addSelectedProduct.imageUrl)} alt={addSelectedProduct.name} className="h-full w-full object-cover" />
                      ) : (
                        <div className="h-full w-full flex items-center justify-center text-2xl">🍽</div>
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold text-gray-900 truncate">{addSelectedProduct.name}</p>
                      <p className="text-xs text-rose-600 font-medium mt-0.5">{addSelectedProduct.price.toLocaleString('vi-VN')}đ</p>
                    </div>
                    <button
                      onClick={() => { setAddSelectedProduct(null); setAddProductSearch(''); }}
                      className="p-1.5 rounded-full hover:bg-rose-100 text-rose-400 hover:text-rose-600 transition-colors"
                    >
                      <FiX size={14} />
                    </button>
                  </div>
                ) : addProductSearch ? (
                  <div className="rounded-2xl border border-gray-100 shadow-lg overflow-hidden h-full">
                    {filteredAddProducts.length === 0 ? (
                      <div className="h-full flex items-center justify-center text-sm text-gray-400">
                        Không tìm thấy sản phẩm chưa có trong kho.
                      </div>
                    ) : (
                      <div className="h-full overflow-y-auto divide-y divide-gray-50">
                        {filteredAddProducts.slice(0, 10).map((p) => (
                          <button
                            key={p.id}
                            onClick={() => { setAddSelectedProduct(p); setAddProductSearch(p.name); }}
                            className="w-full flex items-center gap-3 px-4 py-3 hover:bg-rose-50 transition-colors text-left group"
                          >
                            <div className="h-10 w-10 rounded-xl overflow-hidden shrink-0 bg-gray-100">
                              {p.imageUrl ? (
                                <img src={getImageUrl(p.imageUrl)} alt={p.name} className="h-full w-full object-cover" />
                              ) : (
                                <div className="h-full w-full flex items-center justify-center text-gray-300 text-lg">🍽</div>
                              )}
                            </div>
                            <div className="min-w-0 flex-1">
                              <p className="text-sm font-medium text-gray-800 truncate group-hover:text-rose-700">{p.name}</p>
                              <p className="text-xs text-gray-400">{p.price.toLocaleString('vi-VN')}đ</p>
                            </div>
                            <FiPlus size={14} className="text-gray-300 group-hover:text-rose-500 shrink-0" />
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="h-full flex flex-col items-center justify-center gap-2 text-gray-300">
                    <FiSearch size={28} />
                    <p className="text-sm">Gõ tên sản phẩm để tìm kiếm</p>
                  </div>
                )}
              </div>

              {/* Quantity stepper */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Số lượng nhập ban đầu
                </label>
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => setAddQty((v) => String(Math.max(0, (parseInt(v) || 0) - 1)))}
                    className="h-11 w-11 rounded-xl border border-gray-200 flex items-center justify-center text-gray-600 hover:bg-gray-50 hover:border-gray-300 active:scale-95 transition-all font-bold text-lg"
                  >
                    −
                  </button>
                  <input
                    type="number"
                    min={0}
                    value={addQty}
                    onChange={(e) => setAddQty(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && confirmAdd()}
                    placeholder="0"
                    className="flex-1 h-11 border border-gray-200 rounded-xl px-4 text-center text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-rose-400 focus:border-transparent"
                  />
                  <button
                    type="button"
                    onClick={() => setAddQty((v) => String((parseInt(v) || 0) + 1))}
                    className="h-11 w-11 rounded-xl border border-gray-200 flex items-center justify-center text-gray-600 hover:bg-gray-50 hover:border-gray-300 active:scale-95 transition-all font-bold text-lg"
                  >
                    +
                  </button>
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="flex items-center gap-3 px-6 py-4 bg-gray-50 border-t border-gray-100">
              <button
                onClick={closeAddModal}
                disabled={adding}
                className="flex-1 py-2.5 text-sm font-medium text-gray-600 bg-white border border-gray-200 hover:bg-gray-100 rounded-xl transition-colors disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                onClick={confirmAdd}
                disabled={adding || !addSelectedProduct}
                className="flex-1 py-2.5 text-sm font-semibold text-white bg-gradient-to-r from-rose-500 to-rose-600 hover:from-rose-600 hover:to-rose-700 rounded-xl transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 shadow-sm shadow-rose-200"
              >
                {adding
                  ? <span className="inline-block h-4 w-4 rounded-full border-2 border-white border-t-transparent animate-spin" />
                  : <FiPlus size={15} />
                }
                Thêm vào kho
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Import stock modal */}
      {importItem && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h3 className="text-base font-semibold text-gray-800">Nhập hàng vào kho</h3>
              <button
                onClick={closeImport}
                disabled={importing}
                className="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors disabled:opacity-50"
              >
                <FiX size={16} />
              </button>
            </div>

            <div className="px-6 py-5 space-y-4">
              <div>
                <p className="text-sm font-medium text-gray-800">{importItem.productName}</p>
                <p className="text-xs text-gray-500 mt-0.5">
                  Tồn kho hiện tại:{' '}
                  <span className="font-semibold text-gray-700">{importItem.quantity}</span>
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Số lượng nhập thêm <span className="text-red-500">*</span>
                </label>
                <input
                  type="number"
                  min={1}
                  value={importQty}
                  onChange={(e) => setImportQty(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && confirmImport()}
                  placeholder="Nhập số lượng..."
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
                  autoFocus
                />
                {importQty && parseInt(importQty, 10) > 0 && (
                  <p className="mt-1.5 text-xs text-gray-500">
                    Sau nhập:{' '}
                    <span className="font-semibold text-gray-700">
                      {importItem.quantity + parseInt(importQty, 10)}
                    </span>
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t">
              <button
                onClick={closeImport}
                disabled={importing}
                className="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                onClick={confirmImport}
                disabled={importing}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors disabled:opacity-60 flex items-center gap-2"
              >
                {importing && (
                  <span className="inline-block h-3.5 w-3.5 rounded-full border-2 border-white border-t-transparent animate-spin" />
                )}
                Xác nhận nhập hàng
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
