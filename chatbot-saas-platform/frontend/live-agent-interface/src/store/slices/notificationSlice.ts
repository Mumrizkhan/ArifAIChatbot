import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";

// Add API base URL configuration
const API_BASE_URL = import.meta.env.API_BASE_URL || "http://localhost:8000";

export interface Notification {
  id: string;
  type: "info" | "success" | "warning" | "error";
  title: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
  actionUrl?: string;
  actionText?: string;
  persistent?: boolean;
}

interface NotificationState {
  notifications: Notification[];
  unreadCount: number;
  soundEnabled: boolean;
  desktopEnabled: boolean;
  isLoading: boolean;
  error: string | null;
}

const initialState: NotificationState = {
  notifications: [],
  unreadCount: 0,
  soundEnabled: true,
  desktopEnabled: false,
  isLoading: false,
  error: null,
};

// Async thunks for API operations
export const fetchNotifications = createAsyncThunk("notifications/fetchAll", async (params?: { limit?: number; unreadOnly?: boolean }) => {
  const queryParams = new URLSearchParams();
  if (params?.limit) queryParams.append("limit", params.limit.toString());
  if (params?.unreadOnly) queryParams.append("unreadOnly", "true");

  const response = await fetch(`${API_BASE_URL}/notification/notifications?${queryParams}`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch notifications");
  }

  return response.json();
});

export const markNotificationAsReadAPI = createAsyncThunk("notifications/markAsReadAPI", async (notificationId: string) => {
  const response = await fetch(`${API_BASE_URL}/notification/notifications/${notificationId}/read`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to mark notification as read");
  }

  return notificationId;
});

export const markAllNotificationsAsReadAPI = createAsyncThunk("notifications/markAllAsReadAPI", async () => {
  const response = await fetch(`${API_BASE_URL}/notification/notifications/mark-all-read`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to mark all notifications as read");
  }

  return response.json();
});

export const deleteNotificationAPI = createAsyncThunk("notifications/deleteAPI", async (notificationId: string) => {
  const response = await fetch(`${API_BASE_URL}/notification/notifications/${notificationId}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to delete notification");
  }

  return notificationId;
});

export const updateNotificationSettings = createAsyncThunk(
  "notifications/updateSettings",
  async (settings: { soundEnabled: boolean; desktopEnabled: boolean }) => {
    const response = await fetch(`${API_BASE_URL}/notification/notifications/settings`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      throw new Error("Failed to update notification settings");
    }

    return settings;
  }
);

export const sendNotificationToAgent = createAsyncThunk(
  "notifications/sendToAgent",
  async ({ agentId, notification }: { agentId: string; notification: Omit<Notification, "id" | "timestamp" | "isRead"> }) => {
    const response = await fetch(`${API_BASE_URL}/notification/notifications/send`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({ agentId, ...notification }),
    });

    if (!response.ok) {
      throw new Error("Failed to send notification");
    }

    return response.json();
  }
);

const notificationSlice = createSlice({
  name: "notifications",
  initialState,
  reducers: {
    addNotification: (state, action: PayloadAction<Omit<Notification, "id" | "timestamp" | "isRead">>) => {
      const notification: Notification = {
        ...action.payload,
        id: `notification_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        timestamp: new Date(),
        isRead: false,
      };

      state.notifications.unshift(notification);
      state.unreadCount += 1;

      if (state.notifications.length > 100) {
        state.notifications = state.notifications.slice(0, 100);
      }
    },
    markAsRead: (state, action: PayloadAction<string>) => {
      const notification = state.notifications.find((n) => n.id === action.payload);
      if (notification && !notification.isRead) {
        notification.isRead = true;
        state.unreadCount = Math.max(0, state.unreadCount - 1);
      }
    },
    markAllAsRead: (state) => {
      state.notifications.forEach((notification) => {
        notification.isRead = true;
      });
      state.unreadCount = 0;
    },
    removeNotification: (state, action: PayloadAction<string>) => {
      const index = state.notifications.findIndex((n) => n.id === action.payload);
      if (index !== -1) {
        const notification = state.notifications[index];
        if (!notification.isRead) {
          state.unreadCount = Math.max(0, state.unreadCount - 1);
        }
        state.notifications.splice(index, 1);
      }
    },
    clearAllNotifications: (state) => {
      state.notifications = [];
      state.unreadCount = 0;
    },
    setSoundEnabled: (state, action: PayloadAction<boolean>) => {
      state.soundEnabled = action.payload;
    },
    setDesktopEnabled: (state, action: PayloadAction<boolean>) => {
      state.desktopEnabled = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch notifications
      .addCase(fetchNotifications.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchNotifications.fulfilled, (state, action) => {
        state.isLoading = false;
        state.notifications = action.payload.notifications || action.payload;
        state.unreadCount = action.payload.unreadCount || state.notifications.filter((n) => !n.isRead).length;
      })
      .addCase(fetchNotifications.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch notifications";
      })

      // Mark as read API
      .addCase(markNotificationAsReadAPI.fulfilled, (state, action) => {
        const notification = state.notifications.find((n) => n.id === action.payload);
        if (notification && !notification.isRead) {
          notification.isRead = true;
          state.unreadCount = Math.max(0, state.unreadCount - 1);
        }
      })

      // Mark all as read API
      .addCase(markAllNotificationsAsReadAPI.fulfilled, (state) => {
        state.notifications.forEach((notification) => {
          notification.isRead = true;
        });
        state.unreadCount = 0;
      })

      // Delete notification API
      .addCase(deleteNotificationAPI.fulfilled, (state, action) => {
        const index = state.notifications.findIndex((n) => n.id === action.payload);
        if (index !== -1) {
          const notification = state.notifications[index];
          if (!notification.isRead) {
            state.unreadCount = Math.max(0, state.unreadCount - 1);
          }
          state.notifications.splice(index, 1);
        }
      })

      // Update settings
      .addCase(updateNotificationSettings.fulfilled, (state, action) => {
        state.soundEnabled = action.payload.soundEnabled;
        state.desktopEnabled = action.payload.desktopEnabled;
      })

      // Send notification
      .addCase(sendNotificationToAgent.fulfilled, (state, action) => {
        // Optionally add the sent notification to local state
        if (action.payload) {
          state.notifications.unshift({
            ...action.payload,
            timestamp: new Date(action.payload.timestamp),
          });
        }
      });
  },
});

export const {
  addNotification,
  markAsRead,
  markAllAsRead,
  removeNotification,
  clearAllNotifications,
  setSoundEnabled,
  setDesktopEnabled,
  clearError,
} = notificationSlice.actions;

export default notificationSlice.reducer;
