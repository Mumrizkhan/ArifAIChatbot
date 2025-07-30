# Multitenant SaaS AI Arif Platform

A comprehensive multitenant SaaS AI Arif Platform solution with four React frontends and .NET Core microservices architecture.

## Architecture Overview

### Frontend Applications
- **Admin Dashboard** - System administration and global management
- **Chatbot Widget** - Embeddable chat interface for end-users
- **Live Agent Interface** - Human agent workspace for customer support
- **Tenant Dashboard** - Tenant-specific management and configuration

### Backend Microservices
- **Identity Service** - Authentication, authorization, and user management
- **Tenant Management Service** - Multi-tenant configuration and isolation
- **AI Orchestration Service** - AI model management and conversation orchestration
- **Chat Runtime Service** - Real-time messaging and conversation management
- **Live Agent Service** - Human agent management and routing
- **Knowledge Base Service** - Document management and RAG implementation
- **Notification Service** - Multi-channel notification delivery
- **Workflow Service** - Business process automation and orchestration
- **Subscription Service** - Billing, subscription, and usage management
- **Analytics Service** - Advanced data analytics, metrics, and reporting

### Technology Stack

#### Frontend
- React 18+ with TypeScript
- Tailwind CSS for styling
- Redux Toolkit for state management
- React Hook Form with Zod validation
- React-i18next for multi-language support (English/Arabic)
- Dynamic theming system for tenant customization

#### Backend
- .NET 8 with C# 12
- Clean Architecture + Domain-Driven Design
- CQRS with MediatR pattern
- Entity Framework Core 8
- JWT authentication with refresh tokens
- Policy-based authorization

#### Data Layer
- SQL Server 2022 (Primary database)
- Redis 7+ (Caching)
- RabbitMQ 3.12+ (Message broker)
- Qdrant (Vector database for RAG)

## Getting Started

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- Docker & Docker Compose
- SQL Server
- Redis
- RabbitMQ
- Qdrant

### Development Setup

1. Clone the repository
2. Set up backend services: `cd backend && dotnet restore`
3. Set up frontend applications: `cd frontend && npm install`
4. Start infrastructure services: `docker-compose up -d`
5. Run database migrations
6. Start development servers

## Project Structure

```
arif-platform/
├── frontend/
│   ├── admin-dashboard/          # Admin Dashboard React app
│   ├── chatbot-widget/           # Embeddable Chatbot Widget
│   ├── live-agent-interface/     # Live Agent Interface
│   └── tenant-dashboard/         # Tenant Dashboard
├── backend/
│   └── src/
│       ├── IdentityService/      # Authentication & Authorization
│       ├── TenantManagementService/
│       ├── AIOrchestrationService/
│       ├── ChatRuntimeService/
│       ├── LiveAgentService/
│       ├── KnowledgeBaseService/
│       ├── NotificationService/
│       ├── WorkflowService/
│       ├── SubscriptionService/
│       ├── AnalyticsService/
│       └── Shared/               # Shared libraries
├── infrastructure/
│   ├── docker/                   # Docker configurations
│   ├── kubernetes/               # K8s manifests
│   └── scripts/                  # Deployment scripts
└── docs/                         # Documentation
```

## Features

### Multi-Language Support
- English and Arabic with RTL support
- Dynamic language switching
- Tenant-specific translations
- Localized content management

### Tenant Customization
- Dynamic theming system
- Custom branding and logos
- Configurable UI components
- White-label solutions

### Advanced Analytics
- Real-time metrics and dashboards
- Predictive analytics
- Custom report generation
- Performance monitoring

### Workflow Automation
- Visual workflow designer
- Complex decision trees
- Human task assignments
- External system integrations

## License

This project is proprietary software. All rights reserved.
