#!/bin/bash

# Notification System Integration Test Script
# This script tests the complete notification system flow

echo "?? Starting Notification System Integration Tests..."
echo "=================================================="

# Configuration
BASE_URL="https://localhost:7005"
NOTIFICATION_API="$BASE_URL/api/notification-test"

# Test data
TEST_USER_ID="123e4567-e89b-12d3-a456-426614174000"
TEST_TENANT_ID="123e4567-e89b-12d3-a456-426614174001"
TEST_CONVERSATION_ID="123e4567-e89b-12d3-a456-426614174002"
TEST_AGENT_ID="123e4567-e89b-12d3-a456-426614174003"

echo ""
echo "?? Testing Health Check..."
echo "------------------------"
curl -s "$BASE_URL/health" | jq '.' || echo "? Health check failed"

echo ""
echo "?? Testing Welcome Notification..."
echo "--------------------------------"
curl -X POST "$NOTIFICATION_API/test-welcome" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$TEST_USER_ID\",
    \"tenantId\": \"$TEST_TENANT_ID\",
    \"userName\": \"Test User\"
  }" \
  -s | jq '.' || echo "? Welcome notification test failed"

echo ""
echo "?? Testing Conversation Assignment..."
echo "-----------------------------------"
curl -X POST "$NOTIFICATION_API/test-conversation-assignment" \
  -H "Content-Type: application/json" \
  -d "{
    \"conversationId\": \"$TEST_CONVERSATION_ID\",
    \"agentId\": \"$TEST_AGENT_ID\",
    \"tenantId\": \"$TEST_TENANT_ID\",
    \"customerName\": \"John Doe\"
  }" \
  -s | jq '.' || echo "? Conversation assignment test failed"

echo ""
echo "?? Testing Bulk Notification..."
echo "-----------------------------"
curl -X POST "$NOTIFICATION_API/test-bulk-notification" \
  -H "Content-Type: application/json" \
  -d "{
    \"tenantId\": \"$TEST_TENANT_ID\",
    \"userIds\": [
      \"$TEST_USER_ID\",
      \"123e4567-e89b-12d3-a456-426614174004\"
    ],
    \"title\": \"Test Bulk Notification\",
    \"content\": \"This is a test bulk notification message.\"
  }" \
  -s | jq '.' || echo "? Bulk notification test failed"

echo ""
echo "?? Testing System Alert..."
echo "-------------------------"
curl -X POST "$NOTIFICATION_API/test-system-alert" \
  -H "Content-Type: application/json" \
  -d "{
    \"tenantId\": \"$TEST_TENANT_ID\",
    \"alertType\": \"HighResourceUsage\",
    \"title\": \"Test System Alert\",
    \"description\": \"This is a test system alert for resource usage.\",
    \"severity\": \"Warning\"
  }" \
  -s | jq '.' || echo "? System alert test failed"

echo ""
echo "?? Integration tests completed!"
echo "=============================="
echo ""
echo "?? Next steps:"
echo "1. Check RabbitMQ management console: http://localhost:15672"
echo "2. Review application logs for detailed processing information"
echo "3. Test SignalR real-time connections from your frontend"
echo "4. Configure email/SMS providers for actual delivery testing"