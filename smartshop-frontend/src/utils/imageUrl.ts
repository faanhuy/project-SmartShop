/**
 * Chuyển đổi image path thành URL đầy đủ.
 * - Nếu path đã là http/https → giữ nguyên (external URL)
 * - Nếu path có prefix local: → dùng asset trong public/images/products
 * - Nếu path là relative bắt đầu bằng / → prepend API host
 * - Nếu null/undefined → trả về ''
 */
const API_BASE = (import.meta.env.VITE_API_URL ?? 'https://localhost:7046/api')
  .replace(/\/api$/, ''); // "http://localhost:5284"

export function getImageUrl(path?: string | null): string {
  if (!path) return '/images/products/default.svg';

  const normalizedPath = path.trim().replace(/\\/g, '/');

  if (normalizedPath.startsWith('local:')) {
    return `/images/products/${normalizedPath.slice('local:'.length)}`;
  }

  if (normalizedPath.startsWith('http://') || normalizedPath.startsWith('https://')) {
    return normalizedPath;
  }

  const backendRelativePath = `/${normalizedPath.replace(/^\.?\/+/, '')}`;
  return `${API_BASE}${backendRelativePath}`;
}
