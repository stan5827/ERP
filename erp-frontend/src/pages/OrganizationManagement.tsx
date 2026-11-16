import React, { useCallback, useState, useEffect } from 'react';
import { Search, Plus, Edit2, Trash2, ChevronRight, ChevronDown, Building2, Users, Shield, Loader2, UserCheck } from 'lucide-react';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { useQueryClient } from '@tanstack/react-query';
import { departmentsApi, jobTitlesApi, jobGradesApi, approvalsApi } from '../api/api';
import { getApplicableJobTitles } from '../utils/hrOrganization';

function toPascalCase(obj: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const key in obj) {
    const pascal = key.charAt(0).toUpperCase() + key.slice(1);
    const value = obj[key];
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      result[pascal] = toPascalCase(value as Record<string, unknown>);
    } else if (Array.isArray(value)) {
      result[pascal] = value.map(item => 
        (item && typeof item === 'object') ? toPascalCase(item as Record<string, unknown>) : item
      );
    } else {
      result[pascal] = value;
    }
  }
  return result;
}

const deptTypeConfig: Record<string, { label: string; color: string }> = {
  production: { label: '生產', color: 'var(--emerald-400)' },
  admin: { label: '管理', color: 'var(--cobalt-400)' },
  service: { label: '服務', color: 'var(--amber-400)' },
};

const applicableDeptTypeOptions = [
  { value: '', label: '全部' },
  { value: 'production', label: '生產部門' },
  { value: 'admin', label: '管理部門' },
  { value: 'finance', label: '財務部門' },
  { value: 'service', label: '服務部門' },
];

type OrganizationView = 'departments' | 'grades' | 'titles' | 'approvals';

interface DepartmentRecord {
  id: string;
  deptCode: string;
  deptName: string;
  deptType?: string;
  fullPath?: string;
  parentDeptId?: string;
  costCenterCode?: string;
  isCostCenter?: boolean;
  budgetLimit?: number;
  displayOrder?: number;
  description?: string;
  isActive?: boolean;
  employeeCount?: number;
  children?: DepartmentRecord[];
}

interface JobGradeRecord {
  id: string;
  gradeCode: string;
  gradeName: string;
  level: number;
  minSalary: number;
  maxSalary: number;
  overtimeRate: number;
  travelLimit: number;
  approvalLimit: number;
  employeeCount?: number;
  description?: string;
}

interface JobTitleRecord {
  id: string;
  titleCode: string;
  titleName: string;
  gradeId?: string;
  gradeCode?: string;
  applicableDeptType?: string;
  isSupervisor: boolean;
  employeeCount?: number;
  description?: string;
}

interface ApprovalAuthorityRecord {
  id: string;
  docType: string;
  docTypeName: string;
  gradeId: string;
  gradeCode: string;
  gradeName: string;
  approvalLevel: number;
  minAmount: number;
  maxAmount: number;
  description?: string;
}

function getErrorMessage(error: unknown): string {
  if (typeof error !== 'object' || error === null)
    return error instanceof Error ? error.message : String(error);
  const response = 'response' in error ? error.response : undefined;
  const responseData = typeof response === 'object' && response !== null && 'data' in response
    ? response.data
    : undefined;
  const responseMessage = typeof responseData === 'object' && responseData !== null && 'message' in responseData
    ? responseData.message
    : undefined;
  return typeof responseMessage === 'string'
    ? responseMessage
    : error instanceof Error ? error.message : String(error);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isDepartmentRecord(value: unknown): value is DepartmentRecord {
  return isRecord(value)
    && typeof value.id === 'string'
    && typeof value.deptCode === 'string'
    && typeof value.deptName === 'string'
    && (value.employeeCount === undefined || typeof value.employeeCount === 'number')
    && (value.children === undefined || (Array.isArray(value.children) && value.children.every(isDepartmentRecord)));
}

function isJobGradeRecord(value: unknown): value is JobGradeRecord {
  return isRecord(value)
    && typeof value.id === 'string'
    && typeof value.gradeCode === 'string'
    && typeof value.gradeName === 'string'
    && typeof value.level === 'number'
    && typeof value.minSalary === 'number'
    && typeof value.maxSalary === 'number'
    && typeof value.overtimeRate === 'number'
    && typeof value.travelLimit === 'number'
    && typeof value.approvalLimit === 'number'
    && (value.employeeCount === undefined || typeof value.employeeCount === 'number');
}

function isJobTitleRecord(value: unknown): value is JobTitleRecord {
  return isRecord(value)
    && typeof value.id === 'string'
    && typeof value.titleCode === 'string'
    && typeof value.titleName === 'string'
    && typeof value.isSupervisor === 'boolean'
    && (value.employeeCount === undefined || typeof value.employeeCount === 'number');
}

function isApprovalAuthorityRecord(value: unknown): value is ApprovalAuthorityRecord {
  return isRecord(value)
    && typeof value.id === 'string'
    && typeof value.docType === 'string'
    && typeof value.docTypeName === 'string'
    && typeof value.gradeId === 'string'
    && typeof value.gradeCode === 'string'
    && typeof value.gradeName === 'string'
    && typeof value.approvalLevel === 'number'
    && typeof value.minAmount === 'number'
    && typeof value.maxAmount === 'number';
}

export const OrganizationManagement: React.FC = () => {
  const [activeView, setActiveView] = useState<OrganizationView>('departments');
  const [search, setSearch] = useState('');
  const [titleDepartmentFilter, setTitleDepartmentFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [departments, setDepartments] = useState<DepartmentRecord[]>([]);
  const [departmentTree, setDepartmentTree] = useState<DepartmentRecord[]>([]);
  const [grades, setGrades] = useState<JobGradeRecord[]>([]);
  const [titles, setTitles] = useState<JobTitleRecord[]>([]);
  const [authorities, setAuthorities] = useState<ApprovalAuthorityRecord[]>([]);
  const [expandedDepts, setExpandedDepts] = useState<Set<string>>(new Set());
  const queryClient = useQueryClient();

  const [showDeptModal, setShowDeptModal] = useState(false);
  const [showGradeModal, setShowGradeModal] = useState(false);
  const [showTitleModal, setShowTitleModal] = useState(false);
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [editingDept, setEditingDept] = useState<DepartmentRecord | null>(null);
  const [editingGrade, setEditingGrade] = useState<JobGradeRecord | null>(null);
  const [editingTitle, setEditingTitle] = useState<JobTitleRecord | null>(null);
  const [editingAuth, setEditingAuth] = useState<ApprovalAuthorityRecord | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      if (activeView === 'departments') {
        const [flatRes, treeRes] = await Promise.all([
          departmentsApi.flat(),
          departmentsApi.tree(),
        ]);
        setDepartments((flatRes.data?.data ?? []).filter(isDepartmentRecord));
        setDepartmentTree((treeRes.data?.data ?? []).filter(isDepartmentRecord));
      } else if (activeView === 'grades') {
        const res = await jobGradesApi.list();
        setGrades((res.data?.data ?? []).filter(isJobGradeRecord));
      } else if (activeView === 'titles') {
        const [titlesRes, gradesRes, departmentsRes] = await Promise.all([
          jobTitlesApi.list(),
          jobGradesApi.list(),
          departmentsApi.flat(),
        ]);
        setTitles((titlesRes.data?.data ?? []).filter(isJobTitleRecord));
        setGrades((gradesRes.data?.data ?? []).filter(isJobGradeRecord));
        setDepartments((departmentsRes.data?.data ?? []).filter(isDepartmentRecord));
      } else if (activeView === 'approvals') {
        const [authRes, gradesRes] = await Promise.all([
          approvalsApi.authorities(),
          jobGradesApi.list(),
        ]);
        setAuthorities((authRes.data?.data ?? []).filter(isApprovalAuthorityRecord));
        setGrades((gradesRes.data?.data ?? []).filter(isJobGradeRecord));
      }
    } catch (err) { console.error('Failed to load:', err); }
    finally { setLoading(false); }
  }, [activeView]);

  useEffect(() => { void loadData(); }, [loadData]);

  const toggleDept = (id: string) => {
    const newSet = new Set(expandedDepts);
    if (newSet.has(id))
      newSet.delete(id);
    else
      newSet.add(id);
    setExpandedDepts(newSet);
  };

  const deptFields: FormField[] = [
    { name: 'deptCode', label: '部門代碼', type: 'text', required: true, placeholder: '如: MFG001' },
    { name: 'deptName', label: '部門名稱', type: 'text', required: true, placeholder: '如: 生產製造部' },
    { name: 'deptType', label: '部門類型', type: 'select', required: true, options: [
      { value: 'production', label: '生產部門 (直接人工)' },
      { value: 'admin', label: '管理部門 (間接人工)' },
      { value: 'service', label: '服務部門' },
    ], defaultValue: 'admin' },
    { name: 'parentDeptId', label: '上級部門', type: 'select', options: departments.map(d => ({ value: d.id, label: d.fullPath || d.deptName })) },
    { name: 'costCenterCode', label: '成本中心代碼', type: 'text', placeholder: '如: CC-MFG-001' },
    { name: 'isCostCenter', label: '是否為成本中心', type: 'checkbox', defaultValue: true },
    { name: 'budgetLimit', label: '部門預算上限', type: 'number', placeholder: '0' },
    { name: 'displayOrder', label: '排序', type: 'number', placeholder: '0', defaultValue: 0 },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const gradeFields: FormField[] = [
    { name: 'gradeCode', label: '職等代碼', type: 'text', required: true, placeholder: '如: G1, G2...' },
    { name: 'gradeName', label: '職等名稱', type: 'text', required: true, placeholder: '如: 一職等、二職等...' },
    { name: 'level', label: '排序Level', type: 'number', required: true, placeholder: '1', defaultValue: 1 },
    { name: 'minSalary', label: '最低薪資', type: 'number', required: true, placeholder: '28000', defaultValue: 28000 },
    { name: 'maxSalary', label: '最高薪資', type: 'number', required: true, placeholder: '35000', defaultValue: 35000 },
    { name: 'overtimeRate', label: '加班費倍率', type: 'number', placeholder: '1.33', defaultValue: 1.33 },
    { name: 'travelLimit', label: '差旅費上限', type: 'number', placeholder: '5000', defaultValue: 5000 },
    { name: 'approvalLimit', label: '核決金額上限', type: 'number', required: true, placeholder: '10000', defaultValue: 10000 },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const titleFields: FormField[] = [
    { name: 'titleCode', label: '職稱代碼', type: 'text', required: true, placeholder: '如: WORKER, ENGINEER...' },
    { name: 'titleName', label: '職稱名稱', type: 'text', required: true, placeholder: '如: 作業員、工程師...' },
    { name: 'gradeId', label: '預設職等', type: 'select', options: grades.map(g => ({ value: g.id, label: `${g.gradeCode} - ${g.gradeName}` })) },
    { name: 'applicableDeptType', label: '適用部門類型', type: 'select', options: applicableDeptTypeOptions, defaultValue: '' },
    { name: 'isSupervisor', label: '是否為主管職', type: 'checkbox', defaultValue: false },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const docTypes = [
    { code: 'PO', name: '採購訂單' },
    { code: 'PR', name: '採購申請' },
    { code: 'EXPENSE', name: '費用報支' },
    { code: 'LEAVE', name: '請假申請' },
    { code: 'OVERTIME', name: '加班申請' },
    { code: 'BUDGET', name: '預算申請' },
  ];

  const authFields: FormField[] = [
    { name: 'docType', label: '文件類型', type: 'select', required: true, options: docTypes.map(d => ({ value: d.code, label: d.name })) },
    { name: 'gradeId', label: '職等', type: 'select', required: true, options: grades.map(g => ({ value: g.id, label: `${g.gradeCode} - ${g.gradeName}` })) },
    { name: 'approvalLevel', label: '審核層級', type: 'number', required: true, placeholder: '1', defaultValue: 1 },
    { name: 'minAmount', label: '金額下限', type: 'number', required: true, placeholder: '0', defaultValue: 0 },
    { name: 'maxAmount', label: '金額上限', type: 'number', required: true, placeholder: '10000', defaultValue: 10000 },
    { name: 'description', label: '說明', type: 'textarea' },
  ];

  const handleSave = async (type: string, data: Record<string, unknown>, id?: string) => {
    const cleanedData: Record<string, unknown> = {};
    for (const key in data) {
      const value = data[key];
      if (value === '' || value === undefined) {
        cleanedData[key] = key.includes('Is') || key.includes('Allow') ? false : null;
      } else if (typeof value === 'string') {
        cleanedData[key] = value;
      } else {
        cleanedData[key] = value;
      }
    }
    
    try {
      const pascalData = toPascalCase(cleanedData);
      if (type === 'departments') {
        if (id) await departmentsApi.update(id, pascalData);
        else await departmentsApi.create(pascalData);
      } else if (type === 'job-grades') {
        if (id) await jobGradesApi.update(id, pascalData);
        else await jobGradesApi.create(pascalData);
      } else if (type === 'job-titles') {
        if (id) await jobTitlesApi.update(id, pascalData);
        else await jobTitlesApi.create(pascalData);
      } else if (type === 'approvals/authorities') {
        if (id) await approvalsApi.update(id, pascalData);
        else await approvalsApi.create(pascalData);
      }
      queryClient.invalidateQueries();
      loadData();
    } catch (err: unknown) {
      console.error('Save error:', err);
      alert('儲存失敗: ' + getErrorMessage(err));
    }
  };

  const handleDelete = async (type: string, id: string) => {
    if (!confirm('確定要刪除嗎？')) return;
    try {
      if (type === 'departments') await departmentsApi.delete(id);
      else if (type === 'job-grades') await jobGradesApi.delete(id);
      else if (type === 'job-titles') await jobTitlesApi.delete(id);
      else if (type === 'approvals/authorities') await approvalsApi.delete(id);
      queryClient.invalidateQueries();
      loadData();
    } catch (err: unknown) {
      alert('刪除失敗: ' + getErrorMessage(err));
    }
  };

  const renderDeptTree = (nodes: DepartmentRecord[], level = 0) => {
    return nodes.map(node => {
      const hasChildren = node.children && node.children.length > 0;
      const isExpanded = expandedDepts.has(node.id);
      const typeConfig = deptTypeConfig[node.deptType ?? 'admin'] ?? { label: node.deptType, color: 'var(--slate-400)' };
      return (
        <div key={node.id}>
          <div style={{ display: 'flex', alignItems: 'center', padding: '8px 12px', paddingLeft: level * 24 + 12, borderBottom: '1px solid var(--glass-border)', cursor: hasChildren ? 'pointer' : 'default' }}
            onClick={() => hasChildren && toggleDept(node.id)}>
            {hasChildren ? (
              isExpanded ? <ChevronDown size={14} style={{ marginRight: 8, color: 'var(--slate-500)' }} /> : <ChevronRight size={14} style={{ marginRight: 8, color: 'var(--slate-500)' }} />
            ) : <div style={{ width: 22 }} />}
            <Building2 size={14} style={{ marginRight: 8, color: typeConfig.color }} />
            <span style={{ fontWeight: 600, color: 'var(--cobalt-300)', marginRight: 8 }}>{node.deptCode}</span>
            <span style={{ color: 'var(--slate-200)', marginRight: 8 }}>{node.deptName}</span>
            <span style={{ fontSize: 10, padding: '2px 6px', background: `${typeConfig.color}20`, color: typeConfig.color, borderRadius: 3, marginRight: 8 }}>
              {typeConfig.label}
            </span>
            {(node.employeeCount ?? 0) > 0 && (
              <span style={{ fontSize: 10, padding: '2px 6px', background: 'var(--slate-700)', color: 'var(--slate-400)', borderRadius: 3 }}>
                {node.employeeCount ?? 0} 人
              </span>
            )}
            <div style={{ marginLeft: 'auto', display: 'flex', gap: 4 }}>
              <button className="icon-action-btn" onClick={(e) => { e.stopPropagation(); setEditingDept(node); setShowDeptModal(true); }}><Edit2 size={13} /></button>
              <button className="icon-action-btn" style={{ color: 'var(--crimson-400)' }} onClick={(e) => { e.stopPropagation(); handleDelete('departments', node.id); }}><Trash2 size={13} /></button>
            </div>
          </div>
          {hasChildren && isExpanded && renderDeptTree(node.children, level + 1)}
        </div>
      );
    });
  };

  const selectedTitleDepartment = departments.find(department => department.id === titleDepartmentFilter);
  const titlesForDepartment = titleDepartmentFilter
    ? getApplicableJobTitles(titles, selectedTitleDepartment)
    : titles;
  const filteredTitles = titlesForDepartment.filter(title => {
    const query = search.trim().toLocaleLowerCase();
    if (!query) return true;

    const departmentLabel = applicableDeptTypeOptions.find(option => option.value === title.applicableDeptType)?.label
      ?? title.applicableDeptType
      ?? '全部';
    return [title.titleCode, title.titleName, title.gradeCode, departmentLabel, title.description]
      .some(value => value?.toLocaleLowerCase().includes(query));
  });

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">組織架構管理</h2>
          <p className="module-page-desc">部門設定 · 職等薪資 · 職稱管理 · 核決權限矩陣</p>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 8 }}>
        {[
          { key: 'departments', icon: Building2, label: '部門架構' },
          { key: 'grades', icon: Users, label: '職等設定' },
          { key: 'titles', icon: UserCheck, label: '職稱管理' },
          { key: 'approvals', icon: Shield, label: '核決權限' },
        ].map(tab => (
          <button key={tab.key} className={`rd-tab ${activeView === tab.key ? 'active' : ''}`} onClick={() => setActiveView(tab.key as OrganizationView)}>
            <tab.icon size={14} />{tab.label}
          </button>
        ))}
      </div>

      {activeView === 'titles' && (
        <div className="role-guide-banner">
          <div className="role-guide-icon"><UserCheck size={24} /></div>
          <div>
            <h3>職稱代表員工在公司擔任的工作</h3>
            <p>設定職稱名稱後，可選擇預設職等、適用部門及是否為主管職。建立完成後，就能在員工資料中選用。</p>
          </div>
        </div>
      )}

      <div className={`glass-card${activeView === 'titles' ? ' job-title-toolbar' : ''}`} style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
        <div className="filter-search" style={{ flex: 1, maxWidth: 300 }}>
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input
            className="search-input"
            placeholder={activeView === 'titles' ? '搜尋職稱、代碼、職等或部門...' : '搜尋...'}
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        {activeView === 'titles' && (
          <select
            value={titleDepartmentFilter}
            onChange={e => setTitleDepartmentFilter(e.target.value)}
            aria-label="依部門篩選職稱"
            style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }}
          >
            <option value="">全部部門</option>
            {departments.filter(department => department.isActive !== false).map(department => (
              <option key={department.id} value={department.id}>
                {department.deptCode} - {department.deptName}
              </option>
            ))}
          </select>
        )}
        {activeView === 'titles' && (
          <span className="job-title-result-count">共 {filteredTitles.length} 個職稱</span>
        )}
        {activeView === 'departments' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingDept(null); setShowDeptModal(true); }}><Plus size={13} />新增部門</button>}
        {activeView === 'grades' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingGrade(null); setShowGradeModal(true); }}><Plus size={13} />新增職等</button>}
        {activeView === 'titles' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingTitle(null); setShowTitleModal(true); }}><Plus size={13} />新增職稱</button>}
        {activeView === 'approvals' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingAuth(null); setShowAuthModal(true); }}><Plus size={13} />新增核決</button>}
      </div>

      {loading ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: 40 }}><Loader2 size={24} className="spin-icon" style={{ color: 'var(--slate-500)' }} /></div>
      ) : (
        <>
          {activeView === 'departments' && (
            <div className="glass-card" style={{ padding: 0, overflow: 'hidden' }}>
              {renderDeptTree(departmentTree)}
              {departmentTree.length === 0 && <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>尚無部門資料</div>}
            </div>
          )}

          {activeView === 'grades' && (
            <div className="glass-card" style={{ overflow: 'hidden' }}>
              <table className="data-table">
                <thead><tr><th>職等</th><th>名稱</th><th>Level</th><th>薪資區間</th><th>加班倍率</th><th>差旅上限</th><th>核決上限</th><th>人數</th><th>操作</th></tr></thead>
                <tbody>
                  {grades.filter(g => !search || g.gradeCode.includes(search) || g.gradeName.includes(search)).map(g => (
                    <tr key={g.id}>
                      <td className="mono" style={{ color: 'var(--cobalt-300)', fontWeight: 700 }}>{g.gradeCode}</td>
                      <td style={{ fontWeight: 500 }}>{g.gradeName}</td>
                      <td className="mono" style={{ color: 'var(--slate-400)' }}>L{g.level}</td>
                      <td className="mono" style={{ color: 'var(--slate-300)' }}>{g.minSalary.toLocaleString()} ~ {g.maxSalary.toLocaleString()}</td>
                      <td className="mono" style={{ color: 'var(--amber-300)' }}>{g.overtimeRate}x</td>
                      <td className="mono" style={{ color: 'var(--emerald-300)' }}>{g.travelLimit.toLocaleString()}</td>
                      <td className="mono" style={{ color: g.approvalLimit > 0 ? 'var(--emerald-300)' : 'var(--slate-600)' }}>
                        {g.approvalLimit > 0 ? g.approvalLimit.toLocaleString() : '無'}
                      </td>
                      <td><span style={{ padding: '2px 8px', background: 'var(--slate-700)', borderRadius: 4, fontSize: 12 }}>{g.employeeCount ?? 0}</span></td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="icon-action-btn" onClick={() => { setEditingGrade(g); setShowGradeModal(true); }}><Edit2 size={13} /></button>
                          <button className="icon-action-btn" style={{ color: 'var(--crimson-400)' }} onClick={() => handleDelete('job-grades', g.id)}><Trash2 size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {activeView === 'titles' && (
            filteredTitles.length === 0 ? (
              <div className="glass-card role-empty-state">
                {search ? `找不到符合「${search}」的職稱。` : '尚無職稱資料，請新增第一個職稱。'}
              </div>
            ) : (
              <div className="role-card-grid job-title-card-grid">
                {filteredTitles.map(title => {
                  const TitleIcon = title.isSupervisor ? Shield : UserCheck;
                  const departmentLabel = applicableDeptTypeOptions.find(option => option.value === title.applicableDeptType)?.label
                    ?? title.applicableDeptType
                    ?? '全部部門';
                  return (
                    <article className="role-card glass-card" key={title.id}>
                      <div className="role-card-heading">
                        <div className="role-card-icon"><TitleIcon size={22} /></div>
                        <div className="role-card-title">
                          <div className="role-card-badges">
                            <span className="role-kind-badge system">{title.titleCode}</span>
                            <span className={`role-kind-badge${title.isSupervisor ? ' system' : ''}`}>
                              {title.isSupervisor ? '主管職' : '一般職稱'}
                            </span>
                          </div>
                          <h3>{title.titleName}</h3>
                        </div>
                      </div>
                      <p className="role-card-description">
                        {title.description || (title.isSupervisor
                          ? '主管職稱，可用於員工任用與主管職務辨識。'
                          : '可供員工資料選用，並可設定預設職等與適用部門。')}
                      </p>
                      <div className="job-title-card-meta">
                        <div className="job-title-meta-item">
                          <span>預設職等</span>
                          <strong>{title.gradeCode || '尚未指定'}</strong>
                        </div>
                        <div className="job-title-meta-item">
                          <span>適用範圍</span>
                          <strong>{departmentLabel}</strong>
                        </div>
                      </div>
                      <div className="role-card-footer">
                        <span className="role-permission-count"><Users size={14} /> {title.employeeCount ?? 0} 位員工</span>
                        <div className="role-card-actions">
                          <button className="btn btn-ghost btn-sm" onClick={() => { setEditingTitle(title); setShowTitleModal(true); }}>
                            <Edit2 size={14} /> 編輯
                          </button>
                          <button
                            className="icon-action-btn"
                            onClick={() => handleDelete('job-titles', title.id)}
                            title="刪除職稱"
                            aria-label={`刪除職稱 ${title.titleName}`}
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </div>
                    </article>
                  );
                })}
              </div>
            )
          )}

          {activeView === 'approvals' && (
            <div className="glass-card" style={{ overflow: 'hidden' }}>
              <table className="data-table">
                <thead><tr><th>文件類型</th><th>審核層級</th><th>職等</th><th>金額區間</th><th>說明</th><th>操作</th></tr></thead>
                <tbody>
                  {authorities.filter(a => !search || a.docType.includes(search) || a.docTypeName.includes(search)).map(a => (
                    <tr key={a.id}>
                      <td><span style={{ padding: '4px 10px', background: 'rgba(45,110,245,0.15)', color: 'var(--cobalt-300)', borderRadius: 4, fontWeight: 600 }}>{a.docTypeName}</span></td>
                      <td className="mono" style={{ color: 'var(--amber-300)' }}>Level {a.approvalLevel}</td>
                      <td><span style={{ padding: '2px 8px', background: 'var(--slate-700)', borderRadius: 4 }}>{a.gradeCode} - {a.gradeName}</span></td>
                      <td className="mono" style={{ color: 'var(--slate-300)' }}>{a.minAmount.toLocaleString()} ~ {a.maxAmount >= 99999999 ? '無上限' : a.maxAmount.toLocaleString()}</td>
                      <td style={{ color: 'var(--slate-500)', fontSize: 12 }}>{a.description || '—'}</td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="icon-action-btn" onClick={() => { setEditingAuth(a); setShowAuthModal(true); }}><Edit2 size={13} /></button>
                          <button className="icon-action-btn" style={{ color: 'var(--crimson-400)' }} onClick={() => handleDelete('approvals/authorities', a.id)}><Trash2 size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      <FormModal open={showDeptModal} title={editingDept ? '編輯部門' : '新增部門'} fields={deptFields}
        initialValues={editingDept ? { ...editingDept, isCostCenter: editingDept.isCostCenter } : { isCostCenter: true, isActive: true, displayOrder: 0 }}
        onClose={() => { setShowDeptModal(false); setEditingDept(null); }}
        onSubmit={data => handleSave('departments', data, editingDept?.id)}
      />

      <FormModal open={showGradeModal} title={editingGrade ? '編輯職等' : '新增職等'} fields={gradeFields}
        initialValues={editingGrade ? { ...editingGrade } : { level: 1, minSalary: 28000, maxSalary: 35000, overtimeRate: 1.33, travelLimit: 5000, approvalLimit: 10000 }}
        onClose={() => { setShowGradeModal(false); setEditingGrade(null); }}
        onSubmit={async data => {
          if (Number(data.minSalary) > Number(data.maxSalary)) {
            alert('最低薪資不得大於最高薪資');
            return;
          }
          await handleSave('job-grades', data, editingGrade?.id);
        }}
      />

      <FormModal open={showTitleModal} title={editingTitle ? '編輯職稱' : '新增職稱'} fields={titleFields}
        initialValues={editingTitle ? { ...editingTitle } : { isSupervisor: false, applicableDeptType: '' }}
        onClose={() => { setShowTitleModal(false); setEditingTitle(null); }}
        onSubmit={data => handleSave('job-titles', data, editingTitle?.id)}
      />

      <FormModal open={showAuthModal} title={editingAuth ? '編輯核決權限' : '新增核決權限'} fields={authFields}
        initialValues={editingAuth ? { ...editingAuth } : { approvalLevel: 1, minAmount: 0, maxAmount: 10000 }}
        onClose={() => { setShowAuthModal(false); setEditingAuth(null); }}
        onSubmit={async data => {
          if (Number(data.minAmount) >= Number(data.maxAmount)) {
            alert('金額下限必須小於金額上限');
            return;
          }
          await handleSave('approvals/authorities', data, editingAuth?.id);
        }}
      />

      <style>{`
        .rd-tab { display: flex; align-items: center; gap: 6px; padding: 8px 16px; font-size: 13px; font-weight: 600; border: none; background: transparent; color: var(--slate-500); border-radius: var(--radius-md); cursor: pointer; transition: all var(--transition-fast); }
        .rd-tab:hover { color: var(--slate-300); background: rgba(45, 110, 245, 0.06); }
        .rd-tab.active { color: var(--cobalt-300); background: rgba(45, 110, 245, 0.1); }
        .filter-search { display: flex; align-items: center; gap: 8px; min-width: 220px; padding: 6px 12px; background: var(--slate-800); border: 1px solid var(--slate-700); border-radius: 6px; }
        .search-input { background: transparent; border: none; outline: none; color: var(--slate-200); font-family: var(--font-body); font-size: 13px; flex: 1; }
        .search-input::placeholder { color: var(--slate-600); }
        @keyframes spin { to { transform: rotate(360deg); } }
        .spin-icon { animation: spin 1s linear infinite; }
      `}</style>
    </div>
  );
};
