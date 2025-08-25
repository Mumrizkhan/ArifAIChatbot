import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

interface TenantAnalyticsData {
  conversations: {
    total: number;
    active: number;
    resolved: number;
    averageDuration: number;
    satisfactionScore: number;
  };
  agents: {
    online: number;
    busy: number;
    averageResponseTime: number;
    utilization: number;
  };
  customers: {
    total: number;
    returning: number;
    newToday: number;
    averageRating: number;
  };
  performance: {
    resolutionRate: number;
    firstResponseTime: number;
    escalationRate: number;
    botAccuracy: number;
  };
  trends: {
    conversationVolume: Array<{ date: string; count: number }>;
    satisfactionTrend: Array<{ date: string; score: number }>;
    responseTimeTrend: Array<{ date: string; time: number }>;
  };
  realtime: {
    activeConversations: number;
    waitingCustomers: number;
    onlineAgents: number;
    currentLoad: number;
  };
}

interface TenantNotification {
  id: string;
  type: 'info' | 'warning' | 'error' | 'success';
  title: string;
  message: string;
  timestamp: Date;
  tenantId: string;
}

class TenantSignalRService {
  private connection: HubConnection | null = null;
  private isConnected = false;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 5000;
  private tenantId: string | null = null;

  private onAnalyticsUpdate?: (analytics: TenantAnalyticsData) => void;
  private onRealtimeUpdate?: (realtime: TenantAnalyticsData['realtime']) => void;
  private onTenantNotification?: (notification: TenantNotification) => void;
  private onConnectionStatusChange?: (isConnected: boolean) => void;

  async connect(authToken: string, tenantId: string): Promise<boolean> {
    if (this.isConnected) {
      return true;
    }

    this.tenantId = tenantId;

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl('/chatHub', {
          accessTokenFactory: () => authToken,
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
        .configureLogging(LogLevel.Information)
        .build();

      this.setupEventHandlers();
      
      await this.connection.start();
      this.isConnected = true;
      
      console.log('Tenant SignalR connection established');
      this.onConnectionStatusChange?.(true);
      
      await this.connection.invoke('JoinTenantGroup', tenantId);
      
      return true;
    } catch (error) {
      console.error('Failed to connect to SignalR hub:', error);
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      return false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on('TenantAnalyticsUpdated', (analytics: TenantAnalyticsData) => {
      this.onAnalyticsUpdate?.(analytics);
    });

    this.connection.on('TenantRealtimeUpdated', (realtime: TenantAnalyticsData['realtime']) => {
      this.onRealtimeUpdate?.(realtime);
    });

    this.connection.on('TenantNotification', (notification: TenantNotification) => {
      this.onTenantNotification?.(notification);
    });

    this.connection.onclose(() => {
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      console.log('Tenant SignalR connection closed');
    });

    this.connection.onreconnecting(() => {
      this.isConnected = false;
      this.onConnectionStatusChange?.(false);
      console.log('Tenant SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      this.onConnectionStatusChange?.(true);
      console.log('Tenant SignalR reconnected');
      if (this.tenantId) {
        this.connection?.invoke('JoinTenantGroup', this.tenantId);
      }
    });
  }

  setOnAnalyticsUpdate(handler: (analytics: TenantAnalyticsData) => void): void {
    this.onAnalyticsUpdate = handler;
  }

  setOnRealtimeUpdate(handler: (realtime: TenantAnalyticsData['realtime']) => void): void {
    this.onRealtimeUpdate = handler;
  }

  setOnTenantNotification(handler: (notification: TenantNotification) => void): void {
    this.onTenantNotification = handler;
  }

  setOnConnectionStatusChange(handler: (isConnected: boolean) => void): void {
    this.onConnectionStatusChange = handler;
  }

  async requestTenantAnalyticsUpdate(startDate: Date, endDate: Date): Promise<void> {
    if (!this.isConnected || !this.connection || !this.tenantId) {
      console.warn('SignalR not connected or tenant ID missing, cannot request analytics update');
      return;
    }

    try {
      await this.connection.invoke('RequestTenantAnalyticsUpdate', this.tenantId, startDate.toISOString(), endDate.toISOString());
    } catch (error) {
      console.error('Failed to request tenant analytics update:', error);
    }
  }

  async requestTenantRealtimeUpdate(): Promise<void> {
    if (!this.isConnected || !this.connection || !this.tenantId) {
      console.warn('SignalR not connected or tenant ID missing, cannot request realtime update');
      return;
    }

    try {
      await this.connection.invoke('RequestTenantRealtimeUpdate', this.tenantId);
    } catch (error) {
      console.error('Failed to request tenant realtime update:', error);
    }
  }

  disconnect(): void {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
      this.isConnected = false;
      this.tenantId = null;
      this.onConnectionStatusChange?.(false);
    }
  }

  getConnectionStatus(): boolean {
    return this.isConnected;
  }

  getTenantId(): string | null {
    return this.tenantId;
  }
}

export const tenantSignalRService = new TenantSignalRService();
export default tenantSignalRService;
