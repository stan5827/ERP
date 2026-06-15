import { api, type ApiResponse } from './baseApi';

export interface EmployeeDto {
  id: string;
  employeeNo: string;
  chineseName: string;
  englishName?: string;
  email: string;
  status: string;
  departmentId: string;
  departmentName?: string;
  jobTitleId: string;
  jobTitleName?: string;
}

export const employeesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<EmployeeDto[]>>('/employees', { params }),
  get: (id: string) => api.get<ApiResponse<EmployeeDto>>(`/employees/${id}`),
  create: (data: any) => api.post<ApiResponse>('/employees', data),
  update: (id: string, data: any) => api.put<ApiResponse>(`/employees/${id}`, data),
};

export const departmentsApi = {
  list: () => api.get<ApiResponse<any[]>>('/departments'),
  flat: () => api.get<ApiResponse<any[]>>('/departments/flat'),
};

export const leaveRequestsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<any[]>>('/leaverequests', { params }),
  create: (data: any) => api.post<ApiResponse>('/leaverequests', data),
  approve: (id: string, status: string, comment?: string) => 
    api.post<ApiResponse>(`/leaverequests/${id}/approve`, { status, comment }),
};
