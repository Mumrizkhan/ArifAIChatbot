import { configureStore } from '@reduxjs/toolkit';
import chatReducer from './slices/chatSlice';
import themeReducer from './slices/themeSlice';
import configReducer from './slices/configSlice';

export const store = configureStore({
  reducer: {
    chat: chatReducer,
    theme: themeReducer,
    config: configReducer,
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
