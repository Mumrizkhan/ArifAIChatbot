# Live Agent Analytics Implementation

## Overview
This document outlines the comprehensive analytics implementation for live agent functionality in the chatbot widget.

## ✅ Completed Implementation

### Frontend Components

1. **Analytics Service** (`src/services/analyticsService.ts`)
   - Event tracking and batching functionality
   - API communication with backend
   - Automatic flush on page unload
   - Methods: `trackLiveAgentEvent`, `trackFeedback`, `getDashboardData`, `getAgentMetrics`

2. **Redux Store Integration** (`src/store/slices/chatSlice.ts`)
   - Enhanced all live agent actions with analytics tracking
   - Events tracked: agent requests, assignments, messages, feedback
   - Seamless integration with existing state management

3. **WebSocket Integration** (`src/services/websocket.ts`)
   - Added analytics tracking to `ConversationAssigned` event handler
   - Tracks agent joins with timestamp and metadata

4. **UI Components**
   - **LiveAgentAnalytics.tsx**: Complete dashboard component with metrics visualization
   - **LiveAgentAnalytics.css**: Comprehensive styling with responsive design and dark mode support
   - Features: real-time metrics, agent performance table, responsive layout

### Backend Infrastructure

1. **API Controller** (`src/backend/Controllers/AnalyticsController.cs`)
   - Endpoints: batch events, dashboard data, agent metrics, real-time analytics
   - Full CRUD operations for analytics data
   - Proper error handling and validation

2. **Data Models** (`src/backend/Models/AnalyticsModels.cs`)
   - `AnalyticsEvent` entity with JSONB properties column
   - DTOs for API responses: `LiveAgentAnalyticsSummary`, `AgentMetrics`, etc.
   - Comprehensive data structures for all analytics needs

3. **Database Schema** (`src/backend/Data/ApplicationDbContext.Analytics.cs`)
   - DbSet configuration for AnalyticsEvent
   - Performance indexes on frequently queried columns
   - Materialized view for aggregated metrics

4. **Database Migration** (`src/backend/Migrations/AddAnalyticsSupport.cs`)
   - Creates analytics_events table
   - Adds indexes for optimal query performance
   - Creates materialized view for dashboard queries

5. **Analytics Service** (`src/backend/Services/AnalyticsService.cs`)
   - Complex analytics calculations
   - Event processing and aggregation
   - Performance optimized queries using raw SQL

## 🔧 Required Backend Setup

### Service Registration
Add the following to your backend's `Program.cs` or `Startup.cs`:

```csharp
// Register Analytics Service
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
```

### Database Migration
Run the following command in your backend project:

```bash
dotnet ef database update
```

### File Locations
The backend files are currently located in `src/backend/` but should be moved to your actual backend project:

- Move `Controllers/AnalyticsController.cs` to your backend Controllers folder
- Move `Models/AnalyticsModels.cs` to your backend Models folder  
- Move `Services/AnalyticsService.cs` to your backend Services folder
- Move `Data/ApplicationDbContext.Analytics.cs` to merge with your DbContext
- Move `Migrations/AddAnalyticsSupport.cs` to your backend Migrations folder

## 📊 Analytics Events Tracked

### Live Agent Events
- `agentRequested`: When user requests human agent
- `agentAssigned`: When agent is assigned to conversation
- `agentJoined`: When agent joins conversation (via WebSocket)
- `agentResponseTime`: Agent response timing metrics
- `conversationEscalated`: When conversation is escalated to agent

### Customer Feedback Events
- `feedbackSubmitted`: Customer satisfaction ratings
- `conversationRated`: Overall conversation ratings

### Conversation Events
- `messagesSent`: All messages in agent conversations
- `conversationCompleted`: When agent conversations end

## 🎯 Dashboard Metrics

### Key Performance Indicators
- Total agent requests
- Successful agent assignments
- Average wait time for agent assignment
- Average agent response time
- Customer satisfaction scores
- Conversations completed by agents

### Agent Performance Metrics
- Individual agent statistics
- Response time per agent
- Customer satisfaction per agent
- Active conversations per agent
- Historical performance data

## 🔄 Real-time Features
- Live agent status monitoring
- Real-time conversation metrics
- Automatic dashboard refresh
- WebSocket-based event tracking

## 📱 UI Features
- Responsive design for all screen sizes
- Dark mode support
- Interactive metrics cards
- Agent performance tables
- Real-time status indicators
- Error handling and loading states

## 🚀 Usage

### Frontend Integration
```typescript
import { LiveAgentAnalytics } from './components/LiveAgentAnalytics';

// Use in your admin dashboard
<LiveAgentAnalytics 
  tenantId="your-tenant-id" 
  dateRange={{ from: '2024-01-01', to: '2024-01-31' }} 
/>
```

### Manual Event Tracking
```typescript
import { analyticsService } from './services/analyticsService';

// Track custom events
analyticsService.trackLiveAgentEvent('customEvent', {
  conversationId: 'conv-123',
  customData: { key: 'value' }
});
```

## 🔍 Testing
- All TypeScript compilation errors resolved
- CSS styling implemented with comprehensive responsive design
- Components ready for integration
- Backend APIs structured for easy integration

## 📝 Notes
- All frontend components are TypeScript-safe
- CSS includes responsive design and accessibility features
- Backend follows .NET best practices
- Database schema optimized for analytics queries
- Event batching reduces API calls for better performance
