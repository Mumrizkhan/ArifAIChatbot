import { configureStore } from '@reduxjs/toolkit';
import authSlice from './slices/authSlice';
import tenantSlice from './slices/tenantSlice';
import userSlice from './slices/userSlice';
import analyticsSlice from './slices/analyticsSlice';
import themeSlice from './slices/themeSlice';

export const store = configureStore({
  reducer: {
    auth: authSlice,
    tenant: tenantSlice,
    user: userSlice,
    analytics: analyticsSlice,
    theme: themeSlice,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST'],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
