import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { apiClient } from "../../services/apiClient";
import { ensureISOString } from "../../utils/timestamp";
export interface Message {
  id: string;
  content: string;
  sender: "user" | "bot" | "agent";
  type: "text" | "file" | "image" | "typing";
  timestamp: string; // ISO string format for Redux serialization
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
  tenantId: string;
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

export const startConversation = createAsyncThunk("chat/startConversation", async (tenantId: string, { getState }) => {
  console.log("🚀 Starting conversation for tenant:", tenantId);

  const data = await apiClient.post("/chat/chat/conversations", {
    tenantId,
    customerName: "Anonymous User",
    language: "en",
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
    const state = getState() as any;
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

        // Add the actual bot message
        if (body.botMessage) {
          const botMsg = normalizeApiMessage(body.botMessage, "bot");
          state.currentConversation.messages.push(botMsg);
        }
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
} = chatSlice.actions;

export default chatSlice.reducer;

// Helper to normalize API messages into your Message shape
const mapSender = (s: unknown, fallback: "user" | "bot" | "agent"): Message["sender"] => {
  const v = String(s ?? "").toLowerCase();
  if (v === "bot") return "bot";
  if (v === "customer" || v === "user") return "user";
  if (v === "agent" || v === "human" || v === "support") return "agent";
  return fallback;
};

const normalizeApiMessage = (apiMsg: unknown, senderFallback: "user" | "bot" | "agent"): Message => {
  const msg = apiMsg as Record<string, unknown>;
  // Use utility to ensure timestamp is always an ISO string
  const timestamp = ensureISOString(msg.createdAt ?? msg.timestamp);

  return {
    id: String(msg.id || `msg-${Date.now()}`),
    content: String(msg.content || ""),
    type: (msg.type || "text").toString().toLowerCase() as Message["type"], // "Text" -> "text"
    sender: mapSender(msg.sender, senderFallback), // "Customer"/"Bot" -> "user"/"bot"
    timestamp: timestamp,
  };
};
