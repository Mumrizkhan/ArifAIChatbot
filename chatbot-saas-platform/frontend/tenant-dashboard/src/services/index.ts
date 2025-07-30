// Central export file for all services
export { apiClient } from './api';
export type { ApiResponse, ApiError } from './api';

// Auth Service
export { AuthService } from './authService';
export type {
  LoginCredentials,
  RegisterData,
  User,
  AuthResponse,
  ForgotPasswordData,
  ResetPasswordData,
  ChangePasswordData
} from './authService';

// Tenant Service
export { TenantService } from './tenantService';
export type {
  Tenant,
  TenantSettings,
  TenantSubscription,
  CreateTenantData,
  UpdateTenantData
} from './tenantService';

// Team Service
export { TeamService } from './teamService';
export type {
  TeamMember,
  TeamRole,
  InviteTeamMemberData,
  UpdateTeamMemberData,
  CreateRoleData,
  TeamStats
} from './teamService';

// Chatbot Service
export { ChatbotService } from './chatbotService';
export type {
  Chatbot,
  ChatbotSettings,
  KnowledgeBaseItem,
  ChatbotAnalytics,
  CreateChatbotData,
  UpdateChatbotData,
  CreateKnowledgeBaseData,
  ChatMessage,
  Conversation,
  WorkingHoursSchedule
} from './chatbotService';

// Analytics Service
export { AnalyticsService } from './analyticsService';
export type {
  AnalyticsData,
  OverviewMetrics,
  UserMetrics,
  ChatbotMetrics,
  SubscriptionMetrics,
  PerformanceMetrics,
  CustomReport,
  CreateReportData,
  ExportData
} from './analyticsService';

// Subscription Service
export { SubscriptionService } from './subscriptionService';
export type {
  Subscription,
  Plan,
  PlanFeature,
  PlanLimits,
  Invoice,
  InvoiceLineItem,
  PaymentMethod,
  UsageMetrics,
  BillingAddress,
  CreateSubscriptionData,
  UpdateSubscriptionData
} from './subscriptionService';

// Service helper functions
export const handleApiError = (error: any): string => {
  if (error?.message) {
    return error.message;
  }
  if (typeof error === 'string') {
    return error;
  }
  return 'An unexpected error occurred';
};

export const isApiError = (error: any): error is { message: string; status: number; code?: string } => {
  return error && typeof error === 'object' && 'message' in error && 'status' in error;
};
