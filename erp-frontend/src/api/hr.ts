import { api, ApiResponse, toPascalCase } from './client';

export interface DepartmentDto { id: string; deptCode: string; deptName: string; parentDeptId?: string; costCenter?: string; isActive: boolean; parentDeptName?: string; employeeCount: number; }
export interface EmployeeDto {
  id: string; employeeNo: string; chineseName: string; englishName?: string; nationalId?: string;
  passportNo?: string; birthDate?: string; gender?: string; phone?: string; mobile?: string; email?: string;
  registeredAddress?: string; mailingAddress?: string;
  emergencyContactName?: string; emergencyContactPhone?: string; emergencyContactRelation?: string;
  departmentId?: string; departmentName?: string;
  jobTitleId?: string; jobTitleName?: string; gradeId?: string; gradeName?: string;
  positionTitle?: string; jobGrade?: string; jobCategory?: string;
  supervisorId?: string; supervisorName?: string; hireDate?: string; probationEndDate?: string;
  resignationDate?: string; status: string; salaryType?: string;
  bankCode?: string; bankBranch?: string; bankAccount?: string;
  baseSalary?: number; foodAllowance?: number; positionAllowance?: number; fullAttendanceBonus?: number;
  laborInsuranceGrade?: number; healthInsuranceGrade?: number; pensionContributionRate?: number; taxDependents?: number;
  cardNumber?: string; biometricId?: string; shiftType?: string; overtimePermission?: boolean;
  highestEducation?: string; major?: string; graduationYear?: number; foreignLanguages?: string; skills?: string;
  createdAt?: string;
}

export const departmentsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<DepartmentDto[]>>('/departments', { params }),
  flat: () => api.get<ApiResponse<DepartmentDto[]>>('/departments/flat'),
  tree: () => api.get<ApiResponse<DepartmentDto[]>>('/departments/tree'),
  get: (id: string) => api.get<ApiResponse<DepartmentDto>>(`/departments/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/departments', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/departments/${id}`, toPascalCase(data)),
  delete: (id: string) => api.delete<ApiResponse>(`/departments/${id}`),
};

export const employeesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<EmployeeDto[]>>('/employees', { params }),
  get: (id: string) => api.get<ApiResponse<EmployeeDto>>(`/employees/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/employees', toPascalCase(data)),
  update: async (id: string, data: Record<string, unknown>) => {
    const pascalData = toPascalCase(data);
    return api.put<ApiResponse>(`/employees/${id}`, pascalData);
  },
  terminate: (id: string, reason: string, effectiveDate: string) =>
    api.patch<ApiResponse>(`/employees/${id}/terminate`, { reason, effectiveDate }),
  rehire: (id: string, reason: string) => api.patch<ApiResponse>(`/employees/${id}/rehire`, { reason }),
};

export const jobTitlesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/job-titles', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/job-titles/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/job-titles', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/job-titles/${id}`, toPascalCase(data)),
  delete: (id: string) => api.delete<ApiResponse>(`/job-titles/${id}`),
};

export const jobGradesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/job-grades', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/job-grades/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/job-grades', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/job-grades/${id}`, toPascalCase(data)),
  delete: (id: string) => api.delete<ApiResponse>(`/job-grades/${id}`),
};

export const costCentersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/cost-centers', { params }),
  tree: () => api.get<ApiResponse<unknown[]>>('/cost-centers/tree'),
  budget: () => api.get<ApiResponse<unknown[]>>('/cost-centers/budget'),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/cost-centers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/cost-centers', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/cost-centers/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/cost-centers/${id}`),
};

export const costElementsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/cost-elements', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/cost-elements/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/cost-elements', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/cost-elements/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/cost-elements/${id}`),
};

export const activityTypesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/activity-types', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/activity-types/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/activity-types', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/activity-types/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/activity-types/${id}`),
};

export const approvalsApi = {
  authorities: () => api.get<ApiResponse<unknown[]>>('/approvals/authorities'),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/approvals/authorities', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/approvals/authorities/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/approvals/authorities/${id}`),
};

export const allocationRulesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/allocation/rules', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/allocation/rules/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/allocation/rules', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/allocation/rules/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/allocation/rules/${id}`),
};

export const costPostingsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/cost-postings', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/cost-postings/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/cost-postings', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/cost-postings/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/cost-postings/${id}`),
};

export const workCentersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/work-centers', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/work-centers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/work-centers', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/work-centers/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/work-centers/${id}`),
};
