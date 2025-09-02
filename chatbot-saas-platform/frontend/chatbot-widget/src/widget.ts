import React from "react";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { store } from "./store/store";
import { ChatWidget } from "./components/ChatWidget";
import { initializeWidget } from "./store/slices/configSlice";
import { applyTenantTheme } from "./store/slices/themeSlice";
import { addMessage } from "./store/slices/chatSlice"; // Add missing import
import { apiClient } from "./services/apiClient";
import envConfig from "./config/environment";
import "./i18n";
import "./styles/widget.css";

interface WidgetConfig {
  tenantId: string;
  apiUrl?: string;
  websocketUrl?: string;
  authToken?: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  customerName?: string;
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
  predefinedIntents?: {
    id: string;
    label: string;
    message: string;
    category: string;
    isActive: boolean;
  }[];
}

class ChatbotWidget {
  private container: HTMLElement | null = null;
  private root: ReactDOM.Root | null = null;
  private isInitialized = false;
  private connection: any = null;
  private config: WidgetConfig | null = null; // Add config property

  async init(widgetConfig: WidgetConfig) {
    if (this.isInitialized) {
      console.warn("Chatbot widget is already initialized");
      return;
    }

    if (!widgetConfig.tenantId) {
      throw new Error("tenantId is required to initialize the chatbot widget");
    }

    // Store config for later use and normalize tenant ID to lowercase
    this.config = {
      ...widgetConfig,
      tenantId: widgetConfig.tenantId.toLowerCase(), // Normalize GUID to lowercase
    };

    // Get default values from environment variables with fallbacks
    const defaultApiUrl = envConfig.apiUrl;
    const defaultWebsocketUrl = envConfig.websocketUrl;

    console.log("🔧 Widget initializing with:", {
      apiUrl: widgetConfig.apiUrl || defaultApiUrl,
      websocketUrl: widgetConfig.websocketUrl || defaultWebsocketUrl,
      tenantId: this.config.tenantId, // Use normalized tenant ID
    });

    apiClient.setTenantId(this.config.tenantId); // Use normalized tenant ID

    this.container = document.createElement("div");
    this.container.id = "chatbot-widget-container";
    document.body.appendChild(this.container);

    if (widgetConfig.customCSS) {
      const style = document.createElement("style");
      style.textContent = widgetConfig.customCSS;
      document.head.appendChild(style);
    }

    store.dispatch(
      initializeWidget({
        tenantId: this.config.tenantId, // Use normalized tenant ID
        config: {
          apiUrl: widgetConfig.apiUrl || defaultApiUrl,
          websocketUrl: widgetConfig.websocketUrl || defaultWebsocketUrl,
          userName: widgetConfig.userName,
          userEmail: widgetConfig.userEmail,
          customerName: widgetConfig.customerName,
          language: widgetConfig.language,
          features: widgetConfig.features,
          behavior: widgetConfig.behavior,
          predefinedIntents: widgetConfig.predefinedIntents,
        },
        userId: widgetConfig.userId,
        metadata: widgetConfig.metadata,
      })
    );

    try {
      const { aiService } = await import("./services/aiService");
      const { fileService } = await import("./services/fileService");
      const { proactiveService } = await import("./services/proactiveService");

      aiService.initialize(widgetConfig.apiUrl || defaultApiUrl);
      fileService.initialize(widgetConfig.apiUrl || defaultApiUrl);

      // Use the provided authToken or fallback to environment variable
      const authToken =
        widgetConfig.authToken ||
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxZDhmOGQ2MS0zNjRhLTQyMWUtYTllYS1jMWUxYTIyMWQ5N2YiLCJlbWFpbCI6InRlbmFudDFAZXhhbXBsZS5jb20iLCJyb2xlIjoiVGVuYW50QWRtaW4iLCJ0ZW5hbnRfaWQiOiI4Mzc4NGVhNi01MzYwLTRmM2MtODQzYS0xYWJkMThkNzJlNWQiLCJuYmYiOjE3NTUwODM1MDYsImV4cCI6MTc1NTE2OTkwNiwiaWF0IjoxNzU1MDgzNTA2LCJpc3MiOiJBcmlmUGxhdGZvcm0iLCJhdWQiOiJBcmlmUGxhdGZvcm0ifQ.BVlKSkEeS9YsVh48VW0rWfl21zi-NZvHdS4u2p1eQsU";

      // Store auth token for later use when conversation is created
      if (authToken) {
        console.log("Auth token stored for SignalR connection after conversation creation");
        // Store the auth token and SignalR setup for when conversation is created
        store.dispatch(
          initializeWidget({
            tenantId: this.config.tenantId,
            config: {
              apiUrl: widgetConfig.apiUrl || defaultApiUrl,
              websocketUrl: widgetConfig.websocketUrl || defaultWebsocketUrl,
              userName: widgetConfig.userName,
              userEmail: widgetConfig.userEmail,
              customerName: widgetConfig.customerName,
              language: widgetConfig.language,
              features: widgetConfig.features,
              behavior: widgetConfig.behavior,
              predefinedIntents: widgetConfig.predefinedIntents,
              authToken: authToken, // Store auth token in config
            },
            userId: widgetConfig.userId,
            metadata: widgetConfig.metadata,
          })
        );
      } else {
        console.warn("⚠️ No authToken provided - SignalR connection will not be established");
        store.dispatch(
          initializeWidget({
            tenantId: this.config.tenantId,
            config: {
              apiUrl: widgetConfig.apiUrl || defaultApiUrl,
              websocketUrl: widgetConfig.websocketUrl || defaultWebsocketUrl,
              userName: widgetConfig.userName,
              userEmail: widgetConfig.userEmail,
              customerName: widgetConfig.customerName,
              language: widgetConfig.language,
              features: widgetConfig.features,
              behavior: widgetConfig.behavior,
              predefinedIntents: widgetConfig.predefinedIntents,
            },
            userId: widgetConfig.userId,
            metadata: widgetConfig.metadata,
          })
        );
      }

      if (widgetConfig.features?.proactiveMessages) {
        proactiveService.setupDefaultTriggers();
        proactiveService.startMonitoring();
      }
    } catch (error) {
      console.error("❌ Service initialization error:", error);
    }

    if (widgetConfig.theme || widgetConfig.branding || widgetConfig.language || widgetConfig.customCSS) {
      store.dispatch(
        applyTenantTheme({
          theme: widgetConfig.theme || {},
          branding: widgetConfig.branding || {},
          language: widgetConfig.language,
          customCSS: widgetConfig.customCSS,
        })
      );
    }

    this.root = ReactDOM.createRoot(this.container);
    this.root.render(
      React.createElement(Provider, {
        store,
        children: React.createElement(ChatWidget),
      })
    );

    this.isInitialized = true;

    // Check if there's an existing conversation and setup SignalR handlers
    if (this.connection) {
      try {
        const currentState = store.getState() as any;
        const currentConversation = currentState.chat?.currentConversation;
        if (currentConversation?.id) {
          console.log("Setting up SignalR for existing conversation:", currentConversation.id);
          await this.connection.setupExistingConversation(currentConversation.id);
        }
      } catch (error) {
        console.warn("Failed to setup existing conversation on SignalR:", error);
      }
    }

    // Analytics tracking
    if (typeof window !== "undefined" && "gtag" in window) {
      const gtag = (window as unknown as { gtag: Function }).gtag;
      gtag("event", "chatbot_widget_initialized", {
        tenant_id: this.config.tenantId,
      });
    }

    console.log("✅ Chatbot widget initialized successfully");
  }

  destroy() {
    try {
      // Disconnect SignalR if connected
      if (this.connection && typeof this.connection.disconnect === "function") {
        this.connection.disconnect();
        this.connection = null;
      }

      // Unmount React component
      if (this.root) {
        this.root.unmount();
        this.root = null;
      }

      // Remove DOM element
      if (this.container && this.container.parentNode) {
        this.container.parentNode.removeChild(this.container);
        this.container = null;
      }

      // Remove custom CSS if added
      const customStyles = document.querySelectorAll("style[data-chatbot-custom]");
      customStyles.forEach((style) => style.remove());

      this.isInitialized = false;
      this.config = null;

      console.log("✅ Chatbot widget destroyed successfully");
    } catch (error) {
      console.error("❌ Error destroying widget:", error);
    }
  }

  isReady(): boolean {
    return this.isInitialized && (!this.connection || this.connection.isConnected?.() || this.connection.getConnectionState?.() === "Connected");
  }

  updateConfig(newConfig: Partial<WidgetConfig>): void {
    if (!this.isInitialized) {
      throw new Error("Widget must be initialized before updating config");
    }

    if (!this.config) {
      throw new Error("No existing config found");
    }

    this.config = { ...this.config, ...newConfig };
    this.applyConfiguration();
  }

  sendProactiveMessage(message: { content: string; delay?: number }): void {
    if (!this.isInitialized) {
      throw new Error("Widget must be initialized before sending proactive messages");
    }

    setTimeout(() => {
      store.dispatch(
        addMessage({
          id: `proactive_${Date.now()}`,
          content: message.content,
          sender: "bot",
          timestamp: new Date().toISOString(), // ✅ Now properly serialized as ISO string
          type: "text",
        })
      );
    }, message.delay || 0);
  }

  // Add method to get current config
  getConfig(): WidgetConfig | null {
    return this.config;
  }

  // Add method to check if widget has specific feature enabled
  hasFeature(feature: keyof NonNullable<WidgetConfig["features"]>): boolean {
    return this.config?.features?.[feature] ?? false;
  }

  private applyConfiguration(): void {
    if (!this.config) return;

    // Apply theme changes
    if (this.config.theme || this.config.branding || this.config.language) {
      store.dispatch(
        applyTenantTheme({
          theme: this.config.theme || {},
          branding: this.config.branding || {},
          language: this.config.language,
          customCSS: this.config.customCSS,
        })
      );
    }

    // Update features
    if (this.config.features) {
      store.dispatch(
        initializeWidget({
          tenantId: this.config.tenantId,
          config: {
            apiUrl: this.config.apiUrl || "/api",
            websocketUrl: this.config.websocketUrl || "/chathub",
            userName: this.config.userName,
            userEmail: this.config.userEmail,
            customerName: this.config.customerName,
            language: this.config.language,
            features: this.config.features,
            behavior: this.config.behavior,
            predefinedIntents: this.config.predefinedIntents,
          },
          userId: this.config.userId,
          metadata: this.config.metadata,
        })
      );
    }
  }

  // private applyTheme(theme: NonNullable<WidgetConfig["theme"]>): void {
  //   if (!this.container) return;

  //   // Apply CSS custom properties for theme
  //   const root = this.container;
  //   if (theme.primaryColor) {
  //     root.style.setProperty("--chatbot-primary-color", theme.primaryColor);
  //   }
  //   if (theme.backgroundColor) {
  //     root.style.setProperty("--chatbot-bg-color", theme.backgroundColor);
  //   }
  //   if (theme.textColor) {
  //     root.style.setProperty("--chatbot-text-color", theme.textColor);
  //   }

  //   console.log("✅ Theme applied:", theme);
  // }

  // private applyLanguage(language: string): void {
  //   // Apply language changes through i18n
  //   import("./i18n")
  //     .then((i18nModule) => {
  //       const i18n = i18nModule.default;
  //       if (i18n && typeof i18n.changeLanguage === "function") {
  //         i18n.changeLanguage(language);
  //         console.log("✅ Language applied:", language);
  //       }
  //     })
  //     .catch((error) => {
  //       console.error("❌ Failed to change language:", error);
  //     });
  // }
}

// Create singleton instance
const chatbotWidget = new ChatbotWidget();

// Global window access for external integration
if (typeof window !== "undefined") {
  (window as any).ChatbotWidget = ChatbotWidget;
  (window as any).chatbotWidget = chatbotWidget;
}

// Auto-initialization from script tag
document.addEventListener("DOMContentLoaded", () => {
  const script = document.querySelector("script[data-chatbot-config]");
  if (script) {
    try {
      const configString = script.getAttribute("data-chatbot-config");
      if (configString) {
        const config = JSON.parse(configString);
        if (config.tenantId) {
          chatbotWidget.init(config);
        } else {
          console.warn("⚠️ No tenantId found in chatbot config");
        }
      }
    } catch (error) {
      console.error("❌ Failed to parse chatbot config from script tag:", error);
    }
  }
});

// Named exports for ES modules
export { ChatbotWidget };
export { chatbotWidget };

// Default export for backwards compatibility added
export default ChatbotWidget;
