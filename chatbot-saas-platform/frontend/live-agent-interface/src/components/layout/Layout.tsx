import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '../../hooks/redux';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { NotificationCenter } from '../notifications/NotificationCenter';
import { ToastContainer } from '../ToastContainer';

interface LayoutProps {
  children: React.ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { t } = useTranslation();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [notificationCenterOpen, setNotificationCenterOpen] = useState(false);
  const { isRTL } = useAppSelector((state) => state.theme);

  return (
    <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${isRTL ? 'rtl' : 'ltr'}`}>
      <Sidebar 
        isOpen={sidebarOpen} 
        onToggle={() => setSidebarOpen(!sidebarOpen)} 
      />
      
      <div className={`transition-all duration-300 ${
        sidebarOpen 
          ? isRTL ? 'mr-64' : 'ml-64' 
          : isRTL ? 'mr-16' : 'ml-16'
      }`}>
        <Header 
          onToggleSidebar={() => setSidebarOpen(!sidebarOpen)}
          onToggleNotifications={() => setNotificationCenterOpen(!notificationCenterOpen)}
        />
        
        <main className="p-6">
          {children}
        </main>
      </div>

      <NotificationCenter 
        isOpen={notificationCenterOpen}
        onClose={() => setNotificationCenterOpen(false)}
      />

      <ToastContainer />
    </div>
  );
};
