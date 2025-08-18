import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { tenantApi } from "../../services/api";

interface Tenant {
  id: string;
  name: string;
  subdomain: string;
  status: "Active" | "Inactive" | "Suspended";
  subscriptionPlan: string;
  createdAt: string;
  userCount: number;
  conversationCount: number;
}

interface TenantState {
  tenants: Tenant[];
  currentTenant: Tenant | null;
  isLoading: boolean;
  error: string | null;
  totalCount: number;
  currentPage: number;
  pageSize: number;
}

const initialState: TenantState = {
  tenants: [],
  currentTenant: null,
  isLoading: false,
  error: null,
  totalCount: 0,
  currentPage: 1,
  pageSize: 20,
};

export const fetchTenants = createAsyncThunk(
  "tenant/fetchTenants",
  async ({ page, pageSize, search }: { page: number; pageSize: number; search?: string }) => {
    const response = await tenantApi.getTenants(page, pageSize, search);
    return response;
  }
);

export const createTenant = createAsyncThunk("tenant/createTenant", async (tenantData: Partial<Tenant>) => {
  const response = await tenantApi.createTenant(tenantData);
  return response;
});

export const updateTenant = createAsyncThunk("tenant/updateTenant", async ({ id, data }: { id: string; data: Partial<Tenant> }) => {
  const response = await tenantApi.updateTenant(id, data);
  return response;
});

export const deleteTenant = createAsyncThunk("tenant/deleteTenant", async (id: string) => {
  await tenantApi.deleteTenant(id);
  return id;
});

const tenantSlice = createSlice({
  name: "tenant",
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
      .addCase(fetchTenants.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchTenants.fulfilled, (state, action) => {
        state.isLoading = false;
        state.tenants = action.payload.data;
        state.totalCount = action.payload.totalCount;
        state.currentPage = action.payload.currentPage;
      })
      .addCase(fetchTenants.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch tenants";
      })
      .addCase(createTenant.fulfilled, (state, action) => {
        state.tenants.unshift(action.payload);
        state.totalCount += 1;
      })
      .addCase(updateTenant.fulfilled, (state, action) => {
        const index = state.tenants.findIndex((t) => t.id === action.payload.id);
        if (index !== -1) {
          state.tenants[index] = action.payload;
        }
      })
      .addCase(deleteTenant.fulfilled, (state, action) => {
        state.tenants = state.tenants.filter((t) => t.id !== action.payload);
        state.totalCount -= 1;
      });
  },
});

export const { clearError, setCurrentPage, setPageSize } = tenantSlice.actions;
export default tenantSlice.reducer;
