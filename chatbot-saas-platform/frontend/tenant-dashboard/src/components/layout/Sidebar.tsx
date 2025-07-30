import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '../../hooks/redux';
import { 
  LayoutDashboard, 
  Bot, 
  Users, 
  BarChart3, 
  CreditCard,
  Settings,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';

interface SidebarProps {
  isOpen: boolean;
  onToggle: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen, onToggle }) => {
  const { t } = useTranslation();
  const location = useLocation();
  const { branding, isRTL } = useAppSelector((state) => state.theme);

  const navigation = [
    { name: t('navigation.dashboard'), href: '/dashboard', icon: LayoutDashboard },
    { name: t('navigation.chatbot'), href: '/chatbot', icon: Bot },
    { name: t('navigation.team'), href: '/team', icon: Users },
    { name: t('navigation.analytics'), href: '/analytics', icon: BarChart3 },
    { name: t('navigation.subscription'), href: '/subscription', icon: CreditCard },
    { name: t('navigation.settings'), href: '/settings', icon: Settings },
  ];

  return (
    <div className={`fixed inset-y-0 ${isRTL ? 'right-0' : 'left-0'} z-50 flex`}>
      <div className={`relative flex w-64 flex-col bg-white dark:bg-gray-800 shadow-lg transition-all duration-300 ${
        isOpen ? 'translate-x-0' : isRTL ? 'translate-x-48' : '-translate-x-48'
      }`}>
        <div className="flex h-16 items-center justify-between px-4 border-b border-gray-200 dark:border-gray-700">
          {branding.logo ? (
            <img 
              src={branding.logo} 
              alt={branding.companyName || 'Logo'} 
              className="h-8 w-auto"
            />
          ) : (
            <div className="text-xl font-bold text-gray-900 dark:text-white">
              {branding.companyName || 'Tenant Dashboard'}
            </div>
          )}
          
          <button
            onClick={onToggle}
            className="p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            {isRTL ? (
              isOpen ? <ChevronRight size={20} /> : <ChevronLeft size={20} />
            ) : (
              isOpen ? <ChevronLeft size={20} /> : <ChevronRight size={20} />
            )}
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-2 py-4">
          {navigation.map((item) => {
            const isActive = location.pathname === item.href;
            const Icon = item.icon;
            
            return (
              <NavLink
                key={item.name}
                to={item.href}
                className={`group flex items-center px-2 py-2 text-sm font-medium rounded-md transition-colors ${
                  isActive
                    ? 'bg-blue-100 text-blue-900 dark:bg-blue-900 dark:text-blue-100'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900 dark:text-gray-300 dark:hover:bg-gray-700 dark:hover:text-white'
                }`}
              >
                <Icon
                  className={`${isRTL ? 'ml-3' : 'mr-3'} h-5 w-5 flex-shrink-0 ${
                    isActive ? 'text-blue-500' : 'text-gray-400 group-hover:text-gray-500'
                  }`}
                />
                {item.name}
              </NavLink>
            );
          })}
        </nav>
      </div>

      {!isOpen && (
        <button
          onClick={onToggle}
          className="w-16 bg-white dark:bg-gray-800 shadow-lg border-r border-gray-200 dark:border-gray-700 flex items-center justify-center hover:bg-gray-50 dark:hover:bg-gray-700"
        >
          {isRTL ? <ChevronLeft size={20} /> : <ChevronRight size={20} />}
        </button>
      )}
    </div>
  );
};
