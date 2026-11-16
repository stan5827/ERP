import { api, ApiResponse, toPascalCase } from './client';

export interface WorkOrderMaterialDto { id: string; skuId: string; skuNumber?: string; skuName?: string; unit?: string; inventoryUnit?: string; bomStandardQty: number; requiredQty: number; issuedQty: number; unissuedQty: number; closedShortageQty: number; onHandQty: number; availableQty: number; shortageQty: number; status: string; }
export interface WorkOrderDto { id: string; woNumber: string; woType: string; productSkuId: string; productSkuNumber?: string; productSkuName?: string; salesOrderId?: string; salesOrderNumber?: string; bomId?: string; bomVersion?: string; productionUomCode?: string; plannedQty: number; productionQty?: number; actualQty: number; scrapQty: number; plannedStartDate?: string; plannedEndDate?: string; status: string; priority: string; notes?: string; createdAt: string; materials?: WorkOrderMaterialDto[]; }
export interface WorkOrderCostSummaryDto {
  id: string;
  woNumber: string;
  productSkuName?: string;
  productSkuNumber?: string;
  actualQty: number;
  totalStandardCost?: number;
  actualMaterialCost: number;
  actualLaborCost: number;
  actualOverheadCost: number;
  status?: string;
}
export interface WipReportRequest {
  woNumber: string;
  operationSeq: number;
  employeeId?: string;
  reportType: 'individual' | 'team';
  teamMemberEmployeeIds: string[];
  teamMemberHours: Array<{ employeeId: string; manHours: number }>;
  proxyReason?: string;
  shiftClass: string;
  machineCode?: string;
  equipmentMachineHours?: Array<{ equipmentId: string; machineHours: number }>;
  completedQty: number;
  defectQty: number;
  scrapQty: number;
  defectReasonCode?: string;
  setupHours: number;
  runHours: number;
  manHours: number;
  machineHours: number;
  startTime?: string;
  endTime?: string;
  materialLotNumber?: string;
  finishedLotNumber?: string;
  remarks?: string;
}
export interface WipReportResultDto {
  id: string;
  reportNumber: string;
  workOrderNumber: string;
  operationSeq: number;
  operationCompletedQty: number;
  operationPlannedQty: number;
  workOrderCompletedQty: number;
  workOrderPlannedQty: number;
  status: string;
}
export interface WipReportQueueItemDto {
  id: string;
  reportNumber: string;
  workOrderNumber: string;
  employeeId: string;
  operationSeq: number;
  productName: string;
  operationName: string;
  employeeName: string;
  reportType: 'individual' | 'team';
  shiftClass: string;
  completedQty: number;
  defectQty: number;
  scrapQty: number;
  manHours: number;
  setupHours: number;
  runHours: number;
  startTime?: string;
  endTime?: string;
  submittedAt?: string;
  status: 'submitted' | 'approved' | 'returned';
  proxyReason?: string;
  reviewComments?: string;
  defectReasonCode?: string;
  materialLotNumber?: string;
  finishedLotNumber?: string;
  remarks?: string;
  teamMemberEmployeeIds: string[];
  teamMemberHours: Array<{ employeeId: string; manHours: number }>;
}
export interface EquipmentUsageRequest {
  workOrderId: string;
  operationSeq: number;
  equipmentId: string;
  machineHours: number;
  shiftClass: string;
  startTime: string;
  endTime: string;
  remarks?: string;
}
export interface EquipmentUsageRecordDto extends EquipmentUsageRequest {
  id: string;
  workOrderNumber: string;
  equipmentCode: string;
  equipmentName: string;
  recordedByEmployeeName: string;
}
export interface WorkOrderCostAnalysisDto { workOrderId: string; woNumber: string; actualMaterialCost: number; actualLaborCost: number; actualMachineCost: number; actualOverheadCost: number; totalActualCost: number; standardMaterialCost: number; standardLaborCost: number; standardOverheadCost: number; totalStandardCost: number; varianceAmount: number | null; variancePercentage: number | null; unitCost: number | null; actualQty: number; hasActualCost: boolean; status: string; }
export interface BomDto { id: string; bomCode: string; productSkuId: string; productSkuNumber?: string; productSkuName?: string; version: string; status: string; effectiveDate: string; expiryDate?: string; notes?: string; items: BomItemDto[]; createdAt: string; updatedAt: string; }
export interface BomItemDto { id: string; childSkuId: string; childSkuNumber?: string; childSkuName?: string; quantity: number; unit: string; scrapRate: number; operationSeq: number; isPhantom: boolean; effectiveDate?: string; }
export interface EcnDto { id: string; ecnNumber: string; changeDescription?: string; effectiveDate: string; status: string; createdAt: string; }
export interface EcnImpactDto {
  ecnId: string;
  totalLossAmount: number;
  affectedItemCount: number;
  affectedWorkOrderCount: number;
  affectedPurchaseOrderCount: number;
  suggestedEffectiveDate?: string;
  details: EcnImpactItemDto[];
  affectedWorkOrders: EcnImpactWorkOrderDto[];
}
export interface EcnImpactItemDto {
  skuNumber: string;
  skuName: string;
  currentStock: number;
  unitCost: number;
  potentialLoss: number;
  impactType: string;
}
export interface EcnImpactWorkOrderDto {
  workOrderId: string;
  woNumber: string;
  status: string;
  plannedQty: number;
  productSkuLabel: string;
}
export interface OperationDto { id: string; operationCode: string; operationName: string; setupTime: number; runTime: number; costPerHour: number; isOutsourced: boolean; isActive: boolean; }
export interface RoutingDto { id: string; routingCode: string; routingName: string; productSkuId: string; productSkuName: string; productSkuNumber: string; version: string; isActive: boolean; operations: Array<{ operationSeq: number; operationId: string; operationName: string; operationCode: string; workCenterId: string; workCenterName: string; workCenterCode: string; transferTime: number; waitTime: number; }>; }
export interface InspectionTemplateItemDto { id: string; itemCode: string; itemName: string; inputType: string; specification?: string; unit?: string; lowerLimit?: number; upperLimit?: number; options: string[]; inspectionMethod?: string; instrument?: string; samplingPlan?: string; isRequired: boolean; isCritical: boolean; sortOrder: number; }
export interface InspectionTemplateDto { id: string; templateCode: string; templateName: string; operationId: string; operationCode?: string; operationName?: string; productSkuId?: string; productSkuNumber?: string; productSkuName?: string; version: number; isActive: boolean; notes?: string; items: InspectionTemplateItemDto[]; }
export interface WorkOrderInspectionOperationDto { operationSeq: number; operationId: string; operationCode: string; operationName: string; workCenterId: string; workCenterName?: string; templates: InspectionTemplateDto[]; }
export interface ProcessInspectionReportItemDto { itemName: string; inputType: string; specification?: string; unit?: string; lowerLimit?: number; upperLimit?: number; inspectionMethod?: string; instrument?: string; samplingPlan?: string; isCritical: boolean; value?: string; result: string; notes?: string; }
export interface ProcessInspectionReportDto { id: string; reportNumber: string; workOrderId: string; workOrderNumber?: string; operationSeq: number; operationCode?: string; operationName?: string; workCenterName?: string; templateId: string; templateName?: string; templateVersion: number; productSkuId: string; productSkuNumber?: string; productSkuName?: string; lotNumber?: string; inspectorName: string; inspectedAt: string; sampleQuantity: number; result: string; notes?: string; items: ProcessInspectionReportItemDto[]; }
export interface FqcInspectionLotDto { id: string; inspectionNumber: string; workOrderId: string; workOrderNumber?: string; productSkuId: string; productSkuNumber?: string; lotNumber: string; expiryDate?: string; producedQty: number; sampledQty: number; defectiveQty: number; approvedQty: number; receivedQty: number; availableReceiptQty: number; criticalDefectFound: boolean; inspectionStatus: string; dispositionStatus: string; defectCode?: string; inspectorName: string; inspectedAt: string; decisionBy?: string; decidedAt?: string; quarantinedQty: number; reworkWorkOrderId?: string; dispositionBy?: string; dispositionAt?: string; notes?: string; }
export interface ProductionReceiptDto { id: string; receiptNumber: string; workOrderId: string; workOrderNumber?: string; fqcInspectionLotId: string; fqcInspectionNumber?: string; lotNumber?: string; skuId: string; quantity: number; unitCost: number; totalCost: number; warehouseId: string; binLocationId: string; receivedAt: string; }

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
  getImpact: (id: string) => api.get<ApiResponse<EcnImpactDto>>(`/ecns/${id}/impact`),
};

export const workOrdersApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<WorkOrderDto[]>>('/work-orders', { params }),
  get: (id: string) => api.get<ApiResponse<WorkOrderDto>>(`/work-orders/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse>('/work-orders', toPascalCase(data)),
  createFromSalesOrder: (salesOrderId: string) => api.post<ApiResponse<WorkOrderDto[]>>(`/work-orders/from-sales-order/${salesOrderId}`),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/work-orders/${id}`, toPascalCase(data)),
  recalculateMaterials: (workOrderIds: string[], plannedQty: number) =>
    api.post<ApiResponse>('/work-orders/recalculate-materials', { workOrderIds, plannedQty }),
  updateStatus: (id: string, status: string) => api.patch<ApiResponse>(`/work-orders/${id}/status`, toPascalCase({ status })),
  start: (id: string) => api.patch<ApiResponse>(`/WorkOrders/${id}/start`),
  complete: (id: string, data: Record<string, unknown>) => api.patch<ApiResponse>(`/WorkOrders/${id}/complete`, toPascalCase(data)),
  explodeRouting: (id: string) => api.patch<ApiResponse>(`/work-orders/${id}/explode-routing`),
  createSupplyOrders: (id: string) => api.post<ApiResponse<{ prCount: number; subWoCount: number }>>(`/work-orders/${id}/create-supply-orders`),
  costingAnalysis: (id: string) => api.get<ApiResponse<WorkOrderCostAnalysisDto>>(`/workorders/${id}/costing/analysis`),
};

export const costingApi = {
  summary: () => api.get<ApiResponse<WorkOrderCostSummaryDto[]>>('/work-orders-costing/summary'),
  analysis: (id: string) => api.get<ApiResponse<WorkOrderCostAnalysisDto>>(`/workorders/${id}/costing/analysis`),
  settle: (id: string) => api.post<ApiResponse>(`/workorders/${id}/costing/settle`),
  batchSettle: () => api.post<ApiResponse>('/work-orders-costing/batch-settle'),
};

export const wipReportsApi = {
  list: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/WipReports', { params }),
  create: (data: WipReportRequest) => api.post<ApiResponse<WipReportResultDto>>('/wipreports/submit', data),
  my: () => api.get<ApiResponse<WipReportQueueItemDto[]>>('/wipreports/my'),
  reviewQueue: () => api.get<ApiResponse<WipReportQueueItemDto[]>>('/wipreports/review-queue'),
  approve: (id: string, comments?: string) => api.post<ApiResponse<WipReportResultDto>>(`/wipreports/${id}/approve`, { comments }),
  returnForCorrection: (id: string, comments: string) => api.post<ApiResponse<WipReportResultDto>>(`/wipreports/${id}/return`, { comments }),
  resubmit: (id: string, data: WipReportRequest) => api.post<ApiResponse<WipReportResultDto>>(`/wipreports/${id}/resubmit`, data),
  createEquipmentUsage: (data: EquipmentUsageRequest) => api.post<ApiResponse<EquipmentUsageRecordDto>>('/wipreports/equipment-usage', data),
  equipmentUsage: (workOrderId: string) => api.get<ApiResponse<EquipmentUsageRecordDto[]>>(`/wipreports/work-orders/${workOrderId}/equipment-usage`),
};

export const fqcWorkflowApi = {
  list: (params?: { workOrderId?: string }) => api.get<ApiResponse<FqcInspectionLotDto[]>>('/quality/fqc-inspections', { params }),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<FqcInspectionLotDto>>('/quality/fqc-inspections', data),
  decide: (id: string, data: Record<string, unknown>) => api.post<ApiResponse<FqcInspectionLotDto>>(`/quality/fqc-inspections/${id}/decision`, data),
  createReworkWorkOrder: (id: string) => api.post<ApiResponse<FqcInspectionLotDto>>(`/quality/fqc-inspections/${id}/rework-work-order`),
  createScrapNote: (id: string, reason: string) => api.post<ApiResponse<{ id: string; scrapNumber: string; quantity: number; status: string }>>(`/quality/fqc-inspections/${id}/scrap-note`, { reason }),
  listReceipts: (params?: { workOrderId?: string }) => api.get<ApiResponse<ProductionReceiptDto[]>>('/manufacturing/production-receipts', { params }),
  createReceipt: (data: Record<string, unknown>) => api.post<ApiResponse<ProductionReceiptDto>>('/manufacturing/production-receipts', data),
};

export const operationsApi = {
  list: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<OperationDto[]>>('/operations', { params }),
  get: (id: string) => api.get<ApiResponse<OperationDto>>(`/operations/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<OperationDto>>('/operations', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<OperationDto>>(`/operations/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/operations/${id}`),
};

export const routingsApi = {
  list: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<RoutingDto[]>>('/routings', { params }),
  get: (id: string) => api.get<ApiResponse<RoutingDto>>(`/routings/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<RoutingDto>>('/routings', data),
  update: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<RoutingDto>>(`/routings/${id}`, data),
  delete: (id: string) => api.delete<ApiResponse>(`/routings/${id}`),
};

export const inspectionTemplatesApi = {
  list: (params?: { operationId?: string; productSkuId?: string }) =>
    api.get<ApiResponse<InspectionTemplateDto[]>>('/quality/inspection-templates', { params }),
  get: (id: string) => api.get<ApiResponse<InspectionTemplateDto>>(`/quality/inspection-templates/${id}`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<InspectionTemplateDto>>('/quality/inspection-templates', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse<InspectionTemplateDto>>(`/quality/inspection-templates/${id}`, data),
};

export const processInspectionsApi = {
  workOrderOperations: (workOrderId: string) =>
    api.get<ApiResponse<WorkOrderInspectionOperationDto[]>>(`/quality/work-orders/${workOrderId}/inspection-operations`),
  list: (params?: { workOrderId?: string; lotNumber?: string }) =>
    api.get<ApiResponse<ProcessInspectionReportDto[]>>('/quality/process-inspections', { params }),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<ProcessInspectionReportDto>>('/quality/process-inspections', data),
};
