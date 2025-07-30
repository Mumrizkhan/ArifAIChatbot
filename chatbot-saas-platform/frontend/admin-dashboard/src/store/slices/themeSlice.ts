import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface ThemeState {
  mode: 'light' | 'dark' | 'system';
  language: 'en' | 'ar';
  direction: 'ltr' | 'rtl';
  primaryColor: string;
  sidebarCollapsed: boolean;
}

const initialState: ThemeState = {
  mode: (localStorage.getItem('theme') as 'light' | 'dark' | 'system') || 'system',
  language: (localStorage.getItem('language') as 'en' | 'ar') || 'en',
  direction: (localStorage.getItem('direction') as 'ltr' | 'rtl') || 'ltr',
  primaryColor: localStorage.getItem('primaryColor') || '#3b82f6',
  sidebarCollapsed: localStorage.getItem('sidebarCollapsed') === 'true',
};

const themeSlice = createSlice({
  name: 'theme',
  initialState,
  reducers: {
    setThemeMode: (state, action: PayloadAction<'light' | 'dark' | 'system'>) => {
      state.mode = action.payload;
      localStorage.setItem('theme', action.payload);
    },
    setLanguage: (state, action: PayloadAction<'en' | 'ar'>) => {
      state.language = action.payload;
      state.direction = action.payload === 'ar' ? 'rtl' : 'ltr';
      localStorage.setItem('language', action.payload);
      localStorage.setItem('direction', state.direction);
    },
    setPrimaryColor: (state, action: PayloadAction<string>) => {
      state.primaryColor = action.payload;
      localStorage.setItem('primaryColor', action.payload);
    },
    toggleSidebar: (state) => {
      state.sidebarCollapsed = !state.sidebarCollapsed;
      localStorage.setItem('sidebarCollapsed', state.sidebarCollapsed.toString());
    },
    setSidebarCollapsed: (state, action: PayloadAction<boolean>) => {
      state.sidebarCollapsed = action.payload;
      localStorage.setItem('sidebarCollapsed', action.payload.toString());
    },
  },
});

export const { setThemeMode, setLanguage, setPrimaryColor, toggleSidebar, setSidebarCollapsed } = themeSlice.actions;
export default themeSlice.reducer;
