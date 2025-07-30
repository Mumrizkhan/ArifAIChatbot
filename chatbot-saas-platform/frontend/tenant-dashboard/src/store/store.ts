import { configureStore } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import tenantReducer from './slices/tenantSlice';
import chatbotReducer from './slices/chatbotSlice';
import teamReducer from './slices/teamSlice';
import analyticsReducer from './slices/analyticsSlice';
import themeReducer from './slices/themeSlice';
import subscriptionReducer from './slices/subscriptionSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    tenant: tenantReducer,
    chatbot: chatbotReducer,
    team: teamReducer,
    analytics: analyticsReducer,
    theme: themeReducer,
    subscription: subscriptionReducer,
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
