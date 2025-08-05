import axios, { AxiosInstance } from 'axios';

class ApiClient {
  private axiosInstance: AxiosInstance;
  private tenantId: string | null = null;

  constructor() {
    this.axiosInstance = axios.create({
      baseURL: 'http://localhost:8000',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    this.axiosInstance.interceptors.request.use(
      (config) => {
        if (this.tenantId) {
          config.headers['X-Tenant-ID'] = this.tenantId;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    this.axiosInstance.interceptors.response.use(
      (response) => response,
      (error) => {
        console.error('API Error:', error);
        return Promise.reject(error);
      }
    );
  }

  setTenantId(tenantId: string) {
    this.tenantId = tenantId;
  }

  getTenantId(): string | null {
    return this.tenantId;
  }

  async get(url: string, config?: any) {
    const response = await this.axiosInstance.get(url, config);
    return response.data;
  }

  async post(url: string, data?: any, config?: any) {
    const response = await this.axiosInstance.post(url, data, config);
    return response.data;
  }

  async put(url: string, data?: any, config?: any) {
    const response = await this.axiosInstance.put(url, data, config);
    return response.data;
  }

  async delete(url: string, config?: any) {
    const response = await this.axiosInstance.delete(url, config);
    return response.data;
  }
}

export const apiClient = new ApiClient();
