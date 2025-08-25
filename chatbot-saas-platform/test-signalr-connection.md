# SignalR Connection Testing Guide

## Issues Fixed:

1. **CORS Configuration Mismatch** - Fixed incompatible CORS settings between API Gateway and services
2. **Frontend Credentials Configuration** - Updated to use `withCredentials: true`
3. **Enhanced Debugging** - Added comprehensive logging
4. **WebSocket Support** - Added explicit WebSocket middleware

## Testing Steps:

### 1. Backend Testing
1. Start the backend services (ChatRuntimeService, LiveAgentService)
2. Check the console logs for CORS and SignalR initialization messages
3. Verify the services are listening on the correct ports

### 2. Frontend Testing - Chatbot Widget
1. Open browser developer tools (F12)
2. Go to Console tab
3. Load the chatbot widget
4. Look for these log messages:
   - "SignalR connecting..." with connection details
   - "SignalR requesting auth token..."
   - "SignalR connected successfully"
   - "SignalR: Invoking JoinConversation with ID:"
   - "SignalR: Successfully joined conversation:"

### 3. Frontend Testing - Live Agent Interface
1. Open browser developer tools
2. Login to the live agent interface
3. Check console for SignalR connection logs
4. Verify agent status changes are received

### 4. Test Message Flow
1. Send a message from the chatbot widget
2. Check console for "SignalR: ReceiveMessage event received:" logs
3. Verify messages appear in the UI

## Common Issues to Check:

### Browser Network Tab
- Check for failed requests to SignalR endpoints
- Look for 401 (Unauthorized) or 403 (Forbidden) errors
- Verify CORS preflight requests are successful

### Backend Logs
- Look for "JoinConversation called with ID:" messages
- Check for "Conversation not found for tenant" warnings
- Verify user and tenant IDs are being resolved correctly

### Authentication Issues
- Ensure auth tokens are being passed correctly
- Check if the Authorization attribute needs to be enabled on Hubs
- Verify token validation in JWT middleware

## Debugging Commands:

### Check SignalR Connection State (Browser Console):
```javascript
// If you have access to the signalRService instance
console.log("Connection State:", signalRService.getConnectionState());
console.log("Is Connected:", signalRService.isConnected());
```

### Enable Verbose SignalR Logging (Update frontend):
```typescript
.configureLogging(LogLevel.Debug) // Change from Information to Debug
```

### Test Manual SignalR Connection (Browser Console):
```javascript
// Test direct connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:8000/chat/chatHub", {
        accessTokenFactory: () => "your-token-here"
    })
    .build();

connection.start().then(() => {
    console.log("Direct connection successful");
}).catch(err => {
    console.error("Direct connection failed:", err);
});
```

## Next Steps if Issues Persist:

1. **Check Firewall/Network**: Ensure ports are open and accessible
2. **Verify Service Discovery**: If using containers, check service networking
3. **Test Without Gateway**: Connect directly to services to isolate gateway issues
4. **Enable Authorization**: Uncomment [Authorize] attributes if authentication is required
5. **Check Database**: Verify conversations and tenants exist in the database
6. **Validate JWT**: Check if tokens are valid and not expired

## Files Modified:
- `backend/src/ChatRuntimeService/Program.cs` - CORS and WebSocket configuration
- `backend/src/LiveAgentService/Program.cs` - CORS configuration  
- `frontend/chatbot-widget/src/services/websocket.ts` - Connection and debugging
- `frontend/live-agent-interface/src/services/signalr.ts` - Credentials configuration
- `frontend/tenant-dashboard/src/services/signalr.ts` - Credentials configuration
- `backend/src/ChatRuntimeService/Hubs/ChatHub.cs` - Enhanced logging
