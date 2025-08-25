import { HubConnection, HubConnectionBuilder, HubConnectionState, HttpTransportType, LogLevel } from "@microsoft/signalr";

interface AgentStatusUpdate {
  agentId: string;
  status: "online" | "away" | "busy" | "offline";
  timestamp: string; // ISO string instead of Date
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

interface ConversationEscalation {
  conversationId: string;
  agentId: string;
  reason: string;
  priority: string;
  timestamp: string; // ISO string instead of Date
}

interface AssistanceRequest {
  conversationId: string;
  requestingAgentId: string;
  message: string;
  timestamp: string; // ISO string instead of Date
}

interface BroadcastMessage {
  message: string;
  type: string;
  fromAgentId: string;
  timestamp: string; // ISO string instead of Date
}

interface AgentNotification {
  id: string;
  type: "info" | "warning" | "error" | "success";
  title: string;
  message: string;
  timestamp: string; // ISO string instead of Date
  agentId: string;
}

class AgentSignalRService {
  // Set true to force LongPolling for quick isolation test (do NOT use in production)
  private debugForceLongPolling = false;
  // small helper for safe logging
  private safeTokenLength(tok: string | null) {
    return tok ? tok.length : 0;
  }

  private connection: HubConnection | null = null;
  private isConnected = false;
  private isConnecting = false;
  private handlersWired = false;
  private lastAuthToken: string | null = null;

  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000;

  private agentId: string | null = null;
  private tenantId: string | null = null;
  private isInAgentGroup: boolean = false;

  private onAgentStatusChanged?: (status: AgentStatusUpdate) => void;
  private onConversationAssigned?: (assignment: ConversationAssignment) => void;
  private onConversationTaken?: (assignment: ConversationAssignment) => void;
  private onConversationTransferred?: (transfer: ConversationTransfer) => void;
  private onConversationEscalated?: (escalation: ConversationEscalation) => void;
  private onMessageReceived?: (message: any) => void;
  private onAssistanceRequested?: (request: AssistanceRequest) => void;
  private onBroadcastMessage?: (message: BroadcastMessage) => void;
  private onAgentNotification?: (notification: AgentNotification) => void;
  private onConnectionStatusChange?: (isConnected: boolean) => void;

  async connect(authToken: string, agentId: string, tenantId: string): Promise<boolean> {
    console.log("AgentSignalRService.connect called with agentId:", agentId);

    if (this.isConnecting) {
      console.warn("Connect already in progress - skipping duplicate call");
      return false;
    }

    this.isConnecting = true;

    try {
      if (this.isConnected) {
        console.log("Already connected");
        return true;
      }

      if (!agentId) {
        console.error("Invalid agentId", agentId);
        return false;
      }

      this.agentId = agentId;
      this.tenantId = tenantId;

      const needRebuild = !this.connection || this.lastAuthToken !== authToken;
      if (needRebuild && this.connection) {
        console.log("Auth token changed or connection missing - stopping existing connection to rebuild");
        try {
          await this.connection.stop();
        } catch (e) {
          console.warn("Error stopping existing connection:", e);
        }
        this.connection = null;
        this.handlersWired = false;
      }

      if (!this.connection) {
        const API_BASE_URL = (import.meta as any).env.VITE_API_BASE_URL || "https://api-stg-arif.tetco.sa";
        this.connection = new HubConnectionBuilder()
          .withUrl(`${API_BASE_URL}/agent/agenthub`, {
            accessTokenFactory: () => authToken,
            transport: this.debugForceLongPolling ? HttpTransportType.LongPolling : undefined,
            withCredentials: true
          })
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (retryContext) => {
              if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
                return this.reconnectDelay;
              }
              return null;
            },
          })
          .configureLogging(LogLevel.Trace) // verbose during debugging
          .build();

        this.lastAuthToken = authToken;
      }

      // wire handlers once per connection instance and before starting
      if (!this.handlersWired && this.connection) {
        this.setupEventHandlers();
        this.handlersWired = true;
      }

      // use local reference to avoid races where this.connection is cleared during start
      const conn = this.connection;
      if (!conn) {
        console.error("Connection cleared before start");
        return false;
      }

      if (conn.state === HubConnectionState.Connected) {
        console.log("Connection already in Connected state");
        this.isConnected = true;
      } else if (conn.state === HubConnectionState.Disconnected) {
        console.log("Starting SignalR connection...");
        try {
          // timeout to avoid hanging indefinitely
          await Promise.race([conn.start(), new Promise((_, rej) => setTimeout(() => rej(new Error("SignalR start timeout")), 20000))]);
          this.isConnected = true;
          console.log("SignalR connection started, state:", conn.state);
        } catch (startErr: any) {
          console.error("Failed to start the connection:", startErr?.name ?? startErr, startErr?.message ?? "");
          // cleanup broken instance so next attempt rebuilds
          try {
            await conn.stop();
          } catch (e) {
            /* ignore */
          }
          this.connection = null;
          this.handlersWired = false;
          this.isConnected = false;
          this.onConnectionStatusChange?.(false);
          return false;
        }
      } else {
        console.warn("Connection in transient state:", conn.state, " - skipping start");
      }

      // connected -> join agent group (guarded)
      if (this.isConnected && conn && this.agentId) {
        try {
          // Add retry logic for JoinAgentGroup
          let joinSuccess = false;
          let attempts = 0;
          const maxAttempts = 3;

          while (!joinSuccess && attempts < maxAttempts) {
            try {
              attempts++;
              console.log(`Attempting to join agent group (attempt ${attempts}/${maxAttempts})...`);
              await conn.invoke("JoinAgentGroup");
              console.log("Joined agent group:", this.agentId);
              joinSuccess = true;
            } catch (invokeErr) {
              // Try to get more diagnostic info from the error
              console.error(
                `JoinAgentGroup failed (attempt ${attempts}/${maxAttempts}):`,
                typeof invokeErr === "object" && invokeErr && "message" in invokeErr ? (invokeErr as any).message : String(invokeErr),
                typeof invokeErr === "object" && invokeErr && "stack" in invokeErr ? (invokeErr as any).stack : ""
              );

              if (attempts < maxAttempts) {
                // Wait before retry with exponential backoff
                const delay = Math.min(1000 * Math.pow(2, attempts - 1), 5000);
                console.log(`Retrying JoinAgentGroup in ${delay}ms...`);
                await new Promise((resolve) => setTimeout(resolve, delay));
              } else {
                // Log detailed diagnostic info on final failure
                console.error("All JoinAgentGroup attempts failed. Connection state:", conn.state, "AgentId:", this.agentId);
              }
            }
          }

          // Only consider connection failed if we couldn't join after retries
          if (!joinSuccess) {
            // Don't disconnect - you're connected but just not in a group
            // Let the app continue working with limited functionality
            console.warn("Connected to SignalR but failed to join agent group");
            // Option: Set a flag to indicate partial connection
            this.isInAgentGroup = false;
          } else {
            this.isInAgentGroup = true;
          }
        } catch (outerErr) {
          console.error("Unexpected error in JoinAgentGroup logic:", outerErr);
          // Continue anyway - connection is established
        }
      } else {
        if (!this.agentId) console.warn("No agentId available; skipping JoinAgentGroup");
      }

      this.onConnectionStatusChange?.(true);
      return this.isConnected;
    } finally {
      this.isConnecting = false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // standard server -> client events
    this.connection.on("AgentStatusChanged", (status: AgentStatusUpdate) => {
      try {
        this.onAgentStatusChanged?.(status);
      } catch (e) {
        console.error("onAgentStatusChanged handler error:", e);
      }
    });

    this.connection.on("ConversationAssigned", (assignment: ConversationAssignment) => {
      console.log(assignment, "ConversationAssigned");
      try {
        this.onConversationAssigned?.(assignment);
      } catch (e) {
        console.error("onConversationAssigned handler error:", e);
      }
    });

    this.connection.on("ConversationTaken", (assignment: ConversationAssignment) => {
      try {
        this.onConversationTaken?.(assignment);
      } catch (e) {
        console.error("onConversationTaken handler error:", e);
      }
    });

    this.connection.on("ConversationTransferred", (transfer: ConversationTransfer) => {
      try {
        this.onConversationTransferred?.(transfer);
      } catch (e) {
        console.error("onConversationTransferred handler error:", e);
      }
    });

    this.connection.on("ConversationEscalated", (escalation: ConversationEscalation) => {
      try {
        this.onConversationEscalated?.(escalation);
      } catch (e) {
        console.error("onConversationEscalated handler error:", e);
      }
    });

    this.connection.on("ReceiveMessage", (message: any) => {
      try {
        this.onMessageReceived?.(message);
      } catch (e) {
        console.error("onMessageReceived handler error:", e);
      }
    });

    this.connection.on("AssistanceRequested", (request: AssistanceRequest) => {
      try {
        this.onAssistanceRequested?.(request);
      } catch (e) {
        console.error("onAssistanceRequested handler error:", e);
      }
    });

    this.connection.on("BroadcastMessage", (message: BroadcastMessage) => {
      try {
        this.onBroadcastMessage?.(message);
      } catch (e) {
        console.error("onBroadcastMessage handler error:", e);
      }
    });

    // lifecycle handlers - provide rich logging and maintain state
    this.connection.onclose((error) => {
      if (error) {
        console.error("SignalR onclose — name:", (error as any).name, "message:", (error as any).message);
      } else {
        console.error("SignalR onclose — closed without error");
      }
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      // keep connection instance for automatic reconnect to use; cleanup happens on failed start
    });

    this.connection.onreconnecting((error) => {
      console.warn("SignalR reconnecting:", error);
      this.onConnectionStatusChange?.(false);
    });

    this.connection.onreconnected(() => {
      console.log("SignalR reconnected");
      this.onConnectionStatusChange?.(true);

      // optionally re-join conversation/group without blocking the event loop
      if (!this.connection) return;
      if (!this.agentId) return;

      (async () => {
        try {
          const conn = this.connection!;
          const maxWait = 5000;
          const start = Date.now();
          while (conn && conn.state !== HubConnectionState.Connected && Date.now() - start < maxWait) {
            await new Promise((r) => setTimeout(r, 200));
          }
          if (conn && conn.state === HubConnectionState.Connected) {
            try {
              await conn.invoke("JoinAgentGroup", this.agentId!);
              console.log("Re-joined agent group after reconnect:", this.agentId);
            } catch (err) {
              console.error("Failed to re-join agent group after reconnect:", err);
            }
          } else {
            console.warn("Connection not in Connected state after reconnected event; skipping rejoin");
          }
        } catch (err) {
          console.error("Error in onreconnected handler:", err);
        }
      })();
    });
  }

  setOnAgentStatusChanged(handler: (status: AgentStatusUpdate) => void): void {
    this.onAgentStatusChanged = handler;
  }

  setOnConversationAssigned(handler: (assignment: ConversationAssignment) => void): void {
    this.onConversationAssigned = handler;
  }

  setOnConversationTaken(handler: (assignment: ConversationAssignment) => void): void {
    this.onConversationTaken = handler;
  }

  setOnConversationTransferred(handler: (transfer: ConversationTransfer) => void): void {
    this.onConversationTransferred = handler;
  }

  setOnConversationEscalated(handler: (escalation: ConversationEscalation) => void): void {
    this.onConversationEscalated = handler;
  }

  setOnMessageReceived(handler: (message: any) => void): void {
    this.onMessageReceived = handler;
  }

  setOnAssistanceRequested(handler: (request: AssistanceRequest) => void): void {
    this.onAssistanceRequested = handler;
  }

  setOnBroadcastMessage(handler: (message: BroadcastMessage) => void): void {
    this.onBroadcastMessage = handler;
  }

  setOnAgentNotification(handler: (notification: AgentNotification) => void): void {
    this.onAgentNotification = handler;
  }

  setOnConnectionStatusChange(handler: (isConnected: boolean) => void): void {
    this.onConnectionStatusChange = handler;
  }

  async updateAgentStatus(status: "online" | "away" | "busy" | "offline"): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot update agent status");
      return;
    }

    try {
      await this.connection.invoke("UpdateAgentStatus", status);
    } catch (error) {
      console.error("Failed to update agent status:", error);
    }
  }

  async acceptConversation(conversationId: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot accept conversation");
      return;
    }

    try {
      await this.connection.invoke("AcceptConversation", conversationId);
    } catch (error) {
      console.error("Failed to accept conversation:", error);
    }
  }

  async transferConversation(conversationId: string, targetAgentId: string, reason: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot transfer conversation");
      return;
    }

    try {
      await this.connection.invoke("TransferConversation", conversationId, targetAgentId, reason);
    } catch (error) {
      console.error("Failed to transfer conversation:", error);
    }
  }

  async escalateConversation(conversationId: string, reason: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot escalate conversation");
      return;
    }

    try {
      await this.connection.invoke("EscalateConversation", conversationId, reason);
    } catch (error) {
      console.error("Failed to escalate conversation:", error);
    }
  }

  async requestAssistance(conversationId: string, message: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot request assistance");
      return;
    }

    try {
      await this.connection.invoke("RequestAssistance", conversationId, message);
    } catch (error) {
      console.error("Failed to request assistance:", error);
    }
  }

  async broadcastToAgents(message: string, messageType: string = "info"): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot broadcast message");
      return;
    }

    try {
      await this.connection.invoke("BroadcastToAgents", message, messageType);
    } catch (error) {
      console.error("Failed to broadcast message:", error);
    }
  }

  async joinConversation(conversationId: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot join conversation");
      return;
    }

    try {
      await this.connection.invoke("JoinConversation", conversationId);
    } catch (error) {
      console.error("Failed to join conversation:", error);
    }
  }

  async leaveConversation(conversationId: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot leave conversation");
      return;
    }

    try {
      await this.connection.invoke("LeaveConversation", conversationId);
    } catch (error) {
      console.error("Failed to leave conversation:", error);
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch (e) {
        console.warn("Error while stopping SignalR connection:", e);
      }
      this.connection = null;
      this.isConnected = false;
      this.handlersWired = false;
      this.agentId = null;
      this.tenantId = null;
      this.onConnectionStatusChange?.(false);
    }
  }

  getConnectionStatus(): boolean {
    return this.isConnected;
  }

  getAgentId(): string | null {
    return this.agentId;
  }

  getTenantId(): string | null {
    return this.tenantId;
  }
}

export const agentSignalRService = new AgentSignalRService();
export default agentSignalRService;
