import { useState } from 'react';
import { Search, Plus, Download, Eye, Edit2, Loader2, Landmark } from 'lucide-react';
import { useAssets, useAssetCategories } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { assetsApi } from '../api/api';
import type { AssetDto } from '../api/api';

function getAccumulatedDepreciation(asset: AssetDto): number {
  if (!('depreciations' in asset) || !Array.isArray(asset.depreciations))
    return 0;

  const records: unknown[] = asset.depreciations;
  const latestRecord = records.reduce<{
    period: string;
    accumulatedDepreciation: number;
  } | null>((latest, record) => {
    if (typeof record !== 'object' || record === null
      || !('period' in record) || typeof record.period !== 'string'
      || !('accumulatedDepreciation' in record) || typeof record.accumulatedDepreciation !== 'number')
      return latest;

    return !latest || record.period > latest.period
      ? { period: record.period, accumulatedDepreciation: record.accumulatedDepreciation }
      : latest;
  }, null);

  return latestRecord?.accumulatedDepreciation ?? 0;
}

const statusConfig: Record<string, { label: string; cls: string }> = {
  active: { label: '使用中', cls: 'active' },
  idle: { label: '閒置', cls: 'neutral' },
  maintenance: { label: '維護中', cls: 'warning' },
  disposed: { label: '已報廢', cls: 'neutral' },
};

const assetFields: FormField[] = [
  { name: 'assetNumber', label: '資產編號', type: 'text', required: true, placeholder: '如 ASSET-001', description: '固定資產的唯一識別碼' },
  { name: 'assetName', label: '資產名稱', type: 'text', required: true, placeholder: '資產名稱', description: '固定資產的完整名稱，如：CNC 加工中心機、辦公室隔間、空調設備等。' },
  { name: 'specification', label: '規格型號', type: 'text', placeholder: '選填', description: '資產的型號、規格或料號，有助於後續維護與採購參考。' },
  { name: 'categoryName', label: '資產類別', type: 'text', placeholder: '選填', description: '資產所屬的類別' },
  { name: 'locationName', label: '存放位置', type: 'text', placeholder: '選填', description: '資產的存放位置或使用部門' },
  { name: 'acquisitionCost', label: '取得成本', type: 'number', required: true, placeholder: '0', min: 0, description: '資產的取得成本（含稅），作為折舊計算與資產帳面價值的基準。' },
  { name: 'acquisitionDate', label: '取得日期', type: 'date', description: '資產開始使用的日期，決定折舊提列的起始期間。' },
  { name: 'usefulLife', label: '耐用年限', type: 'number', placeholder: '5', min: 1, description: '資產預估可使用的年數，用於計算每月折舊金額（平均法）。' },
];

export function Assets() {
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [showAdd, setShowAdd] = useState(false);
  const { data: assetsResp, isLoading } = useAssets();
  const { data: categoriesResp, isLoading: catLoading } = useAssetCategories();

  const assets = assetsResp?.data ?? [];
  const categories = categoriesResp?.data ?? [];

  const stats = [
    { label: '資產總數', value: assets.length, color: 'var(--cobalt-400)' },
    { label: '使用中', value: assets.filter(a => a.status === 'active').length, color: 'var(--emerald-400)' },
    { label: '待維護', value: assets.filter(a => a.status === 'maintenance').length, color: 'var(--amber-400)' },
    { label: '資產類別', value: categories.length, color: 'var(--violet-400)' },
  ];

    const filtered = assets.filter(a =>
      (filterStatus === 'all' || a.status === filterStatus) &&
      (a.assetNumber?.includes(search) || a.assetName?.includes(search) || a.categoryName?.includes(search))
    );

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">固定資產管理</h2>
          <p className="module-page-desc">資產登記 → 折舊提列 → 維護保養 → 報廢處分</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
          <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={13} />新增資產</button>
        </div>
      </div>

      <div className="module-stat-grid">
        {stats.map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div className="module-stat-label">{s.label}</div>
            <div className="module-stat-value kpi-value" style={{ color: s.color, fontSize: '20px' }}>
              {catLoading && s.label === '資產類別' ? <Loader2 size={16} className="spin" /> : s.value}
            </div>
          </div>
        ))}
      </div>

      {categories.length > 0 && (
        <div className="glass-card" style={{ padding: 16 }}>
          <div style={{ fontSize: 12, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: 'var(--slate-500)', marginBottom: 12, fontFamily: 'var(--font-display)' }}>
            資產類別統計
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 10 }}>
            {categories.map(cat => (
              <div key={cat.id} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', background: 'var(--navy-850)', borderRadius: 'var(--radius-md)' }}>
                <Landmark size={14} style={{ color: 'var(--cobalt-400)', flexShrink: 0 }} />
                <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-200)' }}>{cat.categoryName}</div>
              <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>
                {assets.filter(asset => asset.categoryName === cat.categoryName).length} 項
              </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="glass-card module-filter-bar">
        <div className="filter-search">
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input
            className="search-input"
            placeholder="搜尋資產編號、名稱、類別..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <div className="filter-tabs">
          {(['all', 'active', 'idle', 'maintenance', 'disposed'] as const).map(s => (
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
              <th>資產編號</th>
              <th>名稱</th>
              <th>類別</th>
              <th>取得日期</th>
              <th>取得成本</th>
              <th>累計折舊</th>
              <th>狀態</th>
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
            ) : filtered.map(asset => {
              const sc = statusConfig[asset.status] ?? { label: asset.status, cls: 'neutral' };
              return (
                <tr key={asset.id}>
                  <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{asset.assetNumber}</td>
               <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{asset.assetName}</td>
               <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{asset.categoryName}</td>
               <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{asset.acquisitionDate}</td>
               <td className="mono" style={{ color: 'var(--emerald-300)', fontWeight: 600 }}>
                 {asset.acquisitionCost?.toLocaleString('zh-TW')}
               </td>
                <td className="mono" style={{ fontSize: 12, color: 'var(--amber-300)' }}>
                   {getAccumulatedDepreciation(asset).toLocaleString('zh-TW')}
                 </td>
                  <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="icon-action-btn" title="查看"><Eye size={13} /></button>
                      <button className="icon-action-btn" title="編輯"><Edit2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

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
      `}</style>

      <FormModal
        open={showAdd}
        onClose={() => setShowAdd(false)}
        title="新增資產"
        fields={assetFields}
        onSubmit={async (data) => {
          if (!data.acquisitionDate) {
            alert('請填寫取得日期');
            return;
          }
          if (Number(data.acquisitionCost) <= 0) {
            alert('取得成本必須大於 0');
            return;
          }
          if (data.residualValue != null && data.acquisitionCost != null && Number(data.residualValue) >= Number(data.acquisitionCost)) {
            alert('殘值必須小於取得成本');
            return;
          }
          await assetsApi.create(data);
        }}
      />
    </div>
  );
}
