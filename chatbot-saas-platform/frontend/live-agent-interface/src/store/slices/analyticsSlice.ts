import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import { RootState } from "../store";
import { 
  analyticsApi, 
  DashboardStats, 
  ConversationMetrics, 
  AgentMetrics, 
  PerformanceMetrics, 
  RealtimeAnalytics,
  AnalyticsData
} from '../../services/analyticsApi';

export const fetchDashboardStats = createAsyncThunk(
  "analytics/fetchDashboardStats",
  async () => {
    const response = await analyticsApi.getDashboardStats();
    if (!response.success) throw new Error(response.message);
    return response.data;
  }
);

export const fetchPerformanceData = createAsyncThunk(
  "analytics/fetchPerformanceData",
  async ({ dateFrom, dateTo }: { dateFrom?: string; dateTo?: string } = {}) => {
    const response = await analyticsApi.getPerformanceMetrics(dateFrom, dateTo);
    if (!response.success) throw new Error(response.message);
    return response.data;
  }
);

export const fetchConversationMetrics = createAsyncThunk(
  "analytics/fetchConversationMetrics",
  async ({ timeRange, tenantId }: { timeRange?: string; tenantId?: string } = {}) => {
    const response = await analyticsApi.getConversationMetrics(timeRange, tenantId);
    if (!response.success) throw new Error(response.message);
    return response.data;
  }
);

export const fetchAgentMetrics = createAsyncThunk(
  "analytics/fetchAgentMetrics",
  async ({ timeRange, tenantId }: { timeRange?: string; tenantId?: string } = {}) => {
    const response = await analyticsApi.getAgentMetrics(timeRange, tenantId);
    if (!response.success) throw new Error(response.message);
    return response.data;
  }
);

export const fetchRealtimeAnalytics = createAsyncThunk(
  "analytics/fetchRealtimeAnalytics",
  async () => {
    const response = await analyticsApi.getRealtimeAnalytics();
    if (!response.success) throw new Error(response.message);
    return response.data;
  }
);

export const fetchAnalyticsData = createAsyncThunk(
  "analytics/fetchAnalyticsData",
  async ({ dateFrom, dateTo, tenantId }: { dateFrom?: string; dateTo?: string; tenantId?: string } = {}) => {
    const response = await analyticsApi.getAnalytics(dateFrom, dateTo, tenantId);
    if (!response.success) throw new Error(response.message);
    return response.data;
  }
);

interface AnalyticsState {
  dashboardStats: DashboardStats | null;
  performanceData: PerformanceMetrics | null;
  conversationMetrics: ConversationMetrics | null;
  agentMetrics: AgentMetrics | null;
  realtimeAnalytics: RealtimeAnalytics | null;
  analyticsData: AnalyticsData | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: AnalyticsState = {
  dashboardStats: null,
  performanceData: null,
  conversationMetrics: null,
  agentMetrics: null,
  realtimeAnalytics: null,
  analyticsData: null,
  isLoading: false,
  error: null,
};

const analyticsSlice = createSlice({
  name: "analytics",
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    updateRealtimeAnalytics: (state, action) => {
      state.realtimeAnalytics = action.payload;
    },
    updateDashboardStats: (state, action) => {
      state.dashboardStats = action.payload;
    },
    updateConversationMetrics: (state, action) => {
      state.conversationMetrics = action.payload;
    },
    updateAgentMetrics: (state, action) => {
      state.agentMetrics = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchDashboardStats.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchDashboardStats.fulfilled, (state, action) => {
        state.dashboardStats = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchDashboardStats.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error fetching dashboard stats";
      })
      // Performance Data
      .addCase(fetchPerformanceData.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchPerformanceData.fulfilled, (state, action) => {
        state.performanceData = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchPerformanceData.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error fetching performance data";
      })
      .addCase(fetchConversationMetrics.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchConversationMetrics.fulfilled, (state, action) => {
        state.conversationMetrics = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchConversationMetrics.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error fetching conversation metrics";
      })
      .addCase(fetchAgentMetrics.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAgentMetrics.fulfilled, (state, action) => {
        state.agentMetrics = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchAgentMetrics.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error fetching agent metrics";
      })
      .addCase(fetchRealtimeAnalytics.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchRealtimeAnalytics.fulfilled, (state, action) => {
        state.realtimeAnalytics = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchRealtimeAnalytics.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error fetching realtime analytics";
      })
      // Analytics Data
      .addCase(fetchAnalyticsData.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAnalyticsData.fulfilled, (state, action) => {
        state.analyticsData = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchAnalyticsData.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error fetching analytics data";
      });
  },
});

export const { 
  clearError, 
  updateRealtimeAnalytics, 
  updateDashboardStats, 
  updateConversationMetrics, 
  updateAgentMetrics 
} = analyticsSlice.actions;

export default analyticsSlice.reducer;

export const selectDashboardStats = (state: RootState) => state.analytics.dashboardStats;
export const selectPerformanceData = (state: RootState) => state.analytics.performanceData;
export const selectConversationMetrics = (state: RootState) => state.analytics.conversationMetrics;
export const selectAgentMetrics = (state: RootState) => state.analytics.agentMetrics;
export const selectRealtimeAnalytics = (state: RootState) => state.analytics.realtimeAnalytics;
export const selectAnalyticsData = (state: RootState) => state.analytics.analyticsData;
export const selectAnalyticsLoading = (state: RootState) => state.analytics.isLoading;
export const selectAnalyticsError = (state: RootState) => state.analytics.error;
