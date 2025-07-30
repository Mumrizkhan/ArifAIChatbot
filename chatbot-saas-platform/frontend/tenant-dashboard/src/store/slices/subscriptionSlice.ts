import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';

export interface Plan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  interval: 'month' | 'year';
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
  status: 'active' | 'canceled' | 'past_due' | 'unpaid';
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
  status: 'paid' | 'pending' | 'failed';
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

export const fetchSubscription = createAsyncThunk(
  'subscription/fetchSubscription',
  async () => {
    const response = await fetch('/api/subscription', {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch subscription');
    }

    return response.json();
  }
);

export const fetchPlans = createAsyncThunk(
  'subscription/fetchPlans',
  async () => {
    const response = await fetch('/api/subscription/plans', {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch plans');
    }

    return response.json();
  }
);

export const fetchUsage = createAsyncThunk(
  'subscription/fetchUsage',
  async () => {
    const response = await fetch('/api/subscription/usage', {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch usage');
    }

    return response.json();
  }
);

export const fetchInvoices = createAsyncThunk(
  'subscription/fetchInvoices',
  async () => {
    const response = await fetch('/api/payments/invoices', {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch invoices');
    }

    return response.json();
  }
);

export const changePlan = createAsyncThunk(
  'subscription/changePlan',
  async ({ planId, prorate }: { planId: string; prorate: boolean }) => {
    const response = await fetch('/api/subscription/change-plan', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify({ planId, prorate }),
    });

    if (!response.ok) {
      throw new Error('Failed to change plan');
    }

    return response.json();
  }
);

export const cancelSubscription = createAsyncThunk(
  'subscription/cancel',
  async ({ immediately }: { immediately: boolean }) => {
    const response = await fetch('/api/subscription/cancel', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify({ immediately }),
    });

    if (!response.ok) {
      throw new Error('Failed to cancel subscription');
    }

    return response.json();
  }
);

export const reactivateSubscription = createAsyncThunk(
  'subscription/reactivate',
  async () => {
    const response = await fetch('/api/subscription/reactivate', {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to reactivate subscription');
    }

    return response.json();
  }
);

const subscriptionSlice = createSlice({
  name: 'subscription',
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
        state.subscription = action.payload;
      })
      .addCase(fetchSubscription.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch subscription';
      })
      .addCase(fetchPlans.fulfilled, (state, action) => {
        state.plans = action.payload;
      })
      .addCase(fetchUsage.fulfilled, (state, action) => {
        state.usage = action.payload;
      })
      .addCase(fetchInvoices.fulfilled, (state, action) => {
        state.invoices = action.payload;
      })
      .addCase(changePlan.fulfilled, (state, action) => {
        state.subscription = action.payload;
      })
      .addCase(cancelSubscription.fulfilled, (state, action) => {
        state.subscription = action.payload;
      })
      .addCase(reactivateSubscription.fulfilled, (state, action) => {
        state.subscription = action.payload;
      });
  },
});

export const { updateUsage, clearError } = subscriptionSlice.actions;
export default subscriptionSlice.reducer;
