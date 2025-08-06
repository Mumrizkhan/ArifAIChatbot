import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { tenantApi } from '../../services/api';

interface SystemSettings {
  companyName: string;
  adminEmail: string;
  timezone: string;
  language: string;
  maintenanceMode: boolean;
  userRegistration: boolean;
  compactMode: boolean;
  animations: boolean;
  twoFactor: boolean;
  sessionTimeout: number;
  passwordExpiry: number;
}

interface NotificationSettings {
  email: boolean;
  push: boolean;
  sms: boolean;
  inApp: boolean;
}

interface IntegrationSettings {
  sendgrid: { connected: boolean; apiKey?: string };
  twilio: { connected: boolean; accountSid?: string; authToken?: string };
  stripe: { connected: boolean; publicKey?: string; secretKey?: string };
  slack: { connected: boolean; webhookUrl?: string };
}

interface SettingsState {
  systemSettings: SystemSettings;
  notificationSettings: NotificationSettings;
  integrationSettings: IntegrationSettings;
  isLoading: boolean;
  error: string | null;
  isSaving: boolean;
}

const initialState: SettingsState = {
  systemSettings: {
    companyName: 'Arif Platform',
    adminEmail: '',
    timezone: 'utc',
    language: 'en',
    maintenanceMode: false,
    userRegistration: true,
    compactMode: false,
    animations: true,
    twoFactor: false,
    sessionTimeout: 30,
    passwordExpiry: 90,
  },
  notificationSettings: {
    email: true,
    push: true,
    sms: false,
    inApp: true,
  },
  integrationSettings: {
    sendgrid: { connected: true },
    twilio: { connected: true },
    stripe: { connected: true },
    slack: { connected: false },
  },
  isLoading: false,
  error: null,
  isSaving: false,
};

export const fetchSettings = createAsyncThunk(
  'settings/fetchSettings',
  async (tenantId: string) => {
    const response = await tenantApi.getTenantSettings(tenantId);
    return response;
  }
);

export const updateSettings = createAsyncThunk(
  'settings/updateSettings',
  async ({ tenantId, settings }: { tenantId: string; settings: Record<string, any> }) => {
    await tenantApi.updateTenantSettings(tenantId, { settings });
    return settings;
  }
);

const settingsSlice = createSlice({
  name: 'settings',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    updateSystemSettings: (state, action: PayloadAction<Partial<SystemSettings>>) => {
      state.systemSettings = { ...state.systemSettings, ...action.payload };
    },
    updateNotificationSettings: (state, action: PayloadAction<Partial<NotificationSettings>>) => {
      state.notificationSettings = { ...state.notificationSettings, ...action.payload };
    },
    updateIntegrationSettings: (state, action: PayloadAction<Partial<IntegrationSettings>>) => {
      state.integrationSettings = { ...state.integrationSettings, ...action.payload };
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
        const settings = action.payload.settings || {};
        if (settings.system) {
          state.systemSettings = { ...state.systemSettings, ...settings.system };
        }
        if (settings.notifications) {
          state.notificationSettings = { ...state.notificationSettings, ...settings.notifications };
        }
        if (settings.integrations) {
          state.integrationSettings = { ...state.integrationSettings, ...settings.integrations };
        }
      })
      .addCase(fetchSettings.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch settings';
      })
      .addCase(updateSettings.pending, (state) => {
        state.isSaving = true;
        state.error = null;
      })
      .addCase(updateSettings.fulfilled, (state) => {
        state.isSaving = false;
      })
      .addCase(updateSettings.rejected, (state, action) => {
        state.isSaving = false;
        state.error = action.error.message || 'Failed to update settings';
      });
  },
});

export const { 
  clearError, 
  updateSystemSettings, 
  updateNotificationSettings, 
  updateIntegrationSettings 
} = settingsSlice.actions;
export default settingsSlice.reducer;
