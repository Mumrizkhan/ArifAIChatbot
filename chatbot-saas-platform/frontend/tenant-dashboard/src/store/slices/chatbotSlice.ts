import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://api-stg.arif.sa";
export interface ChatbotConfig {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  appearance: {
    position: "bottom-right" | "bottom-left" | "top-right" | "top-left";
    size: "small" | "medium" | "large";
    primaryColor: string;
    secondaryColor: string;
    borderRadius: string;
    animation: "slide" | "fade" | "bounce" | "none";
  };
  behavior: {
    autoOpen: boolean;
    autoOpenDelay: number;
    showWelcomeMessage: boolean;
    welcomeMessage: string;
    placeholderText: string;
    maxFileSize: number;
    allowedFileTypes: string[];
  };
  features: {
    fileUpload: boolean;
    voiceMessages: boolean;
    typing: boolean;
    readReceipts: boolean;
    agentHandoff: boolean;
    conversationRating: boolean;
    conversationTranscript: boolean;
  };
  aiSettings: {
    model: string;
    temperature: number;
    maxTokens: number;
    systemPrompt: string;
    fallbackMessage: string;
  };
  integrations: {
    knowledgeBase: boolean;
    crm: boolean;
    analytics: boolean;
  };
}

interface ChatbotState {
  configs: ChatbotConfig[];
  activeConfig: ChatbotConfig | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: ChatbotState = {
  configs: [],
  activeConfig: null,
  isLoading: false,
  error: null,
};

export const fetchChatbotConfigs = createAsyncThunk("chatbot/fetchConfigs", async () => {
  const response = await fetch(`${API_BASE_URL}/chat/chatbot/configs`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch chatbot configs");
  }

  return response.json();
});

export const createChatbotConfig = createAsyncThunk("chatbot/createConfig", async (config: Omit<ChatbotConfig, "id">) => {
  const response = await fetch(`${API_BASE_URL}/chat/chatbot/configs`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
    body: JSON.stringify(config),
  });

  if (!response.ok) {
    throw new Error("Failed to create chatbot config");
  }

  return response.json();
});

export const updateChatbotConfig = createAsyncThunk(
  "chatbot/updateConfig",
  async ({ id, config }: { id: string; config: Partial<ChatbotConfig> }) => {
    const response = await fetch(`${API_BASE_URL}/chat/chatbot/configs/${id}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify(config),
    });

    if (!response.ok) {
      throw new Error("Failed to update chatbot config");
    }

    return response.json();
  }
);

export const deleteChatbotConfig = createAsyncThunk("chatbot/deleteConfig", async (id: string) => {
  const response = await fetch(`${API_BASE_URL}/chat/chatbot/configs/${id}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to delete chatbot config");
  }

  return id;
});

const chatbotSlice = createSlice({
  name: "chatbot",
  initialState,
  reducers: {
    setActiveConfig: (state, action: PayloadAction<ChatbotConfig>) => {
      state.activeConfig = action.payload;
    },
    updateConfigLocal: (state, action: PayloadAction<{ id: string; config: Partial<ChatbotConfig> }>) => {
      const { id, config } = action.payload;
      const existingConfig = state.configs.find((c) => c.id === id);
      if (existingConfig) {
        Object.assign(existingConfig, config);
      }
      if (state.activeConfig && state.activeConfig.id === id) {
        Object.assign(state.activeConfig, config);
      }
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchChatbotConfigs.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchChatbotConfigs.fulfilled, (state, action) => {
        state.isLoading = false;
        state.configs = action.payload;
      })
      .addCase(fetchChatbotConfigs.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch chatbot configs";
      })
      .addCase(createChatbotConfig.fulfilled, (state, action) => {
        state.configs.push(action.payload);
      })
      .addCase(updateChatbotConfig.fulfilled, (state, action) => {
        const index = state.configs.findIndex((c) => c.id === action.payload.id);
        if (index !== -1) {
          state.configs[index] = action.payload;
        }
        if (state.activeConfig && state.activeConfig.id === action.payload.id) {
          state.activeConfig = action.payload;
        }
      })
      .addCase(deleteChatbotConfig.fulfilled, (state, action) => {
        state.configs = state.configs.filter((c) => c.id !== action.payload);
        if (state.activeConfig && state.activeConfig.id === action.payload) {
          state.activeConfig = null;
        }
      });
  },
});

export const { setActiveConfig, updateConfigLocal, clearError } = chatbotSlice.actions;
export default chatbotSlice.reducer;
