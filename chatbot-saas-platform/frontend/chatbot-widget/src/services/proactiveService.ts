import { store } from "../store/store";
import { addMessage } from "../store/slices/chatSlice";

interface ProactiveTrigger {
  condition: () => boolean;
  message: string;
  delay: number;
  triggered: boolean;
  id: string;
  timerId?: ReturnType<typeof setTimeout> | null; // added
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
    this.triggers = this.triggers.filter((trigger) => trigger.id !== id);
  }

  startMonitoring() {
    if (this.monitoringInterval) return;

    this.monitoringInterval = setInterval(() => {
      this.triggers.forEach((trigger) => {
        // Only schedule once
        if (!trigger.triggered && !trigger.timerId && trigger.condition()) {
          // Mark triggered BEFORE scheduling to avoid duplicate timers
          trigger.triggered = true;
          trigger.timerId = setTimeout(() => {
            this.sendProactiveMessage(trigger.message);
            // We keep triggered = true (fire only once)
            trigger.timerId = null;
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
    const state = store.getState();
    const currentConversation = state.chat.currentConversation;

    // Ensure the conversation exists and has no messages
    if (!currentConversation || currentConversation.messages.length > 0) {
      console.log("Proactive message skipped because there are existing messages.");
      return;
    }

    // Add the proactive message
    const message = {
      id: `proactive_${Date.now()}`,
      content,
      sender: "bot" as const,
      timestamp: new Date().toISOString(),
      type: "text" as const,
      pending: false,
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
      "Is there anything else I can help you with?",
      30000
    );

    this.addTrigger(
      () => {
        const state = store.getState();
        const currentConversation = state.chat.currentConversation;

        // Only trigger if the conversation is open and no messages exist
        return state.chat.isOpen && !!currentConversation && currentConversation.messages.length === 0;
      },
      "Welcome! How can I assist you today?",
      0 // Fire immediately when the chat opens
    );
  }

  resetTriggers() {
    this.triggers.forEach((trigger) => {
      if (trigger.timerId) {
        clearTimeout(trigger.timerId);
        trigger.timerId = null;
      }
      trigger.triggered = false;
    });
  }
}

export const proactiveService = new ProactiveService();
