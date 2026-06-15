import { api } from './api';
import { purchaseRequisitionsApi } from './api';
import type { ApiResponse, GoodsReceiptDto, PurchaseOrderDto, SupplierAsnDto, SupplierDto } from './api';

export interface CreateSupplierAsnItemPayload {
  skuId?: string;
  skuNumber: string;
  skuName: string;
  shippedQty: number;
  batchNo?: string;
}

export interface CreateSupplierAsnPayload {
  asnNumber?: string;
  purchaseOrderId?: string;
  expectedArrivalDate: string;
  carrierName?: string;
  trackingNumber?: string;
  remarks?: string;
  items: CreateSupplierAsnItemPayload[];
}

export interface ReceiveSupplierAsnPayload {
  warehouseCode?: string;
  supplierInvoiceNo?: string;
  packingListNo?: string;
  notes?: string;
}

export const procurementPurchaseOrdersApi = {
  list: (params?: Record<string, string>) =>
    api.get<ApiResponse<PurchaseOrderDto[]>>('/purchase-orders', { params }),
  get: (id: string) =>
    api.get<ApiResponse<PurchaseOrderDto>>(`/purchase-orders/${id}`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse>('/purchase-orders', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse>(`/purchase-orders/${id}`, data),
  confirm: (poNumber: string) =>
    api.post<ApiResponse>(`/purchase-orders/${poNumber}/confirm`),
  receive: (poNumber: string, warehouseCode?: string) =>
    api.post<ApiResponse>(`/purchase-orders/${poNumber}/receive`, { warehouse_code: warehouseCode }),
};

export const procurementSuppliersApi = {
  list: (params?: Record<string, string>) =>
    api.get<ApiResponse<SupplierDto[]>>('/suppliers', { params }),
  get: (id: string) =>
    api.get<ApiResponse<SupplierDto>>(`/suppliers/${id}`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse>('/suppliers', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse>(`/suppliers/${id}`, data),
};

export const supplierAsnsApi = {
  list: (params?: Record<string, string>) =>
    api.get<ApiResponse<SupplierAsnDto[]>>('/supplier-asn', { params }),
  get: (id: string) =>
    api.get<ApiResponse<SupplierAsnDto>>(`/supplier-asn/${id}`),
  create: (data: CreateSupplierAsnPayload & { purchaseOrderId: string; items: Array<{ skuId: string; shippedQty: number; lotNumber?: string; manufactureDate?: string; expiryDate?: string }> }) =>
    api.post<ApiResponse<SupplierAsnDto>>('/supplier-asn', data),
  updateStatus: (id: string, status: string) =>
    api.patch<ApiResponse>(`/supplier-asn/${id}/status`, { status }),
  receive: (id: string, data: ReceiveSupplierAsnPayload) =>
    api.post<ApiResponse<GoodsReceiptDto>>(`/GoodsReceipts/from-asn/${id}`, data),
};

export const goodsReceiptsApi = {
  list: (params?: Record<string, string>) =>
    api.get<ApiResponse<GoodsReceiptDto[]>>('/GoodsReceipts', { params }),
  get: (id: string) =>
    api.get<ApiResponse<GoodsReceiptDto>>(`/GoodsReceipts/${id}`),
  receiveFromAsn: (asnId: string, data: ReceiveSupplierAsnPayload) =>
    api.post<ApiResponse<GoodsReceiptDto>>(`/GoodsReceipts/from-asn/${asnId}`, data),
};

export const supplierPortalProcurementApi = {
  purchaseOrders: () =>
    api.get<ApiResponse<PurchaseOrderDto[]>>('/supplier-portal/purchase-orders'),
  confirmPurchaseOrder: (id: string) =>
    api.post<ApiResponse>(`/supplier-portal/purchase-orders/${id}/confirm`),
  asns: () =>
    api.get<ApiResponse<SupplierAsnDto[]>>('/supplier-portal/asns'),
  createAsn: (data: CreateSupplierAsnPayload) =>
    api.post<ApiResponse<string>>('/supplier-portal/asns', data),
  createAsnLegacy: (data: CreateSupplierAsnPayload & { supplierId: string; notes?: string }) =>
    api.post<ApiResponse<string>>('/supplier-portal/asn/create', data),
};

export { purchaseRequisitionsApi };
