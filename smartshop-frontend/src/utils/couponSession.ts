import type { ValidateCouponResult } from '../services/couponService';

const KEY_CODE   = 'cart_coupon_code';
const KEY_RESULT = 'cart_coupon_result';

export const couponSession = {
  save(code: string, result: ValidateCouponResult) {
    sessionStorage.setItem(KEY_CODE,   code);
    sessionStorage.setItem(KEY_RESULT, JSON.stringify(result));
  },

  load(): { code: string; result: ValidateCouponResult } | null {
    const code   = sessionStorage.getItem(KEY_CODE);
    const raw    = sessionStorage.getItem(KEY_RESULT);
    if (!code || !raw) return null;
    try {
      return { code, result: JSON.parse(raw) as ValidateCouponResult };
    } catch {
      return null;
    }
  },

  clear() {
    sessionStorage.removeItem(KEY_CODE);
    sessionStorage.removeItem(KEY_RESULT);
  },
};
