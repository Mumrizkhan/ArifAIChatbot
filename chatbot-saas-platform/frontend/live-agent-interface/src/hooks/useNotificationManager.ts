import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { AppDispatch } from '../store/store';
import { addAgentNotification } from '../store/slices/agentSlice';

interface NotificationOptions {
  id?: string;
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  agentId: string;
  preventDuplicates?: boolean;
  duplicateWindow?: number; // in milliseconds
}

export const useNotificationManager = () => {
  const dispatch = useDispatch<AppDispatch>();

  const showNotification = useCallback((options: NotificationOptions) => {
    const {
      id,
      type,
      title,
      message,
      agentId,
      preventDuplicates = true,
      duplicateWindow = 5000 // 5 seconds
    } = options;

    const notificationId = id || `${type}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    if (preventDuplicates) {
      // Check for duplicate notifications
      const storageKey = `notifications-${type}`;
      const recentNotifications = JSON.parse(sessionStorage.getItem(storageKey) || '{}');
      const now = Date.now();
      const windowStart = now - duplicateWindow;
      
      // Clean up old entries
      Object.keys(recentNotifications).forEach(key => {
        if (recentNotifications[key] < windowStart) {
          delete recentNotifications[key];
        }
      });
      
      // Create a hash of the notification content for duplicate detection
      const contentHash = `${title}-${message}`.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
      
      // Check if this notification was already shown recently
      if (recentNotifications[contentHash]) {
        console.log("🚫 Skipping duplicate notification:", title);
        return;
      }
      
      // Mark this notification as shown
      recentNotifications[contentHash] = now;
      sessionStorage.setItem(storageKey, JSON.stringify(recentNotifications));
    }

    // Dispatch the notification
    dispatch(addAgentNotification({
      id: notificationId,
      type,
      title,
      message,
      timestamp: new Date().toISOString(),
      agentId
    }));

    return notificationId;
  }, [dispatch]);

  const showSuccess = useCallback((title: string, message: string, agentId: string, options?: Partial<NotificationOptions>) => {
    return showNotification({ ...options, type: 'success', title, message, agentId });
  }, [showNotification]);

  const showError = useCallback((title: string, message: string, agentId: string, options?: Partial<NotificationOptions>) => {
    return showNotification({ ...options, type: 'error', title, message, agentId });
  }, [showNotification]);

  const showInfo = useCallback((title: string, message: string, agentId: string, options?: Partial<NotificationOptions>) => {
    return showNotification({ ...options, type: 'info', title, message, agentId });
  }, [showNotification]);

  const showWarning = useCallback((title: string, message: string, agentId: string, options?: Partial<NotificationOptions>) => {
    return showNotification({ ...options, type: 'warning', title, message, agentId });
  }, [showNotification]);

  return {
    showNotification,
    showSuccess,
    showError,
    showInfo,
    showWarning
  };
};
