import axios, { AxiosInstance, AxiosResponse } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080/api';

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('token');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  async get<T>(url: string): Promise<T> {
    const response: AxiosResponse<T> = await this.client.get(url);
    return response.data;
  }

  async post<T>(url: string, data?: any): Promise<T> {
    const response: AxiosResponse<T> = await this.client.post(url, data);
    return response.data;
  }

  async put<T>(url: string, data?: any): Promise<T> {
    const response: AxiosResponse<T> = await this.client.put(url, data);
    return response.data;
  }

  async delete<T>(url: string): Promise<T> {
    const response: AxiosResponse<T> = await this.client.delete(url);
    return response.data;
  }
}

const apiClient = new ApiClient();

export const authApi = {
  login: async (email: string, password: string) => {
    return apiClient.post<{ user: any; token: string }>('/identity/auth/login', {
      email,
      password,
    });
  },

  register: async (userData: any) => {
    return apiClient.post<{ user: any; token: string }>('/identity/auth/register', userData);
  },

  getCurrentUser: async () => {
    return apiClient.get<any>('/identity/auth/me');
  },

  refreshToken: async () => {
    return apiClient.post<{ token: string }>('/identity/auth/refresh');
  },

  logout: async () => {
    return apiClient.post('/identity/auth/logout');
  },
};

export const tenantApi = {
  getTenants: async (page: number, pageSize: number, search?: string) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
      ...(search && { search }),
    });
    return apiClient.get<{
      data: any[];
      totalCount: number;
      currentPage: number;
      pageSize: number;
    }>(`/tenant-management/tenants?${params}`);
  },

  getTenant: async (id: string) => {
    return apiClient.get<any>(`/tenant-management/tenants/${id}`);
  },

  createTenant: async (tenantData: any) => {
    return apiClient.post<any>('/tenant-management/tenants', tenantData);
  },

  updateTenant: async (id: string, tenantData: any) => {
    return apiClient.put<any>(`/tenant-management/tenants/${id}`, tenantData);
  },

  deleteTenant: async (id: string) => {
    return apiClient.delete(`/tenant-management/tenants/${id}`);
  },

  getTenantStats: async (id: string) => {
    return apiClient.get<any>(`/tenant-management/tenants/${id}/stats`);
  },
};

export const userApi = {
  getUsers: async (page: number, pageSize: number, search?: string) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
      ...(search && { search }),
    });
    return apiClient.get<{
      data: any[];
      totalCount: number;
      currentPage: number;
      pageSize: number;
    }>(`/identity/users?${params}`);
  },

  getUser: async (id: string) => {
    return apiClient.get<any>(`/identity/users/${id}`);
  },

  createUser: async (userData: any) => {
    return apiClient.post<any>('/identity/users', userData);
  },

  updateUser: async (id: string, userData: any) => {
    return apiClient.put<any>(`/identity/users/${id}`, userData);
  },

  deleteUser: async (id: string) => {
    return apiClient.delete(`/identity/users/${id}`);
  },

  assignRole: async (userId: string, role: string) => {
    return apiClient.post(`/identity/users/${userId}/roles`, { role });
  },
};

export const analyticsApi = {
  getDashboardStats: async () => {
    return apiClient.get<any>('/analytics/dashboard');
  },

  getConversationMetrics: async (timeRange: string, tenantId?: string) => {
    const params = new URLSearchParams({ timeRange });
    if (tenantId) params.append('tenantId', tenantId);
    return apiClient.get<any>(`/analytics/conversations?${params}`);
  },

  getAgentMetrics: async (timeRange: string, tenantId?: string) => {
    const params = new URLSearchParams({ timeRange });
    if (tenantId) params.append('tenantId', tenantId);
    return apiClient.get<any>(`/analytics/agents?${params}`);
  },

  getBotMetrics: async (timeRange: string, tenantId?: string) => {
    const params = new URLSearchParams({ timeRange });
    if (tenantId) params.append('tenantId', tenantId);
    return apiClient.get<any>(`/analytics/bot?${params}`);
  },

  getCustomReport: async (reportConfig: any) => {
    return apiClient.post<any>('/analytics/reports/custom', reportConfig);
  },
};

export const subscriptionApi = {
  getPlans: async () => {
    return apiClient.get<any[]>('/subscription/plans');
  },

  getSubscriptions: async (page: number, pageSize: number) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    return apiClient.get<{
      data: any[];
      totalCount: number;
      currentPage: number;
      pageSize: number;
    }>(`/subscription/subscriptions?${params}`);
  },

  createSubscription: async (subscriptionData: any) => {
    return apiClient.post<any>('/subscription/subscriptions', subscriptionData);
  },

  updateSubscription: async (id: string, subscriptionData: any) => {
    return apiClient.put<any>(`/subscription/subscriptions/${id}`, subscriptionData);
  },

  cancelSubscription: async (id: string) => {
    return apiClient.post(`/subscription/subscriptions/${id}/cancel`);
  },

  getBillingStats: async () => {
    return apiClient.get<any>('/subscription/billing/stats');
  },
};

export default apiClient;
