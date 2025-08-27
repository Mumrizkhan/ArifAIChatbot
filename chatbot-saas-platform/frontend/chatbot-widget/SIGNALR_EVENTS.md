# SignalR Event Naming Convention - Unified Documentation

## Overview
All SignalR events use **PascalCase** to match C# naming conventions from the backend.

## ChatHub Events (Customer/Chatbot Side)
These events are sent to customers/chatbot widgets connected to the ChatHub:

### Outgoing Events (from backend to frontend):
- `JoinedConversation` - Confirmation when user joins a conversation group
- `ReceiveMessage` - When a new message is received in the conversation
- `UserStartedTyping` - When another user starts typing
- `UserStoppedTyping` - When another user stops typing
- `ConversationAssigned` - When an agent is assigned to the conversation (sent from AgentHub)

### Incoming Events (from frontend to backend):
- `JoinConversation` - Join a conversation group
- `LeaveConversation` - Leave a conversation group
- `SendMessage` - Send a message to the conversation
- `StartTyping` - Indicate user started typing
- `StopTyping` - Indicate user stopped typing

## AgentHub Events (Live Agent Side)
These events are sent to live agents connected to the AgentHub:

### Outgoing Events (from backend to frontend):
- `AgentStatusChanged` - When an agent's status changes
- `ConversationAssigned` - When a conversation is assigned to an agent
- `ConversationTaken` - When another agent takes a conversation
- `ConversationTransferred` - When a conversation is transferred
- `ConversationEscalated` - When a conversation is escalated
- `AssistanceRequested` - When an agent requests assistance
- `BroadcastMessage` - Broadcast messages to all agents
- `MessageReceived` - When a new message is received

## Removed Invalid Events
The following events were removed from the frontend as they don't exist in the backend:
- `AgentAssigned` (replaced with `ConversationAssigned`)
- `ConversationStatusChanged` (not implemented in backend)
- `AgentJoined` (not implemented in backend)
- `AgentLeft` (not implemented in backend)
- `ConversationEnded` (not implemented in backend)

## Case Sensitivity Rules
- **Always use PascalCase** for event names
- **Event handlers are case-sensitive** in SignalR JavaScript client
- **Property names in payload** can vary (ConversationId vs conversationId) - handle both

## Cross-Service Communication
- ChatHub sends `ReceiveMessage` to customers
- AgentHub sends `ConversationAssigned` to agents
- Cross-service notifications use HTTP APIs with tenant headers
