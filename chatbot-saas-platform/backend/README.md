# Arif Platform Backend Services

This directory contains all the microservices for the Arif Platform AI chatbot solution.

## Architecture

The backend consists of 10 microservices built with .NET 8:

1. **IdentityService** (Port 5001) - Authentication and authorization
2. **TenantManagementService** (Port 5002) - Multi-tenant management
3. **AIOrchestrationService** (Port 5003) - AI and LLM integration
4. **ChatRuntimeService** (Port 5004) - Real-time chat functionality
5. **LiveAgentService** (Port 5005) - Human agent management
6. **KnowledgeBaseService** (Port 5006) - Document and vector search
7. **NotificationService** (Port 5007) - Email, SMS, and push notifications
8. **WorkflowService** (Port 5008) - Business process automation
9. **SubscriptionService** (Port 5009) - Billing and subscription management
10. **AnalyticsService** (Port 5010) - Analytics and reporting

## Infrastructure Dependencies

- **SQL Server** (Port 1433) - Primary database
- **Redis** (Port 6379) - Caching and session storage
- **RabbitMQ** (Port 5672) - Message queue and event bus
- **Qdrant** (Port 6333) - Vector database for RAG

## Quick Start

1. **Start Infrastructure Services:**
   ```bash
   ./start-infrastructure.sh
   ```

2. **Build the Solution:**
   ```bash
   dotnet build ArifPlatform.sln
   ```

3. **Start All Microservices:**
   ```bash
   ./start-services.sh
   ```

## Seed Data

The system includes comprehensive seed data that is automatically applied when running in development mode:

- **Users**: Super admin, tenant admins, and agents
- **Tenants**: Multiple test tenants with different configurations
- **Conversations & Messages**: Realistic conversation history
- **Analytics Data**: Sample metrics and usage data
- **Subscription Plans**: Starter, Professional, and Enterprise plans
- **Workflow Templates**: Common automation patterns
- **Knowledge Base**: Sample documents and FAQs
- **Notification Templates**: Email and SMS templates

## Database Configuration

All services use a shared `ApplicationDbContext` with the following databases:
- `ArifPlatform_Identity`
- `ArifPlatform_TenantManagement`
- `ArifPlatform_Analytics`
- etc.

Connection strings are configured in each service's `appsettings.json`.

## Development

Each microservice follows Clean Architecture principles with:
- Domain-driven design (DDD)
- CQRS with MediatR
- Multi-tenant data isolation
- Comprehensive logging and auditing
- JWT-based authentication

## API Documentation

When running in development mode, each service exposes Swagger documentation at:
- `http://localhost:{port}/swagger`

## Monitoring

- RabbitMQ Management: http://localhost:15672 (guest/guest)
- Qdrant Dashboard: http://localhost:6333/dashboard
