import { api, ApiResponse, toPascalCase } from './client';

export interface SupplierDto { id: string; supplierCode: string; supplierName: string; taxId?: string; contactPerson?: string; phone?: string; email?: string; address?: string; paymentTerms?: string; currency: string; rating?: string; invoiceAddress?: string; deliveryAddress?: string; status: string; createdAt: string; }
export interface PurchaseOrderDto { id: string; poNumber: string; supplierId: string; supplierName?: string; poDate: string; currency: string; exchangeRate: number; taxCode?: string; paymentTerms?: string; tradeTerms?: string; subtotal: number; taxAmount: number; totalAmount: number; expectedDeliveryDate?: string; deliveryWarehouseId?: string; status: string; isLocked: boolean; createdAt: string; items: PurchaseOrderItemDto[]; }
export interface PurchaseOrderDetailDto extends PurchaseOrderDto {
  Id?: string;
  PoNumber?: string;
  Items?: PurchaseOrderItemDto[];
}
export interface PurchaseOrderItemDto { id: string; skuId: string; skuNumber?: string; skuName?: string; quantity: number; unit?: string; unitPrice: number; subtotal: number; receivedQty: number; status: string; lotControlled: boolean; inventoryUnit?: string; conversionRate: number; inventoryQty: number; inventoryUnitPrice: number; inventorySubtotal: number; }
export interface PurchaseRequisitionDto { id: string; prNumber: string; prDate: string; departmentId?: string; departmentName?: string; requesterId?: string; requesterName?: string; purpose?: string; priority?: string; budgetAccount?: string; projectCode?: string; workOrderId?: string; status: string; isLocked: boolean; totalEstimatedAmount: number; createdAt: string; itemCount?: number; items?: PurchaseRequisitionItemDto[]; }
export interface PurchaseRequisitionItemDto { id: string; skuId: string; skuNumber?: string; skuName?: string; quantity: number; unit: string; estimatedUnitPrice: number; currency: string; requiredDate?: string; notes?: string; estimatedSubtotal: number; }
export interface AccountsPayableDto { id: string; apNumber: string; supplierInvoiceNumber?: string; supplierInvoiceDate?: string; threeWayMatchResult: string; invoiceAmount: number; taxAmount: number; totalAmount: number; dueDate: string; paymentStatus: string; }

export const suppliersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<SupplierDto[]>>('/suppliers', { params }),
  get: (id: string) => api.get<ApiResponse<SupplierDto>>(`/suppliers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/suppliers', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/suppliers/${id}`, toPascalCase(data)),
  getPortalUsers: (supplierId: string) => api.get<ApiResponse<unknown[]>>(`/suppliers/${supplierId}/portal-users`),
  createPortalInvitation: (supplierId: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/suppliers/${supplierId}/portal-users/invitations`, toPascalCase(data)),
  retryPortalInvitation: (userId: string) => api.post<ApiResponse>(`/suppliers/portal-users/${userId}/retry`),
  resendPortalInvitation: (userId: string) => api.post<ApiResponse>(`/suppliers/portal-users/${userId}/resend-invitation`),
  deletePortalUser: (userId: string) => api.delete<ApiResponse>(`/suppliers/portal-users/${userId}`),
  submitForApproval: (id: string) => api.post<ApiResponse>(`/suppliers/${id}/submit-for-approval`),
  approve: (id: string, data: { comments?: string }) => api.post<ApiResponse>(`/suppliers/${id}/approve`, data),
  reject: (id: string, data: { comments?: string }) => api.post<ApiResponse>(`/suppliers/${id}/reject`, data),
  deactivate: (id: string, data: { comments?: string }) => api.post<ApiResponse>(`/suppliers/${id}/deactivate`, data),
  calculateRating: (id: string) => api.post<ApiResponse>(`/suppliers/${id}/rating/calculate`),
  getRating: (id: string) => api.get<ApiResponse<unknown>>(`/suppliers/${id}/rating`),
};

export const purchaseOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<PurchaseOrderDto[]>>('/purchase-orders', { params }),
  get: (id: string) => api.get<ApiResponse<PurchaseOrderDetailDto>>(`/purchase-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/purchase-orders', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/purchase-orders/${id}`, toPascalCase(data)),
  confirm: (po_no: string) => api.post<ApiResponse>(`/purchase-orders/${po_no}/confirm`),
  receive: (
    poNumber: string,
    warehouseId: string,
    binLocationId: string,
    items: Array<{ purchaseOrderItemId: string; quantity: number; lotNumber?: string }>
  ) => api.post<ApiResponse>(`/purchase-orders/${poNumber}/receive`, {
    warehouseId,
    binLocationId,
    items
  }),
  delete: (id: string) => api.delete<ApiResponse>(`/purchase-orders/${id}`),
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
  getApprovalChain: (id: string) => api.get<ApiResponse<unknown>>(`/purchase-requisitions/${id}/approval-chain`),
  getApprovalHistory: (id: string) => api.get<ApiResponse<unknown[]>>(`/purchase-requisitions/${id}/approval-history`),
  convertToPo: (id: string, supplierId: string) => api.post<ApiResponse>(`/purchase-requisitions/${id}/convert-to-po`, { supplierId }),
  checkBudget: (costCenter: string, amount: number) => api.get<ApiResponse<unknown>>('/purchase-requisitions/budget-check', { params: { costCenter, amount: amount.toString() } }),
};

export const rfqsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/rfqs', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/rfqs', data),
  addResponse: (id: string, data: Record<string, unknown>) => api.post<ApiResponse>(`/rfqs/${id}/responses`, data),
  award: (id: string, supplierId: string) => api.patch<ApiResponse>(`/rfqs/${id}/award`, { supplierId }),
};

export const goodsReceiptsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/goods-receipts', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/goods-receipts/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/goods-receipts', data),
  updateIqc: (id: string, data: Record<string, unknown>) => api.patch<ApiResponse>(`/goods-receipts/${id}/iqc`, data),
  allocateCosts: (id: string, data: { totalTariff: number; totalFreight: number; allocateByAmount?: boolean }) => 
    api.post<ApiResponse>(`/goods-receipts/${id}/allocate-costs`, data),
};

export const accountsPayableApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<AccountsPayableDto[]>>('/accounts-payable', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/accounts-payable', data),
  threeWayMatch: (poId: string) => api.get<ApiResponse<AccountsPayableDto>>(`/accounts-payable/three-way-match/${poId}`),
  pay: (id: string) => api.patch<ApiResponse>(`/accounts-payable/${id}/pay`),
};
