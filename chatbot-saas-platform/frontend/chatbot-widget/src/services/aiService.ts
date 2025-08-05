class AIService {
  private hubUrl: string = import.meta.env.BASE_URL;
  initialize(hubUrl: string) {
    this.hubUrl = this.hubUrl || hubUrl;
  }

  async getBotResponse(message: string, conversationId: string): Promise<string> {
    try {
      const response = await fetch(`${this.hubUrl}/ai/chat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message, conversationId }),
      });
      const data = await response.json();
      return data.response;
    } catch (error) {
      console.error("AI service error:", error);
      return "I apologize, but I'm having trouble responding right now. Please try again.";
    }
  }

  async shouldTransferToAgent(message: string): Promise<boolean> {
    const transferKeywords = ["human", "agent", "person", "help", "support"];
    return transferKeywords.some((keyword) => message.toLowerCase().includes(keyword));
  }

  async analyzeSentiment(message: string): Promise<"positive" | "negative" | "neutral"> {
    try {
      const response = await fetch(`${this.hubUrl}/ai/sentiment`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message }),
      });
      const data = await response.json();
      return data.sentiment;
    } catch (error) {
      console.error("Sentiment analysis error:", error);
      return "neutral";
    }
  }

  async extractIntents(message: string): Promise<string[]> {
    try {
      const response = await fetch(`${this.hubUrl}/ai/intents`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message }),
      });
      const data = await response.json();
      return data.intents || [];
    } catch (error) {
      console.error("Intent extraction error:", error);
      return [];
    }
  }
}

export const aiService = new AIService();
