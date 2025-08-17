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
  metadata?: Record<string, unknown>;
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
    return apiClient.get<Chatbot>('/tenant-management/chatbotconfigs');
  }

  // Create chatbot
  static async createChatbot(data: CreateChatbotData): Promise<ApiResponse<Chatbot>> {
    return apiClient.post<Chatbot>('/tenant-management/chatbotconfigs', data);
  }

  // Update chatbot
  static async updateChatbot(data: UpdateChatbotData): Promise<ApiResponse<Chatbot>> {
    return apiClient.put<Chatbot>('/tenant-management/chatbotconfigs', data);
  }

  // Delete chatbot
  static async deleteChatbot(): Promise<ApiResponse<void>> {
    return apiClient.delete<void>('/tenant-management/chatbotconfigs');
  }

  // Upload chatbot avatar
  static async uploadAvatar(file: File): Promise<ApiResponse<{ avatar: string }>> {
    const formData = new FormData();
    formData.append('avatar', file);
    return apiClient.upload<{ avatar: string }>('/tenant-management/chatbotconfigs/avatar', formData);
  }

  // Knowledge Base Operations
  static async getKnowledgeBase(configId: string, params?: {
    category?: string;
    type?: string;
    search?: string;
    page?: number;
    limit?: number;
  }): Promise<ApiResponse<{ items: KnowledgeBaseItem[]; total: number }>> {
    return apiClient.get(`/tenant-management/chatbotconfigs/${configId}/knowledge-base`, params);
  }

  static async createKnowledgeBaseItem(configId: string, data: CreateKnowledgeBaseData): Promise<ApiResponse<KnowledgeBaseItem>> {
    return apiClient.post<KnowledgeBaseItem>(`/tenant-management/chatbotconfigs/${configId}/knowledge-base`, data);
  }

  static async updateKnowledgeBaseItem(configId: string, id: string, data: Partial<CreateKnowledgeBaseData>): Promise<ApiResponse<KnowledgeBaseItem>> {
    return apiClient.put<KnowledgeBaseItem>(`/tenant-management/chatbotconfigs/${configId}/knowledge-base/${id}`, data);
  }

  static async deleteKnowledgeBaseItem(configId: string, id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/tenant-management/chatbotconfigs/${configId}/knowledge-base/documents/${id}`);
  }

  // Bulk operations for knowledge base
  static async bulkImportKnowledgeBase(configId: string, file: File): Promise<ApiResponse<{ imported: number; failed: number }>> {
    const formData = new FormData();
    formData.append('configId', configId);
    formData.append('file', file);
    return apiClient.upload<{ imported: number; failed: number }>(`/tenant-management/chatbotconfigs/${configId}/knowledge-base/import`, formData);
  }

  // Upload knowledge base document
  static async uploadKnowledgeBaseDocument(configId: string, file: File): Promise<ApiResponse<{ message: string }>> {
    const formData = new FormData();
    formData.append('configId', configId);
    formData.append('file', file);
    return apiClient.upload<{ message: string }>(`/tenant-management/chatbotconfigs/${configId}/knowledge-base/documents`, formData);
  }

  static async exportKnowledgeBase(configId: string, format: 'csv' | 'json' = 'csv'): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.get<{ downloadUrl: string }>(`/tenant-management/chatbotconfigs/${configId}/knowledge-base/export`, { format });
  }

  // Conversations - handled by ChatRuntimeService
  static async getConversations(params?: {
    status?: string;
    userId?: string;
    page?: number;
    limit?: number;
    dateFrom?: string;
    dateTo?: string;
  }): Promise<ApiResponse<{ conversations: Conversation[]; total: number }>> {
    return apiClient.get('/chat/conversations', params);
  }

  static async getConversation(id: string): Promise<ApiResponse<Conversation>> {
    return apiClient.get<Conversation>(`/chat/conversations/${id}`);
  }

  static async updateConversationStatus(id: string, status: 'active' | 'resolved' | 'closed'): Promise<ApiResponse<Conversation>> {
    return apiClient.put<Conversation>(`/chat/conversations/${id}/status`, { status });
  }

  static async rateConversation(id: string, rating: number, feedback?: string): Promise<ApiResponse<void>> {
    return apiClient.post<void>(`/chat/conversations/${id}/rate`, { rating, feedback });
  }

  // Analytics
  static async getAnalytics(configId: string, params?: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  }): Promise<ApiResponse<ChatbotAnalytics>> {
    return apiClient.get(`/tenant-management/chatbotconfigs/${configId}/analytics`, params);
  }

  // Training
  static async trainChatbot(configId: string): Promise<ApiResponse<{ message: string }>> {
    return apiClient.post<{ message: string }>(`/tenant-management/chatbotconfigs/${configId}/train`);
  }

  static async getTrainingData(configId: string): Promise<ApiResponse<unknown>> {
    return apiClient.get<unknown>(`/tenant-management/chatbotconfigs/${configId}/training-data`);
  }

  // Test chatbot - using ChatRuntimeService for actual chat functionality
  static async testChatbot(message: string, conversationId?: string): Promise<ApiResponse<{ userMessage: unknown; botMessage: { content: string }; success: boolean }>> {
    if (!conversationId) {
      const conversationResponse = await apiClient.post<{ id: string }>('/chat/conversations', {
        tenantId: localStorage.getItem('tenantId') || '',
        customerName: 'Test User',
        customerEmail: 'test@example.com',
        subject: 'Chatbot Test',
        language: 'en'
      });
      
      if (!conversationResponse.success || !conversationResponse.data) {
        throw new Error('Failed to create test conversation');
      }
      
      conversationId = conversationResponse.data.id;
    }

    return apiClient.post<{ userMessage: unknown; botMessage: { content: string }; success: boolean }>('/chat/messages', {
      conversationId,
      content: message,
      type: 'text'
    });
  }
}
