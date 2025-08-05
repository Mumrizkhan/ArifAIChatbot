import React from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector, useAppDispatch } from '../../hooks/redux';
import { markAsRead, markAllAsRead, removeNotification, clearAllNotifications } from '../../store/slices/notificationSlice';
import { X, Bell, Check, CheckCheck, Trash2 } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

interface NotificationCenterProps {
  isOpen: boolean;
  onClose: () => void;
}

export const NotificationCenter: React.FC<NotificationCenterProps> = ({ isOpen, onClose }) => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { notifications, unreadCount } = useAppSelector((state) => state.notifications);
  const { isRTL } = useAppSelector((state) => state.theme);

  const handleMarkAsRead = (notificationId: string) => {
    dispatch(markAsRead(notificationId));
  };

  const handleMarkAllAsRead = () => {
    dispatch(markAllAsRead());
  };

  const handleRemoveNotification = (notificationId: string) => {
    dispatch(removeNotification(notificationId));
  };

  const handleClearAll = () => {
    dispatch(clearAllNotifications());
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'success': return '✅';
      case 'warning': return '⚠️';
      case 'error': return '❌';
      default: return 'ℹ️';
    }
  };

  const getNotificationColor = (type: string) => {
    switch (type) {
      case 'success': return 'border-green-200 bg-green-50';
      case 'warning': return 'border-yellow-200 bg-yellow-50';
      case 'error': return 'border-red-200 bg-red-50';
      default: return 'border-blue-200 bg-blue-50';
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-hidden">
      <div className="absolute inset-0 bg-black bg-opacity-25" onClick={onClose}></div>
      
      <div className={`absolute top-0 ${isRTL ? 'left-0' : 'right-0'} h-full w-96 bg-white dark:bg-gray-800 shadow-xl transform transition-transform duration-300 ${
        isOpen ? 'translate-x-0' : isRTL ? '-translate-x-full' : 'translate-x-full'
      }`}>
        <div className="flex flex-col h-full">
          <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center space-x-2 rtl:space-x-reverse">
              <Bell size={20} className="text-gray-600 dark:text-gray-400" />
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
                {t('notifications.title')}
              </h2>
              {unreadCount > 0 && (
                <span className="bg-red-500 text-white text-xs rounded-full px-2 py-1">
                  {unreadCount}
                </span>
              )}
            </div>
            
            <button
              onClick={onClose}
              className="p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
              aria-label={t('common.close')}
            >
              <X size={20} />
            </button>
          </div>

          {notifications.length > 0 && (
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
              <button
                onClick={handleMarkAllAsRead}
                disabled={unreadCount === 0}
                className="flex items-center space-x-2 rtl:space-x-reverse text-sm text-blue-600 hover:text-blue-800 disabled:text-gray-400 disabled:cursor-not-allowed"
              >
                <CheckCheck size={16} />
                <span>{t('notifications.markAllRead')}</span>
              </button>
              
              <button
                onClick={handleClearAll}
                className="flex items-center space-x-2 rtl:space-x-reverse text-sm text-red-600 hover:text-red-800"
              >
                <Trash2 size={16} />
                <span>{t('notifications.clearAll')}</span>
              </button>
            </div>
          )}

          <div className="flex-1 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-gray-500 dark:text-gray-400">
                <Bell size={48} className="mb-4 opacity-50" />
                <p className="text-center">{t('notifications.noNotifications')}</p>
              </div>
            ) : (
              <div className="space-y-2 p-4">
                {notifications.map((notification) => (
                  <div
                    key={notification.id}
                    className={`relative p-4 rounded-lg border ${getNotificationColor(notification.type)} ${
                      !notification.isRead ? 'ring-2 ring-blue-200' : ''
                    }`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start space-x-3 rtl:space-x-reverse flex-1">
                        <span className="text-lg">{getNotificationIcon(notification.type)}</span>
                        
                        <div className="flex-1 min-w-0">
                          <h4 className="text-sm font-medium text-gray-900 dark:text-white">
                            {notification.title}
                          </h4>
                          <p className="text-sm text-gray-600 dark:text-gray-300 mt-1">
                            {notification.message}
                          </p>
                          <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                            {formatDistanceToNow(notification.timestamp, { addSuffix: true })}
                          </p>
                          
                          {notification.actionUrl && notification.actionText && (
                            <a
                              href={notification.actionUrl}
                              className="inline-block mt-2 text-sm text-blue-600 hover:text-blue-800 underline"
                            >
                              {notification.actionText}
                            </a>
                          )}
                        </div>
                      </div>

                      <div className="flex items-center space-x-2 rtl:space-x-reverse ml-2 rtl:ml-0 rtl:mr-2">
                        {!notification.isRead && (
                          <button
                            onClick={() => handleMarkAsRead(notification.id)}
                            className="p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
                            aria-label={t('notifications.markAsRead')}
                          >
                            <Check size={16} />
                          </button>
                        )}
                        
                        {!notification.persistent && (
                          <button
                            onClick={() => handleRemoveNotification(notification.id)}
                            className="p-1 rounded-md text-gray-400 hover:text-red-600 hover:bg-gray-100 dark:hover:bg-gray-700"
                            aria-label={t('common.delete')}
                          >
                            <X size={16} />
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
