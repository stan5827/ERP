import axios, { type AxiosInstance } from 'axios';

// Error handling utility
const handleApiError = (error: any) => {
  // Handle network errors
  if (!error.response) {
    throw new Error('網路連線錯誤，請檢查您的網路連接');
  }

  // Handle authentication errors
  if (error.response.status === 401) {
    localStorage.removeItem('erp_token');
    localStorage.removeItem('erp_user');
    window.location.reload();
    throw new Error('登入已過期，請重新登入');
  }

  // Handle validation errors (400)
  if (error.response.status === 400) {
    const message = error.response.data?.message || '輸入資料驗證失敗';
    const err = new Error(message) as any;
    err.response = error.response; // 傳遞原始回應以獲取詳細 errors
    throw err;
  }

  // Handle forbidden errors (403)
  if (error.response.status === 403) {
    throw new Error('您沒有權限執行此操作');
  }

  // Handle not found errors (404)
  if (error.response.status === 404) {
    throw new Error('請求的資源不存在');
  }

  // Handle server errors (5xx)
  if (error.response.status >= 500) {
    const serverMsg = error.response.data?.message;
    console.error('[API] Server Error 500:', {
      url: error.config?.url,
      method: error.config?.method,
      data: error.response.data
    });
    throw new Error(serverMsg || '伺服器內部錯誤，請稍後再試');
  }

  // Handle other errors
  throw new Error(error.response.data?.message || '發生未知錯誤');
};

const ERP_API_BASE = import.meta.env.VITE_API_BASE_URL || '/api/erp';
const AUTH_API_BASE = import.meta.env.VITE_AUTH_API_BASE_URL || '/api';
const AI_API_BASE = import.meta.env.VITE_AI_API_BASE_URL || '/api/ai';

const attachInterceptors = (client: AxiosInstance) => {
  client.interceptors.request.use((config) => {
    const token = localStorage.getItem('erp_token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  });

  client.interceptors.response.use(
    (res) => res,
    (err) => Promise.reject(handleApiError(err))
  );
};

export const api = axios.create({
  baseURL: ERP_API_BASE,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});
attachInterceptors(api);

const authClient = axios.create({
  baseURL: AUTH_API_BASE,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});
attachInterceptors(authClient);

export const aiApi = axios.create({
  baseURL: AI_API_BASE,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});
attachInterceptors(aiApi);

const API_BASE = ERP_API_BASE;

// ========== Response Types ==========
export interface ApiResponse<T = unknown> {
  success: boolean;
  message?: string;
  data?: T;
  totalCount?: number;
  page?: number;
  pageSize?: number;
}

// ========== Auth ==========
export interface LoginRequest { username: string; password: string; }
export interface LoginResponse {
  userId: string; username: string; email?: string;
  token: string; expiresAt: string; roles: string[]; permissions: string[];
}

export const authApi = {
  login: (req: LoginRequest) => authClient.post<ApiResponse<LoginResponse>>('/auth/login', req),
  register: (req: { username: string; password: string; email?: string; roleIds: string[] }) =>
    authClient.post<ApiResponse>('/auth/register', req),
};

// ========== Common ==========
export const commonApi = {
  getNotifications: (params?: { role?: string; userId?: string }) => 
    api.get<ApiResponse<any[]>>('/notifications', { params }),
};

// ========== System ==========
export interface UserDto { id: string; username: string; email?: string; employeeId?: string; isActive: boolean; lastLoginAt?: string; createdAt: string; roles: string[]; }
export interface RoleDto { id: string; roleName: string; description?: string; isSystem: boolean; permissions: string[]; }

export const usersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<UserDto[]>>('/users', { params }),
  get: (id: string) => api.get<ApiResponse<UserDto>>(`/users/${id}`),
  create: (data: { username: string; password: string; email?: string; roleIds: string[] }) => api.post<ApiResponse>('/users', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/users/${id}`, data),
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

// ========== System Settings ==========
export interface PermissionTreeDto {
  id: string;
  permissionCode: string;
  moduleName: string;
  actionType: string;
  permissionType: string;
  description?: string;
  sortOrder: number;
  children: PermissionTreeDto[];
}

export interface UserWithRolesDto {
  id: string;
  username: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
  roles: RoleDto[];
}

export interface RoleWithPermissionsDto {
  id: string;
  roleName: string;
  description?: string;
  isSystem: boolean;
  createdAt: string;
  permissionIds: string[];
}

export interface ThemeDto {
  name: string;
  displayName: string;
  description?: string;
  isDefault: boolean;
  colors?: ThemeColorsDto;
}

export interface ThemeColorsDto {
  primary: string;
  accent: string;
  background: string;
  surface: string;
  text: string;
  border: string;
}

export interface UserThemePreferenceDto {
  themeName: string;
  customPrimaryColor?: string;
  customAccentColor?: string;
}

export interface ModuleDto {
  id: string;
  name: string;
  icon: string;
  description?: string;
  sortOrder: number;
}

export interface ModuleAccess {
  moduleId: string;
  permissions: string[];
}

export const systemSettingsApi = {
  getPermissionTree: () => api.get<ApiResponse<PermissionTreeDto[]>>('/system-settings/permissions/tree'),
  createPermission: (data: { permissionCode: string; moduleName: string; actionType: string; permissionType: string; description?: string; parentId?: string; sortOrder: number }) =>
    api.post<ApiResponse>('/system-settings/permissions', data),
  updatePermission: (id: string, data: { permissionCode: string; moduleName: string; actionType: string; permissionType: string; description?: string; parentId?: string; sortOrder: number }) =>
    api.put<ApiResponse>(`/system-settings/permissions/${id}`, data),
  deletePermission: (id: string) => api.delete<ApiResponse>(`/system-settings/permissions/${id}`),
  getUsers: () => api.get<ApiResponse<UserWithRolesDto[]>>('/system-settings/users'),
  updateUserRoles: (userId: string, roleIds: string[]) => 
    api.put<ApiResponse>(`/system-settings/users/${userId}/roles`, { roleIds }),
  getRoles: () => api.get<ApiResponse<RoleWithPermissionsDto[]>>('/system-settings/roles'),
  updateRolePermissions: (roleId: string, permissionIds: string[]) =>
    api.put<ApiResponse>(`/system-settings/roles/${roleId}/permissions`, { permissionIds }),
  createRole: (data: { roleName: string; description?: string }) =>
    api.post<ApiResponse>('/system-settings/roles', data),
  deleteRole: (roleId: string) => api.delete<ApiResponse>(`/system-settings/roles/${roleId}`),
  getThemes: () => api.get<ApiResponse<ThemeDto[]>>('/system-settings/themes'),
  getUserTheme: (userId: string) => api.get<ApiResponse<UserThemePreferenceDto>>(`/system-settings/users/${userId}/theme`),
  updateUserTheme: (userId: string, data: { themeName: string; customPrimaryColor?: string; customAccentColor?: string }) =>
    api.put<ApiResponse>(`/system-settings/users/${userId}/theme`, data),
  getModules: () => api.get<ApiResponse<ModuleDto[]>>('/system-settings/modules'),
  getRoleModules: (roleName: string) => api.get<ApiResponse<ModuleAccess[]>>(`/system-settings/role-modules/${roleName}`),
};

// ========== HR ==========
export interface DepartmentDto { id: string; deptCode: string; deptName: string; parentDeptId?: string; costCenter?: string; isActive: boolean; parentDeptName?: string; employeeCount: number; }
export interface EmployeeDto {
  id: string; employeeNo: string; chineseName: string; englishName?: string; nationalId?: string;
  birthDate?: string; gender?: string; phone?: string; mobile?: string; email?: string;
  registeredAddress?: string; mailingAddress?: string;
  emergencyContactName?: string; emergencyContactPhone?: string; emergencyContactRelation?: string;
  departmentId?: string; departmentName?: string;
  jobTitleId?: string; jobTitleName?: string; gradeId?: string; gradeName?: string;
  positionTitle?: string; jobGrade?: string; jobCategory?: string;
  supervisorId?: string; supervisorName?: string; hireDate?: string; probationEndDate?: string;
  status: string; salaryType?: string;
  baseSalary?: number; foodAllowance?: number; positionAllowance?: number; fullAttendanceBonus?: number;
  shiftType?: string; createdAt?: string;
}

export const departmentsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<DepartmentDto[]>>('/departments', { params }),
  flat: (includeInactive = false) => api.get<ApiResponse<DepartmentDto[]>>('/departments/flat', { params: includeInactive ? { includeInactive: 'true' } : undefined }),
  tree: (includeInactive = false) => api.get<ApiResponse<DepartmentDto[]>>('/departments/tree', { params: includeInactive ? { includeInactive: 'true' } : undefined }),
  get: (id: string) => api.get<ApiResponse<DepartmentDto>>(`/departments/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/departments', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/departments/${id}`, toPascalCase(data)),
  delete: (id: string) => api.delete<ApiResponse>(`/departments/${id}`),
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

export const approvalsApi = {
  authorities: () => api.get<ApiResponse<unknown[]>>('/approvals/authorities'),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/approvals/authorities', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/approvals/authorities/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/approvals/authorities/${id}`),
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

export const employeesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<EmployeeDto[]>>('/employees', { params }),
  get: (id: string) => api.get<ApiResponse<EmployeeDto>>(`/employees/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/employees', toPascalCase(data)),
  update: async (id: string, data: Record<string, unknown>) => {
    console.log('API update 發送資料:', JSON.stringify(data));
    const pascalData = toPascalCase(data);
    console.log('API update 轉PascalCase後:', JSON.stringify(pascalData));
    const response = await api.put<ApiResponse>(`/employees/${id}`, pascalData);
    console.log('API update 回應:', JSON.stringify(response));
    return response;
  },
  terminate: (id: string, reason: string) => api.patch<ApiResponse>(`/employees/${id}/terminate`, { reason }),
  rehire: (id: string) => api.patch<ApiResponse>(`/employees/${id}/rehire`),
};

// ========== Inventory ==========
export interface SkuDto {
  id: string; skuNumber: string; skuName: string; specification?: string; barcode?: string;
  unit: string; lotControlled: boolean; serialControlled: boolean; sourceType?: string;
  safetyStock: number; reorderPoint: number; maxStock: number; leadTimeDays: number;
  purchasePrice: number; standardCost: number; status: string; qty: number;
  abcClassification?: string; defaultWarehouseId?: string; defaultWarehouseName?: string;
  createdAt: string;

  category?: string;
  itemType: string;
  model?: string;
  brand?: string;
  isSellable: boolean;
  isPurchasable: boolean;
  isManufacturable: boolean;
  isOutsourced: boolean;
  costMethod: string;
  suggestedPrice: number;
  basePrice: number;
  priceA?: number;
  priceB?: number;
  priceC?: number;
  priceD?: number;
  priceE?: number;
  currency: string;
  isBomTop: boolean;
  isBomComponent: boolean;
  isPhantom: boolean;
  isMto: boolean;
  isMts: boolean;
  isCustom: boolean;
  makeOrBuy: string;
  defaultSupplierId?: string;
  defaultSupplierName?: string;
  supplierItemCode?: string;
  supplierLeadTime?: number;
  drawingNo?: string;
  drawingRevision?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  qualitySpec?: string;
  productionDept?: string;
  machineCode?: string;
  moldCode?: string;

  costUnit?: string;
  latestPurchaseCost: number;
  taxCode?: string;
  taxRate?: string;
  isSubstitute: boolean;
  isAlternate: boolean;
  iqcRequired: boolean;
  oqcRequired: boolean;
  color?: string;
  packingMethod?: string;
  minPackingQty?: number;
  isStocked: boolean;
  materialType?: string;
  designOwner?: string;

  baseUomCode?: string;
  baseUomName?: string;
  baseUomId?: string;
  purchaseUomCode?: string;
  purchaseUomName?: string;
  purchaseUomId?: string;
  salesUomCode?: string;
  salesUomName?: string;
  salesUomId?: string;
  productionUomCode?: string;
  productionUomName?: string;
  productionUomId?: string;
  purchaseToBaseFactor?: number;
  salesToBaseFactor?: number;
  productionToBaseFactor?: number;
  inventoryQty?: number;
}
export interface WarehouseDto { id: string; warehouseCode: string; warehouseName: string; address?: string; type: string; isActive: boolean; binLocationCount: number; }
export interface WarehouseTypeDto { id: string; typeCode: string; typeName: string; description?: string; sortOrder?: number; isActive: boolean; }
export interface InventoryTransactionDto { 
  id: string; 
  transactionNumber?: string;
  skuId: string; 
  skuNumber?: string; 
  skuName?: string; 
  transactionType?: string;
  type?: string;
  quantity?: number;
  qtyChange?: number; 
  qtyBefore?: number; 
  qtyAfter?: number; 
  warehouseId?: string;
  warehouseName?: string;
  binLocationId?: string;
  binLocationCode?: string;
  lotNumber?: string; 
  transactionDate?: string;
  createdBy?: string;
  createdAt?: string; 
}

// ===== MES & BOM =====
export interface BomItemDto { id: string; childSkuId: string; childSkuNumber?: string; childSkuName?: string; quantity: number; unit: string; scrapRate: number; operationSeq: number; isPhantom: boolean; effectiveDate?: string; }
export interface BomDto { id: string; bomCode: string; productSkuId: string; productSkuNumber?: string; productSkuName?: string; version: string; status: string; effectiveDate: string; expiryDate?: string; notes?: string; items: BomItemDto[]; createdAt: string; updatedAt: string; }
export interface EcnDto { id: string; ecnNumber: string; changeDescription?: string; effectiveDate: string; status: string; createdAt: string; }
export interface EcrDto { id: string; ecrNumber: string; title: string; description?: string; changeReason?: string; priority: string; targetBomId?: string; status: string; createdAt: string; targetBomCode?: string; }

export function toPascalCase(obj: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const key in obj) {
    if (!Object.prototype.hasOwnProperty.call(obj, key)) continue;
    const pascal = key.charAt(0).toUpperCase() + key.slice(1);
    let value = obj[key];
    if (value !== null && value !== undefined && typeof value === 'object' && !Array.isArray(value)) {
      result[pascal] = toPascalCase(value as Record<string, unknown>);
    } else if (Array.isArray(value)) {
      result[pascal] = value.map(item => 
        (item && typeof item === 'object') ? toPascalCase(item as Record<string, unknown>) : item
      );
    } else {
      result[pascal] = value;
    }
  }
  return result;
  return result;
}

export const skusApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<SkuDto[]>>('/skus', { params }),
  get: (id: string) => api.get<ApiResponse<SkuDto>>(`/skus/${id}`),
  create: async (data: Record<string, unknown>) => {
    console.log('[DEBUG] 發送資料到 API (原始):', JSON.stringify(data, null, 2));
    const pascalData = toPascalCase(data);
    console.log('[DEBUG] 發送資料到 API (轉換後):', JSON.stringify(pascalData, null, 2));
    try {
      const response = await api.post<ApiResponse>('/skus', pascalData);
      console.log('[DEBUG] API 回應:', JSON.stringify(response.data, null, 2));
      if (!response.data?.success) {
        throw new Error(response.data?.message || '建立失敗');
      }
      return response.data;
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: unknown } };
        console.log('[DEBUG] API 錯誤回應:', JSON.stringify(axiosErr.response?.data, null, 2));
      }
      throw err;
    }
  },
  update: async (id: string, data: Record<string, unknown>) => {
    const response = await api.put<ApiResponse>(`/skus/${id}`, toPascalCase(data));
    if (!response.data?.success) {
      throw new Error(response.data?.message || '更新失敗');
    }
    return response.data;
  },
  all: () => api.get<ApiResponse<SkuDto[]>>('/skus?pageSize=9999'),
  allPurchasable: () => api.get<ApiResponse<SkuDto[]>>('/skus?pageSize=9999&purchasable=true'),
};

export const warehousesApi = {
  list: (params?: { includeInactive?: boolean }) => api.get<ApiResponse<WarehouseDto[]>>('/warehouses', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/warehouses', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/warehouses/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/warehouses/${id}`),
  activate: (id: string) => api.put<ApiResponse>(`/warehouses/${id}/activate`),
};

export const warehouseTypesApi = {
  list: () => api.get<ApiResponse<WarehouseTypeDto[]>>('/warehouse-types'),
  get: (id: string) => api.get<ApiResponse<WarehouseTypeDto>>(`/warehouse-types/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/warehouse-types', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/warehouse-types/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/warehouse-types/${id}`),
};

export const inventoryApi = {
  getSkuBalance: (sku_code: string) => api.get<ApiResponse<any>>(`/inventory/balance/${sku_code}`),
  checkATP: (sku_code: string, requested_qty: number) => 
    api.get<ApiResponse<any>>('/inventory/atp', { params: { sku_code, requested_qty } }),
  adjust: (data: Record<string, unknown>) => api.post<ApiResponse>('/inventory/adjust', data),
  transactions: (params?: Record<string, string>) => api.get<ApiResponse<InventoryTransactionDto[]>>('/inventory/transactions', { params }),
  lowStock: () => api.get<ApiResponse<SkuDto[]>>('/inventory/low-stock'),
  valuation: () => api.get<ApiResponse<{ totalValue: number }>>('/inventory/valuation'),
};

// ========== Inventory Transfers ==========
export interface TransferOrderDto {
  id: string;
  transferNumber: string;
  status: string;
  sourceWarehouseId: string;
  targetWarehouseId: string;
  approvedBy?: string;
  createdBy?: string;
  createdAt: string;
  items: TransferOrderItemDto[];
}

export interface TransferOrderItemDto {
  id: string;
  skuId: string;
  skuName?: string;
  skuNumber?: string;
  transferQty: number;
  lotNumber?: string;
}

export const inventoryTransfersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<TransferOrderDto[]>>('/InventoryTransfers', { params }),
  get: (id: string) => api.get<ApiResponse<TransferOrderDto>>(`/InventoryTransfers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<TransferOrderDto>>('/InventoryTransfers', data),
  approve: (id: string) => api.post<ApiResponse<TransferOrderDto>>(`/InventoryTransfers/${id}/approve`),
  execute: (id: string) => api.post<ApiResponse<TransferOrderDto>>(`/InventoryTransfers/${id}/execute`),
  cancel: (id: string) => api.delete<ApiResponse<boolean>>(`/InventoryTransfers/${id}`),
};

// ========== Completion Orders ==========
export interface CompletionOrderDto {
  id: string;
  completionNumber: string;
  workOrderId: string;
  warehouseId: string;
  status: string;
  qaStatus: string;
  receivedBy?: string;
  createdAt: string;
  items: CompletionOrderItemDto[];
}

export interface CompletionOrderItemDto {
  id: string;
  skuId: string;
  skuName?: string;
  skuNumber?: string;
  receivedQty: number;
  lotNumber?: string;
  manufactureDate?: string;
  expiryDate?: string;
}

export interface CompletableWorkOrderDto {
  id: string;
  woNumber: string;
  productSkuId: string;
  plannedQty: number;
  actualQty: number;
  plannedEndDate?: string;
  status: string;
}

export const completionOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<CompletionOrderDto[]>>('/completion-orders', { params }),
  get: (id: string) => api.get<ApiResponse<CompletionOrderDto>>(`/completion-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<CompletionOrderDto>>('/completion-orders', data),
  complete: (id: string) => api.post<ApiResponse<CompletionOrderDto>>(`/completion-orders/${id}/complete`),
  cancel: (id: string) => api.delete<ApiResponse<boolean>>(`/completion-orders/${id}`),
  getCompletableWorkOrders: () => api.get<ApiResponse<CompletableWorkOrderDto[]>>('/completion-orders/workorders/completable'),
};

// ========== Bin Locations ==========
export interface BinLocationDto {
  id: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  locationCode: string;
  zone?: string;
  aisle?: string;
  rack?: string;
  level?: string;
  position?: string;
  maxWeight?: number;
  gridX?: string;
  gridY?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export const binLocationsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<BinLocationDto[]>>('/bin-locations', { params }),
  get: (id: string) => api.get<ApiResponse<BinLocationDto>>(`/bin-locations/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/bin-locations', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/bin-locations/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/bin-locations/${id}`),
  activate: (id: string) => api.put<ApiResponse>(`/bin-locations/${id}/activate`),
  getByWarehouse: (warehouseId: string) => api.get<ApiResponse<BinLocationDto[]>>('/bin-locations/warehouse/' + warehouseId),
};

// ========== Sales ==========
export interface CustomerDto { id: string; customerCode: string; customerName: string; customerShortName?: string; taxId?: string; contactPerson?: string; contactTitle?: string; phone?: string; mobile?: string; email?: string; address?: string; fixedDiscount: number; creditLimit: number; creditStatus: string; companyType: string; fax?: string; paymentTerms?: string; taxCategory?: string; currency: string; priceLevel?: string; industry?: string; region?: string; salesTerritory?: string; approvalStatus: string; invoiceAddress?: string; leadSource?: string; churnRisk?: boolean; lastPurchaseDate?: string; satisfactionScore?: number; isActive: boolean; createdAt: string; }
export interface SalesOrderDto { id: string; orderNumber: string; customerId: string; customerName?: string; orderDate: string; paymentTerms?: string; tradeTerms?: string; currency: string; subtotal: number; taxAmount: number; totalAmount: number; expectedDeliveryDate?: string; customerDueDate?: string; confirmedDeliveryDate?: string; status: string; manufacturingStatus?: string; creditChecked: boolean; customerPoNumber?: string; salespersonId?: string; salespersonName?: string; departmentId?: string; departmentName?: string; salesChannel?: string; advanceDepositAmount?: number; orderType?: string; productionGroupCode?: string; hsCode?: string; freight?: number; shippingMethod?: string; incoterms?: string; orderRemarks?: string; createdAt: string; items: SalesOrderItemDto[]; }
export interface SalesOrderItemDto { id: string; salesOrderId: string; lineNumber: number; productId: string; productName?: string; skuId?: string; skuNumber?: string; customerItemCode?: string; quantity: number; unitPrice: number; discountRate: number; subtotal: number; deliveredQty: number; backorderQty: number; status: string; warehouseId?: string; warehouseName?: string; taxCode?: string; taxRate?: string; bomId?: string; bomVersion?: string; routingId?: string; workCenterId?: string; workCenterName?: string; needManufacturing: boolean; needOutsourcing: boolean; plannedStartDate?: string; plannedFinishDate?: string; lineRemarks?: string; reviewedBy?: string; reviewedAt?: string; productionStatus?: string; unit?: string; productSpec?: string; invoiceIssued?: boolean; }

export const customersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<CustomerDto[]>>('/customers', { params }),
  get: (id: string) => api.get<ApiResponse<CustomerDto>>(`/customers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/customers', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/customers/${id}`, data),
};

export const salesOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<any[]>>('/sales-orders', { params }),
  get: (id: string) => api.get<ApiResponse<any>>(`/sales-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/sales-orders', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/sales-orders/${id}`, data),
  confirm: (so_no: string) => api.post<ApiResponse>(`/sales-orders/${so_no}/confirm`),
  updateManufacturingStatus: (id: string, status: string) => api.patch<ApiResponse>(`/sales-orders/${id}/manufacturing-status`, { status }),
};

export const quotesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quotes', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/quotes/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/quotes', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/quotes/${id}`, toPascalCase(data)),
  convertToOrder: (id: string) => api.patch<ApiResponse<SalesOrderDto>>(`/quotes/${id}/convert-to-order`),
  approve: (id: string) => api.patch<ApiResponse>(`/quotes/${id}/approve`),
  reject: (id: string) => api.patch<ApiResponse>(`/quotes/${id}/reject`),
};

export const deliveryOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/delivery-orders', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/delivery-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<unknown>>('/delivery-orders', data),
  updateStatus: (id: string, status: string, notes?: string) =>
    api.patch<ApiResponse<unknown>>(`/delivery-orders/${id}/status`, { status, notes }),
  sign: (id: string, signedBy: string) =>
    api.patch<ApiResponse<unknown>>(`/delivery-orders/${id}/sign`, { signedBy }),
};

export const invoicesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/invoices', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/invoices/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<unknown>>('/invoices', data),
  approve: (id: string) => api.patch<ApiResponse<unknown>>(`/invoices/${id}/approve`),
  void: (id: string, voidReason: string) => api.patch<ApiResponse<unknown>>(`/invoices/${id}/void`, { voidReason }),
  aging: () => api.get<ApiResponse<unknown>>('/invoices/aging'),
  arSummary: () => api.get<ApiResponse<unknown>>('/invoices/ar-summary'),
};

export const cashReceiptsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/cash-receipts', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<unknown>>('/cash-receipts', data),
  pendingInvoices: (customerId: string) => api.get<ApiResponse<unknown[]>>(`/cash-receipts/pending-invoices/${customerId}`),
};

export const salesReportsApi = {
  orderToCash:    (params?: Record<string, string>) => api.get<ApiResponse<unknown>>('/SalesReports/order-to-cash', { params }),
  arAging:        () => api.get<ApiResponse<unknown>>('/SalesReports/ar-aging'),
  monthlySummary: (year?: number) => api.get<ApiResponse<unknown>>('/SalesReports/monthly-summary', { params: year ? { year: String(year) } : undefined }),
  creditUsage:    () => api.get<ApiResponse<unknown>>('/SalesReports/credit-usage'),
  periods:        (year?: number) => api.get<ApiResponse<unknown[]>>('/SalesReports/monthly-periods', { params: year ? { year: String(year) } : undefined }),
  closePeriod:    (data: { year: number; month: number; notes?: string }) => api.post<ApiResponse<unknown>>('/SalesReports/monthly-periods/close', data),
};

// ========== Procurement ==========
export interface SupplierDto { id: string; supplierCode: string; supplierName: string; taxId?: string; contactPerson?: string; phone?: string; email?: string; address?: string; paymentTerms?: string; currency: string; rating?: string; invoiceAddress?: string; deliveryAddress?: string; status: string; createdAt: string; }
export interface PurchaseOrderDto { id: string; poNumber: string; supplierId: string; supplierName?: string; poDate: string; currency: string; exchangeRate: number; taxCode?: string; paymentTerms?: string; tradeTerms?: string; subtotal: number; taxAmount: number; totalAmount: number; expectedDeliveryDate?: string; deliveryWarehouseId?: string; status: string; isLocked: boolean; createdAt: string; items: PurchaseOrderItemDto[]; }
export interface PurchaseOrderItemDto { id: string; skuId: string; skuNumber?: string; skuName?: string; quantity: number; unit?: string; unitPrice: number; subtotal: number; receivedQty: number; status: string; inventoryUnit?: string; conversionRate: number; inventoryQty: number; inventoryUnitPrice: number; inventorySubtotal: number; }
export interface PurchaseRequisitionDto { id: string; prNumber: string; prDate: string; departmentId?: string; departmentName?: string; requesterId?: string; requesterName?: string; purpose?: string; priority?: string; budgetAccount?: string; projectCode?: string; workOrderId?: string; status: string; isLocked: boolean; totalEstimatedAmount: number; createdAt: string; itemCount?: number; items?: PurchaseRequisitionItemDto[]; }
export interface PurchaseRequisitionItemDto { id: string; skuId: string; skuNumber?: string; skuName?: string; quantity: number; unit: string; estimatedUnitPrice: number; currency: string; requiredDate?: string; notes?: string; estimatedSubtotal: number; }
export interface SupplierAsnDto {
  id: string;
  asnNumber: string;
  supplierId?: string;
  supplierName?: string;
  purchaseOrderId?: string;
  purchaseOrderNumber?: string;
  expectedArrivalDate?: string;
  status: string;
  createdAt?: string;
  items?: Array<{
    id: string;
    skuId?: string;
    skuNumber?: string;
    skuName?: string;
    shippedQty: number;
    receivedQty?: number;
    batchNo?: string;
  }>;
}
export interface GoodsReceiptDto {
  id: string;
  grNumber: string;
  purchaseOrderId?: string;
  purchaseOrderNumber?: string;
  asnId?: string;
  asnNumber?: string;
  supplierId?: string;
  supplierName?: string;
  warehouseId?: string;
  warehouseName?: string;
  receiptDate?: string;
  grDate?: string;
  status: string;
  iqcStatus?: string;
  totalAmount?: number;
  supplierInvoiceNo?: string;
  packingListNo?: string;
  items?: Array<{
    id: string;
    skuId?: string;
    skuNumber?: string;
    skuName?: string;
    quantity?: number;
    receivedQty?: number;
    acceptedQty?: number;
    rejectedQty?: number;
    iqcResult?: string;
    lotNumber?: string;
  }>;
}
export const suppliersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<SupplierDto[]>>('/suppliers', { params }),
  get: (id: string) => api.get<ApiResponse<SupplierDto>>(`/suppliers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/suppliers', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/suppliers/${id}`, toPascalCase(data)),
  getPortalUsers: (supplierId: string) => api.get<ApiResponse<SupplierPortalUser[]>>(`/suppliers/${supplierId}/portal-users`),
  createPortalUser: (supplierId: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/suppliers/${supplierId}/portal-users`, toPascalCase(data)),
  updatePortalUser: (userId: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/suppliers/portal-users/${userId}`, toPascalCase(data)),
  deletePortalUser: (userId: string) => api.delete<ApiResponse>(`/suppliers/portal-users/${userId}`),
  submitForApproval: (id: string) => api.post<ApiResponse>(`/suppliers/${id}/submit-for-approval`),
  approve: (id: string, data: { employeeId: string; comments?: string }) => api.post<ApiResponse>(`/suppliers/${id}/approve`, data),
  reject: (id: string, data: { employeeId: string; comments?: string }) => api.post<ApiResponse>(`/suppliers/${id}/reject`, data),
  deactivate: (id: string, data: { employeeId: string; comments?: string }) => api.post<ApiResponse>(`/suppliers/${id}/deactivate`, data),
  calculateRating: (id: string) => api.post<ApiResponse>(`/suppliers/${id}/rating/calculate`),
  getRating: (id: string) => api.get<ApiResponse<any>>(`/suppliers/${id}/rating`),
};

interface SupplierPortalUser {
  id: string;
  username: string;
  contactName?: string;
  email?: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export const purchaseOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<any[]>>('/purchase-orders', { params }),
  get: (id: string) => api.get<ApiResponse<any>>(`/purchase-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/purchase-orders', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/purchase-orders/${id}`, data),
  confirm: (po_no: string) => api.post<ApiResponse>(`/purchase-orders/${po_no}/confirm`),
  receive: (po_no: string, warehouse_code?: string) => api.post<ApiResponse>(`/purchase-orders/${po_no}/receive`, { warehouse_code }),
  delete: (id: string) => api.delete<ApiResponse>(`/purchase-orders/${id}`),
};

export const planningApi = {
  getSuggestions: () => api.get<ApiResponse<any[]>>('/planning/suggestions'),
  persistSuggestions: () => api.post<ApiResponse<any[]>>('/planning/suggestions'),
  createPlan: (req: PlanningRequest) => api.post<ApiResponse<PlanningResponse>>('/planning/plan', req),
  executePlan: (req: ExecutionRequest) => api.post<ApiResponse<ExecutionResponse>>('/planning/execute', req),
  reviewResult: (req: ReviewRequest) => api.post<ApiResponse<ReviewResponse>>('/planning/review', req),
  getKpiValidation: () => api.get<ApiResponse<KpiValidationResult>>('/planning/kpi'),
  getHealth: () => api.get<ApiResponse<{ status: string; service: string; version: string; features: string[] }>>('/planning/health'),
  getAgentState: (executionId: string) => api.get<ApiResponse<AgentState>>(`/planning/state/${executionId}`),
  getExecutionHistory: (limit?: number) => api.get<ApiResponse<AgentState[]>>('/planning/history', { params: { limit: limit?.toString() } }),
  getAvailableTools: () => api.get<ApiResponse<Array<{ id: string; name: string; module: string }>>>('/planning/tools'),
  getPlanningGraph: (planId: string) => api.get<ApiResponse<{ nodes: Array<{ id: string; name: string; type: string }>; edges: Array<{ from: string; to: string; condition: string }> }>>(`/planning/graph/${planId}`),
  executeTool: (req: ToolExecuteRequest) => api.post<ApiResponse<ToolResult>>('/planning/tool/execute', req),
};

// ========== MPS ==========
export interface MpsHeaderDto {
  id: string;
  mpsNo: string;
  planName: string;
  horizonStart: string;
  horizonEnd: string;
  timeBucketType: string;
  versionNo: number;
  status: string;
  baseOn: string;
  createdAt: string;
}

export interface MpsLineDto {
  id: string;
  skuId: string;
  skuNumber: string;
  skuName: string;
  bucketStart: string;
  bucketEnd: string;
  planQty: number;
  consumedQty: number;
  adjustedQty: number;
  notes?: string;
}

export interface MpsDetailDto extends MpsHeaderDto {
  lines: MpsLineDto[];
}

export interface ResourceLoadDto {
  resourceId: string;
  resourceCode: string;
  resourceName: string;
  bucketCode: string;
  bucketStart: string;
  bucketEnd: string;
  requiredCapacity: number;
  availableCapacity: number;
  loadPercentage: number;
  status: 'OK' | 'CRITICAL' | 'OVERLOAD';
}

export interface RccpStatusDto {
  mpsHeaderId: string;
  loads: ResourceLoadDto[];
}

export interface WeeklyLoadDto {
  weekCode: string;
  weekStart: string;
  required: number;
  available: number;
  loadPercentage: number;
}

export interface ResourceAggregateLoadDto {
  resourceId: string;
  resourceCode: string;
  resourceName: string;
  resourceType: string;
  uom: string;
  weeklyLoads: WeeklyLoadDto[];
  maxLoadPercentage: number;
}

export interface AggregateRccpDto {
  startDate: string;
  endDate: string;
  resources: ResourceAggregateLoadDto[];
}

export interface CurrencyDto {
  id: string;
  currencyCode: string;
  currencyName: string;
  symbol: string;
  decimalPlaces: number;
  isBaseCurrency: boolean;
}

export interface ExchangeRateDto {
  id: string;
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  validFrom: string;
  validTo?: string;
}

export interface CreateMpsRequest {
  planName: string;
  horizonStart: string;
  horizonEnd: string;
  timeBucketType: string;
  baseOn: string;
  lines: Array<{
    skuId: string;
    bucketStart: string;
    bucketEnd: string;
    planQty: number;
    notes?: string;
  }>;
}

export const currenciesApi = {
  listCurrencies: () => api.get<ApiResponse<CurrencyDto[]>>('/currencies'),
  createCurrency: (data: Partial<CurrencyDto>) => api.post<ApiResponse<CurrencyDto>>('/currencies', data),
  listRates: () => api.get<ApiResponse<ExchangeRateDto[]>>('/currencies/rates'),
  createRate: (data: Partial<ExchangeRateDto>) => api.post<ApiResponse<ExchangeRateDto>>('/currencies/rates', data),
};

export const rccpApi = {
  listResources: () => api.get<ApiResponse<any[]>>('/rccp/resources'),
  createResource: (data: any) => api.post<ApiResponse>('/rccp/resources', data),
  updateResource: (id: string, data: any) => api.put<ApiResponse>(`/rccp/resources/${id}`, data),
  getItemResources: (skuId: string) => api.get<ApiResponse<any[]>>('/rccp/item-resources/' + skuId),
  updateItemResources: (data: any[]) => api.post<ApiResponse>('/rccp/item-resources', data),
  getCapacitySummary: () => api.get<ApiResponse<any>>('/rccp/summary'),
  getWorkCenterCapacity: (workCenterId: string) => api.get<ApiResponse<any>>(`/rccp/work-centers/${workCenterId}`),
};

export const purchaseRequisitionsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<PurchaseRequisitionDto[]>>('/purchase-requisitions', { params }),
  get: (id: string) => api.get<ApiResponse<PurchaseRequisitionDto>>(`/purchase-requisitions/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/purchase-requisitions', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/purchase-requisitions/${id}`, toPascalCase(data)),
  updateStatus: (id: string, status: string) => api.patch<ApiResponse>(`/purchase-requisitions/${id}/status`, { status }),
  toggleLock: (id: string, isLocked: boolean) => api.patch<ApiResponse>(`/purchase-requisitions/${id}/lock`, { isLocked }),
  submit: (id: string) => api.post<ApiResponse>(`/purchase-requisitions/${id}/submit`),
  approve: (id: string, data: { employeeId: string; comments?: string }) => api.post<ApiResponse>(`/purchase-requisitions/${id}/approve`, data),
  reject: (id: string, data: { employeeId: string; comments?: string }) => api.post<ApiResponse>(`/purchase-requisitions/${id}/reject`, data),
  returnForEdit: (id: string, data: { employeeId: string; comments?: string }) => api.post<ApiResponse>(`/purchase-requisitions/${id}/return-for-edit`, data),
  getApprovalChain: (id: string) => api.get<ApiResponse<any>>(`/purchase-requisitions/${id}/approval-chain`),
  getApprovalHistory: (id: string) => api.get<ApiResponse<any[]>>(`/purchase-requisitions/${id}/approval-history`),
  convertToPo: (id: string, supplierId: string) => api.post<ApiResponse>(`/purchase-requisitions/${id}/convert-to-po`, { supplierId }),
  checkBudget: (costCenter: string, amount: number) => api.get<ApiResponse<any>>('/purchase-requisitions/budget-check', { params: { costCenter, amount: amount.toString() } }),
};

export const rfqsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rfqs', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/rfqs', data),
  addResponse: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/rfqs/${id}/responses`, data),
  award: (id: string, supplierId: string) => api.patch<ApiResponse>(`/rfqs/${id}/award`, { supplierId }),
};

export const goodsReceiptsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/GoodsReceipts', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/GoodsReceipts/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/GoodsReceipts', data),
  updateIqc: (id: string, data: Record<string, unknown>) => api.patch<ApiResponse>(`/GoodsReceipts/${id}/iqc`, data),
  allocateCosts: (id: string, data: { totalTariff: number; totalFreight: number; allocateByAmount?: boolean }) => 
    api.post<ApiResponse>(`/GoodsReceipts/${id}/allocate-costs`, data),
};

export interface AccountsPayableDto {
  id: string;
  apNumber: string;
  supplierId?: string;
  supplierName?: string;
  purchaseOrderId?: string;
  purchaseOrderNumber?: string;
  goodsReceiptId?: string;
  goodsReceiptNumber?: string;
  supplierInvoiceNumber?: string;
  supplierInvoiceDate?: string;
  threeWayMatchResult: string;
  invoiceAmount: number;
  taxAmount: number;
  totalAmount: number;
  dueDate?: string;
  paymentStatus: string;
  verificationStatus?: string;
  notes?: string;
  createdAt?: string;
}

export const accountsPayableApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<AccountsPayableDto[]>>('/accounts-payable', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/accounts-payable', data),
  threeWayMatch: (poId: string) => api.get<ApiResponse<AccountsPayableDto>>(`/accounts-payable/three-way-match/${poId}`),
  pay: (id: string) => api.patch<ApiResponse>(`/accounts-payable/${id}/pay`),
};

// ========== Finance ==========
export interface GlAccountDto { id: string; accountNo: string; accountName: string; accountType: string; isActive: boolean; }
export interface JournalEntryDto { id: string; entryDate: string; accountId: string; accountNo?: string; accountName?: string; amountDr: number; amountCr: number; sourceModule: string; sourceDocNo: string; description?: string; }
export interface PostingSetupDto { id: string; transactionType: string; itemGroup?: string; drAccountNo: string; crAccountNo: string; }
export interface GroupedVoucher extends JournalEntryDto {
  entries?: Array<{
    glAccount?: { accountName: string };
    debitAmount: number;
    creditAmount: number;
  }>;
}

export const financeApi = {
  getChartOfAccounts: () => api.get<ApiResponse<GlAccountDto[]>>('/finance/coa'),
  getJournalEntries: (docNo: string) => api.get<ApiResponse<JournalEntryDto[]>>(`/finance/journals/${docNo}`),
  getVouchers: (params?: Record<string, string>) => api.get<ApiResponse<any[]>>('/finance/vouchers', { params }),
  getPostingSetups: () => api.get<ApiResponse<PostingSetupDto[]>>('/finance/posting-setups'),
  seedAccounts: () => api.post<ApiResponse>('/finance/seed'),
  seedBankAccounts: () => api.post<ApiResponse<string>>('/finance/seed-accounts'),
  loans: (params?: Record<string, string>) => api.get<ApiResponse<LoanDto[]>>('/finance/loans', { params }),
  createLoan: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/loans', data),
  bankAccounts: () => api.get<ApiResponse<BankAccountDto[]>>('/finance/bank-accounts'),
  createBankAccount: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/bank-accounts', data),
  investments: (params?: Record<string, string>) => api.get<ApiResponse<InvestmentDto[]>>('/finance/investments', { params }),
  createInvestment: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/investments', data),
  transfers: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/finance/treasury-transfers', { params }),
  createTransfer: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/treasury-transfers', data),
  guarantees: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/finance/guarantees', { params }),
  createGuarantee: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/guarantees', data),
};

// ========== HR Attendance & Leave & Payroll ==========
export interface AttendanceRecordDto {
  id: string; employeeId: string; employeeNo?: string; employeeName?: string;
  departmentId?: string; productionLine?: string; date: string; shiftType?: string;
  scheduledStartTime?: string; scheduledEndTime?: string;
  clockIn?: string; clockOut?: string; clockInLocation?: string; clockOutLocation?: string;
  status: string; lateFlag: boolean; lateMinutes: number;
  earlyLeaveFlag: boolean; earlyLeaveMinutes: number;
  clockingAnomalyFlag: boolean; anomalyType?: string;
  ot1Hours: number; ot2Hours: number; ot3Hours: number; otReason?: string;
  approvalStatus: string; notes?: string;
}

export interface LeaveRequestDto {
  id: string; employeeId: string; employeeNo?: string; employeeName?: string;
  applicationNo: string; leaveType: string; applicationDate: string;
  startDate: string; endDate: string; startTime?: string; endTime?: string;
  totalHours: number; reason?: string; attachmentUrl?: string;
  includesSpecialPeriod: boolean; shiftConflictFlag: boolean;
  autoDeductPayFlag: boolean; impactOtOrBonus: boolean;
  approvalLevel: number; status: string;
  approverId?: string; approvedAt?: string;
  rejectReason?: string; remarks?: string; createdAt: string;
}

export interface LeaveBalanceDto {
  id: string; employeeId: string; employeeNo?: string; employeeName?: string;
  leaveType: string; year: number;
  totalEntitled: number; used: number; remaining: number;
}

export interface PayrollDto {
  id: string; employeeId: string; employeeNo?: string; employeeName?: string;
  periodStart: string; periodEnd: string;
  basicSalary: number; insuranceBase: number; pensionBase: number;
  newLeavingFlag: boolean; scheduledDays: number; actualWorkingDays: number;
  totalLateMinutes: number; totalEarlyMinutes: number; anomalyDays: number;
  totalOt1Hours: number; totalOt2Hours: number; totalOt3Hours: number;
  ot1Pay: number; ot2Pay: number; ot3Pay: number;
  totalLeaveHours: number; annualLeaveHours: number;
  personalLeaveHours: number; sickLeaveHours: number;
  fullAttendanceFlag: boolean;
  lateDeduction: number; earlyDeduction: number; anomalyPenalty: number;
  personalLeaveDeduction: number; sickLeaveDeduction: number; otherDeductions: number;
  overtimePay: number; bonus: number; attendanceBonus: number;
  laborInsuranceDeduction: number; healthInsuranceDeduction: number;
  pensionDeduction: number; withholdingTax: number;
  employerLi: number; employerHi: number; employerPf: number;
  employmentInsurance: number; supplementalHealthInsurance?: number;
  grossPay: number; totalDeductions: number; employerContributions: number;
  netPay: number; calculatedBy?: string; calculatedAt?: string;
  status: string; paymentDate?: string; paymentBankAccount?: string;
  payrollSlipNo?: string; notes?: string; createdAt: string; updatedAt: string;
}

export const attendanceApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<AttendanceRecordDto[]>>('/Attendance', { params }),
  get: (id: string) => api.get<ApiResponse<AttendanceRecordDto>>(`/Attendance/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<AttendanceRecordDto>>('/Attendance', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<AttendanceRecordDto>>(`/Attendance/${id}`, toPascalCase(data)),
  approve: (id: string, status: string, notes?: string) => api.patch<ApiResponse<boolean>>(`/Attendance/${id}/approve`, { status, notes }),
  delete: (id: string) => api.delete<ApiResponse>(`/Attendance/${id}`),
};

export const leaveRequestsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<LeaveRequestDto[]>>('/LeaveRequests', { params }),
  get: (id: string) => api.get<ApiResponse<LeaveRequestDto>>(`/LeaveRequests/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<LeaveRequestDto>>('/LeaveRequests', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<LeaveRequestDto>>(`/LeaveRequests/${id}`, toPascalCase(data)),
  approve: (id: string, status: string, approverId?: string, rejectReason?: string) =>
    api.patch<ApiResponse<boolean>>(`/LeaveRequests/${id}/approve`, { status, approverId, rejectReason }),
  cancel: (id: string, reason?: string) => api.patch<ApiResponse<boolean>>(`/LeaveRequests/${id}/cancel`, { reason }),
  balances: (params?: Record<string, string>) => api.get<ApiResponse<LeaveBalanceDto[]>>('/LeaveRequests/balances', { params }),
};

export const payrollApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<PayrollDto[]>>('/Payroll', { params }),
  get: (id: string) => api.get<ApiResponse<PayrollDto>>(`/Payroll/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<PayrollDto>>('/Payroll', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<PayrollDto>>(`/Payroll/${id}`, toPascalCase(data)),
  updateStatus: (id: string, status: string, paymentDate?: string, paymentBankAccount?: string, payrollSlipNo?: string) =>
    api.patch<ApiResponse<boolean>>(`/Payroll/${id}/status`, { status, paymentDate, paymentBankAccount, payrollSlipNo }),
  delete: (id: string) => api.delete<ApiResponse>(`/Payroll/${id}`),
};

// ========== MES ==========
export interface WorkOrderDto { id: string; woNumber: string; woType: string; productSkuId: string; productSkuNumber?: string; productSkuName?: string; plannedQty: number; actualQty: number; scrapQty: number; plannedStartDate?: string; plannedEndDate?: string; status: string; priority: string; notes?: string; createdAt: string; }
export interface WorkOrderCostAnalysisDto {
    workOrderId: string;
    woNumber: string;
    actualMaterialCost: number;
    actualLaborCost: number;
    actualOverheadCost: number;
    totalActualCost: number;
    standardMaterialCost: number;
    standardLaborCost: number;
    totalStandardCost: number;
    varianceAmount: number;
    variancePercentage: number;
    unitCost: number;
    actualQty: number;
}

export const bomsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<BomDto[]>>('/boms', { params }),
  get: (id: string) => api.get<ApiResponse<BomDto>>(`/boms/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/boms', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/boms/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/boms/${id}`),
  activate: (id: string) => api.patch<ApiResponse>(`/boms/${id}/activate`),
  deprecate: (id: string) => api.patch<ApiResponse>(`/boms/${id}/deprecate`),
  costRollup: (id: string) => api.get<ApiResponse<{ materialCost: number; laborCost: number; overheadCost: number; totalCost: number }>>(`/boms/${id}/cost-rollup`),
};

export const ecnApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<EcnDto[]>>('/ecns', { params }),
  get: (id: string) => api.get<ApiResponse<EcnDto>>(`/ecns/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/ecns', data),
  getImpact: (id: string) => api.get<ApiResponse<any>>(`/ecns/${id}/impact`),
};

export const workOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<WorkOrderDto[]>>('/WorkOrders', { params }),
  get: (id: string) => api.get<ApiResponse<WorkOrderDto>>(`/WorkOrders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/WorkOrders', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/WorkOrders/${id}`, toPascalCase(data)),
  updateStatus: (id: string, status: string) => api.patch<ApiResponse>(`/WorkOrders/${id}/status`, toPascalCase({ status })),
  start: (id: string) => api.patch<ApiResponse>(`/WorkOrders/${id}/start`),
  complete: (id: string, data: Record<string, unknown>) => api.patch<ApiResponse>(`/WorkOrders/${id}/complete`, toPascalCase(data)),
  explodeRouting: (id: string) => api.patch<ApiResponse>(`/WorkOrders/${id}/explode-routing`),
  createSupplyOrders: (id: string) => api.post<ApiResponse>(`/WorkOrders/${id}/create-supply-orders`),
  costingAnalysis: (id: string) => api.get<ApiResponse<WorkOrderCostAnalysisDto>>(`/workorders/${id}/costing/analysis`),
};

export const costingApi = {
  summary: () => api.get<ApiResponse<any[]>>('/work-orders-costing/summary'),
  analysis: (id: string) => api.get<ApiResponse<WorkOrderCostAnalysisDto>>(`/workorders/${id}/costing/analysis`),
  settle: (id: string) => api.post<ApiResponse>(`/workorders/${id}/costing/settle`),
  batchSettle: () => api.post<ApiResponse>('/work-orders-costing/batch-settle'),
};

export const wipReportsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/WipReports', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/WipReports', data),
};

// ========== Dashboard ==========
export interface DashboardKpi { title: string; value: string; unit?: string; changePercent?: number; changeDirection?: string; color?: string; }
export interface DashboardAlert { id: string; title: string; description?: string; severity: string; createdAt: string; }
export interface WorkOrderSummary { id: string; woNumber: string; productName?: string; plannedQty: number; completedQty: number; progress: number; status: string; priority: string; dueDate?: string; }
export interface ProductionTrend { date: string; planned: number; actual: number; efficiency: number; }
export interface AttendanceTrend { date: string; present: number; late: number; absent: number; leave: number; }

export interface SidebarBadge {
  salesCount: number;
  procurementCount: number;
  hrCount: number;
  inventoryCount: number;
  manufacturingCount: number;
  qualityCount: number;
}

export interface DashboardData {
  kpiCards: DashboardKpi[];
  alerts: DashboardAlert[];
  recentWorkOrders: WorkOrderSummary[];
  productionTrend: ProductionTrend[];
  attendanceTrend: AttendanceTrend[];
  totalEmployees: number;
  todayAttendanceCount: number;
  pendingApprovals: number;
  activeAlerts: number;
  sidebarBadges: SidebarBadge;
}

export const dashboardApi = {
  get: () => api.get<ApiResponse<DashboardData>>('/Dashboard'),
  getBadges: () => api.get<ApiResponse<SidebarBadge>>('/Dashboard/sidebar-badges'),
};

// ========== Finance ==========
export interface LoanDto { id: string; loanNumber: string; bankName?: string; loanAmount: number; currency: string; annualInterestRate: number; loanDate: string; maturityDate: string; repaymentMethod: string; status: string; }
export interface BankAccountDto { id: string; bankCode?: string; bankName?: string; accountNumber?: string; currency: string; currentBalance: number; isActive: boolean; }
export interface InvestmentDto { id: string; investmentName: string; investmentCategory: string; totalCost: number; currentMarketValue: number; unrealizedGainLoss: number; }


// ========== Assets ==========
export interface AssetDto { id: string; assetNumber: string; assetName: string; specification?: string; categoryName?: string; locationName?: string; acquisitionCost: number; acquisitionDate?: string; usefulLife: number; status: string; }
export interface AssetCategoryDto { id: string; categoryCode: string; categoryName: string; depreciationMethod: string; defaultUsefulLife: number; }

export const assetsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<AssetDto[]>>('/assets', { params }),
  get: (id: string) => api.get<ApiResponse<AssetDto>>(`/assets/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/assets', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/assets/${id}`, data),
  depreciate: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/depreciation`, data),
  transfer: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/transfer`, data),
  maintenance: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/maintenance`, data),
  dispose: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/assets/${id}/dispose`, data),
  categories: () => api.get<ApiResponse<AssetCategoryDto[]>>('/assets/categories'),
  createCategory: (data: Record<string, unknown>) => api.post<ApiResponse>('/assets/categories', data),
  locations: () => api.get<ApiResponse<unknown[]>>('/assets/locations'),
};

// ========== Budget ==========
export interface BudgetYearDto { id: string; year: number; status: string; }
export interface BudgetVersionDto { id: string; versionNumber: string; versionName?: string; isActive: boolean; }

export const budgetApi = {
  years: () => api.get<ApiResponse<BudgetYearDto[]>>('/budget/years'),
  createYear: (data: Record<string, unknown>) => api.post<ApiResponse>('/budget/years', data),
  updateYear: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/budget/years/${id}`, data),
  versions: (yearId: string) => api.get<ApiResponse<BudgetVersionDto[]>>(`/budget/years/${yearId}/versions`),
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

export interface BudgetAlertDto {
  costCenterName?: string;
  alertType?: string;
  severity?: string;
  message?: string;
  budgetAmount?: number;
  actualAmount?: number;
}

export interface BudgetAnomalyDto {
  costCenterCode?: string;
  anomalyScore: number;
  reason?: string;
  averageAmount?: number;
  actualAmount?: number;
}

export interface BudgetAnalysisDto {
  executionRate: number;
  remainingBudget: number;
  totalBudget: number;
  predictedYearEndBalance: number;
  monthsUntilOverrun?: number;
  recommendations?: string[];
  monthlyUtilization?: Array<{
    month: number;
    actual: number;
    budget: number;
  }>;
}

// ========== Quality ==========
export interface IqcReportDto { id: string; reportNumber: string; supplierName?: string; inspectionDate?: string; reportDate?: string; result: string; batchNumber?: string; sampleQty?: number; defectQty?: number; }
export interface PendingIqcReceiptDto extends GoodsReceiptDto {}
export interface QaReviewLogDto {
  id: string;
  targetType: string;
  targetId: string;
  actionType: string;
  actorName?: string;
  responsiblePerson?: string;
  notes?: string;
  attachmentUrl?: string;
  createdAt: string;
}
export interface SupplierComplaintDto {
  id: string;
  complaintNumber: string;
  supplierName?: string;
  skuName?: string;
  defectType?: string;
  ppm: number;
  priority: string;
  status: string;
  responsiblePerson?: string;
  attachmentUrl?: string;
  closureReason?: string;
  closedAt?: string;
  createdAt?: string;
  capaNumber?: string;
  capaStatus?: string;
  capaResponsiblePerson?: string;
  capaAttachmentUrl?: string;
  capaClosureReason?: string;
  capaClosedAt?: string;
  timeline?: QaReviewLogDto[];
  capaTimeline?: QaReviewLogDto[];
}
export interface NonConformanceReportDto {
  id: string;
  ncrNumber: string;
  sourceType: string;
  sourceId?: string;
  sourceNumber?: string;
  supplierName?: string;
  skuName?: string;
  defectType?: string;
  defectQty: number;
  defectDescription?: string;
  disposition?: string;
  costImpact: number;
  responsiblePerson?: string;
  attachmentUrl?: string;
  closureReason?: string;
  closedAt?: string;
  createdAt: string;
  timeline?: QaReviewLogDto[];
}

export const qualityApi = {
  iqcReports: (params?: Record<string, string>) => api.get<ApiResponse<IqcReportDto[]>>('/quality/iqc-reports', { params }),
  createIqcReport: (data: Record<string, unknown>) => api.post<ApiResponse>('/quality/iqc-reports', data),
  pendingIqcReceipts: () => api.get<ApiResponse<PendingIqcReceiptDto[]>>('/quality/iqc-pending'),
  ipqcReports: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quality/ipqc-reports', { params }),
  createIpqcReport: (data: Record<string, unknown>) => api.post<ApiResponse>('/quality/ipqc-reports', data),
  oqcReports: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quality/oqc-reports', { params }),
  ncrs: (params?: Record<string, string>) => api.get<ApiResponse<NonConformanceReportDto[]>>('/quality/ncrs', { params }),
  createNcr: (data: Record<string, unknown>) => api.post<ApiResponse<NonConformanceReportDto>>('/quality/ncrs', data),
  updateNcrOwnership: (id: string, data: { responsiblePerson?: string; attachmentUrl?: string; notes?: string }) =>
    api.patch<ApiResponse<NonConformanceReportDto>>(`/quality/ncrs/${id}/ownership`, data),
  closeNcr: (id: string, data: { disposition?: string; releaseStock?: boolean; notes?: string }) =>
    api.patch<ApiResponse<NonConformanceReportDto>>(`/quality/ncrs/${id}/close`, data),
  complaints: (params?: Record<string, string>) => api.get<ApiResponse<SupplierComplaintDto[]>>('/quality/complaints', { params }),
  createComplaint: (data: Record<string, unknown>) => api.post<ApiResponse>('/quality/complaints', data),
  updateComplaintOwnership: (id: string, data: { responsiblePerson?: string; attachmentUrl?: string; notes?: string }) =>
    api.patch<ApiResponse<SupplierComplaintDto>>(`/quality/complaints/${id}/ownership`, data),
  createCapa: (complaintId: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/quality/complaints/${complaintId}/capa`, data),
  updateCapaOwnership: (complaintId: string, data: { responsiblePerson?: string; attachmentUrl?: string; notes?: string }) =>
    api.patch<ApiResponse>(`/quality/complaints/${complaintId}/capa/ownership`, data),
  closeCapa: (complaintId: string, data: { closure?: string; verification?: string }) =>
    api.patch<ApiResponse>(`/quality/complaints/${complaintId}/capa/close`, data),
  traceLot: (lotNumber: string) =>
    api.get<ApiResponse<LotTraceDto>>(`/quality/trace/${lotNumber}`),
  traceMaterial: (skuId: string) =>
    api.get<ApiResponse<MaterialTraceDto>>(`/quality/trace/material/${skuId}`),
};

// ========== R&D ==========
export interface RndProjectDto { id: string; projectCode: string; projectName: string; projectType: string; status: string; estimatedBudget: number; startDate?: string; }
export interface RndLogDto { id: string; projectId: string; content: string; type: string; createdAt: string; }

export const rndApi = {
    projects: (params?: Record<string, string>) => api.get<ApiResponse<RndProjectDto[]>>('/rd/projects', { params }),
    getProject: (id: string) => api.get<ApiResponse<RndProjectDto>>(`/rd/projects/${id}`),
    createProject: async (data: Record<string, unknown>) => {
        // Transform to PascalCase and ensure required fields are properly formatted
        const transformedData = {
            ...data,
            ProjectCode: data.ProjectCode || '', // Send empty string to trigger auto-generation
            ProjectName: data.ProjectName || '',
            ProjectType: data.ProjectType || 'new_product',
            EstimatedBudget: data.EstimatedBudget !== undefined ? parseFloat(String(data.EstimatedBudget)) : 0,
            TargetPrice: data.TargetPrice !== null && data.TargetPrice !== undefined && data.TargetPrice !== '' ? parseFloat(String(data.TargetPrice)) : null,
            StartDate: data.StartDate ? new Date(data.StartDate as string).toISOString().split('T')[0] : undefined,
            TargetSampleDate: data.TargetSampleDate ? new Date(data.TargetSampleDate as string).toISOString().split('T')[0] : undefined,
            TargetProductionDate: data.TargetProductionDate ? new Date(data.TargetProductionDate as string).toISOString().split('T')[0] : undefined,
        };
        
        // Remove undefined fields
        const cleanedData = Object.fromEntries(
            Object.entries(transformedData).filter(([_, v]) => v !== undefined)
        );
        
        console.log('Sending R&D project data:', cleanedData);
        try {
            const response = await api.post<ApiResponse>('/rd/projects', cleanedData);
            console.log('API response:', response.data);
            if (!response.data?.success) {
                throw new Error(response.data?.message || '建立失敗');
            }
            return response.data;
        } catch (err: unknown) {
            console.error('API error:', err);
            if (err && typeof err === 'object' && 'response' in err) {
                const axiosErr = err as { response?: { data?: { message?: string } } };
                if (axiosErr.response?.data?.message) {
                    throw new Error(axiosErr.response.data.message);
                }
            }
            throw err;
        }
    },
    updateStatus: (id: string, status: string) => api.patch<ApiResponse>(`/rd/projects/${id}/status`, { status }),
    documents: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rd/documents', { params }),
    createDocument: (data: Record<string, unknown>) => api.post<ApiResponse>('/rd/documents', data),
    logs: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rd/logs', { params }),
    createLog: (data: Record<string, unknown>) => api.post<ApiResponse>('/rd/logs', data),
};

// ========== Trial Production API ==========
export const trialProductionApi = {
    list: (params?: Record<string, string>) => api.get<ApiResponse<any[]>>('/rd/trial-productions', { params }),
    create: (data: Record<string, unknown>) => api.post<ApiResponse<any>>('/rd/trial-productions', data),
    update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<any>>(`/rd/trial-productions/${id}`, data),
};

// ========== Mass Production Transition API ==========
export const massProductionApi = {
    transition: (data: Record<string, unknown>) => api.post<ApiResponse<any>>('/rd/mass-production-transition', data),
    complete: (id: string) => api.patch<ApiResponse<any>>(`/rd/mass-production-transition/${id}/complete`),
};

// ========== ESG ==========
export interface EmissionRecordDto { 
  id: string; 
  sourceId?: string;
  sourceCode?: string;
  sourceName?: string; 
  scope?: string;
  period: string; 
  periodStart?: string;
  periodEnd?: string;
  activityData: number; 
  activityUnit?: string;
  emissionFactor?: number;
  calculatedEmissions: number;
  emissionUnit?: string;
  status?: string;
  createdAt?: string;
}

export interface EmissionSummaryDto {
  scope1?: number;
  scope2?: number;
  scope3?: number;
  total?: number;
}

export interface EmissionSourceDto {
  id: string;
  sourceCode: string;
  sourceName: string;
  scope: string;
  category: string;
  description: string;
  isActive: boolean;
}

export interface EmissionFactorDto {
  id: string;
  factorCode: string;
  factorName: string;
  unit: string;
  factorValue: number;
  unitOfMeasure: string;
  scope: string;
  source: string;
  region: string;
  year: number;
  isVerified: boolean;
}

export interface CarbonReductionTargetDto {
  id: string;
  targetCode: string;
  targetName: string;
  scope: string;
  baselineEmissions: number;
  targetReductionPercent: number;
  targetEmissions: number;
  baseYear: number;
  targetYear: number;
  currentEmissions: number;
  achievedReductionPercent: number;
  description: string;
  status: string;
}

export interface SupplierSustainabilityDto {
  id: string;
  supplierId: string;
  supplierCode: string;
  supplierName: string;
  carbonFootprint: number;
  carbonUnit: string;
  renewableEnergyPercent: number;
  sustainabilityScore: number;
  rating: string;
  lastAssessmentDate: string;
  nextAssessmentDate?: string;
  certification: string;
  improvementPlan: string;
  hasIso14001: boolean;
  hasIso14064: boolean;
  status: string;
}

export interface EnergyConsumptionDto {
  id: string;
  energyType: string;
  scope: string;
  consumption: number;
  unit: string;
  carbonFactor: number;
  calculatedEmissions: number;
  period: string;
}

export interface AiInsightDto {
  id: string;
  type: string;
  title: string;
  description: string;
  severity: string;
  recommendation?: string;
  potentialReduction?: number;
}

export interface EsgReportDto {
  id: string;
  reportCode: string;
  reportName: string;
  reportType: string;
  framework: string;
  reportPeriodStart: string;
  reportPeriodEnd: string;
  totalScope1Emissions: number;
  totalScope2Emissions: number;
  totalScope3Emissions: number;
  totalEmissions: number;
  emissionIntensity: number;
  intensityUnit: string;
  status: string;
  isVerified: boolean;
  verifier: string;
  verifiedAt?: string;
  publishedAt?: string;
}

export const esgApi = {
  emissionSummary: () => api.get<ApiResponse<EmissionSummaryDto>>('/esg/emission-summary'),
  emissionRecords: (params?: Record<string, string>) => api.get<ApiResponse<EmissionRecordDto[]>>('/esg/emission-records', { params }),
  createEmissionRecord: (data: Record<string, unknown>) => api.post<ApiResponse>('/esg/emission-records', data),
  emissionSources: () => api.get<ApiResponse<EmissionSourceDto[]>>('/esg/emission-sources'),
  createEmissionSource: (data: Record<string, unknown>) => api.post<ApiResponse>('/esg/emission-sources', data),
  emissionFactors: (params?: Record<string, string>) => api.get<ApiResponse<EmissionFactorDto[]>>('/esg/emission-factors', { params }),
  reductionTargets: () => api.get<ApiResponse<CarbonReductionTargetDto[]>>('/esg/reduction-targets'),
  supplierSustainability: () => api.get<ApiResponse<SupplierSustainabilityDto[]>>('/esg/supplier-sustainability'),
  energyConsumption: () => api.get<ApiResponse<EnergyConsumptionDto[]>>('/esg/energy-consumption'),
  aiInsights: (params?: Record<string, string>) => api.get<ApiResponse<AiInsightDto[]>>('/esg/ai-insights', { params }),
  reports: () => api.get<ApiResponse<EsgReportDto[]>>('/esg/reports'),
  calculateEmissions: (data: Record<string, unknown>) => api.post<ApiResponse<number>>('/esg/calculate-emissions', data),
};

// ========== Collaboration ==========
export interface AnnouncementDto { id: string; title: string; category: string; content?: string; isPinned: boolean; isUrgent: boolean; publishDate: string; }
export interface MeetingRoomDto { id: string; roomName: string; location?: string; capacity: number; isActive: boolean; }
export interface CrossDeptTaskDto { id: string; taskName: string; urgency: string; status: string; ownerDeptName?: string; expectedCompleteDate?: string; }

// ========== Calendar ==========
export interface CalendarEventDto {
  id: string; title: string; description?: string; eventType: string;
  categoryCode?: string; categoryName?: string; categoryColor?: string;
  location?: string; startDate: string; endDate: string; isAllDay: boolean;
  recurrenceRule?: string; recurrenceParentId?: string;
  participants?: string; organizerId?: string; departmentId?: string;
  visibility: string; sourceModule?: string; sourceId?: string;
  meetingRoomId?: string; meetingRoomName?: string; projectId?: string;
  color?: string; isPinned: boolean; reminders?: string;
  status: string; syncToOutlook: boolean; createdAt: string; updatedAt?: string;
}
export interface CalendarCategoryDto {
  id: string; code: string; name: string; color: string; icon?: string; sortOrder: number; isActive: boolean;
}
export interface CompanyHolidayDto {
  id: string; name: string; date: string; holidayType: string; isRecurring: boolean; description?: string;
}
export interface CalendarShareDto {
  id: string; calendarOwnerId: string; sharedWithEmployeeId?: string;
  sharedWithDeptId?: string; sharedWithRoleId?: string;
  permission: string; canSeeDetails: boolean;
}
export interface CalendarConflictDto {
  eventId: string; title: string; startDate: string; endDate: string; conflictType: string; detail?: string;
}

export const calendarApi = {
  events: (params?: Record<string, string>) => api.get<ApiResponse<CalendarEventDto[]>>('/calendar/events', { params }),
  range: (startDate: string, endDate: string) => api.get<ApiResponse<CalendarEventDto[]>>('/calendar/range', { params: { startDate, endDate } }),
  getEvent: (id: string) => api.get<ApiResponse<CalendarEventDto>>(`/calendar/events/${id}`),
  createEvent: (data: Record<string, unknown>) => api.post<ApiResponse<CalendarEventDto>>('/calendar/events', data),
  updateEvent: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<CalendarEventDto>>(`/calendar/events/${id}`, data),
  deleteEvent: (id: string) => api.delete<ApiResponse>(`/calendar/events/${id}`),
  updateEventStatus: (id: string, status: string) => api.patch<ApiResponse<CalendarEventDto>>(`/calendar/events/${id}/status`, status),
  conflicts: (params: Record<string, string>) => api.get<ApiResponse<CalendarConflictDto[]>>('/calendar/conflicts', { params }),
  exportIcs: (startDate: string, endDate: string) => `${API_BASE}/calendar/export/ics?startDate=${startDate}&endDate=${endDate}`,
  categories: () => api.get<ApiResponse<CalendarCategoryDto[]>>('/calendar/categories'),
  createCategory: (data: Record<string, unknown>) => api.post<ApiResponse<CalendarCategoryDto>>('/calendar/categories', data),
  updateCategory: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<CalendarCategoryDto>>(`/calendar/categories/${id}`, data),
  deleteCategory: (id: string) => api.delete<ApiResponse>(`/calendar/categories/${id}`),
  holidays: (params?: Record<string, string>) => api.get<ApiResponse<CompanyHolidayDto[]>>('/calendar/holidays', { params }),
  createHoliday: (data: Record<string, unknown>) => api.post<ApiResponse<CompanyHolidayDto>>('/calendar/holidays', data),
  updateHoliday: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<CompanyHolidayDto>>(`/calendar/holidays/${id}`, data),
  deleteHoliday: (id: string) => api.delete<ApiResponse>(`/calendar/holidays/${id}`),
  shares: (params?: Record<string, string>) => api.get<ApiResponse<CalendarShareDto[]>>('/calendar/shares', { params }),
  createShare: (data: Record<string, unknown>) => api.post<ApiResponse<CalendarShareDto>>('/calendar/shares', data),
  deleteShare: (id: string) => api.delete<ApiResponse>(`/calendar/shares/${id}`),
};

// ========== UOM ==========
export interface UomDto {
  id: string;
  uomCode: string;
  uomName: string;
  uomNameEn?: string;
  uomType: string;
  decimalPlaces: number;
  isActive: boolean;
  allowDecimal: boolean;
  description?: string;
  displayOrder: number;
  createdAt: string;
}

export interface UomConversionDto {
  id: string;
  skuId?: string;
  skuCode?: string;
  skuName?: string;
  fromUomId: string;
  fromUomCode: string;
  fromUomName: string;
  toUomId: string;
  toUomCode: string;
  toUomName: string;
  factor: number;
  operator: string;
  inverseFactor?: number;
  isActive: boolean;
  description?: string;
  isGlobal: boolean;
}

export interface UomConversionResult {
  originalQty: number;
  originalUom: string;
  convertedQty: number;
  targetUom: string;
  factor: number;
  operator: string;
  decimalPlaces: number;
  formula: string;
}

export const uomsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<UomDto[]>>('/uoms', { params }),
  get: (id: string) => api.get<ApiResponse<UomDto>>(`/uoms/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<UomDto>>('/uoms', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<UomDto>>(`/uoms/${id}`, toPascalCase(data)),
  delete: (id: string) => api.delete<ApiResponse>(`/uoms/${id}`),
  getConversions: (params?: Record<string, string>) => api.get<ApiResponse<UomConversionDto[]>>('/uoms/conversions', { params }),
  createConversion: (data: Record<string, unknown>) => api.post<ApiResponse<UomConversionDto>>('/uoms/conversions', toPascalCase(data)),
  updateConversion: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<UomConversionDto>>(`/uoms/conversions/${id}`, toPascalCase(data)),
  deleteConversion: (id: string) => api.delete<ApiResponse>(`/uoms/conversions/${id}`),
  convert: (params: { skuId: string; qty: number; fromUom: string; toUom: string }) =>
    api.get<ApiResponse<UomConversionResult>>('/uoms/convert', { params }),
  toBase: (params: { skuId: string; qty: number; uom: string }) =>
    api.get<ApiResponse<{ qty: number; uom: string; baseQty: number }>>('/uoms/to-base', { params }),
};

export const collaborationApi = {
  announcements: (params?: Record<string, string>) => api.get<ApiResponse<AnnouncementDto[]>>('/collaboration/announcements', { params }),
  createAnnouncement: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/announcements', data),
  markRead: (id: string) => api.patch<ApiResponse>(`/collaboration/announcements/${id}/read`),
  meetingRooms: () => api.get<ApiResponse<MeetingRoomDto[]>>('/collaboration/meeting-rooms'),
  createMeetingRoom: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/meeting-rooms', data),
  reservations: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/collaboration/meeting-reservations', { params }),
  createReservation: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/meeting-reservations', data),
  tasks: (params?: Record<string, string>) => api.get<ApiResponse<CrossDeptTaskDto[]>>('/collaboration/tasks', { params }),
  createTask: (data: Record<string, unknown>) => api.post<ApiResponse>('/collaboration/tasks', data),
  updateTaskStatus: (id: string, status: string) => api.patch<ApiResponse>(`/collaboration/tasks/${id}/status`, { status }),
  addTaskReply: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/collaboration/tasks/${id}/replies`, data),
};

// ========== AI Agent Monitoring ==========
export interface AgentMetricsSummary {
  totalRequests: number;
  successRate: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
  errorRate: number;
  tokenUsage24h: number;
  activeAgents: number;
  healthScores: Array<{
    agentType: string;
    score: number;
    status: 'healthy' | 'degraded' | 'critical';
    uptime: number;
    lastCheck: string;
  }>;
}

export interface AgentFlowExecution {
  id: string;
  flowType: 'anomaly_dispatch' | 'customer_inquiry' | 'inventory_replenishment';
  flowName: string;
  status: 'running' | 'success' | 'failed';
  triggerSource: string;
  startTime: string;
  endTime?: string;
  duration?: number;
  steps: Array<{
    id: string;
    name: string;
    status: 'pending' | 'running' | 'success' | 'failed' | 'skipped';
    startTime?: string;
    endTime?: string;
    duration?: number;
    output?: string;
    error?: string;
  }>;
  businessImpact?: string;
}

export interface SystemMetricsOverviewDto {
  database?: { status?: string; isPostgres?: boolean };
  environment?: { os?: string; runtime?: string };
  audit?: { totalLogs?: number; activeUsers?: number };
}

export const systemMetricsApi = {
  getOverview: () => api.get<ApiResponse<SystemMetricsOverviewDto>>('/system-metrics/overview'),
  getAudit: () => api.get<ApiResponse<unknown>>('/system-metrics/audit'),
};

export interface SystemJobDto {
  id: string;
  jobType: string;
  status: string;
  progress: number;
  startedAt: string;
  completedAt?: string;
  message?: string;
  error?: string;
}

export const systemJobsApi = {
  list: (params?: { pageSize?: number }) => api.get<ApiResponse<SystemJobDto[]>>('/system/jobs', { params: { count: params?.pageSize } }),
  get: (id: string) => api.get<ApiResponse<SystemJobDto>>(`/system/jobs/${id}`),
};

export const agentApi = {
  metricsSummary: () => api.get<ApiResponse<AgentMetricsSummary>>('/agent/metrics-summary'),
  latencyHistory: (params?: { hours?: number }) => api.get<ApiResponse<Array<{ timestamp: string; p50: number; p95: number; p99: number }>>>('/agent/latency-history', { params }),
  qpsHistory: (params?: { hours?: number }) => api.get<ApiResponse<Array<{ timestamp: string; qps: number }>>>('/agent/qps-history', { params }),
  errorHistory: (params?: { hours?: number }) => api.get<ApiResponse<Array<{ timestamp: string; count: number; rate: number }>>>('/agent/error-history', { params }),
  flowExecutions: (params?: { flowType?: string; status?: string }) => api.get<ApiResponse<AgentFlowExecution[]>>('/agent/flow-executions', { params }),
  triggerFlow: (flowType: string, data: Record<string, unknown>) => api.post<ApiResponse<AgentFlowExecution>>(`/agent/flows/${flowType}/trigger`, data),
  topology: () => api.get<ApiResponse<Array<{ id: string; name: string; type: string; status: string; connections: string[] }>>>('/agent/topology'),
};

// ========== AI Planning ==========
export interface PlanTask {
  id: string;
  description: string;
  dependencies: string[];
  estimatedMinutes: number;
  toolsNeeded: string[];
  status: string;
  priority: number;
}

export interface PlanningRequest {
  taskDescription: string;
  context?: string;
  parameters?: Record<string, unknown>;
  modelId?: string;
}

export interface PlanningResponse {
  success: boolean;
  planId?: string;
  tasks: PlanTask[];
  strategy: string;
  confidence: number;
  errorMessage?: string;
  reasoning?: string[];
}

export interface ExecutionRequest {
  planId: string;
  taskId?: string;
  parallelExecution: boolean;
}

export interface TaskResult {
  taskId: string;
  status: string;
  output?: string;
  error?: string;
  duration: number;
  toolUsed?: string;
}

export interface ExecutionResponse {
  success: boolean;
  executionId?: string;
  results: TaskResult[];
  duration: number;
  errorMessage?: string;
}

export interface ReviewRequest {
  executionId: string;
  feedback?: string;
  autoApproval: boolean;
}

export interface ReviewResponse {
  approved: boolean;
  qualityScore: number;
  issues: string[];
  recommendations?: string;
  suggestedFixes?: string[];
}

export interface KpiValidationResult {
  accuracyRate: number;
  currentRoi: number;
  targetMet: boolean;
  metrics: Record<string, number>;
  trends: Record<string, number>;
  lastUpdated: string;
}

export interface AgentState {
  executionId: string;
  currentNode: string;
  status: string;
  context: Record<string, unknown>;
  history: string[];
  startedAt: string;
  completedAt?: string;
}

export interface ToolExecuteRequest {
  toolId: string;
  parameters?: Record<string, unknown>;
}

export interface ToolResult {
  success: boolean;
  data?: string;
  error?: string;
  elapsedMs: number;
}


// ========== Substitute Material ==========
export interface SubstituteGroupDto {
  id: string;
  groupCode: string;
  groupName?: string;
  description?: string;
  substitutionType: string;
  allowAutoSubstitute: boolean;
  isBidirectional: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  items: SubstituteItemDto[];
}

export interface SubstituteItemDto {
  id: string;
  groupId: string;
  groupCode?: string;
  skuId: string;
  skuNumber?: string;
  skuName?: string;
  priority: number;
  conversionRate?: number;
  isPrimary: boolean;
  status: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  notes?: string;
}

export interface SubstituteUsageLogDto {
  id: string;
  originalSkuId: string;
  originalSkuNumber?: string;
  originalSkuName?: string;
  substituteSkuId: string;
  substituteSkuNumber?: string;
  substituteSkuName?: string;
  workOrderId: string;
  woNumber?: string;
  originalQty: number;
  substituteQty: number;
  costVariance: number;
  reason: string;
  notes?: string;
  createdAt: string;
}

export const substituteApi = {
  groups: (params?: { includeItems?: boolean }) => api.get<ApiResponse<SubstituteGroupDto[]>>('/substitute/groups', { params }),
  getGroup: (id: string) => api.get<ApiResponse<SubstituteGroupDto>>(`/substitute/groups/${id}`),
  createGroup: (data: Record<string, unknown>) => api.post<ApiResponse<SubstituteGroupDto>>('/substitute/groups', data),
  updateGroup: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<SubstituteGroupDto>>(`/substitute/groups/${id}`, data),
  deleteGroup: (id: string) => api.delete<ApiResponse>(`/substitute/groups/${id}`),
  availableSubstitutes: (skuId: string, params?: { requiredQty?: number; requiredDate?: string }) =>
    api.get<ApiResponse<SubstituteItemDto[]>>(`/substitute/available/${skuId}`, { params }),
  useSubstitute: (data: { originalSkuId: string; substituteSkuId: string; workOrderId: string; qty: number; reason?: string }) =>
    api.post<ApiResponse<{ message: string; costVariance: number }>>('/substitute/use', data),
  history: (skuId: string, params?: { fromDate?: string }) =>
    api.get<ApiResponse<SubstituteUsageLogDto[]>>(`/substitute/history/${skuId}`, { params }),
};

// ========== MRP ==========
export interface MrpRunDto {
  id: string;
  runNumber: string;
  runType: string;
  planningHorizonStart: string;
  planningHorizonEnd: string;
  horizonStart?: string;
  horizonEnd?: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  createdBy?: string;
  totalItemsProcessed: number;
  plannedOrdersGenerated: number;
  purchaseRequisitionsGenerated: number;
  errorMessage?: string;
}

export interface MrpRequirementDto {
  id: string;
  skuId: string;
  skuNumber?: string;
  skuName?: string;
  requirementType: string;
  sourceDocumentNumber?: string;
  requiredDate: string;
  requiredQty: number;
  availableQty: number;
  shortageQty: number;
  netRequirement: number;
  actionCode: string;
  substituteSkuId?: string;
  notes?: string;
}

export interface MrpPlannedOrderDto {
  id: string;
  skuId: string;
  skuNumber?: string;
  skuName?: string;
  orderType: string;
  orderQty: number;
  plannedStartDate: string;
  plannedEndDate: string;
  status: string;
  convertedDocumentId?: string;
  convertedDocumentNumber?: string;
  notes?: string;
  createdAt: string;
}

export interface MrpTimelineEvent {
  date: string;
  type: 'demand' | 'supply';
  source: string;
  reference: string;
  description: string;
  changeQty: number;
  balanceAfter: number;
}

export interface MrpTimelineDto {
  initialStock: number;
  unit: string;
  events: MrpTimelineEvent[];
}

export const mrpApi = {
  // Standardized methods
  getRuns: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<MrpRunDto[]>>('/mrp/runs', { params }),
  // 修復 FE-001: 支援 server-side 搜尋
  getOrders: (runId: string, search?: string) => 
    api.get<ApiResponse<MrpPlannedOrderDto[]>>(`/mrp/runs/${runId}/orders`, { 
      params: search ? { search } : undefined 
    }),
  runMrp: (data: { horizonStart?: string; horizonEnd?: string; runType?: string }) => 
    api.post<ApiResponse<MrpRunDto>>('/mrp/run', data),
  convertOrder: (orderId: string) => 
    api.post<ApiResponse<{ message: string; documentNumber: string }>>(`/mrp/orders/${orderId}/convert`),
  getSummary: () => api.get<ApiResponse<any>>('/mrp/summary'),
  getRun: (runId: string) => api.get<ApiResponse<MrpRunDto>>(`/mrp/runs/${runId}`),

  // Legacy Aliases for compatibility
  runs: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<MrpRunDto[]>>('/mrp/runs', { params }),
  plannedOrders: (runId: string, params?: { orderType?: string; status?: string }) => 
    api.get<ApiResponse<MrpPlannedOrderDto[]>>(`/mrp/runs/${runId}/orders`, { params }),
  run: (data: any) => api.post<ApiResponse<MrpRunDto>>('/mrp/run', data),
  confirmPlannedOrder: (orderId: string) => 
    api.post<ApiResponse<{ message: string; documentNumber: string }>>(`/mrp/orders/${orderId}/convert`),
  requirements: (runId: string, params?: any) => 
    api.get<ApiResponse<MrpRequirementDto[]>>(`/mrp/runs/${runId}/requirements`, { params }),
};

// ========== APS ==========
export interface ProductionScheduleDto {
  id: string;
  scheduleNumber: string;
  scheduleType: string;
  scheduleDate: string;
  horizonStart: string;
  horizonEnd: string;
  status: string;
  createdAt: string;
  activatedAt?: string;
  totalTasks: number;
  totalCapacityLoads: number;
  unresolvedConstraints: number;
  tasks: ScheduleTaskDto[];
}

export interface ScheduleTaskDto {
  id: string;
  workOrderId: string;
  operationSeq: number;
  workCenterId: string;
  workCenterName?: string;
  plannedStart: string;
  plannedEnd: string;
  setupHours: number;
  runHours: number;
  totalHours: number;
  quantity: number;
  status: string;
  priority: number;
  sequenceType: string;
  mrpPlannedStart?: string;
}

export interface CapacityLoadDto {
  id: string;
  workCenterId: string;
  workCenterName?: string;
  loadDate: string;
  shiftNumber: number;
  availableHours: number;
  scheduledHours: number;
  loadedHours: number;
  loadPercentage: number;
  loadStatus: string;
  capacityUtilization?: number;
}



export interface ScheduleConstraintDto {
  id: string;
  constraintType: string;
  description: string;
  resourceId?: string;
  resourceType?: string;
  startDate?: string;
  endDate?: string;
  severity: string;
  isResolved: boolean;
  resolvedAt?: string;
  resolutionNotes?: string;
}

export interface GanttTaskDto {
  id: string;
  taskName: string;
  workOrderId: string;
  woNumber: string;
  workCenterId: string;
  workCenterName?: string;
  start: string;
  end: string;
  progress: number;
  status: string;
  priority: number;
  setupHours: number;
  runHours: number;
  totalHours: number;
  quantity: number;
}

export const apsApi = {
  generateSchedule: (data: { horizonStart: string; horizonEnd: string; scheduleType?: string }) =>
    api.post<ApiResponse<ProductionScheduleDto>>('/aps/schedules/generate', data),
  schedules: (params?: { page?: number; pageSize?: number }) =>
    api.get<ApiResponse<ProductionScheduleDto[]>>('/aps/schedules', { params }),
  getSchedule: (scheduleId: string) => api.get<ApiResponse<ProductionScheduleDto>>(`/aps/schedules/${scheduleId}`),
  optimizeSchedule: (scheduleId: string, data: { primaryGoal?: string; respectPriority?: boolean; allowRescheduling?: boolean; notes?: string }) =>
    api.post<ApiResponse<ProductionScheduleDto>>(`/aps/schedules/${scheduleId}/optimize`, data),
  rescheduleTask: (taskId: string, data: { newStart: string; newEnd: string }) =>
    api.post<ApiResponse<{ message: string; affectedTasks: number }>>(`/aps/tasks/${taskId}/reschedule`, data),
  capacityLoads: (scheduleId: string, params?: { workCenterId?: string }) =>
    api.get<ApiResponse<CapacityLoadDto[]>>(`/aps/schedules/${scheduleId}/capacity-loads`, { params }),
  constraints: (scheduleId: string, params?: { includeResolved?: boolean }) =>
    api.get<ApiResponse<ScheduleConstraintDto[]>>(`/aps/schedules/${scheduleId}/constraints`, { params }),
  resolveConstraint: (scheduleId: string, constraintId: string, data: { resolutionNotes?: string }) =>
    api.post<ApiResponse<{ message: string }>>(`/aps/schedules/${scheduleId}/constraints/${constraintId}/resolve`, data),
  ganttData: (workCenterId: string, params: { startDate: string; endDate: string }) =>
    api.get<ApiResponse<GanttTaskDto[]>>(`/aps/work-centers/${workCenterId}/gantt-data`, { params }),
  acceptWorkOrder: (scheduleId: string, data: { workOrderId: string }) =>
    api.post<ApiResponse<{ message: string }>>(`/aps/schedules/${scheduleId}/accept-work-order`, data),
  export: () => api.get<ApiResponse<any>>('/aps/export'),
  importSchedule: (data: any) => api.post<ApiResponse>('/aps/import-schedule', data),
};

// ========== KPI ==========
export interface KpiDashboardDto {
  periodStart: string;
  periodEnd: string;
  averageOee: number;
  oeeByWorkCenter: OeeSummaryDto[];
  oeeTrend: KpiTrendDto[];
  onTimeDeliveryRate: number;
  totalOrders: number;
  onTimeOrders: number;
  delayedOrders: number;
  otdTrend: KpiTrendDto[];
  wipQuantity: number;
  wipValue: number;
  wipByWorkCenter: Record<string, number>;
  scrapQuantity: number;
  scrapAmount: number;
  scrapByReason: Record<string, number>;
  topScrapItems: TopScrapItemDto[];
  capacityUtilization: number;
}

export interface OeeSummaryDto {
  workCenterId: string;
  workCenterName: string;
  availability: number;
  performance: number;
  quality: number;
  oeeValue: number;
  status: string;
}

export interface OeeResultDto {
  workCenterId: string;
  recordDate: string;
  shift?: number;
  plannedRunTime: number;
  actualRunTime: number;
  downtime: number;
  setupTime: number;
  waitTime: number;
  theoreticalOutput: number;
  actualOutput: number;
  goodOutput: number;
  defectOutput: number;
  availability: number;
  performance: number;
  quality: number;
  oeeValue: number;
}

export interface OtdResultDto {
  periodStart: string;
  periodEnd: string;
  totalOrders: number;
  onTimeOrders: number;
  delayedOrders: number;
  onTimeRate: number;
  averageDelayDays: number;
}

export interface WipResultDto {
  totalWipQuantity: number;
  totalWipValue: number;
  wipRatio: number;
  wipByWorkCenter: Record<string, number>;
  wipOrdersCount: number;
}

export interface ScrapResultDto {
  periodStart: string;
  periodEnd: string;
  totalScrapQuantity: number;
  totalScrapAmount: number;
  scrapByReason: Record<string, number>;
  topScrapItems: TopScrapItemDto[];
}

export interface TopScrapItemDto {
  skuId: string;
  skuNumber?: string;
  skuName?: string;
  scrapQuantity: number;
  scrapAmount: number;
}

export interface KpiTrendDto {
  periodStart: string;
  periodEnd: string;
  value: number;
  targetValue?: number;
  achievementRate?: number;
  status: string;
}

export interface WorkCenterDto {
  id: string;
  workCenterCode: string;
  workCenterName: string;
  capacity: number;
  efficiency: number;
}

export const kpiApi = {
  dashboard: (params?: { fromDate?: string; toDate?: string }) =>
    api.get<ApiResponse<KpiDashboardDto>>('/kpi/dashboard', { params }),
  oee: (workCenterId: string, params?: { date?: string; shift?: number }) =>
    api.get<ApiResponse<OeeResultDto>>(`/kpi/oee/${workCenterId}`, { params }),
  oeeHistory: (workCenterId: string, params?: { fromDate?: string; toDate?: string }) =>
    api.get<ApiResponse<Array<{ date: string; totalPlannedTime: number; totalActualTime: number; totalDowntime: number; totalOutput: number; goodOutput: number }>>>(
      `/kpi/oee/${workCenterId}/history`, { params }),
  otd: (params?: { fromDate?: string; toDate?: string }) =>
    api.get<ApiResponse<OtdResultDto>>('/kpi/otd', { params }),
  wip: () => api.get<ApiResponse<WipResultDto>>('/kpi/wip'),
  scrap: (params?: { fromDate?: string; toDate?: string }) =>
    api.get<ApiResponse<ScrapResultDto>>('/kpi/scrap', { params }),
  trends: (kpiCode: string, params?: { fromDate?: string; toDate?: string; periodType?: string }) =>
    api.get<ApiResponse<KpiTrendDto[]>>(`/kpi/trends/${kpiCode}`, { params }),
  definitions: () => api.get<ApiResponse<Array<{ id: string; kpiCode: string; kpiName: string; description?: string; category: string; unit: string; targetValue?: number; warningThreshold?: number; criticalThreshold?: number }>>>('/kpi/definitions'),
  createDefinition: (data: Record<string, unknown>) => api.post<ApiResponse>('/kpi/definitions', data),
  workCenters: () => api.get<ApiResponse<WorkCenterDto[]>>('/kpi/work-centers'),
  addMachineOperationRecord: (data: Record<string, unknown>) => api.post<ApiResponse<{ id: string }>>('/kpi/records/machine-operation', data),
  updateMachineOperationRecord: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse<{ id: string }>>(`/kpi/records/machine-operation/${id}`, data),
};

export interface OperationDto {
  id: string;
  operationCode: string;
  operationName: string;
  setupTime: number;
  runTime: number;
  costPerHour: number;
  isOutsourced: boolean;
  isActive: boolean;
}

export interface RoutingDto {
  id: string;
  routingCode: string;
  routingName: string;
  productSkuId: string;
  productSkuName: string;
  productSkuNumber: string;
  version: string;
  isActive: boolean;
  operations: Array<{
    operationSeq: number;
    operationId: string;
    operationName: string;
    operationCode: string;
    workCenterId: string;
    workCenterName: string;
    workCenterCode: string;
    transferTime: number;
    waitTime: number;
  }>;
}

export const operationsApi = {
  list: (params?: { page?: number; pageSize?: number }) =>
    api.get<ApiResponse<OperationDto[]>>('/operations', { params }),
  get: (id: string) => api.get<ApiResponse<OperationDto>>(`/operations/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<OperationDto>>('/operations', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<OperationDto>>(`/operations/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/operations/${id}`),
};

export const routingsApi = {
  list: (params?: { page?: number; pageSize?: number }) =>
    api.get<ApiResponse<RoutingDto[]>>('/routings', { params }),
  get: (id: string) => api.get<ApiResponse<RoutingDto>>(`/routings/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<RoutingDto>>('/routings', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<RoutingDto>>(`/routings/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/routings/${id}`),
};

// ========== MPS ==========
export interface MpsPlanDto {
  id: string;
  planNumber: string;
  productSkuId: string;
  productSkuName: string;
  productSkuNumber: string;
  quantity: number;
  startDate: string;
  endDate: string;
  priority: string;
  status: string;
  sourceType: string;
  salesOrderId?: string;
  notes?: string;
  createdAt: string;
}

export interface SalesForecastDto {
  id: string;
  productSkuId: string;
  productSkuName: string;
  year: number;
  month: number;
  forecastQty: number;
  actualQty: number;
  sourceType: string;
}

export const mpsApi = {
  plans: (params?: { status?: string }) =>
    api.get<ApiResponse<MpsPlanDto[]>>('/mps/plans', { params }),
  getPlan: (id: string) => api.get<ApiResponse<MpsPlanDto>>(`/mps/plans/${id}`),
  createPlan: (data: { productSkuId: string; quantity: number; startDate: string; endDate: string; priority?: string; sourceType?: string; notes?: string }) =>
    api.post<ApiResponse<{ id: string; planNumber: string }>>('/mps/plans', data),
  updatePlan: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse>(`/mps/plans/${id}`, data),
  releasePlan: (id: string) =>
    api.post<ApiResponse<{ id: string; woNumber: string }>>(`/mps/plans/${id}/release`),
  deletePlan: (id: string) => api.delete<ApiResponse>(`/mps/plans/${id}`),
  forecasts: (params?: { year?: number; month?: number }) =>
    api.get<ApiResponse<SalesForecastDto[]>>('/mps/forecasts', { params }),
  createForecast: (data: { productSkuId: string; year: number; month: number; forecastQty: number; sourceType?: string; notes?: string }) =>
    api.post<ApiResponse<{ id: string }>>('/mps/forecasts', data),
  generate: (data?: { 
    productSkuIds?: string[]; 
    startDate?: string;
    endDate?: string;
    includeForecast?: boolean;
    includeSalesOrders?: boolean;
    includeSafetyStock?: boolean;
    aggregationPeriod?: 'daily' | 'weekly' | 'monthly';
    generationMode?: 'regenerative' | 'net_change';
  }) =>
    api.post<ApiResponse<{ generated: number; plans: unknown[] }>>('/mps/generate', data),
  list: () => api.get<ApiResponse<MpsHeaderDto[]>>('/mps'),
  get: (id: string) => api.get<ApiResponse<MpsDetailDto>>(`/mps/${id}`),
  create: (data: CreateMpsRequest) => api.post<ApiResponse<MpsHeaderDto>>('/mps', data),
  runConsumption: (id: string) => api.post<ApiResponse>(`/mps/${id}/run-consumption`),
  getExceptions: (id: string) => api.get<ApiResponse<any[]>>(`/mps/${id}/exceptions`),
  getRccpStatus: (id: string) => api.get<ApiResponse<RccpStatusDto>>(`/mps/${id}/rccp`),
  getAggregateRccp: (start?: string, end?: string) => api.get<ApiResponse<AggregateRccpDto>>('/mps/aggregate-rccp', { params: { start, end } }),
};

// ========== Barcode ==========
export interface BarcodeRecordDto {
  id: string;
  barcode: string;
  barcodeType: string;
  refId?: string;
  refType?: string;
  refName?: string;
  locationCode?: string;
  qty?: number;
  status: string;
  lastScanTime: string;
}

export interface BarcodeScanHistoryDto {
  id: string;
  barcode: string;
  barcodeType: string;
  action: string;
  result: string;
  operatorName?: string;
  details?: string;
  timestamp: string;
}

export const barcodeApi = {
  lookup: (barcode: string) =>
    api.get<ApiResponse<BarcodeRecordDto>>(`/barcode/lookup/${barcode}`),
  scan: (data: { barcode: string; barcodeType?: string; action?: string; qty?: number; locationCode?: string; details?: string }) =>
    api.post<ApiResponse<BarcodeRecordDto>>('/barcode/scan', data),
  records: (params?: { type?: string; status?: string }) =>
    api.get<ApiResponse<BarcodeRecordDto[]>>('/barcode/records', { params }),
  generate: (data: { prefix?: string; barcodeType?: string; refId?: string; refType?: string; refName?: string; locationCode?: string; qty?: number }) =>
    api.post<ApiResponse<{ barcode: string; type: string }>>('/barcode/generate', data),
  generateBatch: (data: { barcodeType: string; refId?: string; refType?: string; refName?: string; quantity: number; qtyPerBarcode?: number }) =>
    api.post<ApiResponse<{ generated: number; barcodes: string[] }>>('/barcode/generate/batch', data),
  history: (params?: { page?: number; pageSize?: number }) =>
    api.get<ApiResponse<{ data: BarcodeScanHistoryDto[]; total: number; page: number; pageSize: number; totalPages: number }>>('/barcode/history', { params }),
  historyByBarcode: (barcode: string) =>
    api.get<ApiResponse<BarcodeScanHistoryDto[]>>(`/barcode/history/${barcode}`),
};

// ========== Equipment / TPM ==========
export interface EquipmentDto {
  id: string;
  equipmentCode: string;
  equipmentName: string;
  equipmentType: string;
  serialNumber?: string;
  model?: string;
  manufacturer?: string;
  purchaseDate?: string;
  purchasePrice: number;
  status: string;
  maintenanceStatus: string;
  lastMaintenanceDate?: string;
  nextMaintenanceDate?: string;
  workCenterName?: string;
}

export interface MaintenanceRecordDto {
  id: string;
  recordNumber: string;
  equipmentId: string;
  equipmentName: string;
  equipmentCode: string;
  maintenanceType: string;
  startTime: string;
  endTime?: string;
  performedBy?: string;
  workDescription?: string;
  partsReplaced?: string;
  laborHours: number;
  partsCost: number;
  totalCost: number;
  result: string;
  remarks?: string;
}

export interface EquipmentDowntimeDto {
  id: string;
  downtimeNumber: string;
  equipmentId: string;
  equipmentName: string;
  equipmentCode: string;
  downtimeType: string;
  startTime: string;
  endTime?: string;
  cause?: string;
  description?: string;
  status: string;
  resolution?: string;
}

export interface EquipmentStatsDto {
  total: number;
  active: number;
  maintenance: number;
  repair: number;
  openDowntime: number;
  warningMaintenance: number;
  criticalMaintenance: number;
  maintenanceCost: number;
}

export const equipmentApi = {
  list: (params?: { status?: string; type?: string }) =>
    api.get<ApiResponse<EquipmentDto[]>>('/equipment/equipment', { params }),
  get: (id: string) => api.get<ApiResponse<EquipmentDto>>(`/equipment/equipment/${id}`),
  create: (data: { equipmentCode: string; equipmentName: string; equipmentType?: string; workCenterId?: string; serialNumber?: string; model?: string; manufacturer?: string; supplierId?: string; purchaseDate?: string; purchasePrice?: number }) =>
    api.post<ApiResponse<{ id: string; equipmentCode: string }>>('/equipment/equipment', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse>(`/equipment/equipment/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/equipment/equipment/${id}`),
  maintenancePlans: (params?: { equipmentId?: string }) =>
    api.get<ApiResponse>('/equipment/maintenance/plans', { params }),
  createMaintenancePlan: (data: { equipmentId: string; maintenanceType?: string; frequency?: string; frequencyDays?: number; description?: string; checkpointList?: string; nextDueDate?: string }) =>
    api.post<ApiResponse>('/equipment/maintenance/plans', data),
  maintenanceRecords: (params?: { equipmentId?: string; type?: string }) =>
    api.get<ApiResponse<MaintenanceRecordDto[]>>('/equipment/maintenance/records', { params }),
  createMaintenanceRecord: (data: { equipmentId: string; maintenancePlanId?: string; maintenanceType?: string; startTime: string; endTime?: string; performedBy?: string; workDescription?: string; partsReplaced?: string; laborHours: number; partsCost?: number; result?: string; remarks?: string }) =>
    api.post<ApiResponse<{ id: string; recordNumber: string }>>('/equipment/maintenance/records', data),
  downtime: (params?: { equipmentId?: string; status?: string }) =>
    api.get<ApiResponse<EquipmentDowntimeDto[]>>('/equipment/downtime', { params }),
  createDowntime: (data: { equipmentId: string; workOrderId?: string; startTime: string; downtimeType?: string; cause?: string; description?: string; reportedBy?: string }) =>
    api.post<ApiResponse<{ id: string; downtimeNumber: string }>>('/equipment/downtime', data),
  resolveDowntime: (id: string, data: { endTime?: string; assignedTo?: string; resolution?: string }) =>
    api.put<ApiResponse>(`/equipment/downtime/${id}/resolve`, data),
  stats: () => api.get<ApiResponse<EquipmentStatsDto>>('/equipment/stats'),
};

// ========== Quality Trace ==========
export interface LotTraceDto {
  lotNumber: string;
  iqc?: {
    reportNumber: string;
    supplierName: string;
    inspectionDate: string;
    result: string;
    items: { inspectionItem: string; result: string }[];
  };
  ipqc?: {
    reportNumber: string;
    workOrderId: string;
    inspectionDate: string;
    result: string;
  };
  complaint?: {
    complaintNumber: string;
    supplierName: string;
    defectType: string;
    status: string;
  };
  production: { workOrderId: string; operationSeq: number; completedQty: number; materialLotNumber: string }[];
}

export interface MaterialTraceDto {
  skuId: string;
  usedInProducts: { skuId: string }[];
  purchaseHistory: { type: string; supplierName: string; poNumber: string; orderDate: string; quantity: number; unitPrice: number }[];
}
