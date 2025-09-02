import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { subscriptionApi } from "../../services/api";

interface Plan {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  currency: string;
  type: string;
  features: string[];
  limits: Record<string, number>;
  trialDays: number;
  isActive: boolean;
  isPublic: boolean;
  sortOrder: number;
  price?: number;
  subscribers?: number;
}

interface Subscription {
  id: string;
  tenantId: string;
  planId: string;
  planName: string;
  status: string;
  startDate: string;
  endDate?: string;
  nextBillingDate: string;
  billingCycle: string;
  amount: number;
  currency: string;
  isTrialActive: boolean;
  trialEndDate?: string;
  tenantName?: string;
  plan?: string;
  mrr?: number;
  nextBilling?: string;
  users?: number;
}

interface BillingStats {
  monthlyRecurringRevenue: number;
  annualRecurringRevenue: number;
  activeSubscriptions: number;
  trialSubscriptions: number;
  cancelledSubscriptions: number;
  churnRate: number;
  averageRevenuePerUser: number;
  subscriptionsByPlan: Record<string, number>;
  revenueByPlan: Record<string, number>;
}

interface SubscriptionState {
  plans: Plan[];
  subscriptions: Subscription[];
  billingStats: BillingStats | null;
  isLoading: boolean;
  error: string | null;
  totalCount: number;
  currentPage: number;
  pageSize: number;
}

const initialState: SubscriptionState = {
  plans: [],
  subscriptions: [],
  billingStats: null,
  isLoading: false,
  error: null,
  totalCount: 0,
  currentPage: 1,
  pageSize: 20,
};

export const fetchPlans = createAsyncThunk("subscription/fetchPlans", async () => {
  const response = await subscriptionApi.getPlans();
  console.log("fetchPlans API response:", response);
  return response;
});

export const fetchSubscriptions = createAsyncThunk(
  "subscription/fetchSubscriptions",
  async ({ page, pageSize }: { page: number; pageSize: number }) => {
    const response = await subscriptionApi.getSubscriptions(page, pageSize);
    return response;
  }
);

export const fetchBillingStats = createAsyncThunk("subscription/fetchBillingStats", async () => {
  const response = await subscriptionApi.getBillingStats();
  return response;
});

export const createSubscription = createAsyncThunk("subscription/createSubscription", async (subscriptionData: any) => {
  const response = await subscriptionApi.createSubscription(subscriptionData);
  return response;
});

export const updateSubscription = createAsyncThunk("subscription/updateSubscription", async ({ id, data }: { id: string; data: any }) => {
  const response = await subscriptionApi.updateSubscription(id, data);
  return response;
});

export const cancelSubscription = createAsyncThunk("subscription/cancelSubscription", async (id: string) => {
  await subscriptionApi.cancelSubscription(id);
  return id;
});

export const createPlan = createAsyncThunk("subscription/createPlan", async (planData: any) => {
  // Extract the plan from the wrapper object if it exists
  const plan = planData.plan || planData;
  const response = await subscriptionApi.createPlan(plan);
  return response;
});

const subscriptionSlice = createSlice({
  name: "subscription",
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setCurrentPage: (state, action: PayloadAction<number>) => {
      state.currentPage = action.payload;
    },
    setPageSize: (state, action: PayloadAction<number>) => {
      state.pageSize = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPlans.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchPlans.fulfilled, (state, action) => {
        state.isLoading = false;
        console.log("fetchPlans fulfilled, payload:", action.payload);
        state.plans = action.payload;
      })
      .addCase(fetchPlans.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch plans";
      })
      .addCase(fetchSubscriptions.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchSubscriptions.fulfilled, (state, action) => {
        state.isLoading = false;
        state.subscriptions = action.payload.data;
        state.totalCount = action.payload.totalCount;
        state.currentPage = action.payload.currentPage;
      })
      .addCase(fetchSubscriptions.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch subscriptions";
      })
      .addCase(fetchBillingStats.fulfilled, (state, action) => {
        state.billingStats = action.payload;
      })
      .addCase(createSubscription.fulfilled, (state, action) => {
        state.subscriptions.unshift(action.payload);
        state.totalCount += 1;
      })
      .addCase(updateSubscription.fulfilled, (state, action) => {
        const index = state.subscriptions.findIndex((s) => s.id === action.payload.id);
        if (index !== -1) {
          state.subscriptions[index] = action.payload;
        }
      })
      .addCase(cancelSubscription.fulfilled, (state, action) => {
        const index = state.subscriptions.findIndex((s) => s.id === action.payload);
        if (index !== -1) {
          state.subscriptions[index].status = "Cancelled";
        }
      })
      .addCase(createPlan.fulfilled, (state, action) => {
        state.plans.unshift(action.payload);
      });
  },
});

export const { clearError, setCurrentPage, setPageSize } = subscriptionSlice.actions;
export default subscriptionSlice.reducer;
