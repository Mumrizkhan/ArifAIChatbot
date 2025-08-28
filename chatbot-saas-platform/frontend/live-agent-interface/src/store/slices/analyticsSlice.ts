import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import { RootState } from "../store";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://api-stg-arif.tetco.sa";

// Thunk for performance data
export const fetchPerformanceData = createAsyncThunk(
  "analytics/fetchPerformanceData",
  async ({ dateFrom, dateTo }: { dateFrom: string; dateTo: string }) => {
    const res = await fetch(`${API_BASE_URL}/agent/Agents/performance?startDate=${dateFrom}&endDate=${dateTo}`);
    if (!res.ok) throw new Error("Failed to fetch analytics performance data");
    return await res.json();
  }
);

// Thunk for general analytics data
export const fetchAnalyticsData = createAsyncThunk(
  "analytics/fetchAnalyticsData",
  async ({ dateFrom, dateTo }: { dateFrom: string; dateTo: string }) => {
    const res = await fetch(`${API_BASE_URL}/analytics/analytics?dateFrom=${dateFrom}&dateTo=${dateTo}`);
    if (!res.ok) throw new Error("Failed to fetch analytics summary");
    return await res.json();
  }
);

// Thunk for goals data
export const fetchGoalsData = createAsyncThunk("analytics/fetchGoalsData", async (id: string) => {
  const res = await fetch(`${API_BASE_URL}/analytics/analytics/goals:${id}`);
  if (!res.ok) throw new Error("Failed to fetch analytics goals");
  return await res.json();
});

interface AnalyticsState {
  performanceData: any[];
  analyticsData: any;
  goalsData: any;
  isLoading: boolean;
  error: string | null;
}

const initialState: AnalyticsState = {
  performanceData: [],
  analyticsData: null,
  goalsData: null,
  isLoading: false,
  error: null,
};

const analyticsSlice = createSlice({
  name: "analytics",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
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
        state.error = action.error.message || "Error";
      })
      // Analytics Summary Data
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
        state.error = action.error.message || "Error";
      })
      // Goals Data
      .addCase(fetchGoalsData.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchGoalsData.fulfilled, (state, action) => {
        state.goalsData = action.payload;
        state.isLoading = false;
      })
      .addCase(fetchGoalsData.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Error";
      });
  },
});

export default analyticsSlice.reducer;
export const selectPerformanceData = (state: RootState) => state.analytics.performanceData;
export const selectAnalyticsData = (state: RootState) => state.analytics.analyticsData;
export const selectGoalsData = (state: RootState) => state.analytics.goalsData;
export const selectAnalyticsLoading = (state: RootState) => state.analytics.isLoading;
