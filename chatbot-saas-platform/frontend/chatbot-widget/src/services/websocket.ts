import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { store } from "../store/store";
import { addMessage, setTyping, setConnectionStatus, assignAgent, updateConversationStatus, markMessageAsRead } from "../store/slices/chatSlice";
import { ensureISOString } from "../utils/timestamp";

class SignalRService {
  private connection: HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private currentConversationId: string | null = null;
  private handlersSetUp = false; // Track if conversation handlers are set up
  private hubUrl: string = import.meta.env.VITE_WEBSOCKET_URL || `${import.meta.env.VITE_API_BASE_URL || "http://localhost:8000"}/chat/chathub`;

  async connect(tenantId: string, authToken: string, conversationId?: string): Promise<boolean> {
    try {
      console.log("SignalR connecting...", { tenantId, hubUrl: this.hubUrl, hasAuthToken: !!authToken });
      console.log("SignalR authToken (decoded):", this.decodeJWT(authToken));
      store.dispatch(setConnectionStatus("connecting"));

      this.connection = new HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          accessTokenFactory: () => {
            console.log("SignalR requesting auth token...");
            return authToken;
          },
          skipNegotiation: false,
          withCredentials: true,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
            return null; // Stop retrying
          },
        })
        .configureLogging(LogLevel.Information)
        .build();

      // Set up connection-level event handlers BEFORE starting connection
      this.setupEventHandlers();

      await this.connection.start();
      console.log("SignalR connected successfully", { state: this.connection.state });
      store.dispatch(setConnectionStatus("connected"));
      this.reconnectAttempts = 0;

      if (conversationId) {
        console.log("Joining conversation:", conversationId);
        await this.joinConversation(conversationId);
        this.currentConversationId = conversationId;

        // Set up conversation event handlers AFTER joining conversation
        console.log("Setting up conversation event handlers for:", conversationId);
        this.setupConversationEventHandlers();

        // Test if we can send a test message to the hub
        try {
          console.log("🧪 Testing SignalR communication with hub...");
          await this.connection.invoke("StartTyping", conversationId);
          console.log("✅ SignalR communication test successful");
        } catch (error) {
          console.error("❌ SignalR communication test failed:", error);
        }
      }

      return true;
    } catch (error) {
      console.error("SignalR connection failed:", error);
      store.dispatch(setConnectionStatus("error"));
      this.scheduleReconnect(tenantId, authToken, conversationId);
      return false;
    }
  }

  // Helper method to decode JWT for debugging
  private decodeJWT(token: string): unknown {
    try {
      const base64Url = token.split(".")[1];
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split("")
          .map(function (c) {
            return "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2);
          })
          .join("")
      );
      return JSON.parse(jsonPayload);
    } catch (e) {
      console.error("Failed to decode JWT:", e);
      return null;
    }
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    // Connection-level event handlers (set up once during connection)
    this.connection.onreconnecting(() => {
      console.log("SignalR reconnecting...");
      store.dispatch(setConnectionStatus("connecting"));
    });

    this.connection.onreconnected(async () => {
      console.log("SignalR reconnected");
      store.dispatch(setConnectionStatus("connected"));

      if (this.currentConversationId) {
        console.log("Reconnection detected - rejoining conversation and setting up handlers");

        // First rejoin the conversation
        const rejoined = await this.joinConversation(this.currentConversationId);

        if (rejoined) {
          // Then set up conversation handlers after successful rejoin
          console.log("Successfully rejoined conversation after reconnection, setting up handlers");
          this.setupConversationEventHandlers();
        } else {
          console.error("Failed to rejoin conversation after reconnection");
        }
      }
    });

    this.connection.onclose(() => {
      console.log("SignalR connection closed");
      store.dispatch(setConnectionStatus("disconnected"));

      // Reset handlers flag when connection is closed
      this.handlersSetUp = false;
      console.log("🔄 Reset handlers flag due to connection closure");
    });
  }

  private setupConversationEventHandlers() {
    if (!this.connection) {
      console.warn("Cannot setup conversation handlers - no connection");
      return;
    }

    if (this.connection.state !== HubConnectionState.Connected) {
      console.warn("Cannot setup conversation handlers - connection not in Connected state:", this.connection.state);
      return;
    }

    console.log("Setting up conversation-specific event handlers");

    // Check if handlers are already set up for this conversation
    if (this.handlersSetUp && this.connection.state === HubConnectionState.Connected) {
      console.log("Event handlers already set up and connection is active - skipping duplicate setup");
      return;
    }

    console.log("Setting up fresh event handlers for conversation:", this.currentConversationId);

    // Handler for conversation join confirmation - using camelCase to match serialized backend response
    this.connection.on("JoinedConversation", (data) => {
      console.log("🎉 SignalR: JoinedConversation confirmation received:", data);
      console.log("🎉 SignalR: Should now be in group:", `conversation_${data.conversationId}`);
      console.log("🎉 SignalR: Ready to receive ReceiveMessage events");
    });

    this.connection.on("ReceiveMessage", (messageDto) => {
      console.log("🔥 SignalR: ReceiveMessage event received:", messageDto);
      console.log("🔥 SignalR: Current conversation ID:", this.currentConversationId);
      console.log("🔥 SignalR: Message conversation ID:", messageDto.conversationId);
      console.log("🔥 SignalR: Message sender:", messageDto.sender);
      console.log("🔥 SignalR: Message content:", messageDto.content);
      console.log("🔥 SignalR: Full messageDto:", JSON.stringify(messageDto, null, 2));

      // Only process messages for the current conversation
      if (!this.currentConversationId || messageDto.conversationId === this.currentConversationId) {
        // Filter out user messages to prevent duplicates (user messages are added immediately in MessageInput.tsx)
        // Only process messages from agents, bots, or system
        const senderType = messageDto.sender?.toLowerCase();
        if (senderType === "user" || senderType === "customer") {
          console.log("🚫 SignalR: Ignoring user message to prevent duplicate (already added locally):", messageDto.id);
          return;
        }

        console.log("✅ SignalR: Processing non-user message for current conversation");
        store.dispatch(
          addMessage({
            id: messageDto.id,
            content: messageDto.content,
            sender: messageDto.sender.toLowerCase(),
            timestamp: ensureISOString(messageDto.createdAt), // Guaranteed to be an ISO string
            type: messageDto.type.toLowerCase(),
            metadata: {
              senderId: messageDto.senderId?.toString(),
              senderName: messageDto.senderName,
            },
          })
        );
        console.log("✅ SignalR: Message dispatched to store successfully");
      } else {
        console.log("❌ SignalR: Ignoring message for different conversation:", messageDto.conversationId);
      }
    });

    this.connection.on("UserStartedTyping", (typingInfo) => {
      console.log("SignalR: UserStartedTyping event received:", typingInfo);
      console.log("SignalR: Current conversation ID:", this.currentConversationId);

      // Only process typing events for the current conversation
      const typingConversationId = typingInfo.conversationId;
      if (!this.currentConversationId || typingConversationId === this.currentConversationId) {
        store.dispatch(
          setTyping({
            isTyping: true,
            user: typingInfo.userName || "User",
          })
        );
      } else {
        console.log("SignalR: Ignoring typing event for different conversation:", typingConversationId);
      }
    });

    this.connection.on("UserStoppedTyping", (typingInfo) => {
      console.log("SignalR: UserStoppedTyping event received:", typingInfo);
      console.log("SignalR: Current conversation ID:", this.currentConversationId);

      // Only process typing events for the current conversation
      const typingConversationId = typingInfo.conversationId;
      if (!this.currentConversationId || typingConversationId === this.currentConversationId) {
        store.dispatch(
          setTyping({
            isTyping: false,
            user: typingInfo.userName || "User",
          })
        );
      } else {
        console.log("SignalR: Ignoring typing event for different conversation:", typingConversationId);
      }
    });

    // Note: AgentAssigned, AgentJoined, AgentLeft events come from LiveAgentService, not ChatHub
    // The frontend should receive ConversationAssigned from ChatHub instead
    this.connection.on("ConversationAssigned", (assignmentInfo) => {
      console.log("🔥 SignalR: ConversationAssigned event received:", assignmentInfo);
      console.log("🔥 SignalR: Current conversation ID:", this.currentConversationId);

      // Check if this assignment is for the current conversation
      const assignmentConversationId = assignmentInfo.conversationId;
      console.log("🔥 SignalR: Assignment conversation ID:", assignmentConversationId);

      if (!this.currentConversationId || assignmentConversationId === this.currentConversationId) {
        console.log("✅ SignalR: Processing conversation assignment for current conversation");

        // Handle camelCase payload from the backend
        const agentId = assignmentInfo.agentId;
        const agentName = assignmentInfo.agentName || "Agent";
        const agentAvatar = assignmentInfo.agentAvatar;

        if (agentId) {
          console.log("SignalR: Agent assigned to conversation:", { agentId, agentName });

          store.dispatch(
            assignAgent({
              id: agentId.toString(),
              name: agentName,
              avatar: agentAvatar,
            })
          );

          // Update conversation status to indicate agent is now handling the conversation
          store.dispatch(updateConversationStatus("active"));

          // Add a system message to indicate agent assignment
          store.dispatch(
            addMessage({
              id: `system-agent-assigned-${Date.now()}`,
              content: `${agentName} has joined the conversation`,
              sender: "bot",
              type: "text",
              timestamp: new Date().toISOString(),
              metadata: {
                systemMessage: true,
                agentId: agentId.toString(),
                agentName: agentName,
              },
            })
          );
        } else {
          console.warn("SignalR: ConversationAssigned - No valid agent ID found in payload:", assignmentInfo);
        }
      } else {
        console.log("SignalR: Ignoring assignment for different conversation:", assignmentConversationId);
      }
    });

    // Handler for message read status updates
    this.connection.on("MessageMarkedAsRead", (readInfo) => {
      console.log("🔄 SignalR: MessageMarkedAsRead event received:", readInfo);
      console.log("🔄 SignalR: Message ID:", readInfo.messageId);
      console.log("🔄 SignalR: Reader Type:", readInfo.readerType);
      console.log("🔄 SignalR: Current conversation ID:", this.currentConversationId);
      
      // Only process read notifications for the current conversation
      if (!this.currentConversationId || readInfo.conversationId === this.currentConversationId) {
        console.log("✅ SignalR: Processing read notification for current conversation");
        
        // Update the message read status in the store
        store.dispatch(markMessageAsRead({
          messageId: readInfo.messageId
        }));
        
        console.log("✅ SignalR: Message read status updated in store");
      } else {
        console.log("❌ SignalR: Ignoring read notification for different conversation:", readInfo.conversationId);
      }
    });

    // Mark handlers as set up
    this.handlersSetUp = true;
    console.log("✅ All conversation event handlers set up successfully");
  }

  private removeConversationEventHandlers() {
    if (!this.connection) return;

    console.log("Removing existing conversation event handlers");

    // Remove all conversation-specific event handlers - using PascalCase to match backend
    this.connection.off("JoinedConversation");
    this.connection.off("ReceiveMessage");
    this.connection.off("UserStartedTyping");
    this.connection.off("UserStoppedTyping");
    this.connection.off("ConversationAssigned");

    // Reset the flag
    this.handlersSetUp = false;
    console.log("✅ All conversation event handlers removed");
  }

  async sendMessage(conversationId: string, content: string, messageType: string = "Text"): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke("SendMessage", conversationId, content, messageType);
        return true;
      } catch (error) {
        console.error("Failed to send message:", error);
        return false;
      }
    } else {
      console.error("SignalR is not connected");
      return false;
    }
  }

  async sendTyping(conversationId: string, isTyping: boolean): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        if (isTyping) {
          await this.connection.invoke("StartTyping", conversationId);
        } else {
          await this.connection.invoke("StopTyping", conversationId);
        }
      } catch (error) {
        console.error("Failed to send typing indicator:", error);
      }
    }
  }

  // Debug method to test SignalR event reception
  async testSignalREvents(conversationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      console.error("❌ Cannot test SignalR - connection not established");
      return;
    }

    console.log("🧪 Starting comprehensive SignalR test for conversation:", conversationId);

    try {
      // Test 1: Join conversation (using proper method)
      console.log("🧪 Test 1: Joining conversation...");
      const joined = await this.joinConversation(conversationId);
      console.log("✅ Test 1: JoinConversation successful:", joined);

      // Test 2: Start typing
      console.log("🧪 Test 2: Testing typing notification...");
      await this.connection.invoke("StartTyping", conversationId);
      console.log("✅ Test 2: StartTyping successful");

      // Test 3: Stop typing
      console.log("🧪 Test 3: Testing stop typing notification...");
      await this.connection.invoke("StopTyping", conversationId);
      console.log("✅ Test 3: StopTyping successful");

      console.log("✅ All SignalR tests completed successfully");
    } catch (error) {
      console.error("❌ SignalR test failed:", error);
    }
  }

  async joinConversation(conversationId: string): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        console.log("SignalR: Invoking JoinConversation with ID:", conversationId);
        console.log("SignalR: Connection state:", this.connection.state);
        console.log("SignalR: Connection ID:", this.connection.connectionId);

        await this.connection.invoke("JoinConversation", conversationId);
        this.currentConversationId = conversationId;

        console.log(`SignalR: Successfully joined conversation: ${conversationId}`);
        console.log(`SignalR: Should now receive events for group: conversation_${conversationId}`);
        return true;
      } catch (error) {
        console.error("SignalR: Failed to join conversation:", error);
        return false;
      }
    } else {
      console.warn("SignalR: Cannot join conversation - connection not established", {
        state: this.connection?.state,
        conversationId,
      });
    }
    return false;
  }

  async leaveConversation(conversationId: string): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke("LeaveConversation", conversationId);
        if (this.currentConversationId === conversationId) {
          this.currentConversationId = null;
          // Remove conversation-specific event handlers when leaving
          this.removeConversationEventHandlers();
        }
        console.log(`Left conversation: ${conversationId}`);
        return true;
      } catch (error) {
        console.error("Failed to leave conversation:", error);
        return false;
      }
    }
    return false;
  }

  async requestAgent(): Promise<void> {
    console.log("Agent handoff requested");
  }

  async startConversation(conversationId: string): Promise<boolean> {
    console.log("SignalR: Starting conversation with ID:", conversationId);

    if (!this.connection) {
      console.warn("SignalR: Cannot start conversation - no connection");
      return false;
    }

    if (this.connection?.state !== HubConnectionState.Connected) {
      console.warn("SignalR: Cannot start conversation - connection not ready", {
        state: this.connection?.state,
      });
      return false;
    }

    // Join the conversation
    const joined = await this.joinConversation(conversationId);
    if (joined) {
      // Set up conversation event handlers AFTER joining conversation
      console.log("Setting up conversation event handlers for:", conversationId);
      this.setupConversationEventHandlers();
      console.log(`SignalR: Successfully started conversation: ${conversationId}`);
      return true;
    } else {
      console.error(`SignalR: Failed to start conversation: ${conversationId}`);
      return false;
    }
  }

  async setupExistingConversation(conversationId: string): Promise<boolean> {
    console.log("SignalR: Setting up existing conversation with ID:", conversationId);

    if (!this.connection) {
      console.warn("SignalR: Cannot setup existing conversation - no connection");
      return false;
    }

    if (this.connection?.state !== HubConnectionState.Connected) {
      console.warn("SignalR: Cannot setup existing conversation - connection not ready", {
        state: this.connection?.state,
      });
      return false;
    }

    // Only join if not already in this conversation
    if (this.currentConversationId !== conversationId) {
      const joined = await this.joinConversation(conversationId);
      if (joined) {
        // Set up conversation event handlers AFTER joining conversation
        console.log("Setting up conversation event handlers for existing conversation:", conversationId);
        this.setupConversationEventHandlers();
        console.log(`SignalR: Successfully setup existing conversation: ${conversationId}`);
        return true;
      } else {
        console.error(`SignalR: Failed to setup existing conversation: ${conversationId}`);
        return false;
      }
    } else {
      console.log(`SignalR: Already in conversation: ${conversationId}`);
      return true;
    }
  }

  private scheduleReconnect(tenantId: string, authToken: string, conversationId?: string) {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = 1000 * Math.pow(2, this.reconnectAttempts - 1);

      setTimeout(async () => {
        console.log(`Attempting reconnect ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
        await this.connect(tenantId, authToken, conversationId);
      }, delay);
    } else {
      console.error("Max reconnection attempts reached");
      store.dispatch(setConnectionStatus("error"));
    }
  }

  // Add a method to send message directly via SignalR for testing
  async sendTestMessage(conversationId: string, content: string): Promise<boolean> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      console.error("SignalR: Cannot send test message - not connected");
      return false;
    }

    try {
      console.log("🧪 Sending test message via SignalR:", { conversationId, content });
      await this.connection.invoke("SendMessage", conversationId, content, "Text");
      console.log("✅ Test message sent via SignalR");
      return true;
    } catch (error) {
      console.error("❌ Failed to send test message via SignalR:", error);
      return false;
    }
  }

  // Debugging method to list registered event handlers
  listEventHandlers(): string[] {
    if (!this.connection) {
      return [];
    }

    // Access the private handlers property (for debugging only)
    const handlers = (this.connection as any)._callbacks || (this.connection as any).handlers || {};
    const eventNames = Object.keys(handlers);
    console.log("📋 Currently registered SignalR event handlers:", eventNames);
    return eventNames;
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        if (this.currentConversationId) {
          await this.leaveConversation(this.currentConversationId);
        }
        await this.connection.stop();
        console.log("SignalR disconnected");
      } catch (error) {
        console.error("Error during disconnect:", error);
      } finally {
        this.connection = null;
        this.currentConversationId = null;
        this.handlersSetUp = false;
        console.log("🔄 Reset all connection state");
      }
    }
  }

  isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  getConnectionState(): string {
    return this.connection?.state || "Disconnected";
  }

  // Debug method to test SignalR events
  // Public debug methods
  public getDebugInfo() {
    return {
      connectionState: this.connection?.state,
      currentConversationId: this.currentConversationId,
      handlersSetUp: this.handlersSetUp,
      hubUrl: this.hubUrl,
      reconnectAttempts: this.reconnectAttempts,
    };
  }

  public getConnection() {
    return this.connection;
  }

  async testConnection(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      console.log("SignalR: Testing connection...");
      console.log("SignalR: Current conversation ID:", this.currentConversationId);
      console.log("SignalR: Connection ID:", this.connection.connectionId);

      // Test if we can call a simple hub method
      try {
        if (this.currentConversationId) {
          console.log("SignalR: Testing with current conversation:", this.currentConversationId);
          await this.connection.invoke("JoinConversation", this.currentConversationId);
          console.log("SignalR: Re-joined conversation successfully");
        }
      } catch (error) {
        console.error("SignalR: Test connection failed:", error);
      }
    } else {
      console.log("SignalR: Connection not ready for testing, state:", this.connection?.state);
    }
  }
}

export const signalRService = new SignalRService();

// Expose debug methods globally for testing
(window as any).signalRDebug = {
  testEvents: (conversationId: string) => signalRService.testSignalREvents(conversationId),
  getDebugInfo: () => signalRService.getDebugInfo(),
  logEventHandlers: () => signalRService.listEventHandlers(),
};

export default signalRService;
