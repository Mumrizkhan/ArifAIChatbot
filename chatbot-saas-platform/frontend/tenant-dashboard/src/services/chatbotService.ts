import { apiClient, ApiResponse } from './api';

// Chatbot related types
export interface Chatbot {
  id: string;
  tenantId: string;
  name: string;
  description: string;
  avatar?: string;
  isActive: boolean;
  settings: ChatbotSettings;
  knowledgeBase: KnowledgeBaseItem[];
  analytics: ChatbotAnalytics;
  createdAt: string;
  updatedAt: string;
}

export interface ChatbotSettings {
  welcomeMessage: string;
  fallbackMessage: string;
  language: string;
  tone: 'formal' | 'casual' | 'friendly' | 'professional';
  responseDelay: number;
  maxTokens: number;
  temperature: number;
  enableTypingIndicator: boolean;
  enableSoundNotifications: boolean;
  workingHours: {
    enabled: boolean;
    timezone: string;
    schedule: WorkingHoursSchedule[];
  };
  integrations: {
    slack: boolean;
    discord: boolean;
    whatsapp: boolean;
    telegram: boolean;
  };
}

export interface WorkingHoursSchedule {
  day: string;
  enabled: boolean;
  startTime: string;
  endTime: string;
}

export interface KnowledgeBaseItem {
  id: string;
  type: 'faq' | 'document' | 'url' | 'text';
  title: string;
  content: string;
  category: string;
  tags: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChatbotAnalytics {
  totalConversations: number;
  totalMessages: number;
  averageResponseTime: number;
  satisfactionScore: number;
  topQuestions: Array<{ question: string; count: number }>;
  conversationsByDay: Array<{ date: string; count: number }>;
}

export interface CreateChatbotData {
  name: string;
  description: string;
  settings?: Partial<ChatbotSettings>;
}

export interface UpdateChatbotData {
  name?: string;
  description?: string;
  avatar?: string;
  isActive?: boolean;
  settings?: Partial<ChatbotSettings>;
}

export interface CreateKnowledgeBaseData {
  type: 'faq' | 'document' | 'url' | 'text';
  title: string;
  content: string;
  category: string;
  tags: string[];
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  sender: 'user' | 'bot';
  message: string;
  timestamp: string;
  metadata?: Record<string, any>;
}

export interface Conversation {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  status: 'active' | 'resolved' | 'closed';
  rating?: number;
  feedback?: string;
  messages: ChatMessage[];
  startedAt: string;
  endedAt?: string;
}

export class ChatbotService {
  // Get chatbot configuration
  static async getChatbot(): Promise<ApiResponse<Chatbot>> {
    return apiClient.get<Chatbot>('/chatbot');
  }

  // Create chatbot
  static async createChatbot(data: CreateChatbotData): Promise<ApiResponse<Chatbot>> {
    return apiClient.post<Chatbot>('/chatbot', data);
  }

  // Update chatbot
  static async updateChatbot(data: UpdateChatbotData): Promise<ApiResponse<Chatbot>> {
    return apiClient.put<Chatbot>('/chatbot', data);
  }

  // Delete chatbot
  static async deleteChatbot(): Promise<ApiResponse<void>> {
    return apiClient.delete<void>('/chatbot');
  }

  // Upload chatbot avatar
  static async uploadAvatar(file: File): Promise<ApiResponse<{ avatar: string }>> {
    const formData = new FormData();
    formData.append('avatar', file);
    return apiClient.upload<{ avatar: string }>('/chatbot/avatar', formData);
  }

  // Knowledge Base Operations
  static async getKnowledgeBase(params?: {
    category?: string;
    type?: string;
    search?: string;
    page?: number;
    limit?: number;
  }): Promise<ApiResponse<{ items: KnowledgeBaseItem[]; total: number }>> {
    return apiClient.get('/chatbot/knowledge-base', params);
  }

  static async createKnowledgeBaseItem(data: CreateKnowledgeBaseData): Promise<ApiResponse<KnowledgeBaseItem>> {
    return apiClient.post<KnowledgeBaseItem>('/chatbot/knowledge-base', data);
  }

  static async updateKnowledgeBaseItem(id: string, data: Partial<CreateKnowledgeBaseData>): Promise<ApiResponse<KnowledgeBaseItem>> {
    return apiClient.put<KnowledgeBaseItem>(`/chatbot/knowledge-base/${id}`, data);
  }

  static async deleteKnowledgeBaseItem(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/chatbot/knowledge-base/${id}`);
  }

  // Bulk operations for knowledge base
  static async bulkImportKnowledgeBase(file: File): Promise<ApiResponse<{ imported: number; failed: number }>> {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient.upload<{ imported: number; failed: number }>('/chatbot/knowledge-base/import', formData);
  }

  static async exportKnowledgeBase(format: 'csv' | 'json' = 'csv'): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.get<{ downloadUrl: string }>('/chatbot/knowledge-base/export', { format });
  }

  // Conversations
  static async getConversations(params?: {
    status?: string;
    userId?: string;
    page?: number;
    limit?: number;
    dateFrom?: string;
    dateTo?: string;
  }): Promise<ApiResponse<{ conversations: Conversation[]; total: number }>> {
    return apiClient.get('/chatbot/conversations', params);
  }

  static async getConversation(id: string): Promise<ApiResponse<Conversation>> {
    return apiClient.get<Conversation>(`/chatbot/conversations/${id}`);
  }

  static async updateConversationStatus(id: string, status: 'active' | 'resolved' | 'closed'): Promise<ApiResponse<Conversation>> {
    return apiClient.put<Conversation>(`/chatbot/conversations/${id}/status`, { status });
  }

  static async rateConversation(id: string, rating: number, feedback?: string): Promise<ApiResponse<void>> {
    return apiClient.post<void>(`/chatbot/conversations/${id}/rate`, { rating, feedback });
  }

  // Analytics
  static async getAnalytics(params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<ChatbotAnalytics>> {
    return apiClient.get('/chatbot/analytics', params);
  }

  // Training
  static async trainChatbot(): Promise<ApiResponse<{ jobId: string }>> {
    return apiClient.post<{ jobId: string }>('/chatbot/train');
  }

  static async getTrainingStatus(jobId: string): Promise<ApiResponse<{ status: string; progress: number }>> {
    return apiClient.get<{ status: string; progress: number }>(`/chatbot/train/${jobId}/status`);
  }

  // Test chatbot
  static async testChatbot(message: string): Promise<ApiResponse<{ response: string; confidence: number }>> {
    return apiClient.post<{ response: string; confidence: number }>('/chatbot/test', { message });
  }
}
