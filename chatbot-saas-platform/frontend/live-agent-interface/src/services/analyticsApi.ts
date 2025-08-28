import { apiClient } from './api';

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
  getDashboardStats: () =>
    apiClient.get<DashboardStats>('/analytics/dashboard'),
  
  getConversationMetrics: (timeRange: string = '7d', tenantId?: string) =>
    apiClient.get<ConversationMetrics>('/analytics/conversations', { timeRange, tenantId }),
  
  getAgentMetrics: (timeRange: string = '7d', tenantId?: string) =>
    apiClient.get<AgentMetrics>('/analytics/agents', { timeRange, tenantId }),
  
  getPerformanceMetrics: (dateFrom?: string, dateTo?: string) =>
    apiClient.get<PerformanceMetrics>('/analytics/performance', { dateFrom, dateTo }),
  
  getRealtimeAnalytics: () =>
    apiClient.get<RealtimeAnalytics>('/analytics/realtime'),

  getAnalytics: (dateFrom?: string, dateTo?: string, tenantId?: string) =>
    apiClient.get<AnalyticsData>('/analytics/analytics', { dateFrom, dateTo, tenantId }),
};
