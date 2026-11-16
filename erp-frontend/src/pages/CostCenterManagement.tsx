import React, { useCallback, useState, useEffect } from 'react';
import { Plus, Edit2, Trash2, ChevronRight, ChevronDown, DollarSign, Layers, Activity, GitBranch, PieChart, TrendingUp, Building2, Calculator, ArrowRightLeft } from 'lucide-react';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { useQueryClient } from '@tanstack/react-query';
import { costCentersApi, costElementsApi, activityTypesApi, allocationRulesApi, costPostingsApi, departmentsApi, workCentersApi } from '../api/api';
import './CostCenterManagement.css';

interface CostCenterRecord extends Record<string, unknown> {
  id: string;
  costCenterCode?: string;
  costCenterName?: string;
  costCenterType?: string;
  areaSquareMeters?: number;
  children?: CostCenterRecord[];
}

interface BudgetSummaryRecord extends Record<string, unknown> {
  id: string;
  budgetLimit?: number;
  budgetUsed?: number;
  utilizationPercent?: number;
}

interface CostElementRecord extends Record<string, unknown> {
  id: string;
  elementCode?: string;
  elementName?: string;
  elementType?: string;
  glAccountCode?: string;
  glAccountName?: string;
  description?: string;
}

interface ActivityRecord extends Record<string, unknown> {
  id: string;
  activityCode?: string;
  activityName?: string;
  costCenterName?: string;
  unitOfMeasure?: string;
  standardRate?: number;
  description?: string;
}

interface AllocationRecord extends Record<string, unknown> {
  id: string;
  sourceCostCenterName?: string;
  targetCostCenterName?: string;
  allocationBasis?: string;
  percentage?: number;
  effectiveFrom?: string;
  effectiveTo?: string;
  description?: string;
}

interface PostingRecord extends Record<string, unknown> {
  id: string;
  postingDate?: string;
  costCenterName?: string;
  elementName?: string;
  amount?: number;
  quantity?: number;
  referenceType?: string;
  notes?: string;
}

interface DepartmentRecord extends Record<string, unknown> {
  id: string;
  deptCode?: string;
  deptName?: string;
}

interface WorkCenterRecord extends Record<string, unknown> {
  id: string;
  workCenterCode?: string;
  workCenterName?: string;
}

function getRecords<T extends Record<string, unknown>>(value: unknown): T[] {
  return Array.isArray(value)
    ? value.filter(item => item !== null && typeof item === 'object' && !Array.isArray(item)) as T[]
    : [];
}

export const CostCenterManagement: React.FC = () => {
  const [activeView, setActiveView] = useState<'costcenters' | 'elements' | 'activities' | 'allocations' | 'postings'>('costcenters');
  const [loading, setLoading] = useState(true);
  const [costCenters, setCostCenters] = useState<CostCenterRecord[]>([]);
  const [costCenterTree, setCostCenterTree] = useState<CostCenterRecord[]>([]);
  const [budgetSummary, setBudgetSummary] = useState<BudgetSummaryRecord[]>([]);
  const [elements, setElements] = useState<CostElementRecord[]>([]);
  const [activities, setActivities] = useState<ActivityRecord[]>([]);
  const [allocations, setAllocations] = useState<AllocationRecord[]>([]);
  const [postings, setPostings] = useState<PostingRecord[]>([]);
  const [departments, setDepartments] = useState<DepartmentRecord[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenterRecord[]>([]);
  const [expandedCC, setExpandedCC] = useState<Set<string>>(new Set());
  const queryClient = useQueryClient();

  const [showCCModal, setShowCCModal] = useState(false);
  const [showElementModal, setShowElementModal] = useState(false);
  const [showActivityModal, setShowActivityModal] = useState(false);
  const [showAllocationModal, setShowAllocationModal] = useState(false);
  const [showPostingModal, setShowPostingModal] = useState(false);
  const [editingCC, setEditingCC] = useState<CostCenterRecord | null>(null);
  const [editingElement, setEditingElement] = useState<CostElementRecord | null>(null);
  const [editingActivity, setEditingActivity] = useState<ActivityRecord | null>(null);
  const [editingAllocation, setEditingAllocation] = useState<AllocationRecord | null>(null);
  const [editingPosting, setEditingPosting] = useState<PostingRecord | null>(null);

  const stats = {
    totalCC: costCenters.length,
    totalElements: elements.length,
    totalActivities: activities.length,
    totalAllocations: allocations.length,
    totalBudget: budgetSummary.reduce((sum, b) => sum + (b.budgetLimit || 0), 0),
    usedBudget: budgetSummary.reduce((sum, b) => sum + (b.budgetUsed || 0), 0),
  };

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      if (activeView === 'costcenters') {
        const [ccRes, treeRes, budgetRes, deptRes, wcRes] = await Promise.all([
          costCentersApi.list(),
          costCentersApi.tree(),
          costCentersApi.budget(),
          departmentsApi.flat(),
          workCentersApi.list(),
        ]);
        setCostCenters(getRecords<CostCenterRecord>(ccRes.data?.data));
        setCostCenterTree(getRecords<CostCenterRecord>(treeRes.data?.data));
        setBudgetSummary(getRecords<BudgetSummaryRecord>(budgetRes.data?.data));
        setDepartments(getRecords<DepartmentRecord>(deptRes.data?.data));
        setWorkCenters(getRecords<WorkCenterRecord>(wcRes.data?.data));
      } else if (activeView === 'elements') {
        const res = await costElementsApi.list();
        setElements(getRecords<CostElementRecord>(res.data?.data));
      } else if (activeView === 'activities') {
        const res = await activityTypesApi.list();
        setActivities(getRecords<ActivityRecord>(res.data?.data));
      } else if (activeView === 'allocations') {
        const res = await allocationRulesApi.list();
        setAllocations(getRecords<AllocationRecord>(res.data?.data));
      } else if (activeView === 'postings') {
        const res = await costPostingsApi.list();
        setPostings(getRecords<PostingRecord>(res.data?.data));
      }
    } catch (err) { console.error('Failed to load:', err); }
    finally { setLoading(false); }
  }, [activeView]);

  useEffect(() => { void loadData(); }, [loadData]);

  const toggleCC = (id: string) => {
    const newSet = new Set(expandedCC);
    if (newSet.has(id)) newSet.delete(id);
    else newSet.add(id);
    setExpandedCC(newSet);
  };

  const ccTypeConfig: Record<string, { label: string; color: string }> = {
    production: { label: '生產', color: 'var(--emerald-400)' },
    admin: { label: '管理', color: 'var(--cobalt-400)' },
    logistics: { label: '物流', color: 'var(--amber-400)' },
    rd: { label: '研發', color: 'var(--purple-400)' },
    quality: { label: '品質', color: 'var(--pink-400)' },
    sales: { label: '業務', color: 'var(--sky-400)' },
    procurement: { label: '採購', color: 'var(--orange-400)' },
  };

  const elementTypeConfig: Record<string, { label: string; color: string }> = {
    direct: { label: '直接', color: 'var(--emerald-400)' },
    overhead: { label: '間接', color: 'var(--amber-400)' },
    admin: { label: '管理', color: 'var(--cobalt-400)' },
    sales: { label: '銷售', color: 'var(--sky-400)' },
  };

  const ccFields = (parentCCs: CostCenterRecord[]): FormField[] => [
    { name: 'costCenterCode', label: '成本中心代碼', type: 'text', required: true, placeholder: '如: CC-001' },
    { name: 'costCenterName', label: '成本中心名稱', type: 'text', required: true, placeholder: '如: 生產製造中心' },
    { name: 'costCenterType', label: '類型', type: 'select', required: true, options: [
      { value: 'production', label: '生產' },
      { value: 'admin', label: '管理' },
      { value: 'logistics', label: '物流' },
      { value: 'rd', label: '研發' },
      { value: 'quality', label: '品質' },
      { value: 'sales', label: '業務' },
      { value: 'procurement', label: '採購' },
    ]},
    { name: 'parentCostCenterId', label: '上級成本中心', type: 'select', options: [{ value: '', label: '無' }, ...parentCCs.map(c => ({ value: c.id, label: `${c.costCenterCode} - ${c.costCenterName}` }))] },
    { name: 'departmentId', label: '關聯部門', type: 'select', options: [{ value: '', label: '無' }, ...departments.map(d => ({ value: d.id, label: `${d.deptCode} - ${d.deptName}` }))] },
    { name: 'workCenterId', label: '關聯工作中心', type: 'select', options: [{ value: '', label: '無' }, ...workCenters.map(w => ({ value: w.id, label: `${w.workCenterCode} - ${w.workCenterName}` }))] },
    { name: 'areaSquareMeters', label: '占用面積（平方公尺）', type: 'number', placeholder: '製造成本中心租金分攤依據' },
    { name: 'budgetLimit', label: '預算上限', type: 'number', placeholder: '0' },
    { name: 'effectiveFrom', label: '生效日期', type: 'date' },
    { name: 'effectiveTo', label: '失效日期', type: 'date' },
    { name: 'displayOrder', label: '排序', type: 'number', placeholder: '0' },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const elementFields: FormField[] = [
    { name: 'elementCode', label: '要素代碼', type: 'text', required: true, placeholder: '如: MAT, LAB' },
    { name: 'elementName', label: '要素名稱', type: 'text', required: true, placeholder: '如: 直接材料、直接人工' },
    { name: 'elementType', label: '類型', type: 'select', required: true, options: [
      { value: 'direct', label: '直接成本' },
      { value: 'overhead', label: '間接成本 (製造費用)' },
      { value: 'admin', label: '管理費用' },
      { value: 'sales', label: '銷售費用' },
    ]},
    { name: 'glAccountCode', label: 'GL帳戶代碼', type: 'text', placeholder: '如: 5101' },
    { name: 'glAccountName', label: 'GL帳戶名稱', type: 'text', placeholder: '如: 直接材料成本' },
    { name: 'displayOrder', label: '排序', type: 'number', placeholder: '0' },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const activityFields = (ccs: CostCenterRecord[]): FormField[] => [
    { name: 'activityCode', label: '作業代碼', type: 'text', required: true, placeholder: '如: MACH-HR' },
    { name: 'activityName', label: '作業名稱', type: 'text', required: true, placeholder: '如: 加工工時' },
    { name: 'costCenterId', label: '所屬成本中心', type: 'select', required: true, options: ccs.map(c => ({ value: c.id, label: `${c.costCenterCode} - ${c.costCenterName}` })) },
    { name: 'unitOfMeasure', label: '計價單位', type: 'text', placeholder: '如: 小時、機時' },
    { name: 'standardRate', label: '標準費率', type: 'number', required: true, placeholder: '0' },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const allocationFields = (ccs: CostCenterRecord[]): FormField[] => [
    { name: 'sourceCostCenterId', label: '來源成本中心', type: 'select', required: true, options: ccs.map(c => ({ value: c.id, label: `${c.costCenterCode} - ${c.costCenterName}` })) },
    { name: 'targetCostCenterId', label: '目標成本中心', type: 'select', required: true, options: ccs.map(c => ({ value: c.id, label: `${c.costCenterCode} - ${c.costCenterName}` })) },
    { name: 'allocationBasis', label: '分配基準', type: 'select', required: true, options: [
      { value: 'percentage', label: '百分比' },
      { value: 'actual', label: '實際用量' },
      { value: 'budget', label: '預算比例' },
    ]},
    { name: 'percentage', label: '分配比例 (%)', type: 'number', required: true, placeholder: '0' },
    { name: 'effectiveFrom', label: '生效日期', type: 'date' },
    { name: 'effectiveTo', label: '失效日期', type: 'date' },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const postingFields = (ccs: CostCenterRecord[], els: CostElementRecord[]): FormField[] => [
    { name: 'costCenterId', label: '成本中心', type: 'select', required: true, options: ccs.map(c => ({ value: c.id, label: `${c.costCenterCode} - ${c.costCenterName}` })) },
    { name: 'costElementId', label: '成本要素', type: 'select', required: true, options: els.map(e => ({ value: e.id, label: `${e.elementCode} - ${e.elementName}` })) },
    { name: 'postingDate', label: '分攤日期', type: 'date', required: true },
    { name: 'amount', label: '金額', type: 'number', required: true, placeholder: '0' },
    { name: 'quantity', label: '數量', type: 'number', placeholder: '1' },
    { name: 'referenceType', label: '參考類型', type: 'text', placeholder: '如: PO, WO, EXPENSE' },
    { name: 'notes', label: '備註', type: 'textarea' },
  ];

  const handleSaveCC = async (data: Record<string, unknown>) => {
    if (editingCC) await costCentersApi.update(editingCC.id, data);
    else await costCentersApi.create(data);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleSaveElement = async (data: Record<string, unknown>) => {
    if (editingElement) await costElementsApi.update(editingElement.id, data);
    else await costElementsApi.create(data);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleSaveActivity = async (data: Record<string, unknown>) => {
    if (editingActivity) await activityTypesApi.update(editingActivity.id, data);
    else await activityTypesApi.create(data);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleSaveAllocation = async (data: Record<string, unknown>) => {
    if (editingAllocation) await allocationRulesApi.update(editingAllocation.id, data);
    else await allocationRulesApi.create(data);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleSavePosting = async (data: Record<string, unknown>) => {
    if (editingPosting) await costPostingsApi.update(editingPosting.id, data);
    else await costPostingsApi.create(data);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleDeleteCC = async (id: string) => {
    if (!confirm('確定要刪除此成本中心嗎？')) return;
    await costCentersApi.delete(id);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleDeleteElement = async (id: string) => {
    if (!confirm('確定要刪除此成本要素嗎？')) return;
    await costElementsApi.delete(id);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleDeleteActivity = async (id: string) => {
    if (!confirm('確定要刪除此作業類型嗎？')) return;
    await activityTypesApi.delete(id);
    queryClient.invalidateQueries();
    loadData();
  };

  const handleDeleteAllocation = async (id: string) => {
    if (!confirm('確定要刪除此分配規則嗎？')) return;
    await allocationRulesApi.delete(id);
    queryClient.invalidateQueries();
    loadData();
  };

  const getUtilizationColor = (pct: number) => {
    if (pct >= 90) return 'var(--red-400)';
    if (pct >= 70) return 'var(--amber-400)';
    return 'var(--emerald-400)';
  };

  const renderTreeNode = (node: CostCenterRecord, level = 0) => {
    const hasChildren = node.children?.length > 0;
    const isExpanded = expandedCC.has(node.id);
    const budget = budgetSummary.find(b => b.id === node.id);
    const utilization = budget?.utilizationPercent ?? 0;

    return (
      <div key={node.id} className="tree-node">
        <div
          className="tree-node-item"
          style={{ paddingLeft: `${level * 20 + 12}px` }}
        >
          {hasChildren ? (
            <button onClick={() => toggleCC(node.id)} className="tree-toggle">
              {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
            </button>
          ) : <span className="w-4" />}
          <span className="tree-code" style={{ backgroundColor: ccTypeConfig[node.costCenterType]?.color + '20', color: ccTypeConfig[node.costCenterType]?.color }}>
            {node.costCenterCode}
          </span>
          <span className="tree-name">{node.costCenterName}</span>
          {node.areaSquareMeters != null && (
            <span className="text-xs text-secondary">{node.areaSquareMeters.toLocaleString()} m²</span>
          )}
          {budget && (
            <div className="tree-budget">
              <span className="tree-budget-text">
                {budget.budgetUsed?.toLocaleString() ?? 0} / {budget.budgetLimit?.toLocaleString() ?? 0}
              </span>
              <div className="tree-progress">
                <div className="tree-progress-bar">
                  <div className="tree-progress-fill" style={{ width: `${Math.min(utilization, 100)}%`, backgroundColor: getUtilizationColor(utilization) }} />
                </div>
              </div>
              <span className="tree-utilization" style={{ color: getUtilizationColor(utilization) }}>{utilization.toFixed(0)}%</span>
            </div>
          )}
          <div className="tree-actions">
            <button onClick={() => { setEditingCC(node); setShowCCModal(true); }} className="icon-action-btn" title="編輯"><Edit2 size={14} /></button>
            <button onClick={() => handleDeleteCC(node.id)} className="icon-action-btn text-danger" title="刪除"><Trash2 size={14} /></button>
          </div>
        </div>
        {isExpanded && hasChildren && node.children?.map(child => renderTreeNode(child, level + 1))}
      </div>
    );
  };

  return (
    <div className="costcenter-page">
      <div className="page-header-section">
        <div className="header-content">
          <div className="header-icon">
            <Building2 size={28} />
          </div>
          <div className="header-text">
            <h1 className="page-title">成本中心管理</h1>
            <p className="page-subtitle">管理組織的成本結構、預算分配與費用分攤</p>
          </div>
        </div>
        <button className="btn-primary btn-lg" onClick={() => { setEditingCC(null); setShowCCModal(true); }}>
          <Plus size={18} />新增成本中心
        </button>
      </div>

      <div className="stats-overview">
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(45, 110, 245, 0.15)', color: 'var(--cobalt-300)' }}>
            <Layers size={20} />
          </div>
          <div className="stat-content">
            <span className="stat-value">{stats.totalCC}</span>
            <span className="stat-label">成本中心</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(16, 185, 122, 0.15)', color: 'var(--emerald-300)' }}>
            <PieChart size={20} />
          </div>
          <div className="stat-content">
            <span className="stat-value">{stats.totalElements}</span>
            <span className="stat-label">成本要素</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(245, 151, 10, 0.15)', color: 'var(--amber-300)' }}>
            <Activity size={20} />
          </div>
          <div className="stat-content">
            <span className="stat-value">{stats.totalActivities}</span>
            <span className="stat-label">作業類型</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(232, 51, 82, 0.15)', color: 'var(--crimson-300)' }}>
            <TrendingUp size={20} />
          </div>
          <div className="stat-content">
            <span className="stat-value">{stats.totalAllocations}</span>
            <span className="stat-label">分配規則</span>
          </div>
        </div>
        <div className="stat-card stat-card-wide">
          <div className="stat-icon" style={{ background: 'rgba(45, 110, 245, 0.15)', color: 'var(--cobalt-300)' }}>
            <DollarSign size={20} />
          </div>
          <div className="stat-content">
            <div className="budget-row">
              <span className="stat-value">{(stats.totalBudget / 10000).toFixed(1)}萬</span>
              <span className="stat-separator">/</span>
              <span className="stat-value stat-value-secondary">{(stats.usedBudget / 10000).toFixed(1)}萬</span>
            </div>
            <span className="stat-label">預算總額 / 已使用</span>
          </div>
          <div className="stat-progress">
            <div className="progress-bar-mini">
              <div 
                className="progress-fill-mini" 
                style={{ width: `${stats.totalBudget > 0 ? (stats.usedBudget / stats.totalBudget) * 100 : 0}%` }} 
              />
            </div>
          </div>
        </div>
      </div>

      <div className="view-tabs-container">
        <div className="view-tabs">
          <button 
            className={`view-tab ${activeView === 'costcenters' ? 'active' : ''}`} 
            onClick={() => setActiveView('costcenters')}
          >
            <Layers size={16} />成本中心
          </button>
          <button 
            className={`view-tab ${activeView === 'elements' ? 'active' : ''}`} 
            onClick={() => setActiveView('elements')}
          >
            <Calculator size={16} />成本要素
          </button>
          <button 
            className={`view-tab ${activeView === 'activities' ? 'active' : ''}`} 
            onClick={() => setActiveView('activities')}
          >
            <Activity size={16} />作業類型
          </button>
          <button 
            className={`view-tab ${activeView === 'allocations' ? 'active' : ''}`} 
            onClick={() => setActiveView('allocations')}
          >
            <GitBranch size={16} />分配規則
          </button>
          <button 
            className={`view-tab ${activeView === 'postings' ? 'active' : ''}`} 
            onClick={() => setActiveView('postings')}
          >
            <ArrowRightLeft size={16} />成本分攤
          </button>
        </div>
      </div>

      {loading ? (
        <div className="loading-container">
          <div className="loading-spinner" />
          <p>載入資料中...</p>
        </div>
      ) : (
        <>
          {activeView === 'costcenters' && (
            <div className="content-card">
              <div className="card-header-custom">
                <div className="card-title-group">
                  <Layers size={18} className="card-title-icon" />
                  <h2>成本中心結構</h2>
                </div>
                <span className="card-badge">{costCenters.length} 筆資料</span>
              </div>
              <div className="tree-container">
                {costCenterTree.length > 0 ? costCenterTree.map(node => renderTreeNode(node)) : (
                  <div className="empty-state-modern">
                    <Building2 size={48} />
                    <p>尚無成本中心資料</p>
                    <button className="btn-primary btn-sm" onClick={() => { setEditingCC(null); setShowCCModal(true); }}>
                      <Plus size={14} />建立第一個成本中心
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeView === 'elements' && (
            <div className="content-card">
              <div className="card-header-custom">
                <div className="card-title-group">
                  <Calculator size={18} className="card-title-icon" />
                  <h2>成本要素</h2>
                </div>
                <button className="btn-primary btn-sm" onClick={() => { setEditingElement(null); setShowElementModal(true); }}>
                  <Plus size={14} />新增要素
                </button>
              </div>
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>代碼</th>
                      <th>名稱</th>
                      <th>類型</th>
                      <th>GL帳戶</th>
                      <th>說明</th>
                      <th>操作</th>
                    </tr>
                  </thead>
                  <tbody>
                    {elements.map(el => (
                      <tr key={el.id}>
                        <td className="font-mono text-xs"><span className="badge-modern">{el.elementCode}</span></td>
                        <td className="font-medium">{el.elementName}</td>
                        <td><span className="badge-type" style={{ backgroundColor: elementTypeConfig[el.elementType]?.color + '20', color: elementTypeConfig[el.elementType]?.color }}>{elementTypeConfig[el.elementType]?.label}</span></td>
                        <td className="text-xs text-secondary">{el.glAccountCode} {el.glAccountName}</td>
                        <td className="text-xs text-secondary">{el.description}</td>
                        <td>
                          <div className="action-buttons">
                            <button onClick={() => { setEditingElement(el); setShowElementModal(true); }} className="icon-action-btn" title="編輯"><Edit2 size={14} /></button>
                            <button onClick={() => handleDeleteElement(el.id)} className="icon-action-btn text-danger" title="刪除"><Trash2 size={14} /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {elements.length === 0 && (
                  <div className="empty-state-modern">
                    <Calculator size={48} />
                    <p>尚無成本要素</p>
                    <button className="btn-primary btn-sm" onClick={() => { setEditingElement(null); setShowElementModal(true); }}>
                      <Plus size={14} />新增要素
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeView === 'activities' && (
            <div className="content-card">
              <div className="card-header-custom">
                <div className="card-title-group">
                  <Activity size={18} className="card-title-icon" />
                  <h2>作業類型</h2>
                </div>
                <button className="btn-primary btn-sm" onClick={() => { setEditingActivity(null); setShowActivityModal(true); }}>
                  <Plus size={14} />新增作業
                </button>
              </div>
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr><th>代碼</th><th>名稱</th><th>成本中心</th><th>單位</th><th>標準費率</th><th>說明</th><th>操作</th></tr>
                  </thead>
                  <tbody>
                    {activities.map(act => (
                      <tr key={act.id}>
                        <td className="font-mono text-xs"><span className="badge-modern">{act.activityCode}</span></td>
                        <td className="font-medium">{act.activityName}</td>
                        <td className="text-xs">{act.costCenterName}</td>
                        <td>{act.unitOfMeasure}</td>
                        <td className="text-right font-mono font-medium">{act.standardRate.toLocaleString()}</td>
                        <td className="text-xs text-secondary">{act.description}</td>
                        <td>
                          <div className="action-buttons">
                            <button onClick={() => { setEditingActivity(act); setShowActivityModal(true); }} className="icon-action-btn" title="編輯"><Edit2 size={14} /></button>
                            <button onClick={() => handleDeleteActivity(act.id)} className="icon-action-btn text-danger" title="刪除"><Trash2 size={14} /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {activities.length === 0 && (
                  <div className="empty-state-modern">
                    <Activity size={48} />
                    <p>尚無作業類型</p>
                    <button className="btn-primary btn-sm" onClick={() => { setEditingActivity(null); setShowActivityModal(true); }}>
                      <Plus size={14} />新增作業
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeView === 'allocations' && (
            <div className="content-card">
              <div className="card-header-custom">
                <div className="card-title-group">
                  <GitBranch size={18} className="card-title-icon" />
                  <h2>分配規則</h2>
                </div>
                <button className="btn-primary btn-sm" onClick={() => { setEditingAllocation(null); setShowAllocationModal(true); }}>
                  <Plus size={14} />新增規則
                </button>
              </div>
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr><th>來源</th><th>目標</th><th>基準</th><th>比例</th><th>期間</th><th>說明</th><th>操作</th></tr>
                  </thead>
                  <tbody>
                    {allocations.map(rule => (
                      <tr key={rule.id}>
                        <td className="text-xs">{rule.sourceCostCenterName}</td>
                        <td className="text-xs">{rule.targetCostCenterName}</td>
                        <td><span className="badge-basis">{rule.allocationBasis}</span></td>
                        <td className="font-mono font-medium">{rule.percentage}%</td>
                        <td className="text-xs">{rule.effectiveFrom?.slice(0,10)} ~ {rule.effectiveTo?.slice(0,10) || '-'}</td>
                        <td className="text-xs text-secondary">{rule.description}</td>
                        <td>
                          <div className="action-buttons">
                            <button onClick={() => { setEditingAllocation(rule); setShowAllocationModal(true); }} className="icon-action-btn" title="編輯"><Edit2 size={14} /></button>
                            <button onClick={() => handleDeleteAllocation(rule.id)} className="icon-action-btn text-danger" title="刪除"><Trash2 size={14} /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {allocations.length === 0 && (
                  <div className="empty-state-modern">
                    <GitBranch size={48} />
                    <p>尚無分配規則</p>
                    <button className="btn-primary btn-sm" onClick={() => { setEditingAllocation(null); setShowAllocationModal(true); }}>
                      <Plus size={14} />新增規則
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeView === 'postings' && (
            <div className="content-card">
              <div className="card-header-custom">
                <div className="card-title-group">
                  <ArrowRightLeft size={18} className="card-title-icon" />
                  <h2>成本分攤記錄</h2>
                </div>
                <button className="btn-primary btn-sm" onClick={() => { setEditingPosting(null); setShowPostingModal(true); }}>
                  <Plus size={14} />新增分攤
                </button>
              </div>
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr><th>日期</th><th>成本中心</th><th>要素</th><th>金額</th><th>數量</th><th>參考</th><th>備註</th></tr>
                  </thead>
                  <tbody>
                    {postings.slice(0, 50).map(p => (
                      <tr key={p.id}>
                        <td className="font-mono text-xs">{p.postingDate?.slice(0,10)}</td>
                        <td className="text-xs">{p.costCenterName}</td>
                        <td className="text-xs">{p.elementName}</td>
                        <td className="text-right font-mono font-medium">{p.amount.toLocaleString()}</td>
                        <td className="text-right font-mono">{p.quantity}</td>
                        <td className="text-xs">{p.referenceType}</td>
                        <td className="text-xs text-secondary">{p.notes}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {postings.length === 0 && (
                  <div className="empty-state-modern">
                    <ArrowRightLeft size={48} />
                    <p>尚無分攤記錄</p>
                    <button className="btn-primary btn-sm" onClick={() => { setEditingPosting(null); setShowPostingModal(true); }}>
                      <Plus size={14} />新增分攤
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}
        </>
      )}

      <FormModal
        open={showCCModal}
        title={editingCC ? '編輯成本中心' : '新增成本中心'}
        fields={ccFields(costCenters.filter(c => c.id !== editingCC?.id))}
        initialValues={editingCC || { effectiveFrom: new Date().toISOString().slice(0,10), isActive: true }}
        onSubmit={handleSaveCC}
        onClose={() => { setShowCCModal(false); setEditingCC(null); }}
      />

      <FormModal
        open={showElementModal}
        title={editingElement ? '編輯成本要素' : '新增成本要素'}
        fields={elementFields}
        initialValues={editingElement || { isActive: true }}
        onSubmit={handleSaveElement}
        onClose={() => { setShowElementModal(false); setEditingElement(null); }}
      />

      <FormModal
        open={showActivityModal}
        title={editingActivity ? '編輯作業類型' : '新增作業類型'}
        fields={activityFields(costCenters)}
        initialValues={editingActivity || { isActive: true }}
        onSubmit={handleSaveActivity}
        onClose={() => { setShowActivityModal(false); setEditingActivity(null); }}
      />

      <FormModal
        open={showAllocationModal}
        title={editingAllocation ? '編輯分配規則' : '新增分配規則'}
        fields={allocationFields(costCenters)}
        initialValues={editingAllocation || { effectiveFrom: new Date().toISOString().slice(0,10), allocationBasis: 'percentage', percentage: 0, isActive: true }}
        onSubmit={async (data: Record<string, unknown>) => {
          const pct = Number(data.percentage);
          if (pct <= 0 || pct > 100) {
            alert('分配比例必須介於 0 到 100 之間');
            return;
          }
          if (data.sourceCostCenterId === data.targetCostCenterId) {
            alert('來源成本中心與目標成本中心不可相同');
            return;
          }
          await handleSaveAllocation(data);
        }}
        onClose={() => { setShowAllocationModal(false); setEditingAllocation(null); }}
      />

      <FormModal
        open={showPostingModal}
        title="新增成本分攤"
        fields={postingFields(costCenters, elements)}
        initialValues={{ postingDate: new Date().toISOString().slice(0,10), quantity: 1 }}
        onSubmit={handleSavePosting}
        onClose={() => { setShowPostingModal(false); setEditingPosting(null); }}
      />
    </div>
  );
};
