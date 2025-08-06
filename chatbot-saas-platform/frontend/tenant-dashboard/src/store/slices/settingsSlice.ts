import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { apiClient } from '../../services/api';

interface TenantSettings {
  tenantName: string;
  tenantDescription: string;
  contactEmail: string;
  contactPhone: string;
  timezone: string;
  language: string;
  dateFormat: string;
  currency: string;
  enableNotifications: boolean;
  enableEmailNotifications: boolean;
  enableSmsNotifications: boolean;
  enablePushNotifications: boolean;
  notificationFrequency: string;
  dataRetentionDays: number;
  enableDataExport: boolean;
  enableAuditLogs: boolean;
  sessionTimeout: number;
  enableTwoFactor: boolean;
  allowedDomains: string;
  ipWhitelist: string;
}

interface SettingsState {
  settings: TenantSettings;
  isLoading: boolean;
  error: string | null;
  isSaving: boolean;
}

const initialState: SettingsState = {
  settings: {
    tenantName: '',
    tenantDescription: '',
    contactEmail: '',
    contactPhone: '',
    timezone: 'UTC',
    language: 'en',
    dateFormat: 'MM/DD/YYYY',
    currency: 'USD',
    enableNotifications: true,
    enableEmailNotifications: true,
    enableSmsNotifications: false,
    enablePushNotifications: true,
    notificationFrequency: 'immediate',
    dataRetentionDays: 365,
    enableDataExport: true,
    enableAuditLogs: true,
    sessionTimeout: 30,
    enableTwoFactor: false,
    allowedDomains: '',
    ipWhitelist: '',
  },
  isLoading: false,
  error: null,
  isSaving: false,
};

export const fetchSettings = createAsyncThunk(
  'settings/fetchSettings',
  async () => {
    const response = await apiClient.get('/tenant-management/tenant/settings');
    return response.data;
  }
);

export const updateSettings = createAsyncThunk(
  'settings/updateSettings',
  async (settings: Partial<TenantSettings>) => {
    const response = await apiClient.put('/tenant-management/tenant/settings', settings);
    return response.data;
  }
);

const settingsSlice = createSlice({
  name: 'settings',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    updateSettingsLocal: (state, action: PayloadAction<Partial<TenantSettings>>) => {
      state.settings = { ...state.settings, ...action.payload };
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchSettings.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchSettings.fulfilled, (state, action) => {
        state.isLoading = false;
        state.settings = { ...state.settings, ...(action.payload || {}) };
      })
      .addCase(fetchSettings.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch settings';
      })
      .addCase(updateSettings.pending, (state) => {
        state.isSaving = true;
        state.error = null;
      })
      .addCase(updateSettings.fulfilled, (state, action) => {
        state.isSaving = false;
        state.settings = { ...state.settings, ...(action.payload || {}) };
      })
      .addCase(updateSettings.rejected, (state, action) => {
        state.isSaving = false;
        state.error = action.error.message || 'Failed to update settings';
      });
  },
});

export const { clearError, updateSettingsLocal } = settingsSlice.actions;
export default settingsSlice.reducer;
