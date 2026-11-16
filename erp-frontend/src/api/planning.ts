import { api, ApiResponse } from './client';

// ========== MRP ==========
export interface MrpRunDto { id: string; runNumber: string; runType: string; planningHorizonStart: string; planningHorizonEnd: string; horizonStart?: string; horizonEnd?: string; status: string; startedAt: string; completedAt?: string; createdBy?: string; totalItemsProcessed: number; plannedOrdersGenerated: number; purchaseRequisitionsGenerated: number; errorMessage?: string; }
export interface MrpRequirementDto { id: string; skuId: string; skuNumber?: string; skuName?: string; requirementType: string; sourceDocumentNumber?: string; requiredDate: string; requiredQty: number; availableQty: number; shortageQty: number; netRequirement: number; actionCode: string; substituteSkuId?: string; notes?: string; }
export interface MrpPlannedOrderDto { id: string; skuId: string; skuNumber?: string; skuName?: string; orderType: string; orderQty: number; plannedStartDate: string; plannedEndDate: string; status: string; convertedDocumentId?: string; convertedDocumentNumber?: string; notes?: string; createdAt: string; }
export interface MrpSummaryDto {
  hasRuns: boolean;
  latestRunId?: string;
  latestRunNo?: string;
  completedAt?: string;
  plannedOrders?: Array<{ type: string; count: number; qty: number }>;
}

export const mrpApi = {
  getRuns: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<MrpRunDto[]>>('/mrp/runs', { params }),
  getOrders: (runId: string, search?: string) => 
    api.get<ApiResponse<MrpPlannedOrderDto[]>>(`/mrp/runs/${runId}/orders`, { params: search ? { search } : undefined }),
  runMrp: (data: { horizonStart?: string; horizonEnd?: string; runType?: string }) => 
    api.post<ApiResponse<MrpRunDto>>('/mrp/run', data),
  convertOrder: (orderId: string) => 
    api.post<ApiResponse<{ message: string; documentNumber: string }>>(`/mrp/orders/${orderId}/convert`),
  getSummary: () => api.get<ApiResponse<MrpSummaryDto>>('/mrp/summary'),
  getRun: (runId: string) => api.get<ApiResponse<MrpRunDto>>(`/mrp/runs/${runId}`),
  runs: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<MrpRunDto[]>>('/mrp/runs', { params }),
  plannedOrders: (runId: string, params?: { orderType?: string; status?: string }) => 
    api.get<ApiResponse<MrpPlannedOrderDto[]>>(`/mrp/runs/${runId}/orders`, { params }),
  run: (data: { horizonStart?: string; horizonEnd?: string; runType?: string }) => api.post<ApiResponse<MrpRunDto>>('/mrp/run', data),
  confirmPlannedOrder: (orderId: string) => 
    api.post<ApiResponse<{ message: string; documentNumber: string }>>(`/mrp/orders/${orderId}/convert`),
  requirements: (runId: string, params?: Record<string, string | number | boolean>) =>
    api.get<ApiResponse<MrpRequirementDto[]>>(`/mrp/runs/${runId}/requirements`, { params }),
};

// ========== MPS ==========
export interface MpsHeaderDto { id: string; mpsNo: string; planName: string; horizonStart: string; horizonEnd: string; timeBucketType: string; versionNo: number; status: string; baseOn: string; createdAt: string; }
export interface MpsLineDto { id: string; skuId: string; skuNumber: string; skuName: string; bucketStart: string; bucketEnd: string; planQty: number; consumedQty: number; adjustedQty: number; notes?: string; }
export interface MpsDetailDto extends MpsHeaderDto { lines: MpsLineDto[]; }
export interface ResourceLoadDto { resourceId: string; resourceCode: string; resourceName: string; bucketCode: string; bucketStart: string; bucketEnd: string; requiredCapacity: number; availableCapacity: number; loadPercentage: number; status: 'OK' | 'CRITICAL' | 'OVERLOAD'; }
export interface RccpStatusDto { mpsHeaderId: string; loads: ResourceLoadDto[]; }
export interface WeeklyLoadDto { weekCode: string; weekStart: string; required: number; available: number; loadPercentage: number; }
export interface ResourceAggregateLoadDto { resourceId: string; resourceCode: string; resourceName: string; resourceType: string; uom: string; weeklyLoads: WeeklyLoadDto[]; maxLoadPercentage: number; }
export interface AggregateRccpDto { startDate: string; endDate: string; resources: ResourceAggregateLoadDto[]; }
export interface CreateMpsRequest { planName: string; horizonStart: string; horizonEnd: string; timeBucketType: string; baseOn: string; lines: Array<{ skuId: string; bucketStart: string; bucketEnd: string; planQty: number; notes?: string; }>; }
export interface CurrencyDto { id: string; currencyCode: string; currencyName: string; symbol: string; decimalPlaces: number; isBaseCurrency: boolean; }
export interface ExchangeRateDto { id: string; fromCurrency: string; toCurrency: string; rate: number; validFrom: string; validTo?: string; }
export interface MpsPlanDto { id: string; planNumber: string; productSkuId: string; productSkuName: string; productSkuNumber: string; quantity: number; startDate: string; endDate: string; priority: string; status: string; sourceType: string; salesOrderId?: string; notes?: string; createdAt: string; }
export interface SalesForecastDto { id: string; productSkuId: string; productSkuName: string; year: number; month: number; forecastQty: number; actualQty: number; sourceType: string; }

export const currenciesApi = {
  listCurrencies: () => api.get<ApiResponse<CurrencyDto[]>>('/currencies'),
  createCurrency: (data: Partial<CurrencyDto>) => api.post<ApiResponse<CurrencyDto>>('/currencies', data),
  listRates: () => api.get<ApiResponse<ExchangeRateDto[]>>('/currencies/rates'),
  createRate: (data: Partial<ExchangeRateDto>) => api.post<ApiResponse<ExchangeRateDto>>('/currencies/rates', data),
};

export const rccpApi = {
  listResources: () => api.get<ApiResponse<unknown[]>>('/rccp/resources'),
  createResource: (data: Record<string, unknown>) => api.post<ApiResponse>('/rccp/resources', data),
  updateResource: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/rccp/resources/${id}`, data),
  getItemResources: (skuId: string) => api.get<ApiResponse<unknown[]>>('/rccp/item-resources/' + skuId),
  updateItemResources: (data: Record<string, unknown>[]) => api.post<ApiResponse>('/rccp/item-resources', data),
  getCapacitySummary: () => api.get<ApiResponse<unknown>>('/rccp/summary'),
  getWorkCenterCapacity: (workCenterId: string) => api.get<ApiResponse<unknown>>(`/rccp/work-centers/${workCenterId}`),
};

export const mpsApi = {
  plans: (params?: { status?: string }) => api.get<ApiResponse<MpsPlanDto[]>>('/mps/plans', { params }),
  getPlan: (id: string) => api.get<ApiResponse<MpsPlanDto>>(`/mps/plans/${id}`),
  createPlan: (data: { productSkuId: string; quantity: number; startDate: string; endDate: string; priority?: string; sourceType?: string; notes?: string }) => api.post<ApiResponse<{ id: string; planNumber: string }>>('/mps/plans', data),
  updatePlan: (id: string, data: Record<string, unknown>) => api.put<ApiResponse>(`/mps/plans/${id}`, data),
  releasePlan: (id: string) => api.post<ApiResponse<{ id: string; woNumber: string }>>(`/mps/plans/${id}/release`),
  deletePlan: (id: string) => api.delete<ApiResponse>(`/mps/plans/${id}`),
  forecasts: (params?: { year?: number; month?: number }) => api.get<ApiResponse<SalesForecastDto[]>>('/mps/forecasts', { params }),
  createForecast: (data: { productSkuId: string; year: number; month: number; forecastQty: number; sourceType?: string; notes?: string }) => api.post<ApiResponse<{ id: string }>>('/mps/forecasts', data),
  generate: (data?: { productSkuIds?: string[]; startDate?: string; endDate?: string; includeForecast?: boolean; includeSalesOrders?: boolean; includeSafetyStock?: boolean; aggregationPeriod?: 'daily' | 'weekly' | 'monthly'; generationMode?: 'regenerative' | 'net_change'; }) => api.post<ApiResponse<{ generated: number; plans: unknown[] }>>('/mps/generate', data),
  list: () => api.get<ApiResponse<MpsHeaderDto[]>>('/mps'),
  get: (id: string) => api.get<ApiResponse<MpsDetailDto>>(`/mps/${id}`),
  create: (data: CreateMpsRequest) => api.post<ApiResponse<MpsHeaderDto>>('/mps', data),
  runConsumption: (id: string) => api.post<ApiResponse>(`/mps/${id}/run-consumption`),
  getExceptions: (id: string) => api.get<ApiResponse<unknown[]>>(`/mps/${id}/exceptions`),
  getRccpStatus: (id: string) => api.get<ApiResponse<RccpStatusDto>>(`/mps/${id}/rccp`),
  getAggregateRccp: (start?: string, end?: string) => api.get<ApiResponse<AggregateRccpDto>>('/mps/aggregate-rccp', { params: { start, end } }),
};

// ========== APS ==========
export interface ProductionScheduleDto { id: string; scheduleNumber: string; scheduleType: string; scheduleDate: string; horizonStart: string; horizonEnd: string; status: string; createdAt: string; activatedAt?: string; totalTasks: number; totalCapacityLoads: number; unresolvedConstraints: number; tasks: ScheduleTaskDto[]; }
export interface ScheduleTaskDto { id: string; workOrderId: string; operationSeq: number; workCenterId: string; workCenterName?: string; plannedStart: string; plannedEnd: string; setupHours: number; runHours: number; totalHours: number; quantity: number; status: string; priority: number; sequenceType: string; mrpPlannedStart?: string; }
export interface CapacityLoadDto { id: string; workCenterId: string; workCenterName?: string; loadDate: string; shiftNumber: number; availableHours: number; scheduledHours: number; loadedHours: number; loadPercentage: number; loadStatus: string; capacityUtilization?: number; }
export interface ScheduleConstraintDto { id: string; constraintType: string; description: string; resourceId?: string; resourceType?: string; startDate?: string; endDate?: string; severity: string; isResolved: boolean; resolvedAt?: string; resolutionNotes?: string; }
export interface GanttTaskDto { id: string; taskName: string; workOrderId: string; woNumber: string; workCenterId: string; workCenterName?: string; start: string; end: string; progress: number; status: string; priority: number; setupHours: number; runHours: number; totalHours: number; quantity: number; }

export const apsApi = {
  generateSchedule: (data: { horizonStart: string; horizonEnd: string; scheduleType?: string }) => api.post<ApiResponse<ProductionScheduleDto>>('/aps/schedules/generate', data),
  schedules: (params?: { page?: number; pageSize?: number }) => api.get<ApiResponse<ProductionScheduleDto[]>>('/aps/schedules', { params }),
  getSchedule: (scheduleId: string) => api.get<ApiResponse<ProductionScheduleDto>>(`/aps/schedules/${scheduleId}`),
  optimizeSchedule: (scheduleId: string, data: { primaryGoal?: string; respectPriority?: boolean; allowRescheduling?: boolean; notes?: string }) => api.post<ApiResponse<ProductionScheduleDto>>(`/aps/schedules/${scheduleId}/optimize`, data),
  rescheduleTask: (taskId: string, data: { newStart: string; newEnd: string }) => api.post<ApiResponse<{ message: string; affectedTasks: number }>>(`/aps/tasks/${taskId}/reschedule`, data),
  capacityLoads: (scheduleId: string, params?: { workCenterId?: string }) => api.get<ApiResponse<CapacityLoadDto[]>>(`/aps/schedules/${scheduleId}/capacity-loads`, { params }),
  constraints: (scheduleId: string, params?: { includeResolved?: boolean }) => api.get<ApiResponse<ScheduleConstraintDto[]>>(`/aps/schedules/${scheduleId}/constraints`, { params }),
  resolveConstraint: (scheduleId: string, constraintId: string, data: { resolutionNotes?: string }) => api.post<ApiResponse<{ message: string }>>(`/aps/schedules/${scheduleId}/constraints/${constraintId}/resolve`, data),
  ganttData: (workCenterId: string, params: { startDate: string; endDate: string }) => api.get<ApiResponse<GanttTaskDto[]>>(`/aps/work-centers/${workCenterId}/gantt-data`, { params }),
  acceptWorkOrder: (scheduleId: string, data: { workOrderId: string }) => api.post<ApiResponse<{ message: string }>>(`/aps/schedules/${scheduleId}/accept-work-order`, data),
  export: () => api.get<ApiResponse<unknown>>('/aps/export'),
  importSchedule: (data: Record<string, unknown>) => api.post<ApiResponse>('/aps/import-schedule', data),
};

// ========== KPI ==========
export interface KpiDashboardDto { periodStart: string; periodEnd: string; averageOee: number; oeeByWorkCenter: OeeSummaryDto[]; oeeTrend: KpiTrendDto[]; onTimeDeliveryRate: number; totalOrders: number; onTimeOrders: number; delayedOrders: number; otdTrend: KpiTrendDto[]; wipQuantity: number; wipValue: number; wipByWorkCenter: Record<string, number>; scrapQuantity: number; scrapAmount: number; scrapByReason: Record<string, number>; topScrapItems: TopScrapItemDto[]; capacityUtilization: number; }
export interface OeeSummaryDto { workCenterId: string; workCenterName: string; availability: number; performance: number; quality: number; oeeValue: number; status: string; }
export interface OeeResultDto { workCenterId: string; recordDate: string; shift?: number; plannedRunTime: number; actualRunTime: number; downtime: number; setupTime: number; waitTime: number; theoreticalOutput: number; actualOutput: number; goodOutput: number; defectOutput: number; availability: number; performance: number; quality: number; oeeValue: number; }
export interface OtdResultDto { periodStart: string; periodEnd: string; totalOrders: number; onTimeOrders: number; delayedOrders: number; onTimeRate: number; averageDelayDays: number; }
export interface WipResultDto { totalWipQuantity: number; totalWipValue: number; wipRatio: number; wipByWorkCenter: Record<string, number>; wipOrdersCount: number; }
export interface ScrapResultDto { periodStart: string; periodEnd: string; totalScrapQuantity: number; totalScrapAmount: number; scrapByReason: Record<string, number>; topScrapItems: TopScrapItemDto[]; }
export interface TopScrapItemDto { skuId: string; skuNumber?: string; skuName?: string; scrapQuantity: number; scrapAmount: number; }
export interface KpiTrendDto { periodStart: string; periodEnd: string; value: number; targetValue?: number; achievementRate?: number; status: string; }
export interface WorkCenterDto { id: string; workCenterCode: string; workCenterName: string; capacity: number; efficiency: number; }

export const kpiApi = {
  dashboard: (params?: { fromDate?: string; toDate?: string }) => api.get<ApiResponse<KpiDashboardDto>>('/kpis/dashboard', { params }),
  oee: (workCenterId: string, params?: { date?: string; shift?: number }) => api.get<ApiResponse<OeeResultDto>>(`/kpi/oee/${workCenterId}`, { params }),
  oeeHistory: (workCenterId: string, params?: { fromDate?: string; toDate?: string }) => api.get<ApiResponse<Array<{ date: string; totalPlannedTime: number; totalActualTime: number; totalDowntime: number; totalOutput: number; goodOutput: number }>>>(`/kpi/oee/${workCenterId}/history`, { params }),
  otd: (params?: { fromDate?: string; toDate?: string }) => api.get<ApiResponse<OtdResultDto>>('/kpi/otd', { params }),
  wip: () => api.get<ApiResponse<WipResultDto>>('/kpi/wip'),
  scrap: (params?: { fromDate?: string; toDate?: string }) => api.get<ApiResponse<ScrapResultDto>>('/kpi/scrap', { params }),
  trends: (kpiCode: string, params?: { fromDate?: string; toDate?: string; periodType?: string }) => api.get<ApiResponse<KpiTrendDto[]>>(`/kpi/trends/${kpiCode}`, { params }),
  definitions: () => api.get<ApiResponse<Array<{ id: string; kpiCode: string; kpiName: string; description?: string; category: string; unit: string; targetValue?: number; warningThreshold?: number; criticalThreshold?: number }>>>('/kpi/definitions'),
  createDefinition: (data: Record<string, unknown>) => api.post<ApiResponse>('/kpi/definitions', data),
  workCenters: () => api.get<ApiResponse<WorkCenterDto[]>>('/kpi/work-centers'),
  addMachineOperationRecord: (data: Record<string, unknown>) => api.post<ApiResponse<{ id: string }>>('/kpi/records/machine-operation', data),
  updateMachineOperationRecord: (id: string, data: Record<string, unknown>) => api.put<ApiResponse<{ id: string }>>(`/kpi/records/machine-operation/${id}`, data),
};

// ========== Dashboard ==========
export interface DashboardKpi { title: string; value: string; unit?: string; changePercent?: number; changeDirection?: string; color?: string; }
export interface DashboardAlert { id: string; title: string; description?: string; severity: string; createdAt: string; }
export interface WorkOrderSummary { id: string; woNumber: string; productName?: string; plannedQty: number; completedQty: number; progress: number; status: string; priority: string; dueDate?: string; }
export interface ProductionTrend { date: string; planned: number; actual: number; efficiency: number; }
export interface AttendanceTrend { date: string; present: number; late: number; absent: number; leave: number; }
export interface SidebarBadge { salesCount: number; procurementCount: number; hrCount: number; inventoryCount: number; manufacturingCount: number; qualityCount: number; }
export interface DashboardData { kpiCards: DashboardKpi[]; alerts: DashboardAlert[]; recentWorkOrders: WorkOrderSummary[]; productionTrend: ProductionTrend[]; attendanceTrend: AttendanceTrend[]; totalEmployees: number; todayAttendanceCount: number; pendingApprovals: number; activeAlerts: number; sidebarBadges: SidebarBadge; }

export const dashboardApi = {
  get: () => api.get<ApiResponse<DashboardData>>('/dashboard'),
  getBadges: () => api.get<ApiResponse<SidebarBadge>>('/dashboard/sidebar-badges'),
};

// ========== AI Planning ==========
export interface PlanTask { id: string; description: string; dependencies: string[]; estimatedMinutes: number; toolsNeeded: string[]; status: string; priority: number; }
export interface PlanningRequest { taskDescription: string; context?: string; parameters?: Record<string, unknown>; modelId?: string; }
export interface PlanningResponse { success: boolean; planId?: string; tasks: PlanTask[]; strategy: string; confidence: number; errorMessage?: string; reasoning?: string[]; }
export interface ExecutionRequest { planId: string; taskId?: string; parallelExecution: boolean; }
export interface TaskResult { taskId: string; status: string; output?: string; error?: string; duration: number; toolUsed?: string; }
export interface ExecutionResponse { success: boolean; executionId?: string; results: TaskResult[]; duration: number; errorMessage?: string; }
export interface ReviewRequest { executionId: string; feedback?: string; autoApproval: boolean; }
export interface ReviewResponse { approved: boolean; qualityScore: number; issues: string[]; recommendations?: string; suggestedFixes?: string[]; }
export interface KpiValidationResult { accuracyRate: number; currentRoi: number; targetMet: boolean; metrics: Record<string, number>; trends: Record<string, number>; lastUpdated: string; }
export interface AgentState { executionId: string; currentNode: string; status: string; context: Record<string, unknown>; history: string[]; startedAt: string; completedAt?: string; }
export interface ToolExecuteRequest { toolId: string; parameters?: Record<string, unknown>; }
export interface ToolResult { success: boolean; data?: string; error?: string; elapsedMs: number; }

export const planningApi = {
  getSuggestions: () => api.get<ApiResponse<unknown[]>>('/planning/suggestions'),
  persistSuggestions: () => api.post<ApiResponse<unknown[]>>('/planning/suggestions'),
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
