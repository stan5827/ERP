import React, { useState, useEffect } from 'react';
import { Search, Plus, Edit2, Trash2, ChevronRight, ChevronDown, Building2, Users, Shield, Settings, Loader2, X, ArrowRight, UserCheck } from 'lucide-react';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { useQueryClient } from '@tanstack/react-query';
import { departmentsApi, jobTitlesApi, jobGradesApi, approvalsApi } from '../api/api';

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

export const OrganizationManagement: React.FC = () => {
  const [activeView, setActiveView] = useState<'departments' | 'grades' | 'titles' | 'approvals'>('departments');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [departments, setDepartments] = useState<any[]>([]);
  const [departmentTree, setDepartmentTree] = useState<any[]>([]);
  const [grades, setGrades] = useState<any[]>([]);
  const [titles, setTitles] = useState<any[]>([]);
  const [authorities, setAuthorities] = useState<any[]>([]);
  const [expandedDepts, setExpandedDepts] = useState<Set<string>>(new Set());
  const [showInactiveDepartments, setShowInactiveDepartments] = useState(false);
  const queryClient = useQueryClient();

  const [showDeptModal, setShowDeptModal] = useState(false);
  const [showGradeModal, setShowGradeModal] = useState(false);
  const [showTitleModal, setShowTitleModal] = useState(false);
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [editingDept, setEditingDept] = useState<any>(null);
  const [editingGrade, setEditingGrade] = useState<any>(null);
  const [editingTitle, setEditingTitle] = useState<any>(null);
  const [editingAuth, setEditingAuth] = useState<any>(null);
  const [hasInactiveDepartments, setHasInactiveDepartments] = useState(false);

  useEffect(() => { loadData(); }, [activeView, showInactiveDepartments]);

  const loadData = async () => {
    setLoading(true);
    try {
      if (activeView === 'departments') {
        if (showInactiveDepartments) {
          const [flatRes, treeRes] = await Promise.all([
            departmentsApi.flat(true),
            departmentsApi.tree(true),
          ]);
          setDepartments(flatRes.data?.data ?? []);
          setDepartmentTree(treeRes.data?.data ?? []);
          setHasInactiveDepartments(false);
        } else {
          const [flatRes, treeRes, flatAllRes] = await Promise.all([
            departmentsApi.flat(false),
            departmentsApi.tree(false),
            departmentsApi.flat(true),
          ]);
          const activeDepartments = flatRes.data?.data ?? [];
          const allDepartments = flatAllRes.data?.data ?? [];
          setDepartments(activeDepartments);
          setDepartmentTree(treeRes.data?.data ?? []);
          setHasInactiveDepartments(allDepartments.length > activeDepartments.length);
        }
      } else if (activeView === 'grades') {
        const res = await jobGradesApi.list();
        setGrades(res.data?.data ?? []);
      } else if (activeView === 'titles') {
        const [titlesRes, gradesRes] = await Promise.all([
          jobTitlesApi.list(),
          jobGradesApi.list(),
        ]);
        setTitles(titlesRes.data?.data ?? []);
        setGrades(gradesRes.data?.data ?? []);
      } else if (activeView === 'approvals') {
        const [authRes, gradesRes] = await Promise.all([
          approvalsApi.authorities(),
          jobGradesApi.list(),
        ]);
        setAuthorities(authRes.data?.data ?? []);
        setGrades(gradesRes.data?.data ?? []);
      }
    } catch (err) { console.error('Failed to load:', err); }
    finally { setLoading(false); }
  };

  const toggleDept = (id: string) => {
    const newSet = new Set(expandedDepts);
    newSet.has(id) ? newSet.delete(id) : newSet.add(id);
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
    { name: 'isActive', label: '啟用狀態', type: 'checkbox', placeholder: '啟用', defaultValue: true },
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
    { name: 'applicableDeptType', label: '適用部門類型', type: 'select', options: [
      { value: '', label: '全部' },
      { value: 'production', label: '生產部門' },
      { value: 'admin', label: '管理部門' },
      { value: 'service', label: '服務部門' },
    ], defaultValue: '' },
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

  const handleSave = async (type: string, data: any, id?: string) => {
    const cleanedData: Record<string, unknown> = {};
    for (const key in data) {
      let value = data[key];
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
    } catch (err: any) {
      console.error('Save error:', err);
      alert('儲存失敗: ' + (err.response?.data?.message || err.message));
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
    } catch (err: any) {
      alert('刪除失敗: ' + (err.response?.data?.message || err.message));
    }
  };

  const renderDeptTree = (nodes: any[], level = 0) => {
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
            {node.employeeCount > 0 && (
              <span style={{ fontSize: 10, padding: '2px 6px', background: 'var(--slate-700)', color: 'var(--slate-400)', borderRadius: 3 }}>
                {node.employeeCount} 人
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
          <button key={tab.key} className={`rd-tab ${activeView === tab.key ? 'active' : ''}`} onClick={() => setActiveView(tab.key as any)}>
            <tab.icon size={14} />{tab.label}
          </button>
        ))}
      </div>

      <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
        <div className="filter-search" style={{ flex: 1, maxWidth: 300 }}>
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        {activeView === 'departments' && (
          <>
            <button className="btn btn-ghost btn-sm" onClick={() => setShowInactiveDepartments(prev => !prev)}>
              {showInactiveDepartments ? '隱藏停用部門' : '顯示停用部門'}
            </button>
            <button className="btn btn-primary btn-sm" onClick={() => { setEditingDept(null); setShowDeptModal(true); }}><Plus size={13} />新增部門</button>
          </>
        )}
        {activeView === 'grades' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingGrade(null); setShowGradeModal(true); }}><Plus size={13} />新增職等</button>}
        {activeView === 'titles' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingTitle(null); setShowTitleModal(true); }}><Plus size={13} />新增職稱</button>}
        {activeView === 'approvals' && <button className="btn btn-primary btn-sm" onClick={() => { setEditingAuth(null); setShowAuthModal(true); }}><Plus size={13} />新增核決</button>}
      </div>
      {activeView === 'departments' && hasInactiveDepartments && !showInactiveDepartments && !loading && (
        <div className="glass-card" style={{ marginTop: 12, borderLeft: '4px solid var(--amber-400)', background: 'rgba(250, 211, 51, 0.08)', color: 'var(--slate-100)', padding: '12px 16px' }}>
          <strong>注意：</strong> 系統中存在停用部門。若你剛新增的部門沒有出現在架構中，請點選「顯示停用部門」檢查該部門是否為停用狀態。
        </div>
      )}

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
                      <td><span style={{ padding: '2px 8px', background: 'var(--slate-700)', borderRadius: 4, fontSize: 12 }}>{g.employeeCount}</span></td>
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
            <div className="glass-card" style={{ overflow: 'hidden' }}>
              <table className="data-table">
                <thead><tr><th>職稱代碼</th><th>職稱名稱</th><th>預設職等</th><th>適用部門</th><th>主管職</th><th>人數</th><th>操作</th></tr></thead>
                <tbody>
                  {titles.filter(t => !search || t.titleCode.includes(search) || t.titleName.includes(search)).map(t => (
                    <tr key={t.id}>
                      <td className="mono" style={{ color: 'var(--cobalt-300)', fontWeight: 700 }}>{t.titleCode}</td>
                      <td style={{ fontWeight: 500 }}>{t.titleName}</td>
                      <td>{t.gradeCode ? <span style={{ padding: '2px 8px', background: 'var(--slate-700)', borderRadius: 4, fontSize: 12 }}>{t.gradeCode}</span> : <span style={{ color: 'var(--slate-500)' }}>—</span>}</td>
                      <td style={{ color: 'var(--slate-400)', fontSize: 12 }}>{t.applicableDeptType || '全部'}</td>
                      <td><span style={{ fontSize: 11, padding: '2px 8px', background: t.isSupervisor ? 'rgba(16,185,129,0.15)' : 'var(--slate-700)', color: t.isSupervisor ? 'var(--emerald-400)' : 'var(--slate-500)', borderRadius: 4 }}>{t.isSupervisor ? '是' : '否'}</span></td>
                      <td><span style={{ padding: '2px 8px', background: 'var(--slate-700)', borderRadius: 4, fontSize: 12 }}>{t.employeeCount}</span></td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="icon-action-btn" onClick={() => { setEditingTitle(t); setShowTitleModal(true); }}><Edit2 size={13} /></button>
                          <button className="icon-action-btn" style={{ color: 'var(--crimson-400)' }} onClick={() => handleDelete('job-titles', t.id)}><Trash2 size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
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
        initialValues={editingGrade || { level: 1, minSalary: 28000, maxSalary: 35000, overtimeRate: 1.33, travelLimit: 5000, approvalLimit: 10000 }}
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
        initialValues={editingTitle || { isSupervisor: false, applicableDeptType: '' }}
        onClose={() => { setShowTitleModal(false); setEditingTitle(null); }}
        onSubmit={data => handleSave('job-titles', data, editingTitle?.id)}
      />

      <FormModal open={showAuthModal} title={editingAuth ? '編輯核決權限' : '新增核決權限'} fields={authFields}
        initialValues={editingAuth || { approvalLevel: 1, minAmount: 0, maxAmount: 10000 }}
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
