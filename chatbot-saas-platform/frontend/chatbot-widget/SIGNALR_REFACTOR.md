# SignalR Event Handler Refactoring

## Problem
Previously, SignalR event handlers for conversation-specific events (like `ReceiveMessage`, `ConversationAssigned`, etc.) were being registered during the initial connection setup, before any conversation was created. This caused issues where:

1. Events could be received for conversations that didn't exist yet
2. Event handlers were not properly scoped to specific conversations
3. The conversation ID was not available when handlers were registered

## Solution
Refactored the SignalR service to separate connection-level and conversation-specific event handlers:

### Connection-Level Handlers (Setup during `connect()`)
- `onreconnecting` - Connection state management
- `onreconnected` - Connection state management + re-setup conversation handlers
- `onclose` - Connection state management

### Conversation-Specific Handlers (Setup during `joinConversation()`)
- `ReceiveMessage` - Chat messages
- `UserStartedTyping` / `UserStoppedTyping` - Typing indicators
- `AgentAssigned` / `ConversationAssigned` - Agent assignment
- `ConversationStatusChanged` - Status updates
- `AgentJoined` / `AgentLeft` - Agent presence
- `ConversationEnded` - Conversation lifecycle

## New Flow
1. **Widget initialization** → SignalR connects with connection-level handlers only
2. **Conversation creation** → `startConversation()` async thunk calls `signalRService.startConversation(conversationId)`
3. **SignalR conversation setup** → `startConversation()` calls `joinConversation()` which calls `setupConversationEventHandlers()`
4. **Event handling** → Events are now properly scoped to the active conversation

## New Methods Added
- `setupConversationEventHandlers()` - Sets up conversation-specific event handlers
- `removeConversationEventHandlers()` - Cleans up conversation-specific handlers
- `startConversation(conversationId)` - Joins conversation and sets up handlers
- `setupExistingConversation(conversationId)` - Sets up handlers for existing conversations (reconnection scenarios)

## Key Benefits
- Events are only registered after conversation is created
- Proper conversation context for all events
- Clean separation of connection vs conversation concerns
- Better error handling and debugging
- Prevents race conditions between connection and conversation setup
