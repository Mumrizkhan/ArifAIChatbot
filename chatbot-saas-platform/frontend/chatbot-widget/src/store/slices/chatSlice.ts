import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { apiClient } from "../../services/apiClient";
import { ensureISOString } from "../../utils/timestamp";
export interface Message {
  id: string;
  content: string;
  sender: "user" | "bot" | "agent";
  type: "text" | "file" | "image" | "typing" | "feedback";
  timestamp: string; // ISO string format for Redux serialization
  isRead?: boolean;
  readAt?: string;
  metadata?: {
    fileName?: string;
    fileSize?: number;
    fileType?: string;
    imageUrl?: string;
    senderId?: string;
    senderName?: string;
    systemMessage?: boolean;
    agentId?: string;
    agentName?: string;
    feedbackRequest?: boolean;
    ratingScale?: {
      min: number;
      max: number;
      labels: string[];
    };
    feedbackPrompt?: string;
  };
}

export interface Conversation {
  id: string;
  messages: Message[];
  status: "active" | "waiting" | "ended";
  assignedAgent?: {
    id: string;
    name: string;
    avatar?: string;
  };
  startedAt: string; // ISO string format for Redux serialization
  endedAt?: string; // ISO string format for Redux serialization
  rating?: number; // Customer rating (1-5)
  feedback?: string; // Customer feedback text
}

interface ChatState {
  isOpen: boolean;
  isMinimized: boolean;
  currentConversation: Conversation | null;
  isConnected: boolean;
  isTyping: boolean;
  typingUser?: string;
  unreadCount: number;
  isLoading: boolean;
  error: string | null;
  connectionStatus: "connecting" | "connected" | "disconnected" | "error";
  lastSeen?: string; // ISO string format for Redux serialization
}

const initialState: ChatState = {
  isOpen: false,
  isMinimized: false,
  currentConversation: null,
  isConnected: false,
  isTyping: false,
  unreadCount: 0,
  isLoading: false,
  error: null,
  connectionStatus: "disconnected",
};

interface ApiResponse {
  userMessage?: unknown;
  botMessage?: unknown;
  success?: boolean;
}

interface ErrorPayload {
  message?: string;
}

interface ConversationPayload {
  id: string;
  messages?: unknown[];
  status?: string;
  startedAt?: string;
}

interface ConfigState {
  widget: {
    tenantId: string;
    apiUrl: string;
    websocketUrl: string;
    authToken?: string;
    features: Record<string, boolean>;
    behavior: Record<string, unknown>;
    security: Record<string, unknown>;
    analytics: Record<string, unknown>;
    predefinedIntents: unknown[];
    customerName: string;
    userName: string;
    userEmail: string;
    language: string;
  };
  isInitialized: boolean;
  isEmbedded: boolean;
  parentDomain: string;
  userId?: string;
  sessionId: string;
  metadata: Record<string, unknown>;
}

interface RootState {
  chat: ChatState;
  config: ConfigState;
}

export const sendMessage = createAsyncThunk(
  "chat/sendMessage",
  async (payload: { content: string; type: "text" | "file" }, { getState, rejectWithValue }) => {
    try {
      const { chat } = getState() as RootState;
      const conversationId = chat.currentConversation?.id;

      // Conversation should already exist when chat is opened
      if (!conversationId) {
        throw new Error("No active conversation found. Please open the chat widget first.");
      }

      const res = await apiClient.post("/chat/chat/messages", {
        conversationId,
        content: payload.content,
        type: payload.type,
      });

      return res;
    } catch (e: unknown) {
      const error = e as { response?: { data?: unknown }; message?: string };
      return rejectWithValue(error?.response?.data ?? { message: error?.message || "Send failed" });
    }
  }
);

interface CustomerInfo {
  customerName?: string;
  userEmail?: string;
  language?: string;
  userId?: string;
}

export const fetchCustomerInfo = createAsyncThunk("chat/fetchCustomerInfo", async (payload: { tenantId: string; userId?: string }, { getState }) => {
  console.log("🚀 Fetching customer info for:", payload);

  const state = getState() as RootState;
  const authToken = state.config?.widget?.authToken;

  // Make API call to get customer information
  const data = await apiClient.get(`/chat/customers/${payload.userId || "current"}`, {
    headers: authToken ? { Authorization: `Bearer ${authToken}` } : {},
    params: { tenantId: payload.tenantId },
  });

  console.log("✅ Customer info fetched:", data);

  return {
    customerName: data.customerName || data.name || data.displayName,
    userEmail: data.email,
    language: data.preferredLanguage || data.language,
    userId: data.id || data.userId,
  } as CustomerInfo;
});

export const startConversation = createAsyncThunk("chat/startConversation", async (tenantId: string, { getState, dispatch }) => {
  console.log("🚀 Starting conversation for tenant:", tenantId);

  const state = getState() as RootState;
  let customerName = state.config?.widget?.customerName || state.config?.widget?.userName || state.config?.widget?.userEmail || "Anonymous User";
  let language = state.config?.widget?.language || "en";

  // Optionally fetch customer info from API if customerName is not set or is "Anonymous User"
  if (!state.config?.widget?.customerName || customerName === "Anonymous User") {
    try {
      const userId = state.config?.userId;
      if (userId) {
        console.log("🔍 Fetching customer info from API...");
        // const customerInfo = (await (dispatch as any)(fetchCustomerInfo({ tenantId, userId })).unwrap()) as CustomerInfo;
        // customerName = customerInfo?.customerName || customerName;
        // language = customerInfo?.language || language;
        console.log("✅ Customer info updated:", { customerName, language });
      }
    } catch (error) {
      console.warn("⚠️ Failed to fetch customer info, using fallback:", error);
      // Continue with existing values
    }
  }

  const data = await apiClient.post("/chat/chat/conversations", {
    tenantId,
    customerName,
    language,
  });

  console.log("✅ Conversation created via API:", data);

  const conversation = {
    id: data.id,
    messages: [],
    status: "active" as const,
    startedAt: new Date(data.startedAt || new Date()).toISOString(),
  } as Conversation;

  // Setup SignalR connection and event handlers for this conversation
  try {
    const { signalRService } = await import("../../services/websocket");
    const state = getState() as RootState;
    const authToken = state.config?.widget?.authToken;

    if (authToken) {
      // First establish SignalR connection if not already connected
      if (!signalRService.isConnected()) {
        console.log("SignalR: Establishing connection for conversation:", data.id);
        const connected = await signalRService.connect(tenantId, authToken, data.id);
        if (connected) {
          console.log("✅ SignalR connection established with conversation:", data.id);
        } else {
          console.warn("⚠️ Failed to establish SignalR connection");
        }
      } else {
        // If already connected, just join the conversation
        console.log("SignalR: Already connected, joining conversation:", data.id);
        const started = await signalRService.startConversation(data.id);
        if (started) {
          console.log("✅ SignalR conversation setup completed for:", data.id);
        } else {
          console.warn("⚠️ Failed to setup SignalR for conversation:", data.id);
        }
      }
    } else {
      console.warn("⚠️ No authToken available - SignalR connection will not be established");
    }
  } catch (error) {
    console.error("❌ Error setting up SignalR for conversation:", error);
    // Don't fail the conversation creation if SignalR setup fails
  }

  return conversation;
});

export const requestHumanAgent = createAsyncThunk("chat/requestHumanAgent", async (conversationId: string) => {
  return await apiClient.post(`/chat/chat/conversations/${conversationId}/escalate`);
});

export const markMessageAsReadAPI = createAsyncThunk("chat/markMessageAsReadAPI", async ({ messageId }: { messageId: string }) => {
  return await apiClient.put(`/chat/chat/messages/${messageId}/mark-read`);
});

export const submitRating = createAsyncThunk(
  "chat/submitRating",
  async ({ conversationId, messageId, rating, feedback }: { conversationId: string; messageId: string; rating: number; feedback?: string }) => {
    const ratingData = {
      conversationId,
      messageId,
      rating,
      feedback,
      submittedAt: new Date().toISOString(),
    };

    return await apiClient.post(`/chat/conversations/${conversationId}/rating`, ratingData);
  }
);

const chatSlice = createSlice({
  name: "chat",
  initialState,
  reducers: {
    toggleChat: (state) => {
      state.isOpen = !state.isOpen;
      if (state.isOpen) {
        state.unreadCount = 0;
        state.isMinimized = false;
      }
    },
    minimizeChat: (state) => {
      state.isMinimized = true;
    },
    maximizeChat: (state) => {
      state.isMinimized = false;
    },
    closeChat: (state) => {
      state.isOpen = false;
      state.isMinimized = false;
    },
    addMessage: (state, action: PayloadAction<Message>) => {
      if (state.currentConversation) {
        const exists = state.currentConversation.messages.some((msg) => msg.id === action.payload.id);
        if (!exists) {
          state.currentConversation.messages.push(action.payload);
        }
      }
    },
    setTyping: (state, action: PayloadAction<{ isTyping: boolean; user?: string }>) => {
      state.isTyping = action.payload.isTyping;
      state.typingUser = action.payload.user;
    },
    setConnectionStatus: (state, action: PayloadAction<ChatState["connectionStatus"]>) => {
      state.connectionStatus = action.payload;
      state.isConnected = action.payload === "connected";
    },
    updateConversationStatus: (state, action: PayloadAction<Conversation["status"]>) => {
      if (state.currentConversation) {
        state.currentConversation.status = action.payload;
      }
    },
    assignAgent: (state, action: PayloadAction<{ id: string; name: string; avatar?: string }>) => {
      if (state.currentConversation) {
        state.currentConversation.assignedAgent = action.payload;
      }
    },
    clearError: (state) => {
      state.error = null;
    },
    markAsRead: (state) => {
      state.unreadCount = 0;
      state.lastSeen = new Date().toISOString();
    },
    markMessageAsRead: (state, action: PayloadAction<{ messageId: string }>) => {
      if (state.currentConversation) {
        const message = state.currentConversation.messages.find((msg) => msg.id === action.payload.messageId);
        if (message) {
          message.isRead = true;
          message.readAt = new Date().toISOString();
        }
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(sendMessage.pending, (state) => {
        state.isLoading = true;
        state.error = null;

        // Add a pending bot message only if there are no existing pending messages
        if (state.currentConversation) {
          const hasPendingBotMessage = state.currentConversation.messages.some((msg) => msg.id.startsWith("pending-bot-"));

          if (!hasPendingBotMessage) {
            state.currentConversation.messages.push({
              id: `pending-bot-${Date.now()}`, // Temporary ID
              content: "...", // Placeholder content
              sender: "bot",
              type: "text",
              timestamp: new Date().toISOString(),
              metadata: {},
            });
          }
        }
      })
      .addCase(sendMessage.fulfilled, (state, action) => {
        state.isLoading = false;

        const body = action.payload as ApiResponse;
        if (!state.currentConversation || !body) return;

        // Remove the pending bot message
        state.currentConversation.messages = state.currentConversation.messages.filter((msg) => !msg.id.startsWith("pending-bot-"));

        // Note: Bot message will be added via SignalR ReceiveMessage event to prevent duplicates
        // No need to add bot message here as it will come through real-time events
      })
      .addCase(sendMessage.rejected, (state, action) => {
        state.isLoading = false;
        state.error = (action.payload as ErrorPayload)?.message || action.error.message || "Failed to send message";

        // Remove the pending bot message
        if (state.currentConversation) {
          state.currentConversation.messages = state.currentConversation.messages.filter((msg) => !msg.id.startsWith("pending-bot-"));
        }
      })
      .addCase(startConversation.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(startConversation.fulfilled, (state, action) => {
        state.isLoading = false;
        const conv = action.payload as ConversationPayload;

        // Preserve existing messages if they already exist
        state.currentConversation = {
          id: conv.id || `conv-${Date.now()}`,
          status: (conv.status as Conversation["status"]) || "active",
          startedAt: ensureISOString(conv.startedAt),
          messages: state.currentConversation?.messages.length
            ? state.currentConversation.messages
            : Array.isArray(conv?.messages)
            ? (conv.messages as Message[])
            : [],
        };
        state.error = null;
      })
      .addCase(startConversation.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to start conversation";
      })
      .addCase(requestHumanAgent.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(requestHumanAgent.fulfilled, (state) => {
        state.isLoading = false;
        if (state.currentConversation) {
          state.currentConversation.status = "waiting";
        }
      })
      .addCase(requestHumanAgent.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to request human agent";
      })
      .addCase(markMessageAsReadAPI.fulfilled, () => {
        // The API call succeeded, but the actual state update will come via SignalR
        console.log("Message marked as read via API");
      })
      .addCase(markMessageAsReadAPI.rejected, (_, action) => {
        console.error("Failed to mark message as read:", action.error.message);
      })
      .addCase(submitRating.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(submitRating.fulfilled, (state, action) => {
        state.isLoading = false;
        // Store the rating in the conversation
        if (state.currentConversation) {
          state.currentConversation.rating = action.payload.rating;
          state.currentConversation.feedback = action.payload.feedback;
        }
        console.log("Rating submitted successfully");
      })
      .addCase(submitRating.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to submit rating";
      })
      .addCase(fetchCustomerInfo.pending, () => {
        console.log("Fetching customer info...");
      })
      .addCase(fetchCustomerInfo.fulfilled, (_, action) => {
        console.log("Customer info fetched successfully:", action.payload);
        // Customer info is handled in startConversation, no state update needed here
      })
      .addCase(fetchCustomerInfo.rejected, (_, action) => {
        console.warn("Failed to fetch customer info:", action.error.message);
        // Don't set error state since this is optional
      });
  },
});

export const {
  toggleChat,
  minimizeChat,
  maximizeChat,
  closeChat,
  addMessage,
  setTyping,
  setConnectionStatus,
  updateConversationStatus,
  assignAgent,
  clearError,
  markAsRead,
  markMessageAsRead,
} = chatSlice.actions;

export default chatSlice.reducer;
