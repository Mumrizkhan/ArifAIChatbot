import { apiClient, ApiResponse } from './api';

// Tenant related types
export interface Tenant {
  id: string;
  name: string;
  domain: string;
  logo?: string;
  primaryColor: string;
  secondaryColor: string;
  settings: TenantSettings;
  subscription: TenantSubscription;
  createdAt: string;
  updatedAt: string;
}

export interface TenantSettings {
  allowRegistration: boolean;
  requireEmailVerification: boolean;
  maxUsers: number;
  features: string[];
  customDomain?: string;
  ssoEnabled: boolean;
}

export interface TenantSubscription {
  plan: string;
  status: 'active' | 'inactive' | 'trial' | 'expired';
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEnd?: string;
}

export interface CreateTenantData {
  name: string;
  domain: string;
  ownerEmail: string;
  ownerName: string;
  plan?: string;
}

export interface UpdateTenantData {
  name?: string;
  domain?: string;
  logo?: string;
  primaryColor?: string;
  secondaryColor?: string;
  settings?: Partial<TenantSettings>;
}

export class TenantService {
  // Get current tenant
  static async getCurrentTenant(): Promise<ApiResponse<Tenant>> {
    return apiClient.get<Tenant>('/tenant');
  }

  // Get tenant by ID
  static async getTenantById(id: string): Promise<ApiResponse<Tenant>> {
    return apiClient.get<Tenant>(`/tenant/${id}`);
  }

  // Create new tenant
  static async createTenant(data: CreateTenantData): Promise<ApiResponse<Tenant>> {
    return apiClient.post<Tenant>('/tenant', data);
  }

  // Update tenant
  static async updateTenant(data: UpdateTenantData): Promise<ApiResponse<Tenant>> {
    return apiClient.put<Tenant>('/tenant', data);
  }

  // Delete tenant
  static async deleteTenant(): Promise<ApiResponse<void>> {
    return apiClient.delete<void>('/tenant');
  }

  // Upload tenant logo
  static async uploadLogo(file: File): Promise<ApiResponse<{ logo: string }>> {
    const formData = new FormData();
    formData.append('logo', file);
    return apiClient.upload<{ logo: string }>('/tenant/logo', formData);
  }

  // Get tenant settings
  static async getSettings(): Promise<ApiResponse<TenantSettings>> {
    return apiClient.get<TenantSettings>('/tenant/settings');
  }

  // Update tenant settings
  static async updateSettings(settings: Partial<TenantSettings>): Promise<ApiResponse<TenantSettings>> {
    return apiClient.put<TenantSettings>('/tenant/settings', settings);
  }

  // Get subscription info
  static async getSubscription(): Promise<ApiResponse<TenantSubscription>> {
    return apiClient.get<TenantSubscription>('/tenant/subscription');
  }

  // Update subscription
  static async updateSubscription(plan: string): Promise<ApiResponse<TenantSubscription>> {
    return apiClient.put<TenantSubscription>('/tenant/subscription', { plan });
  }

  // Cancel subscription
  static async cancelSubscription(): Promise<ApiResponse<void>> {
    return apiClient.delete<void>('/tenant/subscription');
  }
}
