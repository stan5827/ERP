import React, { useState, useEffect, useMemo } from 'react';
import { Search, Plus, Download, Eye, Edit2, Loader2, CheckCircle, Clock, X, FolderOpen, TrendingUp, Users, Building2, DollarSign, ChevronDown, ChevronRight } from 'lucide-react';
import { useBudgetYears, useBudgetVersions, useExpenseBudgets, useLaborBudgets, useCapexBudgets } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { budgetApi, departmentsApi } from '../api/api';
import { useQuery, useQueryClient } from '@tanstack/react-query';

const statusConfig: Record<string, { label: string; cls: string }> = {
  draft: { label: '編制中', cls: 'neutral' },
  pending: { label: '審核中', cls: 'warning' },
  approved: { label: '已核准', cls: 'active' },
  closed: { label: '已結案', cls: 'neutral' },
};

const budgetYearFields: FormField[] = [
  { name: 'year', label: '預算年度', type: 'number', required: true, placeholder: '2026', min: 2020, max: 2030, description: '建立該年度的預算框架。' },
];

const editBudgetYearFields: FormField[] = [
  { name: 'year', label: '預算年度', type: 'number', required: true, min: 2020, max: 2030 },
  { 
    name: 'status', 
    label: '狀態', 
    type: 'select', 
    options: [
      { label: '編制中', value: 'draft' },
      { label: '審核中', value: 'pending' },
      { label: '已核准', value: 'approved' },
      { label: '已結案', value: 'closed' }
    ] 
  },
];

const versionFields: FormField[] = [
  { name: 'versionNumber', label: '版本號', type: 'text', required: true, placeholder: 'V1.0' },
  { name: 'versionName', label: '版本名稱', type: 'text', placeholder: '初版預算' },
  { name: 'isActive', label: '設為生效版本', type: 'checkbox', placeholder: '啟用此版本' },
  { name: 'notes', label: '備註', type: 'textarea' },
];

const expenseBudgetFields: FormField[] = [
  { name: 'year', label: '預算年度', type: 'number', required: true, placeholder: '2026' },
  { name: 'departmentId', label: '部門', type: 'select', required: true, options: [] },
  { name: 'accountCode', label: '科目代碼', type: 'text' },
  { name: 'accountName', label: '科目名稱', type: 'text' },
  { name: 'annualBudget', label: '年度預算', type: 'number', required: true },
  { name: 'priorYearActual', label: '去年同期實際', type: 'number' },
  { name: 'controlLevel', label: '控制等級', type: 'select', options: [{ label: '警告', value: 'warning' }, { label: '嚴格', value: 'strict' }, { label: '彈性', value: 'flexible' }] },
];

const laborBudgetFields: FormField[] = [
  { name: 'year', label: '預算年度', type: 'number', required: true, placeholder: '2026' },
  { name: 'departmentId', label: '部門', type: 'select', required: true, options: [] },
  { name: 'currentHeadcount', label: '現有人數', type: 'number', required: true },
  { name: 'currentSalaryTotal', label: '現有薪資總額', type: 'number', required: true },
  { name: 'positionTitle', label: '職稱', type: 'text' },
  { name: 'jobGrade', label: '職等', type: 'text' },
  { name: 'additionalHeadcount', label: '預計增聘人數', type: 'number' },
  { name: 'plannedHireMonth', label: '預計聘用月份', type: 'number', min: 1, max: 12 },
  { name: 'estimatedStartingSalary', label: '預估起薪', type: 'number' },
  { name: 'justification', label: '增聘理由', type: 'textarea' },
];

const capexBudgetFields: FormField[] = [
  { name: 'projectName', label: '專案名稱', type: 'text', required: true },
  { name: 'plannedPurchaseDate', label: '預計購買日期', type: 'date' },
  { name: 'estimatedAmount', label: '預估金額', type: 'number', required: true },
  { name: 'usefulLife', label: '使用年限', type: 'number' },
  { name: 'fundingSource', label: '資金來源', type: 'text' },
  { name: 'estimatedMonthlyDepreciation', label: '每月折舊', type: 'number' },
  { name: 'plannedCommissionDate', label: '預計啟用日期', type: 'date' },
  { name: 'roi', label: '預估投資報酬率 (%)', type: 'number' },
  { name: 'paybackPeriodYears', label: '回收年限', type: 'number' },
];

export function Budget() {
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [showAddYear, setShowAddYear] = useState(false);
  const [showAddVersion, setShowAddVersion] = useState(false);
  const [showAddExpense, setShowAddExpense] = useState(false);
  const [showAddLabor, setShowAddLabor] = useState(false);
  const [showAddCapex, setShowAddCapex] = useState(false);
  const [showEditYear, setShowEditYear] = useState(false);
  const [editingYear, setEditingYear] = useState<any>(null);
  const [selectedYear, setSelectedYear] = useState<any>(null);
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);
  
  const queryClient = useQueryClient();
  const { data: budgetResp, isLoading, isError, error, refetch: refetchYears } = useBudgetYears();
  
  // 終極診斷邏輯：找出資料到底在哪裡
  const years = useMemo(() => {
    if (!budgetResp) return [];
    
    // 1. 取得原始陣列 (相容包裹在 Data/data 或直接是陣列的情況)
    const rawData = (budgetResp as any).data || (budgetResp as any).Data || 
                    (Array.isArray(budgetResp) ? budgetResp : []);
    
    if (!Array.isArray(rawData)) {
      console.warn('[Budget] 資料格式非陣列:', rawData);
      return [];
    }

    // 2. 映射屬性並標準化
    return rawData.map(item => {
      const i = item as any;
      return {
        ...i,
        id: i.id || i.Id || i.ID,
        year: i.year || i.Year || i.YEAR,
        status: i.status || i.Status || 'draft'
      };
    });
  }, [budgetResp]);

  useEffect(() => {
    if (years.length > 0) {
      console.log('[Budget] 成功解析年度預算:', years);
    }
  }, [years]);

  const { data: versionsResp, isLoading: versionsLoading, refetch: refetchVersions } = useBudgetVersions(selectedYear?.id || '');
  
  useEffect(() => {
    if (selectedYear?.id) {
      refetchVersions();
    }
  }, [selectedYear?.id, refetchVersions]);

  const versionsData = (versionsResp as any);
  const versions = versionsData?.data || versionsData?.Data || [];

  // 自動選取最新的一個版本
  useEffect(() => {
    if (versions.length > 0 && !selectedVersionId) {
      setSelectedVersionId(versions[0].id || versions[0].Id);
    }
  }, [versions, selectedVersionId]);

  const stats = [
    { label: '預算年度', value: years.length, color: 'var(--cobalt-400)' },
    { label: '編制中', value: years.filter(y => (y.status || y.Status) === 'draft').length, color: 'var(--slate-400)' },
    { label: '已核准', value: years.filter(y => (y.status || y.Status) === 'approved').length, color: 'var(--emerald-400)' },
    { label: '審核中', value: years.filter(y => (y.status || y.Status) === 'pending').length, color: 'var(--amber-400)' },
  ];

  const filtered = useMemo(() => {
    if (!years || !Array.isArray(years)) return [];
    
    // 偵錯日誌：確認資料來源
    console.log('[Budget] 原始預算年度資料:', years);
    
    return years
      .filter(y => {
        // 確保屬性名稱正確 (相容 year 或 Year)
        const yYear = y.year || y.Year;
        const yStatus = y.status || y.Status;
        
        const matchesStatus = filterStatus === 'all' || yStatus === filterStatus;
        const matchesSearch = !search || String(yYear).includes(search);
        return matchesStatus && matchesSearch;
      })
      .sort((a, b) => (b.year || b.Year) - (a.year || a.Year)); // 強制由新到舊排序
  }, [years, filterStatus, search]);

  const handleYearClick = (year: { id: string; year: string }) => {
    setSelectedYear(year);
    setSelectedVersionId(null);
  };

  const toggleVersion = (versionId: string) => {
    if (selectedVersionId === versionId) {
      setSelectedVersionId(null);
    } else {
      setSelectedVersionId(versionId);
    }
  };

  // 抓取部門清單
  const { data: deptsResp } = useQuery({ 
    queryKey: ['departments', 'list'], 
    queryFn: () => departmentsApi.list().then(r => r.data) 
  });
  
  const departmentOptions = useMemo(() => {
    const rawDepts = (deptsResp as any)?.data || (deptsResp as any)?.Data || (Array.isArray(deptsResp) ? deptsResp : []);
    return rawDepts.map((d: any) => ({
      label: `${d.deptCode || d.DeptCode} ${d.deptName || d.DeptName}`,
      value: d.id || d.Id
    }));
  }, [deptsResp]);

  // 動態欄位定義：將部門清單注入
  const dynamicExpenseFields = useMemo(() => {
    return expenseBudgetFields.map(f => {
      if (f.name === 'departmentId') return { ...f, options: departmentOptions };
      if (f.name === 'year' && selectedYear) return { ...f, initialValue: selectedYear.year };
      return f;
    });
  }, [departmentOptions, selectedYear]);

  const dynamicLaborFields = useMemo(() => {
    return laborBudgetFields.map(f => {
      if (f.name === 'departmentId') return { ...f, options: departmentOptions };
      if (f.name === 'year' && selectedYear) return { ...f, initialValue: selectedYear.year };
      return f;
    });
  }, [departmentOptions, selectedYear]);

  const handleCreateYearSuccess = async () => {
    setShowAddYear(false);
    await queryClient.invalidateQueries({ queryKey: ['budget'] });
    alert('年度預算建立成功！');
  };

  const handleUpdateYearSuccess = async () => {
    setShowEditYear(false);
    setEditingYear(null);
    await queryClient.invalidateQueries({ queryKey: ['budget'] });
    alert('年度預算更新成功！');
  };

  const handleEditClick = (e: React.MouseEvent, year: any) => {
    e.stopPropagation();
    setEditingYear(year);
    setShowEditYear(true);
  };

  const handleCreateVersionSuccess = async () => {
    setShowAddVersion(false);
    await queryClient.invalidateQueries({ queryKey: ['budget', 'versions', selectedYear?.id] });
    refetchVersions();
    alert('版本建立成功！');
  };

  const handleCreateExpenseSuccess = async () => {
    setShowAddExpense(false);
    if (selectedVersionId) {
      await queryClient.invalidateQueries({ queryKey: ['budget', 'expense-budgets', selectedVersionId] });
    }
    alert('費用預算建立成功！');
  };

  const handleCreateLaborSuccess = async () => {
    setShowAddLabor(false);
    if (selectedVersionId) {
      await queryClient.invalidateQueries({ queryKey: ['budget', 'labor-budgets', selectedVersionId] });
    }
    alert('人事預算建立成功！');
  };

  const handleCreateCapexSuccess = async () => {
    setShowAddCapex(false);
    if (selectedVersionId) {
      await queryClient.invalidateQueries({ queryKey: ['budget', 'capex-budgets', selectedVersionId] });
    }
    alert('資本支出預算建立成功！');
  };

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">預算規劃</h2>
          <p className="module-page-desc">預算編列 → 審核 → 核准 → 執行 → 考核</p>
        </div>
        <div className="module-page-actions">
          {isError && <span style={{ color: 'var(--red-400)', fontSize: '12px', marginRight: '8px' }}>載入失敗: {(error as any)?.message}</span>}
          <span style={{ fontSize: '11px', color: 'var(--slate-500)', border: '1px solid var(--slate-800)', padding: '2px 6px', borderRadius: '4px', marginRight: '8px' }}>
            系統抓到 {years.length} 筆年度資料
          </span>
          <button className="btn btn-ghost btn-sm" onClick={() => {
            console.log('手動觸發刷新...');
            refetchYears();
          }}>
            <Loader2 size={13} className={isLoading ? "spin" : ""} /> 刷新
          </button>
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
          <button className="btn btn-primary btn-sm" onClick={() => setShowAddYear(true)}><Plus size={13} />新增年度預算</button>
        </div>
      </div>

      <div className="module-stat-grid">
        {stats.map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div className="module-stat-label">{s.label}</div>
            <div className="module-stat-value kpi-value" style={{ color: s.color, fontSize: '20px' }}>
              {s.value}
            </div>
          </div>
        ))}
      </div>

      <div className="glass-card module-filter-bar">
        <div className="filter-search">
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input
            className="search-input"
            placeholder="搜尋年度..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <div className="filter-tabs">
          {(['all', 'draft', 'pending', 'approved', 'closed'] as const).map(s => (
            <button
              key={s}
              className={`tab-item ${filterStatus === s ? 'active' : ''}`}
              onClick={() => setFilterStatus(s)}
            >
              {s === 'all' ? '全部' : statusConfig[s]?.label}
            </button>
          ))}
        </div>
      </div>

      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <table className="data-table">
          <thead>
            <tr>
              <th>預算年度</th>
              <th>狀態</th>
              <th>總預算</th>
              <th>已執行</th>
              <th>執行率</th>
              <th>部門數</th>
              <th>核准日期</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
              </td></tr>
            ) : filtered.length === 0 ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>暫無資料</td></tr>
            ) : filtered.map(year => {
              // 相容後端可能回傳 Data 或 data 的情況
              const y = (year as any);
              const yId = y.id || y.Id;
              const yYear = y.year || y.Year;
              const yStatus = y.status || y.Status;
              const yTotalBudget = y.totalBudget || y.TotalBudget;
              const yUsedAmount = y.usedAmount || y.UsedAmount;
              const yDepartments = y.departments || y.Departments;
              const yApprovedDate = y.approvedDate || y.ApprovedDate;

              const sc = statusConfig[yStatus] ?? { label: yStatus || '編制中', cls: 'info' };
              const executionRate = yTotalBudget && yUsedAmount
                ? ((yUsedAmount / yTotalBudget) * 100).toFixed(1) + '%'
                : '-';
              const isSelected = selectedYear?.id === yId;
              
              return (
                <tr key={yId} style={{ background: isSelected ? 'var(--navy-800)' : undefined }}>
                  <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '13px', fontWeight: 600 }}>{yYear}</td>
                  <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                  <td className="mono" style={{ color: 'var(--emerald-300)', fontWeight: 600 }}>
                    {yTotalBudget?.toLocaleString('zh-TW')}
                  </td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>
                    {yUsedAmount?.toLocaleString('zh-TW') ?? '-'}
                  </td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--amber-300)' }}>{executionRate}</td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>{yDepartments ?? '-'}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>
                    {yStatus === 'approved' && yApprovedDate ? (
                      <><CheckCircle size={11} style={{ display: 'inline', marginRight: 4, color: 'var(--emerald-400)' }} />{yApprovedDate}</>
                    ) : yStatus === 'pending' ? (
                      <><Clock size={11} style={{ display: 'inline', marginRight: 4, color: 'var(--amber-400)' }} />審核中</>
                    ) : yStatus === 'draft' ? (
                      <span style={{ color: 'var(--cobalt-400)' }}>編制中...</span>
                    ) : '-'}
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button 
                        className={`btn ${isSelected ? 'btn-primary' : 'btn-ghost'} btn-xs`}
                        onClick={() => handleYearClick({ id: yId, year: yYear })}
                      >
                        {isSelected ? <CheckCircle size={12} /> : <TrendingUp size={12} />}
                        {isSelected ? '編輯中' : '預算編列'}
                      </button>
                      <button className="icon-action-btn" title="修改年度設定" onClick={(e) => handleEditClick(e, y)}><Edit2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {selectedYear && (
        <div className="glass-card" style={{ marginTop: 16 }}>
          <div className="finance-section-header">
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <FolderOpen size={16} style={{ color: 'var(--cobalt-400)' }} />
              <h3 className="finance-section-title">{selectedYear.year} 年度預算版本</h3>
              <span className="finance-section-count">{versions.length} 版本</span>
            </div>
            <button className="btn btn-primary btn-sm" style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 6 }} onClick={() => setShowAddVersion(true)}>
              <Plus size={14} /> 新增版本
            </button>
          </div>
          {versionsLoading ? (
            <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>
              <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
            </div>
          ) : versions.length === 0 ? (
            <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>尚無預算版本，請先新增版本</div>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th style={{ width: 40 }}></th>
                  <th>版本號</th>
                  <th>版本名稱</th>
                  <th>狀態</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {versions.map(v => (
                  <tr key={v.id}>
                    <td>
                      <button className="icon-action-btn" onClick={() => toggleVersion(v.id)}>
                        {selectedVersionId === v.id ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
                      </button>
                    </td>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontWeight: 600 }}>{v.versionNumber}</td>
                    <td>{v.versionName || '-'}</td>
                    <td><span className={`status-badge status-${v.isActive ? 'active' : 'neutral'}`}>{v.isActive ? '生效中' : '草稿'}</span></td>
                    <td>
                      <button className="icon-action-btn" title="展開" onClick={() => toggleVersion(v.id)}>
                        <FolderOpen size={13} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          
          {selectedVersionId && (
            <VersionDetails 
              versionId={selectedVersionId} 
              onAddExpense={() => setShowAddExpense(true)} 
              onAddLabor={() => setShowAddLabor(true)} 
              onAddCapex={() => setShowAddCapex(true)} 
            />
          )}
        </div>
      )}

      <style>{`
        .module-page { display: flex; flex-direction: column; gap: 16px; }
        .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; }
        .module-page-title { font-size: 20px; font-weight: 700; letter-spacing: -0.02em; color: var(--slate-100); }
        .module-page-desc { font-size: 12px; color: var(--slate-500); margin-top: 2px; font-family: var(--font-mono); }
        .module-page-actions { display: flex; gap: 8px; }
        .module-stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
        .module-stat-card { padding: 14px 16px; }
        .module-stat-label { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.07em; color: var(--slate-500); margin-bottom: 8px; font-family: var(--font-display); }
        .module-stat-value { font-size: 24px; font-weight: 700; letter-spacing: -0.04em; }
        .module-filter-bar { display: flex; align-items: center; gap: 12px; padding: 10px 14px; }
        .filter-search { display: flex; align-items: center; gap: 8px; min-width: 220px; }
        .filter-tabs { display: flex; gap: 2px; background: var(--navy-850); padding: 3px; border-radius: var(--radius-md); flex: 1; }
        .tab-item { padding: 5px 12px; font-size: 12px; border-radius: 6px; }
        .icon-action-btn { width: 26px; height: 26px; display: flex; align-items: center; justify-content: center; background: transparent; border: none; border-radius: var(--radius-sm); color: var(--slate-500); cursor: pointer; transition: all var(--transition-fast); }
        .icon-action-btn:hover { background: rgba(45, 110, 245, 0.08); color: var(--cobalt-300); }
        .finance-section-header { padding: 14px 16px; display: flex; align-items: center; gap: 8px; }
        .finance-section-title { font-size: 14px; font-weight: 700; color: var(--slate-200); margin: 0; }
        .finance-section-count { font-size: 11px; color: var(--slate-500); font-family: var(--font-mono); background: var(--navy-850); padding: 2px 8px; border-radius: var(--radius-sm); }
        .budget-type-btns { display: flex; gap: 8px; padding: 12px 16px; border-top: 1px solid var(--navy-700); }
        .budget-type-btn { display: flex; align-items: center; gap: 6px; padding: 8px 12px; font-size: 12px; border-radius: var(--radius-sm); border: 1px solid var(--navy-600); background: var(--navy-850); color: var(--slate-300); cursor: pointer; }
        .budget-type-btn:hover { border-color: var(--cobalt-500); color: var(--cobalt-300); }
      `}</style>

      <FormModal
        open={showAddYear}
        onClose={() => setShowAddYear(false)}
        title="新增年度預算"
        fields={budgetYearFields}
        onSubmit={async (data) => {
          try {
            const year = Number(data.year);
            if (year < 2020 || year > 2030) {
              alert('預算年度必須介於 2020 到 2030 之間');
              return;
            }
            await budgetApi.createYear(data);
            handleCreateYearSuccess();
          } catch (err: any) {
            const msg = err.response?.data?.message || err.message || String(err);
            alert('建立失敗：' + msg);
          }
        }}
      />

      <FormModal
        open={showEditYear}
        onClose={() => {
          setShowEditYear(false);
          setEditingYear(null);
        }}
        title="編輯年度預算"
        fields={editBudgetYearFields}
        initialValues={editingYear}
        onSubmit={async (data) => {
          try {
            await budgetApi.updateYear(editingYear.id, data);
            handleUpdateYearSuccess();
          } catch (err: any) {
            alert('更新失敗：' + (err.message || '未知錯誤'));
          }
        }}
      />

      <FormModal
        open={showAddVersion}
        onClose={() => setShowAddVersion(false)}
        title="新增預算版本"
        fields={versionFields}
        onSubmit={async (data) => {
          try {
            if (!selectedYear) {
              alert('請先選擇年度');
              return;
            }
            console.log('建立版本，資料:', { ...data, budgetYearId: selectedYear.id });
            await budgetApi.createVersion({ ...data, budgetYearId: selectedYear.id });
            await handleCreateVersionSuccess();
          } catch (err: any) {
            alert('建立失敗：' + (err.message || String(err)));
          }
        }}
      />

      <FormModal
        open={showAddExpense}
        onClose={() => setShowAddExpense(false)}
        title="新增費用預算"
        fields={dynamicExpenseFields}
        onSubmit={async (data) => {
          try {
            if (!selectedVersionId) {
              alert('請先展開一個版本');
              return;
            }
            await budgetApi.createExpenseBudget({
              budgetVersionId: selectedVersionId,
              year: Number(data.year),
              departmentId: data.departmentId,
              items: [{
                accountCode: data.accountCode,
                accountName: data.accountName,
                annualBudget: Number(data.annualBudget),
                priorYearActual: Number(data.priorYearActual) || 0,
                controlLevel: data.controlLevel,
                month01: Number(data.annualBudget) / 12,
                month02: Number(data.annualBudget) / 12,
                month03: Number(data.annualBudget) / 12,
                month04: Number(data.annualBudget) / 12,
                month05: Number(data.annualBudget) / 12,
                month06: Number(data.annualBudget) / 12,
                month07: Number(data.annualBudget) / 12,
                month08: Number(data.annualBudget) / 12,
                month09: Number(data.annualBudget) / 12,
                month10: Number(data.annualBudget) / 12,
                month11: Number(data.annualBudget) / 12,
                month12: Number(data.annualBudget) / 12,
              }]
            });
            await handleCreateExpenseSuccess();
          } catch (err: any) {
            alert('建立失敗：' + (err.message || String(err)));
          }
        }}
      />

      <FormModal
        open={showAddLabor}
        onClose={() => setShowAddLabor(false)}
        title="新增人事預算"
        fields={dynamicLaborFields}
        onSubmit={async (data) => {
          try {
            if (!selectedVersionId) {
              alert('請先展開一個版本');
              return;
            }
            await budgetApi.createLaborBudget({
              budgetVersionId: selectedVersionId,
              year: Number(data.year),
              departmentId: data.departmentId,
              currentHeadcount: Number(data.currentHeadcount),
              currentSalaryTotal: Number(data.currentSalaryTotal),
              items: [{
                positionTitle: data.positionTitle,
                jobGrade: data.jobGrade,
                additionalHeadcount: Number(data.additionalHeadcount) || 0,
                plannedHireMonth: data.plannedHireMonth ? Number(data.plannedHireMonth) : null,
                estimatedStartingSalary: Number(data.estimatedStartingSalary) || 0,
                estimatedOvertimeBudget: 0,
                estimatedInsuranceBudget: 0,
                recruitmentBudget: 0,
                justification: data.justification,
              }]
            });
            await handleCreateLaborSuccess();
          } catch (err: any) {
            alert('建立失敗：' + (err.message || String(err)));
          }
        }}
      />

      <FormModal
        open={showAddCapex}
        onClose={() => setShowAddCapex(false)}
        title="新增資本支出預算"
        fields={capexBudgetFields}
        onSubmit={async (data) => {
          try {
            if (!selectedVersionId) {
              alert('請先展開一個版本');
              return;
            }
            await budgetApi.createCapexBudget({
              budgetVersionId: selectedVersionId,
              projectName: data.projectName,
              plannedPurchaseDate: data.plannedPurchaseDate,
              estimatedAmount: Number(data.estimatedAmount),
              usefulLife: Number(data.usefulLife) || 0,
              fundingSource: data.fundingSource,
              estimatedMonthlyDepreciation: Number(data.estimatedMonthlyDepreciation) || 0,
              plannedCommissionDate: data.plannedCommissionDate,
              roi: data.roi ? Number(data.roi) : null,
              paybackPeriodYears: data.paybackPeriodYears ? Number(data.paybackPeriodYears) : null,
            });
            await handleCreateCapexSuccess();
          } catch (err: any) {
            alert('建立失敗：' + (err.message || String(err)));
          }
        }}
      />
    </div>
  );
}

function VersionDetails({ versionId, onAddExpense, onAddLabor, onAddCapex }: { versionId: string; onAddExpense: () => void; onAddLabor: () => void; onAddCapex: () => void }) {
  const { data: expenseResp } = useExpenseBudgets(versionId);
  const { data: laborResp } = useLaborBudgets(versionId);
  const { data: capexResp } = useCapexBudgets(versionId);

  const expenses = (expenseResp as { data?: any[] })?.data || [];
  const labors = (laborResp as { data?: any[] })?.data || [];
  const capex = (capexResp as { data?: any[] })?.data || [];

  return (
    <div style={{ padding: 16, background: 'var(--navy-850)', borderRadius: '0 0 var(--radius-md) var(--radius-md)' }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 16 }}>
        <div className="glass-card" style={{ padding: 12 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
            <DollarSign size={14} style={{ color: 'var(--emerald-400)' }} />
            <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-300)' }}>費用預算</span>
          </div>
          <div style={{ fontSize: 20, fontWeight: 700, color: 'var(--emerald-300)' }}>
            {expenses.length} 筆
          </div>
        </div>
        <div className="glass-card" style={{ padding: 12 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
            <Users size={14} style={{ color: 'var(--cobalt-400)' }} />
            <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-300)' }}>人事預算</span>
          </div>
          <div style={{ fontSize: 20, fontWeight: 700, color: 'var(--cobalt-300)' }}>
            {labors.length} 筆
          </div>
        </div>
        <div className="glass-card" style={{ padding: 12 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
            <TrendingUp size={14} style={{ color: 'var(--amber-400)' }} />
            <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-300)' }}>資本支出</span>
          </div>
          <div style={{ fontSize: 20, fontWeight: 700, color: 'var(--amber-300)' }}>
            {capex.length} 筆
          </div>
        </div>
      </div>

      <div className="budget-type-btns">
        <button className="budget-type-btn" onClick={onAddExpense}>
          <Plus size={12} /> 新增費用預算
        </button>
        <button className="budget-type-btn" onClick={onAddLabor}>
          <Plus size={12} /> 新增人事預算
        </button>
        <button className="budget-type-btn" onClick={onAddCapex}>
          <Plus size={12} /> 新增資本支出
        </button>
      </div>
    </div>
  );
}
