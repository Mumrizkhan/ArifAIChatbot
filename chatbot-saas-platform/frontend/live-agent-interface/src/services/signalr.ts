import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

interface AgentStatusUpdate {
  agentId: string;
  status: "online" | "away" | "busy" | "offline";
  timestamp: Date;
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

interface ConversationEscalation {
  conversationId: string;
  agentId: string;
  reason: string;
  priority: string;
  timestamp: Date;
}

interface AssistanceRequest {
  conversationId: string;
  requestingAgentId: string;
  message: string;
  timestamp: Date;
}

interface BroadcastMessage {
  message: string;
  type: string;
  fromAgentId: string;
  timestamp: Date;
}

interface AgentNotification {
  id: string;
  type: "info" | "warning" | "error" | "success";
  title: string;
  message: string;
  timestamp: Date;
  agentId: string;
}

class AgentSignalRService {
  private connection: HubConnection | null = null;
  private isConnected = false;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000;
  private agentId: string | null = null;
  private tenantId: string | null = null;

  private onAgentStatusChanged?: (status: AgentStatusUpdate) => void;
  private onConversationAssigned?: (assignment: ConversationAssignment) => void;
  private onConversationTaken?: (assignment: ConversationAssignment) => void;
  private onConversationTransferred?: (transfer: ConversationTransfer) => void;
  private onConversationEscalated?: (escalation: ConversationEscalation) => void;
  private onMessageReceived?: (message: any) => void;
  private onAssistanceRequested?: (request: AssistanceRequest) => void;
  private onBroadcastMessage?: (message: BroadcastMessage) => void;
  private onConnectionStatusChange?: (isConnected: boolean) => void;

  async connect(authToken: string, agentId: string, tenantId: string): Promise<boolean> {
    console.log("connect() called with agentId:", agentId);

    if (this.isConnected) {
      console.warn("Already connected to SignalR");
      return true;
    }

    if (!agentId) {
      console.error("Invalid agentId:", agentId);
      return false;
    }

    this.agentId = agentId;
    this.tenantId = tenantId;

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl(`https://api-stg-arif.tetco.sa/agent/agentHub`, {
          accessTokenFactory: () => authToken,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
              return this.reconnectDelay;
            }
            return null;
          },
        })
        .configureLogging(LogLevel.Information)
        .build();

      this.setupEventHandlers();
      await this.connection.start();
      this.isConnected = true;

      console.log("Agent SignalR connection established");
      this.onConnectionStatusChange?.(true);

      console.log("Agent ID being sent to JoinAgentGroup:", this.agentId);
      await this.connection.invoke("JoinAgentGroup", this.agentId);

      return true;
    } catch (error) {
      console.error("Failed to connect to AgentHub:", error);
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      return false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on("AgentStatusChanged", (status: AgentStatusUpdate) => {
      this.onAgentStatusChanged?.(status);
    });

    this.connection.on("ConversationAssigned", (assignment: ConversationAssignment) => {
      this.onConversationAssigned?.(assignment);
    });

    this.connection.on("ConversationTaken", (assignment: ConversationAssignment) => {
      this.onConversationTaken?.(assignment);
    });

    this.connection.on("ConversationTransferred", (transfer: ConversationTransfer) => {
      this.onConversationTransferred?.(transfer);
    });

    this.connection.on("ConversationEscalated", (escalation: ConversationEscalation) => {
      this.onConversationEscalated?.(escalation);
    });

    this.connection.on("ReceiveMessage", (message: any) => {
      this.onMessageReceived?.(message);
    });

    this.connection.on("AssistanceRequested", (request: AssistanceRequest) => {
      this.onAssistanceRequested?.(request);
    });

    this.connection.on("BroadcastMessage", (message: BroadcastMessage) => {
      this.onBroadcastMessage?.(message);
    });

    this.connection.onclose((error) => {
      console.error("SignalR connection closed:", error);
    });

    this.connection.onreconnecting((error) => {
      console.warn("SignalR reconnecting:", error);
    });

    this.connection.onreconnected(() => {
      console.log("SignalR reconnected");
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
    console.log("Agent notification handler set:", handler);
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

  disconnect(): void {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
      this.isConnected = false;
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
}

export const agentSignalRService = new AgentSignalRService();
export default agentSignalRService;
