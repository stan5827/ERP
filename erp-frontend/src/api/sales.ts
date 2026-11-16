import { api, ApiResponse, toPascalCase } from './client';

export interface CustomerDto { id: string; customerCode: string; customerName: string; customerShortName?: string; taxId?: string; contactPerson?: string; contactTitle?: string; phone?: string; mobile?: string; email?: string; address?: string; fixedDiscount: number; creditLimit: number; creditStatus: string; companyType: string; fax?: string; paymentTerms?: string; taxCategory?: string; currency: string; priceLevel?: string; industry?: string; region?: string; salesTerritory?: string; approvalStatus: string; invoiceAddress?: string; leadSource?: string; churnRisk?: boolean; lastPurchaseDate?: string; satisfactionScore?: number; isActive: boolean; createdAt: string; }
export interface SalesOrderDto { id: string; orderNumber: string; customerId: string; customerName?: string; orderDate: string; paymentTerms?: string; tradeTerms?: string; currency: string; subtotal: number; taxAmount: number; totalAmount: number; expectedDeliveryDate?: string; customerDueDate?: string; confirmedDeliveryDate?: string; status: string; manufacturingStatus?: string; creditChecked: boolean; customerPoNumber?: string; salespersonId?: string; salespersonName?: string; departmentId?: string; departmentName?: string; salesChannel?: string; advanceDepositAmount?: number; orderType?: string; productionGroupCode?: string; hsCode?: string; freight?: number; shippingMethod?: string; incoterms?: string; orderRemarks?: string; createdAt: string; items: SalesOrderItemDto[]; }
export interface SalesOrderConfirmationDto {
  orderNumber: string;
  creditLimit: number;
  creditUsageBeforeOrder: number;
  creditUsageAfterOrder: number;
  creditLimitExceeded: boolean;
  inventoryWarnings: Array<{
    lineNumber: number;
    skuNumber?: string;
    skuName?: string;
    requiredQty: number;
    availableQty: number;
    shortageQty: number;
  }>;
}
export interface SalesOrderItemDto { id: string; salesOrderId: string; lineNumber: number; productId: string; productName?: string; skuId?: string; skuNumber?: string; skuName?: string; unitName?: string; customerItemCode?: string; quantity: number; unitPrice: number; discountRate: number; subtotal: number; deliveredQty: number; backorderQty: number; status: string; warehouseId?: string; warehouseName?: string; taxCode?: string; taxRate?: string; bomId?: string; bomVersion?: string; routingId?: string; workCenterId?: string; workCenterName?: string; needManufacturing: boolean; needOutsourcing: boolean; plannedStartDate?: string; plannedFinishDate?: string; lineRemarks?: string; reviewedBy?: string; reviewedAt?: string; productionStatus?: string; unit?: string; productSpec?: string; invoiceIssued?: boolean; }
export interface CreateDeliveryOrderRequest {
  salesOrderId: string;
  deliveryDate?: string;
  shippingMethod?: string;
  trackingNumber?: string;
  deliveryAddress?: string;
  items: Array<{
    skuId: string;
    quantity: number;
    warehouseId: string;
    binLocationId: string;
    lotNumber: string;
    serialNumber?: string;
  }>;
}
export interface DeliveryInspectionItemDto { deliveryOrderItemId: string; skuId: string; skuNumber?: string; skuName?: string; lotNumber?: string; quantity: number; result: string; warnings: string[]; reports: import('./mes').ProcessInspectionReportDto[]; }
export interface DeliveryInspectionReportDto { deliveryOrderId: string; deliveryNumber: string; deliveryDate: string; salesOrderNumber?: string; customerName?: string; deliveryAddress?: string; overallResult: string; warnings: string[]; items: DeliveryInspectionItemDto[]; }
export interface CashReceiptAllocationDto {
  id: string;
  salesInvoiceId: string;
  allocatedAmount: number;
  discountAmount: number;
  exchangeGainLoss: number;
}
export interface CashReceiptDto {
  id: string;
  receiptNumber: string;
  receiptDate: string;
  customerId: string;
  customerName?: string;
  paymentMethod: string;
  receivedAmount: number;
  discountAmount: number;
  currency: string;
  unappliedAmount: number;
  bankAccountId?: string;
  referenceNumber?: string;
  notes?: string;
  createdAt: string;
  allocations?: CashReceiptAllocationDto[];
}
export interface PendingInvoiceDto {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string;
  totalAmount: number;
  paidAmount: number;
  discountedAmount: number;
  outstanding: number;
  currency: string;
}
export interface CreateCashReceiptRequest {
  receiptDate: string;
  customerId: string;
  paymentMethod: string;
  receivedAmount: number;
  discountAmount: number;
  currency: string;
  bankAccountId: string;
  referenceNumber?: string;
  notes?: string;
  allocations: Array<{
    salesInvoiceId: string;
    allocatedAmount: number;
    discountAmount: number;
  }>;
}

export const customersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<CustomerDto[]>>('/customers', { params }),
  get: (id: string) => api.get<ApiResponse<CustomerDto>>(`/customers/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/customers', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/customers/${id}`, data),
};

export const salesOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<SalesOrderDto[]>>('/sales-orders', { params }),
  get: (id: string) => api.get<ApiResponse<SalesOrderDto>>(`/sales-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/sales-orders', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/sales-orders/${id}`, data),
  confirm: (so_no: string) => api.post<ApiResponse<SalesOrderConfirmationDto>>(`/sales-orders/${so_no}/confirm`),
  updateManufacturingStatus: (id: string, status: string) => api.patch<ApiResponse>(`/sales-orders/${id}/manufacturing-status`, { status }),
};

export const quotesApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/quotes', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/quotes/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<{ quoteNumber: string }>>('/quotes', toPascalCase(data)),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/quotes/${id}`, toPascalCase(data)),
  convertToOrder: (id: string) => api.patch<ApiResponse<SalesOrderDto>>(`/quotes/${id}/convert-to-order`),
  approve: (id: string) => api.patch<ApiResponse>(`/quotes/${id}/approve`),
  reject: (id: string) => api.patch<ApiResponse>(`/quotes/${id}/reject`),
};

export const deliveryOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/delivery-orders', { params }),
  get: (id: string) => api.get<ApiResponse<unknown>>(`/delivery-orders/${id}`),
  inspectionReport: (id: string) => api.get<ApiResponse<DeliveryInspectionReportDto>>(`/delivery-orders/${id}/inspection-report`),
  create: (data: CreateDeliveryOrderRequest) => api.post<ApiResponse<unknown>>('/delivery-orders', data),
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
  list: (params?: Record<string, string>) => api.get<ApiResponse<CashReceiptDto[]>>('/cash-receipts', { params }),
  create: (data: CreateCashReceiptRequest) => api.post<ApiResponse<CashReceiptDto>>('/cash-receipts', data),
  applyUnapplied: (receiptId: string, data: { salesInvoiceId: string; amount: number }) =>
    api.post<ApiResponse<CashReceiptDto>>(`/cash-receipts/${receiptId}/apply-unapplied`, data),
  pendingInvoices: (customerId: string) =>
    api.get<ApiResponse<PendingInvoiceDto[]>>(`/cash-receipts/pending-invoices/${customerId}`),
};

export const salesReportsApi = {
  orderToCash:    (params?: Record<string, string>) => api.get<ApiResponse<unknown>>('/sales-reports/order-to-cash', { params }),
  arAging:        () => api.get<ApiResponse<unknown>>('/sales-reports/ar-aging'),
  monthlySummary: (year?: number) => api.get<ApiResponse<unknown>>('/sales-reports/monthly-summary', { params: year ? { year: String(year) } : undefined }),
  creditUsage:    () => api.get<ApiResponse<unknown>>('/sales-reports/credit-usage'),
  periods:        (year?: number) => api.get<ApiResponse<unknown[]>>('/sales-reports/monthly-periods', { params: year ? { year: String(year) } : undefined }),
  closePeriod:    (data: { year: number; month: number; notes?: string }) => api.post<ApiResponse<unknown>>('/sales-reports/monthly-periods/close', data),
};
