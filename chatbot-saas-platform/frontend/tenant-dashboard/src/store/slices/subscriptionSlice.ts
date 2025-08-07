import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { SubscriptionService } from "../../services/subscriptionService";
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
export interface Plan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  interval: "month" | "year";
  features: string[];
  limits: {
    conversations: number;
    agents: number;
    storage: number;
    apiCalls: number;
  };
  isPopular: boolean;
}

export interface Subscription {
  id: string;
  planId: string;
  plan: Plan;
  status: "active" | "canceled" | "past_due" | "unpaid";
  currentPeriodStart: Date;
  currentPeriodEnd: Date;
  cancelAtPeriodEnd: boolean;
  trialEnd?: Date;
}

export interface Usage {
  conversations: number;
  agents: number;
  storage: number;
  apiCalls: number;
  period: {
    start: Date;
    end: Date;
  };
}

export interface Invoice {
  id: string;
  number: string;
  amount: number;
  currency: string;
  status: "paid" | "pending" | "failed";
  dueDate: Date;
  paidAt?: Date;
  downloadUrl?: string;
}

interface SubscriptionState {
  subscription: Subscription | null;
  plans: Plan[];
  usage: Usage | null;
  invoices: Invoice[];
  isLoading: boolean;
  error: string | null;
}

const initialState: SubscriptionState = {
  subscription: null,
  plans: [],
  usage: null,
  invoices: [],
  isLoading: false,
  error: null,
};

export const fetchSubscription = createAsyncThunk("subscription/fetchSubscription", async () => {
  const response = await SubscriptionService.getCurrentSubscription();
  return response.data;
});

export const fetchPlans = createAsyncThunk("subscription/fetchPlans", async () => {
  const response = await SubscriptionService.getPlans();
  return response.data;
});

export const fetchUsage = createAsyncThunk("subscription/fetchUsage", async () => {
  const response = await SubscriptionService.getUsageMetrics();
  return response.data;
});

export const fetchInvoices = createAsyncThunk("subscription/fetchInvoices", async () => {
  const response = await SubscriptionService.getInvoices();
  return response.data.invoices || response.data;
});

export const changePlan = createAsyncThunk("subscription/changePlan", async ({ planId }: { planId: string; prorate: boolean }) => {
  const response = await SubscriptionService.updateSubscription({ planId });
  return response.data;
});

export const cancelSubscription = createAsyncThunk("subscription/cancel", async ({ immediately }: { immediately: boolean }) => {
  const response = await SubscriptionService.cancelSubscription(!immediately);
  return response.data;
});

export const reactivateSubscription = createAsyncThunk("subscription/reactivate", async () => {
  const response = await SubscriptionService.reactivateSubscription();
  return response.data;
});

const subscriptionSlice = createSlice({
  name: "subscription",
  initialState,
  reducers: {
    updateUsage: (state, action: PayloadAction<Partial<Usage>>) => {
      if (state.usage) {
        state.usage = { ...state.usage, ...action.payload };
      }
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchSubscription.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchSubscription.fulfilled, (state, action) => {
        state.isLoading = false;
        state.subscription = action.payload as any;
      })
      .addCase(fetchSubscription.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch subscription";
      })
      .addCase(fetchPlans.fulfilled, (state, action) => {
        state.plans = action.payload as any;
      })
      .addCase(fetchUsage.fulfilled, (state, action) => {
        state.usage = action.payload as any;
      })
      .addCase(fetchInvoices.fulfilled, (state, action) => {
        state.invoices = action.payload as any;
      })
      .addCase(changePlan.fulfilled, (state, action) => {
        state.subscription = action.payload as any;
      })
      .addCase(cancelSubscription.fulfilled, (state, action) => {
        state.subscription = action.payload as any;
      })
      .addCase(reactivateSubscription.fulfilled, (state, action) => {
        state.subscription = action.payload as any;
      });
  },
});

export const { updateUsage, clearError } = subscriptionSlice.actions;
export default subscriptionSlice.reducer;
