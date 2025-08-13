// Environment configuration helper
export const config = {
  apiUrl: import.meta.env.VITE_API_URL || "https://api-stg.arif.sa",
  websocketUrl: import.meta.env.VITE_WEBSOCKET_URL || "wss://api-stg.arif.sa/chat/chatHub",
  apiEndpoint: import.meta.env.VITE_CHATBOT_API_ENDPOINT || "/chatbot",
  jwtSecret: import.meta.env.VITE_JWT_SECRET_KEY || "",
  apiKey: import.meta.env.VITE_API_KEY || "",
  
  // Chatbot configuration
  chatbotName: import.meta.env.VITE_CHATBOT_NAME || "Arif AI Chatbot",
  defaultAvatarUrl: import.meta.env.VITE_DEFAULT_AVATAR_URL || "/assets/default-avatar.png",
  theme: import.meta.env.VITE_CHATBOT_THEME || "light",
  typingDelay: parseInt(import.meta.env.VITE_TYPING_DELAY || "1000"),
  maxMessageLength: parseInt(import.meta.env.VITE_MAX_MESSAGE_LENGTH || "2000"),
  
  // Feature flags
  enableFileUpload: import.meta.env.VITE_ENABLE_FILE_UPLOAD === "true",
  enableVoiceInput: import.meta.env.VITE_ENABLE_VOICE_INPUT === "true",
  enableEmojiPicker: import.meta.env.VITE_ENABLE_EMOJI_PICKER === "true",
  enableTypingIndicator: import.meta.env.VITE_ENABLE_TYPING_INDICATOR === "true",
  
  // Development tools
  debugMode: import.meta.env.VITE_DEBUG_MODE === "true",
  logLevel: import.meta.env.VITE_LOG_LEVEL || "info",
  enableReduxDevtools: import.meta.env.VITE_ENABLE_REDUX_DEVTOOLS === "true",
  
  // Widget configuration
  widgetPosition: import.meta.env.VITE_WIDGET_POSITION || "bottom-right",
  widgetThemeColor: import.meta.env.VITE_WIDGET_THEME_COLOR || "#007bff",
  widgetWidth: parseInt(import.meta.env.VITE_WIDGET_WIDTH || "400"),
  widgetHeight: parseInt(import.meta.env.VITE_WIDGET_HEIGHT || "600"),
  
  // Analytics & monitoring
  enableAnalytics: import.meta.env.VITE_ENABLE_ANALYTICS === "true",
  analyticsId: import.meta.env.VITE_ANALYTICS_ID || "",
  errorReporting: import.meta.env.VITE_ERROR_REPORTING === "true",
  
  // Rate limiting
  messageRateLimit: parseInt(import.meta.env.VITE_MESSAGE_RATE_LIMIT || "10"),
  rateLimitWindow: parseInt(import.meta.env.VITE_RATE_LIMIT_WINDOW || "60000"),
};

// Log configuration in development
if (config.debugMode) {
  console.log("🔧 Environment Configuration:", {
    apiUrl: config.apiUrl,
    websocketUrl: config.websocketUrl,
    debugMode: config.debugMode,
    enabledFeatures: {
      fileUpload: config.enableFileUpload,
      voiceInput: config.enableVoiceInput,
      emojiPicker: config.enableEmojiPicker,
      typingIndicator: config.enableTypingIndicator,
    }
  });
}

export default config;
