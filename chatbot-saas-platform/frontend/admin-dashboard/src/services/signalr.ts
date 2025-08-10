import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
interface DashboardStats {
  totalTenants: number;
  totalUsers: number;
  totalConversations: number;
  activeAgents: number;
  monthlyRevenue: number;
  growthRate: number;
}

interface SystemNotification {
  id: string;
  type: "info" | "warning" | "error" | "success";
  title: string;
  message: string;
  timestamp: Date;
  tenantId?: string;
}

interface ConversationMetrics {
  totalConversations: number;
  averageDuration: number;
  resolutionRate: number;
  satisfactionScore: number;
  dailyData: Array<{
    date: string;
    conversations: number;
    resolved: number;
    avgDuration: number;
  }>;
}

interface AgentMetrics {
  totalAgents: number;
  activeAgents: number;
  averageResponseTime: number;
  averageRating: number;
  topPerformers: Array<{
    id: string;
    name: string;
    rating: number;
    conversationsHandled: number;
  }>;
}

class AdminSignalRService {
  private connection: HubConnection | null = null;
  private isConnected = false;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000;

  private onDashboardStatsUpdate?: (stats: DashboardStats) => void;
  private onConversationMetricsUpdate?: (metrics: ConversationMetrics) => void;
  private onAgentMetricsUpdate?: (metrics: AgentMetrics) => void;
  private onSystemNotification?: (notification: SystemNotification) => void;
  private onConnectionStatusChange?: (isConnected: boolean) => void;

  async connect(authToken: string): Promise<boolean> {
    if (this.isConnected) {
      return true;
    }

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}chat/chatHub`, {
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

      console.log("Admin SignalR connection established");
      this.onConnectionStatusChange?.(true);

      await this.connection.invoke("JoinAdminGroup");

      return true;
    } catch (error) {
      console.error("Failed to connect to SignalR hub:", error);
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      return false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on("DashboardStatsUpdated", (stats: DashboardStats) => {
      this.onDashboardStatsUpdate?.(stats);
    });

    this.connection.on("ConversationMetricsUpdated", (metrics: ConversationMetrics) => {
      this.onConversationMetricsUpdate?.(metrics);
    });

    this.connection.on("AgentMetricsUpdated", (metrics: AgentMetrics) => {
      this.onAgentMetricsUpdate?.(metrics);
    });

    this.connection.on("SystemNotification", (notification: SystemNotification) => {
      this.onSystemNotification?.(notification);
    });

    this.connection.onclose(() => {
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      console.log("Admin SignalR connection closed");
    });

    this.connection.onreconnecting(() => {
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      console.log("Admin SignalR reconnecting...");
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      this.onConnectionStatusChange?.(true);
      console.log("Admin SignalR reconnected");
      this.connection?.invoke("JoinAdminGroup");
    });
  }

  setOnDashboardStatsUpdate(handler: (stats: DashboardStats) => void): void {
    this.onDashboardStatsUpdate = handler;
  }

  setOnConversationMetricsUpdate(handler: (metrics: ConversationMetrics) => void): void {
    this.onConversationMetricsUpdate = handler;
  }

  setOnAgentMetricsUpdate(handler: (metrics: AgentMetrics) => void): void {
    this.onAgentMetricsUpdate = handler;
  }

  setOnSystemNotification(handler: (notification: SystemNotification) => void): void {
    this.onSystemNotification = handler;
  }

  setOnConnectionStatusChange(handler: (isConnected: boolean) => void): void {
    this.onConnectionStatusChange = handler;
  }

  async requestDashboardStatsUpdate(): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot request dashboard stats update");
      return;
    }

    try {
      await this.connection.invoke("RequestDashboardStatsUpdate");
    } catch (error) {
      console.error("Failed to request dashboard stats update:", error);
    }
  }

  async requestConversationMetricsUpdate(timeRange: string, tenantId?: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot request conversation metrics update");
      return;
    }

    try {
      await this.connection.invoke("RequestConversationMetricsUpdate", timeRange, tenantId);
    } catch (error) {
      console.error("Failed to request conversation metrics update:", error);
    }
  }

  async requestAgentMetricsUpdate(timeRange: string, tenantId?: string): Promise<void> {
    if (!this.isConnected || !this.connection) {
      console.warn("SignalR not connected, cannot request agent metrics update");
      return;
    }

    try {
      await this.connection.invoke("RequestAgentMetricsUpdate", timeRange, tenantId);
    } catch (error) {
      console.error("Failed to request agent metrics update:", error);
    }
  }

  disconnect(): void {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
    }
  }

  getConnectionStatus(): boolean {
    return this.isConnected;
  }
}

export const adminSignalRService = new AdminSignalRService();
export default adminSignalRService;
