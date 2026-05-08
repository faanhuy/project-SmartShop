import { create } from 'zustand';
import { storeService } from '../services/storeService';
import type { Store } from '../types/store';

const STORAGE_KEY = 'selectedStoreId';

interface StoreSelectionState {
  stores: Store[];
  selectedStore: Store | null;
  setSelectedStore: (store: Store) => void;
  fetchStores: () => Promise<void>;
}

export const useStoreSelectionStore = create<StoreSelectionState>((set, get) => ({
  stores: [],
  selectedStore: null,

  setSelectedStore: (store: Store) => {
    localStorage.setItem(STORAGE_KEY, store.id);
    set({ selectedStore: store });
  },

  fetchStores: async () => {
    const stores = await storeService.getStores();
    set({ stores });

    // Restore selectedStore from localStorage if still valid
    const savedId = localStorage.getItem(STORAGE_KEY);
    if (savedId) {
      const match = stores.find((s) => s.id === savedId);
      if (match && !get().selectedStore) {
        set({ selectedStore: match });
      }
    }
  },
}));
