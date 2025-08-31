import axios, { AxiosInstance, AxiosResponse } from "axios";

// Use Vite env (set at build/preview time)
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string) || "https://api-stg-arif.tetco.sa";
const API_TIMEOUT = Number(import.meta.env.VITE_API_TIMEOUT) || 10000;
const AUTH_TOKEN_KEY = (import.meta.env.VITE_AUTH_TOKEN_KEY as string) || "token";

// debug: confirm at runtime (preview or dev console)
console.log("VITE env:", { MODE: import.meta.env.MODE, API_BASE_URL, API_TIMEOUT, AUTH_TOKEN_KEY });

class ApiClient {
  private client: AxiosInstance;
  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      timeout: API_TIMEOUT,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem(AUTH_TOKEN_KEY);
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
          localStorage.removeItem(AUTH_TOKEN_KEY);
          window.location.href = "/login";
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
    return apiClient.post<{ user: any; token: string }>("/identity/auth/login", {
      email,
      password,
    });
  },

  register: async (userData: any) => {
    return apiClient.post<{ user: any; token: string }>("/identity/auth/register", userData);
  },

  getCurrentUser: async () => {
    return apiClient.get<any>("/identity/auth/me");
  },

  refreshToken: async () => {
    return apiClient.post<{ token: string }>("/identity/auth/refresh");
  },

  logout: async () => {
    return apiClient.post("/identity/auth/logout");
  },
};

export const systemSettingsApi = {
  getSystemSettings: () =>
    apiClient.get('/tenant-management/systemsettings'),
  
  updateSystemSettings: (data: { systemSettings?: Record<string, any>; notificationSettings?: Record<string, any>; integrationSettings?: Record<string, any> }) =>
    apiClient.put('/tenant-management/systemsettings', data),
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
    return apiClient.post<any>("/tenant-management/tenants", tenantData);
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

  getTenantSettings: async (id: string) => {
    return apiClient.get<any>(`/tenant-management/tenants/${id}/settings`);
  },

  updateTenantSettings: async (id: string, settings: any) => {
    return apiClient.put<any>(`/tenant-management/tenants/${id}/settings`, settings);
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
    return apiClient.post<any>("/identity/users", userData);
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
    return apiClient.get<any>("/analytics/analytics/dashboard");
  },

  getConversationMetrics: async (timeRange: string, tenantId?: string) => {
    const params = new URLSearchParams({ timeRange });
    if (tenantId) params.append("tenantId", tenantId);
    return apiClient.get<any>(`/analytics/analytics/conversations?${params}`);
  },

  getAgentMetrics: async (timeRange: string, tenantId?: string) => {
    const params = new URLSearchParams({ timeRange });
    if (tenantId) params.append("tenantId", tenantId);
    return apiClient.get<any>(`/analytics/analytics/agents?${params}`);
  },

  getBotMetrics: async (timeRange: string, tenantId?: string) => {
    const params = new URLSearchParams({ timeRange });
    if (tenantId) params.append("tenantId", tenantId);
    return apiClient.get<any>(`/analytics/analytics/bot?${params}`);
  },

  getCustomReport: async (reportConfig: any) => {
    return apiClient.post<any>("/analytics/analytics/reports/custom", reportConfig);
  },
};

export const subscriptionApi = {
  getPlans: async () => {
    return apiClient.get<any[]>("/subscription/plans");
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
    return apiClient.post<any>("/subscription/subscriptions", subscriptionData);
  },

  updateSubscription: async (id: string, subscriptionData: any) => {
    return apiClient.put<any>(`/subscription/subscriptions/${id}`, subscriptionData);
  },

  cancelSubscription: async (id: string) => {
    return apiClient.post(`/subscription/subscriptions/${id}/cancel`);
  },

  getBillingStats: async () => {
    return apiClient.get<any>("subscription/subscriptions/billing/stats");
  },
  createPlan: async (planData: any) => {
    return apiClient.post<any>("/subscription/Plans", planData);
  },
};

export default apiClient;
