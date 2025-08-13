import axios, { AxiosInstance } from "axios";

// Get API URL from environment with fallback
const VITE_API_URL = import.meta.env.VITE_API_URL || "https://api-stg.arif.sa";

class ApiClient {
  private axiosInstance: AxiosInstance;
  private tenantId: string | null = null;

  constructor() {
    console.log("🔧 ApiClient initialized with baseURL:", VITE_API_URL);
    
    this.axiosInstance = axios.create({
      baseURL: VITE_API_URL,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    this.axiosInstance.interceptors.request.use(
      (config) => {
        if (this.tenantId) {
          config.headers["X-Tenant-ID"] = this.tenantId;
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
        console.error("API Error:", error);
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
