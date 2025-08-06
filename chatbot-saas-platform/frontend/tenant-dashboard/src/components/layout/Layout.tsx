import React, { useState } from 'react';
import { useAppSelector } from '../../hooks/redux';
import { Sidebar } from './Sidebar';
import { Header } from './Header';

interface LayoutProps {
  children: React.ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
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
        />
        
        <main className="p-6">
          {children}
        </main>
      </div>
    </div>
  );
};
