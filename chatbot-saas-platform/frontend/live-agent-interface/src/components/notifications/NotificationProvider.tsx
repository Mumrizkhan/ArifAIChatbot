import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../hooks/redux';
import { addNotification } from '../../store/slices/notificationSlice';
import { io, Socket } from 'socket.io-client';

interface NotificationProviderProps {
  children: React.ReactNode;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children }) => {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((state) => state.auth);
  const { soundEnabled, desktopEnabled } = useAppSelector((state) => state.notifications);

  useEffect(() => {
    if (!user) return;

    const socket: Socket = io('/notifications', {
      auth: {
        token: localStorage.getItem('token'),
      },
    });

    socket.on('notification', (notification) => {
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

    return () => {
      socket.disconnect();
    };
  }, [user, dispatch, soundEnabled, desktopEnabled]);

  return <>{children}</>;
};
