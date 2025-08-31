import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://api-stg-arif.tetco.sa";
export interface TenantNotification {
  id: string;
  type: "info" | "warning" | "error" | "success";
  title: string;
  message: string;
  timestamp: string; // Changed from Date to string (ISO format)
  tenantId: string;
}

export interface AnalyticsData {
  conversations: {
    total: number;
    active: number;
    resolved: number;
    averageDuration: number;
    satisfactionScore: number;
  };
  agents: {
    online: number;
    busy: number;
    averageResponseTime: number;
    utilization: number;
  };
  customers: {
    total: number;
    returning: number;
    newToday: number;
    averageRating: number;
  };
  performance: {
    resolutionRate: number;
    firstResponseTime: number;
    escalationRate: number;
    botAccuracy: number;
  };
  trends: {
    conversationVolume: Array<{ date: string; count: number }>;
    satisfactionTrend: Array<{ date: string; score: number }>;
    responseTimeTrend: Array<{ date: string; time: number }>;
  };
  realtime: {
    activeConversations: number;
    waitingCustomers: number;
    onlineAgents: number;
    currentLoad: number;
  };
}

interface AnalyticsState {
  data: AnalyticsData | null;
  isLoading: boolean;
  error: string | null;
  dateRange: {
    start: string; // Changed from Date to string (ISO format)
    end: string; // Changed from Date to string (ISO format)
  };
  refreshInterval: number;
  isSignalRConnected: boolean;
  tenantNotifications: TenantNotification[];
}

const initialState: AnalyticsState = {
  data: null,
  isLoading: false,
  error: null,
  dateRange: {
    start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days ago
    end: new Date().toISOString(),
  },
  refreshInterval: 30000, // 30 seconds
  isSignalRConnected: false,
  tenantNotifications: [],
};

export const fetchAnalytics = createAsyncThunk("analytics/fetchData", async (params: { startDate: Date; endDate: Date }) => {
  const response = await fetch(`${API_BASE_URL}/analytics/analytics`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
    body: JSON.stringify({
      startDate: params.startDate.toISOString(),
      endDate: params.endDate.toISOString(),
    }),
  });

  if (!response.ok) {
    throw new Error("Failed to fetch analytics data");
  }

  return response.json();
});

export const fetchRealtimeAnalytics = createAsyncThunk("analytics/fetchRealtime", async () => {
  const response = await fetch(`${API_BASE_URL}/analytics/analytics/realtime`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch realtime analytics");
  }

  return response.json();
});

export const exportAnalytics = createAsyncThunk(
  "analytics/export",
  async (params: { format: "csv" | "pdf" | "excel"; startDate: Date; endDate: Date }) => {
    const response = await fetch(`${API_BASE_URL}/analytics/analytics/export`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({
        format: params.format,
        startDate: params.startDate.toISOString(),
        endDate: params.endDate.toISOString(),
      }),
    });

    if (!response.ok) {
      throw new Error("Failed to export analytics data");
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `analytics-${params.format}-${Date.now()}.${params.format}`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    return { success: true };
  }
);

const analyticsSlice = createSlice({
  name: "analytics",
  initialState,
  reducers: {
    setDateRange: (state, action: PayloadAction<{ start: string; end: string }>) => {
      state.dateRange = action.payload;
    },
    updateRealtimeData: (state, action: PayloadAction<AnalyticsData["realtime"]>) => {
      if (state.data) {
        state.data.realtime = action.payload;
      }
    },
    setRefreshInterval: (state, action: PayloadAction<number>) => {
      state.refreshInterval = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
    updateAnalyticsDataRealtime: (state, action: PayloadAction<AnalyticsData>) => {
      state.data = action.payload;
    },
    setSignalRConnectionStatus: (state, action: PayloadAction<boolean>) => {
      state.isSignalRConnected = action.payload;
    },
    addTenantNotification: (state, action: PayloadAction<TenantNotification>) => {
      state.tenantNotifications.unshift(action.payload);
      if (state.tenantNotifications.length > 50) {
        state.tenantNotifications = state.tenantNotifications.slice(0, 50);
      }
    },
    removeTenantNotification: (state, action: PayloadAction<string>) => {
      state.tenantNotifications = state.tenantNotifications.filter((notification) => notification.id !== action.payload);
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchAnalytics.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAnalytics.fulfilled, (state, action) => {
        state.isLoading = false;
        state.data = action.payload;
      })
      .addCase(fetchAnalytics.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch analytics data";
      })
      .addCase(fetchRealtimeAnalytics.fulfilled, (state, action) => {
        if (state.data) {
          state.data.realtime = action.payload;
        }
      })
      .addCase(exportAnalytics.rejected, (state, action) => {
        state.error = action.error.message || "Failed to export analytics data";
      });
  },
});

export const {
  setDateRange,
  updateRealtimeData,
  setRefreshInterval,
  clearError,
  updateAnalyticsDataRealtime,
  setSignalRConnectionStatus,
  addTenantNotification,
  removeTenantNotification,
} = analyticsSlice.actions;
export default analyticsSlice.reducer;
