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
  const { user, isAuthenticated, token } = useAppSelector((state) => state.auth);
  const { soundEnabled, desktopEnabled } = useAppSelector((state) => state.notifications);

  useEffect(() => {
    console.log("🔗 NotificationProvider: Auth state changed", { 
      hasUser: !!user, 
      isAuthenticated, 
      hasToken: !!token,
      userId: user?.id,
      tenantId: user?.tenantId 
    });

    // Connect to SignalR when user is authenticated and we have all required info
    if (isAuthenticated && token && user && user.id && user.tenantId) {
      console.log("🔗 NotificationProvider: Attempting SignalR connection...");
      
      agentSignalRService.connect(token, user.id, user.tenantId).then((connected: boolean) => {
        console.log("🔗 NotificationProvider: SignalR connection result:", connected);
        dispatch(setSignalRConnectionStatus(connected));
        
        if (connected) {
          console.log("🔗 NotificationProvider: Setting up SignalR event handlers...");
          
          agentSignalRService.setOnAgentNotification((notification: any) => {
            console.log("📢 NotificationProvider: Received agent notification:", notification);
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
            console.log("🔗 NotificationProvider: SignalR connection status changed:", isConnected);
            dispatch(setSignalRConnectionStatus(isConnected));
          });
        }
      }).catch((error) => {
        console.error("🔗 NotificationProvider: SignalR connection failed:", error);
        dispatch(setSignalRConnectionStatus(false));
      });
    } else {
      console.log("🔗 NotificationProvider: Not ready for SignalR connection", {
        missingAuth: !isAuthenticated,
        missingToken: !token,
        missingUser: !user,
        missingUserId: !user?.id,
        missingTenantId: !user?.tenantId
      });
    }

    return () => {
      console.log("🔗 NotificationProvider: Cleaning up SignalR connection...");
      agentSignalRService.disconnect();
    };
  }, [user, isAuthenticated, token, dispatch, soundEnabled, desktopEnabled]);

  return <>{children}</>;
};
