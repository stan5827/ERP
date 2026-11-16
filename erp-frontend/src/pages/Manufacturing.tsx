import React, { useCallback, useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { 
  Search, Plus, Download, Eye, Edit2,
  ChevronDown, Send, Users, Loader2,
  Check, X, Activity, AlertCircle,
  Zap, Calendar, Calculator, Package
} from 'lucide-react';
import { 
  useWorkOrders, useCreateWorkOrder, useUpdateWorkOrder, useUpdateWorkOrderStatus, 
  useStartWorkOrder, useSkus, useBoms,
  useWorkOrder, useCreateWipReport, useExplodeRouting, useCreateSupplyOrders, useUoms, useEmployees, useCurrentUser
} from '../hooks/useApi';
import type { WorkOrderDto } from '../api/api';
import { equipmentApi } from '../api/api';
import { wipReportsApi } from '../api/mes';
import type { WipReportQueueItemDto, WipReportRequest } from '../api/mes';
import type { ModuleId } from '../types';
import { useRole } from '../contexts/useRole';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import './styles/Manufacturing-Cyber.css';
import { WorkOrderCostReport } from './WorkOrderCostReport';
import { MaterialIssueWorkflow } from './MaterialIssueWorkflow';
import { AgentWipFactsPanel } from './AgentWipFactsPanel';

const statusDisplay: Record<string, { label: string; className: string }> = {
  pending: { label: '待發放', className: 'cy-status-pending' },
  planned: { label: '計畫中', className: 'cy-status-pending' },
  released: { label: '已發放', className: 'cy-status-pending' },
  in_progress: { label: '生產中', className: 'cy-status-active' },
  completed: { label: '已完工', className: 'cy-status-complete' },
  closed: { label: '已結案', className: 'cy-status-complete' },
};

const priorityDisplay: Record<string, { label: string; color: string }> = {
  urgent: { label: '緊急', color: '#f43f5e' },
  high: { label: '高', color: '#fbbf24' },
  normal: { label: '一般', color: '#38bdf8' },
  low: { label: '低', color: '#94a3b8' },
};

const getMaterialIssueProgress = (materials: WorkOrderDto['materials']) => {
  if (!materials?.length)
    return null;

  const issuedCount = materials.filter(material => material.issuedQty > 0).length;
  const unissuedCount = materials.filter(material => material.unissuedQty > 0).length;
  const closedCount = materials.filter(material => material.closedShortageQty > 0).length;
  const label = unissuedCount > 0
    ? issuedCount > 0 ? '部分領料' : '尚未領料'
    : closedCount > 0 ? `領料已結案（短少 ${closedCount} 項）` : '已領足';
  const color = unissuedCount === 0 ? '#34d399' : issuedCount > 0 ? '#fbbf24' : '#94a3b8';
  const formatQty = (quantity: number) => quantity.toLocaleString('zh-TW', { maximumFractionDigits: 2 });
  const details = materials.map(material => {
    const unit = material.unit || material.inventoryUnit || '';
    const closedText = material.closedShortageQty > 0
      ? `，短少結案 ${formatQty(material.closedShortageQty)} ${unit}`
      : '';
    return `${material.skuNumber || material.skuName || '物料'}：已領 ${formatQty(material.issuedQty)} / ${formatQty(material.requiredQty)} ${unit}，未領 ${formatQty(material.unissuedQty)} ${unit}${closedText}`;
  });

  return { label, color, issuedCount, totalCount: materials.length, details };
};

const reportLabelStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  gap: 7,
  color: '#cbd5e1',
  fontSize: 13,
  fontWeight: 600,
};

const reportInputStyle: React.CSSProperties = {
  width: '100%',
  minHeight: 44,
  padding: '10px 12px',
  borderRadius: 8,
  border: '1px solid rgba(148, 163, 184, 0.3)',
  background: '#111c31',
  color: '#f8fafc',
  fontSize: 15,
};

interface EquipmentMachineHourDraft {
  equipmentId: string;
  machineHours: string;
  startTime: string;
  endTime: string;
}

const toLocalDateTimeInput = (value: Date) =>
  new Date(value.getTime() - value.getTimezoneOffset() * 60000).toISOString().slice(0, 19);

const toLocalDateInput = (value: Date) =>
  new Date(value.getTime() - value.getTimezoneOffset() * 60000).toISOString().slice(0, 10);

const toLocalTimeInput = (value: string) => value.length >= 19 ? value.slice(11, 19) : '';

const combineLocalDateTime = (date: string, time: string) => time ? `${date}T${time}` : '';

const calculateHours = (startTime: string, endTime: string) => {
  if (!startTime || !endTime) return 0;
  const milliseconds = new Date(endTime).getTime() - new Date(startTime).getTime();
  return milliseconds > 0 ? Number((milliseconds / 3600000).toFixed(2)) : 0;
};

interface WorkOrderOperation {
  id: string;
  operationSeq: number;
  operationName: string;
  workCenterName?: string;
  plannedQty: number;
  completedQty: number;
  status: string;
}

interface DetailedWorkOrder extends WorkOrderDto {
  operations?: WorkOrderOperation[];
}

interface BomComponent {
  id?: string;
  childSkuId: string;
  childSkuNumber?: string;
  childSkuName?: string;
  quantity: number;
  unit?: string;
  scrapRate?: number;
}

interface MaterialRow {
  id: string;
  skuNumber?: string;
  skuName?: string;
  unit?: string;
  inventoryUnit?: string;
  standardQty: number;
  requiredQty: number;
  availableQty: number;
  shortageQty: number;
  status: string;
}

interface ManufacturingProps {
  onNavigate?: (module: ModuleId) => void;
}

export const Manufacturing: React.FC<ManufacturingProps> = ({ onNavigate }) => {
  const { hasPermission } = useRole();
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [showForm, setShowForm] = useState(false);
  const [selectedWo, setSelectedWo] = useState<WorkOrderDto | null>(null);
  const [showDetails, setShowDetails] = useState(false);
  const [showMaterialIssue, setShowMaterialIssue] = useState(false);
  const [showCostReport, setShowCostReport] = useState(false);
  const [showReportForm, setShowReportForm] = useState(false);
  const [showReportReview, setShowReportReview] = useState(false);
  const [correctionReportId, setCorrectionReportId] = useState<string | null>(null);
  const [reportToCorrect, setReportToCorrect] = useState<WipReportQueueItemDto | null>(null);
  const [showEquipmentPicker, setShowEquipmentPicker] = useState(false);
  const [equipmentSearch, setEquipmentSearch] = useState('');
  const [selectedOp, setSelectedOp] = useState<WorkOrderOperation | null>(null);
  const [reportingMode, setReportingMode] = useState(false);
  const [reportDraft, setReportDraft] = useState({
    employeeId: '',
    reportType: 'individual' as 'individual' | 'team',
    teamMemberEmployeeIds: [] as string[],
    teamMemberHours: {} as Record<string, string>,
    proxyReason: '',
    shiftClass: 'day',
    equipmentMachineHours: [] as EquipmentMachineHourDraft[],
    completedQty: '0',
    defectQty: '0',
    scrapQty: '0',
    defectReasonCode: '',
    setupHours: '0',
    runHours: '0',
    manHours: '0',
    machineHours: '0',
    workDate: '',
    startTime: '',
    endTime: '',
    materialLotNumber: '',
    finishedLotNumber: '',
    remarks: '',
  });
  const [isSavingEquipmentUsage, setIsSavingEquipmentUsage] = useState(false);
  
  const queryClient = useQueryClient();
  const { data, isLoading } = useWorkOrders();
  const workOrders: WorkOrderDto[] = data?.data ?? [];
  const { data: skuData } = useSkus({ pageSize: '9999' });
  const skus = skuData?.data ?? [];
  const { data: bomData } = useBoms();
  const boms = bomData?.data ?? [];
  const { data: uomData } = useUoms();
  const { data: employeeData, isLoading: isEmployeesLoading, isError: isEmployeesError } = useEmployees({ pageSize: '500' });
  const { data: equipmentResponse, isLoading: isEquipmentLoading, isError: isEquipmentError } = useQuery({
    queryKey: ['report-equipment'],
    queryFn: () => equipmentApi.list({ status: 'active' }).then(response => response.data),
  });
  const employees = (employeeData?.data ?? []).filter(employee => employee.status === 'active');
  const currentUser = useCurrentUser();
  const currentEmployeeId = currentUser?.employeeId ?? '';
  const canProxyReport = currentUser?.roles.includes('Admin')
    || currentUser?.roles.includes('Production Supervisor')
    || currentUser?.permissions.includes('manufacturing:proxy-report')
    || currentUser?.permissions.includes('admin.*');
  const canApproveReports = currentUser?.roles.includes('Admin')
    || currentUser?.permissions.includes('manufacturing:approve')
    || currentUser?.permissions.includes('admin.*');
  const myReportsQuery = useQuery({
    queryKey: ['wip-reports', 'my'],
    queryFn: () => wipReportsApi.my().then(response => response.data.data ?? []),
    enabled: Boolean(currentUser),
  });
  const reviewQueueQuery = useQuery({
    queryKey: ['wip-reports', 'review-queue'],
    queryFn: () => wipReportsApi.reviewQueue().then(response => response.data.data ?? []),
    enabled: Boolean(canApproveReports),
  });
  const equipment = equipmentResponse?.data ?? [];
  const totalSelectedMachineHours = reportDraft.equipmentMachineHours.reduce(
    (total, item) => total + (Number(item.machineHours) || 0),
    0
  );
  useEffect(() => {
    if (reportDraft.equipmentMachineHours.length === 0) return;
    const totalMachineHours = totalSelectedMachineHours.toFixed(2);
    setReportDraft(current => current.runHours === totalMachineHours
      ? current
      : { ...current, runHours: totalMachineHours });
  }, [totalSelectedMachineHours, reportDraft.equipmentMachineHours.length]);
  const uomNamesByCode = new Map(
    (uomData?.data ?? []).map(uom => [uom.uomCode, uom.uomName])
  );
  const getUnitName = (unitCode?: string) => unitCode
    ? uomNamesByCode.get(unitCode) ?? unitCode
    : '';
  const productionUnitCode = selectedWo?.productionUomCode;
  const productionUnit = getUnitName(productionUnitCode) || '單位未確認';
  const openReportForOperation = useCallback((operation: WorkOrderOperation) => {
    const now = new Date();
    const localDateTime = toLocalDateTimeInput(now);
    const localStartTime = toLocalDateTimeInput(new Date(now.getTime() - 60 * 60 * 1000));
    setSelectedOp(operation);
    setCorrectionReportId(null);
    setReportDraft({
      employeeId: currentEmployeeId,
      reportType: 'individual',
      teamMemberEmployeeIds: [],
      teamMemberHours: currentEmployeeId ? { [currentEmployeeId]: '0' } : {},
      proxyReason: '',
      shiftClass: 'day',
      equipmentMachineHours: [],
      completedQty: '0',
      defectQty: '0',
      scrapQty: '0',
      defectReasonCode: '',
      setupHours: '0',
      runHours: '0',
      manHours: '0',
      machineHours: '0',
      workDate: localStartTime.slice(0, 10),
      startTime: localStartTime,
      endTime: localDateTime,
      materialLotNumber: '',
      finishedLotNumber: '',
      remarks: '',
    });
    setShowEquipmentPicker(false);
    setEquipmentSearch('');
    setShowReportForm(true);
  }, [currentEmployeeId]);

  const createMutation = useCreateWorkOrder();
  const updateMutation = useUpdateWorkOrder();
  const updateStatusMutation = useUpdateWorkOrderStatus();
  const startMutation = useStartWorkOrder();
  const createWipMutation = useCreateWipReport();
  const explodeMutation = useExplodeRouting();
  const supplyMutation = useCreateSupplyOrders();
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create');

  // 添加錯誤處理
  const handleMutationError = (error: unknown) => {
    if (typeof error === 'object' && error !== null && 'response' in error) {
      const response = error.response;
      if (typeof response === 'object' && response !== null && 'data' in response) {
        const data = response.data;
        if (typeof data === 'object' && data !== null && 'message' in data && typeof data.message === 'string') {
          alert(data.message);
          return;
        }
      }
    }
    alert(error instanceof Error ? error.message : '操作失敗');
  };
  
  // 獲取選中工單的詳細資料（含工序）
  const {
    data: woDetail,
    isLoading: isWorkOrderLoading,
    isError: isWorkOrderError,
    refetch: refetchWorkOrder
  } = useWorkOrder(selectedWo?.id || '');
  const detailedWo = woDetail?.data as DetailedWorkOrder | undefined;
  const materialUsageSummary = (detailedWo?.materials ?? selectedWo?.materials ?? [])
    .map(material => {
      const quantity = material.requiredQty.toLocaleString('zh-TW', { maximumFractionDigits: 2 });
      const unit = getUnitName(material.unit || material.inventoryUnit) || '單位未設定';
      return `${quantity} ${unit}`;
    })
    .join('、') || '未設定物料';

  React.useEffect(() => {
    if (!reportToCorrect || !detailedWo || selectedWo?.woNumber !== reportToCorrect.workOrderNumber) return;
    const operation = detailedWo.operations?.find(item => Number(item.operationSeq) === reportToCorrect.operationSeq);
    if (!operation) {
      alert('找不到退回報工對應的工序，無法重新編輯。');
      setReportToCorrect(null);
      return;
    }
    const asLocalInput = (value?: string) => {
      if (!value) return '';
      const date = new Date(value);
      return toLocalDateTimeInput(date);
    };
    setSelectedOp(operation);
    setReportDraft(current => ({
      ...current,
      employeeId: reportToCorrect.employeeId,
      reportType: reportToCorrect.reportType,
      teamMemberEmployeeIds: reportToCorrect.teamMemberEmployeeIds ?? [],
      teamMemberHours: (reportToCorrect.teamMemberHours ?? []).reduce<Record<string, string>>(
        (hours, participant) => ({ ...hours, [participant.employeeId]: String(participant.manHours) }),
        {}
      ),
      proxyReason: reportToCorrect.proxyReason ?? '',
      shiftClass: reportToCorrect.shiftClass,
      completedQty: String(reportToCorrect.completedQty),
      defectQty: String(reportToCorrect.defectQty),
      scrapQty: String(reportToCorrect.scrapQty),
      defectReasonCode: reportToCorrect.defectReasonCode ?? '',
      setupHours: String(reportToCorrect.setupHours),
      runHours: String(reportToCorrect.runHours),
      manHours: String(reportToCorrect.manHours),
      workDate: asLocalInput(reportToCorrect.startTime).slice(0, 10),
      startTime: asLocalInput(reportToCorrect.startTime),
      endTime: asLocalInput(reportToCorrect.endTime),
      materialLotNumber: reportToCorrect.materialLotNumber ?? '',
      finishedLotNumber: reportToCorrect.finishedLotNumber ?? '',
      remarks: reportToCorrect.remarks ?? '',
    }));
    setCorrectionReportId(reportToCorrect.id);
    setShowReportReview(false);
    setShowReportForm(true);
    setReportToCorrect(null);
  }, [reportToCorrect, detailedWo, selectedWo]);

  const filtered = workOrders.filter(wo =>
    (filterStatus === 'all' || wo.status === filterStatus) &&
    (wo.woNumber.includes(search)
      || (wo.productSkuName ?? '').includes(search)
      || (wo.salesOrderNumber ?? '').includes(search))
  );

  const getProgress = (wo: WorkOrderDto) =>
    wo.plannedQty > 0 ? Math.round((wo.actualQty / wo.plannedQty) * 100) : 0;

  const handleSubmitForm = async (values: Record<string, unknown>) => {
    const payload = {
      ...values,
      plannedQty: Number(values.plannedQty),
    };
    
    if (formMode === 'edit' && selectedWo) {
      await updateMutation.mutateAsync({ id: selectedWo.id, data: payload });
    } else {
      await createMutation.mutateAsync(payload);
    }
    
    queryClient.invalidateQueries({ queryKey: ['work-orders'] });
    setShowForm(false);
    setSelectedWo(null);
  };

  const handleReportSubmit = async () => {
    if (!selectedWo || !selectedOp) return;
    const completedQty = Number(reportDraft.completedQty);
    const defectQty = Number(reportDraft.defectQty);
    const scrapQty = Number(reportDraft.scrapQty);
    if (!currentEmployeeId) {
      alert('目前登入帳號尚未綁定員工，請聯絡管理員完成帳號與員工對應。');
      return;
    }
    if (reportDraft.reportType === 'team' && reportDraft.teamMemberEmployeeIds.length === 0) {
      alert('班組報工請至少選擇一位共同作業人員。');
      return;
    }
    if (reportDraft.employeeId !== currentEmployeeId && (!canProxyReport || !reportDraft.proxyReason.trim())) {
      alert('主管代填必須具備代填權限並填寫原因。');
      return;
    }
    if (![completedQty, defectQty, scrapQty].every(Number.isFinite) ||
        completedQty < 0 || defectQty < 0 || scrapQty < 0 ||
        completedQty + defectQty + scrapQty <= 0) {
      alert('良品、不良品與報廢數量需為非負數，且合計必須大於零。');
      return;
    }
    if ((defectQty > 0 || scrapQty > 0) && !reportDraft.defectReasonCode.trim()) {
      alert('有不良或報廢數量時，請選擇不良原因。');
      return;
    }
    if (reportDraft.startTime && reportDraft.endTime &&
        new Date(reportDraft.endTime) <= new Date(reportDraft.startTime)) {
      alert('報工結束時間必須晚於開始時間。');
      return;
    }
    if (reportDraft.startTime && reportDraft.endTime &&
        new Date(reportDraft.endTime).getTime() - new Date(reportDraft.startTime).getTime() > 24 * 60 * 60 * 1000) {
      alert('每筆報工時段不可超過 24 小時，跨日或換班請分開填報。');
      return;
    }
    const finalOperationSeq = Math.max(...(detailedWo?.operations ?? []).map(operation => Number(operation.operationSeq)));
    if (Number(selectedOp.operationSeq) === finalOperationSeq && !reportDraft.finishedLotNumber.trim()) {
      alert('最後工序報工必須填寫成品批號，才能建立對應的 FQC 批次。');
      return;
    }

    const setupHours = Number(reportDraft.setupHours);
    const runHours = Number(reportDraft.runHours);
    const teamMemberHours = reportDraft.reportType === 'team'
      ? [currentEmployeeId, ...reportDraft.teamMemberEmployeeIds].map(employeeId => ({
        employeeId,
        manHours: Number(reportDraft.teamMemberHours[employeeId] ?? '0'),
      }))
      : [];
    if (teamMemberHours.some(item => !Number.isFinite(item.manHours) || item.manHours < 0)) {
      alert('請為班組每位成員輸入有效的人工工時。');
      return;
    }
    const manHours = reportDraft.reportType === 'team'
      ? teamMemberHours.reduce((total, item) => total + item.manHours, 0)
      : Number(reportDraft.manHours);
    const payload: WipReportRequest = {
      woNumber: selectedWo.woNumber,
      operationSeq: Number(selectedOp.operationSeq),
      employeeId: reportDraft.employeeId || currentEmployeeId,
      reportType: reportDraft.reportType,
      teamMemberEmployeeIds: reportDraft.teamMemberEmployeeIds,
      teamMemberHours,
      proxyReason: reportDraft.employeeId !== currentEmployeeId ? reportDraft.proxyReason.trim() : undefined,
      shiftClass: reportDraft.shiftClass,
      completedQty,
      defectQty,
      scrapQty,
      defectReasonCode: reportDraft.defectReasonCode || undefined,
      setupHours,
      runHours,
      manHours,
      machineHours: 0,
      startTime: reportDraft.startTime ? new Date(reportDraft.startTime).toISOString() : undefined,
      endTime: reportDraft.endTime ? new Date(reportDraft.endTime).toISOString() : undefined,
      materialLotNumber: reportDraft.materialLotNumber || undefined,
      finishedLotNumber: reportDraft.finishedLotNumber.trim() || undefined,
      remarks: reportDraft.remarks || undefined,
    };

    try {
      const response = correctionReportId
        ? (await wipReportsApi.resubmit(correctionReportId, payload)).data
        : await createWipMutation.mutateAsync(payload);
      const report = response.data;
      setShowReportForm(false);
      setCorrectionReportId(null);
      await refetchWorkOrder();
      queryClient.invalidateQueries({ queryKey: ['work-orders'] });
      alert(`${correctionReportId ? '修正後報工已重新送出' : '報工已送出待審'}！報工單號：${report?.reportNumber ?? '已建立'}\n核准前不會計入正式生產進度。`);
    } catch (error) {
      handleMutationError(error);
    }
  };

  const handleEquipmentUsageSubmit = async () => {
    if (!selectedWo || !selectedOp || reportDraft.equipmentMachineHours.length === 0) {
      alert('請選擇設備並輸入各台設備的機台工時。');
      return;
    }
    const invalidUsage = reportDraft.equipmentMachineHours.some(item =>
      !Number.isFinite(Number(item.machineHours)) || Number(item.machineHours) <= 0 ||
      !item.startTime || !item.endTime || new Date(item.endTime) <= new Date(item.startTime)
    );
    if (invalidUsage) {
      alert('請為每台選擇的設備設定有效開始時間、結束時間及大於零的機台工時。');
      return;
    }

    setIsSavingEquipmentUsage(true);
    let savedCount = 0;
    try {
      for (const usage of reportDraft.equipmentMachineHours) {
        await wipReportsApi.createEquipmentUsage({
          workOrderId: selectedWo.id,
          operationSeq: Number(selectedOp.operationSeq),
          equipmentId: usage.equipmentId,
          machineHours: Number(usage.machineHours),
          shiftClass: reportDraft.shiftClass,
          startTime: new Date(usage.startTime).toISOString(),
          endTime: new Date(usage.endTime).toISOString(),
          remarks: reportDraft.remarks || undefined,
        });
        savedCount++;
      }
      alert(`已另行建立 ${savedCount} 筆設備使用紀錄。`);
      setReportDraft(current => ({ ...current, equipmentMachineHours: [] }));
    } catch (error) {
      if (savedCount > 0) {
        alert(`已建立 ${savedCount} 筆設備使用紀錄；後續設備儲存失敗，請檢查設備清單後重試。`);
      } else {
      handleMutationError(error);
      }
    } finally {
      setIsSavingEquipmentUsage(false);
    }
  };

  const handleReviewReport = async (report: WipReportQueueItemDto, approve: boolean) => {
    const comments = approve
      ? window.prompt(`核准報工 ${report.reportNumber}？（可填審核備註）`, '') ?? null
      : window.prompt(`請輸入退回報工 ${report.reportNumber} 的原因：`, '') ?? null;
    if (comments === null || (!approve && !comments.trim())) return;

    try {
      if (approve) {
        await wipReportsApi.approve(report.id, comments || undefined);
      } else {
        await wipReportsApi.returnForCorrection(report.id, comments.trim());
      }
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['wip-reports'] }),
        queryClient.invalidateQueries({ queryKey: ['work-orders'] }),
      ]);
    } catch (error) {
      handleMutationError(error);
    }
  };

  // 智慧報工引導邏輯
  React.useEffect(() => {
    if (reportingMode && detailedWo && detailedWo.id === selectedWo?.id) {
      const ops = detailedWo.operations || [];
      if (ops.length === 1) {
        openReportForOperation(ops[0]);
        setReportingMode(false);
        setShowDetails(false); // 直接報工，不需要看詳情
      }
    }
  }, [detailedWo, openReportForOperation, reportingMode, selectedWo]);

  const formFields: FormField[] = [
    { name: 'woType', label: '類型', type: 'select', description: '一般：正常生產工單；重工：不良品返修或重新加工工單。', options: [
      { value: 'regular', label: '一般' }, { value: 'rework', label: '重工' }
    ] },
    { name: 'productSkuId', label: '產品', type: 'select', required: true,
      options: skus.map(s => ({ value: s.id, label: `${s.skuName} (${s.skuNumber})${s.productionUomCode ? ` · ${s.productionUomCode}` : ''}` }))
    },
    { name: 'plannedQty', label: '計劃數量（生產單位）', type: 'number', description: '依所選商品的生產單位計量，工單會保存建立時的單位。', required: true, min: 1 },
    { name: 'plannedStartDate', label: '計劃開始日', type: 'date' },
    { name: 'plannedEndDate', label: '計劃結束日', type: 'date' },
    { name: 'notes', label: '備註', type: 'textarea' },
  ];

  return (
    <div className="manufacturing-container">
      {/* Header Section */}
      <header className="cy-header">
        <div className="cy-title-group">
          <div className="cy-badge-label">Active Matrix Control</div>
          <h1>製造指揮中心</h1>
        </div>
        <div style={{ display: 'flex', gap: '1rem' }}>
          {currentUser && (
            <button className="btn-cy" onClick={() => setShowReportReview(true)}>
              <Users size={16} /> 我的報工 / 審核
              {canApproveReports && (reviewQueueQuery.data?.length ?? 0) > 0 && ` (${reviewQueueQuery.data?.length})`}
            </button>
          )}
          <button className="btn-cy" style={{ background: 'transparent', border: '1px solid rgba(255,255,255,0.1)' }}>
            <Download size={16} /> 導出數據
          </button>
          <button className="btn-cy" onClick={() => { setFormMode('create'); setSelectedWo(null); setShowForm(true); }}>
            <Plus size={18} /> 建立工單
          </button>
        </div>
      </header>

      <AgentWipFactsPanel />

      {/* Statistics Section */}
      <section className="cy-stats-grid">
        <div className="cy-stat-card">
          <div className="cy-stat-label">當前活動工單</div>
          <div className="cy-stat-value">{workOrders.length}</div>
          <div className="cy-stat-trend">▲ 2.5% vs Last Week</div>
        </div>
        <div className="cy-stat-card">
          <div className="cy-stat-label">產線稼動率</div>
          <div className="cy-stat-value">92.4%</div>
          <div className="cy-stat-trend" style={{ color: '#fbbf24' }}>● Normal Operation</div>
        </div>
        <div className="cy-stat-card">
          <div className="cy-stat-label">本月完工數</div>
          <div className="cy-stat-value">1,248</div>
          <div className="cy-stat-trend">▲ 8.1% vs Goal</div>
        </div>
        <div className="cy-stat-card">
          <div className="cy-stat-label">警示項目</div>
          <div className="cy-stat-value" style={{ color: '#f43f5e' }}>{workOrders.filter(w => w.priority === 'urgent').length}</div>
          <div className="cy-stat-trend" style={{ color: '#f43f5e' }}>Critical Attention</div>
        </div>
      </section>

      {/* Main Panel Section */}
      <div className="cy-panel">
        <div className="cy-controls">
          <div className="cy-search-wrapper">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500" size={16} />
            <input 
              className="cy-search-input" 
              placeholder="搜尋工單 ID、產品名稱或技術編碼..."
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
          <div style={{ display: 'flex', background: 'rgba(0,0,0,0.2)', padding: '0.25rem', borderRadius: '0.75rem', border: '1px solid rgba(255,255,255,0.05)' }}>
            {['all', 'planned', 'pending', 'released', 'in_progress', 'completed'].map(s => (
              <button
                key={s}
                onClick={() => setFilterStatus(s)}
                style={{
                  padding: '0.5rem 1rem',
                  borderRadius: '0.5rem',
                  fontSize: '0.8rem',
                  transition: 'all 0.2s',
                  background: filterStatus === s ? 'rgba(0, 242, 255, 0.1)' : 'transparent',
                  color: filterStatus === s ? '#00f2ff' : '#64748b',
                  border: 'none',
                  cursor: 'pointer'
                }}
              >
                {s === 'all' ? '全部資源' : statusDisplay[s]?.label || s}
              </button>
            ))}
          </div>
        </div>

        <div className="cy-table-wrapper">
          <table className="cy-table">
            <thead>
              <tr>
                <th>工單編號</th>
                <th>訂單編號</th>
                <th>產品規格 / 品名</th>
                <th>優先等級</th>
                <th>執行進度</th>
                <th>預計交付</th>
                <th>當前狀態</th>
                <th>領料進度</th>
                <th style={{ textAlign: 'right' }}>系統操作</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={9} style={{ textAlign: 'center', padding: '3rem' }}>系統同步中...</td></tr>
              ) : filtered.map((wo) => (
                <tr key={wo.id} className="cy-row">
                  <td><span className="cy-wo-number">{wo.woNumber}</span></td>
                  <td>{wo.salesOrderNumber || '未關聯訂單'}</td>
                  <td>
                    <div style={{ fontWeight: 600 }}>{wo.productSkuName}</div>
                    <div style={{ fontSize: '0.7rem', color: '#64748b' }}>{wo.productSkuNumber}</div>
                  </td>
                  <td>
                    <span style={{ color: priorityDisplay[wo.priority]?.color, fontWeight: 700, fontSize: '0.8rem' }}>
                      {priorityDisplay[wo.priority]?.label}
                    </span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                      <div className="cy-progress-track">
                        <div className="cy-progress-fill" style={{ width: `${getProgress(wo)}%` }} />
                      </div>
                      <span style={{ fontSize: '0.75rem', fontFamily: 'monospace' }}>{getProgress(wo)}%</span>
                    </div>
                  </td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.8rem' }}>
                      <Calendar size={14} className="text-slate-500" />
                      {wo.plannedEndDate ? new Date(wo.plannedEndDate).toLocaleDateString() : '---'}
                    </div>
                  </td>
                  <td>
                    <span className={`cy-status ${statusDisplay[wo.status]?.className}`}>
                      {statusDisplay[wo.status]?.label}
                    </span>
                  </td>
                  <td>
                    {(() => {
                      const progress = getMaterialIssueProgress(wo.materials);
                      return progress ? (
                        <div style={{ minWidth: 190, maxWidth: 320, fontSize: '0.72rem' }}>
                          <div style={{ color: progress.color, fontWeight: 700, marginBottom: 4 }}>
                            {progress.label}（{progress.issuedCount}/{progress.totalCount} 項）
                          </div>
                          <div title={progress.details.join('\n')} style={{ color: '#94a3b8', lineHeight: 1.5 }}>
                            {progress.details.slice(0, 2).map((detail, index) => (
                              <div key={wo.materials![index].id} style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                {detail}
                              </div>
                            ))}
                            {progress.details.length > 2 && <div>另有 {progress.details.length - 2} 項（滑鼠停留查看）</div>}
                          </div>
                        </div>
                      ) : (
                        <span style={{ color: '#64748b', fontSize: '0.72rem' }}>無用料資料</span>
                      );
                    })()}
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                      {(wo.status === 'pending' || wo.status === 'planned') && (
                        <button className="cy-action-btn" style={{ color: 'var(--cobalt-400)' }} onClick={() => updateStatusMutation.mutate(
                          { id: wo.id, status: 'released' },
                          {
                            onSuccess: () => alert(`工單 ${wo.woNumber} 已發放。`),
                            onError: handleMutationError,
                          },
                        )} disabled={updateStatusMutation.isPending} title="發放工單">
                          <Send size={14} />
                        </button>
                      )}
                      
                      {wo.status === 'released' && hasPermission('manufacturing', 'execute') && (
                        <button className="cy-action-btn" style={{ color: 'var(--emerald-400)' }} onClick={() => startMutation.mutate(wo.id, {
                          onSuccess: () => alert(`工單 ${wo.woNumber} 已開始生產。`),
                          onError: handleMutationError,
                        })} disabled={startMutation.isPending} title="開始生產">
                          <Zap size={14} />
                        </button>
                      )}

                      {wo.status === 'in_progress' && (
                        <button className="cy-action-btn" style={{ color: 'var(--purple-400)' }} onClick={() => {
                          setSelectedWo(wo);
                          setReportingMode(true);
                          setShowDetails(true);
                        }} title="工序報工">
                          <Zap size={14} className={reportingMode && selectedWo?.id === wo.id ? 'animate-pulse' : ''} />
                        </button>
                      )}

                      <button className="cy-action-btn" onClick={() => { setSelectedWo(wo); setFormMode('edit'); setShowForm(true); }} title="修改計畫">
                        <Edit2 size={14} />
                      </button>

                      <button className="cy-action-btn" onClick={() => { setSelectedWo(wo); setShowDetails(true); }} title="詳情">
                        <Eye size={14} />
                      </button>

                      <button className="cy-action-btn" style={{ color: '#fbbf24' }} onClick={() => { setSelectedWo(wo); setShowCostReport(true); }} title="成本分析">
                        <Calculator size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Details Modal (Optimized) */}
      <AnimatePresence>
        {showDetails && selectedWo && (
          <div 
            style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.85)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px' }} 
            onClick={() => { setShowDetails(false); setReportingMode(false); }}
          >
            <motion.div 
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              onClick={e => e.stopPropagation()}
              className="cy-panel"
              style={{ width: '1000px', maxWidth: '95vw', maxHeight: '90vh', overflowY: 'auto', padding: '2.5rem', position: 'relative', border: '1px solid var(--cy-border)' }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem' }}>
                <div>
                  <div className="cy-badge-label">Work Order Details</div>
                  <h2 style={{ fontSize: '1.5rem', color: '#fff' }}>{selectedWo.woNumber}</h2>
                </div>
                <button onClick={() => setShowDetails(false)} style={{ background: 'none', border: 'none', color: '#64748b', cursor: 'pointer' }}>
                  <X size={24} />
                </button>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem', marginBottom: '2rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>產品名稱</label>
                  <div style={{ color: '#fff', fontWeight: 600 }}>{selectedWo.productSkuName}</div>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>訂單編號</label>
                  <div style={{ color: '#fff', fontWeight: 600 }}>{selectedWo.salesOrderNumber || '未關聯訂單'}</div>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>當前狀態</label>
                  <span className={`cy-status ${statusDisplay[selectedWo.status]?.className}`}>{statusDisplay[selectedWo.status]?.label}</span>
                </div>

                <div style={{ gridColumn: 'span 2' }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem', borderBottom: '1px solid rgba(255,255,255,0.05)', paddingBottom: '0.5rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <Package size={16} style={{ color: 'var(--cy-cyan)' }} />
                      <span style={{ fontSize: '0.9rem', fontWeight: 600, color: '#fff' }}>生產需料清單 (Material Requirements)</span>
                    </div>
                    <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                      <button
                        className="btn-cy"
                        style={{ padding: '4px 12px', fontSize: '0.75rem' }}
                        onClick={() => setShowMaterialIssue(true)}
                      >
                        工單領料
                      </button>
                      {detailedWo?.materials?.some(material => material.status === 'shortage') && (
                        <button
                          className="btn-cy"
                          style={{
                            padding: '4px 12px',
                            fontSize: '0.75rem',
                            background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                            border: 'none',
                            boxShadow: '0 0 15px rgba(245, 158, 11, 0.3)'
                          }}
                          disabled={supplyMutation.isPending}
                          onClick={() => supplyMutation.mutate(selectedWo.id)}
                        >
                          {supplyMutation.isPending ? '處理中...' : '一鍵自動轉單補貨'}
                        </button>
                      )}
                    </div>
                  </div>
                  
                  {(() => {
                    const activeBom = boms?.find(b => b.productSkuId === selectedWo.productSkuId && b.status === 'active');
                    const materialRows = detailedWo?.materials?.length
                      ? detailedWo.materials.map((material): MaterialRow => ({
                          id: material.id,
                          skuNumber: material.skuNumber,
                          skuName: material.skuName,
                          unit: material.unit,
                          inventoryUnit: material.inventoryUnit,
                          standardQty: material.bomStandardQty,
                          requiredQty: material.requiredQty,
                          availableQty: material.availableQty,
                          shortageQty: material.shortageQty,
                          status: material.status
                        }))
                      : (activeBom?.items ?? []).map((item: BomComponent): MaterialRow => {
                          const sku = skuData?.data.find(candidate => candidate.id === item.childSkuId);
                          const requiredQty = item.quantity * (1 + (item.scrapRate || 0) / 100) * selectedWo.plannedQty;
                          const availableQty = sku?.inventoryQty || 0;
                          return {
                            id: item.id ?? item.childSkuId,
                            skuNumber: item.childSkuNumber,
                            skuName: item.childSkuName,
                            unit: item.unit,
                            inventoryUnit: sku?.unit,
                            standardQty: item.quantity,
                            requiredQty,
                            availableQty,
                            shortageQty: Math.max(0, requiredQty - availableQty),
                            status: Math.max(0, requiredQty - availableQty) > 0 ? 'shortage' : 'pending'
                          };
                        });
                    if (materialRows.length === 0) return <div style={{ fontSize: '0.85rem', color: '#64748b', padding: '1rem', textAlign: 'center', background: 'rgba(255,255,255,0.02)', borderRadius: '0.5rem' }}>此工單沒有可顯示的 BOM 用料明細。</div>;

                    return (
                      <table style={{ width: '100%', fontSize: '0.85rem', borderCollapse: 'collapse' }}>
                        <thead>
                          <tr style={{ color: '#64748b', textAlign: 'left', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                            <th style={{ padding: '0.5rem' }}>料號/品名</th>
                            <th style={{ padding: '0.5rem' }}>單位用量</th>
                            <th style={{ padding: '0.5rem' }}>需求總量</th>
                            <th style={{ padding: '0.5rem' }}>可用庫存</th>
                            <th style={{ padding: '0.5rem' }}>狀態</th>
                          </tr>
                        </thead>
                        <tbody>
                          {materialRows.map(material => {
                            const isShortage = material.shortageQty > 0 && material.status !== 'ordered';

                            return (
                              <tr key={material.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                                <td style={{ padding: '0.75rem 0.5rem' }}>
                                  <div style={{ color: '#fff' }}>{material.skuNumber}</div>
                                  <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{material.skuName}</div>
                                </td>
                                <td style={{ padding: '0.75rem 0.5rem', color: '#94a3b8' }}>{material.standardQty} {getUnitName(material.unit)}</td>
                                <td style={{ padding: '0.75rem 0.5rem', color: '#fff', fontWeight: 600 }}>{material.requiredQty} {getUnitName(material.unit)}</td>
                                <td style={{ padding: '0.75rem 0.5rem', color: isShortage ? '#ef4444' : '#10b981' }}>
                                  {material.availableQty} {getUnitName(material.inventoryUnit || material.unit)}
                                </td>
                                <td style={{ padding: '0.75rem 0.5rem' }}>
                                  {material.status === 'ordered' ? (
                                    <span style={{ color: '#fbbf24' }}>已轉單</span>
                                  ) : isShortage ? (
                                    <span style={{ display: 'flex', alignItems: 'center', gap: 4, color: '#ef4444' }}>
                                      <AlertCircle size={12} /> 缺料
                                    </span>
                                  ) : (
                                    <span style={{ color: '#10b981' }}>充足</span>
                                  )}
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    );
                  })()}
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>生產進度 ({selectedWo.actualQty} / {selectedWo.plannedQty})</label>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                    <div className="cy-progress-track" style={{ flex: 1, height: '8px' }}>
                      <div className="cy-progress-fill" style={{ width: `${getProgress(selectedWo)}%` }} />
                    </div>
                    <span style={{ color: '#fff', fontSize: '0.85rem' }}>{getProgress(selectedWo)}%</span>
                  </div>
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>優先級與交期</label>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                     <div style={{ color: priorityDisplay[selectedWo.priority]?.color, fontWeight: 700, fontSize: '0.85rem' }}>{priorityDisplay[selectedWo.priority]?.label}</div>
                     <div style={{ color: '#94a3b8', fontSize: '0.85rem' }}>交期: {selectedWo.plannedEndDate ? new Date(selectedWo.plannedEndDate).toLocaleDateString() : '未定'}</div>
                  </div>
                </div>

                <div style={{ gridColumn: 'span 2' }}>
                  <div 
                    style={{ 
                      display: 'flex', 
                      alignItems: 'center', 
                      gap: '0.5rem', 
                      marginBottom: '1rem', 
                      borderBottom: '1px solid rgba(255,255,255,0.05)', 
                      paddingBottom: '0.5rem',
                      animation: reportingMode ? 'cy-pulse 2s infinite' : 'none'
                    }}
                  >
                    <Activity size={16} style={{ color: reportingMode ? '#a855f7' : 'var(--cy-cyan)' }} />
                    <span style={{ fontSize: '0.9rem', fontWeight: 600, color: '#fff' }}>
                      工序進度追蹤 {reportingMode && <span style={{ color: '#a855f7', fontSize: '0.7rem', marginLeft: 8 }}>(請點擊下方工序按鈕進行報工)</span>}
                    </span>
                  </div>
                  
                  {isWorkOrderLoading ? (
                    <div style={{ fontSize: '0.85rem', color: '#64748b', padding: '1rem', textAlign: 'center' }}>載入工序資料中...</div>
                  ) : isWorkOrderError ? (
                    <div style={{ fontSize: '0.85rem', color: '#ef4444', padding: '1rem', textAlign: 'center' }}>
                      工序資料載入失敗，請確認連線後重試。
                      <button
                        className="btn-cy"
                        style={{ display: 'block', margin: '0.75rem auto 0' }}
                        onClick={() => refetchWorkOrder()}
                      >
                        重新載入
                      </button>
                    </div>
                  ) : detailedWo?.id === selectedWo.id && detailedWo.operations?.length > 0 ? (
                    <table style={{ width: '100%', fontSize: '0.85rem', borderCollapse: 'collapse' }}>
                      <thead>
                        <tr style={{ color: '#64748b', textAlign: 'left', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                          <th style={{ padding: '0.5rem' }}>序號</th>
                          <th style={{ padding: '0.5rem' }}>工序名稱</th>
                          <th style={{ padding: '0.5rem' }}>工作中心</th>
                          <th style={{ padding: '0.5rem' }}>本工單計畫/完成</th>
                          <th style={{ padding: '0.5rem' }}>狀態</th>
                          <th style={{ padding: '0.5rem', textAlign: 'right' }}>操作</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detailedWo.operations.map(op => (
                          <tr key={op.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                            <td style={{ padding: '0.75rem 0.5rem', color: 'var(--cy-cyan)', fontWeight: 'bold' }}>{op.operationSeq}</td>
                            <td style={{ padding: '0.75rem 0.5rem', color: '#fff' }}>{op.operationName}</td>
                            <td style={{ padding: '0.75rem 0.5rem', color: '#94a3b8' }}>{op.workCenterName}</td>
                            <td style={{ padding: '0.75rem 0.5rem', color: '#fff' }}>
                              <div>{op.plannedQty.toLocaleString()} / {op.completedQty.toLocaleString()} {productionUnit}</div>
                            </td>
                            <td style={{ padding: '0.75rem 0.5rem' }}>
                              <span className={`status-badge status-${op.status === 'completed' ? 'active' : op.status === 'in_progress' ? 'warning' : 'neutral'}`}>
                                {op.status === 'completed' ? '已完成' : op.status === 'in_progress' ? '進行中' : '待生產'}
                              </span>
                            </td>
                            <td style={{ padding: '0.75rem 0.5rem', textAlign: 'right' }}>
                              {selectedWo.status === 'in_progress' && op.status !== 'completed' && (
                                <button className="btn-cy" style={{ padding: '2px 8px', fontSize: '0.7rem' }} onClick={() => openReportForOperation(op)}>
                                  報工
                                </button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  ) : detailedWo?.id === selectedWo.id && Array.isArray(detailedWo.operations) ? (
                    <div style={{ fontSize: '0.85rem', color: '#64748b', padding: '2rem', textAlign: 'center', background: 'rgba(255,255,255,0.02)', borderRadius: '0.5rem' }}>
                      <p>此工單尚無工序資料（可能是舊有資料）。</p>
                      <button 
                        className="btn-cy" 
                        style={{ marginTop: '1rem' }}
                        disabled={explodeMutation.isPending}
                        onClick={() => explodeMutation.mutate(selectedWo.id, { 
                          onError: handleMutationError,
                          onSuccess: () => alert('工法展開成功！')
                        })}
                      >
                        {explodeMutation.isPending ? '生成中...' : '自動生成工序資料 (修復歷史工單)'}
                      </button>
                    </div>
                  ) : !detailedWo || detailedWo.id !== selectedWo.id || !Array.isArray(detailedWo.operations) ? (
                    <div style={{ fontSize: '0.85rem', color: '#ef4444', padding: '1rem', textAlign: 'center' }}>
                      尚未取得完整工序資料。
                      <button
                        className="btn-cy"
                        style={{ display: 'block', margin: '0.75rem auto 0' }}
                        onClick={() => refetchWorkOrder()}
                      >
                        重新載入
                      </button>
                    </div>
                  ) : (
                    <div style={{ fontSize: '0.85rem', color: '#64748b', padding: '1rem', textAlign: 'center' }}>載入工序資料中...</div>
                  )}
                </div>
              </div>

              <div style={{ padding: '1rem', background: 'rgba(255,255,255,0.03)', borderRadius: '0.75rem', border: '1px solid rgba(255,255,255,0.05)', marginBottom: '2rem' }}>
                <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>工單備註</label>
                <p style={{ fontSize: '0.85rem', color: '#94a3b8', margin: 0 }}>{selectedWo.notes || '本工單尚無特殊備註。'}</p>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '1rem' }}>
                <button className="btn-cy" style={{ background: 'rgba(255,255,255,0.05)' }} onClick={() => setShowDetails(false)}>關閉視窗</button>
                <button className="btn-cy" onClick={() => onNavigate && onNavigate('dashboard')}>前往生產看板</button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>

      {/* Cost Report Modal */}
      <AnimatePresence>
        {showCostReport && selectedWo && (
          <div 
            style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.85)', zIndex: 1050, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px' }} 
            onClick={() => setShowCostReport(false)}
          >
            <motion.div 
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              onClick={e => e.stopPropagation()}
              className="cy-panel"
              style={{ width: '900px', maxWidth: '95vw', maxHeight: '90vh', overflowY: 'auto', padding: '2rem', border: '1px solid #fbbf24', position: 'relative' }}
            >
              <WorkOrderCostReport 
                workOrderId={selectedWo.id} 
                onClose={() => setShowCostReport(false)} 
              />
            </motion.div>
          </div>
        )}
      </AnimatePresence>

      <FormModal
        open={showForm}
        title={formMode === 'edit' ? `修改計畫: ${selectedWo?.woNumber}` : "工單建立中心"}
        fields={formFields}
        initialValues={selectedWo ? { ...selectedWo } : {}}
        onClose={() => { setShowForm(false); setSelectedWo(null); }}
        onSubmit={handleSubmitForm}
        width={500}
      />
      {showMaterialIssue && selectedWo && (
        <MaterialIssueWorkflow
          workOrderId={selectedWo.id}
          workOrderNumber={selectedWo.woNumber}
          plannedQty={selectedWo.plannedQty}
          productionUomCode={selectedWo.productionUomCode}
          onClose={() => setShowMaterialIssue(false)}
        />
      )}

      <AnimatePresence>
        {showReportReview && (
          <div
            style={{ position: 'fixed', inset: 0, zIndex: 1090, background: 'rgba(2, 6, 23, 0.88)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16 }}
            onClick={() => setShowReportReview(false)}
          >
            <motion.section
              initial={{ opacity: 0, y: 12, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 12, scale: 0.98 }}
              onClick={event => event.stopPropagation()}
              style={{ width: 900, maxWidth: '96vw', maxHeight: '90vh', overflowY: 'auto', padding: 24, borderRadius: 16, border: '1px solid rgba(56, 189, 248, 0.4)', background: '#0f172a', color: '#e2e8f0' }}
            >
              <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 18 }}>
                <div>
                  <h2 style={{ margin: 0, color: '#f8fafc' }}>我的工序報工</h2>
                  <small style={{ color: '#94a3b8' }}>送出後鎖定；退回時可修正後重新送出，所有操作均保留歷程。</small>
                </div>
                <button type="button" className="btn-cy" onClick={() => setShowReportReview(false)}>關閉</button>
              </header>
              {myReportsQuery.isLoading && <p>載入報工紀錄中…</p>}
              {myReportsQuery.isError && <p role="alert" style={{ color: '#f87171' }}>報工紀錄載入失敗，請重新整理後再試。</p>}
              <div style={{ display: 'grid', gap: 10 }}>
                {(myReportsQuery.data ?? []).map(report => (
                  <article key={report.id} style={{ padding: 14, border: '1px solid rgba(148,163,184,.22)', borderRadius: 10, background: 'rgba(255,255,255,.03)' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                      <strong>{report.reportNumber} · {report.workOrderNumber} / 工序 {report.operationSeq}</strong>
                      <span style={{ color: report.status === 'returned' ? '#fbbf24' : report.status === 'approved' ? '#4ade80' : '#38bdf8' }}>
                        {report.status === 'returned' ? '已退回' : report.status === 'approved' ? '已核准' : '待審核'}
                      </span>
                    </div>
                    <div style={{ marginTop: 6, color: '#cbd5e1', fontSize: 13 }}>
                      {report.reportType === 'team' ? '班組報工' : '個人報工'} · {report.shiftClass} · 良品 {report.completedQty} / 不良 {report.defectQty} / 報廢 {report.scrapQty} · 人工 {report.manHours} 小時
                    </div>
                    {report.reviewComments && <p style={{ margin: '8px 0 0', color: '#fbbf24' }}>審核意見：{report.reviewComments}</p>}
                    {report.status === 'returned' && (
                      <button
                        type="button"
                        className="btn-cy"
                        style={{ marginTop: 10 }}
                        onClick={() => {
                          const workOrder = workOrders.find(item => item.woNumber === report.workOrderNumber);
                          if (!workOrder) {
                            alert('無法載入此報工的工單，請確認工單仍可查詢。');
                            return;
                          }
                          setReportToCorrect(report);
                          setSelectedWo(workOrder);
                        }}
                      >
                        修正並重新送出
                      </button>
                    )}
                  </article>
                ))}
                {!myReportsQuery.isLoading && !myReportsQuery.isError && (myReportsQuery.data?.length ?? 0) === 0 && (
                  <p style={{ color: '#94a3b8' }}>尚無本人建立的報工紀錄。</p>
                )}
              </div>
              {canApproveReports && (
                <section style={{ marginTop: 24 }}>
                  <h3 style={{ color: '#7dd3fc' }}>待審核報工</h3>
                  {reviewQueueQuery.isLoading && <p>載入待審清單中…</p>}
                  {reviewQueueQuery.isError && <p role="alert" style={{ color: '#f87171' }}>待審清單載入失敗，請重新整理後再試。</p>}
                  <div style={{ display: 'grid', gap: 10 }}>
                    {(reviewQueueQuery.data ?? []).map(report => (
                      <article key={report.id} style={{ padding: 14, border: '1px solid rgba(56,189,248,.25)', borderRadius: 10 }}>
                        <strong>{report.reportNumber} · {report.workOrderNumber} / 工序 {report.operationSeq}</strong>
                        <div style={{ marginTop: 6, color: '#cbd5e1', fontSize: 13 }}>
                          {report.employeeName} · {report.reportType === 'team' ? '班組' : '個人'} · {report.shiftClass} · 良品 {report.completedQty} / 不良 {report.defectQty} / 報廢 {report.scrapQty} · 人工 {report.manHours} 小時
                        </div>
                        {report.proxyReason && <div style={{ marginTop: 4, color: '#fbbf24' }}>主管代填原因：{report.proxyReason}</div>}
                        <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
                          <button type="button" className="btn-cy" onClick={() => handleReviewReport(report, true)}>核准</button>
                          <button type="button" className="btn-cy" onClick={() => handleReviewReport(report, false)}>退回並要求修正</button>
                        </div>
                      </article>
                    ))}
                    {!reviewQueueQuery.isLoading && !reviewQueueQuery.isError && (reviewQueueQuery.data?.length ?? 0) === 0 && (
                      <p style={{ color: '#94a3b8' }}>目前沒有待審核的報工。</p>
                    )}
                  </div>
                </section>
              )}
            </motion.section>
          </div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {showReportForm && selectedWo && selectedOp && (
          <div
            style={{ position: 'fixed', inset: 0, zIndex: 1100, background: 'rgba(2, 6, 23, 0.88)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16 }}
            onClick={() => !createWipMutation.isPending && setShowReportForm(false)}
          >
            <motion.section
              initial={{ opacity: 0, y: 12, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 12, scale: 0.98 }}
              onClick={event => event.stopPropagation()}
              style={{ width: 1050, maxWidth: '96vw', maxHeight: '94vh', overflowY: 'auto', padding: 24, borderRadius: 16, border: '1px solid rgba(56, 189, 248, 0.4)', background: '#0f172a', color: '#e2e8f0', boxShadow: '0 24px 80px rgba(0,0,0,.6)' }}
            >
              <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 16, marginBottom: 20 }}>
                <div>
                  <div style={{ color: '#38bdf8', fontSize: 12, letterSpacing: 1.5, fontWeight: 700 }}>SHOP FLOOR REPORT</div>
                  <h2 style={{ margin: '6px 0', color: '#f8fafc' }}>工序報工</h2>
                  <div style={{ color: '#94a3b8', fontSize: 14 }}>報工單號將於儲存後自動產生</div>
                </div>
                <button type="button" aria-label="關閉報工表單" className="btn-cy" disabled={createWipMutation.isPending} onClick={() => setShowReportForm(false)}><X size={20} /></button>
              </header>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 12, padding: 16, marginBottom: 20, borderRadius: 12, background: 'rgba(255,255,255,.04)', border: '1px solid rgba(255,255,255,.08)' }}>
                <div><div style={{ color: '#94a3b8', fontSize: 12 }}>關聯工單</div><strong>{selectedWo.woNumber}</strong></div>
                <div><div style={{ color: '#94a3b8', fontSize: 12 }}>產品</div><strong>{selectedWo.productSkuNumber} {selectedWo.productSkuName}</strong></div>
                <div><div style={{ color: '#94a3b8', fontSize: 12 }}>工序 / 工作中心</div><strong>{selectedOp.operationSeq}. {selectedOp.operationName} / {selectedOp.workCenterName || '未指定'}</strong></div>
                <div><div style={{ color: '#94a3b8', fontSize: 12 }}>工單物料用量 / 生產數量 / 累計良品</div><strong>{materialUsageSummary} / {detailedWo?.productionQty ?? selectedWo.plannedQty} {productionUnit} / {selectedOp.completedQty} {productionUnit}</strong></div>
              </div>

              <section style={{ marginBottom: 20 }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, marginBottom: 12 }}>
                  <h3 style={{ fontSize: 15, color: '#7dd3fc', margin: 0 }}>報工人員與時段</h3>
                  <label style={{ ...reportLabelStyle, width: 170, margin: 0 }}>報工日期
                    <input
                      type="date"
                      value={reportDraft.workDate}
                      onChange={event => {
                        const workDate = event.target.value;
                        setReportDraft(current => ({
                          ...current,
                          workDate,
                          startTime: combineLocalDateTime(workDate, toLocalTimeInput(current.startTime)),
                          endTime: combineLocalDateTime(workDate, toLocalTimeInput(current.endTime)),
                          equipmentMachineHours: current.equipmentMachineHours.map(item => ({
                            ...item,
                            startTime: combineLocalDateTime(workDate, toLocalTimeInput(item.startTime)),
                            endTime: combineLocalDateTime(workDate, toLocalTimeInput(item.endTime)),
                          })),
                        }));
                      }}
                      style={reportInputStyle}
                    />
                  </label>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))', gap: 14 }}>
                  <label style={reportLabelStyle}>報工類型 *
                    <select value={reportDraft.reportType} onChange={event => setReportDraft(current => ({
                      ...current,
                      reportType: event.target.value as 'individual' | 'team',
                      teamMemberEmployeeIds: [],
                      teamMemberHours: currentEmployeeId ? { [currentEmployeeId]: '0' } : {},
                    }))} style={reportInputStyle}>
                      <option value="individual">個人報工</option>
                      <option value="team">班組共同報工（產量只登錄一次）</option>
                    </select>
                  </label>
                  <div style={reportLabelStyle}>登入作業員
                    <div style={{ ...reportInputStyle, display: 'flex', alignItems: 'center' }}>
                      {employees.find(employee => employee.id === currentEmployeeId)?.chineseName
                        ?? currentUser?.employeeName
                        ?? currentUser?.username
                        ?? '尚未登入'}
                    </div>
                    {!currentEmployeeId && <small role="alert" style={{ color: '#fbbf24' }}>目前帳號未綁定員工，無法報工。</small>}
                  </div>
                  {canProxyReport && reportDraft.reportType === 'individual' && (
                    <label style={reportLabelStyle}>主管代填人員
                      <select value={reportDraft.employeeId || currentEmployeeId} onChange={event => setReportDraft(current => ({
                        ...current,
                        employeeId: event.target.value,
                      }))} style={reportInputStyle}>
                        <option value={currentEmployeeId}>本人（登入者）</option>
                        {employees.filter(employee => employee.id !== currentEmployeeId).map(employee =>
                          <option key={employee.id} value={employee.id}>{employee.employeeNo} - {employee.chineseName}</option>
                        )}
                      </select>
                    </label>
                  )}
                  {reportDraft.employeeId && reportDraft.employeeId !== currentEmployeeId && (
                    <label style={reportLabelStyle}>主管代填原因 *
                      <input required maxLength={1000} value={reportDraft.proxyReason} onChange={event => setReportDraft(current => ({
                        ...current,
                        proxyReason: event.target.value,
                      }))} style={reportInputStyle} />
                    </label>
                  )}
                  <label style={reportLabelStyle}>班別 *
                    <select value={reportDraft.shiftClass} onChange={event => setReportDraft({ ...reportDraft, shiftClass: event.target.value })} style={reportInputStyle}>
                      <option value="day">白班</option><option value="night">夜班</option><option value="other">其他</option>
                    </select>
                  </label>
                  {reportDraft.reportType === 'team' && (
                    <div style={{ ...reportLabelStyle, gridColumn: '1 / -1' }}>班組成員（負責人為登入者）*
                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 8, padding: 12, borderRadius: 8, border: '1px solid rgba(148, 163, 184, 0.3)' }}>
                        {employees.filter(employee => employee.id !== currentEmployeeId).map(employee => (
                          <label key={employee.id} style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#e2e8f0', fontWeight: 400 }}>
                            <input
                              type="checkbox"
                              checked={reportDraft.teamMemberEmployeeIds.includes(employee.id)}
                              onChange={event => {
                                const isChecked = event.target.checked;
                                setReportDraft(current => {
                                const teamMemberEmployeeIds = isChecked
                                  ? [...current.teamMemberEmployeeIds, employee.id]
                                  : current.teamMemberEmployeeIds.filter(id => id !== employee.id);
                                const teamMemberHours = { ...current.teamMemberHours };
                                if (isChecked) teamMemberHours[employee.id] = '0';
                                else delete teamMemberHours[employee.id];
                                return { ...current, teamMemberEmployeeIds, teamMemberHours };
                                });
                              }}
                            />
                            {employee.employeeNo} - {employee.chineseName}
                          </label>
                        ))}
                        {isEmployeesLoading && <small style={{ color: '#94a3b8' }}>載入員工中…</small>}
                        {isEmployeesError && <small role="alert" style={{ color: '#f87171' }}>員工資料載入失敗，請重新整理後再試。</small>}
                        {!isEmployeesLoading && !isEmployeesError && employees.length <= 1 && <small role="alert" style={{ color: '#fbbf24' }}>目前沒有其他在職員工可加入班組。</small>}
                      </div>
                      <small style={{ color: '#94a3b8' }}>班組總產量只建立一筆報工，不會按人數重複累加。</small>
                    </div>
                  )}
                  <div style={reportLabelStyle}>設備（選填，可多選）
                    <div
                      style={{ position: 'relative' }}
                      onKeyDown={event => {
                        if (event.key === 'Escape') setShowEquipmentPicker(false);
                      }}
                    >
                      <button
                        type="button"
                        aria-haspopup="listbox"
                        aria-expanded={showEquipmentPicker}
                        onClick={() => setShowEquipmentPicker(open => !open)}
                        title={reportDraft.equipmentMachineHours
                          .map(row => equipment.find(item => item.id === row.equipmentId)?.equipmentCode)
                          .filter(Boolean)
                          .join(', ')}
                        style={{
                          ...reportInputStyle,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          gap: 8,
                          textAlign: 'left',
                          cursor: 'pointer',
                        }}
                      >
                        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {reportDraft.equipmentMachineHours.length > 0
                            ? `已選擇 ${reportDraft.equipmentMachineHours.length} 台設備`
                            : '點擊選擇設備'}
                        </span>
                        <ChevronDown
                          size={16}
                          style={{
                            flexShrink: 0,
                            transform: showEquipmentPicker ? 'rotate(180deg)' : 'none',
                            transition: 'transform 0.2s',
                          }}
                        />
                      </button>
                      {showEquipmentPicker && (
                        <div
                          style={{
                            position: 'absolute',
                            top: 'calc(100% + 4px)',
                            left: 0,
                            right: 0,
                            zIndex: 20,
                            overflow: 'hidden',
                            border: '1px solid rgba(148, 163, 184, 0.3)',
                            borderRadius: 8,
                            background: '#0f172a',
                            boxShadow: '0 16px 32px rgba(0, 0, 0, 0.45)',
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 10px', borderBottom: '1px solid rgba(148, 163, 184, 0.2)' }}>
                            <Search size={15} style={{ flexShrink: 0, color: '#94a3b8' }} />
                            <input
                              autoFocus
                              aria-label="搜尋設備"
                              placeholder="搜尋設備編號或名稱..."
                              value={equipmentSearch}
                              onChange={event => setEquipmentSearch(event.target.value)}
                              style={{ width: '100%', border: 0, outline: 0, background: 'transparent', color: '#f8fafc', fontSize: 13 }}
                            />
                          </div>
                          <div role="listbox" aria-multiselectable="true" style={{ maxHeight: 180, overflowY: 'auto', padding: 4 }}>
                            {equipment
                              .filter(item => `${item.equipmentCode} ${item.equipmentName}`.toLocaleLowerCase().includes(equipmentSearch.trim().toLocaleLowerCase()))
                              .map(item => {
                                const isSelected = reportDraft.equipmentMachineHours.some(row => row.equipmentId === item.id);
                                return (
                                  <button
                                    key={item.id}
                                    type="button"
                                    role="option"
                                    aria-selected={isSelected}
                                    onClick={() => setReportDraft(current => ({
                                      ...current,
                                      equipmentMachineHours: isSelected
                                        ? current.equipmentMachineHours.filter(row => row.equipmentId !== item.id)
                                        : [...current.equipmentMachineHours, { equipmentId: item.id, machineHours: '0', startTime: '', endTime: '' }],
                                    }))}
                                    style={{
                                      display: 'flex',
                                      alignItems: 'center',
                                      gap: 9,
                                      width: '100%',
                                      padding: '8px',
                                      border: 0,
                                      borderRadius: 5,
                                      background: isSelected ? 'rgba(56, 189, 248, 0.12)' : 'transparent',
                                      color: '#e2e8f0',
                                      textAlign: 'left',
                                      fontSize: 13,
                                      cursor: 'pointer',
                                    }}
                                  >
                                    <span style={{
                                      display: 'grid',
                                      placeItems: 'center',
                                      width: 16,
                                      height: 16,
                                      flexShrink: 0,
                                      border: `1px solid ${isSelected ? '#38bdf8' : 'rgba(148, 163, 184, 0.55)'}`,
                                      borderRadius: 4,
                                      background: isSelected ? '#0284c7' : 'transparent',
                                    }}>
                                      {isSelected && <Check size={12} color="#fff" />}
                                    </span>
                                    <span>{item.equipmentCode} - {item.equipmentName}</span>
                                  </button>
                                );
                              })}
                            {!isEquipmentLoading && !isEquipmentError && equipment.filter(item =>
                              `${item.equipmentCode} ${item.equipmentName}`.toLocaleLowerCase().includes(equipmentSearch.trim().toLocaleLowerCase())
                            ).length === 0 && (
                              <small style={{ display: 'block', padding: 10, color: '#94a3b8', textAlign: 'center' }}>
                                {equipment.length === 0 ? '目前沒有可用設備，可先不指定。' : '查無符合設備。'}
                              </small>
                            )}
                          </div>
                          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8, padding: 8, borderTop: '1px solid rgba(148, 163, 184, 0.2)' }}>
                            <button
                              type="button"
                              disabled={reportDraft.equipmentMachineHours.length === 0}
                              onClick={() => setReportDraft(current => ({ ...current, equipmentMachineHours: [] }))}
                              style={{ border: 0, background: 'transparent', color: '#94a3b8', fontSize: 12, cursor: 'pointer' }}
                            >
                              清除選擇
                            </button>
                            <button
                              type="button"
                              onClick={() => setShowEquipmentPicker(false)}
                              style={{ padding: '5px 12px', border: 0, borderRadius: 5, background: '#0284c7', color: '#fff', fontSize: 12, cursor: 'pointer' }}
                            >
                              完成
                            </button>
                          </div>
                        </div>
                      )}
                    </div>
                    {isEquipmentLoading && <small style={{ color: '#94a3b8' }}>載入設備中…</small>}
                    {isEquipmentError && <small role="alert" style={{ color: '#f87171' }}>設備資料載入失敗，請重新整理後再試。</small>}
                  </div>
                  <label style={reportLabelStyle}>開始時間
                    <div style={{ display: 'flex', gap: 8 }}>
                      <input type="time" step="1" value={toLocalTimeInput(reportDraft.startTime)} onChange={event => {
                        const startTime = combineLocalDateTime(reportDraft.workDate, event.target.value);
                        setReportDraft(current => ({
                          ...current,
                          startTime,
                          manHours: String(calculateHours(startTime, current.endTime)),
                        }));
                      }} style={{ ...reportInputStyle, flex: 1 }} />
                      <button type="button" className="btn-cy" onClick={() => setReportDraft(current => ({
                        ...current,
                        startTime: toLocalDateTimeInput(new Date()),
                        workDate: toLocalDateInput(new Date()),
                        endTime: '',
                        manHours: '0',
                      }))}>開始</button>
                    </div>
                  </label>
                  <label style={reportLabelStyle}>結束時間
                    <div style={{ display: 'flex', gap: 8 }}>
                      <input type="time" step="1" value={toLocalTimeInput(reportDraft.endTime)} onChange={event => {
                        const endTime = combineLocalDateTime(reportDraft.workDate, event.target.value);
                        setReportDraft(current => ({
                          ...current,
                          endTime,
                          manHours: String(calculateHours(current.startTime, endTime)),
                        }));
                      }} style={{ ...reportInputStyle, flex: 1 }} />
                      <button type="button" className="btn-cy" disabled={!reportDraft.startTime} onClick={() => {
                        const endTime = toLocalDateTimeInput(new Date());
                        setReportDraft(current => ({
                          ...current,
                          endTime,
                          manHours: String(calculateHours(current.startTime, endTime)),
                        }));
                      }}>停止</button>
                    </div>
                  </label>
                  <label style={reportLabelStyle}>準備時間 (小時)
                    <input type="number" min="0" step="0.1" value={reportDraft.setupHours} onChange={event => setReportDraft({ ...reportDraft, setupHours: event.target.value })} style={reportInputStyle} />
                  </label>
                  <label style={reportLabelStyle}>加工時間 (小時)
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={reportDraft.runHours}
                      readOnly={reportDraft.equipmentMachineHours.length > 0}
                      onChange={event => setReportDraft({ ...reportDraft, runHours: event.target.value })}
                      style={{
                        ...reportInputStyle,
                        ...(reportDraft.equipmentMachineHours.length > 0 ? { background: 'rgba(148, 163, 184, 0.08)', cursor: 'not-allowed' } : {}),
                      }}
                    />
                    {reportDraft.equipmentMachineHours.length > 0 && (
                      <small style={{ color: '#94a3b8' }}>由所選設備的機台工時自動加總</small>
                    )}
                  </label>
                  {reportDraft.reportType === 'individual' ? (
                    <label style={reportLabelStyle}>本人實際人工工時 (小時)
                      <input type="number" min="0" step="0.1" value={reportDraft.manHours} onChange={event => setReportDraft({ ...reportDraft, manHours: event.target.value })} style={reportInputStyle} />
                    </label>
                  ) : (
                    <div style={{ ...reportLabelStyle, gridColumn: '1 / -1' }}>各班組成員實際人工工時 (小時) *
                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))', gap: 10 }}>
                        {[currentEmployeeId, ...reportDraft.teamMemberEmployeeIds].filter(Boolean).map(employeeId => {
                          const member = employees.find(employee => employee.id === employeeId);
                          return (
                            <label key={employeeId} style={reportLabelStyle}>
                              {employeeId === currentEmployeeId ? '負責人' : '班組成員'} · {member?.employeeNo ?? ''} {member?.chineseName ?? ''}
                              <input
                                type="number"
                                min="0"
                                step="0.1"
                                value={reportDraft.teamMemberHours[employeeId] ?? '0'}
                                onChange={event => setReportDraft(current => ({
                                  ...current,
                                  teamMemberHours: { ...current.teamMemberHours, [employeeId]: event.target.value },
                                }))}
                                style={reportInputStyle}
                              />
                            </label>
                          );
                        })}
                      </div>
                      <small style={{ color: '#94a3b8' }}>
                        班組人工合計：{[currentEmployeeId, ...reportDraft.teamMemberEmployeeIds]
                          .reduce((total, employeeId) => total + (Number(reportDraft.teamMemberHours[employeeId]) || 0), 0)
                          .toFixed(2)} 小時，成本會按成員各自工時分別記錄。
                      </small>
                    </div>
                  )}
                  <div style={{ gridColumn: '1 / -1' }}>
                    <div style={{ marginBottom: 10, color: '#7dd3fc', fontSize: 14, fontWeight: 700 }}>設備獨立工時記錄</div>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 12 }}>
                      {reportDraft.equipmentMachineHours.map(item => {
                        const selectedEquipment = equipment.find(equipmentItem => equipmentItem.id === item.equipmentId);
                        return (
                          <div key={item.equipmentId} style={{
                            display: 'grid',
                            gridTemplateColumns: 'minmax(0, 1fr) auto',
                            gap: 10,
                            padding: 14,
                            borderRadius: 10,
                            border: '1px solid rgba(56, 189, 248, 0.28)',
                            background: 'rgba(15, 23, 42, 0.72)',
                          }}>
                            <div style={{ gridColumn: '1 / -1', display: 'flex', justifyContent: 'space-between', gap: 8 }}>
                              <strong>{selectedEquipment ? `${selectedEquipment.equipmentCode} - ${selectedEquipment.equipmentName}` : '未命名設備'}</strong>
                              <span style={{ color: item.startTime && !item.endTime ? '#4ade80' : '#94a3b8', fontSize: 12 }}>
                                {item.startTime && !item.endTime ? '計時中' : item.endTime ? '已停止' : '未開始'}
                              </span>
                            </div>
                            <label style={{ ...reportLabelStyle, gridColumn: '1 / -1', margin: 0 }}>機台工時 (小時)
                              <input
                                type="number"
                                min="0"
                                max="999999.99"
                                step="0.01"
                                value={item.machineHours}
                                onChange={event => setReportDraft(current => ({
                                  ...current,
                                  equipmentMachineHours: current.equipmentMachineHours.map(row =>
                                    row.equipmentId === item.equipmentId
                                      ? { ...row, machineHours: event.target.value }
                                      : row
                                  ),
                                }))}
                                style={reportInputStyle}
                              />
                            </label>
                            <label style={{ ...reportLabelStyle, gridColumn: '1 / 2', margin: 0 }}>開始時間
                              <input
                                type="time"
                                step="1"
                                value={toLocalTimeInput(item.startTime)}
                                onChange={event => setReportDraft(current => ({
                                  ...current,
                                  equipmentMachineHours: current.equipmentMachineHours.map(row =>
                                    row.equipmentId === item.equipmentId
                                      ? {
                                          ...row,
                                          startTime: combineLocalDateTime(current.workDate, event.target.value),
                                          machineHours: String(calculateHours(combineLocalDateTime(current.workDate, event.target.value), row.endTime)),
                                        }
                                      : row
                                  ),
                                }))}
                                style={reportInputStyle}
                              />
                            </label>
                            <button
                              type="button"
                              className="btn-cy"
                              style={{ alignSelf: 'end', height: 40 }}
                              onClick={() => setReportDraft(current => ({
                                ...current,
                                equipmentMachineHours: current.equipmentMachineHours.map(row =>
                                  row.equipmentId === item.equipmentId
                                    ? { ...row, startTime: toLocalDateTimeInput(new Date()), endTime: '', machineHours: '0' }
                                    : row
                                ),
                              }))}
                            >
                              開始
                            </button>
                            <label style={{ ...reportLabelStyle, gridColumn: '1 / 2', margin: 0 }}>結束時間
                              <input
                                type="time"
                                step="1"
                                value={toLocalTimeInput(item.endTime)}
                                onChange={event => setReportDraft(current => ({
                                  ...current,
                                  equipmentMachineHours: current.equipmentMachineHours.map(row =>
                                    row.equipmentId === item.equipmentId
                                      ? {
                                          ...row,
                                          endTime: combineLocalDateTime(current.workDate, event.target.value),
                                          machineHours: String(calculateHours(row.startTime, combineLocalDateTime(current.workDate, event.target.value))),
                                        }
                                      : row
                                  ),
                                }))}
                                style={reportInputStyle}
                              />
                            </label>
                            <button
                              type="button"
                              className="btn-cy"
                              disabled={!item.startTime}
                              style={{ alignSelf: 'end', height: 40 }}
                              onClick={() => {
                                const endTime = toLocalDateTimeInput(new Date());
                                setReportDraft(current => ({
                                  ...current,
                                  equipmentMachineHours: current.equipmentMachineHours.map(row =>
                                    row.equipmentId === item.equipmentId
                                      ? { ...row, endTime, machineHours: String(calculateHours(row.startTime, endTime)) }
                                      : row
                                  ),
                                }));
                              }}
                            >
                              停止
                            </button>
                          </div>
                        );
                      })}
                    </div>
                    <small style={{ gridColumn: '1 / -1', color: '#94a3b8' }}>
                      機台總工時：{totalSelectedMachineHours.toFixed(2)} 小時
                    </small>
                    <div style={{ gridColumn: '1 / -1', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                      <small style={{ color: '#94a3b8' }}>設備工時為獨立紀錄，不會併入個人工時或報工審核。</small>
                      <button
                        type="button"
                        className="btn-cy"
                        disabled={isSavingEquipmentUsage || reportDraft.equipmentMachineHours.length === 0}
                        onClick={handleEquipmentUsageSubmit}
                      >
                        {isSavingEquipmentUsage ? '設備紀錄儲存中…' : '另存設備使用紀錄'}
                      </button>
                    </div>
                  </div>
                </div>
              </section>

              <section style={{ marginBottom: 20 }}>
                <h3 style={{ fontSize: 15, color: '#7dd3fc', margin: '0 0 12px' }}>產出與品質判定</h3>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: 14 }}>
                  {([
                    ['completedQty', '本次完工良品數 *'],
                    ['defectQty', '本次不良數'],
                    ['scrapQty', '本次報廢數'],
                  ] as const).map(([key, label]) => (
                    <label key={key} style={reportLabelStyle}>{label}
                      <input type="number" min="0" step="0.01" inputMode="decimal" value={reportDraft[key]} onChange={event => setReportDraft({ ...reportDraft, [key]: event.target.value })} style={{ ...reportInputStyle, fontSize: 20, minHeight: 52 }} />
                    </label>
                  ))}
                  <label style={reportLabelStyle}>不良原因代碼
                    <select value={reportDraft.defectReasonCode} onChange={event => setReportDraft({ ...reportDraft, defectReasonCode: event.target.value })} style={reportInputStyle}>
                      <option value="">無不良 / 請選擇</option>
                      <option value="E-01">E-01 外觀刮傷</option>
                      <option value="E-02">E-02 尺寸偏差</option>
                      <option value="E-99">E-99 其他</option>
                    </select>
                  </label>
                  <label style={reportLabelStyle}>物料批號
                    <input value={reportDraft.materialLotNumber} maxLength={50} onChange={event => setReportDraft({ ...reportDraft, materialLotNumber: event.target.value })} style={reportInputStyle} />
                  </label>
                  {Number(selectedOp?.operationSeq) === Math.max(...(detailedWo?.operations ?? []).map(operation => Number(operation.operationSeq))) && (
                    <label style={reportLabelStyle}>成品批號（最後工序必填）
                      <input value={reportDraft.finishedLotNumber} maxLength={50} onChange={event => setReportDraft({ ...reportDraft, finishedLotNumber: event.target.value })} style={reportInputStyle} />
                    </label>
                  )}
                </div>
                <div style={{ marginTop: 14, padding: 14, borderRadius: 10, background: 'rgba(56,189,248,.08)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', color: '#cbd5e1', fontSize: 13, marginBottom: 8 }}>
                    <span>核准後預估工序良品進度</span><strong>{Number(selectedOp.completedQty || 0) + (Number(reportDraft.completedQty) || 0)} / {detailedWo?.productionQty ?? selectedWo.plannedQty}</strong>
                  </div>
                  <div style={{ height: 8, borderRadius: 99, background: 'rgba(148,163,184,.2)', overflow: 'hidden' }}>
                    <div style={{ width: `${Math.min(100, (detailedWo?.productionQty ?? selectedWo.plannedQty) > 0 ? ((Number(selectedOp.completedQty || 0) + (Number(reportDraft.completedQty) || 0)) / (detailedWo?.productionQty ?? selectedWo.plannedQty)) * 100 : 0)}%`, height: '100%', background: '#38bdf8', transition: 'width .15s ease' }} />
                  </div>
                </div>
              </section>

              <label style={reportLabelStyle}>現場備註
                <textarea rows={3} maxLength={2000} value={reportDraft.remarks} onChange={event => setReportDraft({ ...reportDraft, remarks: event.target.value })} style={{ ...reportInputStyle, resize: 'vertical' }} />
              </label>

              <footer style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, marginTop: 22 }}>
                <button type="button" className="btn-cy" disabled={createWipMutation.isPending} onClick={() => setShowReportForm(false)}>取消</button>
                <button type="button" className="btn-cy" disabled={createWipMutation.isPending || !currentEmployeeId} onClick={handleReportSubmit} style={{ minWidth: 150, minHeight: 48 }}>
                  {createWipMutation.isPending ? <><Loader2 size={16} className="animate-spin" /> 送出中…</> : <><Send size={16} /> 送出待審</>}
                </button>
              </footer>
            </motion.section>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};
