import { store } from '../store/store';
import { addMessage, setTyping, setConnectionStatus, assignAgent, updateConversationStatus } from '../store/slices/chatSlice';

class WebSocketService {
  private ws: WebSocket | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000;
  private heartbeatInterval: number | null = null;

  connect(tenantId: string, conversationId?: string) {
    const state = store.getState();
    const { websocketUrl } = state.config.widget;
    const { sessionId, userId } = state.config;

    const wsUrl = new URL(websocketUrl, window.location.origin);
    wsUrl.searchParams.set('tenantId', tenantId);
    wsUrl.searchParams.set('sessionId', sessionId);
    if (conversationId) {
      wsUrl.searchParams.set('conversationId', conversationId);
    }
    if (userId) {
      wsUrl.searchParams.set('userId', userId);
    }

    try {
      store.dispatch(setConnectionStatus('connecting'));
      this.ws = new WebSocket(wsUrl.toString());
      this.setupEventListeners();
    } catch (error) {
      console.error('WebSocket connection failed:', error);
      store.dispatch(setConnectionStatus('error'));
      this.scheduleReconnect();
    }
  }

  private setupEventListeners() {
    if (!this.ws) return;

    this.ws.onopen = () => {
      console.log('WebSocket connected');
      store.dispatch(setConnectionStatus('connected'));
      this.reconnectAttempts = 0;
      this.startHeartbeat();
    };

    this.ws.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        this.handleMessage(data);
      } catch (error) {
        console.error('Failed to parse WebSocket message:', error);
      }
    };

    this.ws.onclose = (event) => {
      console.log('WebSocket disconnected:', event.code, event.reason);
      store.dispatch(setConnectionStatus('disconnected'));
      this.stopHeartbeat();
      
      if (event.code !== 1000) { // Not a normal closure
        this.scheduleReconnect();
      }
    };

    this.ws.onerror = (error) => {
      console.error('WebSocket error:', error);
      store.dispatch(setConnectionStatus('error'));
    };
  }

  private handleMessage(data: any) {
    switch (data.type) {
      case 'message':
        store.dispatch(addMessage({
          id: data.id,
          content: data.content,
          sender: data.sender,
          timestamp: new Date(data.timestamp),
          type: data.messageType || 'text',
          metadata: data.metadata,
        }));
        break;

      case 'typing':
        store.dispatch(setTyping({
          isTyping: data.isTyping,
          user: data.user,
        }));
        break;

      case 'agent_assigned':
        store.dispatch(assignAgent({
          id: data.agent.id,
          name: data.agent.name,
          avatar: data.agent.avatar,
        }));
        break;

      case 'conversation_status':
        store.dispatch(updateConversationStatus(data.status));
        break;

      case 'error':
        console.error('WebSocket error message:', data.message);
        break;

      case 'pong':
        break;

      default:
        console.log('Unknown message type:', data.type);
    }
  }

  sendMessage(content: string, type: 'text' | 'file' = 'text', metadata?: any) {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      const message = {
        type: 'message',
        content,
        messageType: type,
        metadata,
        timestamp: new Date().toISOString(),
      };
      this.ws.send(JSON.stringify(message));
    } else {
      console.error('WebSocket is not connected');
    }
  }

  sendTyping(isTyping: boolean) {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({
        type: 'typing',
        isTyping,
      }));
    }
  }

  requestAgent() {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({
        type: 'request_agent',
      }));
    }
  }

  private startHeartbeat() {
    this.heartbeatInterval = window.setInterval(() => {
      if (this.ws && this.ws.readyState === WebSocket.OPEN) {
        this.ws.send(JSON.stringify({ type: 'ping' }));
      }
    }, 30000); // Send ping every 30 seconds
  }

  private stopHeartbeat() {
    if (this.heartbeatInterval) {
      window.clearInterval(this.heartbeatInterval);
      this.heartbeatInterval = null;
    }
  }

  private scheduleReconnect() {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
      
      setTimeout(() => {
        const state = store.getState();
        const { tenantId } = state.config.widget;
        const conversationId = state.chat.currentConversation?.id;
        this.connect(tenantId, conversationId);
      }, delay);
    }
  }

  disconnect() {
    this.stopHeartbeat();
    if (this.ws) {
      this.ws.close(1000, 'User disconnected');
      this.ws = null;
    }
  }

  isConnected(): boolean {
    return this.ws?.readyState === WebSocket.OPEN;
  }
}

export const websocketService = new WebSocketService();
