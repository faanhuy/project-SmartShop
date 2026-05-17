import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';

export interface SignalRNotification {
  notificationId: string;
  title: string;
  message: string;
  orderId?: string;
}

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7046/api';
const HUB_URL = `${API_BASE_URL.replace(/\/?api\/?$/, '')}/hubs/orders`;

function normalizeNotificationPayload(data: any): SignalRNotification {
  return {
    notificationId: data?.notificationId ?? data?.NotificationId ?? '',
    title: data?.title ?? data?.Title ?? '',
    message: data?.message ?? data?.Message ?? 'Đơn hàng đã được cập nhật trạng thái.',
    orderId: data?.orderId ?? data?.OrderId,
  };
}

export function useSignalR(onOrderStatusUpdated: (data: SignalRNotification) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const callbackRef = useRef(onOrderStatusUpdated);
  const { accessToken } = useAuthStore();

  // Keep callback ref fresh without re-connecting
  useEffect(() => {
    callbackRef.current = onOrderStatusUpdated;
  }, [onOrderStatusUpdated]);

  useEffect(() => {
    if (!accessToken) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('OrderStatusUpdated', (data: any) => {
      callbackRef.current(normalizeNotificationPayload(data));
    });

    connection.on('ReturnRequestUpdated', (data: any) => {
      callbackRef.current(normalizeNotificationPayload(data));
    });

    connection.start().catch((err) => console.error('SignalR connection failed:', err));
    connectionRef.current = connection;

    return () => {
      connection.stop();
      connectionRef.current = null;
    };
  }, [accessToken]);
}
