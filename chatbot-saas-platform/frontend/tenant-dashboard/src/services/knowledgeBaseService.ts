import { apiClient, ApiResponse } from './api';

export interface Document {
  id: string;
  title: string;
  originalFileName: string;
  fileType: string;
  fileSize: number;
  status: 'Uploaded' | 'Processing' | 'Processed' | 'Failed';
  summary?: string;
  tags?: string[];
  language?: string;
  chunkCount?: number;
  isEmbedded: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface KnowledgeBaseStatistics {
  totalDocuments: number;
  processedDocuments: number;
  failedDocuments: number;
  totalFileSize: number;
  totalChunks: number;
  documentsByType: Record<string, number>;
  documentsByLanguage: Record<string, number>;
  lastUpdated: string;
}

export interface DocumentSearchRequest {
  query: string;
  language?: string;
  tags?: string[];
  limit?: number;
  minScore?: number;
  filters?: Record<string, any>;
}

export interface DocumentSearchResult {
  id: string;
  title: string;
  content: string;
  score: number;
  metadata?: Record<string, any>;
}

export class KnowledgeBaseService {
  static async uploadDocument(file: File, title?: string, tags?: string[], language?: string): Promise<ApiResponse<{ DocumentId: string; Title: string; Status: string; Message: string }>> {
    const formData = new FormData();
    formData.append('File', file);
    if (title) formData.append('Title', title);
    if (tags) formData.append('Tags', JSON.stringify(tags));
    if (language) formData.append('Language', language);
    
    return apiClient.upload<{ DocumentId: string; Title: string; Status: string; Message: string }>('/knowledgebase/documents/upload', formData);
  }

  static async getDocuments(page: number = 1, pageSize: number = 20): Promise<ApiResponse<Document[]>> {
    return apiClient.get<Document[]>('/knowledgebase/documents', { page, pageSize });
  }

  static async getDocument(documentId: string): Promise<ApiResponse<Document>> {
    return apiClient.get<Document>(`/knowledgebase/documents/${documentId}`);
  }

  static async deleteDocument(documentId: string): Promise<ApiResponse<{ message: string }>> {
    return apiClient.delete<{ message: string }>(`/knowledgebase/documents/${documentId}`);
  }

  static async searchDocuments(request: DocumentSearchRequest): Promise<ApiResponse<DocumentSearchResult[]>> {
    return apiClient.post<DocumentSearchResult[]>('/knowledgebase/documents/search', request);
  }

  static async getStatistics(): Promise<ApiResponse<KnowledgeBaseStatistics>> {
    return apiClient.get<KnowledgeBaseStatistics>('/knowledgebase/documents/statistics');
  }

  static async reprocessDocument(documentId: string): Promise<ApiResponse<{ message: string }>> {
    return apiClient.post<{ message: string }>(`/knowledgebase/documents/${documentId}/reprocess`);
  }

  static async downloadDocument(documentId: string): Promise<Response> {
    const response = await fetch(`/knowledgebase/documents/${documentId}/download`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`,
      },
    });
    return response;
  }
}
