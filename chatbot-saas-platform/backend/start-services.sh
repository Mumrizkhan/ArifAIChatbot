#!/bin/bash

echo "Starting Arif Platform Microservices..."

cd src/IdentityService && dotnet run --urls="http://localhost:5001" &
cd ../TenantManagementService && dotnet run --urls="http://localhost:5002" &
cd ../AIOrchestrationService && dotnet run --urls="http://localhost:5003" &
cd ../ChatRuntimeService && dotnet run --urls="http://localhost:5004" &
cd ../LiveAgentService && dotnet run --urls="http://localhost:5005" &
cd ../KnowledgeBaseService && dotnet run --urls="http://localhost:5006" &
cd ../NotificationService && dotnet run --urls="http://localhost:5007" &
cd ../WorkflowService && dotnet run --urls="http://localhost:5008" &
cd ../SubscriptionService && dotnet run --urls="http://localhost:5009" &
cd ../AnalyticsService && dotnet run --urls="http://localhost:5010" &

echo "All microservices started!"
echo "Identity Service: http://localhost:5001"
echo "Tenant Management: http://localhost:5002"
echo "AI Orchestration: http://localhost:5003"
echo "Chat Runtime: http://localhost:5004"
echo "Live Agent: http://localhost:5005"
echo "Knowledge Base: http://localhost:5006"
echo "Notification: http://localhost:5007"
echo "Workflow: http://localhost:5008"
echo "Subscription: http://localhost:5009"
echo "Analytics: http://localhost:5010"

wait
