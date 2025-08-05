import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";

// Add API base URL configuration from environment variables
const API_BASE_URL = import.meta.env.API_BASE_URL || "http://localhost:8000";

export interface Message {
  id: string;
  conversationId: string;
  content: string;
  sender: "customer" | "agent" | "system";
  timestamp: Date;
  type: "text" | "file" | "image" | "system";
  metadata?: {
    fileName?: string;
    fileSize?: number;
    fileType?: string;
    imageUrl?: string;
  };
  isRead: boolean;
}

export interface Customer {
  id: string;
  name: string;
  email?: string;
  avatar?: string;
  location?: string;
  language: string;
  metadata?: Record<string, any>;
}

export interface Conversation {
  id: string;
  customer: Customer;
  assignedAgent?: {
    id: string;
    name: string;
    avatar?: string;
  };
  status: "waiting" | "active" | "resolved" | "closed";
  priority: "low" | "medium" | "high" | "urgent";
  channel: "web" | "mobile" | "email" | "phone";
  subject?: string;
  tags: string[];
  messages: Message[];
  createdAt: Date;
  updatedAt: Date;
  waitTime?: number;
  responseTime?: number;
  rating?: number;
  feedback?: string;
  unreadCount: number;
}

interface ConversationAssignment {
  conversationId: string;
  agentId: string;
  timestamp: Date;
}

interface ConversationTransfer {
  conversationId: string;
  fromAgentId: string;
  toAgentId: string;
  reason: string;
  timestamp: Date;
}

interface ConversationState {
  conversations: Conversation[];
  activeConversation: Conversation | null;
  isLoading: boolean;
  error: string | null;
  filters: {
    status: string[];
    priority: string[];
    channel: string[];
  };
  searchQuery: string;
  typingUsers: Record<string, { userId: string; userName: string }>;
  isSignalRConnected: boolean;
}

const initialState: ConversationState = {
  conversations: [],
  activeConversation: null,
  isLoading: false,
  error: null,
  filters: {
    status: [],
    priority: [],
    channel: [],
  },
  searchQuery: "",
  typingUsers: {},
  isSignalRConnected: false,
};

export const fetchConversations = createAsyncThunk(
  "conversations/fetchAll",
  async (params?: { status?: string; priority?: string; limit?: number }) => {
    const queryParams = new URLSearchParams();
    if (params?.status) queryParams.append("status", params.status);
    if (params?.priority) queryParams.append("priority", params.priority);
    if (params?.limit) queryParams.append("limit", params.limit.toString());

    const response = await fetch(`${API_BASE_URL}/chat/conversations?${queryParams}`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
    });

    if (!response.ok) {
      throw new Error("Failed to fetch conversations");
    }

    return response.json();
  }
);

export const fetchConversation = createAsyncThunk("conversations/fetchOne", async (conversationId: string) => {
  const response = await fetch(`${API_BASE_URL}/chat/conversations/${conversationId}`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch conversation");
  }

  return response.json();
});

export const sendMessage = createAsyncThunk(
  "conversations/sendMessage",
  async ({
    conversationId,
    content,
    type = "text",
    metadata,
  }: {
    conversationId: string;
    content: string;
    type?: Message["type"];
    metadata?: Message["metadata"];
  }) => {
    const response = await fetch(`${API_BASE_URL}/chat/conversations/${conversationId}/messages`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({ content, type, metadata }),
    });

    if (!response.ok) {
      throw new Error("Failed to send message");
    }

    return response.json();
  }
);

export const updateConversationStatus = createAsyncThunk(
  "conversations/updateStatus",
  async ({ conversationId, status }: { conversationId: string; status: Conversation["status"] }) => {
    const response = await fetch(`${API_BASE_URL}/chat/conversations/${conversationId}/status`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({ status }),
    });

    if (!response.ok) {
      throw new Error("Failed to update conversation status");
    }

    return { conversationId, status };
  }
);

export const assignConversation = createAsyncThunk(
  "conversations/assign",
  async ({ conversationId, agentId }: { conversationId: string; agentId: string }) => {
    const response = await fetch(`${API_BASE_URL}/chat/conversations/${conversationId}/assign`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({ agentId }),
    });

    if (!response.ok) {
      throw new Error("Failed to assign conversation");
    }

    return response.json();
  }
);

export const transferConversation = createAsyncThunk(
  "conversations/transfer",
  async ({ conversationId, toAgentId, reason }: { conversationId: string; toAgentId: string; reason: string }) => {
    const response = await fetch(`${API_BASE_URL}/chat/conversations/${conversationId}/transfer`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({ toAgentId, reason }),
    });

    if (!response.ok) {
      throw new Error("Failed to transfer conversation");
    }

    return response.json();
  }
);

export const rateConversation = createAsyncThunk(
  "conversations/rate",
  async ({ conversationId, rating, feedback }: { conversationId: string; rating: number; feedback?: string }) => {
    const response = await fetch(`${API_BASE_URL}/chat/conversations/${conversationId}/rate`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify({ rating, feedback }),
    });

    if (!response.ok) {
      throw new Error("Failed to rate conversation");
    }

    return { conversationId, rating, feedback };
  }
);

const conversationSlice = createSlice({
  name: "conversations",
  initialState,
  reducers: {
    setActiveConversation: (state, action: PayloadAction<Conversation | null>) => {
      state.activeConversation = action.payload;
      if (action.payload) {
        const conversation = state.conversations.find((c) => c.id === action.payload!.id);
        if (conversation) {
          conversation.unreadCount = 0;
        }
      }
    },
    addMessage: (state, action: PayloadAction<Message>) => {
      const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
      if (conversation) {
        conversation.messages.push(action.payload);
        conversation.updatedAt = new Date();
        if (state.activeConversation?.id !== conversation.id) {
          conversation.unreadCount += 1;
        }
      }
      if (state.activeConversation?.id === action.payload.conversationId) {
        state.activeConversation.messages.push(action.payload);
      }
    },
    updateMessage: (state, action: PayloadAction<Message>) => {
      const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
      if (conversation) {
        const messageIndex = conversation.messages.findIndex((m) => m.id === action.payload.id);
        if (messageIndex !== -1) {
          conversation.messages[messageIndex] = action.payload;
        }
      }
      if (state.activeConversation?.id === action.payload.conversationId) {
        const messageIndex = state.activeConversation.messages.findIndex((m) => m.id === action.payload.id);
        if (messageIndex !== -1) {
          state.activeConversation.messages[messageIndex] = action.payload;
        }
      }
    },
    setTypingUser: (state, action: PayloadAction<{ conversationId: string; userId: string; userName: string }>) => {
      state.typingUsers[action.payload.conversationId] = {
        userId: action.payload.userId,
        userName: action.payload.userName,
      };
    },
    removeTypingUser: (state, action: PayloadAction<string>) => {
      delete state.typingUsers[action.payload];
    },
    updateFilters: (state, action: PayloadAction<Partial<ConversationState["filters"]>>) => {
      state.filters = { ...state.filters, ...action.payload };
    },
    setSearchQuery: (state, action: PayloadAction<string>) => {
      state.searchQuery = action.payload;
    },
    markConversationAsRead: (state, action: PayloadAction<string>) => {
      const conversation = state.conversations.find((c) => c.id === action.payload);
      if (conversation) {
        conversation.unreadCount = 0;
        conversation.messages.forEach((message) => {
          if (!message.isRead && message.sender === "customer") {
            message.isRead = true;
          }
        });
      }
    },
    clearError: (state) => {
      state.error = null;
    },
    setConversationSignalRStatus: (state, action: PayloadAction<boolean>) => {
      state.isSignalRConnected = action.payload;
    },
    assignConversationRealtime: (state, action: PayloadAction<ConversationAssignment>) => {
      const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
      if (conversation) {
        conversation.status = "active";
        conversation.assignedAgent = { id: action.payload.agentId, name: "Agent" };
      }
    },
    transferConversationRealtime: (state, action: PayloadAction<ConversationTransfer>) => {
      const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
      if (conversation) {
        conversation.assignedAgent = { id: action.payload.toAgentId, name: "Agent" };
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchConversations.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchConversations.fulfilled, (state, action) => {
        state.isLoading = false;
        state.conversations = Array.isArray(action.payload) ? action.payload : [];
      })
      .addCase(fetchConversations.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch conversations";
      })
      .addCase(fetchConversation.fulfilled, (state, action) => {
        const existingIndex = state.conversations.findIndex((c) => c.id === action.payload.id);
        if (existingIndex !== -1) {
          state.conversations[existingIndex] = action.payload;
        } else {
          state.conversations.push(action.payload);
        }
      })
      .addCase(sendMessage.fulfilled, (state, action) => {
        const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
        if (conversation) {
          conversation.messages.push(action.payload);
          conversation.updatedAt = new Date();
        }
        if (state.activeConversation?.id === action.payload.conversationId) {
          state.activeConversation!.messages.push(action.payload);
        }
      })
      .addCase(updateConversationStatus.fulfilled, (state, action) => {
        const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
        if (conversation) {
          conversation.status = action.payload.status;
        }
        if (state.activeConversation?.id === action.payload.conversationId) {
          state.activeConversation.status = action.payload.status;
        }
      })
      .addCase(assignConversation.fulfilled, (state, action) => {
        const conversation = state.conversations.find((c) => c.id === action.payload.id);
        if (conversation) {
          conversation.assignedAgent = action.payload.assignedAgent;
        }
        if (state.activeConversation?.id === action.payload.id) {
          state.activeConversation!.assignedAgent = action.payload.assignedAgent;
        }
      })
      .addCase(transferConversation.fulfilled, (state, action) => {
        const conversation = state.conversations.find((c) => c.id === action.payload.id);
        if (conversation) {
          conversation.assignedAgent = action.payload.assignedAgent;
        }
        if (state.activeConversation?.id === action.payload.id) {
          state.activeConversation!.assignedAgent = action.payload.assignedAgent;
        }
      })
      .addCase(rateConversation.fulfilled, (state, action) => {
        const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
        if (conversation) {
          conversation.rating = action.payload.rating;
          conversation.feedback = action.payload.feedback;
        }
        if (state.activeConversation?.id === action.payload.conversationId) {
          state.activeConversation.rating = action.payload.rating;
          state.activeConversation.feedback = action.payload.feedback;
        }
      });
  },
});

export const {
  setActiveConversation,
  addMessage,
  updateMessage,
  setTypingUser,
  removeTypingUser,
  updateFilters,
  setSearchQuery,
  markConversationAsRead,
  clearError,
  setConversationSignalRStatus,
  assignConversationRealtime,
  transferConversationRealtime,
} = conversationSlice.actions;

export default conversationSlice.reducer;
