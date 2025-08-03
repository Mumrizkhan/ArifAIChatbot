import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../hooks/redux';
import { addNotification } from '../../store/slices/notificationSlice';
import { agentSignalRService } from '../../services/signalr';
import { setSignalRConnectionStatus, addAgentNotification } from '../../store/slices/agentSlice';

interface NotificationProviderProps {
  children: React.ReactNode;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children }) => {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((state) => state.auth);
  const { soundEnabled, desktopEnabled } = useAppSelector((state) => state.notifications);

  useEffect(() => {
    if (!user) return;

    const token = localStorage.getItem('token');
    if (token && user.id && user.tenantId) {
      agentSignalRService.connect(token, user.id, user.tenantId).then((connected: boolean) => {
        dispatch(setSignalRConnectionStatus(connected));
        
        if (connected) {
          agentSignalRService.setOnAgentNotification((notification: any) => {
            dispatch(addAgentNotification(notification));
            dispatch(addNotification(notification));

            if (soundEnabled) {
              const audio = new Audio('/notification-sound.mp3');
              audio.play().catch(() => {
                console.log('Could not play notification sound');
              });
            }

            if (desktopEnabled && 'Notification' in window) {
              if (Notification.permission === 'granted') {
                new Notification(notification.title, {
                  body: notification.message,
                  icon: '/favicon.ico',
                });
              } else if (Notification.permission !== 'denied') {
                Notification.requestPermission().then((permission) => {
                  if (permission === 'granted') {
                    new Notification(notification.title, {
                      body: notification.message,
                      icon: '/favicon.ico',
                    });
                  }
                });
              }
            }
          });

          agentSignalRService.setOnConnectionStatusChange((isConnected: boolean) => {
            dispatch(setSignalRConnectionStatus(isConnected));
          });
        }
      });
    }

    return () => {
      agentSignalRService.disconnect();
    };
  }, [user, dispatch, soundEnabled, desktopEnabled]);

  return <>{children}</>;
};
