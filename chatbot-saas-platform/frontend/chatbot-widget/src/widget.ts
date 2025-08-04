import React from "react";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { store } from "./store/store";
import { ChatWidget } from "./components/ChatWidget";
import { initializeWidget } from "./store/slices/configSlice";
import { applyTenantTheme } from "./store/slices/themeSlice";
import "./i18n";
import "./styles/widget.css";
 
interface WidgetConfig {
  tenantId: string;
  apiUrl?: string;
  websocketUrl?: string;
  authToken?: string;
  userId?: string;
  metadata?: Record<string, any>;
  theme?: {
    primaryColor?: string;
    secondaryColor?: string;
    backgroundColor?: string;
    textColor?: string;
    borderRadius?: string;
    fontFamily?: string;
    fontSize?: string;
    headerColor?: string;
    headerTextColor?: string;
    userMessageColor?: string;
    botMessageColor?: string;
    shadowColor?: string;
    position?: "bottom-right" | "bottom-left" | "top-right" | "top-left";
    size?: "small" | "medium" | "large";
    animation?: "slide" | "fade" | "bounce" | "none";
  };
  branding?: {
    logo?: string;
    companyName?: string;
    welcomeMessage?: string;
    placeholderText?: string;
  };
  language?: "en" | "ar";
  customCSS?: string;
  features?: {
    fileUpload?: boolean;
    voiceMessages?: boolean;
    typing?: boolean;
    readReceipts?: boolean;
    agentHandoff?: boolean;
    conversationRating?: boolean;
    conversationTranscript?: boolean;
    proactiveMessages?: boolean;
  };
  behavior?: {
    autoOpen?: boolean;
    autoOpenDelay?: number;
    showWelcomeMessage?: boolean;
    persistConversation?: boolean;
    maxFileSize?: number;
    allowedFileTypes?: string[];
    maxMessageLength?: number;
  };
}
 
class ChatbotWidget {
  private container: HTMLElement | null = null;
  private root: ReactDOM.Root | null = null;
  private isInitialized = false;
 
  async init(config: WidgetConfig) {
    if (this.isInitialized) {
      console.warn("Chatbot widget is already initialized");
      return;
    }
 
    if (!config.tenantId) {
      throw new Error("tenantId is required to initialize the chatbot widget");
    }
 
    this.container = document.createElement("div");
    this.container.id = "chatbot-widget-container";
    document.body.appendChild(this.container);
 
    if (config.customCSS) {
      const style = document.createElement("style");
      style.textContent = config.customCSS;
      document.head.appendChild(style);
    }
 
    store.dispatch(
      initializeWidget({
        tenantId: config.tenantId,
        config: {
          apiUrl: config.apiUrl || "/api",
          websocketUrl: config.websocketUrl || "/chatHub",
          features: config.features,
          behavior: config.behavior,
        },
        userId: config.userId,
        metadata: config.metadata,
      })
    );
 
    const { aiService } = await import("./services/aiService");
    const { fileService } = await import("./services/fileService");
    const { proactiveService } = await import("./services/proactiveService");
    const { signalRService } = await import("./services/websocket");
 
    aiService.initialize(config.apiUrl || "/api");
    fileService.initialize(config.apiUrl || "/api");
    const authToken = config.authToken || "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxZDhmOGQ2MS0zNjRhLTQyMWUtYTllYS1jMWUxYTIyMWQ5N2YiLCJlbWFpbCI6InRlbmFudDFAZXhhbXBsZS5jb20iLCJyb2xlIjoiVGVuYW50QWRtaW4iLCJ0ZW5hbnRfaWQiOiI4Mzc4NGVhNi01MzYwLTRmM2MtODQzYS0xYWJkMThkNzJlNWQiLCJuYmYiOjE3NTQyOTY3NjIsImV4cCI6MTc1NDM4MzE2MiwiaWF0IjoxNzU0Mjk2NzYyLCJpc3MiOiJBcmlmUGxhdGZvcm0iLCJhdWQiOiJBcmlmUGxhdGZvcm0ifQ.qjEItyJFbTBBa2bcD-1y_ztNqQmNhP9pNV6qQFqiPHU";
    // if (config.authToken) {
    try {
      const connected = await signalRService.connect(config.tenantId, authToken);
      if (!connected) {
        console.warn("Failed to establish SignalR connection");
      }
    } catch (error) {
      console.error("SignalR connection error:", error);
    }
    // } else {
    //   console.warn("No authToken provided - SignalR connection will not be established");
    // }
 
    if (config.features?.proactiveMessages) {
      proactiveService.setupDefaultTriggers();
      proactiveService.startMonitoring();
    }
 
    if (config.theme || config.branding || config.language || config.customCSS) {
      store.dispatch(
        applyTenantTheme({
          theme: config.theme || {},
          branding: config.branding || {},
          language: config.language,
          customCSS: config.customCSS,
        })
      );
    }
 
    this.root = ReactDOM.createRoot(this.container);
    this.root.render(React.createElement(Provider, { store, children: React.createElement(ChatWidget) }));
 
    this.isInitialized = true;
 
    if (typeof window !== "undefined" && (window as any).gtag) {
      (window as any).gtag("event", "chatbot_widget_initialized", {
        tenant_id: config.tenantId,
      });
    }
  }
 
  destroy() {
    if (this.root) {
      this.root.unmount();
      this.root = null;
    }
 
    if (this.container && this.container.parentNode) {
      this.container.parentNode.removeChild(this.container);
      this.container = null;
    }
 
    this.isInitialized = false;
  }
 
  updateConfig(config: Partial<WidgetConfig>) {
    if (!this.isInitialized) {
      console.warn("Widget must be initialized before updating config");
      return;
    }
 
    if (config.theme || config.branding || config.language || config.customCSS) {
      store.dispatch(
        applyTenantTheme({
          theme: config.theme || {},
          branding: config.branding || {},
          language: config.language,
          customCSS: config.customCSS,
        })
      );
    }
  }
 
  isReady() {
    return this.isInitialized;
  }
}
 
const chatbotWidget = new ChatbotWidget();
 
if (typeof window !== "undefined") {
  (window as any).ChatbotWidget = ChatbotWidget;
  (window as any).chatbotWidget = chatbotWidget;
}
 
document.addEventListener("DOMContentLoaded", () => {
  const script = document.querySelector("script[data-chatbot-config]");
  if (script) {
    try {
      const config = JSON.parse(script.getAttribute("data-chatbot-config") || "{}");
      if (config.tenantId) {
        chatbotWidget.init(config);
      }
    } catch (error) {
      console.error("Failed to parse chatbot config from script tag:", error);
    }
  }
});
 
export { ChatbotWidget };
export default chatbotWidget;
 
 