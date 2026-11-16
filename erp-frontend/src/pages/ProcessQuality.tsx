import React, { useEffect, useState } from 'react';
import { ClipboardCheck, ClipboardList, FileCheck2, Plus, Save, Settings2, Trash2, X } from 'lucide-react';
import {
  inspectionTemplatesApi,
  operationsApi,
  processInspectionsApi,
  routingsApi,
} from '../api/api';
import type {
  InspectionTemplateDto,
  ProcessInspectionReportDto,
  WorkOrderInspectionOperationDto,
} from '../api/api';
import { fqcWorkflowApi } from '../api/mes';
import type { FqcWorkOrderOptionDto } from '../api/mes';
import { useRole } from '../contexts/useRole';
import { getProcessQualityViews, type ProcessQualityView } from '../utils/processQualityViews';
import { formatWorkOrderOption } from '../utils/workOrderDisplay';
import { FqcWorkflowPanel } from './FqcWorkflowPanel';

type TemplateItemDraft = {
  itemCode: string;
  itemName: string;
  inputType: string;
  specification: string;
  unit: string;
  lowerLimit: string;
  upperLimit: string;
  options: string;
  inspectionMethod: string;
  instrument: string;
  samplingPlan: string;
  isRequired: boolean;
  isCritical: boolean;
};

type InspectionItemPreset = {
  id: string;
  guidance: string;
  item: Omit<TemplateItemDraft, 'itemCode'>;
};

type TemplateDraft = {
  templateCode: string;
  templateName: string;
  operationId: string;
  productSkuIds: string[];
  isActive: boolean;
  notes: string;
};

type InspectionValue = { value: string; result: string; notes: string };

const resultLabels: Record<string, string> = {
  pass: '合格',
  fail: '不合格',
  na: '不適用',
  incomplete: '未完成',
  pending: '待檢',
};

const emptyItem = (itemCode = ''): TemplateItemDraft => ({
  itemCode,
  itemName: '',
  inputType: 'passFail',
  specification: '',
  unit: '',
  lowerLimit: '',
  upperLimit: '',
  options: '',
  inspectionMethod: '',
  instrument: '',
  samplingPlan: '',
  isRequired: true,
  isCritical: false,
});

const makeInspectionItemPreset = (
  id: string,
  itemName: string,
  inputType: string,
  specification: string,
  inspectionMethod: string,
  instrument: string,
  unit = '',
  guidance = '',
): InspectionItemPreset => ({
  id,
  guidance,
  item: {
    itemName,
    inputType,
    specification,
    unit,
    lowerLimit: '',
    upperLimit: '',
    options: '',
    inspectionMethod,
    instrument,
    samplingPlan: '依產品品質計畫設定首件、巡檢及出貨抽樣頻率',
    isRequired: true,
    isCritical: false,
  },
});

const getNextInspectionItemCode = (templateCode: string, items: TemplateItemDraft[]) => {
  const prefix = templateCode.trim() || 'QC';
  let sequence = 1;
  let itemCode = `${prefix}-${String(sequence).padStart(2, '0')}`;
  while (items.some(item => item.itemCode === itemCode)) {
    sequence += 1;
    itemCode = `${prefix}-${String(sequence).padStart(2, '0')}`;
  }
  return itemCode;
};

const inspectionItemPresetGroups: Array<{
  id: string;
  label: string;
  description: string;
  items: InspectionItemPreset[];
}> = [
  {
    id: 'appearance',
    label: '外觀檢驗',
    description: '外觀、表面瑕疵與色差',
    items: [
      makeInspectionItemPreset('sink', '縮水 / 凹陷', 'passFail', '依客戶核准簽樣及外觀面允收標準判定。', '目視檢查，對照客戶核准簽樣。', '標準光源 / 核准簽樣', '', '厚壁處及肋骨背面。'),
      makeInspectionItemPreset('weld', '結合線 / 熔接痕', 'passFail', '依外觀面簽樣標準；結構面確認無裂損且符合產品要求。', '目視檢查；必要時依品質計畫進行強度確認。', '核准簽樣'),
      makeInspectionItemPreset('bubble', '氣泡 / 銀紋', 'passFail', '依客戶外觀允收標準及核准簽樣判定。', '目視檢查，於指定光源下確認。', '標準光源 / 核准簽樣'),
      makeInspectionItemPreset('flow', '流痕 / 霧斑 / 光澤差異', 'passFail', '依客戶核准簽樣及外觀允收標準判定。', '目視檢查並比對色板或核准簽樣。', '標準光源 / 核准簽樣'),
      makeInspectionItemPreset('flash', '毛邊 / 披鋒', 'passFail', '允收高度依工程圖面或客戶規格設定；另確認不刮手、不影響裝配。', '目視及觸感確認；必要時量測毛邊高度。', '卡尺 / 核准簽樣', '', '如需數值管制，請改為數值量測並填入允收上限。'),
      makeInspectionItemPreset('surface', '刮傷 / 碰傷 / 髒污 / 黑點', 'passFail', '依產品外觀分級、客戶簽樣及黑點允收規格判定。', '目視檢查，記錄缺陷位置與數量。', '標準光源 / 放大鏡'),
      makeInspectionItemPreset('color', '色差', 'number', '比對客戶核准色板；允收 ΔE 標準依客戶或產品規格設定。', '使用色差儀量測並與標準色板比對。', '色差儀', 'ΔE', '請依客戶規格填寫允收上限，不預設通用值。'),
    ],
  },
  {
    id: 'dimension',
    label: '尺寸檢驗',
    description: '圖面尺寸、公差與變形',
    items: [
      makeInspectionItemPreset('critical-dimension', '關鍵 / 裝配尺寸', 'number', '依 2D 工程圖面與 GD&T 標註填寫尺寸及公差。', '依圖面指定量測位置與基準量測。', '卡尺 / 高度規 / 專用檢具', 'mm', '可逐項建立不同尺寸與各自公差；請設定至少一項允收界限。'),
      makeInspectionItemPreset('hole-dimension', '孔徑 / 外徑 / 厚度', 'number', '依工程圖面標註的尺寸與公差判定。', '依圖面指定量具量測。', '卡尺 / 塞規 / 專用檢具', 'mm', '請依圖面設定允收上下限。'),
      makeInspectionItemPreset('warpage', '翹曲 / 平面度', 'number', '依工程圖面或客戶允收規格設定平面度 / 變形量。', '置於基準平台量測指定位置。', '高度規 / 平台 / 專用檢具', 'mm', '請依產品圖面或品質計畫設定至少一項允收界限。'),
    ],
  },
  {
    id: 'function',
    label: '功能 / 物性',
    description: '裝配、耐候與二次加工',
    items: [
      makeInspectionItemPreset('snap-fit', '卡扣裝配 / 扣合牢固度', 'passFail', '依產品裝配要求及客戶允收標準判定。', '依規定方式組裝並確認扣合、鬆脫與破損情況。', '產品配件 / 拉力計'),
      makeInspectionItemPreset('boss-torque', '螺絲柱鎖付扭力', 'number', '扭力上下限依工程圖面或產品驗證規格設定。', '依規定螺絲及鎖付條件測試滑牙或拔出扭力。', '扭力計', 'N·m', '請依產品規格填寫允收上下限。'),
      makeInspectionItemPreset('heat-cold', '耐熱 / 耐寒 / 溫度循環', 'passFail', '測試溫度、時間及判定條件依客戶或產品驗證規格設定。', '依核准測試計畫執行，確認變形、龜裂及功能。', '溫度試驗箱'),
      makeInspectionItemPreset('drop', '落下衝擊', 'passFail', '落下高度、方向、次數及允收條件依產品驗證規格設定。', '依核准測試計畫執行並檢查破損及功能。', '落下試驗設備'),
      makeInspectionItemPreset('coating', '塗層附著力 / 百格測試', 'passFail', '適用於噴漆、電鍍或印刷件；判定等級依客戶規格設定。', '依核准方法執行百格及膠帶測試。', '百格刀 / 膠帶', '', '測試方法及允收等級請依客戶規範確認。'),
    ],
  },
];

const emptyTemplate = (): TemplateDraft => ({
  templateCode: '',
  templateName: '',
  operationId: '',
  productSkuIds: [],
  isActive: true,
  notes: '',
});

const getErrorMessage = (error: unknown) =>
  (error as { response?: { data?: { message?: string } }; message?: string }).response?.data?.message ??
  (error as { message?: string }).message ??
  '操作失敗，請稍後再試';

export function ProcessQuality({ onBack }: { onBack: () => void }) {
  const { hasPermission } = useRole();
  const canCreate = hasPermission('quality', 'create');
  const canEdit = hasPermission('quality', 'edit');
  const availableViews = getProcessQualityViews({
    qualityView: hasPermission('quality', 'view'),
    engineeringView: hasPermission('engineering', 'view'),
    inventoryView: hasPermission('inventory', 'view'),
  });
  const [view, setView] = useState<ProcessQualityView>('fqc');
  const activeView = availableViews.includes(view) ? view : (availableViews[0] ?? null);
  const [templates, setTemplates] = useState<InspectionTemplateDto[]>([]);
  const [operations, setOperations] = useState<Array<{ id: string; operationCode: string; operationName: string }>>([]);
  const [selectedSkus, setSelectedSkus] = useState<Array<{ id: string; skuNumber: string; skuName: string }>>([]);
  const [operationProducts, setOperationProducts] = useState<Array<{ id: string; skuNumber: string; skuName: string }>>([]);
  const [operationProductsLoading, setOperationProductsLoading] = useState(false);
  const [productSearch, setProductSearch] = useState('');
  const [productDropdownOpen, setProductDropdownOpen] = useState(false);
  const [productSearchError, setProductSearchError] = useState('');
  const [workOrders, setWorkOrders] = useState<FqcWorkOrderOptionDto[]>([]);
  const [editingTemplate, setEditingTemplate] = useState<InspectionTemplateDto | null>(null);
  const [templateDraft, setTemplateDraft] = useState<TemplateDraft>(emptyTemplate);
  const [itemDrafts, setItemDrafts] = useState<TemplateItemDraft[]>([]);
  const [activePresetCategory, setActivePresetCategory] = useState<string | null>('appearance');
  const [workOrderId, setWorkOrderId] = useState('');
  const [operationOptions, setOperationOptions] = useState<WorkOrderInspectionOperationDto[]>([]);
  const [operationSeq, setOperationSeq] = useState('');
  const [templateId, setTemplateId] = useState('');
  const [lotNumber, setLotNumber] = useState('');
  const [inspectorName, setInspectorName] = useState('');
  const [sampleQuantity, setSampleQuantity] = useState('1');
  const [inspectionNotes, setInspectionNotes] = useState('');
  const [values, setValues] = useState<Record<string, InspectionValue>>({});
  const [reports, setReports] = useState<ProcessInspectionReportDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [templateDataError, setTemplateDataError] = useState('');
  const [workOrdersLoading, setWorkOrdersLoading] = useState(false);
  const [workOrdersError, setWorkOrdersError] = useState('');

  useEffect(() => {
    if (activeView !== 'records')
      return;
    let cancelled = false;
    setWorkOrdersLoading(true);
    setWorkOrdersError('');
    void fqcWorkflowApi.workOrders()
      .then(response => {
        if (!cancelled)
          setWorkOrders(response.data?.data ?? []);
      })
      .catch(loadError => {
        if (!cancelled) {
          setWorkOrders([]);
          setWorkOrdersError(getErrorMessage(loadError));
        }
      })
      .finally(() => {
        if (!cancelled)
          setWorkOrdersLoading(false);
      });
    return () => { cancelled = true; };
  }, [activeView]);

  useEffect(() => {
    if (activeView !== 'templates')
      return;
    let cancelled = false;
    const loadTemplateData = async () => {
      setLoading(true);
      const results = await Promise.allSettled([
        inspectionTemplatesApi.list(),
        operationsApi.list({ page: 1, pageSize: 200 }),
      ]);
      if (cancelled)
        return;

      const [templateResult, operationResult] = results;
      if (templateResult.status === 'fulfilled')
        setTemplates(templateResult.value.data?.data ?? []);
      if (operationResult.status === 'fulfilled')
        setOperations(operationResult.value.data?.data ?? []);

      const requestLabels = ['檢驗範本', '工藝清單'];
      const failedRequests = results.flatMap((result, index) =>
        result.status === 'rejected'
          ? [`${requestLabels[index]}：${getErrorMessage(result.reason)}`]
          : []);
      setTemplateDataError(failedRequests.join('；'));
      setLoading(false);
    };
    void loadTemplateData();
    return () => { cancelled = true; };
  }, [activeView]);

  useEffect(() => {
    let cancelled = false;
    if (!templateDraft.operationId) {
      setOperationProducts([]);
      setOperationProductsLoading(false);
      setProductSearchError('');
      return;
    }

    setOperationProducts([]);
    setOperationProductsLoading(true);
    setProductSearchError('');
    void routingsApi.list({ operationId: templateDraft.operationId })
      .then(response => {
        if (!response.data.success)
          throw new Error(response.data.message || '載入工藝適用產品失敗');
        if (cancelled)
          return;
        const products = new Map<string, { id: string; skuNumber: string; skuName: string }>();
        (response.data.data ?? []).forEach(routing => {
          if (!products.has(routing.productSkuId)) {
            products.set(routing.productSkuId, {
              id: routing.productSkuId,
              skuNumber: routing.productSkuNumber ?? '',
              skuName: routing.productSkuName ?? '',
            });
          }
        });
        setOperationProducts([...products.values()].sort((first, second) => first.skuNumber.localeCompare(second.skuNumber)));
      })
      .catch(loadError => {
        if (!cancelled)
          setProductSearchError(getErrorMessage(loadError));
      })
      .finally(() => {
        if (!cancelled)
          setOperationProductsLoading(false);
      });

    return () => { cancelled = true; };
  }, [templateDraft.operationId]);

  useEffect(() => {
    if (!workOrderId) {
      setOperationOptions([]);
      setReports([]);
      setOperationSeq('');
      setTemplateId('');
      return;
    }
    const loadWorkOrderInspections = async () => {
      try {
        const [operationResponse, reportResponse] = await Promise.all([
          processInspectionsApi.workOrderOperations(workOrderId),
          processInspectionsApi.list({ workOrderId }),
        ]);
        const options = operationResponse.data?.data ?? [];
        setOperationOptions(options);
        setReports(reportResponse.data?.data ?? []);
        setOperationSeq(options[0]?.operationSeq.toString() ?? '');
      } catch (loadError) {
        setError(getErrorMessage(loadError));
      }
    };
    void loadWorkOrderInspections();
  }, [workOrderId]);

  const selectedOperation = operationOptions.find(item => item.operationSeq.toString() === operationSeq);
  const selectedTemplate = selectedOperation?.templates.find(item => item.id === templateId);
  const normalizedProductSearch = productSearch.trim().toLocaleLowerCase();
  const matchingOperationProducts = operationProducts
    .filter(product => !templateDraft.productSkuIds.includes(product.id))
    .filter(product => !normalizedProductSearch ||
      `${product.skuNumber} ${product.skuName}`.toLocaleLowerCase().includes(normalizedProductSearch));
  const visibleOperationProducts = matchingOperationProducts.slice(0, 20);

  useEffect(() => {
    setTemplateId(selectedOperation?.templates[0]?.id ?? '');
  }, [operationSeq, operationOptions, selectedOperation?.templates]);

  useEffect(() => {
    const next: Record<string, InspectionValue> = {};
    selectedTemplate?.items.forEach(item => {
      next[item.id] = { value: '', result: 'pass', notes: '' };
    });
    setValues(next);
  }, [templateId, templates, selectedTemplate?.items]);

  const refreshTemplates = async () => {
    const response = await inspectionTemplatesApi.list();
    setTemplates(response.data?.data ?? []);
  };

  const changeTemplateOperation = (operationId: string) => {
    setTemplateDraft(current => ({ ...current, operationId, productSkuIds: [] }));
    setSelectedSkus([]);
    setProductSearch('');
    setProductDropdownOpen(false);
  };

  const addTemplateProduct = (product: { id: string; skuNumber: string; skuName: string }) => {
    setTemplateDraft(current => current.productSkuIds.includes(product.id)
      ? current
      : { ...current, productSkuIds: [...current.productSkuIds, product.id] });
    setSelectedSkus(current => current.some(selected => selected.id === product.id)
      ? current
      : [...current, product]);
    setProductSearch('');
    setProductDropdownOpen(true);
  };

  const updateItemDraft = (index: number, patch: Partial<TemplateItemDraft>) => {
    setItemDrafts(rows => rows.map((row, rowIndex) => rowIndex === index ? { ...row, ...patch } : row));
  };

  const addBlankInspectionItem = () => {
    setItemDrafts(items => [...items, emptyItem(getNextInspectionItemCode(templateDraft.templateCode, items))]);
  };

  const addInspectionPreset = (preset: Omit<TemplateItemDraft, 'itemCode'>) => {
    setItemDrafts(items => [...items, {
      ...preset,
      itemCode: getNextInspectionItemCode(templateDraft.templateCode, items),
    }]);
  };

  const beginCreateTemplate = () => {
    if (!canCreate)
      return;
    setEditingTemplate(null);
    setTemplateDraft(emptyTemplate());
    setSelectedSkus([]);
    setProductSearch('');
    setProductDropdownOpen(false);
    setItemDrafts([]);
    setActivePresetCategory('appearance');
    setError('');
  };

  const beginEditTemplate = (template: InspectionTemplateDto) => {
    if (!canEdit)
      return;
    setEditingTemplate(template);
    setTemplateDraft({
      templateCode: template.templateCode,
      templateName: template.templateName,
      operationId: template.operationId,
      productSkuIds: template.productSkus.map(product => product.id),
      isActive: template.isActive,
      notes: template.notes ?? '',
    });
    setSelectedSkus(template.productSkus.map(product => ({
      id: product.id,
      skuNumber: product.skuNumber,
      skuName: product.skuName,
    })));
    setProductSearch('');
    setProductDropdownOpen(false);
    setActivePresetCategory(null);
    setItemDrafts(template.items.map(item => ({
      itemCode: item.itemCode,
      itemName: item.itemName,
      inputType: item.inputType,
      specification: item.specification ?? '',
      unit: item.unit ?? '',
      lowerLimit: item.lowerLimit?.toString() ?? '',
      upperLimit: item.upperLimit?.toString() ?? '',
      options: item.options.join(', '),
      inspectionMethod: item.inspectionMethod ?? '',
      instrument: item.instrument ?? '',
      samplingPlan: item.samplingPlan ?? '',
      isRequired: item.isRequired,
      isCritical: item.isCritical,
    })));
    setError('');
  };

  const saveTemplate = async () => {
    if (!templateDraft.templateCode.trim() || !templateDraft.templateName.trim() ||
        !templateDraft.operationId || itemDrafts.length === 0 ||
        itemDrafts.some(item => !item.itemCode.trim() || !item.itemName.trim())) {
      setError('請填妥範本代碼、名稱、工藝，並至少新增一個具有項目代碼與名稱的檢驗項目。');
      return;
    }
    if (itemDrafts.some(item => item.inputType === 'number' && item.lowerLimit === '' && item.upperLimit === '')) {
      setError('數值量測項目必須設定至少一項允收下限或上限，系統才能正確判定檢驗結果。');
      return;
    }
    if (itemDrafts.some(item => item.inputType === 'number' && item.lowerLimit !== '' && item.upperLimit !== '' && Number(item.lowerLimit) > Number(item.upperLimit))) {
      setError('數值量測項目的下限不可大於上限。');
      return;
    }
    const data = {
      ...templateDraft,
      items: itemDrafts.map((item, index) => ({
        ...item,
        lowerLimit: item.lowerLimit === '' ? null : Number(item.lowerLimit),
        upperLimit: item.upperLimit === '' ? null : Number(item.upperLimit),
        options: item.options.split(',').map(option => option.trim()).filter(Boolean),
        sortOrder: index,
      })),
    };

    setSaving(true);
    setError('');
    try {
      if (editingTemplate) await inspectionTemplatesApi.update(editingTemplate.id, data);
      else await inspectionTemplatesApi.create(data);
      await refreshTemplates();
      setEditingTemplate(null);
      setItemDrafts([]);
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const submitInspection = async () => {
    if (!workOrderId || !selectedOperation || !selectedTemplate || !inspectorName.trim() ||
        !Number.isFinite(Number(sampleQuantity)) || Number(sampleQuantity) <= 0) {
      setError('請選擇工單、適用範本，並填寫檢驗人員與有效的抽樣數量。');
      return;
    }
    setSaving(true);
    setError('');
    try {
      await processInspectionsApi.create({
        workOrderId,
        operationSeq: selectedOperation.operationSeq,
        templateId: selectedTemplate.id,
        lotNumber: lotNumber.trim() || null,
        inspectorName: inspectorName.trim(),
        sampleQuantity: Number(sampleQuantity),
        notes: inspectionNotes.trim() || null,
        items: selectedTemplate.items.map(item => ({
          templateItemId: item.id,
          value: values[item.id]?.value ?? '',
          result: values[item.id]?.result ?? 'pass',
          notes: values[item.id]?.notes ?? '',
        })),
      });
      const response = await processInspectionsApi.list({ workOrderId });
      setReports(response.data?.data ?? []);
      setLotNumber('');
      setInspectionNotes('');
      setError('');
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const updateValue = (id: string, patch: Partial<InspectionValue>) => {
    setValues(current => ({ ...current, [id]: { ...current[id], ...patch } }));
  };

  const viewOptions: Array<{
    id: ProcessQualityView;
    label: string;
    description: string;
    icon: typeof ClipboardCheck;
    permissionHint: string;
  }> = [
    {
      id: 'fqc',
      label: '成品檢驗（FQC）',
      description: '工單批次判定、隔離品處置與完工入庫',
      icon: FileCheck2,
      permissionHint: canCreate ? '依權限執行申請或判定' : '檢視權限',
    },
    {
      id: 'records',
      label: '製程檢驗紀錄',
      description: '依工單與工序登錄、查詢製程檢驗結果',
      icon: ClipboardList,
      permissionHint: canCreate ? '可登錄與查詢' : '唯讀查詢',
    },
    {
      id: 'templates',
      label: '檢驗範本管理',
      description: '設定適用工藝、產品與檢驗項目',
      icon: Settings2,
      permissionHint: canCreate || canEdit ? '可依角色維護範本' : '唯讀查詢',
    },
  ];

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">工藝檢驗</h2>
          <p className="module-page-desc">依角色顯示可用作業 · 檢驗紀錄 · 成品批次放行與處置</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-ghost btn-sm" onClick={onBack}>返回品質管理</button>
          {activeView === 'templates' && canCreate && (
            <button className="btn btn-primary btn-sm" onClick={beginCreateTemplate}>
              <Plus size={13} />新增檢驗範本
            </button>
          )}
        </div>
      </div>

      {availableViews.length > 0 ? (
        <nav className="process-quality-nav" aria-label="工藝檢驗作業">
          {viewOptions.filter(option => availableViews.includes(option.id)).map(option => {
            const Icon = option.icon;
            const selected = activeView === option.id;
            return (
              <button
                key={option.id}
                className={`process-quality-nav-card${selected ? ' active' : ''}`}
                aria-pressed={selected}
                onClick={() => { setError(''); setView(option.id); }}
              >
                <Icon size={20} aria-hidden="true" />
                <span className="process-quality-nav-copy">
                  <strong>{option.label}</strong>
                  <small>{option.description}</small>
                  <small className="process-quality-permission">{option.permissionHint}</small>
                </span>
              </button>
            );
          })}
        </nav>
      ) : (
        <div className="glass-card" role="alert" style={{ padding: 16 }}>
          目前帳號沒有工藝檢驗檢視權限（quality:view）。請洽系統管理員確認角色授權。
        </div>
      )}

      {error && <div className="glass-card" role="alert" style={{ padding: 12, color: 'var(--crimson-300)' }}>{error}</div>}

      {activeView === 'fqc' ? (
        <FqcWorkflowPanel />
      ) : activeView === 'templates' ? (
        <>
          {templateDataError && <div className="glass-card" role="alert" style={{ padding: 12, color: 'var(--crimson-300)' }}>{templateDataError}</div>}
          {editingTemplate !== null || itemDrafts.length > 0 ? (
            <div className="glass-card" style={{ padding: 20, display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>
                  {editingTemplate ? `編輯範本 · v${editingTemplate.version}` : '新增檢驗範本'}
                </h3>
                <button className="btn btn-ghost btn-sm" onClick={() => { setEditingTemplate(null); setItemDrafts([]); }}>
                  取消
                </button>
              </div>
              <div className="inspection-template-grid">
                <label>範本代碼<input className="form-input" value={templateDraft.templateCode} onChange={e => setTemplateDraft({ ...templateDraft, templateCode: e.target.value })} /></label>
                <label>範本名稱<input className="form-input" value={templateDraft.templateName} onChange={e => setTemplateDraft({ ...templateDraft, templateName: e.target.value })} /></label>
                <label>適用工藝<select className="form-input" value={templateDraft.operationId} onChange={event => changeTemplateOperation(event.target.value)}><option value="">選擇工藝</option>{operations.map(operation => <option key={operation.id} value={operation.id}>{operation.operationCode} - {operation.operationName}</option>)}</select></label>
                <fieldset className="inspection-template-product-field">
                  <legend>適用產品 <span>{templateDraft.productSkuIds.length > 0 ? `已選 ${templateDraft.productSkuIds.length} 項` : '未選擇（適用所有產品）'}</span></legend>
                  <div className="inspection-template-product-summary">
                    <small>
                      {templateDraft.operationId
                        ? `僅顯示工藝路線包含所選工藝的產品（${operationProductsLoading ? '載入中…' : `${operationProducts.length} 項`}）。未選產品時，範本適用於此工藝下所有產品。`
                        : '請先選擇工藝，再從該工藝的產品中選取。未選產品時，範本適用於此工藝下所有產品。'}
                    </small>
                    {selectedSkus.length > 0 && (
                      <button
                        type="button"
                        className="btn btn-ghost btn-sm"
                        onClick={() => {
                          setSelectedSkus([]);
                          setTemplateDraft(current => ({ ...current, productSkuIds: [] }));
                        }}
                      >
                        清除選取，套用所有產品
                      </button>
                    )}
                  </div>
                  {selectedSkus.length > 0 && (
                    <div className="inspection-template-selected-products" aria-label="已選產品">
                      {selectedSkus.map(sku => (
                        <span key={sku.id} className="inspection-template-product-chip">
                          {sku.skuNumber} - {sku.skuName}
                          <button
                            type="button"
                            aria-label={`移除 ${sku.skuNumber} - ${sku.skuName}`}
                            onClick={() => {
                              setSelectedSkus(current => current.filter(product => product.id !== sku.id));
                              setTemplateDraft(current => ({
                                ...current,
                                productSkuIds: current.productSkuIds.filter(id => id !== sku.id),
                              }));
                            }}
                          >
                            <X size={12} aria-hidden="true" />
                          </button>
                        </span>
                      ))}
                    </div>
                  )}
                  <div
                    className="inspection-template-product-picker"
                    onBlur={event => {
                      const nextTarget = event.relatedTarget;
                      if (!(nextTarget instanceof Node) || !event.currentTarget.contains(nextTarget))
                        setProductDropdownOpen(false);
                    }}
                  >
                    <input
                      className="form-input inspection-template-product-search"
                      type="search"
                      value={productSearch}
                      disabled={!templateDraft.operationId}
                      onFocus={() => setProductDropdownOpen(true)}
                      onChange={event => setProductSearch(event.target.value)}
                      onKeyDown={event => {
                        if (event.key === 'Escape')
                          setProductDropdownOpen(false);
                      }}
                      placeholder={templateDraft.operationId
                        ? '輸入產品料號或品名，從此工藝的產品中選取'
                        : '請先選擇適用工藝'}
                      aria-label="輸入並選取適用產品"
                      role="combobox"
                      aria-autocomplete="list"
                      aria-expanded={productDropdownOpen}
                      aria-controls="inspection-template-product-options"
                    />
                    {productDropdownOpen && templateDraft.operationId && (
                      <div id="inspection-template-product-options" className="inspection-template-product-dropdown" role="listbox">
                        {operationProductsLoading ? (
                          <div className="inspection-template-product-dropdown-message">載入此工藝的產品中…</div>
                        ) : productSearchError ? (
                          <div className="inspection-template-product-error" role="alert">{productSearchError}</div>
                        ) : operationProducts.length === 0 ? (
                          <div className="inspection-template-product-dropdown-message">沒有啟用中的產品工藝路線包含此工藝。</div>
                        ) : visibleOperationProducts.length === 0 ? (
                          <div className="inspection-template-product-dropdown-message">找不到符合的未選產品，請調整關鍵字。</div>
                        ) : (
                          <>
                            {visibleOperationProducts.map(product => (
                              <button
                                key={product.id}
                                type="button"
                                role="option"
                                aria-selected={false}
                                className="inspection-template-product-option"
                                onMouseDown={event => event.preventDefault()}
                                onClick={() => addTemplateProduct(product)}
                              >
                                <span className="inspection-template-product-option-code">{product.skuNumber}</span>
                                <span>{product.skuName}</span>
                              </button>
                            ))}
                            <div className="inspection-template-product-dropdown-footer">
                              {matchingOperationProducts.length > visibleOperationProducts.length
                                ? `符合 ${matchingOperationProducts.length} 項，顯示前 ${visibleOperationProducts.length} 項；請輸入關鍵字縮小範圍。`
                                : `共 ${matchingOperationProducts.length} 項符合產品`}
                            </div>
                          </>
                        )}
                      </div>
                    )}
                  </div>
                  {!productDropdownOpen && templateDraft.operationId && (
                    <small className="inspection-template-product-hint">點擊欄位即可展開產品清單，也可輸入料號或品名篩選。</small>
                  )}
                </fieldset>
              </div>
              <label>備註<textarea className="form-input" value={templateDraft.notes} onChange={e => setTemplateDraft({ ...templateDraft, notes: e.target.value })} /></label>

              <section className="inspection-items-section">
                <div className="inspection-items-heading">
                  <div>
                    <h4>檢驗項目 <span>{itemDrafts.length}</span></h4>
                    <small>先從常用項目快速加入，再依產品圖面、簽樣及客戶要求調整。</small>
                  </div>
                  <button type="button" className="btn btn-ghost btn-sm" onClick={addBlankInspectionItem}><Plus size={13} />新增自訂項目</button>
                </div>

                <div className="inspection-item-preset-panel">
                  <div className="inspection-item-preset-heading">
                    <strong>射出成型常用項目</strong>
                    <small>選擇一個項目即可加入範本，不需要的項目不用加入。</small>
                  </div>
                  <div className="inspection-item-preset-categories" role="group" aria-label="檢驗項目分類">
                    {inspectionItemPresetGroups.map(group => (
                      <button
                        key={group.id}
                        type="button"
                        className={`inspection-item-preset-category${activePresetCategory === group.id ? ' active' : ''}`}
                        aria-pressed={activePresetCategory === group.id}
                        onClick={() => setActivePresetCategory(current => current === group.id ? null : group.id)}
                      >
                        <strong>{group.label}</strong>
                        <small>{group.description}</small>
                      </button>
                    ))}
                  </div>
                  {activePresetCategory && inspectionItemPresetGroups
                    .filter(group => group.id === activePresetCategory)
                    .map(group => (
                      <div key={group.id} className="inspection-item-preset-list">
                        {group.items.map(preset => (
                          <div key={preset.id} className="inspection-item-preset-option">
                            <div>
                              <strong>{preset.item.itemName}</strong>
                              {preset.guidance && <small>{preset.guidance}</small>}
                            </div>
                            <button type="button" className="btn btn-ghost btn-sm" onClick={() => addInspectionPreset(preset.item)}>
                              <Plus size={13} />加入
                            </button>
                          </div>
                        ))}
                      </div>
                    ))}
                  <p className="inspection-item-preset-notice">
                    範本只提供起始建議，不代表通用允收標準；請依產品圖面、客戶簽樣與品質計畫確認規格及抽樣方式。首件、巡檢頻率及出貨 AQL 請依客戶要求設定。
                  </p>
                </div>

                {itemDrafts.length === 0 ? (
                  <div className="inspection-item-empty-state">尚未加入檢驗項目。請選擇上方常用項目，或新增自訂項目。</div>
                ) : itemDrafts.map((item, index) => (
                  <article key={index} className="inspection-item-editor">
                    <div className="inspection-item-card-heading">
                      <strong>檢驗項目 {String(index + 1).padStart(2, '0')}</strong>
                      <button
                        type="button"
                        className="icon-action-btn"
                        aria-label={`移除檢驗項目 ${index + 1}`}
                        title="移除此項目"
                        onClick={() => setItemDrafts(rows => rows.filter((_, itemIndex) => itemIndex !== index))}
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                    <div className="inspection-item-main-grid">
                      <label className="inspection-item-code-field">項目代碼
                        <input className="form-input" value={item.itemCode} onChange={event => updateItemDraft(index, { itemCode: event.target.value })} />
                      </label>
                      <label className="inspection-item-name-field">檢驗項目名稱
                        <input className="form-input" value={item.itemName} placeholder="例如：毛邊 / 披鋒" onChange={event => updateItemDraft(index, { itemName: event.target.value })} />
                      </label>
                      <label>檢驗結果型態
                        <select className="form-input" value={item.inputType} onChange={event => updateItemDraft(index, { inputType: event.target.value })}>
                          <option value="passFail">合格 / 不合格</option>
                          <option value="number">數值量測</option>
                          <option value="choice">選擇題</option>
                          <option value="text">文字紀錄</option>
                          <option value="boolean">是 / 否</option>
                        </select>
                      </label>
                    </div>
                    <label className="inspection-item-detail-field">檢驗規格 / 判定標準
                      <textarea className="form-input" rows={2} value={item.specification} placeholder="依工程圖面、客戶簽樣或產品允收標準填寫" onChange={event => updateItemDraft(index, { specification: event.target.value })} />
                    </label>
                    <div className="inspection-item-detail-grid">
                      <label>檢驗方法
                        <input className="form-input" value={item.inspectionMethod} placeholder="例如：目視檢查並對照簽樣" onChange={event => updateItemDraft(index, { inspectionMethod: event.target.value })} />
                      </label>
                      <label>量具 / 設備
                        <input className="form-input" value={item.instrument} placeholder="例如：卡尺、色差儀" onChange={event => updateItemDraft(index, { instrument: event.target.value })} />
                      </label>
                      <label>抽樣計畫
                        <input className="form-input" value={item.samplingPlan} placeholder="依品質計畫設定首件、巡檢與出貨抽樣方式" onChange={event => updateItemDraft(index, { samplingPlan: event.target.value })} />
                      </label>
                    </div>
                    <div className="inspection-item-footer">
                      <label><input type="checkbox" checked={item.isRequired} onChange={event => updateItemDraft(index, { isRequired: event.target.checked })} /> 必填</label>
                      <label><input type="checkbox" checked={item.isCritical} onChange={event => updateItemDraft(index, { isCritical: event.target.checked })} /> 重要品質特性</label>
                    </div>
                    <details className="inspection-item-advanced">
                      <summary>進階設定（單位與允收範圍）</summary>
                      {item.inputType === 'number' && <small className="inspection-item-range-hint">請依圖面或客戶規格設定至少一項允收上下限；未設限會導致量測值無法正確判定。</small>}
                      <div className="inspection-item-advanced-grid">
                        {item.inputType === 'number' && (
                          <>
                            <label>單位<input className="form-input" value={item.unit} placeholder="例如：mm、ΔE" onChange={event => updateItemDraft(index, { unit: event.target.value })} /></label>
                            <label>下限<input type="number" className="form-input" value={item.lowerLimit} onChange={event => updateItemDraft(index, { lowerLimit: event.target.value })} /></label>
                            <label>上限<input type="number" className="form-input" value={item.upperLimit} onChange={event => updateItemDraft(index, { upperLimit: event.target.value })} /></label>
                          </>
                        )}
                        {item.inputType === 'choice' && (
                          <label className="inspection-item-advanced-wide">選項（以逗號分隔）
                            <input className="form-input" value={item.options} placeholder="合格, 不合格, 不適用" onChange={event => updateItemDraft(index, { options: event.target.value })} />
                          </label>
                        )}
                      </div>
                    </details>
                  </article>
                ))}
              </section>
              <label className="inspection-item-footer"><input type="checkbox" checked={templateDraft.isActive} onChange={e => setTemplateDraft({ ...templateDraft, isActive: e.target.checked })} /> 啟用此範本</label>
              <div><button className="btn btn-primary btn-sm" disabled={saving} onClick={() => void saveTemplate()}><Save size={13} />{saving ? '儲存中…' : '儲存範本'}</button></div>
            </div>
          ) : loading ? (
            <div className="glass-card" style={{ padding: 32, textAlign: 'center', color: 'var(--slate-400)' }}>載入中…</div>
          ) : templates.length === 0 ? (
            <div className="glass-card" style={{ padding: 32, textAlign: 'center', color: 'var(--slate-400)' }}>尚未建立工藝檢驗範本。</div>
          ) : (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <table className="data-table">
                <thead><tr><th>範本代碼 / 版本</th><th>檢驗範本</th><th>適用工藝</th><th>適用產品</th><th>項目數</th><th>狀態</th><th>操作</th></tr></thead>
                <tbody>{templates.map(template => (
                  <tr key={template.id}>
                    <td className="mono">{template.templateCode} v{template.version}</td>
                    <td>{template.templateName}</td>
                    <td>{template.operationCode} - {template.operationName}</td>
                    <td>{template.productSkus.length > 0
                      ? template.productSkus.map(product => `${product.skuNumber} - ${product.skuName}`).join('、')
                      : '所有產品'}</td>
                    <td>{template.items.length}</td>
                    <td>{template.isActive ? '啟用' : '停用'}</td>
                    <td>{canEdit && <button className="btn btn-ghost btn-sm" onClick={() => beginEditTemplate(template)}>編輯</button>}</td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
          )}
        </>
      ) : activeView === 'records' ? (
        <>
          <div className="glass-card" style={{ padding: 20, display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div className="inspection-template-grid">
              <label style={{ gridColumn: '1 / -1' }}>工單<select className="form-input" value={workOrderId} onChange={e => setWorkOrderId(e.target.value)} disabled={workOrdersLoading || !!workOrdersError}><option value="">{workOrdersLoading ? '載入工單中…' : '選擇工單'}</option>{workOrders.map(wo => <option key={wo.id} value={wo.id}>{formatWorkOrderOption(wo)}</option>)}</select></label>
              <label>工藝<select className="form-input" value={operationSeq} onChange={e => setOperationSeq(e.target.value)} disabled={!operationOptions.length}><option value="">選擇工藝</option>{operationOptions.map(operation => <option key={`${operation.operationSeq}-${operation.operationId}`} value={operation.operationSeq}>{operation.operationSeq}. {operation.operationName} ({operation.workCenterName ?? '未指定工作中心'})</option>)}</select></label>
              <label>檢驗範本<select className="form-input" value={templateId} onChange={e => setTemplateId(e.target.value)} disabled={!selectedOperation?.templates.length}><option value="">選擇範本</option>{selectedOperation?.templates.map(template => <option key={template.id} value={template.id}>{template.templateName} v{template.version}</option>)}</select></label>
              <label>批號<input className="form-input" value={lotNumber} onChange={e => setLotNumber(e.target.value)} placeholder="出貨明細相同批號用於追溯" /></label>
              <label>檢驗人員<input className="form-input" value={inspectorName} onChange={e => setInspectorName(e.target.value)} /></label>
              <label>抽樣數量<input type="number" min="0.01" step="0.01" className="form-input" value={sampleQuantity} onChange={e => setSampleQuantity(e.target.value)} /></label>
            </div>
            {workOrdersError && <p role="alert" style={{ color: 'var(--crimson-300)' }}>無法載入工單：{workOrdersError}</p>}
            {selectedTemplate && (
              <>
                <h3 style={{ color: 'var(--slate-100)', fontSize: 14, fontWeight: 700 }}>檢驗項目 · {selectedTemplate.templateName} v{selectedTemplate.version}</h3>
                {selectedTemplate.items.map(item => {
                  const current = values[item.id] ?? { value: '', result: 'pass', notes: '' };
                  return (
                    <div key={item.id} className="inspection-record-item">
                      <div style={{ minWidth: 160 }}>
                        <strong>{item.itemName}</strong>{item.isRequired && <span style={{ color: 'var(--crimson-300)' }}> *</span>}
                        {item.isCritical && <span className="inspection-critical">重要特性</span>}
                        <small>{item.specification || '未設定規格'}{item.unit ? ` (${item.unit})` : ''}</small>
                        {(item.lowerLimit !== undefined || item.upperLimit !== undefined) && <small>允收範圍：{item.lowerLimit ?? '−∞'} ~ {item.upperLimit ?? '∞'}</small>}
                      </div>
                      {item.inputType === 'choice' ? (
                        <select className="form-input" value={current.value} onChange={e => updateValue(item.id, { value: e.target.value })} disabled={!canCreate}>
                          <option value="">選擇結果</option>{item.options.map(option => <option key={option} value={option}>{option}</option>)}
                        </select>
                      ) : item.inputType === 'boolean' ? (
                        <select className="form-input" value={current.value} onChange={e => updateValue(item.id, { value: e.target.value })} disabled={!canCreate}>
                          <option value="">選擇結果</option><option value="true">是</option><option value="false">否</option>
                        </select>
                      ) : item.inputType === 'passFail' ? (
                        <select className="form-input" value={current.result} onChange={e => updateValue(item.id, { value: e.target.value === 'pass' ? '合格' : '不合格', result: e.target.value })} disabled={!canCreate}>
                          <option value="pass">合格</option><option value="fail">不合格</option>
                        </select>
                      ) : (
                        <input className="form-input" type={item.inputType === 'number' ? 'number' : 'text'} step={item.inputType === 'number' ? 'any' : undefined} value={current.value} onChange={e => updateValue(item.id, { value: e.target.value })} placeholder={item.inputType === 'number' ? '輸入實測值' : '輸入檢驗結果'} disabled={!canCreate} />
                      )}
                      {item.inputType !== 'number' && item.inputType !== 'passFail' && (
                        <select className="form-input" aria-label={`${item.itemName}判定`} value={current.result} onChange={e => updateValue(item.id, { result: e.target.value })} disabled={!canCreate}>
                          <option value="pass">合格</option><option value="fail">不合格</option><option value="na">不適用</option>
                        </select>
                      )}
                      <input className="form-input" value={current.notes} onChange={e => updateValue(item.id, { notes: e.target.value })} placeholder="備註" disabled={!canCreate} />
                    </div>
                  );
                })}
                <label>檢驗備註<textarea className="form-input" value={inspectionNotes} onChange={e => setInspectionNotes(e.target.value)} disabled={!canCreate} /></label>
                {canCreate && <div><button className="btn btn-primary btn-sm" disabled={saving} onClick={() => void submitInspection()}><ClipboardCheck size={13} />{saving ? '提交中…' : '提交檢驗紀錄'}</button></div>}
              </>
            )}
            {!selectedTemplate && workOrderId && selectedOperation && (
              <div style={{ color: 'var(--amber-300)', fontSize: 13 }}>此工藝尚無啟用的檢驗範本，請先到「檢驗範本」建立並啟用範本。</div>
            )}
          </div>

          {workOrderId && (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <h3 style={{ padding: 16, color: 'var(--slate-100)', fontSize: 14, fontWeight: 700 }}>最近工藝檢驗紀錄</h3>
              <table className="data-table">
                <thead><tr><th>報告編號</th><th>工藝</th><th>範本</th><th>批號</th><th>檢驗人員</th><th>檢驗時間</th><th>判定</th></tr></thead>
                <tbody>{reports.map(report => (
                  <tr key={report.id}>
                    <td className="mono">{report.reportNumber}</td><td>{report.operationName ?? '—'}</td>
                    <td>{report.templateName} v{report.templateVersion}</td><td>{report.lotNumber || '—'}</td>
                    <td>{report.inspectorName}</td><td>{new Date(report.inspectedAt).toLocaleString()}</td>
                    <td>{resultLabels[report.result] ?? report.result}</td>
                  </tr>
                ))}{reports.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--slate-500)' }}>尚無檢驗紀錄</td></tr>}</tbody>
              </table>
            </div>
          )}
        </>
      ) : null}

      <style>{`
        .process-quality-nav { display: grid; grid-template-columns: repeat(auto-fit, minmax(230px, 1fr)); gap: 12px; }
        .process-quality-nav-card { display: flex; align-items: flex-start; gap: 12px; min-height: 104px; padding: 16px; text-align: left; color: var(--slate-300); background: var(--navy-900, var(--navy-850)); border: 1px solid var(--navy-700, var(--navy-800)); border-radius: var(--radius-md); cursor: pointer; transition: border-color var(--transition-fast), background var(--transition-fast), transform var(--transition-fast); }
        .process-quality-nav-card:hover { border-color: var(--cobalt-500); transform: translateY(-1px); }
        .process-quality-nav-card.active { color: var(--cobalt-200); background: rgba(45, 110, 245, 0.12); border-color: var(--cobalt-500); box-shadow: inset 0 0 0 1px rgba(45, 110, 245, 0.16); }
        .process-quality-nav-copy { display: grid; gap: 5px; }
        .process-quality-nav-copy strong { color: var(--slate-100); font-size: 14px; }
        .process-quality-nav-copy small { color: var(--slate-400); font-size: 11px; line-height: 1.5; }
        .process-quality-nav-copy .process-quality-permission { color: var(--cobalt-300); font-weight: 600; }
        .inspection-template-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 12px; }
        .inspection-template-grid > label { display: flex; flex-direction: column; gap: 6px; color: var(--slate-400); font-size: 12px; }
        .inspection-template-product-field { grid-column: 1 / -1; min-width: 0; margin: 0; padding: 12px; border: 1px solid var(--slate-700); border-radius: 8px; color: var(--slate-400); font-size: 12px; background: rgba(15, 23, 42, 0.35); }
        .inspection-template-product-field legend { padding: 0 6px; color: var(--slate-200); font-weight: 600; }
        .inspection-template-product-field legend span { margin-left: 6px; padding: 3px 8px; border-radius: 999px; color: var(--cobalt-200); background: rgba(59, 130, 246, 0.14); font-size: 11px; font-weight: 500; }
        .inspection-template-product-summary { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 10px; }
        .inspection-template-product-field small { color: var(--slate-500); font-size: 11px; line-height: 1.5; }
        .inspection-template-selected-products { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 10px; }
        .inspection-template-product-chip { display: inline-flex; align-items: center; gap: 5px; max-width: 100%; padding: 4px 6px 4px 9px; border: 1px solid rgba(59, 130, 246, 0.28); border-radius: 999px; color: var(--cobalt-100); background: rgba(59, 130, 246, 0.1); font-size: 11px; }
        .inspection-template-product-chip button { display: inline-flex; align-items: center; justify-content: center; padding: 2px; border: 0; border-radius: 50%; color: var(--cobalt-200); background: transparent; cursor: pointer; }
        .inspection-template-product-chip button:hover { color: var(--slate-50); background: rgba(59, 130, 246, 0.3); }
        .inspection-template-product-picker { position: relative; max-width: 640px; }
        .inspection-template-product-search { width: 100%; max-width: none; }
        .inspection-template-product-dropdown { position: absolute; top: calc(100% + 4px); left: 0; right: 0; z-index: 40; max-height: 280px; overflow-y: auto; padding: 5px; border: 1px solid var(--slate-600); border-radius: 8px; background: var(--navy-900, #07111f); box-shadow: 0 12px 28px rgba(0, 0, 0, 0.4); }
        .inspection-template-product-option { display: flex; align-items: center; gap: 10px; width: 100%; padding: 9px 10px; border: 0; border-radius: 5px; color: var(--slate-300); background: transparent; text-align: left; cursor: pointer; }
        .inspection-template-product-option:hover, .inspection-template-product-option:focus-visible { background: rgba(59, 130, 246, 0.14); outline: none; }
        .inspection-template-product-option-code { min-width: 120px; color: var(--cobalt-200); font-family: var(--font-mono, monospace); }
        .inspection-template-product-dropdown-message, .inspection-template-product-dropdown-footer { padding: 9px 10px; color: var(--slate-500); font-size: 11px; }
        .inspection-template-product-dropdown-footer { border-top: 1px solid var(--slate-800); }
        .inspection-template-product-hint { display: block; margin-top: 7px; }
        .inspection-template-product-error { color: var(--rose-400) !important; }
        .inspection-items-section { display: flex; flex-direction: column; gap: 12px; }
        .inspection-items-heading, .inspection-item-preset-heading, .inspection-item-card-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
        .inspection-items-heading h4 { display: flex; align-items: center; gap: 8px; margin: 0 0 5px; color: var(--slate-200); font-size: 14px; }
        .inspection-items-heading h4 span { padding: 2px 8px; border-radius: 999px; color: var(--cobalt-200); background: rgba(59, 130, 246, 0.14); font-size: 11px; }
        .inspection-items-heading small, .inspection-item-preset-heading small { color: var(--slate-500); font-size: 11px; line-height: 1.5; }
        .inspection-item-preset-panel { padding: 14px; border: 1px solid var(--slate-700); border-radius: 8px; background: rgba(15, 23, 42, 0.35); }
        .inspection-item-preset-heading { justify-content: flex-start; flex-wrap: wrap; margin-bottom: 10px; }
        .inspection-item-preset-heading strong { color: var(--slate-200); font-size: 13px; }
        .inspection-item-preset-categories { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; }
        .inspection-item-preset-category { display: flex; flex-direction: column; align-items: flex-start; gap: 4px; padding: 10px 12px; border: 1px solid var(--slate-700); border-radius: 6px; color: var(--slate-300); background: var(--navy-900, var(--navy-850)); text-align: left; cursor: pointer; }
        .inspection-item-preset-category.active { border-color: var(--cobalt-500); background: rgba(45, 110, 245, 0.12); }
        .inspection-item-preset-category strong { font-size: 12px; }
        .inspection-item-preset-category small { color: var(--slate-500); font-size: 10px; }
        .inspection-item-preset-list { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 6px; max-height: 260px; overflow-y: auto; margin-top: 10px; }
        .inspection-item-preset-option { display: flex; align-items: center; justify-content: space-between; gap: 10px; min-width: 0; padding: 8px 10px; border: 1px solid var(--slate-800); border-radius: 6px; background: rgba(15, 23, 42, 0.5); }
        .inspection-item-preset-option > div { display: grid; gap: 3px; min-width: 0; }
        .inspection-item-preset-option strong { color: var(--slate-200); font-size: 11px; }
        .inspection-item-preset-option small { color: var(--slate-500); font-size: 10px; }
        .inspection-item-preset-notice { margin: 10px 0 0; color: var(--amber-300); font-size: 11px; line-height: 1.5; }
        .inspection-item-empty-state { padding: 18px; border: 1px dashed var(--slate-700); border-radius: 8px; color: var(--slate-500); font-size: 12px; text-align: center; }
        .inspection-item-editor { padding: 14px; border: 1px solid var(--slate-700); border-radius: 8px; background: var(--slate-850, var(--slate-800)); }
        .inspection-item-card-heading { margin-bottom: 12px; }
        .inspection-item-card-heading strong { color: var(--cobalt-200); font-size: 12px; }
        .inspection-item-main-grid { display: grid; grid-template-columns: minmax(140px, 0.8fr) minmax(0, 2fr) minmax(220px, 1fr); gap: 10px; }
        .inspection-item-code-field input { font-family: var(--font-mono, monospace); }
        .inspection-item-editor label { display: flex; flex-direction: column; gap: 6px; min-width: 0; color: var(--slate-400); font-size: 11px; }
        .inspection-item-name-field { font-weight: 600; }
        .inspection-item-detail-field { margin-top: 10px; }
        .inspection-item-detail-field textarea { resize: vertical; }
        .inspection-item-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; margin-top: 10px; }
        .inspection-item-footer { display: flex; align-items: center; gap: 16px; margin-top: 10px; color: var(--slate-400); font-size: 11px; }
        .inspection-item-editor .inspection-item-footer label { display: inline-flex; flex-direction: row; align-items: center; gap: 6px; }
        .inspection-item-advanced { margin-top: 10px; border-top: 1px solid var(--slate-700); color: var(--slate-400); font-size: 11px; }
        .inspection-item-advanced summary { padding-top: 10px; cursor: pointer; }
        .inspection-item-range-hint { display: block; margin-top: 8px; color: var(--amber-300); line-height: 1.5; }
        .inspection-item-advanced-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 10px; padding-top: 10px; }
        .inspection-item-advanced-wide { grid-column: 1 / -1; }
        .inspection-record-item { display: grid; grid-template-columns: minmax(160px, 1.2fr) repeat(3, minmax(130px, 1fr)); gap: 10px; align-items: center; padding: 10px 0; border-top: 1px solid var(--slate-800); }
        .inspection-record-item strong { display: block; color: var(--slate-100); font-size: 13px; }
        .inspection-record-item small { display: block; margin-top: 3px; color: var(--slate-500); font-size: 11px; }
        .inspection-critical { margin-left: 6px; padding: 2px 5px; border-radius: 4px; color: var(--amber-300); background: rgba(245,158,11,.12); font-size: 10px; }
        @media (max-width: 760px) {
          .inspection-record-item { grid-template-columns: 1fr; }
          .inspection-item-preset-categories { grid-template-columns: 1fr; }
          .inspection-item-main-grid, .inspection-item-detail-grid { grid-template-columns: 1fr; }
          .inspection-items-heading, .inspection-item-preset-heading { align-items: flex-start; flex-direction: column; }
        }
      `}</style>
    </div>
  );
}
