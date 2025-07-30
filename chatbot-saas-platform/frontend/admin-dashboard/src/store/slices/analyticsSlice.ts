import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { analyticsApi } from '../../services/api';

interface DashboardStats {
  totalTenants: number;
  totalUsers: number;
  totalConversations: number;
  activeAgents: number;
  monthlyRevenue: number;
  growthRate: number;
}

interface ConversationMetrics {
  totalConversations: number;
  averageDuration: number;
  resolutionRate: number;
  satisfactionScore: number;
  dailyData: Array<{
    date: string;
    conversations: number;
    resolved: number;
    avgDuration: number;
  }>;
}

interface AgentMetrics {
  totalAgents: number;
  activeAgents: number;
  averageResponseTime: number;
  averageRating: number;
  topPerformers: Array<{
    id: string;
    name: string;
    rating: number;
    conversationsHandled: number;
  }>;
}

interface AnalyticsState {
  dashboardStats: DashboardStats | null;
  conversationMetrics: ConversationMetrics | null;
  agentMetrics: AgentMetrics | null;
  botMetrics: any | null;
  customReports: any[];
  isLoading: boolean;
  error: string | null;
  selectedTimeRange: string;
  selectedTenant: string | null;
}

const initialState: AnalyticsState = {
  dashboardStats: null,
  conversationMetrics: null,
  agentMetrics: null,
  botMetrics: null,
  customReports: [],
  isLoading: false,
  error: null,
  selectedTimeRange: '7d',
  selectedTenant: null,
};

export const fetchDashboardStats = createAsyncThunk(
  'analytics/fetchDashboardStats',
  async () => {
    const response = await analyticsApi.getDashboardStats();
    return response;
  }
);

export const fetchConversationMetrics = createAsyncThunk(
  'analytics/fetchConversationMetrics',
  async ({ timeRange, tenantId }: { timeRange: string; tenantId?: string }) => {
    const response = await analyticsApi.getConversationMetrics(timeRange, tenantId);
    return response;
  }
);

export const fetchAgentMetrics = createAsyncThunk(
  'analytics/fetchAgentMetrics',
  async ({ timeRange, tenantId }: { timeRange: string; tenantId?: string }) => {
    const response = await analyticsApi.getAgentMetrics(timeRange, tenantId);
    return response;
  }
);

export const fetchBotMetrics = createAsyncThunk(
  'analytics/fetchBotMetrics',
  async ({ timeRange, tenantId }: { timeRange: string; tenantId?: string }) => {
    const response = await analyticsApi.getBotMetrics(timeRange, tenantId);
    return response;
  }
);

export const generateCustomReport = createAsyncThunk(
  'analytics/generateCustomReport',
  async (reportConfig: any) => {
    const response = await analyticsApi.getCustomReport(reportConfig);
    return response;
  }
);

const analyticsSlice = createSlice({
  name: 'analytics',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setTimeRange: (state, action: PayloadAction<string>) => {
      state.selectedTimeRange = action.payload;
    },
    setSelectedTenant: (state, action: PayloadAction<string | null>) => {
      state.selectedTenant = action.payload;
    },
    clearCustomReports: (state) => {
      state.customReports = [];
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchDashboardStats.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchDashboardStats.fulfilled, (state, action) => {
        state.isLoading = false;
        state.dashboardStats = action.payload;
      })
      .addCase(fetchDashboardStats.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch dashboard stats';
      })
      .addCase(fetchConversationMetrics.fulfilled, (state, action) => {
        state.conversationMetrics = action.payload;
      })
      .addCase(fetchAgentMetrics.fulfilled, (state, action) => {
        state.agentMetrics = action.payload;
      })
      .addCase(fetchBotMetrics.fulfilled, (state, action) => {
        state.botMetrics = action.payload;
      })
      .addCase(generateCustomReport.fulfilled, (state, action) => {
        state.customReports.push(action.payload);
      });
  },
});

export const { clearError, setTimeRange, setSelectedTenant, clearCustomReports } = analyticsSlice.actions;
export default analyticsSlice.reducer;
