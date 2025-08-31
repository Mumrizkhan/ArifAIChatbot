# Notification System Integration Test Script (PowerShell)
# This script tests the complete notification system flow

Write-Host "?? Starting Notification System Integration Tests..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green

# Configuration
$BaseUrl = "https://localhost:7005"
$NotificationApi = "$BaseUrl/api/notification-test"

# Test data
$TestUserId = "123e4567-e89b-12d3-a456-426614174000"
$TestTenantId = "123e4567-e89b-12d3-a456-426614174001"
$TestConversationId = "123e4567-e89b-12d3-a456-426614174002"
$TestAgentId = "123e4567-e89b-12d3-a456-426614174003"

Write-Host ""
Write-Host "?? Testing Health Check..." -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Cyan

try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get
    Write-Host "? Health check successful: $($healthResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "? Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Testing Welcome Notification..." -ForegroundColor Cyan
Write-Host "--------------------------------" -ForegroundColor Cyan

try {
    $welcomeBody = @{
        userId = $TestUserId
        tenantId = $TestTenantId
        userName = "Test User"
    } | ConvertTo-Json

    $welcomeResponse = Invoke-RestMethod -Uri "$NotificationApi/test-welcome" -Method Post -Body $welcomeBody -ContentType "application/json"
    Write-Host "? Welcome notification test successful: $($welcomeResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "? Welcome notification test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Testing Conversation Assignment..." -ForegroundColor Cyan
Write-Host "-----------------------------------" -ForegroundColor Cyan

try {
    $assignmentBody = @{
        conversationId = $TestConversationId
        agentId = $TestAgentId
        tenantId = $TestTenantId
        customerName = "John Doe"
    } | ConvertTo-Json

    $assignmentResponse = Invoke-RestMethod -Uri "$NotificationApi/test-conversation-assignment" -Method Post -Body $assignmentBody -ContentType "application/json"
    Write-Host "? Conversation assignment test successful: $($assignmentResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "? Conversation assignment test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Testing Bulk Notification..." -ForegroundColor Cyan
Write-Host "-----------------------------" -ForegroundColor Cyan

try {
    $bulkBody = @{
        tenantId = $TestTenantId
        userIds = @($TestUserId, "123e4567-e89b-12d3-a456-426614174004")
        title = "Test Bulk Notification"
        content = "This is a test bulk notification message."
    } | ConvertTo-Json

    $bulkResponse = Invoke-RestMethod -Uri "$NotificationApi/test-bulk-notification" -Method Post -Body $bulkBody -ContentType "application/json"
    Write-Host "? Bulk notification test successful: $($bulkResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "? Bulk notification test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Testing System Alert..." -ForegroundColor Cyan
Write-Host "-------------------------" -ForegroundColor Cyan

try {
    $alertBody = @{
        tenantId = $TestTenantId
        alertType = "HighResourceUsage"
        title = "Test System Alert"
        description = "This is a test system alert for resource usage."
        severity = "Warning"
    } | ConvertTo-Json

    $alertResponse = Invoke-RestMethod -Uri "$NotificationApi/test-system-alert" -Method Post -Body $alertBody -ContentType "application/json"
    Write-Host "? System alert test successful: $($alertResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "? System alert test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Integration tests completed!" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host ""
Write-Host "?? Next steps:" -ForegroundColor Yellow
Write-Host "1. Check RabbitMQ management console: http://localhost:15672" -ForegroundColor White
Write-Host "2. Review application logs for detailed processing information" -ForegroundColor White
Write-Host "3. Test SignalR real-time connections from your frontend" -ForegroundColor White
Write-Host "4. Configure email/SMS providers for actual delivery testing" -ForegroundColor White