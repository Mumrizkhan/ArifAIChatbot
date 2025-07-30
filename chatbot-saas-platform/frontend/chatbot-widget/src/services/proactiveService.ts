import { store } from '../store/store';
import { addMessage } from '../store/slices/chatSlice';

interface ProactiveTrigger {
  condition: () => boolean;
  message: string;
  delay: number;
  triggered: boolean;
  id: string;
}

class ProactiveService {
  private triggers: ProactiveTrigger[] = [];
  private monitoringInterval: NodeJS.Timeout | null = null;
  private lastActivity: number = Date.now();

  addTrigger(condition: () => boolean, message: string, delay: number = 5000): string {
    const id = `trigger_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    this.triggers.push({ condition, message, delay, triggered: false, id });
    return id;
  }

  removeTrigger(id: string) {
    this.triggers = this.triggers.filter(trigger => trigger.id !== id);
  }

  startMonitoring() {
    if (this.monitoringInterval) return;

    this.monitoringInterval = setInterval(() => {
      this.triggers.forEach((trigger) => {
        if (!trigger.triggered && trigger.condition()) {
          setTimeout(() => {
            this.sendProactiveMessage(trigger.message);
            trigger.triggered = true;
          }, trigger.delay);
        }
      });
    }, 1000);
  }

  stopMonitoring() {
    if (this.monitoringInterval) {
      clearInterval(this.monitoringInterval);
      this.monitoringInterval = null;
    }
  }

  updateActivity() {
    this.lastActivity = Date.now();
  }

  private sendProactiveMessage(content: string) {
    const message = {
      id: `proactive_${Date.now()}`,
      content,
      sender: 'bot' as const,
      timestamp: new Date(),
      type: 'text' as const,
    };
    
    store.dispatch(addMessage(message));
  }

  setupDefaultTriggers() {
    this.addTrigger(
      () => {
        const state = store.getState();
        const timeSinceActivity = Date.now() - this.lastActivity;
        return state.chat.isOpen && !state.chat.isTyping && timeSinceActivity > 30000;
      },
      'Is there anything else I can help you with?',
      30000
    );

    this.addTrigger(
      () => {
        const state = store.getState();
        return state.chat.isOpen && state.chat.currentConversation?.messages.length === 0;
      },
      'Welcome! How can I assist you today?',
      2000
    );
  }

  resetTriggers() {
    this.triggers.forEach(trigger => {
      trigger.triggered = false;
    });
  }
}

export const proactiveService = new ProactiveService();
