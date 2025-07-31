var __defProp = Object.defineProperty;
var __defNormalProp = (obj, key, value) => key in obj ? __defProp(obj, key, { enumerable: true, configurable: true, writable: true, value }) : obj[key] = value;
var __publicField = (obj, key, value) => __defNormalProp(obj, typeof key !== "symbol" ? key + "" : key, value);
var __async = (__this, __arguments, generator) => {
  return new Promise((resolve, reject) => {
    var fulfilled = (value) => {
      try {
        step(generator.next(value));
      } catch (e) {
        reject(e);
      }
    };
    var rejected = (value) => {
      try {
        step(generator.throw(value));
      } catch (e) {
        reject(e);
      }
    };
    var step = (x) => x.done ? resolve(x.value) : Promise.resolve(x.value).then(fulfilled, rejected);
    step((generator = generator.apply(__this, __arguments)).next());
  });
};
class AIService {
  constructor() {
    __publicField(this, "apiUrl", "");
  }
  initialize(apiUrl) {
    this.apiUrl = apiUrl;
  }
  getBotResponse(message, conversationId) {
    return __async(this, null, function* () {
      try {
        const response = yield fetch(`${this.apiUrl}/ai/chat`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ message, conversationId })
        });
        const data = yield response.json();
        return data.response;
      } catch (error) {
        console.error("AI service error:", error);
        return "I apologize, but I'm having trouble responding right now. Please try again.";
      }
    });
  }
  shouldTransferToAgent(message) {
    return __async(this, null, function* () {
      const transferKeywords = ["human", "agent", "person", "help", "support"];
      return transferKeywords.some(
        (keyword) => message.toLowerCase().includes(keyword)
      );
    });
  }
  analyzeSentiment(message) {
    return __async(this, null, function* () {
      try {
        const response = yield fetch(`${this.apiUrl}/ai/sentiment`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ message })
        });
        const data = yield response.json();
        return data.sentiment;
      } catch (error) {
        console.error("Sentiment analysis error:", error);
        return "neutral";
      }
    });
  }
  extractIntents(message) {
    return __async(this, null, function* () {
      try {
        const response = yield fetch(`${this.apiUrl}/ai/intents`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ message })
        });
        const data = yield response.json();
        return data.intents || [];
      } catch (error) {
        console.error("Intent extraction error:", error);
        return [];
      }
    });
  }
}
const aiService = new AIService();
export {
  aiService
};
