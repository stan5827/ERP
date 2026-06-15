import React, { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Search, Download, Leaf, Zap, Factory, Lightbulb, AlertTriangle, TrendingDown, TrendingUp, Plus, Target, Truck, FileText, Loader2, CheckCircle } from 'lucide-react';
import { useEmissionRecords, useEmissionSummary, useAiInsights, useEmissionSources, useEmissionFactors, useReductionTargets, useSupplierSustainability, useEnergyConsumption, useEsgReports } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { esgApi } from '../api/api';

const scopeLabels: Record<string, { label: string; icon: React.ReactNode; color: string }> = {
  scope1: { label: 'Scope 1 直接排放', icon: <Factory size={16} />, color: 'var(--crimson-400)' },
  scope2: { label: 'Scope 2 間接排放 (能源)', icon: <Zap size={16} />, color: 'var(--amber-400)' },
  scope3: { label: 'Scope 3 其他間接排放', icon: <Leaf size={16} />, color: 'var(--emerald-400)' },
};

const insightIcons: Record<string, React.ReactNode> = {
  anomaly: <AlertTriangle size={14} style={{ color: 'var(--amber-400)' }} />,
  reduction: <TrendingDown size={14} style={{ color: 'var(--emerald-400)' }} />,
  increase: <TrendingUp size={14} style={{ color: 'var(--crimson-400)' }} />,
  default: <Lightbulb size={14} style={{ color: 'var(--cobalt-400)' }} />,
};

const ratingConfig: Record<string, { label: string; color: string }> = {
  A: { label: 'A 級 - 優', color: 'var(--emerald-400)' },
  B: { label: 'B 級 - 良', color: 'var(--cobalt-400)' },
  C: { label: 'C 級 - 待改善', color: 'var(--amber-400)' },
};

const emissionRecordFields: FormField[] = [
  { name: 'sourceId', label: '排放源', type: 'select', required: true, description: '選擇排放活動的來源設備或類別，將決定使用的排放因子。' },
  { name: 'period', label: '申報期間', type: 'select', required: true, options: [
    { value: '2026-Q1', label: '2026年第一季' },
    { value: '2025-Q4', label: '2025年第四季' },
    { value: '2025-ANNUAL', label: '2025年度' },
  ], description: '排放數據所屬的統計期間，須與財務報表期間對齊。' },
  { name: 'periodStart', label: '期間起日', type: 'date', required: true, description: '統計期間的開始日期，用於排放量歸屬。' },
  { name: 'periodEnd', label: '期間訖日', type: 'date', required: true, description: '統計期間的結束日期。' },
  { name: 'activityData', label: '活動數據', type: 'number', required: true, description: '能源消耗或活動量，如：用電度數( kWh)、燃油公升數( L)、煤炭噸數( tonne)等。' },
  { name: 'activityUnit', label: '活動單位', type: 'select', required: true, options: [
    { value: 'kWh', label: 'kWh 度' },
    { value: 'L', label: '公升' },
    { value: 'kg', label: '公斤' },
    { value: 'tonne', label: '公噸' },
    { value: 'km', label: '公里' },
    { value: 'MWh', label: 'MWh' },
  ], description: '活動數據的度量單位，須與排放因子單位匹配。' },
  { name: 'remarks', label: '備註', type: 'textarea', description: '補充說明，如：數據來源、異常原因或計算假設。' },
];

const emissionSourceFields: FormField[] = [
  { name: 'sourceCode', label: '排放源代碼', type: 'text', required: true, placeholder: '如 SCOPE1-001', description: '排放源的唯一識別碼，建議依範疇編碼以利統計。' },
  { name: 'sourceName', label: '排放源名稱', type: 'text', required: true, placeholder: '如 工廠鍋爐燃煤', description: '排放源的完整名稱，便於營運人員識別与管理。' },
  { name: 'scope', label: '排放範疇', type: 'select', required: true, options: [
    { value: 'scope1', label: 'Scope 1 - 直接排放' },
    { value: 'scope2', label: 'Scope 2 - 間接排放(能源)' },
    { value: 'scope3', label: 'Scope 3 - 其他間接排放' },
  ], description: '依據 GHG Protocol 分類的排放範疇，決定後續計算與申報方式。' },
  { name: 'category', label: '類別', type: 'select', required: true, options: [
    { value: '固定燃燒源', label: '固定燃燒源' },
    { value: '移動源', label: '移動源' },
    { value: '外逸排放', label: '外逸排放' },
    { value: '外購能源', label: '外購能源' },
    { value: '運輸配送', label: '運輸配送' },
    { value: '員工差旅', label: '員工差旅' },
    { value: '廢棄物處理', label: '廢棄物處理' },
  ], description: '排放源的細部分類，有助於識別減排熱點。' },
  { name: 'description', label: '說明', type: 'textarea', description: '描述排放源的具體位置、設備型號或操作方式。' },
];

export function ESG() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'emissions' | 'energy' | 'targets' | 'suppliers' | 'reports'>('emissions');
  const [showAddRecord, setShowAddRecord] = useState(false);
  const [showAddSource, setShowAddSource] = useState(false);
  
  const { data: summaryResp, isLoading: summaryLoading } = useEmissionSummary();
  const { data: recordsResp, isLoading: recordsLoading } = useEmissionRecords();
  const { data: sourcesResp } = useEmissionSources();
  const { data: factorsResp } = useEmissionFactors();
  const { data: targetsResp } = useReductionTargets();
  const { data: suppliersResp } = useSupplierSustainability();
  const { data: energyResp } = useEnergyConsumption();
  const { data: reportsResp } = useEsgReports();
  const { data: insightsResp, isLoading: insightsLoading } = useAiInsights();
  
  const summaryData = summaryResp?.data ?? {} as any;
  const recordsData = recordsResp?.data ?? [];
  const sourcesData = sourcesResp?.data ?? [];
  const factorsData = factorsResp?.data ?? [];
  const targetsData = targetsResp?.data ?? [];
  const suppliersData = suppliersResp?.data ?? [];
  const energyData = energyResp?.data ?? [];
  const reportsData = reportsResp?.data ?? [];
  const insightsData = insightsResp?.data ?? [];
  
  const totalEmissions = (summaryData.scope1 ?? 0) + (summaryData.scope2 ?? 0) + (summaryData.scope3 ?? 0);
  
  const filtered = recordsData.filter((r: any) =>
    !search || (r.sourceName ?? '').includes(search) || (r.period ?? '').includes(search)
  );

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">ESG 永續發展與碳排放管理</h2>
          <p className="module-page-desc">碳盤查 · 排放追蹤 · 供應商永續 · AI 洞察 · ISO 14064 合規</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm" onClick={() => setShowAddSource(true)}><Plus size={13} />新增排放源</button>
          <button className="btn btn-ghost btn-sm" onClick={() => setShowAddRecord(true)}><Plus size={13} />新增排放紀錄</button>
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出報告</button>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="glass-card module-filter-bar" style={{ gap: 8 }}>
        <button className={`tab-item ${activeTab === 'emissions' ? 'active' : ''}`} onClick={() => setActiveTab('emissions')}>
          <Leaf size={12} style={{ marginRight: 4 }} />碳排放
        </button>
        <button className={`tab-item ${activeTab === 'energy' ? 'active' : ''}`} onClick={() => setActiveTab('energy')}>
          <Zap size={12} style={{ marginRight: 4 }} />能源消耗
        </button>
        <button className={`tab-item ${activeTab === 'targets' ? 'active' : ''}`} onClick={() => setActiveTab('targets')}>
          <Target size={12} style={{ marginRight: 4 }} />減排目標
        </button>
        <button className={`tab-item ${activeTab === 'suppliers' ? 'active' : ''}`} onClick={() => setActiveTab('suppliers')}>
          <Truck size={12} style={{ marginRight: 4 }} />供應商永續
        </button>
        <button className={`tab-item ${activeTab === 'reports' ? 'active' : ''}`} onClick={() => setActiveTab('reports')}>
          <FileText size={12} style={{ marginRight: 4 }} />ESG報告
        </button>
        <div style={{ flex: 1 }} />
        <div className="filter-search" style={{ width: 200 }}>
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      {/* Emission Tab */}
      {activeTab === 'emissions' && (
        <>
          <div className="module-stat-grid">
            {summaryLoading ? (
              Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="glass-card module-stat-card" style={{ minHeight: 80 }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                    <Loader2 size={18} className="spin" style={{ animation: 'spin 1s linear infinite', color: 'var(--slate-500)' }} />
                  </div>
                </div>
              ))
            ) : (
              <>
                {Object.entries(scopeLabels).map(([key, meta]) => (
                  <div key={key} className="glass-card module-stat-card">
                    <div className="module-stat-label" style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                      {meta.icon}
                      {meta.label}
                    </div>
                    <div className="module-stat-value kpi-value" style={{ color: meta.color, fontSize: '20px' }}>
                      {summaryData[key as keyof typeof summaryData] != null ? `${(summaryData[key as keyof typeof summaryData] as number).toLocaleString('zh-TW')} tCO₂e` : '—'}
                    </div>
                  </div>
                ))}
                <div className="glass-card module-stat-card">
                  <div className="module-stat-label">總排放量合計</div>
                  <div className="module-stat-value kpi-value" style={{ color: 'var(--slate-100)', fontSize: '20px' }}>
                    {totalEmissions > 0 ? `${totalEmissions.toLocaleString('zh-TW')} tCO₂e` : '—'}
                  </div>
                </div>
              </>
            )}
          </div>

          <div className="glass-card" style={{ overflow: 'hidden' }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>排放源</th>
                  <th>範疇</th>
                  <th>期間</th>
                  <th>活動數據</th>
                  <th>排放因子</th>
                  <th>排放量 (tCO₂e)</th>
                  <th>狀態</th>
                </tr>
              </thead>
              <tbody>
                {recordsLoading ? (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                    <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
                  </td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>無排放紀錄</td></tr>
                ) : filtered.map(r => (
                  <tr key={r.id}>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{r.sourceName ?? '—'}</td>
                    <td>
                      <span className={`status-badge status-${r.scope === 'scope1' ? 'danger' : r.scope === 'scope2' ? 'warning' : 'info'}`}>
                        {r.scope === 'scope1' ? 'Scope 1' : r.scope === 'scope2' ? 'Scope 2' : r.scope === 'scope3' ? 'Scope 3' : '—'}
                      </span>
                    </td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{r.period}</td>
                    <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>{r.activityData?.toLocaleString('zh-TW')} {r.activityUnit}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{r.emissionFactor ?? '—'}</td>
                    <td className="mono" style={{ color: 'var(--amber-300)', fontWeight: 600 }}>{r.calculatedEmissions?.toLocaleString('zh-TW')}</td>
                    <td>
                      <span className={`status-badge status-${r.status === 'verified' ? 'active' : 'warning'}`}>
                        {r.status === 'verified' ? '已驗證' : r.status === 'draft' ? '草稿' : r.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {/* Energy Tab */}
      {activeTab === 'energy' && (
        <div className="glass-card" style={{ overflow: 'hidden' }}>
          <div style={{ padding: '14px 16px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-200)', display: 'flex', alignItems: 'center', gap: 6 }}>
              <Zap size={16} style={{ color: 'var(--amber-400)' }} />能源消耗與碳排放
            </h3>
          </div>
          <table className="data-table">
            <thead>
              <tr>
                <th>能源類型</th>
                <th>範疇</th>
                <th>消耗量</th>
                <th>碳排放因子</th>
                <th>碳排放量 (tCO₂e)</th>
                <th>期間</th>
              </tr>
            </thead>
            <tbody>
              {energyData.length === 0 ? (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>無能源消耗資料</td></tr>
              ) : energyData.map((e: any) => (
                <tr key={e.id}>
                  <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{e.energyType}</td>
                  <td>
                    <span className={`status-badge status-${e.scope === 'scope1' ? 'danger' : 'warning'}`}>
                      {e.scope === 'scope1' ? 'Scope 1' : 'Scope 2'}
                    </span>
                  </td>
                  <td className="mono" style={{ color: 'var(--slate-300)' }}>{e.consumption.toLocaleString('zh-TW')} {e.unit}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{e.carbonFactor}</td>
                  <td className="mono" style={{ color: 'var(--amber-300)', fontWeight: 600 }}>{e.calculatedEmissions.toLocaleString('zh-TW')}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{e.period}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Reduction Targets Tab */}
      {activeTab === 'targets' && (
        <div style={{ display: 'grid', gap: 12 }}>
          {targetsData.length === 0 ? (
            <div className="glass-card" style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無減排目標設定</div>
          ) : targetsData.map((t: any) => (
            <div key={t.id} className="glass-card" style={{ padding: 20 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 }}>
                <div>
                  <h3 style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-100)', marginBottom: 4 }}>{t.targetName}</h3>
                  <p style={{ fontSize: 12, color: 'var(--slate-500)' }}>{t.description}</p>
                </div>
                <span className={`status-badge status-${t.status === 'active' ? 'active' : 'neutral'}`}>
                  {t.status === 'active' ? '進行中' : t.status}
                </span>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16, marginBottom: 16 }}>
                <div style={{ background: 'var(--navy-850)', padding: 12, borderRadius: 8 }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>基準年排放</div>
                  <div style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-200)' }}>{t.baselineEmissions.toLocaleString()} tCO₂e</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{t.baseYear} 年</div>
                </div>
                <div style={{ background: 'var(--navy-850)', padding: 12, borderRadius: 8 }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>目標排放</div>
                  <div style={{ fontSize: 16, fontWeight: 600, color: 'var(--emerald-400)' }}>{t.targetEmissions.toLocaleString()} tCO₂e</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{t.targetYear} 年 - 減{t.targetReductionPercent}%</div>
                </div>
                <div style={{ background: 'var(--navy-850)', padding: 12, borderRadius: 8 }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>目前排放</div>
                  <div style={{ fontSize: 16, fontWeight: 600, color: 'var(--amber-400)' }}>{t.currentEmissions.toLocaleString()} tCO₂e</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>2026年</div>
                </div>
                <div style={{ background: 'var(--navy-850)', padding: 12, borderRadius: 8 }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>達成率</div>
                  <div style={{ fontSize: 20, fontWeight: 700, color: t.achievedReductionPercent >= (t.targetReductionPercent * 0.5) ? 'var(--emerald-400)' : 'var(--amber-400)' }}>
                    {t.achievedReductionPercent.toFixed(1)}%
                  </div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>優於時程</div>
                </div>
              </div>
              <div style={{ background: 'var(--navy-900)', borderRadius: 8, height: 12, overflow: 'hidden' }}>
                <div style={{ 
                  width: `${Math.min(t.achievedReductionPercent / t.targetReductionPercent * 100, 100)}%`, 
                  height: '100%', 
                  background: t.achievedReductionPercent >= (t.targetReductionPercent * 0.5) 
                    ? 'linear-gradient(90deg, var(--emerald-500), var(--emerald-400))' 
                    : 'linear-gradient(90deg, var(--amber-500), var(--amber-400))',
                  borderRadius: 8,
                  transition: 'width 0.5s ease'
                }} />
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Suppliers Tab */}
      {activeTab === 'suppliers' && (
        <div className="glass-card" style={{ overflow: 'hidden' }}>
          <div style={{ padding: '14px 16px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-200)', display: 'flex', alignItems: 'center', gap: 6 }}>
              <Truck size={16} style={{ color: 'var(--emerald-400)' }} />供應商永續評估
            </h3>
          </div>
          <table className="data-table">
            <thead>
              <tr>
                <th>供應商</th>
                <th>碳足跡</th>
                <th>再生能源%</th>
                <th>永續評分</th>
                <th>評級</th>
                <th>ISO認證</th>
                <th>下次評估</th>
              </tr>
            </thead>
            <tbody>
              {suppliersData.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>無供應商永續資料</td></tr>
              ) : suppliersData.map((s: any) => {
                const rating = ratingConfig[s.rating] ?? { label: s.rating, color: 'var(--slate-500)' };
                return (
                  <tr key={s.id}>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{s.supplierName}</td>
                    <td className="mono" style={{ color: 'var(--amber-300)' }}>{s.carbonFootprint.toLocaleString()} {s.carbonUnit}</td>
                    <td className="mono" style={{ color: s.renewableEnergyPercent >= 50 ? 'var(--emerald-400)' : 'var(--slate-400)' }}>
                      {s.renewableEnergyPercent}%
                    </td>
                    <td className="mono" style={{ fontWeight: 600 }}>{s.sustainabilityScore}</td>
                    <td>
                      <span style={{ 
                        display: 'inline-block', 
                        padding: '2px 10px', 
                        borderRadius: 12, 
                        background: rating.color + '20', 
                        color: rating.color, 
                        fontWeight: 700,
                        fontSize: 12
                      }}>
                        {rating.label}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {s.hasIso14001 && <span className="status-badge status-active">14001</span>}
                        {s.hasIso14064 && <span className="status-badge status-info">14064</span>}
                      </div>
                    </td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>
                      {s.nextAssessmentDate ? new Date(s.nextAssessmentDate).toLocaleDateString('zh-TW') : '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Reports Tab */}
      {activeTab === 'reports' && (
        <div className="glass-card" style={{ overflow: 'hidden' }}>
          <div style={{ padding: '14px 16px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-200)', display: 'flex', alignItems: 'center', gap: 6 }}>
              <FileText size={16} style={{ color: 'var(--cobalt-400)' }} />ESG 報告
            </h3>
          </div>
          <table className="data-table">
            <thead>
              <tr>
                <th>報告編號</th>
                <th>報告名稱</th>
                <th>類型</th>
                <th>框架</th>
                <th>總排放量</th>
                <th>碳強度</th>
                <th>狀態</th>
                <th>驗證</th>
              </tr>
            </thead>
            <tbody>
              {reportsData.length === 0 ? (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無ESG報告</td></tr>
              ) : reportsData.map((r: any) => (
                <tr key={r.id}>
                  <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{r.reportCode}</td>
                  <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{r.reportName}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{r.reportType}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-400)' }}>{r.framework}</td>
                  <td className="mono" style={{ color: 'var(--amber-300)', fontWeight: 600 }}>{r.totalEmissions.toLocaleString()} tCO₂e</td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>{r.emissionIntensity} {r.intensityUnit}</td>
                  <td>
                    <span className={`status-badge status-${r.status === 'published' ? 'active' : r.status === 'draft' ? 'neutral' : 'warning'}`}>
                      {r.status === 'published' ? '已發布' : r.status === 'draft' ? '草稿' : r.status}
                    </span>
                  </td>
                  <td>
                    {r.isVerified ? (
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4, color: 'var(--emerald-400)', fontSize: 11 }}>
                        <CheckCircle size={12} /> {r.verifier}
                      </span>
                    ) : (
                      <span style={{ color: 'var(--slate-500)', fontSize: 11 }}>未驗證</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* AI Insights - Always visible */}
      <div className="glass-card" style={{ padding: '16px' }}>
        <div style={{ fontSize: '13px', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'var(--slate-500)', marginBottom: 12, fontFamily: 'var(--font-display)' }}>
          AI 洞察與減排建議
        </div>
        {insightsLoading ? (
          <div style={{ textAlign: 'center', padding: '24px 0', color: 'var(--slate-500)' }}>
            <Loader2 size={18} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />分析中...
          </div>
        ) : insightsData.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '24px 0', color: 'var(--slate-500)', fontSize: 12 }}>暫無 AI 洞察</div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))', gap: 12 }}>
            {insightsData.map((insight: any, i: number) => {
              const icon = insightIcons[insight.type] ?? insightIcons.default;
              return (
                <div key={insight.id ?? i} style={{ display: 'flex', gap: 10, padding: '14px 16px', background: 'rgba(255,255,255,0.02)', borderRadius: 'var(--radius-md)', border: '1px solid rgba(255,255,255,0.04)' }}>
                  <div style={{ marginTop: 2 }}>{icon}</div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)', marginBottom: 4 }}>
                      {insight.title}
                    </div>
                    <div style={{ fontSize: 12, color: 'var(--slate-400)', lineHeight: 1.6, marginBottom: insight.recommendation ? 8 : 0 }}>
                      {insight.description}
                    </div>
                    {insight.recommendation && (
                      <div style={{ fontSize: 11, color: 'var(--cobalt-300)', padding: '8px 10px', background: 'rgba(45,110,245,0.08)', borderRadius: 6 }}>
                        💡 {insight.recommendation}
                      </div>
                    )}
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 10 }}>
                      {insight.severity && (
                        <span className={`status-badge status-${insight.severity === 'high' || insight.severity === 'warning' ? 'warning' : insight.severity === 'critical' ? 'danger' : 'info'}`}>
                          {insight.severity === 'high' ? '高' : insight.severity === 'warning' ? '注意' : insight.severity === 'critical' ? '嚴重' : '資訊'}
                        </span>
                      )}
                      {insight.potentialReduction != null && (
                        <span style={{ fontSize: 11, color: 'var(--emerald-400)', fontWeight: 600 }}>
                          預估減排: {insight.potentialReduction} tCO₂e/年
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Form Modals */}
      <FormModal
        open={showAddRecord}
        title="新增排放紀錄"
        width={600}
        fields={emissionRecordFields}
        onClose={() => setShowAddRecord(false)}
        onSubmit={async (data) => {
          await esgApi.createEmissionRecord(data);
          queryClient.invalidateQueries({ queryKey: ['emission-records'] });
          setShowAddRecord(false);
        }}
      />

      <FormModal
        open={showAddSource}
        title="新增排放源"
        width={550}
        fields={emissionSourceFields}
        onClose={() => setShowAddSource(false)}
        onSubmit={async (data) => {
          await esgApi.createEmissionSource(data);
          queryClient.invalidateQueries({ queryKey: ['emission-sources'] });
          setShowAddSource(false);
        }}
      />

      <style>{`
        .module-page { display: flex; flex-direction: column; gap: 16px; }
        .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; }
        .module-page-title { font-size: 20px; font-weight: 700; letter-spacing: -0.02em; color: var(--slate-100); }
        .module-page-desc { font-size: 12px; color: var(--slate-500); margin-top: 2px; font-family: var(--font-mono); }
        .module-page-actions { display: flex; gap: 8px; }
        .module-stat-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 12px; }
        .module-stat-card { padding: 14px 16px; }
        .module-stat-label { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.07em; color: var(--slate-500); margin-bottom: 8px; font-family: var(--font-display); }
        .module-stat-value { font-size: 24px; font-weight: 700; letter-spacing: -0.04em; }
        .module-filter-bar { display: flex; align-items: center; gap: 12px; padding: 10px 14px; }
        .filter-search { display: flex; align-items: center; gap: 8px; min-width: 220px; }
        .spin { animation: spin 1s linear infinite; }
        @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
        .tab-item { padding: 6px 14px; font-size: 12px; border-radius: 6px; display: flex; align-items: center; }
      `}</style>
    </div>
  );
}
