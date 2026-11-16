import React, { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Search, Plus, Download, Eye, Clock, Loader2 } from 'lucide-react';
import { useIqcReports, useSupplierComplaints, useSuppliers } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { qualityApi } from '../api/api';
import type { IqcInspectionDto } from '../api/system';
import { ProcessQuality } from './ProcessQuality';

const iqcResultConfig: Record<string, { label: string; cls: string }> = {
  pass: { label: '合格', cls: 'active' },
  fail: { label: '不合格', cls: 'danger' },
  conditional: { label: '條件通過', cls: 'warning' },
  pending: { label: '待檢', cls: 'neutral' },
};

const complaintStatusConfig: Record<string, { label: string; cls: string }> = {
  open: { label: '待處理', cls: 'neutral' },
  investigating: { label: '調查中', cls: 'info' },
  resolved: { label: '已解決', cls: 'active' },
  closed: { label: '已結案', cls: 'active' },
};

const priorityConfig: Record<string, { label: string; cls: string }> = {
  critical: { label: '嚴重', cls: 'danger' },
  high: { label: '高', cls: 'warning' },
  medium: { label: '中', cls: 'info' },
  low: { label: '低', cls: 'neutral' },
};

const buildComplaintFields = (suppliers: Array<{ id: string; supplierName: string }>): FormField[] => [
  { name: 'supplierId', label: '供應商', type: 'select', required: true, options: suppliers.map(s => ({ value: s.id, label: s.supplierName })), description: '發生品質問題的供應商對象，便於後續追溯與輔導。' },
  { name: 'defectType', label: '缺陷類型', type: 'text', placeholder: '如 外觀不良', description: '描述不良的類型，如：外觀不良、尺寸偏差、功能異常、標籤錯誤等。' },
  { name: 'defectQty', label: '不良數量', type: 'number', placeholder: '0', min: 0, description: '本次客訴涉及的不良品數量，作為後續索賠與改善效果的參考依據。' },
  { name: 'priority', label: '優先級', type: 'select', options: [{ value: 'high', label: '高' }, { value: 'medium', label: '中' }, { value: 'low', label: '低' }], description: '高優先級：影響生產或出貨；中：需限期改善；低：可列入例行追蹤。' },
  { name: 'description', label: '問題描述', type: 'textarea', placeholder: '問題描述...', description: '詳細描述發現問題的時間、數量、影響範圍及初步原因分析。' },
];

export function Quality() {
  const [search, setSearch] = useState('');
  const [lotSearch, setLotSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'iqc' | 'complaints' | 'trace' | 'process'>('iqc');
  const [showAdd, setShowAdd] = useState(false);
  const [selectedIqcId, setSelectedIqcId] = useState<string | null>(null);
  const { data: iqcResp, isLoading: iqcLoading } = useIqcReports();
  const { data: complaintResp, isLoading: complaintLoading } = useSupplierComplaints();
  const { data: suppliersResp } = useSuppliers();
  const suppliers = (suppliersResp as { data?: Array<{ id: string; supplierName: string }> })?.data ?? [];

  const iqcReports = iqcResp?.data ?? [];
  const complaints = complaintResp?.data ?? [];

  const filteredIqc = iqcReports.filter(r =>
    r.reportNumber.includes(search) || (r.supplierName ?? '').includes(search)
  );
  const filteredComplaints = complaints.filter(c =>
    c.complaintNumber.includes(search) ||
    (c.supplierName ?? '').includes(search) ||
    (c.skuName ?? '').includes(search)
  );

  const isLoading = activeTab === 'iqc' ? iqcLoading : complaintLoading;

  const stats = [
    { label: 'IQC 報告數', value: iqcReports.length, color: 'var(--cobalt-400)' },
    { label: 'IQC 不合格', value: iqcReports.filter(r => r.result === 'fail').length, color: 'var(--crimson-400)' },
    { label: '客訴待處理', value: complaints.filter(c => c.status === 'open').length, color: 'var(--amber-400)' },
    { label: '客訴總數', value: complaints.length, color: 'var(--emerald-400)' },
  ];

  if (activeTab === 'process') return <ProcessQuality onBack={() => setActiveTab('iqc')} />;

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">品質管理</h2>
          <p className="module-page-desc">IQC 進料檢驗 · 供應商客訴追蹤</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
          {activeTab === 'complaints' && (
            <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={13} />新增客訴</button>
          )}
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
            placeholder="搜尋編號、供應商..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <div className="filter-tabs">
          <button
            className={`tab-item ${activeTab === 'iqc' ? 'active' : ''}`}
            onClick={() => setActiveTab('iqc')}
          >
            IQC 進料檢驗
          </button>
          <button
            className={`tab-item ${activeTab === 'complaints' ? 'active' : ''}`}
            onClick={() => setActiveTab('complaints')}
          >
            供應商客訴
          </button>
          <button
            className={`tab-item ${activeTab === 'trace' ? 'active' : ''}`}
            onClick={() => setActiveTab('trace')}
          >
            Lot 追溯查詢
          </button>
          <button
            className="tab-item"
            onClick={() => setActiveTab('process')}
          >
            工藝檢驗
          </button>
        </div>
      </div>

      <div className="glass-card" style={{ overflow: 'hidden' }}>
        {activeTab === 'iqc' ? (
          <table className="data-table">
            <thead>
              <tr>
                <th>報告編號</th>
                <th>收貨單 / 料號</th>
                <th>批號 / 數量</th>
                <th>供應商</th>
                <th>檢驗日期</th>
                <th>結果</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                  <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
                </td></tr>
              ) : filteredIqc.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>目前沒有 IQC 檢驗批；需檢物料收貨後會自動建立待檢單。</td></tr>
              ) : filteredIqc.map(r => {
                const rc = iqcResultConfig[r.result] ?? { label: r.result, cls: 'neutral' };
                return (
                  <tr key={r.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{r.reportNumber}</td>
                    <td>
                      <div>{r.goodsReceiptNumber ?? '—'}</div>
                      <div style={{ color: 'var(--slate-400)', fontSize: 11 }}>{r.skuName ?? '—'}</div>
                    </td>
                    <td>{r.batchNumber || '—'} / {r.receivedQty}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{r.supplierName ?? '—'}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{r.inspectionDate}</td>
                    <td><span className={`status-badge status-${rc.cls}`}>{rc.label}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button className="btn btn-ghost btn-sm" onClick={() => setSelectedIqcId(r.id)}>
                          <Eye size={13} />{r.result === 'pending' ? '執行檢驗' : '查看記錄'}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>客訴編號</th>
                <th>供應商</th>
                <th>品項</th>
                <th>缺陷類型</th>
                <th>PPM</th>
                <th>優先度</th>
                <th>狀態</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                  <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
                </td></tr>
              ) : filteredComplaints.map(c => {
                const sc = complaintStatusConfig[c.status] ?? { label: c.status, cls: 'neutral' };
                const pc = priorityConfig[c.priority] ?? { label: c.priority, cls: 'neutral' };
                return (
                  <tr key={c.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{c.complaintNumber}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{c.supplierName ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{c.skuName ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{c.defectType ?? '—'}</td>
                    <td className="mono" style={{
                      color: c.ppm > 1000 ? 'var(--crimson-300)' : c.ppm > 500 ? 'var(--amber-300)' : 'var(--emerald-300)',
                      fontWeight: 600, fontSize: 12
                    }}>
                      {c.ppm.toLocaleString()}
                    </td>
                    <td><span className={`status-badge status-${pc.cls}`}>{pc.label}</span></td>
                    <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button className="icon-action-btn" title="查看"><Eye size={13} /></button>
                        {c.status === 'open' && (
                          <button className="icon-action-btn" title="開始調查" style={{ color: 'var(--cobalt-300)' }}>
                            <Clock size={13} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {activeTab === 'trace' && (
        <div className="glass-card">
          <div style={{ padding: '20px 24px', borderBottom: '1px solid var(--navy-800)' }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-100)', marginBottom: 16 }}>Lot 號碼追溯查詢</h3>
            <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end' }}>
              <div style={{ flex: 1, maxWidth: 300 }}>
                <label style={{ fontSize: 11, color: 'var(--slate-500)', display: 'block', marginBottom: 6 }}>輸入 Lot 號碼</label>
                <input
                  className="input"
                  placeholder="例如: LOT-20260401-001"
                  value={lotSearch}
                  onChange={e => setLotSearch(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && lotSearch && setActiveTab('trace')}
                />
              </div>
              <button className="btn btn-primary btn-sm">
                <Search size={13} />搜尋
              </button>
            </div>
            {lotSearch && (
              <div style={{ marginTop: 24 }}>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 20 }}>
                  <div className="stat-card">
                    <div className="stat-label">原料來源</div>
                    <div className="stat-value" style={{ color: 'var(--cobalt-300)' }}>供應商 A</div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-label">進料日期</div>
                    <div className="stat-value" style={{ color: 'var(--emerald-300)' }}>2026-04-01</div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-label">IQC 結果</div>
                    <div className="stat-value"><span className="status-badge status-active">合格</span></div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-label">投產工單</div>
                    <div className="stat-value" style={{ color: 'var(--cobalt-300)' }}>WO-2026-0042</div>
                  </div>
                </div>
                <div style={{ fontSize: 12, color: 'var(--slate-500)', marginBottom: 8 }}>追溯流程</div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                  {[
                    { label: '採購', status: 'completed' },
                    { label: '進貨', status: 'completed' },
                    { label: 'IQC', status: 'completed' },
                    { label: '入庫', status: 'completed' },
                    { label: '領料', status: 'completed' },
                    { label: '生產', status: 'in_progress' },
                    { label: 'IPQC', status: 'pending' },
                    { label: '出貨', status: 'pending' },
                  ].map((step, i) => (
                    <div key={step.label} style={{ display: 'flex', alignItems: 'center' }}>
                      <div style={{
                        padding: '6px 12px',
                        borderRadius: 6,
                        fontSize: 12,
                        fontWeight: 500,
                        background: step.status === 'completed' ? 'var(--emerald-900)' : step.status === 'in_progress' ? 'var(--cobalt-900)' : 'var(--navy-850)',
                        color: step.status === 'completed' ? 'var(--emerald-300)' : step.status === 'in_progress' ? 'var(--cobalt-300)' : 'var(--slate-500)',
                        border: step.status === 'completed' ? '1px solid var(--emerald-700)' : step.status === 'in_progress' ? '1px solid var(--cobalt-700)' : '1px solid var(--navy-800)'
                      }}>
                        {step.label}
                      </div>
                      {i < 7 && <div style={{ width: 24, height: 2, background: 'var(--navy-700)' }} />}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
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
        .stat-card { padding: 12px 16px; background: var(--navy-850); border-radius: var(--radius-md); border: 1px solid var(--navy-800); }
        .stat-label { font-size: 11px; color: var(--slate-500); margin-bottom: 4px; text-transform: uppercase; letter-spacing: 0.05em; }
        .stat-value { font-size: 14px; font-weight: 600; color: var(--slate-200); }
      `}</style>

      <FormModal
        open={showAdd}
        onClose={() => setShowAdd(false)}
        title="新增客訴報告"
        fields={buildComplaintFields(suppliers)}
        onSubmit={async (data) => {
          if (data.defectQty != null && Number(data.defectQty) <= 0) {
            alert('不良數量必須大於 0');
            return;
          }
          await qualityApi.createComplaint(data);
        }}
      />
      {selectedIqcId && (
        <IqcInspectionModal
          id={selectedIqcId}
          onClose={() => setSelectedIqcId(null)}
        />
      )}
    </div>
  );
}

function IqcInspectionModal({ id, onClose }: { id: string; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [result, setResult] = useState<'pass' | 'fail'>('pass');
  const [sampleSize, setSampleSize] = useState(1);
  const [acceptNumber, setAcceptNumber] = useState(0);
  const [rejectNumber, setRejectNumber] = useState(0);
  const [items, setItems] = useState([{
    inspectionItem: '',
    specification: '',
    lowerLimit: '',
    upperLimit: '',
    measuredValue: '',
    result: 'pass' as 'pass' | 'fail',
  }]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape')
        onClose();
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  const { data, isLoading, error } = useQuery({
    queryKey: ['iqc-inspection', id],
    queryFn: async () => {
      const response = await qualityApi.iqcInspection(id);
      if (!response.data?.success || !response.data.data) {
        throw new Error(response.data?.message || '載入 IQC 檢驗單失敗');
      }
      return response.data.data;
    },
  });
  const inspection: IqcInspectionDto | undefined = data;
  const completeMutation = useMutation({
    mutationFn: () => qualityApi.completeIqcInspection(id, {
      result,
      sampleSize,
      acceptNumber,
      rejectNumber,
      items: items.map(item => ({
        ...item,
        lowerLimit: item.lowerLimit === '' ? undefined : Number(item.lowerLimit),
        upperLimit: item.upperLimit === '' ? undefined : Number(item.upperLimit),
      })),
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['quality-iqc'] });
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] });
      queryClient.invalidateQueries({ queryKey: ['inventory-atp'] });
      onClose();
      alert(result === 'pass' ? '檢驗合格，庫存已放行至良品儲位' : '已記錄不合格，庫存維持隔離並待 MRB 處理');
    },
    onError: (mutationError: Error) => alert(mutationError.message || '提交 IQC 判定失敗'),
  });

  const addItem = () => setItems(current => [...current, {
    inspectionItem: '',
    specification: '',
    lowerLimit: '',
    upperLimit: '',
    measuredValue: '',
    result: 'pass' as 'pass' | 'fail',
  }]);

  const updateItem = (index: number, field: keyof typeof items[number], value: string) =>
    setItems(current => current.map((item, itemIndex) =>
      itemIndex === index ? { ...item, [field]: value } : item
    ));

  const canSubmit = inspection?.result === 'pending' && !completeMutation.isPending;

  return createPortal(
    <div style={{ position: 'fixed', inset: 0, zIndex: 1100, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20 }}>
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.85)' }} onClick={onClose} />
      <div
        className="glass-card"
        role="dialog"
        aria-modal="true"
        aria-labelledby="iqc-inspection-title"
        style={{ position: 'relative', width: 900, maxWidth: '100%', maxHeight: '90vh', overflowY: 'auto', padding: 24 }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
          <h3 id="iqc-inspection-title">進料檢驗：{inspection?.reportNumber ?? '載入中'}</h3>
          <button className="btn btn-ghost btn-sm" onClick={onClose}>關閉</button>
        </div>
        {isLoading ? <div style={{ padding: 30, textAlign: 'center' }}>載入中...</div> : error ? (
          <div role="alert" style={{ color: 'var(--crimson-300)' }}>{(error as Error).message}</div>
        ) : inspection && (
          <>
            <div className="glass-card" style={{ padding: 14, marginBottom: 16, display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10 }}>
              <span>收貨單：{inspection.goodsReceiptNumber}</span>
              <span>物料：{inspection.skuNumber} · {inspection.skuName}</span>
              <span>供應商：{inspection.supplierName || '—'}</span>
              <span>批號：{inspection.lotNumber || '—'}</span>
              <span>收貨數量：{inspection.receivedQty}</span>
              <span>檢驗後良品儲位：{inspection.releaseBinLocationCode || '—'}</span>
            </div>
            {inspection.result !== 'pending' ? (
              <>
                <div style={{ marginBottom: 12 }}>判定結果：{inspection.result === 'pass' ? '合格' : '不合格'}；抽樣數：{inspection.sampleSize}；不良數：{inspection.rejectNumber}</div>
                {inspection.result === 'fail' && <div role="status" style={{ color: 'var(--amber-300)', marginBottom: 12 }}>物料仍在隔離狀態，退貨／返工／特採須依後續 MRB 流程處理。</div>}
                <table className="data-table">
                  <thead><tr><th>檢驗項目</th><th>規格</th><th>下限</th><th>上限</th><th>實測值</th><th>判定</th></tr></thead>
                  <tbody>{inspection.items.map(item => <tr key={item.id}>
                    <td>{item.inspectionItem}</td><td>{item.specification}</td><td>{item.lowerLimit ?? '—'}</td>
                    <td>{item.upperLimit ?? '—'}</td><td>{item.measuredValue}</td><td>{item.result === 'pass' ? '合格' : '不合格'}</td>
                  </tr>)}</tbody>
                </table>
              </>
            ) : (
              <>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10, marginBottom: 16 }}>
                  <label>抽樣數<input className="form-input" type="number" min="1" max={inspection.receivedQty} value={sampleSize} onChange={event => setSampleSize(Number(event.target.value))} /></label>
                  <label>允收數<input className="form-input" type="number" min="0" value={acceptNumber} onChange={event => setAcceptNumber(Number(event.target.value))} /></label>
                  <label>拒收數<input className="form-input" type="number" min="0" value={rejectNumber} onChange={event => setRejectNumber(Number(event.target.value))} /></label>
                  <label>整批判定<select className="form-input" value={result} onChange={event => setResult(event.target.value as 'pass' | 'fail')}><option value="pass">合格</option><option value="fail">不合格</option></select></label>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                  <strong>檢驗項目與實測記錄</strong>
                  <button className="btn btn-ghost btn-sm" onClick={addItem}><Plus size={13} />新增項目</button>
                </div>
                <div style={{ overflowX: 'auto' }}>
                  <table className="data-table">
                    <thead><tr><th>檢驗項目</th><th>規格 / 方法</th><th>下限</th><th>上限</th><th>實測值</th><th>判定</th></tr></thead>
                    <tbody>{items.map((item, index) => <tr key={index}>
                      <td><input className="form-input" value={item.inspectionItem} onChange={event => updateItem(index, 'inspectionItem', event.target.value)} /></td>
                      <td><input className="form-input" value={item.specification} onChange={event => updateItem(index, 'specification', event.target.value)} /></td>
                      <td><input className="form-input" type="number" value={item.lowerLimit} onChange={event => updateItem(index, 'lowerLimit', event.target.value)} /></td>
                      <td><input className="form-input" type="number" value={item.upperLimit} onChange={event => updateItem(index, 'upperLimit', event.target.value)} /></td>
                      <td><input className="form-input" value={item.measuredValue} onChange={event => updateItem(index, 'measuredValue', event.target.value)} /></td>
                      <td><select className="form-input" value={item.result} onChange={event => updateItem(index, 'result', event.target.value)}><option value="pass">合格</option><option value="fail">不合格</option></select></td>
                    </tr>)}</tbody>
                  </table>
                </div>
                <div role="note" style={{ color: 'var(--amber-300)', marginTop: 12, fontSize: 12 }}>
                  尚未完成判定前，待檢庫存不計入可用量。判定不合格後仍會隔離，後續需依 MRB 決議處置。
                </div>
                <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16 }}>
                  <button className="btn btn-primary" disabled={!canSubmit} onClick={() => completeMutation.mutate()}>
                    {completeMutation.isPending ? '提交中...' : '提交檢驗判定'}
                  </button>
                </div>
              </>
            )}
          </>
        )}
      </div>
    </div>,
    document.body
  );
}
