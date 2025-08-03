import React, { createContext, useContext, useEffect } from 'react';
import { useAppSelector } from '../../hooks/redux';

interface ThemeContextType {
  theme: any;
  branding: any;
  isDarkMode: boolean;
  isRTL: boolean;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
};

interface ThemeProviderProps {
  children: React.ReactNode;
}

export const ThemeProvider: React.FC<ThemeProviderProps> = ({ children }) => {
  const { theme, branding, isDarkMode, isRTL, customCSS } = useAppSelector((state) => state.theme);

  useEffect(() => {
    const root = document.documentElement;
    
    Object.entries(theme).forEach(([key, value]) => {
      const cssVar = `--${key.replace(/([A-Z])/g, '-$1').toLowerCase()}`;
      root.style.setProperty(cssVar, value as string);
    });

    if (customCSS) {
      const existingStyle = document.getElementById('tenant-custom-css');
      if (existingStyle) {
        existingStyle.remove();
      }

      const style = document.createElement('style');
      style.id = 'tenant-custom-css';
      style.textContent = customCSS;
      document.head.appendChild(style);
    }

    if (isDarkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [theme, customCSS, isDarkMode]);

  const value = {
    theme,
    branding,
    isDarkMode,
    isRTL,
  };

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
};
