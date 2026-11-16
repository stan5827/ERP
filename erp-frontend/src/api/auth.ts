import { api, ApiResponse } from './client';

export interface LoginRequest { username: string; password: string; }
export interface LoginResponse {
  userId: string; employeeId?: string; username: string; email?: string;
  token: string; expiresAt: string; roles: string[]; permissions: string[];
  mustChangePassword: boolean;
}
export interface UserDto { id: string; username: string; email?: string; employeeId?: string; isActive: boolean; lastLoginAt?: string; createdAt: string; roles: string[]; }
export interface RoleDto { id: string; roleName: string; description?: string; isSystem: boolean; permissions: string[]; }

export const authApi = {
  login: (req: LoginRequest) => api.post<ApiResponse<LoginResponse>>('/auth/login', req),
  changePassword: (req: { oldPassword: string; newPassword: string }) =>
    api.post<ApiResponse>('/auth/change-password', req),
  adminReset: (req: { secret: string; username: string; newPassword: string }) =>
    api.post<ApiResponse>('/auth/admin-reset', req),
  register: (req: { username: string; password: string; email?: string; roleIds: string[] }) =>
    api.post<ApiResponse>('/auth/register', req),
};

export const usersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<UserDto[]>>('/users', { params }),
  get: (id: string) => api.get<ApiResponse<UserDto>>(`/users/${id}`),
  create: (data: { username: string; password: string; email?: string; employeeId: string; roleIds: string[] }) => api.post<ApiResponse>('/users', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/users/${id}`, data),
  updateRoles: (id: string, data: { roleIds: string[]; reason: string }) =>
    api.put<ApiResponse>(`/users/${id}/roles`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/users/${id}`),
};

export const rolesApi = {
  list: () => api.get<ApiResponse<RoleDto[]>>('/roles'),
  get: (id: string) => api.get<ApiResponse<RoleDto>>(`/roles/${id}`),
  create: (data: { roleName: string; description?: string; permissionIds: string[] }) => api.post<ApiResponse>('/roles', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/roles/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/roles/${id}`),
};

export const permissionsApi = {
  list: () => api.get<ApiResponse<Record<string, { id: string; permissionCode: string; description?: string }[]>>>('/permissions'),
};

export const commonApi = {
  getNotifications: (params?: { role?: string; userId?: string }) => 
    api.get<ApiResponse<unknown[]>>('/notifications', { params }),
};
