import { apiClient } from "./api";

export interface DashboardStats {
  totalTenants: number;
  activeTenants: number;
  totalUsers: number;
  activeUsers: number;
  totalConversations: number;
  activeConversations: number;
  totalMessages: number;
  todayMessages: number;
}

export interface ConversationMetrics {
  total: number;
  completed: number;
  active: number;
  averageRating: number;
}

export interface AgentMetrics {
  totalAgents: number;
  activeAgents: number;
  averageResponseTime: number;
  averageRating: number;
}

export interface PerformanceMetrics {
  averageResponseTime: number;
  systemUptime: number;
  errorRate: number;
}

export interface RealtimeAnalytics {
  activeUsers: number;
  ongoingConversations: number;
  availableAgents: number;
  systemLoad: number;
}

export interface AnalyticsData {
  data: Record<string, any>;
  timeRange: string;
  generatedAt: string;
}

export const analyticsApi = {
  getDashboardStats: (tenantId?: string) => apiClient.get<DashboardStats>("/analytics/analytics/dashboard", tenantId ? { tenantId } : {}),

  getConversationMetrics: (timeRange: string = "7d", tenantId?: string) =>
    apiClient.get<ConversationMetrics>("/analytics/analytics/conversations", { timeRange, tenantId }),

  getAgentMetrics: (timeRange: string = "7d", tenantId?: string) =>
    apiClient.get<AgentMetrics>("/analytics/analytics/agents", { timeRange, tenantId }),

  getPerformanceMetrics: (dateFrom?: string, dateTo?: string) =>
    apiClient.get<PerformanceMetrics>("/agent/agents/performance", { dateFrom, dateTo }),

  getRealtimeAnalytics: () => apiClient.get<RealtimeAnalytics>("/analytics/analytics/realtime"),

  getAnalytics: (dateFrom?: string, dateTo?: string, tenantId?: string) =>
    apiClient.post<AnalyticsData>("/analytics/analytics", { dateFrom, dateTo, tenantId }),

  getAgentStats: (agentId: string, startDate?: string, endDate?: string) =>
    apiClient.get<any>(`/agent/agents/${agentId}/stats`, { startDate, endDate }),

  getAgentPerformance: (startDate?: string, endDate?: string) => apiClient.get<any>("/agent/agents/performance", { startDate, endDate }),
};
