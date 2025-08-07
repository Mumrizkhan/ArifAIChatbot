import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
export interface TenantSettings {
  id: string;
  name: string;
  domain: string;
  logo?: string;
  primaryColor: string;
  secondaryColor: string;
  language: "en" | "ar";
  timezone: string;
  businessHours: {
    start: string;
    end: string;
    days: string[];
  };
  features: {
    aiChatbot: boolean;
    liveAgent: boolean;
    analytics: boolean;
    workflows: boolean;
    knowledgeBase: boolean;
  };
  integrations: {
    crm?: string;
    helpdesk?: string;
    analytics?: string;
  };
  customization: {
    welcomeMessage: string;
    chatbotName: string;
    theme: "light" | "dark" | "auto";
    customCSS?: string;
  };
}

interface TenantState {
  settings: TenantSettings | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: TenantState = {
  settings: null,
  isLoading: false,
  error: null,
};

export const fetchTenantSettings = createAsyncThunk("tenant/fetchSettings", async (tenantId: string) => {
  const response = await fetch(`${API_BASE_URL}/tenant-management/tenants/${tenantId}/settings`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch tenant settings");
  }

  return response.json();
});

export const updateTenantSettings = createAsyncThunk(
  "tenant/updateSettings",
  async ({ tenantId, settings }: { tenantId: string; settings: Partial<TenantSettings> }) => {
    const response = await fetch(`${API_BASE_URL}/tenant-management/tenants/${tenantId}/settings`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify(settings),
    });

    if (!response.ok) {
      throw new Error("Failed to update tenant settings");
    }

    return response.json();
  }
);

const tenantSlice = createSlice({
  name: "tenant",
  initialState,
  reducers: {
    updateSettingsLocal: (state, action: PayloadAction<Partial<TenantSettings>>) => {
      if (state.settings) {
        state.settings = { ...state.settings, ...action.payload };
      }
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchTenantSettings.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchTenantSettings.fulfilled, (state, action) => {
        state.isLoading = false;
        state.settings = action.payload;
      })
      .addCase(fetchTenantSettings.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch tenant settings";
      })
      .addCase(updateTenantSettings.fulfilled, (state, action) => {
        state.settings = action.payload;
      });
  },
});

export const { updateSettingsLocal, clearError } = tenantSlice.actions;
export default tenantSlice.reducer;
