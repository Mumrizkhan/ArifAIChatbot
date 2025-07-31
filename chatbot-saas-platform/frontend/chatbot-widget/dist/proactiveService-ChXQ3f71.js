var __defProp = Object.defineProperty;
var __defNormalProp = (obj, key, value) => key in obj ? __defProp(obj, key, { enumerable: true, configurable: true, writable: true, value }) : obj[key] = value;
var __publicField = (obj, key, value) => __defNormalProp(obj, typeof key !== "symbol" ? key + "" : key, value);
import { s as store, a as addMessage } from "./widget-Dy2vHlqS.js";
class ProactiveService {
  constructor() {
    __publicField(this, "triggers", []);
    __publicField(this, "monitoringInterval", null);
    __publicField(this, "lastActivity", Date.now());
  }
  addTrigger(condition, message, delay = 5e3) {
    const id = `trigger_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    this.triggers.push({ condition, message, delay, triggered: false, id });
    return id;
  }
  removeTrigger(id) {
    this.triggers = this.triggers.filter((trigger) => trigger.id !== id);
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
    }, 1e3);
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
  sendProactiveMessage(content) {
    const message = {
      id: `proactive_${Date.now()}`,
      content,
      sender: "bot",
      timestamp: /* @__PURE__ */ new Date(),
      type: "text"
    };
    store.dispatch(addMessage(message));
  }
  setupDefaultTriggers() {
    this.addTrigger(
      () => {
        const state = store.getState();
        const timeSinceActivity = Date.now() - this.lastActivity;
        return state.chat.isOpen && !state.chat.isTyping && timeSinceActivity > 3e4;
      },
      "Is there anything else I can help you with?",
      3e4
    );
    this.addTrigger(
      () => {
        var _a;
        const state = store.getState();
        return state.chat.isOpen && ((_a = state.chat.currentConversation) == null ? void 0 : _a.messages.length) === 0;
      },
      "Welcome! How can I assist you today?",
      2e3
    );
  }
  resetTriggers() {
    this.triggers.forEach((trigger) => {
      trigger.triggered = false;
    });
  }
}
const proactiveService = new ProactiveService();
export {
  proactiveService
};
