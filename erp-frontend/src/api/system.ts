import { api, ApiResponse } from './client';

// ========== Shared DTO interfaces ==========
export interface AssetDto { id: string; assetNumber: string; assetName: string; specification?: string; categoryName?: string; locationName?: string; acquisitionCost: number; acquisitionDate?: string; usefulLife: number; status: string; }

export interface IqcReportDto {
  id: string; reportNumber: string; goodsReceiptId: string; goodsReceiptNumber?: string;
  supplierName?: string; skuId?: string; skuName?: string; batchNumber?: string;
  receivedQty: number; sampleQty: number; defectQty: number; inspectionDate: string; result: string;
}
export interface IqcInspectionItemDto {
  id: string; inspectionItem?: string; specification?: string; lowerLimit?: number;
  upperLimit?: number; measuredValue?: string; result: string;
}
export interface IqcInspectionDto {
  id: string; reportNumber: string; goodsReceiptId: string; goodsReceiptNumber: string;
  goodsReceiptItemId?: string; skuId?: string; skuNumber?: string; skuName?: string;
  supplierName?: string; lotNumber?: string; receivedQty: number; releaseBinLocationId?: string;
  releaseBinLocationCode?: string; result: string; sampleSize: number; acceptNumber: number;
  rejectNumber: number; inspectorId?: string; inspectionDate: string; items: IqcInspectionItemDto[];
}

export interface SupplierComplaintDto { id: string; complaintNumber: string; supplierName?: string; skuName?: string; defectType?: string; ppm: number; priority: string; status: string; }

export interface RndProjectDto { id: string; projectCode: string; projectName: string; projectType?: string; status: string; estimatedBudget?: number; startDate?: string; description?: string; managerName?: string; budget?: number; progress?: number; }

export interface RndLogDto { id: string; logType?: string; content?: string; severity?: string; createdAt?: string; }

export interface AnnouncementDto { id: string; title: string; category: string; content?: string; isPinned: boolean; isUrgent: boolean; publishDate: string; }

export interface CrossDeptTaskDto { id: string; taskName: string; urgency: string; status: string; ownerDeptName?: string; expectedCompleteDate?: string; }

export interface CalendarEventDto { id: string; title: string; description?: string; eventType: string; categoryCode?: string; categoryName?: string; categoryColor?: string; location?: string; startDate: string; endDate: string; isAllDay: boolean; recurrenceRule?: string; recurrenceParentId?: string; participants?: string; organizerId?: string; departmentId?: string; visibility: string; sourceModule?: string; sourceId?: string; meetingRoomId?: string; meetingRoomName?: string; projectId?: string; color?: string; isPinned: boolean; reminders?: string; status: string; syncToOutlook: boolean; createdAt: string; updatedAt?: string; }

export interface CalendarCategoryDto { id: string; code: string; name: string; color: string; icon?: string; sortOrder: number; isActive: boolean; }

export interface AttendanceRecordDto { id: string; employeeId: string; employeeNo?: string; employeeName?: string; departmentId?: string; productionLine?: string; date: string; shiftType?: string; scheduledStartTime?: string; scheduledEndTime?: string; clockIn?: string; clockOut?: string; clockInLocation?: string; clockOutLocation?: string; status: string; lateFlag: boolean; lateMinutes: number; earlyLeaveFlag: boolean; earlyLeaveMinutes: number; clockingAnomalyFlag: boolean; anomalyType?: string; ot1Hours: number; ot2Hours: number; ot3Hours: number; otReason?: string; approvalStatus: string; notes?: string; }

export interface LeaveRequestDto { id: string; employeeId: string; employeeNo?: string; employeeName?: string; applicationNo: string; leaveType: string; applicationDate: string; startDate: string; endDate: string; startTime?: string; endTime?: string; totalHours: number; reason?: string; attachmentUrl?: string; includesSpecialPeriod: boolean; shiftConflictFlag: boolean; autoDeductPayFlag: boolean; impactOtOrBonus: boolean; approvalLevel: number; status: string; approverId?: string; approvedAt?: string; rejectReason?: string; remarks?: string; createdAt: string; }

export interface PayrollDto { id: string; employeeId: string; employeeNo?: string; employeeName?: string; periodStart: string; periodEnd: string; basicSalary: number; insuranceBase: number; pensionBase: number; scheduledDays: number; actualWorkingDays: number; totalLateMinutes: number; totalEarlyMinutes: number; ot1Pay: number; ot2Pay: number; ot3Pay: number; bonus: number; totalDeductions: number; grossPay: number; netPay: number; status: string; createdAt: string; updatedAt: string; }

export interface PayrollPolicyAuditEventDto { action: string; actorUserId: string; actorUsername: string; occurredAt: string; details?: string; }
export interface PayrollPolicyVersionDto {
  id: string; versionCode: string; effectiveFrom: string; approvalDocumentReference: string; policyJson: string;
  status: 'draft' | 'pending_finance' | 'approved' | 'rejected'; createdByUserId: string; createdByUsername: string;
  createdAt: string; updatedAt: string; submittedByUserId?: string; submittedByUsername?: string; submittedAt?: string;
  financeReviewerUserId?: string; financeReviewerUsername?: string; financeReviewedAt?: string; reviewComment?: string;
  calculationEnabled: false; auditTrail: PayrollPolicyAuditEventDto[];
}
export interface SavePayrollPolicyRequest {
  versionCode: string; effectiveFrom: string; approvalDocumentReference: string; policyJson: string;
}

export interface UomDto { id: string; uomCode: string; uomName: string; uomNameEn?: string; uomType: string; decimalPlaces: number; isActive: boolean; allowDecimal: boolean; description?: string; displayOrder: number; createdAt: string; }

export interface UomConversionDto { id: string; skuId?: string; skuCode?: string; skuName?: string; fromUomId: string; fromUomCode: string; fromUomName: string; toUomId: string; toUomCode: string; toUomName: string; factor: number; operator: string; inverseFactor?: number; isActive: boolean; description?: string; isGlobal: boolean; }

export interface SubstituteItemDto { id: string; groupId: string; groupCode?: string; skuId: string; skuNumber?: string; skuName?: string; priority: number; conversionRate?: number; isPrimary: boolean; status: string; effectiveFrom?: string; effectiveTo?: string; notes?: string; }

export interface SubstituteGroupDto { id: string; groupCode: string; groupName?: string; description?: string; substitutionType: string; allowAutoSubstitute: boolean; isBidirectional: boolean; isActive: boolean; createdAt: string; updatedAt?: string; items: SubstituteItemDto[]; }

// ========== Existing API modules (unchanged below) ==========
export interface PermissionTreeDto { id: string; permissionCode: string; moduleName: string; actionType: string; permissionType: string; description?: string; sortOrder: number; children: PermissionTreeDto[]; }
export interface UserWithRolesDto { id: string; username: string; email?: string; employeeId?: string; isActive: boolean; createdAt: string; roles: { id: string; roleName: string; description?: string; isSystem: boolean; createdAt: string; permissionIds: string[]; }[]; }
export interface RoleWithPermissionsDto { id: string; roleName: string; description?: string; isSystem: boolean; isActive: boolean; createdAt: string; permissionIds: string[]; }
export interface ThemeDto { name: string; displayName: string; description?: string; isDefault: boolean; colors?: ThemeColorsDto; }
export interface ThemeColorsDto { primary: string; accent: string; background: string; surface: string; text: string; border: string; }
export interface UserThemePreferenceDto { themeName: string; customPrimaryColor?: string; customAccentColor?: string; }
export interface ModuleDto { id: string; name: string; icon: string; description?: string; sortOrder: number; }
export interface ModuleAccess { moduleId: string; permissions: string[]; }

export const systemSettingsApi = {
  getPermissionTree: () => api.get<ApiResponse<PermissionTreeDto[]>>('/system-settings/permissions/tree'),
  createPermission: (data: { permissionCode: string; moduleName: string; actionType: string; permissionType: string; description?: string; parentId?: string; sortOrder: number }) => api.post<ApiResponse>('/system-settings/permissions', data),
  updatePermission: (id: string, data: { permissionCode: string; moduleName: string; actionType: string; permissionType: string; description?: string; parentId?: string; sortOrder: number }) => api.put<ApiResponse>(`/system-settings/permissions/${id}`, data),
  deletePermission: (id: string) => api.delete<ApiResponse>(`/system-settings/permissions/${id}`),
  getUsers: () => api.get<ApiResponse<UserWithRolesDto[]>>('/system-settings/users'),
  getLinkedEmployeeIds: () => api.get<ApiResponse<string[]>>('/system-settings/users/linked-employee-ids'),
  updateUserRoles: (userId: string, roleIds: string[], reason: string) => api.put<ApiResponse>(`/users/${userId}/roles`, { roleIds, reason }),
  getRoles: () => api.get<ApiResponse<RoleWithPermissionsDto[]>>('/system-settings/roles'),
  updateRolePermissions: (roleId: string, permissionIds: string[]) => api.put<ApiResponse>(`/system-settings/roles/${roleId}/permissions`, { permissionIds }),
  createRole: (data: { roleName: string; description?: string }) => api.post<ApiResponse>('/system-settings/roles', data),
  deleteRole: (roleId: string) => api.delete<ApiResponse>(`/system-settings/roles/${roleId}`),
  getThemes: () => api.get<ApiResponse<ThemeDto[]>>('/system-settings/themes'),
  getUserTheme: (userId: string) => api.get<ApiResponse<UserThemePreferenceDto>>(`/system-settings/users/${userId}/theme`),
  updateUserTheme: (userId: string, data: { themeName: string; customPrimaryColor?: string; customAccentColor?: string }) => api.put<ApiResponse>(`/system-settings/users/${userId}/theme`, data),
  getModules: () => api.get<ApiResponse<ModuleDto[]>>('/system-settings/modules'),
  getRoleModules: (roleName: string) => api.get<ApiResponse<ModuleAccess[]>>(`/system-settings/role-modules/${roleName}`),
  getCurrentUserModules: () => api.get<ApiResponse<ModuleAccess[]>>('/system-settings/permissions/me'),
};

export const assetsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; assetNumber: string; assetName: string; specification?: string; categoryName?: string; locationName?: string; acquisitionCost: number; acquisitionDate?: string; usefulLife: number; status: string; }[]>>('/assets', { params }),
  get: (id: string) => api.get<ApiResponse<{ id: string; assetNumber: string; assetName: string; specification?: string; categoryName?: string; locationName?: string; acquisitionCost: number; acquisitionDate?: string; usefulLife: number; status: string; }>>(`/assets/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/assets', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/assets/${id}`, data),
  depreciate: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/depreciation`, data),
  transfer: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/transfer`, data),
  maintenance: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/maintenance`, data),
  dispose: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/dispose`, data),
  categories: () => api.get<ApiResponse<{ id: string; categoryCode: string; categoryName: string; depreciationMethod: string; defaultUsefulLife: number; }[]>>('/assets/categories'),
  createCategory: (data: Record<string, unknown>) => api.post<ApiResponse>('/assets/categories', data),
  locations: () => api.get<ApiResponse<unknown[]>>('/assets/locations'),
};

export const budgetApi = {
  years: () => api.get<ApiResponse<{ id: string; year: number; status: string; }[]>>('/budget/years'),
  createYear: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/years', data),
  updateYear: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/budget/years/${id}`, data),
  versions: (yearId: string) => api.get<ApiResponse<{ id: string; versionNumber: string; versionName?: string; isActive: boolean; }[]>>(`/budget/years/${yearId}/versions`),
  createVersion: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/versions', data),
  salesForecasts: (versionId: string) => api.get<ApiResponse<unknown[]>>(`/budget/versions/${versionId}/sales-forecasts`),
  createSalesForecast: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/sales-forecasts', data),
  expenseBudgets: (versionId: string) => api.get<ApiResponse<unknown[]>>(`/budget/versions/${versionId}/expense-budgets`),
  createExpenseBudget: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/expense-budgets', data),
  laborBudgets: (versionId: string) => api.get<ApiResponse<unknown[]>>(`/budget/versions/${versionId}/labor-budgets`),
  createLaborBudget: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/labor-budgets', data),
  capexBudgets: (versionId: string) => api.get<ApiResponse<unknown[]>>(`/budget/versions/${versionId}/capex-budgets`),
  createCapexBudget: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/capex-budgets', data),
};

export const qualityApi = {
  iqcReports: (params?: Record<string, string>) => api.get<ApiResponse<IqcReportDto[]>>('/quality/iqc-reports', { params }),
  iqcInspection: (id: string) => api.get<ApiResponse<IqcInspectionDto>>(`/quality/iqc-reports/${id}`),
  completeIqcInspection: (id: string, data: {
    result: 'pass' | 'fail'; sampleSize: number; acceptNumber: number;
    rejectNumber: number; items: Array<{
      inspectionItem: string; specification: string; lowerLimit?: number; upperLimit?: number;
      measuredValue: string; result: 'pass' | 'fail';
    }>;
  }) => api.post<ApiResponse<IqcInspectionDto>>(`/quality/iqc-reports/${id}/decision`, data),
  ipqcReports: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quality/ipqc-reports', { params }),
  createIpqcReport: (data: Record<string, unknown>) => api.post<ApiResponse>('/quality/ipqc-reports', data),
  oqcReports: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quality/oqc-reports', { params }),
  ncr: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quality/ncr', { params }),
  complaints: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; complaintNumber: string; supplierName?: string; skuName?: string; defectType?: string; ppm: number; priority: string; status: string; }[]>>('/quality/complaints', { params }),
  createComplaint: (data: Record<string, unknown>) => api.post<ApiResponse>('/quality/complaints', data),
  createCapa: (complaintId: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/quality/complaints/${complaintId}/capa`, data),
  traceLot: (lotNumber: string) => api.get<ApiResponse<{ lotNumber: string }>>(`/quality/trace/${lotNumber}`),
  traceMaterial: (skuId: string) => api.get<ApiResponse<{ skuId: string }>>(`/quality/trace/material/${skuId}`),
};

export const rndApi = {
  projects: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; projectCode: string; projectName: string; projectType: string; status: string; estimatedBudget: number; startDate?: string; }[]>>('/rd/projects', { params }),
  getProject: (id: string) => api.get<ApiResponse<{ id: string; projectCode: string; projectName: string; projectType: string; status: string; estimatedBudget: number; startDate?: string; }>>(`/rd/projects/${id}`),
  createProject: async (data: Record<string, unknown>) => {
    const transformedData = { ...data, ProjectCode: data.ProjectCode || '', ProjectName: data.ProjectName || '', ProjectType: data.ProjectType || 'new_product', EstimatedBudget: data.EstimatedBudget !== undefined ? parseFloat(String(data.EstimatedBudget)) : 0, TargetPrice: data.TargetPrice !== null && data.TargetPrice !== undefined && data.TargetPrice !== '' ? parseFloat(String(data.TargetPrice)) : null, StartDate: data.StartDate ? new Date(data.StartDate as string).toISOString().split('T')[0] : undefined, TargetSampleDate: data.TargetSampleDate ? new Date(data.TargetSampleDate as string).toISOString().split('T')[0] : undefined, TargetProductionDate: data.TargetProductionDate ? new Date(data.TargetProductionDate as string).toISOString().split('T')[0] : undefined, };
    const cleanedData = Object.fromEntries(Object.entries(transformedData).filter(([, value]) => value !== undefined));
    const response = await api.post<ApiResponse>('/rd/projects', cleanedData);
    if (!response.data?.success) throw new Error(response.data?.message || '建立失敗');
    return response.data;
  },
  updateStatus: (id: string, status: string) => api.patch<ApiResponse>(`/rd/projects/${id}/status`, { status }),
  documents: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rd/documents', { params }),
  createDocument: (data: Record<string, unknown>) => api.post<ApiResponse>('/rd/documents', data),
  logs: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rd/logs', { params }),
  createLog: (data: Record<string, unknown>) => api.post<ApiResponse>('/rd/logs', data),
};

export const trialProductionApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rd/trial-productions', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<unknown>>('/rd/trial-productions', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<unknown>>(`/rd/trial-productions/${id}`, data),
};

export const massProductionApi = {
  transition: (data: Record<string, unknown>) => api.post<ApiResponse<unknown>>('/rd/mass-production-transition', data),
  complete: (id: string) => api.patch<ApiResponse<unknown>>(`/rd/mass-production-transition/${id}/complete`),
};

export const collaborationApi = {
  announcements: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; title: string; category: string; content?: string; isPinned: boolean; isUrgent: boolean; publishDate: string; }[]>>('/collaboration/announcements', { params }),
  createAnnouncement: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/announcements', data),
  markRead: (id: string) => api.patch<ApiResponse>(`/collaboration/announcements/${id}/read`),
  meetingRooms: () => api.get<ApiResponse<{ id: string; roomName: string; location?: string; capacity: number; isActive: boolean; }[]>>('/collaboration/meeting-rooms'),
  createMeetingRoom: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/meeting-rooms', data),
  reservations: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/collaboration/meeting-reservations', { params }),
  createReservation: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/meeting-reservations', data),
  tasks: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; taskName: string; urgency: string; status: string; ownerDeptName?: string; expectedCompleteDate?: string; }[]>>('/collaboration/tasks', { params }),
  createTask: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/tasks', data),
  updateTaskStatus: (id: string, status: string) => api.patch<ApiResponse>(`/collaboration/tasks/${id}/status`, { status }),
  addTaskReply: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/collaboration/tasks/${id}/replies`, data),
};

export const calendarApi = {
  events: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; title: string; description?: string; eventType: string; categoryCode?: string; categoryName?: string; categoryColor?: string; location?: string; startDate: string; endDate: string; isAllDay: boolean; recurrenceRule?: string; recurrenceParentId?: string; participants?: string; organizerId?: string; departmentId?: string; visibility: string; sourceModule?: string; sourceId?: string; meetingRoomId?: string; meetingRoomName?: string; projectId?: string; color?: string; isPinned: boolean; reminders?: string; status: string; syncToOutlook: boolean; createdAt: string; updatedAt?: string; }[]>>('/calendar/events', { params }),
  range: (startDate: string, endDate: string) => api.get<ApiResponse<{ id: string; title: string; description?: string; eventType: string; categoryCode?: string; categoryName?: string; categoryColor?: string; location?: string; startDate: string; endDate: string; isAllDay: boolean; recurrenceRule?: string; recurrenceParentId?: string; participants?: string; organizerId?: string; departmentId?: string; visibility: string; sourceModule?: string; sourceId?: string; meetingRoomId?: string; meetingRoomName?: string; projectId?: string; color?: string; isPinned: boolean; reminders?: string; status: string; syncToOutlook: boolean; createdAt: string; updatedAt?: string; }[]>>('/calendar/range', { params: { startDate, endDate } }),
  getEvent: (id: string) => api.get<ApiResponse<{ id: string; title: string; description?: string; eventType: string; categoryCode?: string; categoryName?: string; categoryColor?: string; location?: string; startDate: string; endDate: string; isAllDay: boolean; recurrenceRule?: string; recurrenceParentId?: string; participants?: string; organizerId?: string; departmentId?: string; visibility: string; sourceModule?: string; sourceId?: string; meetingRoomId?: string; meetingRoomName?: string; projectId?: string; color?: string; isPinned: boolean; reminders?: string; status: string; syncToOutlook: boolean; createdAt: string; updatedAt?: string; }>>(`/calendar/events/${id}`),
  createEvent: (data: Record<string, unknown>) => api.post<ApiResponse<{ id: string; title: string; description?: string; eventType: string; categoryCode?: string; categoryName?: string; categoryColor?: string; location?: string; startDate: string; endDate: string; isAllDay: boolean; recurrenceRule?: string; recurrenceParentId?: string; participants?: string; organizerId?: string; departmentId?: string; visibility: string; sourceModule?: string; sourceId?: string; meetingRoomId?: string; meetingRoomName?: string; projectId?: string; color?: string; isPinned: boolean; reminders?: string; status: string; syncToOutlook: boolean; createdAt: string; updatedAt?: string; }>>('/calendar/events', data),
  updateEvent: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/calendar/events/${id}`, data),
  deleteEvent: (id: string) => api.delete<ApiResponse>(`/calendar/events/${id}`),
  updateEventStatus: (id: string, status: string) => api.patch<ApiResponse>(`/calendar/events/${id}/status`, status),
  conflicts: (params: Record<string, string>) => api.get<ApiResponse<{ eventId: string; title: string; startDate: string; endDate: string; conflictType: string; detail?: string; }[]>>('/calendar/conflicts', { params }),
  exportIcs: (startDate: string, endDate: string) => `http://localhost:5100/api/erp/calendar/export/ics?startDate=${startDate}&endDate=${endDate}`,
  categories: () => api.get<ApiResponse<{ id: string; code: string; name: string; color: string; icon?: string; sortOrder: number; isActive: boolean; }[]>>('/calendar/categories'),
  createCategory: (data: Record<string, unknown>) => api.post<ApiResponse>('/calendar/categories', data),
  updateCategory: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/calendar/categories/${id}`, data),
  deleteCategory: (id: string) => api.delete<ApiResponse>(`/calendar/categories/${id}`),
  holidays: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; name: string; date: string; holidayType: string; isRecurring: boolean; description?: string; }[]>>('/calendar/holidays', { params }),
  createHoliday: (data: Record<string, unknown>) => api.post<ApiResponse>('/calendar/holidays', data),
  updateHoliday: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/calendar/holidays/${id}`, data),
  deleteHoliday: (id: string) => api.delete<ApiResponse>(`/calendar/holidays/${id}`),
  shares: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; calendarOwnerId: string; sharedWithEmployeeId?: string; sharedWithDeptId?: string; sharedWithRoleId?: string; permission: string; canSeeDetails: boolean; }[]>>('/calendar/shares', { params }),
  createShare: (data: Record<string, unknown>) => api.post<ApiResponse>('/calendar/shares', data),
  deleteShare: (id: string) => api.delete<ApiResponse>(`/calendar/shares/${id}`),
};

export const uomsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; uomCode: string; uomName: string; uomNameEn?: string; uomType: string; decimalPlaces: number; isActive: boolean; allowDecimal: boolean; description?: string; displayOrder: number; createdAt: string; }[]>>('/uoms', { params }),
  get: (id: string) => api.get<ApiResponse<{ id: string; uomCode: string; uomName: string; uomNameEn?: string; uomType: string; decimalPlaces: number; isActive: boolean; allowDecimal: boolean; description?: string; displayOrder: number; createdAt: string; }>>(`/uoms/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/uoms', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/uoms/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/uoms/${id}`),
  getConversions: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; skuId?: string; skuCode?: string; skuName?: string; fromUomId: string; fromUomCode: string; fromUomName: string; toUomId: string; toUomCode: string; toUomName: string; factor: number; operator: string; inverseFactor?: number; isActive: boolean; description?: string; isGlobal: boolean; }[]>>('/uoms/conversions', { params }),
  createConversion: (data: Record<string, unknown>) => api.post<ApiResponse>('/uoms/conversions', data),
  updateConversion: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/uoms/conversions/${id}`, data),
  deleteConversion: (id: string) => api.delete<ApiResponse>(`/uoms/conversions/${id}`),
  convert: (params: { skuId: string; qty: number; fromUom: string; toUom: string }) => api.get<ApiResponse<{ originalQty: number; originalUom: string; convertedQty: number; targetUom: string; factor: number; operator: string; decimalPlaces: number; formula: string; }>>('/uoms/convert', { params }),
  toBase: (params: { skuId: string; qty: number; uom: string }) => api.get<ApiResponse<{ qty: number; uom: string; baseQty: number }>>('/uoms/to-base', { params }),
};

export const agentApi = {
  metricsSummary: () => api.get<ApiResponse<{ totalRequests: number; successRate: number; avgLatencyMs: number; p95LatencyMs: number; errorRate: number; tokenUsage24h: number; activeAgents: number; healthScores: Array<{ agentType: string; score: number; status: 'healthy' | 'degraded' | 'critical'; uptime: number; lastCheck: string; }> }>>('/agent/metrics-summary'),
  latencyHistory: (params?: { hours?: number }) => api.get<ApiResponse<Array<{ timestamp: string; p50: number; p95: number; p99: number }>>>('/agent/latency-history', { params }),
  qpsHistory: (params?: { hours?: number }) => api.get<ApiResponse<Array<{ timestamp: string; qps: number }>>>('/agent/qps-history', { params }),
  errorHistory: (params?: { hours?: number }) => api.get<ApiResponse<Array<{ timestamp: string; count: number; rate: number }>>>('/agent/error-history', { params }),
  flowExecutions: (params?: { flowType?: string; status?: string }) => api.get<ApiResponse<{ id: string; flowType: 'anomaly_dispatch' | 'customer_inquiry' | 'inventory_replenishment'; flowName: string; status: 'running' | 'success' | 'failed'; triggerSource: string; startTime: string; endTime?: string; duration?: number; steps: Array<{ id: string; name: string; status: 'pending' | 'running' | 'success' | 'failed' | 'skipped'; startTime?: string; endTime?: string; duration?: number; output?: string; error?: string; }>; businessImpact?: string; }[]>>('/agent/flow-executions', { params }),
  triggerFlow: (flowType: string, data: Record<string, unknown>) => api.post<ApiResponse<{ id: string; flowType: string; flowName: string; status: string; triggerSource: string; startTime: string; endTime?: string; duration?: number; steps: Array<{ id: string; name: string; status: string; startTime?: string; endTime?: string; duration?: number; output?: string; error?: string; }>; businessImpact?: string; }>>(`/agent/flows/${flowType}/trigger`, data),
  topology: () => api.get<ApiResponse<Array<{ id: string; name: string; type: string; status: string; connections: string[] }>>>('/agent/topology'),
};

export const substituteApi = {
  groups: (params?: { includeItems?: boolean }) => api.get<ApiResponse<{ id: string; groupCode: string; groupName?: string; description?: string; substitutionType: string; allowAutoSubstitute: boolean; isBidirectional: boolean; isActive: boolean; createdAt: string; updatedAt: string; items: { id: string; groupId: string; groupCode?: string; skuId: string; skuNumber?: string; skuName?: string; priority: number; conversionRate?: number; isPrimary: boolean; status: string; effectiveFrom?: string; effectiveTo?: string; notes?: string; }[] }[]>>('/substitute/groups', { params }),
  getGroup: (id: string) => api.get<ApiResponse<{ id: string; groupCode: string; groupName?: string; description?: string; substitutionType: string; allowAutoSubstitute: boolean; isBidirectional: boolean; isActive: boolean; createdAt: string; updatedAt: string; items: { id: string; groupId: string; groupCode?: string; skuId: string; skuNumber?: string; skuName?: string; priority: number; conversionRate?: number; isPrimary: boolean; status: string; effectiveFrom?: string; effectiveTo?: string; notes?: string; }[] }>>(`/substitute/groups/${id}`),
  createGroup: (data: Record<string, unknown>) => api.post<ApiResponse>('/substitute/groups', data),
  updateGroup: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/substitute/groups/${id}`, data),
  deleteGroup: (id: string) => api.delete<ApiResponse>(`/substitute/groups/${id}`),
  availableSubstitutes: (skuId: string, params?: { requiredQty?: number; requiredDate?: string }) => api.get<ApiResponse<{ id: string; groupId: string; groupCode?: string; skuId: string; skuNumber?: string; skuName?: string; priority: number; conversionRate?: number; isPrimary: boolean; status: string; effectiveFrom?: string; effectiveTo?: string; notes?: string; }[]>>(`/substitute/available/${skuId}`, { params }),
  useSubstitute: (data: { originalSkuId: string; substituteSkuId: string; workOrderId: string; qty: number; reason?: string }) => api.post<ApiResponse<{ message: string; costVariance: number }>>('/substitute/use', data),
  history: (skuId: string, params?: { fromDate?: string }) => api.get<ApiResponse<{ id: string; originalSkuId: string; originalSkuNumber?: string; originalSkuName?: string; substituteSkuId: string; substituteSkuNumber?: string; substituteSkuName?: string; workOrderId: string; woNumber?: string; originalQty: number; substituteQty: number; costVariance: number; reason: string; notes?: string; createdAt: string; }[]>>(`/substitute/history/${skuId}`, { params }),
};

export const barcodeApi = {
  lookup: (barcode: string) => api.get<ApiResponse<{ id: string; barcode: string; barcodeType: string; refId?: string; refType?: string; refName?: string; locationCode?: string; qty?: number; status: string; lastScanTime: string; }>>(`/barcode/lookup/${barcode}`),
  scan: (data: { barcode: string; barcodeType?: string; action?: string; qty?: number; locationCode?: string; details?: string }) => api.post<ApiResponse<{ id: string; barcode: string; barcodeType: string; refId?: string; refType?: string; refName?: string; locationCode?: string; qty?: number; status: string; lastScanTime: string; }>>('/barcode/scan', data),
  records: (params?: { type?: string; status?: string }) => api.get<ApiResponse<{ id: string; barcode: string; barcodeType: string; refId?: string; refType?: string; refName?: string; locationCode?: string; qty?: number; status: string; lastScanTime: string; }[]>>('/barcode/records', { params }),
  generate: (data: { prefix?: string; barcodeType?: string; refId?: string; refType?: string; refName?: string; locationCode?: string; qty?: number }) => api.post<ApiResponse<{ barcode: string; type: string }>>('/barcode/generate', data),
  generateBatch: (data: { barcodeType: string; refId?: string; refType?: string; refName?: string; quantity: number; qtyPerBarcode?: number }) => api.post<ApiResponse<{ generated: number; barcodes: string[] }>>('/barcode/generate/batch', data),
  history: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<{ data: { id: string; barcode: string; barcodeType: string; action: string; result: string; operatorName?: string; details?: string; timestamp: string; }[]; total: number; page: number; pageSize: number; totalPages: number; }>>('/barcode/history', { params }),
  historyByBarcode: (barcode: string) => api.get<ApiResponse<{ id: string; barcode: string; barcodeType: string; action: string; result: string; operatorName?: string; details?: string; timestamp: string; }[]>>(`/barcode/history/${barcode}`),
};

export const equipmentApi = {
  list: (params?: { status?: string; type?: string }) => api.get<ApiResponse<{ id: string; equipmentCode: string; equipmentName: string; equipmentType: string; serialNumber?: string; model?: string; manufacturer?: string; purchaseDate?: string; purchasePrice: number; status: string; maintenanceStatus: string; lastMaintenanceDate?: string; nextMaintenanceDate?: string; workCenterName?: string; }[]>>('/equipment/equipment', { params }),
  get: (id: string) => api.get<ApiResponse<{ id: string; equipmentCode: string; equipmentName: string; equipmentType: string; serialNumber?: string; model?: string; manufacturer?: string; purchaseDate?: string; purchasePrice: number; status: string; maintenanceStatus: string; lastMaintenanceDate?: string; nextMaintenanceDate?: string; workCenterName?: string; }>>(`/equipment/equipment/${id}`),
  create: (data: { equipmentCode: string; equipmentName: string; equipmentType?: string; workCenterId?: string; serialNumber?: string; model?: string; manufacturer?: string; supplierId?: string; purchaseDate?: string; purchasePrice?: number }) => api.post<ApiResponse<{ id: string; equipmentCode: string }>>('/equipment/equipment', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/equipment/equipment/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/equipment/equipment/${id}`),
  maintenancePlans: (params?: { equipmentId?: string }) => api.get<ApiResponse>('/equipment/maintenance/plans', { params }),
  createMaintenancePlan: (data: { equipmentId: string; maintenanceType?: string; frequency?: string; frequencyDays?: number; description?: string; checkpointList?: string; nextDueDate?: string }) => api.post<ApiResponse>('/equipment/maintenance/plans', data),
  maintenanceRecords: (params?: { equipmentId?: string; type?: string }) => api.get<ApiResponse<{ id: string; recordNumber: string; equipmentId: string; equipmentName: string; equipmentCode: string; maintenanceType: string; startTime: string; endTime?: string; performedBy?: string; workDescription?: string; partsReplaced?: string; laborHours: number; partsCost: number; totalCost: number; result: string; remarks?: string; }[]>>('/equipment/maintenance/records', { params }),
  createMaintenanceRecord: (data: { equipmentId: string; maintenancePlanId?: string; maintenanceType?: string; startTime: string; endTime?: string; performedBy?: string; workDescription?: string; partsReplaced?: string; laborHours: number; partsCost?: number; result?: string; remarks?: string }) => api.post<ApiResponse<{ id: string; recordNumber: string }>>('/equipment/maintenance/records', data),
  downtime: (params?: { equipmentId?: string; status?: string }) => api.get<ApiResponse<{ id: string; downtimeNumber: string; equipmentId: string; equipmentName: string; equipmentCode: string; downtimeType: string; startTime: string; endTime?: string; cause?: string; description?: string; status: string; resolution?: string; }[]>>('/equipment/downtime', { params }),
  createDowntime: (data: { equipmentId: string; workOrderId?: string; startTime: string; downtimeType?: string; cause?: string; description?: string; reportedBy?: string }) => api.post<ApiResponse<{ id: string; downtimeNumber: string }>>('/equipment/downtime', data),
  resolveDowntime: (id: string, data: { endTime?: string; assignedTo?: string; resolution?: string }) => api.put<ApiResponse>(`/equipment/downtime/${id}/resolve`, data),
  stats: () => api.get<ApiResponse<{ total: number; active: number; maintenance: number; repair: number; openDowntime: number; warningMaintenance: number; criticalMaintenance: number; maintenanceCost: number; }>>('/equipment/stats'),
};

export const hrApi = {
  attendance: {
    list: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; departmentId?: string; productionLine?: string; date: string; shiftType?: string; scheduledStartTime?: string; scheduledEndTime?: string; clockIn?: string; clockOut?: string; clockInLocation?: string; clockOutLocation?: string; status: string; lateFlag: boolean; lateMinutes: number; earlyLeaveFlag: boolean; earlyLeaveMinutes: number; clockingAnomalyFlag: boolean; anomalyType?: string; ot1Hours: number; ot2Hours: number; ot3Hours: number; otReason?: string; approvalStatus: string; notes?: string; }[]>>('/Attendance', { params }),
    get: (id: string) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; departmentId?: string; productionLine?: string; date: string; shiftType?: string; scheduledStartTime?: string; scheduledEndTime?: string; clockIn?: string; clockOut?: string; clockInLocation?: string; clockOutLocation?: string; status: string; lateFlag: boolean; lateMinutes: number; earlyLeaveFlag: boolean; earlyLeaveMinutes: number; clockingAnomalyFlag: boolean; anomalyType?: string; ot1Hours: number; ot2Hours: number; ot3Hours: number; otReason?: string; approvalStatus: string; notes?: string; }>>(`/Attendance/${id}`),
    create: (data: Record<string, unknown>) => api.post<ApiResponse>('/Attendance', data),
    update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/Attendance/${id}`, data),
    approve: (id: string, status: string, notes?: string) => api.patch<ApiResponse<boolean>>(`/Attendance/${id}/approve`, { status, notes }),
    delete: (id: string) => api.delete<ApiResponse>(`/Attendance/${id}`),
  },
  leaveRequests: {
    list: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; applicationNo: string; leaveType: string; applicationDate: string; startDate: string; endDate: string; startTime?: string; endTime?: string; totalHours: number; reason?: string; attachmentUrl?: string; includesSpecialPeriod: boolean; shiftConflictFlag: boolean; autoDeductPayFlag: boolean; impactOtOrBonus: boolean; approvalLevel: number; status: string; approverId?: string; approvedAt?: string; rejectReason?: string; remarks?: string; createdAt: string; }[]>>('/leave-requests', { params }),
    get: (id: string) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; applicationNo: string; leaveType: string; applicationDate: string; startDate: string; endDate: string; startTime?: string; endTime?: string; totalHours: number; reason?: string; attachmentUrl?: string; includesSpecialPeriod: boolean; shiftConflictFlag: boolean; autoDeductPayFlag: boolean; impactOtOrBonus: boolean; approvalLevel: number; status: string; approverId?: string; approvedAt?: string; rejectReason?: string; remarks?: string; createdAt: string; }>>(`/leave-requests/${id}`),
    create: (data: Record<string, unknown>) => api.post<ApiResponse>('/leave-requests', data),
    update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/leave-requests/${id}`, data),
    approve: (id: string, status: string, approverId?: string, rejectReason?: string) => api.patch<ApiResponse<boolean>>(`/leave-requests/${id}/approve`, { status, approverId, rejectReason }),
    cancel: (id: string, reason?: string) => api.patch<ApiResponse<boolean>>(`/leave-requests/${id}/cancel`, { reason }),
    balances: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; leaveType: string; year: number; totalEntitled: number; used: number; remaining: number; }[]>>('/leave-requests/balances', { params }),
  },
  payroll: {
    list: (params?: Record<string, string>) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; periodStart: string; periodEnd: string; basicSalary: number; insuranceBase: number; pensionBase: number; newLeavingFlag: boolean; scheduledDays: number; actualWorkingDays: number; totalLateMinutes: number; totalEarlyMinutes: number; anomalyDays: number; totalOt1Hours: number; totalOt2Hours: number; totalOt3Hours: number; ot1Pay: number; ot2Pay: number; ot3Pay: number; totalLeaveHours: number; annualLeaveHours: number; personalLeaveHours: number; sickLeaveHours: number; fullAttendanceFlag: boolean; lateDeduction: number; earlyDeduction: number; anomalyPenalty: number; personalLeaveDeduction: number; sickLeaveDeduction: number; otherDeductions: number; overtimePay: number; bonus: number; attendanceBonus: number; laborInsuranceDeduction: number; healthInsuranceDeduction: number; pensionDeduction: number; withholdingTax: number; employerLi: number; employerHi: number; employerPf: number; employmentInsurance: number; supplementalHealthInsurance?: number; grossPay: number; totalDeductions: number; employerContributions: number; netPay: number; calculatedBy?: string; calculatedAt?: string; status: string; paymentDate?: string; paymentBankAccount?: string; payrollSlipNo?: string; notes?: string; createdAt: string; updatedAt: string; }[]>>('/Payroll', { params }),
    get: (id: string) => api.get<ApiResponse<{ id: string; employeeId: string; employeeNo?: string; employeeName?: string; }>>(`/Payroll/${id}`),
    create: (data: Record<string, unknown>) => api.post<ApiResponse>('/Payroll', data),
    update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/Payroll/${id}`, data),
    updateStatus: (id: string, status: string, paymentDate?: string, paymentBankAccount?: string, payrollSlipNo?: string) => api.patch<ApiResponse<boolean>>(`/Payroll/${id}/status`, { status, paymentDate, paymentBankAccount, payrollSlipNo }),
    delete: (id: string) => api.delete<ApiResponse>(`/Payroll/${id}`),
    policies: {
      list: () => api.get<ApiResponse<PayrollPolicyVersionDto[]>>('/Payroll/policies'),
      pendingFinanceReview: () => api.get<ApiResponse<PayrollPolicyVersionDto[]>>('/Payroll/policies/pending-finance-review'),
      get: (id: string) => api.get<ApiResponse<PayrollPolicyVersionDto>>(`/Payroll/policies/${id}`),
      create: (data: SavePayrollPolicyRequest) => api.post<ApiResponse<PayrollPolicyVersionDto>>('/Payroll/policies', data),
      update: (id: string, data: SavePayrollPolicyRequest) => api.put<ApiResponse<PayrollPolicyVersionDto>>(`/Payroll/policies/${id}`, data),
      submit: (id: string, confirmedOfficialSources: boolean) => api.post<ApiResponse<PayrollPolicyVersionDto>>(
        `/Payroll/policies/${id}/submit`,
        { confirmedOfficialSources },
      ),
      approve: (id: string, comment: string, confirmedOfficialSources: boolean) => api.post<ApiResponse<PayrollPolicyVersionDto>>(
        `/Payroll/policies/${id}/approve`,
        { comment, confirmedOfficialSources },
      ),
      reject: (id: string, comment: string, confirmedOfficialSources: boolean) => api.post<ApiResponse<PayrollPolicyVersionDto>>(
        `/Payroll/policies/${id}/reject`,
        { comment, confirmedOfficialSources },
      ),
    },
  },
};
