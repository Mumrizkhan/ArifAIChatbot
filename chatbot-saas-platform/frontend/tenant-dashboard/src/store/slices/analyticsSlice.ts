import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';

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
    start: Date;
    end: Date;
  };
  refreshInterval: number;
}

const initialState: AnalyticsState = {
  data: null,
  isLoading: false,
  error: null,
  dateRange: {
    start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
    end: new Date(),
  },
  refreshInterval: 30000, // 30 seconds
};

export const fetchAnalytics = createAsyncThunk(
  'analytics/fetchData',
  async (params: { startDate: Date; endDate: Date }) => {
    const response = await fetch('/api/analytics', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify({
        startDate: params.startDate.toISOString(),
        endDate: params.endDate.toISOString(),
      }),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch analytics data');
    }

    return response.json();
  }
);

export const fetchRealtimeAnalytics = createAsyncThunk(
  'analytics/fetchRealtime',
  async () => {
    const response = await fetch('/api/analytics/realtime', {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch realtime analytics');
    }

    return response.json();
  }
);

export const exportAnalytics = createAsyncThunk(
  'analytics/export',
  async (params: { format: 'csv' | 'pdf' | 'excel'; startDate: Date; endDate: Date }) => {
    const response = await fetch('/api/analytics/export', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify({
        format: params.format,
        startDate: params.startDate.toISOString(),
        endDate: params.endDate.toISOString(),
      }),
    });

    if (!response.ok) {
      throw new Error('Failed to export analytics data');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
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
  name: 'analytics',
  initialState,
  reducers: {
    setDateRange: (state, action: PayloadAction<{ start: Date; end: Date }>) => {
      state.dateRange = action.payload;
    },
    updateRealtimeData: (state, action: PayloadAction<AnalyticsData['realtime']>) => {
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
        state.error = action.error.message || 'Failed to fetch analytics data';
      })
      .addCase(fetchRealtimeAnalytics.fulfilled, (state, action) => {
        if (state.data) {
          state.data.realtime = action.payload;
        }
      })
      .addCase(exportAnalytics.rejected, (state, action) => {
        state.error = action.error.message || 'Failed to export analytics data';
      });
  },
});

export const { setDateRange, updateRealtimeData, setRefreshInterval, clearError } = analyticsSlice.actions;
export default analyticsSlice.reducer;
