import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponse } from '../types/auth';

interface AuthUser {
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  cartVersion: number;
  wishlistVersion: number;
  setAuth: (data: AuthResponse) => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  updateUser: (patch: Partial<AuthUser>) => void;
  logout: () => void;
  refreshCartCount: () => void;
  refreshWishlistCount: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      cartVersion: 0,
      wishlistVersion: 0,

      setAuth: (data: AuthResponse) =>
        set({
          accessToken: data.token,
          refreshToken: data.refreshToken,
          user: {
            email: data.email,
            firstName: data.firstName,
            lastName: data.lastName,
            role: data.role,
          },
          isAuthenticated: true,
        }),

      setTokens: (accessToken: string, refreshToken: string) =>
        set({ accessToken, refreshToken }),

      updateUser: (patch: Partial<AuthUser>) =>
        set((s) => ({ user: s.user ? { ...s.user, ...patch } : s.user })),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          user: null,
          isAuthenticated: false,
          cartVersion: 0,
          wishlistVersion: 0,
        }),

      refreshCartCount: () =>
        set((s) => ({ cartVersion: s.cartVersion + 1 })),

      refreshWishlistCount: () =>
        set((s) => ({ wishlistVersion: s.wishlistVersion + 1 })),
    }),
    {
      name: 'smartshop-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        // cartVersion không persist — luôn bắt đầu từ 0
      }),
    }
  )
);
