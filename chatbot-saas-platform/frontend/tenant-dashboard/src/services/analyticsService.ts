import { apiClient, ApiResponse } from './api';

// Analytics related types
export interface AnalyticsData {
  overview: OverviewMetrics;
  userMetrics: UserMetrics;
  chatbotMetrics: ChatbotMetrics;
  subscriptionMetrics: SubscriptionMetrics;
  performanceMetrics: PerformanceMetrics;
}

export interface OverviewMetrics {
  totalUsers: number;
  activeUsers: number;
  totalConversations: number;
  totalRevenue: number;
  growthRate: number;
  retentionRate: number;
}

export interface UserMetrics {
  newUsers: Array<{ date: string; count: number }>;
  activeUsers: Array<{ date: string; count: number }>;
  usersByCountry: Array<{ country: string; count: number }>;
  usersByDevice: Array<{ device: string; count: number }>;
  userRetention: Array<{ period: string; rate: number }>;
}

export interface ChatbotMetrics {
  totalConversations: number;
  averageSessionDuration: number;
  messageVolume: Array<{ date: string; count: number }>;
  topIntents: Array<{ intent: string; count: number }>;
  satisfactionScores: Array<{ date: string; score: number }>;
  responseAccuracy: number;
}

export interface SubscriptionMetrics {
  mrr: number; // Monthly Recurring Revenue
  arr: number; // Annual Recurring Revenue
  churnRate: number;
  ltv: number; // Lifetime Value
  revenueByPlan: Array<{ plan: string; revenue: number }>;
  subscriptionGrowth: Array<{ date: string; count: number }>;
}

export interface PerformanceMetrics {
  apiResponseTime: Array<{ endpoint: string; avgTime: number }>;
  systemUptime: number;
  errorRate: number;
  resourceUsage: {
    cpu: number;
    memory: number;
    storage: number;
  };
}

export interface CustomReport {
  id: string;
  name: string;
  description: string;
  type: string;
  filters: Record<string, any>;
  schedule?: {
    frequency: 'daily' | 'weekly' | 'monthly';
    time: string;
    recipients: string[];
  };
  createdAt: string;
  updatedAt: string;
}

export interface CreateReportData {
  name: string;
  description: string;
  type: string;
  filters: Record<string, any>;
  schedule?: {
    frequency: 'daily' | 'weekly' | 'monthly';
    time: string;
    recipients: string[];
  };
}

export interface ExportData {
  format: 'csv' | 'xlsx' | 'pdf';
  type: 'users' | 'conversations' | 'revenue' | 'custom';
  dateRange: {
    from: string;
    to: string;
  };
  filters?: Record<string, any>;
}

export class AnalyticsService {
  // Get overview analytics
  static async getOverview(params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<OverviewMetrics>> {
    return apiClient.get<OverviewMetrics>('/analytics/overview', params);
  }

  // Get user analytics
  static async getUserAnalytics(params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<UserMetrics>> {
    return apiClient.get<UserMetrics>('/analytics/users', params);
  }

  // Get chatbot analytics
  static async getChatbotAnalytics(params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<ChatbotMetrics>> {
    return apiClient.get<ChatbotMetrics>('/analytics/chatbot', params);
  }

  // Get subscription analytics
  static async getSubscriptionAnalytics(params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<SubscriptionMetrics>> {
    return apiClient.get<SubscriptionMetrics>('/analytics/subscription', params);
  }

  // Get performance analytics
  static async getPerformanceAnalytics(params?: {
    dateFrom?: string;
    dateTo?: string;
  }): Promise<ApiResponse<PerformanceMetrics>> {
    return apiClient.get<PerformanceMetrics>('/analytics/performance', params);
  }

  // Get all analytics data
  static async getAllAnalytics(params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<AnalyticsData>> {
    return apiClient.get<AnalyticsData>('/analytics', params);
  }

  // Custom Reports
  static async getReports(): Promise<ApiResponse<CustomReport[]>> {
    return apiClient.get<CustomReport[]>('/analytics/reports');
  }

  static async getReport(id: string): Promise<ApiResponse<CustomReport>> {
    return apiClient.get<CustomReport>(`/analytics/reports/${id}`);
  }

  static async createReport(data: CreateReportData): Promise<ApiResponse<CustomReport>> {
    return apiClient.post<CustomReport>('/analytics/reports', data);
  }

  static async updateReport(id: string, data: Partial<CreateReportData>): Promise<ApiResponse<CustomReport>> {
    return apiClient.put<CustomReport>(`/analytics/reports/${id}`, data);
  }

  static async deleteReport(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/analytics/reports/${id}`);
  }

  static async runReport(id: string): Promise<ApiResponse<any>> {
    return apiClient.post<any>(`/analytics/reports/${id}/run`);
  }

  // Export functionality
  static async exportData(data: ExportData): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.post<{ downloadUrl: string }>('/analytics/export', data);
  }

  // Real-time metrics
  static async getRealTimeMetrics(): Promise<ApiResponse<{
    activeUsers: number;
    ongoingConversations: number;
    systemLoad: number;
    responseTime: number;
  }>> {
    return apiClient.get('/analytics/realtime');
  }

  // Comparison analytics
  static async getComparison(params: {
    metric: string;
    currentPeriod: { from: string; to: string };
    previousPeriod: { from: string; to: string };
  }): Promise<ApiResponse<{
    current: any;
    previous: any;
    change: number;
    changePercent: number;
  }>> {
    return apiClient.post('/analytics/compare', params);
  }

  // Goal tracking
  static async getGoals(): Promise<ApiResponse<Array<{
    id: string;
    name: string;
    target: number;
    current: number;
    progress: number;
    deadline: string;
  }>>> {
    return apiClient.get('/analytics/goals');
  }

  static async createGoal(data: {
    name: string;
    target: number;
    metric: string;
    deadline: string;
  }): Promise<ApiResponse<any>> {
    return apiClient.post('/analytics/goals', data);
  }

  static async updateGoal(id: string, data: {
    name?: string;
    target?: number;
    deadline?: string;
  }): Promise<ApiResponse<any>> {
    return apiClient.put(`/analytics/goals/${id}`, data);
  }

  static async deleteGoal(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/analytics/goals/${id}`);
  }
}
