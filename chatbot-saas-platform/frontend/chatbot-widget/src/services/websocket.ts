import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { store } from "../store/store";
import { addMessage, setTyping, setConnectionStatus, assignAgent, updateConversationStatus } from "../store/slices/chatSlice";

class SignalRService {
  private connection: HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private currentConversationId: string | null = null;
  private hubUrl: string = import.meta.env.VITE_WEBSOCKET_URL || "/chatHub"; // Add this line
  async connect(tenantId: string, authToken: string, conversationId?: string): Promise<boolean> {
    try {
      store.dispatch(setConnectionStatus("connecting"));

      this.connection = new HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          accessTokenFactory: () => authToken,
          skipNegotiation: false,
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

      this.setupEventHandlers();

      await this.connection.start();
      console.log("SignalR connected");
      store.dispatch(setConnectionStatus("connected"));
      this.reconnectAttempts = 0;

      if (conversationId) {
        await this.joinConversation(conversationId);
        this.currentConversationId = conversationId;
      }

      return true;
    } catch (error) {
      console.error("SignalR connection failed:", error);
      store.dispatch(setConnectionStatus("error"));
      this.scheduleReconnect(tenantId, authToken, conversationId);
      return false;
    }
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    this.connection.onreconnecting(() => {
      console.log("SignalR reconnecting...");
      store.dispatch(setConnectionStatus("connecting"));
    });

    this.connection.onreconnected(async () => {
      console.log("SignalR reconnected");
      store.dispatch(setConnectionStatus("connected"));

      if (this.currentConversationId) {
        await this.joinConversation(this.currentConversationId);
      }
    });

    this.connection.onclose(() => {
      console.log("SignalR connection closed");
      store.dispatch(setConnectionStatus("disconnected"));
    });

    this.connection.on("ReceiveMessage", (messageDto) => {
      store.dispatch(
        addMessage({
          id: messageDto.Id,
          content: messageDto.Content,
          sender: messageDto.Sender.toLowerCase(),
          // timestamp: new Date(messageDto.CreatedAt),
          timestamp: new Date(messageDto.CreatedAt), // Returns a Date object
          type: messageDto.Type.toLowerCase(),
          metadata: {
            senderId: messageDto.SenderId?.toString(),
            senderName: messageDto.SenderName,
          },
        })
      );
    });

    this.connection.on("UserStartedTyping", (typingInfo) => {
      store.dispatch(
        setTyping({
          isTyping: true,
          user: typingInfo.UserName || "User",
        })
      );
    });

    this.connection.on("UserStoppedTyping", (typingInfo) => {
      store.dispatch(
        setTyping({
          isTyping: false,
          user: typingInfo.UserName || "User",
        })
      );
    });

    this.connection.on("AgentAssigned", (agentInfo) => {
      store.dispatch(
        assignAgent({
          id: agentInfo.id,
          name: agentInfo.name,
          avatar: agentInfo.avatar,
        })
      );
    });

    this.connection.on("ConversationAssigned", (assignmentInfo) => {
      console.log("Conversation assigned to agent:", assignmentInfo);
      
      // Handle different possible payload structures from the backend
      const agentId = assignmentInfo.AgentId || assignmentInfo.agentId || assignmentInfo.Id || assignmentInfo.id;
      const agentName = assignmentInfo.AgentName || assignmentInfo.agentName || 
                       assignmentInfo.Name || assignmentInfo.name || 
                       assignmentInfo.CustomerName || "Agent";
      const agentAvatar = assignmentInfo.AgentAvatar || assignmentInfo.agentAvatar || 
                         assignmentInfo.Avatar || assignmentInfo.avatar;
      
      if (agentId) {
        store.dispatch(
          assignAgent({
            id: agentId.toString(),
            name: agentName,
            avatar: agentAvatar,
          })
        );
        
        // Update conversation status to indicate agent is now handling the conversation
        store.dispatch(updateConversationStatus("active"));
        
        // Optionally add a system message to indicate agent assignment
        store.dispatch(
          addMessage({
            id: `system-agent-assigned-${Date.now()}`,
            content: `${agentName} has joined the conversation`,
            sender: "bot",
            type: "text",
            timestamp: new Date(),
            metadata: {
              systemMessage: true,
              agentId: agentId.toString(),
              agentName: agentName,
            },
          })
        );
      } else {
        console.warn("ConversationAssigned: No valid agent ID found in payload:", assignmentInfo);
      }
    });

    this.connection.on("ConversationStatusChanged", (status) => {
      console.log("Conversation status changed:", status);
      store.dispatch(updateConversationStatus(status));
    });

    // Additional handlers for conversation events
    this.connection.on("AgentJoined", (agentInfo) => {
      console.log("Agent joined conversation:", agentInfo);
      const agentId = agentInfo.id || agentInfo.Id || agentInfo.agentId || agentInfo.AgentId;
      const agentName = agentInfo.name || agentInfo.Name || agentInfo.agentName || agentInfo.AgentName || "Agent";
      const agentAvatar = agentInfo.avatar || agentInfo.Avatar || agentInfo.agentAvatar || agentInfo.AgentAvatar;
      
      if (agentId) {
        store.dispatch(
          assignAgent({
            id: agentId.toString(),
            name: agentName,
            avatar: agentAvatar,
          })
        );
        
        store.dispatch(
          addMessage({
            id: `system-agent-joined-${Date.now()}`,
            content: `${agentName} has joined the conversation`,
            sender: "bot",
            type: "text",
            timestamp: new Date(),
            metadata: {
              systemMessage: true,
              agentId: agentId.toString(),
              agentName: agentName,
            },
          })
        );
      }
    });

    this.connection.on("AgentLeft", (agentInfo) => {
      console.log("Agent left conversation:", agentInfo);
      const agentName = agentInfo.name || agentInfo.Name || agentInfo.agentName || agentInfo.AgentName || "Agent";
      
      store.dispatch(
        addMessage({
          id: `system-agent-left-${Date.now()}`,
          content: `${agentName} has left the conversation`,
          sender: "bot",
          type: "text",
          timestamp: new Date(),
          metadata: {
            systemMessage: true,
          },
        })
      );
    });

    this.connection.on("ConversationEnded", (endInfo) => {
      console.log("Conversation ended:", endInfo);
      store.dispatch(updateConversationStatus("ended"));
      
      store.dispatch(
        addMessage({
          id: `system-conversation-ended-${Date.now()}`,
          content: "This conversation has ended",
          sender: "bot",
          type: "text",
          timestamp: new Date(),
          metadata: {
            systemMessage: true,
          },
        })
      );
    });
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

  async joinConversation(conversationId: string): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke("JoinConversation", conversationId);
        this.currentConversationId = conversationId;
        console.log(`Joined conversation: ${conversationId}`);
        return true;
      } catch (error) {
        console.error("Failed to join conversation:", error);
        return false;
      }
    }
    return false;
  }

  async leaveConversation(conversationId: string): Promise<boolean> {
    if (this.connection?.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke("LeaveConversation", conversationId);
        if (this.currentConversationId === conversationId) {
          this.currentConversationId = null;
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
      }
    }
  }

  isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  getConnectionState(): string {
    return this.connection?.state || "Disconnected";
  }
}

export const signalRService = new SignalRService();
