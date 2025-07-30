import React from 'react';
import { useLocation, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSelector, useDispatch } from 'react-redux';
import { RootState, AppDispatch } from '../../store/store';
import { toggleSidebar } from '../../store/slices/themeSlice';
import { cn } from '../../lib/utils';
import { Button } from '../ui/button';
import {
  LayoutDashboard,
  Users,
  Building2,
  BarChart3,
  CreditCard,
  Settings,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';

const Sidebar: React.FC = () => {
  const { t } = useTranslation();
  const location = useLocation();
  const dispatch = useDispatch<AppDispatch>();
  const { sidebarCollapsed } = useSelector((state: RootState) => state.theme);

  const menuItems = [
    {
      icon: LayoutDashboard,
      label: t('navigation.dashboard'),
      path: '/dashboard',
    },
    {
      icon: Building2,
      label: t('navigation.tenants'),
      path: '/tenants',
    },
    {
      icon: Users,
      label: t('navigation.users'),
      path: '/users',
    },
    {
      icon: BarChart3,
      label: t('navigation.analytics'),
      path: '/analytics',
    },
    {
      icon: CreditCard,
      label: t('navigation.subscriptions'),
      path: '/subscriptions',
    },
    {
      icon: Settings,
      label: t('navigation.settings'),
      path: '/settings',
    },
  ];

  return (
    <div className={cn(
      'fixed left-0 top-0 h-full bg-card border-r border-border transition-all duration-300 z-50',
      sidebarCollapsed ? 'w-16' : 'w-64'
    )}>
      <div className="flex items-center justify-between p-4 border-b border-border">
        {!sidebarCollapsed && (
          <h1 className="text-xl font-bold text-foreground">
            ChatBot Admin
          </h1>
        )}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => dispatch(toggleSidebar())}
          className="p-2"
        >
          {sidebarCollapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
        </Button>
      </div>

      <nav className="mt-6">
        <ul className="space-y-2 px-3">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;

            return (
              <li key={item.path}>
                <Link
                  to={item.path}
                  className={cn(
                    'flex items-center gap-3 px-3 py-2 rounded-lg transition-colors',
                    'hover:bg-accent hover:text-accent-foreground',
                    isActive && 'bg-primary text-primary-foreground',
                    sidebarCollapsed && 'justify-center'
                  )}
                >
                  <Icon size={20} />
                  {!sidebarCollapsed && (
                    <span className="font-medium">{item.label}</span>
                  )}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>
    </div>
  );
};

export default Sidebar;
