import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as api from '../api/api';

// ========== Generic helper ==========
function usePagedQuery<T>(key: string, fetcher: (params?: Record<string, string>) => Promise<unknown>, params?: Record<string, string>, refreshKey?: number) {
  return useQuery({
    queryKey: [key, params, refreshKey],
    queryFn: async () => {
      const res = await fetcher(params) as { data: api.ApiResponse<T[]> };
      return {
        data: res.data?.data || [],
        totalCount: res.data?.totalCount || 0
      };
    },
  });
}

// ========== Auth ==========
export const useLogin = () => {
  return useMutation({
    mutationFn: (req: api.LoginRequest) => api.authApi.login(req).then(r => r.data),
    onSuccess: (data) => {
      if (data.success && data.data) {
        localStorage.setItem('erp_token', data.data.token);
        localStorage.setItem('erp_user', JSON.stringify(data.data));
        window.location.reload();
      }
    },
  });
};

export const useLogout = () => {
  const qc = useQueryClient();
  return () => {
    localStorage.removeItem('erp_token');
    localStorage.removeItem('erp_user');
    qc.clear();
    window.location.reload();
  };
};

export const useCurrentUser = () => {
  const stored = localStorage.getItem('erp_user');
  return stored ? JSON.parse(stored) as api.LoginResponse : null;
};

// ========== Dashboard ==========
export const useDashboard = () =>
  useQuery({ queryKey: ['dashboard'], queryFn: () => api.dashboardApi.get().then(r => r.data) });

export const useSidebarBadges = () =>
  useQuery({ 
    queryKey: ['sidebar-badges'], 
    queryFn: () => api.dashboardApi.getBadges().then(r => r.data),
    refetchInterval: 30000 // Refresh every 30 seconds
  });

// ========== Users & Roles ==========
export const useUsers = (params?: Record<string, string>) =>
  usePagedQuery<api.UserDto>('users', api.usersApi.list, params);
export const useRoles = () =>
  useQuery({ queryKey: ['roles'], queryFn: () => api.rolesApi.list().then(r => r.data) });
export const usePermissions = () =>
  useQuery({ queryKey: ['permissions'], queryFn: () => api.permissionsApi.list().then(r => r.data) });

export const useCreateUser = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (d: Parameters<typeof api.usersApi.create>[0]) => api.usersApi.create(d).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['users'] }) });
};
export const useUpdateUser = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ id, data }: { id: string; data: Record<string, unknown> }) => api.usersApi.update(id, data).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['users'] }) });
};
export const useDeleteUser = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (id: string) => api.usersApi.delete(id).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['users'] }) });
};

// ========== Departments & Employees ==========
export const useDepartments = (params?: Record<string, string>) =>
  usePagedQuery<api.DepartmentDto>('departments', api.departmentsApi.list, params);
export const useDepartmentTree = () =>
  useQuery({ queryKey: ['departments', 'tree'], queryFn: () => api.departmentsApi.tree().then(r => r.data) });

export const useEmployees = (params?: Record<string, string>, refreshKey?: number) =>
  usePagedQuery<api.EmployeeDto>('employees', api.employeesApi.list, params, refreshKey);
export const useEmployee = (id: string) =>
  useQuery({ queryKey: ['employees', id], queryFn: () => api.employeesApi.get(id).then(r => r.data), enabled: !!id });
export const useCreateEmployee = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (d: Record<string, unknown>) => api.employeesApi.create(d).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['employees'] }) });
};
export const useTerminateEmployee = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ id, reason }: { id: string; reason: string }) => api.employeesApi.terminate(id, reason).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['employees'] }) });
};

// ========== Inventory ==========
export const useSkus = (params?: Record<string, string>) =>
  usePagedQuery<api.SkuDto>('skus', api.skusApi.list, params);
export const useUoms = () =>
  useQuery({ queryKey: ['uoms'], queryFn: () => api.uomsApi.list().then(r => r.data) });
export const useUomConversions = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['uom-conversions', params], queryFn: () => api.uomsApi.getConversions(params).then(r => r.data) });
export function useWarehouses(includeInactive = false) {
  return useQuery({ queryKey: ['warehouses', includeInactive], queryFn: () => api.warehousesApi.list({ includeInactive }).then(r => r.data) });
}
export const useInventoryTransactions = (params?: Record<string, string>) =>
  usePagedQuery<api.InventoryTransactionDto>('inventory-transactions', api.inventoryApi.transactions, params);
export const useLowStock = () =>
  useQuery({ queryKey: ['inventory', 'low-stock'], queryFn: () => api.inventoryApi.lowStock().then(r => r.data) });
export const useInventoryValuation = () =>
  useQuery({ queryKey: ['inventory', 'valuation'], queryFn: () => api.inventoryApi.valuation().then(r => r.data) });

export const useBinLocations = (warehouseId?: string, isActive?: boolean) =>
  useQuery({
    queryKey: ['bin-locations', warehouseId, isActive],
    queryFn: async () => {
      try {
        const params: Record<string, string> = {};
        if (warehouseId) params.warehouseId = warehouseId;
        if (isActive !== undefined) params.isActive = isActive.toString();
        const res = await api.binLocationsApi.list(params);
        return res.data;
      } catch (e) {
        console.error('Failed to load bin locations:', e);
        return { success: true, data: [] };
      }
    },
  });

// ========== Sales ==========
export const useCustomers = (params?: Record<string, string>) =>
  usePagedQuery<api.CustomerDto>('customers', api.customersApi.list, params);
export const useSalesOrders = (params?: Record<string, string>) =>
  usePagedQuery<api.SalesOrderDto>('sales-orders', api.salesOrdersApi.list, params);
export const useCreateSalesOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (d: Record<string, unknown>) => api.salesOrdersApi.create(d).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['sales-orders'] }) });
};
export const useQuotes = (params?: Record<string, string>) =>
  usePagedQuery('quotes', api.quotesApi.list, params);
export const useInvoices = (params?: Record<string, string>) =>
  usePagedQuery('invoices', api.invoicesApi.list, params);
export const useAgingAnalysis = () =>
  useQuery({ queryKey: ['invoices', 'aging'], queryFn: () => api.invoicesApi.aging().then(r => r.data) });

export const useOrderToCash = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['sales-reports', 'order-to-cash', params], queryFn: () => api.salesReportsApi.orderToCash(params).then(r => r.data) });

// ========== Procurement ==========
export const useSuppliers = (params?: Record<string, string>) =>
  usePagedQuery<api.SupplierDto>('suppliers', api.suppliersApi.list, params);
export const usePurchaseOrders = (params?: Record<string, string>) =>
  usePagedQuery<api.PurchaseOrderDto>('purchase-orders', api.purchaseOrdersApi.list, params);
export const useCreatePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (d: Record<string, unknown>) => api.purchaseOrdersApi.create(d).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['purchase-orders'] }) });
};

export const useUpdatePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (variables: { id: string; data: Record<string, unknown> }) => api.purchaseOrdersApi.update(variables.id, variables.data).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['purchase-orders'] }) });
};
export const usePurchaseRequisitions = (params?: Record<string, string>) =>
  usePagedQuery<api.PurchaseRequisitionDto>('purchase-requisitions', api.purchaseRequisitionsApi.list, params);
export const useGoodsReceipts = (params?: Record<string, string>) =>
  usePagedQuery('goods-receipts', api.goodsReceiptsApi.list, params);
export const useAccountsPayable = (params?: Record<string, string>) =>
  usePagedQuery('accounts-payable', api.accountsPayableApi.list, params);

// ========== MES ==========


export const useBoms = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['boms', params], queryFn: () => api.bomsApi.list(params).then(r => r.data) });

export const useCreateBom = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: Record<string, unknown>) => api.bomsApi.create(data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['boms'] })
  })
}

export const useUpdateBom = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (variables: { id: string; data: Record<string, unknown> }) => api.bomsApi.update(variables.id, variables.data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['boms'] })
  })
}

export const useDeleteBom = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.bomsApi.delete(id).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['boms'] })
  })
}

export const useActivateBom = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.bomsApi.activate(id).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['boms'] })
  });
};

export const useDeprecateBom = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.bomsApi.deprecate(id).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['boms'] })
  });
};

export const useEcns = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['ecns', params], queryFn: () => api.ecnApi.list(params).then(r => r.data) });

export const useCreateEcn = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Record<string, unknown>) => api.ecnApi.create(data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['ecns'] })
  });
};
export const useBom = (id: string) =>
  useQuery({ queryKey: ['boms', id], queryFn: () => api.bomsApi.get(id).then(r => r.data), enabled: !!id });
export const useWorkOrders = (params?: Record<string, string>) =>
  usePagedQuery<api.WorkOrderDto>('work-orders', api.workOrdersApi.list, params);
export const useCreateWorkOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (d: Record<string, unknown>) => api.workOrdersApi.create(d).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['work-orders'] }) });
};
export const useUpdateWorkOrderStatus = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ id, status }: { id: string; status: string }) => api.workOrdersApi.updateStatus(id, status).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['work-orders'] }) });
};

export const useStartWorkOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (id: string) => api.workOrdersApi.start(id).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['work-orders'] }) });
};

export const useCompleteWorkOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ id, data }: { id: string; data: Record<string, unknown> }) => api.workOrdersApi.complete(id, data).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['work-orders'] }) });
};

export const useUpdateWorkOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Record<string, unknown> }) => api.workOrdersApi.update(id, data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['work-orders'] })
  });
};

export const useWorkOrderCostingSummary = () =>
  useQuery({ queryKey: ['work-orders-costing-summary'], queryFn: () => api.costingApi.summary().then(r => r.data) });
export const useWorkOrderCosting = (id: string) =>
  useQuery({ queryKey: ['work-orders', id, 'costing'], queryFn: () => api.costingApi.analysis(id).then(r => r.data), enabled: !!id });
export const useSettleCosting = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (id: string) => api.costingApi.settle(id).then(r => r.data), onSuccess: () => { qc.invalidateQueries({ queryKey: ['work-orders-costing-summary'] }); qc.invalidateQueries({ queryKey: ['work-orders'] }); } });
};
export const useBatchSettleCosting = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: () => api.costingApi.batchSettle().then(r => r.data), onSuccess: () => { qc.invalidateQueries({ queryKey: ['work-orders-costing-summary'] }); qc.invalidateQueries({ queryKey: ['work-orders'] }); } });
};
export const useWorkOrder = (id: string) =>
  useQuery({ queryKey: ['work-orders', id], queryFn: () => api.workOrdersApi.get(id).then(r => r.data), enabled: !!id });

export const useCreateWipReport = () => {
    const qc = useQueryClient();
    return useMutation({ 
        mutationFn: (d: Record<string, unknown>) => api.wipReportsApi.create(d).then(r => r.data), 
        onSuccess: (_, variables) => {
            qc.invalidateQueries({ queryKey: ['work-orders'] });
            qc.invalidateQueries({ queryKey: ['work-orders', (variables as any).workOrderId] });
        } 
    });
};

export const useExplodeRouting = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => api.workOrdersApi.explodeRouting(id).then(r => r.data),
        onSuccess: (_, id) => {
            qc.invalidateQueries({ queryKey: ['work-orders'] });
            qc.invalidateQueries({ queryKey: ['work-orders', id] });
        }
    });
};

export const useCreateSupplyOrders = () => {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => api.workOrdersApi.createSupplyOrders(id).then(r => r.data),
        onSuccess: (data: any, id) => {
            qc.invalidateQueries({ queryKey: ['work-orders'] });
            qc.invalidateQueries({ queryKey: ['work-orders', id] });
            const msg = `轉單成功！\n已生成 ${data.data.prCount} 份請購單\n已生成 ${data.data.subWoCount} 份子工單`;
            alert(msg);
        }
    });
};

// ========== Finance ==========
export const useLoans = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['finance', 'loans', params], queryFn: () => api.financeApi.loans(params).then(r => r.data) });
export const useBankAccounts = () =>
  useQuery({ queryKey: ['finance', 'bank-accounts'], queryFn: () => api.financeApi.bankAccounts().then(r => r.data) });
export const useInvestments = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['finance', 'investments', params], queryFn: () => api.financeApi.investments(params).then(r => r.data) });
export const useGuarantees = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['finance', 'guarantees', params], queryFn: () => api.financeApi.guarantees(params).then(r => r.data) });

// ========== Assets ==========
export const useAssets = (params?: Record<string, string>) =>
  usePagedQuery<api.AssetDto>('assets', api.assetsApi.list, params);
export const useAssetCategories = () =>
  useQuery({ queryKey: ['assets', 'categories'], queryFn: () => api.assetsApi.categories().then(r => r.data) });

// ========== Budget ==========
export const useBudgetYears = () =>
  useQuery({ 
    queryKey: ['budget', 'years'], 
    queryFn: () => api.budgetApi.years().then(r => r.data),
    staleTime: 0,
    gcTime: 0,
    refetchOnWindowFocus: true
  });
export const useBudgetVersions = (yearId: string) =>
  useQuery({ 
    queryKey: ['budget', 'versions', yearId], 
    queryFn: () => {
      console.log('[useBudgetVersions] 正在載入版本, yearId:', yearId);
      return api.budgetApi.versions(yearId).then(r => r.data);
    }, 
    enabled: !!yearId 
  });
export const useSalesForecasts = (versionId: string) =>
  useQuery({ queryKey: ['budget', 'sales-forecasts', versionId], queryFn: () => api.budgetApi.salesForecasts(versionId).then(r => r.data), enabled: !!versionId });
export const useExpenseBudgets = (versionId: string) =>
  useQuery({ queryKey: ['budget', 'expense-budgets', versionId], queryFn: () => api.budgetApi.expenseBudgets(versionId).then(r => r.data), enabled: !!versionId });
export const useLaborBudgets = (versionId: string) =>
  useQuery({ queryKey: ['budget', 'labor-budgets', versionId], queryFn: () => api.budgetApi.laborBudgets(versionId).then(r => r.data), enabled: !!versionId });
export const useCapexBudgets = (versionId: string) =>
  useQuery({ queryKey: ['budget', 'capex-budgets', versionId], queryFn: () => api.budgetApi.capexBudgets(versionId).then(r => r.data), enabled: !!versionId });
export const useBudgetAlerts = () =>
  useQuery({ queryKey: ['budget', 'alerts'], queryFn: async () => [] as api.BudgetAlertDto[] });
export const useBudgetAnomalies = () =>
  useQuery({ queryKey: ['budget', 'anomalies'], queryFn: async () => [] as api.BudgetAnomalyDto[] });
export const useBudgetAnalysis = (deptCode: string, year: number) =>
  useQuery({
    queryKey: ['budget', 'analysis', deptCode, year],
    queryFn: async () => null as api.BudgetAnalysisDto | null,
    enabled: !!deptCode,
  });

// ========== Quality ==========
export const useIqcReports = (params?: Record<string, string>) =>
  usePagedQuery<api.IqcReportDto>('quality-iqc', api.qualityApi.iqcReports, params);
export const useSupplierComplaints = (params?: Record<string, string>) =>
  usePagedQuery<api.SupplierComplaintDto>('quality-complaints', api.qualityApi.complaints, params);

// ========== R&D ==========
export const useRndProjects = (params?: Record<string, string>) =>
  usePagedQuery<api.RndProjectDto>('rd-projects', api.rndApi.projects, params);

export const useCreateRndProject = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Record<string, unknown>) => api.rndApi.createProject(data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['rd-projects'] })
  });
};
export const useRndDocuments = (params?: Record<string, string>) =>
  usePagedQuery('rd-documents', api.rndApi.documents, params);
export const useRndLogs = (params?: Record<string, string>) =>
  usePagedQuery<api.RndLogDto>('rd-logs', api.rndApi.logs, params);
export const useCreateRndLog = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Record<string, unknown>) => api.rndApi.createLog(data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['rd-logs'] })
  });
};
export const useTrialProduction = (params?: Record<string, string>) =>
  usePagedQuery<any>('trial-productions', api.trialProductionApi.list, params);
export const useCreateTrialProduction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Record<string, unknown>) => api.trialProductionApi.create(data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trial-productions'] })
  });
};
export const useMassProductionTransition = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Record<string, unknown>) => api.massProductionApi.transition(data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rd-projects'] });
      qc.invalidateQueries({ queryKey: ['boms'] });
    }
  });
};

// ========== ESG ==========
export const useEmissionRecords = (params?: Record<string, string>) =>
  usePagedQuery<api.EmissionRecordDto>('emission-records', api.esgApi.emissionRecords, params);
export const useEmissionSummary = () =>
  useQuery({ queryKey: ['esg', 'emission-summary'], queryFn: () => api.esgApi.emissionSummary().then(r => r.data) });
export const useEmissionSources = () =>
  useQuery({ queryKey: ['esg', 'emission-sources'], queryFn: () => api.esgApi.emissionSources().then(r => r.data) });
export const useEmissionFactors = (params?: Record<string, string>) =>
  useQuery({ queryKey: ['esg', 'emission-factors', params], queryFn: () => api.esgApi.emissionFactors(params).then(r => r.data) });
export const useReductionTargets = () =>
  useQuery({ queryKey: ['esg', 'reduction-targets'], queryFn: () => api.esgApi.reductionTargets().then(r => r.data) });
export const useSupplierSustainability = () =>
  useQuery({ queryKey: ['esg', 'supplier-sustainability'], queryFn: () => api.esgApi.supplierSustainability().then(r => r.data) });
export const useEnergyConsumption = () =>
  useQuery({ queryKey: ['esg', 'energy-consumption'], queryFn: () => api.esgApi.energyConsumption().then(r => r.data) });
export const useEsgReports = () =>
  useQuery({ queryKey: ['esg', 'reports'], queryFn: () => api.esgApi.reports().then(r => r.data) });
export const useAiInsights = (params?: Record<string, string>) =>
  usePagedQuery<api.AiInsightDto>('ai-insights', api.esgApi.aiInsights, params);

// ========== Collaboration ==========
export const useAnnouncements = (params?: Record<string, string>) =>
  usePagedQuery<api.AnnouncementDto>('announcements', api.collaborationApi.announcements, params);
export const useCreateAnnouncement = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (d: Record<string, unknown>) => api.collaborationApi.createAnnouncement(d).then(r => r.data), onSuccess: () => qc.invalidateQueries({ queryKey: ['announcements'] }) });
};
export const useMeetingRooms = () =>
  useQuery({ queryKey: ['collaboration', 'meeting-rooms'], queryFn: () => api.collaborationApi.meetingRooms().then(r => r.data) });
export const useCrossDeptTasks = (params?: Record<string, string>) =>
  usePagedQuery<api.CrossDeptTaskDto>('cross-dept-tasks', api.collaborationApi.tasks, params);

// ========== Calendar ==========
export const useCalendarEvents = (params?: Record<string, string>) =>
  usePagedQuery<api.CalendarEventDto>('calendar-events', api.calendarApi.events, params);

export const useCalendarRange = (startDate: string, endDate: string, enabled = true) =>
  useQuery({
    queryKey: ['calendar', 'range', startDate, endDate],
    queryFn: () => api.calendarApi.range(startDate, endDate).then(r => r.data),
    enabled: enabled && !!startDate && !!endDate,
  });

export const useCalendarEvent = (id: string) =>
  useQuery({
    queryKey: ['calendar', 'events', id],
    queryFn: () => api.calendarApi.getEvent(id).then(r => r.data),
    enabled: !!id,
  });

export const useCreateCalendarEvent = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (d: Record<string, unknown>) => api.calendarApi.createEvent(d).then(r => r.data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['calendar'] }); },
  });
};

export const useUpdateCalendarEvent = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Record<string, unknown> }) =>
      api.calendarApi.updateEvent(id, data).then(r => r.data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['calendar'] }); },
  });
};

export const useDeleteCalendarEvent = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.calendarApi.deleteEvent(id).then(r => r.data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['calendar'] }); },
  });
};

export const useCalendarCategories = () =>
  useQuery({
    queryKey: ['calendar', 'categories'],
    queryFn: () => api.calendarApi.categories().then(r => r.data),
  });

export const useCompanyHolidays = (year?: number) =>
  useQuery({
    queryKey: ['calendar', 'holidays', year],
    queryFn: () => api.calendarApi.holidays(year ? { year: String(year) } : undefined).then(r => r.data),
  });

export const useCreateCalendarCategory = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (d: Record<string, unknown>) => api.calendarApi.createCategory(d).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['calendar', 'categories'] }),
  });
};

export const useCreateMeetingRoom = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (d: Record<string, unknown>) => api.collaborationApi.createMeetingRoom(d).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['collaboration', 'meeting-rooms'] }),
  });
};

// ========== AI Agent Monitoring ==========
export const useAgentMetricsSummary = () =>
  useQuery({ queryKey: ['agent', 'metrics-summary'], queryFn: () => api.agentApi.metricsSummary().then(r => r.data), refetchInterval: 10000 });

export const useAgentLatencyHistory = (hours: number = 24) =>
  useQuery({ queryKey: ['agent', 'latency-history', hours], queryFn: () => api.agentApi.latencyHistory({ hours }).then(r => r.data), refetchInterval: 30000 });

export const useAgentQpsHistory = (hours: number = 24) =>
  useQuery({ queryKey: ['agent', 'qps-history', hours], queryFn: () => api.agentApi.qpsHistory({ hours }).then(r => r.data), refetchInterval: 30000 });

export const useAgentErrorHistory = (hours: number = 24) =>
  useQuery({ queryKey: ['agent', 'error-history', hours], queryFn: () => api.agentApi.errorHistory({ hours }).then(r => r.data), refetchInterval: 30000 });

export const useAgentFlowExecutions = (params?: { flowType?: string; status?: string }) =>
  useQuery({ queryKey: ['agent', 'flow-executions', params], queryFn: () => api.agentApi.flowExecutions(params).then(r => r.data), refetchInterval: 15000 });

export const useTriggerAgentFlow = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ flowType, data }: { flowType: string; data: Record<string, unknown> }) =>
      api.agentApi.triggerFlow(flowType, data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['agent', 'flow-executions'] }),
  });
};

export const useAgentTopology = () =>
  useQuery({ queryKey: ['agent', 'topology'], queryFn: () => api.agentApi.topology().then(r => r.data), refetchInterval: 30000 });

// ========== AI Planning ==========
export const usePlanningCreatePlan = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: api.PlanningRequest) => api.planningApi.createPlan(req).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['planning'] }),
  });
};

export const usePlanningExecutePlan = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: api.ExecutionRequest) => api.planningApi.executePlan(req).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['planning'] }),
  });
};

export const usePlanningReview = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: api.ReviewRequest) => api.planningApi.reviewResult(req).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['planning'] }),
  });
};

export const usePlanningKpi = () =>
  useQuery({ queryKey: ['planning', 'kpi'], queryFn: () => api.planningApi.getKpiValidation().then(r => r.data) });

export const usePlanningHealth = () =>
  useQuery({ queryKey: ['planning', 'health'], queryFn: () => api.planningApi.getHealth().then(r => r.data) });

export const usePlanningTools = () =>
  useQuery({ queryKey: ['planning', 'tools'], queryFn: () => api.planningApi.getAvailableTools().then(r => r.data) });

export const usePlanningToolExecute = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: api.ToolExecuteRequest) => api.planningApi.executeTool(req).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['planning'] }),
  });
};
