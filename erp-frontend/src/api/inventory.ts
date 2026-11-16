import { api, ApiResponse, toPascalCase } from './client';

export interface SkuDto {
  id: string; skuNumber: string; skuName: string; specification?: string; barcode?: string;
  unit: string; lotControlled: boolean; serialControlled: boolean; sourceType?: string;
  safetyStock: number; reorderPoint: number; maxStock: number; leadTimeDays: number;
  purchasePrice: number; standardCost: number; status: string; qty: number;
  abcClassification?: string; defaultWarehouseId?: string; defaultWarehouseName?: string;
  defaultBinLocationId?: string;
  createdAt: string;
  category?: string; itemType: string; model?: string; brand?: string;
  isSellable: boolean; isPurchasable: boolean; isManufacturable: boolean; isOutsourced: boolean;
  costMethod: string; suggestedPrice: number; basePrice: number;
  priceA?: number; priceB?: number; priceC?: number; priceD?: number; priceE?: number;
  currency: string; isBomTop: boolean; isBomComponent: boolean; isPhantom: boolean;
  isMto: boolean; isMts: boolean; isCustom: boolean; makeOrBuy: string;
  defaultSupplierId?: string; defaultSupplierName?: string; supplierItemCode?: string;
  supplierLeadTime?: number; drawingNo?: string; drawingRevision?: string;
  effectiveFrom?: string; effectiveTo?: string; qualitySpec?: string;
  productionDept?: string; machineCode?: string; moldCode?: string;
  costUnit?: string; latestPurchaseCost: number; taxCode?: string; taxRate?: string;
  isSubstitute: boolean; isAlternate: boolean; iqcRequired: boolean; oqcRequired: boolean;
  color?: string; packingMethod?: string; minPackingQty?: number; isStocked: boolean;
  materialType?: string; designOwner?: string;
  baseUomCode?: string; baseUomName?: string; baseUomId?: string;
  purchaseUomCode?: string; purchaseUomName?: string; purchaseUomId?: string;
  salesUomCode?: string; salesUomName?: string; salesUomId?: string;
  productionUomCode?: string; productionUomName?: string; productionUomId?: string;
  purchaseToBaseFactor?: number; salesToBaseFactor?: number; productionToBaseFactor?: number;
  inventoryQty?: number;
}
export interface WarehouseDto { id: string; warehouseCode: string; warehouseName: string; address?: string; type: string; isActive: boolean; binLocationCount: number; }
export interface InventoryTransactionDto {
  trx_id: string; sku_code: string; sku_name: string; type: string;
  qty_change: number; qty_before: number; qty_after: number;
  warehouse: string; bin_location?: string; lot_number?: string;
  notes?: string; created_at: string;
}
export interface InventoryStocktakeBalanceDto {
  skuCode: string;
  skuName: string;
  warehouseCode: string;
  lotNumber: string;
  expiryDate?: string;
  systemQty: number;
}
export interface InventoryStocktakeResultDto {
  trxId?: string;
  skuCode: string;
  warehouseCode: string;
  lotNumber: string;
  qtyBefore: number;
  countedQty: number;
  qtyChange: number;
  qtyAfter: number;
  expiryDate?: string;
}
export interface InventoryWarehouseBalanceDto {
  warehouseCode?: string;
  warehouseName?: string;
  binLocationCode?: string;
  totalQty: number;
}
export interface SkuBalanceDto {
  by_warehouse?: InventoryWarehouseBalanceDto[];
}
export interface AvailableToPromiseDto {
  allocated: number;
  available: number;
  is_sufficient?: boolean;
}

export interface TransferOrderDto {
  id: string; transferNumber: string; status: string;
  sourceWarehouseId: string; targetWarehouseId: string;
  approvedBy?: string; createdBy?: string; createdAt: string;
  items: TransferOrderItemDto[];
}
export interface TransferOrderItemDto {
  id: string; skuId: string; skuName?: string; skuNumber?: string;
  transferQty: number; lotNumber?: string;
}
export interface CompletionOrderDto {
  id: string; completionNumber: string; workOrderId: string; warehouseId: string;
  status: string; qaStatus: string; receivedBy?: string; createdAt: string;
  items: CompletionOrderItemDto[];
}
export interface CompletionOrderItemDto {
  id: string; skuId: string; skuName?: string; skuNumber?: string;
  receivedQty: number; lotNumber?: string; manufactureDate?: string; expiryDate?: string;
}
export interface CompletableWorkOrderDto {
  id: string; woNumber: string; productSkuId: string; plannedQty: number;
  actualQty: number; plannedEndDate?: string; status: string;
}
export interface BinLocationDto {
  id: string; warehouseId: string; warehouseCode: string; warehouseName: string;
  locationCode: string; zone?: string; aisle?: string; rack?: string; level?: string;
  position?: string; maxWeight?: number; gridX?: string; gridY?: string;
  isActive: boolean; createdAt: string; updatedAt: string;
}
export interface MaterialIssueDto {
  id: string; issueNumber: string; workOrderId: string; workOrderNumber?: string;
  issueWarehouseId?: string; warehouseCode?: string; warehouseName?: string; bomVersion?: string;
  status: string; createdAt: string; issuedAt?: string; reversalOfIssueId?: string; notes?: string;
  items: MaterialIssueItemDto[];
}
export interface MaterialIssueItemDto {
  id: string; skuId: string; skuNumber?: string; skuName?: string; workOrderMaterialId?: string;
  unit?: string; inventoryUnit?: string; requiredQty: number; issuedQty: number; shortageQty: number;
  closedShortageQty: number; status: string; shortageClosureReason?: string;
  defaultWarehouseId?: string; defaultBinLocationId?: string; lotControlled: boolean;
  serialControlled: boolean; onHandQty: number; defaultIssueQty: number;
  allocations: MaterialIssueAllocationDto[];
}
export interface MaterialIssueAllocationDto {
  id: string; warehouseId: string; warehouseCode?: string; warehouseName?: string;
  binLocationId?: string; binLocationCode?: string; lotNumber?: string; issuedQty: number;
  unitCost: number; inventoryTransactionId?: string;
}

export const materialIssuesApi = {
  list: (workOrderId?: string) => api.get<ApiResponse<MaterialIssueDto[]>>(
    '/material-issues', { params: workOrderId ? { workOrderId } : undefined }),
  get: (id: string) => api.get<ApiResponse<MaterialIssueDto>>(`/material-issues/${id}`),
  createDraft: (workOrderId: string) =>
    api.post<ApiResponse<MaterialIssueDto>>(`/material-issues/work-orders/${workOrderId}/draft`),
  post: (id: string, allocations: Array<{
    issueItemId: string; warehouseId: string; binLocationId?: string;
    lotNumber?: string; issuedQty: number;
  }>) => api.post<ApiResponse<MaterialIssueDto>>(`/material-issues/${id}/post`, { allocations }),
  closeShortage: (id: string, itemId: string, quantity: number, reason: string) =>
    api.post<ApiResponse<MaterialIssueDto>>(
      `/material-issues/${id}/items/${itemId}/close-shortage`, { quantity, reason }),
  cancel: (id: string) => api.post<ApiResponse<MaterialIssueDto>>(`/material-issues/${id}/cancel`),
  reverse: (id: string) => api.post<ApiResponse<MaterialIssueDto>>(`/material-issues/${id}/reverse`),
};

export const skusApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<SkuDto[]>>('/skus', { params }),
  get: (id: string) => api.get<ApiResponse<SkuDto>>(`/skus/${id}`),
  create: async (data: Record<string, unknown>) => {
    const pascalData = toPascalCase(data);
    const response = await api.post<ApiResponse>('/skus', pascalData);
    if (!response.data?.success) throw new Error(response.data?.message || '建立失敗');
    return response.data;
  },
  update: async (id: string, data: Record<string, unknown>) => {
    const response = await api.put<ApiResponse>(`/skus/${id}`, toPascalCase(data));
    if (!response.data?.success) throw new Error(response.data?.message || '更新失敗');
    return response.data;
  },
  all: () => api.get<ApiResponse<SkuDto[]>>('/skus?pageSize=9999'),
  allPurchasable: () => api.get<ApiResponse<SkuDto[]>>('/skus?pageSize=100&isPurchasable=true'),
};

export const warehousesApi = {
  list: (params?: { includeInactive?: boolean }) => api.get<ApiResponse<WarehouseDto[]>>('/warehouses', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/warehouses', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/warehouses/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/warehouses/${id}`),
  activate: (id: string) => api.put<ApiResponse>(`/warehouses/${id}/activate`),
};

export const inventoryApi = {
  getSkuBalance: (sku_code: string) => api.get<ApiResponse<SkuBalanceDto>>(`/inventory/balance/${sku_code}`),
  getStocktakeBalance: (skuCode: string, warehouseCode: string, lotNumber: string) =>
    api.get<ApiResponse<InventoryStocktakeBalanceDto>>('/inventory/stocktake/balance', {
      params: { skuCode, warehouseCode, lotNumber },
    }),
  postStocktake: (data: {
    skuCode: string;
    warehouseCode: string;
    lotNumber: string;
    expiryDate: string;
    countedQty: number;
    expectedSystemQty: number;
    notes?: string;
  }) => api.post<ApiResponse<InventoryStocktakeResultDto>>('/inventory/stocktake', data),
  checkATP: (sku_code: string, requested_qty: number) => 
    api.get<ApiResponse<AvailableToPromiseDto>>('/inventory/atp', { params: { sku_code, requested_qty } }),
  adjust: (data: Record<string, unknown>) => api.post<ApiResponse>('/inventory/adjust', data),
  transactions: (params?: Record<string, string>) => api.get<ApiResponse<InventoryTransactionDto[]>>('/inventory/transactions', { params }),
  lowStock: () => api.get<ApiResponse<SkuDto[]>>('/inventory/low-stock'),
  valuation: () => api.get<ApiResponse<{ totalValue: number }>>('/inventory/valuation'),
};

export const inventoryTransfersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<TransferOrderDto[]>>('/InventoryTransfers', { params }),
  get: (id: string) => api.get<ApiResponse<TransferOrderDto>>(`/InventoryTransfers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<TransferOrderDto>>('/InventoryTransfers', data),
  approve: (id: string) => api.post<ApiResponse<TransferOrderDto>>(`/InventoryTransfers/${id}/approve`),
  execute: (id: string) => api.post<ApiResponse<TransferOrderDto>>(`/InventoryTransfers/${id}/execute`),
  cancel: (id: string) => api.delete<ApiResponse<boolean>>(`/InventoryTransfers/${id}`),
};

export const completionOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<CompletionOrderDto[]>>('/completion-orders', { params }),
  get: (id: string) => api.get<ApiResponse<CompletionOrderDto>>(`/completion-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<CompletionOrderDto>>('/completion-orders', data),
  complete: (id: string) => api.post<ApiResponse<CompletionOrderDto>>(`/completion-orders/${id}/complete`),
  cancel: (id: string) => api.delete<ApiResponse<boolean>>(`/completion-orders/${id}`),
  getCompletableWorkOrders: () => api.get<ApiResponse<CompletableWorkOrderDto[]>>('/completion-orders/workorders/completable'),
};

export const binLocationsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<BinLocationDto[]>>('/bin-locations', { params }),
  get: (id: string) => api.get<ApiResponse<BinLocationDto>>(`/bin-locations/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/bin-locations', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/bin-locations/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/bin-locations/${id}`),
  activate: (id: string) => api.put<ApiResponse>(`/bin-locations/${id}/activate`),
  getByWarehouse: (warehouseId: string) => api.get<ApiResponse<BinLocationDto[]>>('/bin-locations/warehouse/' + warehouseId),
};
