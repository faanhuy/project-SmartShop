export const formatPrice = (price: number): string =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);

export const formatDate = (iso: string): string =>
  new Date(iso).toLocaleDateString('vi-VN', { dateStyle: 'medium' });

export const formatDateTime = (iso: string): string => {
  const date = new Date(iso);

  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');

  return `${day}/${month}/${year} ${hours}:${minutes}`;
};
