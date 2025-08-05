import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';

export interface Message {
  id: string;
  content: string;
  sender: 'user' | 'bot' | 'agent';
  timestamp: Date;
  type: 'text' | 'file' | 'image' | 'typing';
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
  status: 'active' | 'waiting' | 'ended';
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
  connectionStatus: 'connecting' | 'connected' | 'disconnected' | 'error';
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
  connectionStatus: 'disconnected',
};

export const sendMessage = createAsyncThunk(
  'chat/sendMessage',
  async (message: { content: string; type: 'text' | 'file' }, { getState }) => {
    const state = getState() as any;
    const conversationId = state.chat.currentConversation?.id;
    
    if (!conversationId) {
      throw new Error('No active conversation found');
    }

    const response = await fetch('http://localhost:8000/chat/chat/messages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        conversationId,
        content: message.content,
        type: message.type
      }),
    });
    
    if (!response.ok) {
      throw new Error('Failed to send message');
    }
    
    return response.json();
  }
);

export const startConversation = createAsyncThunk(
  'chat/startConversation',
  async (tenantId: string) => {
    const response = await fetch('http://localhost:8000/chat/chat/conversations', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ 
        tenantId,
        customerName: 'Anonymous User',
        language: 'en'
      }),
    });
    
    if (!response.ok) {
      throw new Error('Failed to create conversation');
    }
    
    const data = await response.json();
    
    return {
      id: data.id,
      messages: [],
      status: 'active' as const,
      startedAt: new Date(data.createdAt || new Date()),
    } as Conversation;
  }
);

export const requestHumanAgent = createAsyncThunk(
  'chat/requestHumanAgent',
  async (conversationId: string) => {
    const response = await fetch(`http://localhost:8000/chat/chat/conversations/${conversationId}/escalate`, {
      method: 'POST',
    });
    return response.json();
  }
);

const chatSlice = createSlice({
  name: 'chat',
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
        if (!state.isOpen && action.payload.sender !== 'user') {
          state.unreadCount += 1;
        }
      }
    },
    setTyping: (state, action: PayloadAction<{ isTyping: boolean; user?: string }>) => {
      state.isTyping = action.payload.isTyping;
      state.typingUser = action.payload.user;
    },
    setConnectionStatus: (state, action: PayloadAction<ChatState['connectionStatus']>) => {
      state.connectionStatus = action.payload;
      state.isConnected = action.payload === 'connected';
    },
    updateConversationStatus: (state, action: PayloadAction<Conversation['status']>) => {
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
      })
      .addCase(sendMessage.fulfilled, (state, action) => {
        state.isLoading = false;
        if (action.payload.success) {
        }
      })
      .addCase(sendMessage.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to send message';
      })
      .addCase(startConversation.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(startConversation.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentConversation = action.payload;
        state.error = null;
      })
      .addCase(startConversation.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to start conversation';
      })
      .addCase(requestHumanAgent.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(requestHumanAgent.fulfilled, (state) => {
        state.isLoading = false;
        if (state.currentConversation) {
          state.currentConversation.status = 'waiting';
        }
      })
      .addCase(requestHumanAgent.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to request human agent';
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
