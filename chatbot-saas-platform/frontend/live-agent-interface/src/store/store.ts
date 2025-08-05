import { configureStore } from "@reduxjs/toolkit";
import authReducer from "./slices/authSlice";
import agentReducer from "./slices/agentSlice";
import conversationReducer from "./slices/conversationSlice";
import themeReducer from "./slices/themeSlice";
import notificationReducer from "./slices/notificationSlice";
import selectedConversationReducer from "./slices/selectedConversationSlice";

export const store = configureStore({
  reducer: {
    auth: authReducer,
    agent: agentReducer,
    conversations: conversationReducer,
    selectedConversation: selectedConversationReducer,
    theme: themeReducer,
    notifications: notificationReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ["persist/PERSIST"],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
