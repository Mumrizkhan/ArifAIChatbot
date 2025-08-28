import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";

// Interface for backend message format
interface BackendMessage {
  // Backend returns camelCase properties
  id: string;
  conversationId: string;
  content: string;
  type: string;
  sender: string;
  senderId?: string;
  senderName?: string;
  createdAt: string;
  isRead: boolean;
}

// API Base URL with fallback
const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || "https://api-stg-arif.tetco.sa";

export interface Message {
  id: string;
  conversationId: string;
  content: string;
  sender: "customer" | "agent" | "system";
  timestamp: string; // Changed from Date to string
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
  phone?: string;
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
  createdAt: string; // Changed from Date to string
  updatedAt: string; // Changed from Date to string
  waitTime?: number;
  responseTime?: number;
  rating?: number;
  feedback?: string;
  unreadCount: number;
}

interface ConversationAssignment {
  conversationId: string;
  agentId: string;
  timestamp: string; // ISO string instead of Date
}

interface ConversationTransfer {
  conversationId: string;
  fromAgentId: string;
  toAgentId: string;
  reason: string;
  timestamp: string; // ISO string instead of Date
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

const transformConversationAssignment = (assignment: any): Conversation => {
  console.log("Transforming conversation assignment:", assignment);
  
  return {
    id: assignment.conversationId || assignment.ConversationId,
    customer: {
      id: assignment.customerId || assignment.CustomerId || "unknown",
      name: assignment.customerName || assignment.CustomerName || "Unknown Customer",
      email: assignment.customerEmail || assignment.CustomerEmail,
      language: assignment.language || assignment.Language || "en",
    },
    assignedAgent:
      assignment.agentId || assignment.AgentId
        ? {
            id: assignment.agentId || assignment.AgentId,
            name: assignment.agentName || assignment.AgentName || "Agent",
          }
        : undefined,
    status: (() => {
      const statusValue = assignment.status || assignment.Status;
      if (typeof statusValue === "string") {
        return statusValue.toLowerCase();
      }
      return "active";
    })() as any,
    priority: (() => {
      const priorityValue = assignment.priority || assignment.Priority;
      // Only call toLowerCase if it's a string
      if (typeof priorityValue === "string") {
        return priorityValue.toLowerCase();
      }
      // It's likely a numeric priority (1,2,3) or other non-string format
      // Convert to expected string format or use default
      return "medium";
    })() as any,
    channel: "web" as any,
    subject: assignment.subject || assignment.Subject || "Conversation",
    tags: [],
    messages: [],
    createdAt: assignment.timestamp || assignment.Timestamp || new Date().toISOString(),
    updatedAt: assignment.timestamp || assignment.Timestamp || new Date().toISOString(),
    unreadCount: assignment.unreadMessages || assignment.UnreadMessages || 0,
  };
};

export const fetchConversations = createAsyncThunk(
  "conversations/fetchAll",
  async (params?: { status?: string; priority?: string; limit?: number }) => {
    const queryParams = new URLSearchParams();
    if (params?.status) queryParams.append("status", params.status);
    if (params?.priority) queryParams.append("priority", params.priority);
    if (params?.limit) queryParams.append("limit", params.limit.toString());

    const response = await fetch(`${API_BASE_URL}/agent/conversations?${queryParams}`, {
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
  const response = await fetch(`${API_BASE_URL}/agent/conversations/${conversationId}`, {
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
    const response = await fetch(`${API_BASE_URL}/agent/conversations/${conversationId}/messages`, {
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
    const response = await fetch(`${API_BASE_URL}/agent/conversations/${conversationId}/status`, {
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
    const response = await fetch(`${API_BASE_URL}/agent/conversations/${conversationId}/assign`, {
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
    const response = await fetch(`${API_BASE_URL}/agent/conversations/${conversationId}/transfer`, {
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
    const response = await fetch(`${API_BASE_URL}/agent/conversations/${conversationId}/rate`, {
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
        // Check if message already exists to prevent duplicates
        const existingMessage = conversation.messages.find((m) => m.id === action.payload.id);
        if (!existingMessage) {
          conversation.messages.push(action.payload);
          conversation.updatedAt = new Date().toISOString();
          if (state.activeConversation?.id !== conversation.id) {
            conversation.unreadCount += 1;
          }
        }
      }
      if (state.activeConversation?.id === action.payload.conversationId) {
        // Check if message already exists in active conversation to prevent duplicates
        const existingActiveMessage = state.activeConversation.messages.find((m) => m.id === action.payload.id);
        if (!existingActiveMessage) {
          state.activeConversation.messages.push(action.payload);
        }
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
    markMessageAsRead: (state, action: PayloadAction<{ messageId: string }>) => {
      for (const conversation of state.conversations) {
        const message = conversation.messages.find(msg => msg.id === action.payload.messageId);
        if (message && !message.isRead) {
          message.isRead = true;
          // Decrease unread count if it was an unread customer message
          if (message.sender === "customer" && conversation.unreadCount > 0) {
            conversation.unreadCount -= 1;
          }
          break;
        }
      }
    },
    clearError: (state) => {
      state.error = null;
    },
    setConversationSignalRStatus: (state, action: PayloadAction<boolean>) => {
      state.isSignalRConnected = action.payload;
    },
    assignConversationRealtime: (state, action: PayloadAction<ConversationAssignment>) => {
      const existingIndex = state.conversations.findIndex((c) => c.id === action.payload.conversationId);
      const transformedConversation = transformConversationAssignment(action.payload);

      if (existingIndex !== -1) {
        state.conversations[existingIndex] = transformedConversation;
      } else {
        state.conversations.unshift(transformedConversation);
      }
    },
    transferConversationRealtime: (state, action: PayloadAction<ConversationTransfer>) => {
      const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
      if (conversation) {
        conversation.assignedAgent = { id: action.payload.toAgentId, name: "Agent" };
      }
    },
    addNewConversation: (state, action: PayloadAction<Conversation | any>) => {
      const conversation = action.payload.id ? action.payload : transformConversationAssignment(action.payload);
      const existingIndex = state.conversations.findIndex((c) => c.id === conversation.id);
      if (existingIndex === -1) {
        state.conversations.unshift(conversation);
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
        const rawConversations = Array.isArray(action.payload) ? action.payload : [];
        state.conversations = rawConversations.map(transformConversationAssignment);
      })
      .addCase(fetchConversations.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch conversations";
      })
      .addCase(fetchConversation.fulfilled, (state, action) => {
        // Transform backend response to match frontend Conversation interface
        const backendData = action.payload;
        
        console.log("🔍 Live Agent: Raw backend response:", backendData);
        
        const transformedConversation: Conversation = {
          id: backendData.conversationId, // Backend returns camelCase
          customer: {
            id: backendData.customerId || "unknown",
            name: backendData.customerName || "Anonymous User",
            email: backendData.customerEmail,
            phone: backendData.customerPhone,
            avatar: backendData.customerAvatar,
            language: backendData.language || "en"
          },
          assignedAgent: backendData.agentId ? {
            id: backendData.agentId,
            name: backendData.agentName || "Agent",
            avatar: backendData.agentAvatar
          } : undefined,
          status: backendData.status?.toLowerCase() as "waiting" | "active" | "resolved" | "closed" || "waiting",
          priority: (() => {
            // Handle numeric priority (2 = medium, 1 = low, 3 = high, 4 = urgent)
            const priorityValue = backendData.priority;
            if (typeof priorityValue === 'number') {
              switch (priorityValue) {
                case 1: return "low";
                case 2: return "medium"; 
                case 3: return "high";
                case 4: return "urgent";
                default: return "medium";
              }
            }
            return priorityValue?.toLowerCase() as "low" | "medium" | "high" | "urgent" || "medium";
          })(),
          channel: backendData.channel?.toLowerCase() as "web" | "mobile" | "email" | "phone" || "web",
          subject: backendData.subject,
          tags: backendData.tags || [],
          messages: (backendData.messages || []).map((msg: BackendMessage) => ({
            id: msg.id,
            conversationId: msg.conversationId,
            content: msg.content,
            sender: (() => {
              const senderType = msg.sender?.toLowerCase();
              // Map 'bot' to 'system' to match our interface
              if (senderType === 'bot') return 'system';
              return senderType as "customer" | "agent" | "system";
            })(),
            timestamp: msg.createdAt || new Date().toISOString(),
            type: msg.type?.toLowerCase() as "text" | "file" | "image" | "system" || "text",
            isRead: msg.isRead ?? false,
            metadata: {
              senderId: msg.senderId,
              senderName: msg.senderName
            }
          })),
          createdAt: backendData.createdAt,
          updatedAt: backendData.updatedAt,
          waitTime: backendData.waitTime,
          responseTime: backendData.responseTime,
          rating: backendData.rating,
          feedback: backendData.feedback,
          unreadCount: (backendData.messages || []).filter((msg: BackendMessage) => !msg.isRead && msg.sender?.toLowerCase() !== 'agent').length
        };

        console.log("🔄 Live Agent: Transformed conversation:", transformedConversation);
        console.log("🔄 Live Agent: Message count:", transformedConversation.messages.length);
        console.log("🔄 Live Agent: Sample messages:", transformedConversation.messages.slice(0, 3));

        const existingIndex = state.conversations.findIndex((c) => c.id === transformedConversation.id);
        if (existingIndex !== -1) {
          state.conversations[existingIndex] = transformedConversation;
        } else {
          state.conversations.push(transformedConversation);
        }

        // Update active conversation if it matches
        if (state.activeConversation?.id === transformedConversation.id) {
          state.activeConversation = transformedConversation;
        }
      })
      .addCase(sendMessage.fulfilled, (state, action) => {
        const conversation = state.conversations.find((c) => c.id === action.payload.conversationId);
        if (conversation) {
          // Check if message already exists to prevent duplicates
          const existingMessage = conversation.messages.find((m) => m.id === action.payload.id);
          if (!existingMessage) {
            conversation.messages.push(action.payload);
            conversation.updatedAt = new Date().toISOString();
          }
        }
        if (state.activeConversation?.id === action.payload.conversationId) {
          // Check if message already exists in active conversation to prevent duplicates
          const existingActiveMessage = state.activeConversation?.messages.find((m) => m.id === action.payload.id);
          if (!existingActiveMessage && state.activeConversation) {
            state.activeConversation.messages.push(action.payload);
          }
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
  markMessageAsRead,
  clearError,
  setConversationSignalRStatus,
  assignConversationRealtime,
  transferConversationRealtime,
  addNewConversation,
} = conversationSlice.actions;

export default conversationSlice.reducer;
