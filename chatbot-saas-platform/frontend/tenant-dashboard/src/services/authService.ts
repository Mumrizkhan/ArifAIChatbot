import { apiClient, ApiResponse } from './api';

// Auth related types
export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterData {
  email: string;
  password: string;
  name: string;
  tenantName?: string;
}

export interface User {
  id: string;
  email: string;
  name: string;
  role: string;
  tenantId: string;
  avatar?: string;
  permissions: string[];
}

export interface AuthResponse {
  user: User;
  token: string;
  refreshToken: string;
}

export interface ForgotPasswordData {
  email: string;
}

export interface ResetPasswordData {
  token: string;
  password: string;
}

export interface ChangePasswordData {
  currentPassword: string;
  newPassword: string;
}

export class AuthService {
  // Login user
  static async login(credentials: LoginCredentials): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post<AuthResponse>('/auth/login', credentials);
  }

  // Register new user
  static async register(data: RegisterData): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post<AuthResponse>('/auth/register', data);
  }

  // Get current user
  static async getCurrentUser(): Promise<ApiResponse<User>> {
    return apiClient.get<User>('/auth/me');
  }

  // Refresh token
  static async refreshToken(): Promise<ApiResponse<{ token: string }>> {
    const refreshToken = localStorage.getItem('refreshToken');
    return apiClient.post<{ token: string }>('/auth/refresh', { refreshToken });
  }

  // Logout
  static async logout(): Promise<ApiResponse<void>> {
    const refreshToken = localStorage.getItem('refreshToken');
    return apiClient.post<void>('/auth/logout', { refreshToken });
  }

  // Forgot password
  static async forgotPassword(data: ForgotPasswordData): Promise<ApiResponse<void>> {
    return apiClient.post<void>('/auth/forgot-password', data);
  }

  // Reset password
  static async resetPassword(data: ResetPasswordData): Promise<ApiResponse<void>> {
    return apiClient.post<void>('/auth/reset-password', data);
  }

  // Change password
  static async changePassword(data: ChangePasswordData): Promise<ApiResponse<void>> {
    return apiClient.put<void>('/auth/change-password', data);
  }

  // Update profile
  static async updateProfile(data: Partial<User>): Promise<ApiResponse<User>> {
    return apiClient.put<User>('/auth/profile', data);
  }

  // Upload avatar
  static async uploadAvatar(file: File): Promise<ApiResponse<{ avatar: string }>> {
    const formData = new FormData();
    formData.append('avatar', file);
    return apiClient.upload<{ avatar: string }>('/auth/avatar', formData);
  }
}
