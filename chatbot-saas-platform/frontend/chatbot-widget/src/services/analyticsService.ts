import { apiClient } from './apiClient';

export interface AnalyticsEvent {
  id?: string;
  eventType: string;
  eventData: Record<string, unknown>;
  timestamp: string;
  sessionId: string;
  conversationId?: string;
  agentId?: string;
  userId?: string;
  tenantId: string;
  metadata?: Record<string, unknown>;
}

export interface LiveAgentAnalytics {
  // Agent assignment events
  agentRequested: {
    conversationId: string;
    requestedAt: string;
    waitTime?: number; // time from first message to agent request
  };
  agentAssigned: {
    conversationId: string;
    agentId: string;
    agentName: string;
    assignedAt: string;
    waitTime: number; // time from request to assignment
    queuePosition?: number;
  };
  agentJoined: {
    conversationId: string;
    agentId: string;
    agentName: string;
    joinedAt: string;
    responseTime?: number; // time from assignment to first response
  };
  agentLeft: {
    conversationId: string;
    agentId: string;
    agentName: string;
    leftAt: string;
    sessionDuration: number;
  };

  // Conversation events
  conversationEscalated: {
    conversationId: string;
    fromAgentId?: string;
    toAgentId: string;
    escalatedAt: string;
    reason: string;
  };
  conversationTransferred: {
    conversationId: string;
    fromAgentId: string;
    toAgentId: string;
    transferredAt: string;
    reason?: string;
  };
  conversationEnded: {
    conversationId: string;
    agentId?: string;
    endedAt: string;
    endedBy: 'customer' | 'agent' | 'system';
    duration: number;
    messageCount: number;
    resolutionStatus: 'resolved' | 'unresolved' | 'escalated';
  };

  // Message events
  messageReceived: {
    conversationId: string;
    messageId: string;
    senderId: string;
    senderType: 'customer' | 'agent' | 'bot';
    messageType: 'text' | 'file' | 'image';
    receivedAt: string;
    responseTime?: number; // time between messages
  };
  messageSent: {
    conversationId: string;
    messageId: string;
    agentId?: string;
    messageType: 'text' | 'file' | 'image';
    sentAt: string;
    length: number;
  };

  // Customer satisfaction
  feedbackRequested: {
    conversationId: string;
    agentId?: string;
    requestedAt: string;
    feedbackType: 'rating' | 'survey' | 'comment';
  };
  feedbackSubmitted: {
    conversationId: string;
    agentId?: string;
    submittedAt: string;
    rating?: number;
    feedback?: string;
    satisfaction: 'satisfied' | 'neutral' | 'unsatisfied';
  };

  // Performance metrics
  responseTime: {
    conversationId: string;
    agentId: string;
    messageId: string;
    responseTimeMs: number;
    measuredAt: string;
  };
  sessionDuration: {
    conversationId: string;
    agentId?: string;
    startTime: string;
    endTime: string;
    durationMs: number;
  };
}

/**
 * Analytics service for reading analytics data from the backend.
 * Note: Analytics events are now created automatically by the backend 
 * when actions occur, rather than being sent from the frontend.
 */
class AnalyticsService {
  private isEnabled: boolean = true;

  /**
   * Get analytics dashboard data for a tenant
   */
  async getDashboardData(tenantId: string, dateRange?: { from: string; to: string }): Promise<unknown> {
    try {
      const params = new URLSearchParams();
      if (dateRange) {
        params.append('from', dateRange.from);
        params.append('to', dateRange.to);
      }

      const response = await apiClient.get(`/api/analytics/dashboard/${tenantId}?${params.toString()}`);
      return response;
    } catch (error) {
      console.error('❌ Analytics: Failed to fetch dashboard data:', error);
      throw error;
    }
  }

  /**
   * Get live agent performance metrics
   */
  async getAgentMetrics(agentId: string, dateRange?: { from: string; to: string }): Promise<unknown> {
    try {
      const params = new URLSearchParams();
      if (dateRange) {
        params.append('from', dateRange.from);
        params.append('to', dateRange.to);
      }

      const response = await apiClient.get(`/api/analytics/agents/${agentId}/metrics?${params.toString()}`);
      return response;
    } catch (error) {
      console.error('❌ Analytics: Failed to fetch agent metrics:', error);
      throw error;
    }
  }

  /**
   * Get conversation analytics
   */
  async getConversationAnalytics(conversationId: string): Promise<unknown> {
    try {
      const response = await apiClient.get(`/api/analytics/conversations/${conversationId}`);
      return response;
    } catch (error) {
      console.error('❌ Analytics: Failed to fetch conversation analytics:', error);
      throw error;
    }
  }

  /**
   * Get real-time analytics data for a tenant
   */
  async getRealtimeAnalytics(tenantId: string): Promise<unknown> {
    try {
      const response = await apiClient.get(`/api/analytics/realtime/${tenantId}`);
      return response;
    } catch (error) {
      console.error('❌ Analytics: Failed to fetch realtime analytics:', error);
      throw error;
    }
  }

  /**
   * Enable or disable analytics service
   */
  setEnabled(enabled: boolean): void {
    this.isEnabled = enabled;
  }

  /**
   * Check if analytics is enabled
   */
  isAnalyticsEnabled(): boolean {
    return this.isEnabled;
  }
}

export const analyticsService = new AnalyticsService();
