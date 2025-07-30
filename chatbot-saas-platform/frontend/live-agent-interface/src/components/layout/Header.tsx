import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector, useAppDispatch } from '../../hooks/redux';
import { logout } from '../../store/slices/authSlice';
import { setLanguage, toggleDarkMode } from '../../store/slices/themeSlice';
import { updateAgentStatus } from '../../store/slices/agentSlice';
import { 
  Bell, 
  Menu, 
  Moon, 
  Sun, 
  Globe, 
  LogOut,
  Settings,
  User,
  ChevronDown
} from 'lucide-react';

interface HeaderProps {
  onToggleSidebar: () => void;
  onToggleNotifications: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onToggleSidebar, onToggleNotifications }) => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [statusMenuOpen, setStatusMenuOpen] = useState(false);
  
  const { user } = useAppSelector((state) => state.auth);
  const { currentAgent } = useAppSelector((state) => state.agent);
  const { unreadCount } = useAppSelector((state) => state.notifications);
  const { isDarkMode, language, isRTL } = useAppSelector((state) => state.theme);

  const handleLogout = () => {
    dispatch(logout());
  };

  const handleLanguageChange = () => {
    const newLanguage = language === 'en' ? 'ar' : 'en';
    dispatch(setLanguage(newLanguage));
  };

  const handleStatusChange = (status: 'online' | 'away' | 'busy' | 'offline') => {
    if (currentAgent) {
      dispatch(updateAgentStatus({ agentId: currentAgent.id, status }));
    }
    setStatusMenuOpen(false);
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'online': return 'bg-green-500';
      case 'away': return 'bg-yellow-500';
      case 'busy': return 'bg-red-500';
      case 'offline': return 'bg-gray-500';
      default: return 'bg-gray-500';
    }
  };

  return (
    <header className="bg-white dark:bg-gray-800 shadow-sm border-b border-gray-200 dark:border-gray-700">
      <div className="flex h-16 items-center justify-between px-4">
        <div className="flex items-center">
          <button
            onClick={onToggleSidebar}
            className="p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
            aria-label={t('accessibility.toggleSidebar')}
          >
            <Menu size={20} />
          </button>
        </div>

        <div className="flex items-center space-x-4 rtl:space-x-reverse">
          {currentAgent && (
            <div className="relative">
              <button
                onClick={() => setStatusMenuOpen(!statusMenuOpen)}
                className="flex items-center space-x-2 rtl:space-x-reverse px-3 py-2 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                <div className={`w-3 h-3 rounded-full ${getStatusColor(currentAgent.status)}`}></div>
                <span>{t(`agent.${currentAgent.status}`)}</span>
                <ChevronDown size={16} />
              </button>

              {statusMenuOpen && (
                <div className="absolute right-0 rtl:right-auto rtl:left-0 mt-2 w-48 bg-white dark:bg-gray-800 rounded-md shadow-lg ring-1 ring-black ring-opacity-5 z-50">
                  <div className="py-1">
                    {['online', 'away', 'busy', 'offline'].map((status) => (
                      <button
                        key={status}
                        onClick={() => handleStatusChange(status as any)}
                        className="flex items-center w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                      >
                        <div className={`w-3 h-3 rounded-full ${getStatusColor(status)} mr-3 rtl:mr-0 rtl:ml-3`}></div>
                        {t(`agent.${status}`)}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          <button
            onClick={onToggleNotifications}
            className="relative p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
            aria-label={t('accessibility.notificationCenter')}
          >
            <Bell size={20} />
            {unreadCount > 0 && (
              <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full h-5 w-5 flex items-center justify-center">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </button>

          <button
            onClick={() => dispatch(toggleDarkMode())}
            className="p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
            aria-label={t('settings.theme')}
          >
            {isDarkMode ? <Sun size={20} /> : <Moon size={20} />}
          </button>

          <button
            onClick={handleLanguageChange}
            className="p-2 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700"
            aria-label={t('settings.language')}
          >
            <Globe size={20} />
          </button>

          <div className="relative">
            <button
              onClick={() => setUserMenuOpen(!userMenuOpen)}
              className="flex items-center space-x-3 rtl:space-x-reverse text-sm rounded-full focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              aria-label={t('accessibility.userMenu')}
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
                    href="/profile"
                    className="flex items-center px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <User size={16} className="mr-3 rtl:mr-0 rtl:ml-3" />
                    {t('navigation.profile')}
                  </a>
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
    </header>
  );
};
