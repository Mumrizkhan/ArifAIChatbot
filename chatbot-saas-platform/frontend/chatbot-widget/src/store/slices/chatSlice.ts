import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { apiClient } from "../../services/apiClient";
const VITE_API_URL = import.meta.env.VITE_API_URL || "https://api-stg-arif.tetco.sa";
export interface Message {
  id: string;
  content: string;
  sender: "user" | "bot" | "agent";
  type: "text" | "file" | "image" | "typing";
  timestamp: Date;
  metadata?: {
    fileName?: string;
    fileSize?: number;
    fileType?: string;
    imageUrl?: string;
    senderId?: string;
    senderName?: string;
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
  startedAt: Date;
  endedAt?: Date;
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
  lastSeen?: Date;
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

export const sendMessage = createAsyncThunk(
  "chat/sendMessage",
  async (payload: { content: string; type: "text" | "file" }, { getState, rejectWithValue }) => {
    try {
      const { chat } = getState() as { chat: ChatState };
      const conversationId = chat.currentConversation?.id;
      if (!conversationId) throw new Error("No active conversation");

      const res = await apiClient.post(`${import.meta.env.VITE_API_URL}/chat/chat/messages`, {
        conversationId,
        content: payload.content,
        type: payload.type,
      });

      // Support both axios-like and body-returning clients
      const body = res && typeof res === "object" && "data" in res ? (res as any).data : res;
      return body; // { userMessage, botMessage, success }
    } catch (e: any) {
      return rejectWithValue(e?.response?.data ?? { message: e?.message || "Send failed" });
    }
  }
);

export const startConversation = createAsyncThunk("chat/startConversation", async (tenantId: string) => {
  const data = await apiClient.post(`${VITE_API_URL}/chat/chat/conversations`, {
    tenantId,
    customerName: "Anonymous User",
    language: "en",
  });

  return {
    id: data.id,
    messages: [],
    status: "active" as const,
    startedAt: new Date(data.createdAt || new Date()),
  } as Conversation;
});

export const requestHumanAgent = createAsyncThunk("chat/requestHumanAgent", async (conversationId: string) => {
  return await apiClient.post(`${VITE_API_URL}/chat/conversations/${conversationId}/escalate`);
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
        state.currentConversation.messages.push(action.payload);
      } else {
        // If no conversation exists, create a new one with the message
        state.currentConversation = {
          id: `temp-conversation-${Date.now()}`, // Temporary ID for the conversation
          messages: [action.payload],
          status: "active",
          startedAt: new Date(),
        };
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
      state.lastSeen = new Date();
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
              timestamp: new Date(),
              metadata: {},
            });
          }
        }
      })
      .addCase(sendMessage.fulfilled, (state, action) => {
        state.isLoading = false;

        const body = action.payload as any;
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
        state.error = (action.payload as any)?.message || action.error.message || "Failed to send message";

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
        const conv = action.payload as any;

        // Preserve existing messages if they already exist
        state.currentConversation = {
          ...conv,
          messages: state.currentConversation?.messages.length
            ? state.currentConversation.messages
            : Array.isArray(conv?.messages)
            ? conv.messages
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
const mapSender = (s: any, fallback: "user" | "bot" | "agent"): Message["sender"] => {
  const v = String(s ?? "").toLowerCase();
  if (v === "bot") return "bot";
  if (v === "customer" || v === "user") return "user";
  if (v === "agent" || v === "human" || v === "support") return "agent";
  return fallback;
};

const normalizeApiMessage = (apiMsg: any, senderFallback: "user" | "bot" | "agent"): Message => ({
  id: apiMsg.id,
  content: apiMsg.content,
  type: (apiMsg.type || "text").toString().toLowerCase(), // "Text" -> "text"
  sender: mapSender(apiMsg.sender, senderFallback), // "Customer"/"Bot" -> "user"/"bot"
  timestamp: new Date(apiMsg.createdAt ?? apiMsg.timestamp ?? Date.now()),
});
