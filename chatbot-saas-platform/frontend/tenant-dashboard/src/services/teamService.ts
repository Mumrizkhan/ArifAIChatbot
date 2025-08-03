import { apiClient, ApiResponse } from './api';

// Team related types
export interface TeamMember {
  id: string;
  userId: string;
  tenantId: string;
  email: string;
  name: string;
  role: string;
  permissions: string[];
  avatar?: string;
  status: 'active' | 'inactive' | 'pending';
  invitedAt: string;
  joinedAt?: string;
  lastActiveAt?: string;
}

export interface TeamRole {
  id: string;
  name: string;
  description: string;
  permissions: string[];
  isCustom: boolean;
}

export interface InviteTeamMemberData {
  email: string;
  role: string;
  permissions?: string[];
  message?: string;
}

export interface UpdateTeamMemberData {
  role?: string;
  permissions?: string[];
  status?: 'active' | 'inactive';
}

export interface CreateRoleData {
  name: string;
  description: string;
  permissions: string[];
}

export interface TeamStats {
  totalMembers: number;
  activeMembers: number;
  pendingInvites: number;
  inactiveMembers: number;
}

export class TeamService {
  // Get all team members
  static async getTeamMembers(params?: {
    page?: number;
    limit?: number;
    role?: string;
    status?: string;
    search?: string;
  }): Promise<ApiResponse<{ members: TeamMember[]; total: number; page: number; limit: number }>> {
    return apiClient.get('/team/members', params);
  }

  // Get team member by ID
  static async getTeamMember(id: string): Promise<ApiResponse<TeamMember>> {
    return apiClient.get<TeamMember>(`/team/members/${id}`);
  }

  // Invite team member
  static async inviteTeamMember(data: InviteTeamMemberData): Promise<ApiResponse<TeamMember>> {
    return apiClient.post<TeamMember>('/team/invite', data);
  }

  // Update team member
  static async updateTeamMember(id: string, data: UpdateTeamMemberData): Promise<ApiResponse<TeamMember>> {
    return apiClient.put<TeamMember>(`/team/members/${id}`, data);
  }

  // Remove team member
  static async removeTeamMember(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/team/members/${id}`);
  }

  // Resend invitation
  static async resendInvitation(id: string): Promise<ApiResponse<void>> {
    return apiClient.post<void>(`/team/members/${id}/resend-invite`);
  }

  // Cancel invitation
  static async cancelInvitation(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/team/invites/${id}`);
  }

  // Get team roles
  static async getRoles(): Promise<ApiResponse<TeamRole[]>> {
    return apiClient.get<TeamRole[]>('/team/roles');
  }

  // Create custom role
  static async createRole(data: CreateRoleData): Promise<ApiResponse<TeamRole>> {
    return apiClient.post<TeamRole>('/team/roles', data);
  }

  // Update role
  static async updateRole(id: string, data: Partial<CreateRoleData>): Promise<ApiResponse<TeamRole>> {
    return apiClient.put<TeamRole>(`/team/roles/${id}`, data);
  }

  // Delete role
  static async deleteRole(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/team/roles/${id}`);
  }

  // Get team statistics
  static async getTeamStats(): Promise<ApiResponse<TeamStats>> {
    return apiClient.get<TeamStats>('/team/stats');
  }

  // Get available permissions
  static async getPermissions(): Promise<ApiResponse<string[]>> {
    return apiClient.get<string[]>('/team/permissions');
  }

  // Bulk update team members
  static async bulkUpdateMembers(memberIds: string[], data: UpdateTeamMemberData): Promise<ApiResponse<void>> {
    return apiClient.put<void>('/team/members/bulk', { memberIds, ...data });
  }

  // Export team members
  static async exportMembers(format: 'csv' | 'xlsx' = 'csv'): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.get<{ downloadUrl: string }>('/team/export', { format });
  }
}
