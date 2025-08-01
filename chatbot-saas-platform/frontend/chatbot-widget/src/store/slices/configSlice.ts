import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface WidgetConfig {
  tenantId: string;
  apiUrl: string;
  websocketUrl: string;
  features: {
    fileUpload: boolean;
    voiceMessages: boolean;
    typing: boolean;
    readReceipts: boolean;
    agentHandoff: boolean;
    conversationRating: boolean;
    conversationTranscript: boolean;
    proactiveMessages: boolean;
  };
  behavior: {
    autoOpen: boolean;
    autoOpenDelay: number;
    showWelcomeMessage: boolean;
    persistConversation: boolean;
    maxFileSize: number;
    allowedFileTypes: string[];
    maxMessageLength: number;
  };
  security: {
    allowedDomains: string[];
    requireAuth: boolean;
    encryptMessages: boolean;
  };
  analytics: {
    trackEvents: boolean;
    trackUserJourney: boolean;
    customEvents: string[];
  };
}

interface ConfigState {
  widget: WidgetConfig;
  isInitialized: boolean;
  isEmbedded: boolean;
  parentDomain: string;
  userId?: string;
  sessionId: string;
  metadata: Record<string, any>;
}

const defaultConfig: WidgetConfig = {
  tenantId: '',
  apiUrl: '/api',
  websocketUrl: '/ws',
  features: {
    fileUpload: true,
    voiceMessages: false,
    typing: true,
    readReceipts: true,
    agentHandoff: true,
    conversationRating: true,
    conversationTranscript: true,
    proactiveMessages: false,
  },
  behavior: {
    autoOpen: false,
    autoOpenDelay: 3000,
    showWelcomeMessage: true,
    persistConversation: true,
    maxFileSize: 10 * 1024 * 1024, // 10MB
    allowedFileTypes: ['image/*', '.pdf', '.doc', '.docx', '.txt'],
    maxMessageLength: 1000,
  },
  security: {
    allowedDomains: [],
    requireAuth: false,
    encryptMessages: false,
  },
  analytics: {
    trackEvents: true,
    trackUserJourney: false,
    customEvents: [],
  },
};

const initialState: ConfigState = {
  widget: defaultConfig,
  isInitialized: false,
  isEmbedded: false,
  parentDomain: '',
  sessionId: '',
  metadata: {},
};

const configSlice = createSlice({
  name: 'config',
  initialState,
  reducers: {
    initializeWidget: (state, action: PayloadAction<{
      tenantId: string;
      config?: {
        apiUrl?: string;
        websocketUrl?: string;
        features?: Partial<WidgetConfig['features']>;
        behavior?: Partial<WidgetConfig['behavior']>;
      };
      userId?: string;
      metadata?: Record<string, any>;
    }>) => {
      const { tenantId, config, userId, metadata } = action.payload;
      state.widget.tenantId = tenantId;
      if (config) {
        if (config.apiUrl) state.widget.apiUrl = config.apiUrl;
        if (config.websocketUrl) state.widget.websocketUrl = config.websocketUrl;
        if (config.features) {
          state.widget.features = { ...state.widget.features, ...config.features };
        }
        if (config.behavior) {
          state.widget.behavior = { ...state.widget.behavior, ...config.behavior };
        }
      }
      state.userId = userId;
      state.metadata = metadata || {};
      state.sessionId = generateSessionId();
      state.isInitialized = true;
      state.parentDomain = window.location.hostname;
      state.isEmbedded = window !== window.top;
    },
    updateConfig: (state, action: PayloadAction<Partial<WidgetConfig>>) => {
      state.widget = { ...state.widget, ...action.payload };
    },
    setUserId: (state, action: PayloadAction<string>) => {
      state.userId = action.payload;
    },
    updateMetadata: (state, action: PayloadAction<Record<string, any>>) => {
      state.metadata = { ...state.metadata, ...action.payload };
    },
    setFeatureEnabled: (state, action: PayloadAction<{ feature: keyof WidgetConfig['features']; enabled: boolean }>) => {
      state.widget.features[action.payload.feature] = action.payload.enabled;
    },
    updateBehavior: (state, action: PayloadAction<Partial<WidgetConfig['behavior']>>) => {
      state.widget.behavior = { ...state.widget.behavior, ...action.payload };
    },
    trackEvent: (state, action: PayloadAction<{ event: string; data?: any }>) => {
      if (state.widget.analytics.trackEvents) {
        const eventData = {
          ...action.payload,
          timestamp: new Date().toISOString(),
          sessionId: state.sessionId,
          tenantId: state.widget.tenantId,
          userId: state.userId,
        };
        
        if (typeof window !== 'undefined' && (window as any).gtag) {
          (window as any).gtag('event', action.payload.event, eventData);
        }
        
        console.log('Analytics Event:', eventData);
      }
    },
  },
});

function generateSessionId(): string {
  return `session_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
}

export const {
  initializeWidget,
  updateConfig,
  setUserId,
  updateMetadata,
  setFeatureEnabled,
  updateBehavior,
  trackEvent,
} = configSlice.actions;

export default configSlice.reducer;
