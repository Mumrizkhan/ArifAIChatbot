import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";

// Add API base URL configuration
const API_BASE_URL = import.meta.env.API_BASE_URL 

export interface Agent {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  status: "online" | "away" | "busy" | "offline";
  currentConversations: number;
  maxConversations: number;
  skills: string[];
  languages: string[];
  rating: number;
  totalConversations: number;
  averageResponseTime: number;
  lastActivity: Date;
  phone?: string;
  bio?: string;
  location?: string;
  timezone?: string;
  language?: string;
  specializations?: string[];
}

export interface AgentStats {
  totalConversations: number;
  activeConversations: number;
  averageResponseTime: number;
  customerSatisfaction: number;
  resolutionRate: number;
  onlineTime: number;
  averageRating: number;
  monthlySatisfaction?: number;
  monthlyResponseTime?: number;
}

interface AgentNotification {
  id: string;
  type: "info" | "warning" | "error" | "success";
  title: string;
  message: string;
  timestamp: Date;
  agentId: string;
}

interface AgentStatusUpdate {
  agentId: string;
  status: "online" | "away" | "busy" | "offline";
  timestamp: Date;
}

interface AgentState {
  currentAgent: Agent | null;
  agents: Agent[];
  stats: AgentStats | null;
  isLoading: boolean;
  error: string | null;
  isSignalRConnected: boolean;
  agentNotifications: AgentNotification[];
}

const initialState: AgentState = {
  currentAgent: null,
  agents: [],
  stats: null,
  isLoading: false,
  error: null,
  isSignalRConnected: false,
  agentNotifications: [],
};

export const fetchAgentProfile = createAsyncThunk("agent/fetchProfile", async (agentId: string) => {
  const response = await fetch(`${API_BASE_URL}/agent/agents/${agentId}`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch agent profile");
  }

  return response.json();
});

export const updateAgentProfile = createAsyncThunk(
  "agent/updateProfile",
  async ({ id, profileData }: { id: string; profileData: Partial<Agent> }) => {
    const response = await fetch(`${API_BASE_URL}/agent/agents/${id}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
      body: JSON.stringify(profileData),
    });

    if (!response.ok) {
      throw new Error("Failed to update agent profile");
    }

    return response.json();
  }
);

export const updateAgentStatus = createAsyncThunk("agent/updateStatus", async ({ agentId, status }: { agentId: string; status: Agent["status"] }) => {
  const response = await fetch(`${API_BASE_URL}/agent/agents/${agentId}/status`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
    body: JSON.stringify({ status }),
  });

  if (!response.ok) {
    throw new Error("Failed to update agent status");
  }

  return { agentId, status };
});

export const fetchAgentStats = createAsyncThunk("agent/fetchStats", async (agentId: string) => {
  const response = await fetch(`${API_BASE_URL}/agent/agents/${agentId}/stats`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch agent stats");
  }

  return response.json();
});

export const fetchAllAgents = createAsyncThunk("agent/fetchAll", async () => {
  const response = await fetch(`${API_BASE_URL}/agent/agents`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to fetch agents");
  }

  return response.json();
});

const agentSlice = createSlice({
  name: "agent",
  initialState,
  reducers: {
    setCurrentAgent: (state, action: PayloadAction<Agent>) => {
      state.currentAgent = action.payload;
    },
    updateAgentStatusLocal: (state, action: PayloadAction<{ agentId: string; status: Agent["status"] }>) => {
      if (state.currentAgent && state.currentAgent.id === action.payload.agentId) {
        state.currentAgent.status = action.payload.status;
      }
      const agent = state.agents.find((a) => a.id === action.payload.agentId);
      if (agent) {
        agent.status = action.payload.status;
      }
    },
    incrementConversationCount: (state, action: PayloadAction<string>) => {
      if (state.currentAgent && state.currentAgent.id === action.payload) {
        state.currentAgent.currentConversations += 1;
      }
      const agent = state.agents.find((a) => a.id === action.payload);
      if (agent) {
        agent.currentConversations += 1;
      }
    },
    decrementConversationCount: (state, action: PayloadAction<string>) => {
      if (state.currentAgent && state.currentAgent.id === action.payload) {
        state.currentAgent.currentConversations = Math.max(0, state.currentAgent.currentConversations - 1);
      }
      const agent = state.agents.find((a) => a.id === action.payload);
      if (agent) {
        agent.currentConversations = Math.max(0, agent.currentConversations - 1);
      }
    },
    clearError: (state) => {
      state.error = null;
    },
    setSignalRConnectionStatus: (state, action: PayloadAction<boolean>) => {
      state.isSignalRConnected = action.payload;
    },
    updateAgentStatusRealtime: (state, action: PayloadAction<AgentStatusUpdate>) => {
      if (state.currentAgent && state.currentAgent.id === action.payload.agentId) {
        state.currentAgent.status = action.payload.status;
      }
      const agent = state.agents.find((a) => a.id === action.payload.agentId);
      if (agent) {
        agent.status = action.payload.status;
      }
    },
    addAgentNotification: (state, action: PayloadAction<AgentNotification>) => {
      state.agentNotifications.unshift(action.payload);
      if (state.agentNotifications.length > 50) {
        state.agentNotifications = state.agentNotifications.slice(0, 50);
      }
    },
    clearAgentNotifications: (state) => {
      state.agentNotifications = [];
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchAgentProfile.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAgentProfile.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentAgent = action.payload;
      })
      .addCase(fetchAgentProfile.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || "Failed to fetch agent profile";
      })
      .addCase(updateAgentProfile.fulfilled, (state, action) => {
        state.currentAgent = action.payload;
      })
      .addCase(updateAgentStatus.fulfilled, (state, action) => {
        if (state.currentAgent && state.currentAgent.id === action.payload.agentId) {
          state.currentAgent.status = action.payload.status;
        }
        const agent = state.agents.find((a) => a.id === action.payload.agentId);
        if (agent) {
          agent.status = action.payload.status;
        }
      })
      .addCase(fetchAgentStats.fulfilled, (state, action) => {
        state.stats = action.payload;
      })
      .addCase(fetchAllAgents.fulfilled, (state, action) => {
        state.agents = action.payload;
      });
  },
});

export const {
  setCurrentAgent,
  updateAgentStatusLocal,
  incrementConversationCount,
  decrementConversationCount,
  clearError,
  setSignalRConnectionStatus,
  updateAgentStatusRealtime,
  addAgentNotification,
  clearAgentNotifications,
} = agentSlice.actions;

export default agentSlice.reducer;
