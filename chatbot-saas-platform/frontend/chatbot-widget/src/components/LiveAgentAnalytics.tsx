import React, { useState, useEffect, useCallback } from 'react';
import { analyticsService } from '../services/analyticsService';
import './LiveAgentAnalytics.css';

interface LiveAgentAnalyticsProps {
  tenantId: string;
  dateRange?: {
    from: string;
    to: string;
  };
}

interface DashboardData {
  totalAgentRequests?: number;
  agentAssignments?: number;
  averageWaitTime?: number;
  averageResponseTime?: number;
  averageRating?: number;
  conversationsWithAgents?: number;
  topAgents?: Array<{
    agentId: string;
    agentName: string;
    conversations: number;
    averageRating: number;
    responseTime: number;
    conversationsHandled?: number;
    averageResponseTime?: number;
  }>;
}

interface MetricsData {
  totalAgentRequests: number;
  successfulAssignments: number;
  averageWaitTime: number;
  averageResponseTime: number;
  customerSatisfaction: number;
  conversationsCompleted: number;
  agentMetrics: Array<{
    agentId: string;
    agentName: string;
    conversations: number;
    averageRating: number;
    responseTime: number;
  }>;
}

export const LiveAgentAnalytics: React.FC<LiveAgentAnalyticsProps> = ({ 
  tenantId, 
  dateRange 
}) => {
  const [metrics, setMetrics] = useState<MetricsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const fetchAnalytics = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const dashboardData = await analyticsService.getDashboardData(tenantId, dateRange) as DashboardData;
      
      // Transform the data for display
      const transformedMetrics: MetricsData = {
        totalAgentRequests: dashboardData.totalAgentRequests || 0,
        successfulAssignments: dashboardData.agentAssignments || 0,
        averageWaitTime: dashboardData.averageWaitTime || 0,
        averageResponseTime: dashboardData.averageResponseTime || 0,
        customerSatisfaction: dashboardData.averageRating || 0,
        conversationsCompleted: dashboardData.conversationsWithAgents || 0,
        agentMetrics: (dashboardData.topAgents || []).map((agent) => ({
          agentId: agent.agentId,
          agentName: agent.agentName || `Agent ${agent.agentId}`,
          conversations: agent.conversationsHandled || 0,
          averageRating: agent.averageRating || 0,
          responseTime: agent.averageResponseTime || 0,
        }))
      };

      setMetrics(transformedMetrics);
    } catch (err) {
      console.error('Failed to fetch analytics:', err);
      setError('Failed to load analytics data');
    } finally {
      setLoading(false);
    }
  }, [tenantId, dateRange]);

  const handleRefresh = async () => {
    setRefreshing(true);
    await fetchAnalytics();
    setRefreshing(false);
  };

  useEffect(() => {
    fetchAnalytics();
  }, [fetchAnalytics]);

  const formatDuration = (milliseconds: number): string => {
    const seconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (hours > 0) {
      return `${hours}h ${minutes % 60}m`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    } else {
      return `${seconds}s`;
    }
  };

  const formatRating = (rating: number): string => {
    return rating.toFixed(1);
  };

  const getPerformanceColor = (rating: number): string => {
    if (rating >= 4.5) return '#10b981'; // green
    if (rating >= 4.0) return '#3b82f6'; // blue
    if (rating >= 3.5) return '#f59e0b'; // yellow
    if (rating >= 3.0) return '#ef4444'; // red
    return '#6b7280'; // gray
  };

  const getSatisfactionIcon = (rating: number): string => {
    if (rating >= 4.5) return '😊';
    if (rating >= 4.0) return '🙂';
    if (rating >= 3.5) return '😐';
    if (rating >= 3.0) return '🙁';
    return '😞';
  };

  if (loading) {
    return (
      <div className="analytics-loading">
        <div className="loading-spinner"></div>
        <p>Loading analytics...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="analytics-error">
        <div className="error-icon">⚠️</div>
        <p>{error}</p>
        <button onClick={handleRefresh} className="retry-button">
          Try Again
        </button>
      </div>
    );
  }

  if (!metrics) {
    return (
      <div className="analytics-empty">
        <p>No analytics data available</p>
      </div>
    );
  }

  const assignmentRate = metrics.totalAgentRequests > 0 
    ? (metrics.successfulAssignments / metrics.totalAgentRequests * 100).toFixed(1)
    : '0';

  return (
    <div className="live-agent-analytics">
      <div className="analytics-header">
        <h2>Live Agent Analytics</h2>
        <button 
          onClick={handleRefresh} 
          className={`refresh-button ${refreshing ? 'refreshing' : ''}`}
          disabled={refreshing}
        >
          🔄 {refreshing ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {/* Key Metrics Overview */}
      <div className="metrics-grid">
        <div className="metric-card">
          <div className="metric-icon">📞</div>
          <div className="metric-content">
            <h3>Agent Requests</h3>
            <div className="metric-value">{metrics.totalAgentRequests}</div>
            <div className="metric-subtitle">
              {assignmentRate}% assignment rate
            </div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon">⏱️</div>
          <div className="metric-content">
            <h3>Avg Wait Time</h3>
            <div className="metric-value">{formatDuration(metrics.averageWaitTime)}</div>
            <div className="metric-subtitle">Time to agent assignment</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon">💬</div>
          <div className="metric-content">
            <h3>Avg Response Time</h3>
            <div className="metric-value">{formatDuration(metrics.averageResponseTime)}</div>
            <div className="metric-subtitle">Agent response time</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon">{getSatisfactionIcon(metrics.customerSatisfaction)}</div>
          <div className="metric-content">
            <h3>Customer Satisfaction</h3>
            <div 
              className="metric-value"
              style={{ color: getPerformanceColor(metrics.customerSatisfaction) }}
            >
              {formatRating(metrics.customerSatisfaction)}/5.0
            </div>
            <div className="metric-subtitle">Average rating</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon">✅</div>
          <div className="metric-content">
            <h3>Conversations</h3>
            <div className="metric-value">{metrics.conversationsCompleted}</div>
            <div className="metric-subtitle">Handled by agents</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon">👥</div>
          <div className="metric-content">
            <h3>Active Agents</h3>
            <div className="metric-value">{metrics.agentMetrics.length}</div>
            <div className="metric-subtitle">Currently available</div>
          </div>
        </div>
      </div>

      {/* Agent Performance Table */}
      {metrics.agentMetrics.length > 0 && (
        <div className="agent-performance-section">
          <h3>Agent Performance</h3>
          <div className="agent-table">
            <div className="table-header">
              <div className="header-cell">Agent</div>
              <div className="header-cell">Conversations</div>
              <div className="header-cell">Avg Response</div>
              <div className="header-cell">Rating</div>
              <div className="header-cell">Performance</div>
            </div>
            {metrics.agentMetrics.map((agent) => (
              <div key={agent.agentId} className="table-row">
                <div className="cell agent-info">
                  <div className="agent-name">{agent.agentName}</div>
                  <div className="agent-id">ID: {agent.agentId}</div>
                </div>
                <div className="cell">{agent.conversations}</div>
                <div className="cell">{formatDuration(agent.responseTime)}</div>
                <div className="cell">
                  <span style={{ color: getPerformanceColor(agent.averageRating) }}>
                    {formatRating(agent.averageRating)}/5.0
                  </span>
                </div>
                <div className="cell">
                  <div 
                    className="performance-bar"
                    style={{ 
                      '--performance': `${(agent.averageRating / 5) * 100}%`,
                      '--color': getPerformanceColor(agent.averageRating)
                    } as React.CSSProperties}
                  >
                    <div className="performance-fill"></div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Real-time Status */}
      <div className="realtime-status">
        <h3>System Status</h3>
        <div className="status-indicators">
          <div className="status-item">
            <div className="status-dot active"></div>
            <span>Analytics Collection: Active</span>
          </div>
          <div className="status-item">
            <div className="status-dot active"></div>
            <span>Agent Assignment: Working</span>
          </div>
          <div className="status-item">
            <div className="status-dot active"></div>
            <span>Feedback Collection: Enabled</span>
          </div>
        </div>
      </div>
    </div>
  );
};
