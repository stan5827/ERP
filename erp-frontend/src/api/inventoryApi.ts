import { api, type ApiResponse } from './baseApi';

export interface SkuDto {
  id: string;
  skuNumber: string;
  skuName: string;
  specification?: string;
  itemType: string;
  sourceType: string;
  baseUomId: string;
  baseUomName?: string;
  qty: number;
  status: string;
}

export const skusApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<SkuDto[]>>('/skus', { params }),
  get: (id: string) => api.get<ApiResponse<SkuDto>>(`/skus/${id}`),
  create: (data: any) => api.post<ApiResponse>('/skus', data),
  update: (id: string, data: any) => api.put<ApiResponse>(`/skus/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/skus/${id}`),
};

export const warehousesApi = {
  list: () => api.get<ApiResponse<any[]>>('/warehouses'),
  create: (data: any) => api.post<ApiResponse>('/warehouses', data),
};

export const inventoryApi = {
  getTransactions: (params?: Record<string, string>) => api.get<ApiResponse<any[]>>('/inventorytransactions', { params }),
  getTransfers: () => api.get<ApiResponse<any[]>>('/inventorytransfers'),
  createTransfer: (data: any) => api.post<ApiResponse>('/inventorytransfers', data),
};
