import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector, useAppDispatch } from '../../hooks/redux';
import { logout } from '../../store/slices/authSlice';
import { setLanguage, toggleDarkMode } from '../../store/slices/themeSlice';
import { TenantNotificationCenter } from '../TenantNotificationCenter';
import { 
  Menu, 
  Moon, 
  Sun, 
  Globe, 
  LogOut,
  Settings,
  User,
  ChevronDown,
  Bell
} from 'lucide-react';

interface HeaderProps {
  onToggleSidebar: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onToggleSidebar }) => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [notificationCenterOpen, setNotificationCenterOpen] = useState(false);
  
  const { user } = useAppSelector((state) => state.auth);
  const { isDarkMode, language } = useAppSelector((state) => state.theme);
  const { tenantNotifications } = useAppSelector((state) => state.analytics);

  const handleLogout = () => {
    dispatch(logout());
  };

  const handleLanguageChange = () => {
    const newLanguage = language === 'en' ? 'ar' : 'en';
    dispatch(setLanguage(newLanguage));
  };

  return (
    <header className="bg-white dark:bg-gray-800 shadow-sm border-b border-gray-200 dark:border-gray-700">
      <div className="flex h-16 items-center justify-between px-4">
        <div className="flex items-center">
          <button
            onClick={onToggleSidebar}
            className="p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <Menu size={20} />
          </button>
        </div>

        <div className="flex items-center space-x-4 rtl:space-x-reverse">
          <button
            onClick={() => setNotificationCenterOpen(!notificationCenterOpen)}
            className="relative p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <Bell size={20} />
            {tenantNotifications.length > 0 && (
              <span className="absolute -top-1 -right-1 h-4 w-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center">
                {tenantNotifications.length > 9 ? '9+' : tenantNotifications.length}
              </span>
            )}
          </button>

          <button
            onClick={() => dispatch(toggleDarkMode())}
            className="p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            {isDarkMode ? <Sun size={20} /> : <Moon size={20} />}
          </button>

          <button
            onClick={handleLanguageChange}
            className="p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <Globe size={20} />
          </button>

          <div className="relative">
            <button
              onClick={() => setUserMenuOpen(!userMenuOpen)}
              className="flex items-center space-x-3 rtl:space-x-reverse text-sm rounded-full focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              {user?.avatar ? (
                <img className="h-8 w-8 rounded-full" src={user.avatar} alt={user.name} />
              ) : (
                <div className="h-8 w-8 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center">
                  <User size={16} />
                </div>
              )}
              <span className="hidden md:block text-gray-700 dark:text-gray-300">{user?.name}</span>
              <ChevronDown size={16} className="text-gray-400" />
            </button>

            {userMenuOpen && (
              <div className="absolute right-0 rtl:right-auto rtl:left-0 mt-2 w-48 bg-white dark:bg-gray-800 rounded-md shadow-lg ring-1 ring-black ring-opacity-5 z-50">
                <div className="py-1">
                  <a
                    href="/settings"
                    className="flex items-center px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <Settings size={16} className="mr-3 rtl:mr-0 rtl:ml-3" />
                    {t('navigation.settings')}
                  </a>
                  <button
                    onClick={handleLogout}
                    className="flex items-center w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <LogOut size={16} className="mr-3 rtl:mr-0 rtl:ml-3" />
                    {t('auth.logout')}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
      
      <TenantNotificationCenter 
        isOpen={notificationCenterOpen} 
        onClose={() => setNotificationCenterOpen(false)} 
      />
    </header>
  );
};
