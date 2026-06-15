import React, { useState } from 'react';
import { Search, Plus, Download, Eye, AlertTriangle, CheckCircle, Clock, Loader2, User, Paperclip, History, FileText } from 'lucide-react';
import { useIqcReports, useSupplierComplaints, useSuppliers } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { qualityApi } from '../api/api';
import { useQuery, useQueryClient } from '@tanstack/react-query';

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

const ncrDispositionConfig: Record<string, { label: string; cls: string }> = {
  hold: { label: '隔離中', cls: 'warning' },
  released: { label: '已釋放', cls: 'active' },
  scrapped: { label: '已報廢', cls: 'danger' },
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
  const [activeTab, setActiveTab] = useState<'iqc' | 'complaints' | 'ncr' | 'trace'>('iqc');
  const [showIqcModal, setShowIqcModal] = useState(false);
  const [showComplaintModal, setShowComplaintModal] = useState(false);
  const [showNcrOwnershipModal, setShowNcrOwnershipModal] = useState(false);
  const [showComplaintOwnershipModal, setShowComplaintOwnershipModal] = useState(false);
  const [showCapaOwnershipModal, setShowCapaOwnershipModal] = useState(false);
  const [selectedNcrId, setSelectedNcrId] = useState<string | null>(null);
  const [selectedComplaintId, setSelectedComplaintId] = useState<string | null>(null);
  const { data: iqcResp, isLoading: iqcLoading } = useIqcReports();
  const { data: complaintResp, isLoading: complaintLoading } = useSupplierComplaints();
  const { data: suppliersResp } = useSuppliers();
  const queryClient = useQueryClient();
  const { data: pendingIqcResp } = useQuery({
    queryKey: ['quality-pending-iqc'],
    queryFn: () => qualityApi.pendingIqcReceipts().then(res => res.data)
  });
  const { data: ncrResp, isLoading: ncrLoading } = useQuery({
    queryKey: ['quality-ncrs'],
    queryFn: () => qualityApi.ncrs().then(res => res.data)
  });
  const suppliers = (suppliersResp as { data?: Array<{ id: string; supplierName: string }> })?.data ?? [];

  const iqcReports = iqcResp?.data ?? [];
  const complaints = complaintResp?.data ?? [];
  const pendingIqcReceipts = pendingIqcResp?.data ?? [];
  const ncrs = ncrResp?.data ?? [];
  const selectedNcr = ncrs.find(n => n.id === selectedNcrId) ?? null;
  const selectedComplaint = complaints.find(c => c.id === selectedComplaintId) ?? null;

  const filteredIqc = iqcReports.filter(r =>
    r.reportNumber.includes(search) || (r.supplierName ?? '').includes(search)
  );
  const filteredComplaints = complaints.filter(c =>
    c.complaintNumber.includes(search) ||
    (c.supplierName ?? '').includes(search) ||
    (c.skuName ?? '').includes(search)
  );
  const filteredNcrs = ncrs.filter(n =>
    n.ncrNumber.includes(search) ||
    (n.sourceNumber ?? '').includes(search) ||
    (n.supplierName ?? '').includes(search) ||
    (n.skuName ?? '').includes(search)
  );

  const isLoading = activeTab === 'iqc' ? iqcLoading : activeTab === 'complaints' ? complaintLoading : activeTab === 'ncr' ? ncrLoading : false;

  const stats = [
    { label: 'IQC 報告數', value: iqcReports.length, color: 'var(--cobalt-400)' },
    { label: '待檢收貨', value: pendingIqcReceipts.length, color: 'var(--amber-400)' },
    { label: 'IQC 不合格', value: iqcReports.filter(r => r.result === 'fail').length, color: 'var(--crimson-400)' },
    { label: '客訴待處理', value: complaints.filter(c => c.status === 'open').length, color: 'var(--violet-400)' },
    { label: 'NCR 隔離中', value: ncrs.filter(n => (n.disposition ?? 'hold') === 'hold').length, color: 'var(--amber-400)' },
    { label: '客訴總數', value: complaints.length, color: 'var(--emerald-400)' },
  ];

  const refreshQualityData = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['quality-iqc'] }),
      queryClient.invalidateQueries({ queryKey: ['quality-complaints'] }),
      queryClient.invalidateQueries({ queryKey: ['quality-ncrs'] }),
      queryClient.invalidateQueries({ queryKey: ['quality-pending-iqc'] }),
      queryClient.invalidateQueries({ queryKey: ['accounts-payable'] }),
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] }),
    ]);
  };

  const createDefaultCapa = async (complaintId: string) => {
    await qualityApi.createCapa(complaintId, {
      d1Team: 'Quality / Procurement',
      problemDescription: 'Auto-created CAPA from IQC failure',
      interimContainment: 'IQC hold is active',
      rootCause: 'Pending analysis',
      permanentAction: 'Pending action plan',
      verification: 'Pending verification',
      prevention: 'Pending prevention plan',
      closure: 'Open',
    });
    await refreshQualityData();
  };

  const closeDefaultCapa = async (complaintId: string) => {
    await qualityApi.closeCapa(complaintId, {
      closure: 'CAPA verified and closed',
      verification: 'Closure reviewed by quality team',
    });
    await refreshQualityData();
  };

  const closeNcr = async (ncrId: string, disposition: string) => {
    await qualityApi.closeNcr(ncrId, {
      disposition,
      releaseStock: true,
      notes: disposition === 'scrapped' ? 'Scrap disposition processed' : 'Released from quality hold',
    });
    await refreshQualityData();
  };

  const updateNcrOwnership = async (data: Record<string, unknown>) => {
    if (!selectedNcr) return;
    await qualityApi.updateNcrOwnership(selectedNcr.id, {
      responsiblePerson: data.responsiblePerson as string | undefined,
      attachmentUrl: data.attachmentUrl as string | undefined,
      notes: data.notes as string | undefined,
    });
    await refreshQualityData();
    setShowNcrOwnershipModal(false);
  };

  const updateComplaintOwnership = async (data: Record<string, unknown>) => {
    if (!selectedComplaint) return;
    await qualityApi.updateComplaintOwnership(selectedComplaint.id, {
      responsiblePerson: data.responsiblePerson as string | undefined,
      attachmentUrl: data.attachmentUrl as string | undefined,
      notes: data.notes as string | undefined,
    });
    await refreshQualityData();
    setShowComplaintOwnershipModal(false);
  };

  const updateCapaOwnership = async (data: Record<string, unknown>) => {
    if (!selectedComplaint) return;
    await qualityApi.updateCapaOwnership(selectedComplaint.id, {
      responsiblePerson: data.responsiblePerson as string | undefined,
      attachmentUrl: data.attachmentUrl as string | undefined,
      notes: data.notes as string | undefined,
    });
    await refreshQualityData();
    setShowCapaOwnershipModal(false);
  };

  const renderTimeline = (timeline?: Array<{ id: string; actionType: string; actorName?: string; responsiblePerson?: string; notes?: string; attachmentUrl?: string; createdAt: string }>) => (
    <div style={{ display: 'grid', gap: 10 }}>
      {(timeline ?? []).length === 0 ? (
        <div style={{ color: 'var(--slate-500)', fontSize: 12 }}>暫無審查紀錄</div>
      ) : timeline!.map(entry => (
        <div key={entry.id} style={{
          padding: '12px 14px',
          borderRadius: 10,
          border: '1px solid var(--navy-800)',
          background: 'rgba(7, 14, 28, 0.72)',
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 6 }}>
            <div style={{ fontSize: 12, color: 'var(--slate-200)', fontWeight: 600 }}>{entry.actionType}</div>
            <div className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{entry.createdAt.slice(0, 19).replace('T', ' ')}</div>
          </div>
          <div style={{ fontSize: 12, color: 'var(--slate-400)', display: 'grid', gap: 4 }}>
            <div>執行者：{entry.actorName ?? 'system'}</div>
            <div>責任人：{entry.responsiblePerson ?? '未指定'}</div>
            {entry.notes && <div>備註：{entry.notes}</div>}
            {entry.attachmentUrl && <div>附件：{entry.attachmentUrl}</div>}
          </div>
        </div>
      ))}
    </div>
  );

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">品質管理</h2>
          <p className="module-page-desc">IQC 進料檢驗 · 供應商客訴追蹤</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
          <button
            className="btn btn-primary btn-sm"
            onClick={() => activeTab === 'complaints' ? setShowComplaintModal(true) : setShowIqcModal(true)}
          >
            <Plus size={13} />
            {activeTab === 'complaints' ? '新增客訴' : '執行 IQC'}
          </button>
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
            className={`tab-item ${activeTab === 'ncr' ? 'active' : ''}`}
            onClick={() => setActiveTab('ncr')}
          >
            NCR 隔離
          </button>
          <button
            className={`tab-item ${activeTab === 'trace' ? 'active' : ''}`}
            onClick={() => setActiveTab('trace')}
          >
            Lot 追溯查詢
          </button>
        </div>
      </div>

      <div className="glass-card" style={{ overflow: 'hidden' }}>
        {activeTab === 'iqc' ? (
          <>
            <div style={{ padding: '14px 16px', borderBottom: '1px solid var(--navy-800)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div style={{ fontSize: 12, color: 'var(--slate-400)' }}>
                待檢收貨 {pendingIqcReceipts.length} 筆
              </div>
              {pendingIqcReceipts.length > 0 && (
                <button className="btn btn-outline btn-sm" onClick={() => setShowIqcModal(true)}>
                  <CheckCircle size={13} />選擇收貨單執行 IQC
                </button>
              )}
            </div>
            <table className="data-table">
              <thead>
                <tr>
                  <th>報告編號</th>
                  <th>供應商</th>
                  <th>檢驗日期</th>
                  <th>批號</th>
                  <th>結果</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={6} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                    <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
                  </td></tr>
                ) : filteredIqc.map(r => {
                  const rc = iqcResultConfig[r.result] ?? { label: r.result, cls: 'neutral' };
                  return (
                    <tr key={r.id}>
                      <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{r.reportNumber}</td>
                      <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{r.supplierName ?? '—'}</td>
                      <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{(r.reportDate || r.inspectionDate || '').slice(0, 10)}</td>
                      <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{r.batchNumber || '—'}</td>
                      <td><span className={`status-badge status-${rc.cls}`}>{rc.label}</span></td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="icon-action-btn" title="查看"><Eye size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </>
        ) : activeTab === 'complaints' ? (
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
                <th>責任人</th>
                <th>附件</th>
                <th>結案原因</th>
                <th>CAPA 狀態</th>
                <th>CAPA 責任人</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={13} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
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
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{c.responsiblePerson ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{c.attachmentUrl ? <span className="mono">{c.attachmentUrl}</span> : '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{c.closureReason ?? '—'}</td>
                    <td><span className={`status-badge status-${(c.capaStatus === 'closed' ? 'active' : c.capaStatus === 'open' ? 'warning' : 'neutral')}`}>{c.capaStatus ?? '—'}</span></td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{c.capaResponsiblePerson ?? '—'}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button className="icon-action-btn" title="查看" onClick={() => {
                          setSelectedComplaintId(c.id);
                          setActiveTab('complaints');
                        }}><Eye size={13} /></button>
                        <button className="icon-action-btn" title="補責任人 / 附件" style={{ color: 'var(--cobalt-300)' }} onClick={() => {
                          setSelectedComplaintId(c.id);
                          setShowComplaintOwnershipModal(true);
                        }}>
                          <User size={13} />
                        </button>
                        {c.status === 'open' && (
                          <button className="icon-action-btn" title="建立 CAPA" style={{ color: 'var(--cobalt-300)' }} onClick={async () => {
                            setSelectedComplaintId(c.id);
                            await createDefaultCapa(c.id);
                          }}>
                            <Clock size={13} />
                          </button>
                        )}
                        {c.status !== 'closed' && (
                          <button className="icon-action-btn" title="結案 CAPA" style={{ color: 'var(--emerald-300)' }} onClick={async () => {
                            setSelectedComplaintId(c.id);
                            await closeDefaultCapa(c.id);
                          }}>
                            <CheckCircle size={13} />
                          </button>
                        )}
                        {c.status === 'investigating' && (
                          <button className="icon-action-btn" title="CAPA 維護" style={{ color: 'var(--amber-300)' }} onClick={() => {
                            setSelectedComplaintId(c.id);
                            setShowCapaOwnershipModal(true);
                          }}>
                            <FileText size={13} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : activeTab === 'ncr' ? (
          <table className="data-table">
            <thead>
              <tr>
                <th>NCR 編號</th>
                <th>來源單號</th>
                <th>供應商</th>
                <th>品項</th>
                <th>不良數量</th>
                <th>處置</th>
                <th>責任人</th>
                <th>附件</th>
                <th>結案原因</th>
                <th>建立時間</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={10} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                  <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
                </td></tr>
              ) : filteredNcrs.map(n => {
                const dc = ncrDispositionConfig[n.disposition ?? 'hold'] ?? { label: n.disposition ?? 'hold', cls: 'neutral' };
                return (
                  <tr key={n.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{n.ncrNumber}</td>
                    <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>{n.sourceNumber ?? '—'}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{n.supplierName ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{n.skuName ?? '—'}</td>
                    <td className="mono" style={{ color: 'var(--amber-300)', fontWeight: 600 }}>{n.defectQty.toLocaleString()}</td>
                    <td><span className={`status-badge status-${dc.cls}`}>{dc.label}</span></td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{n.responsiblePerson ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{n.attachmentUrl ? <span className="mono">{n.attachmentUrl}</span> : '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{n.closureReason ?? '—'}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{n.createdAt.slice(0, 10)}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button className="icon-action-btn" title="查看" onClick={() => {
                          setSelectedNcrId(n.id);
                          setActiveTab('ncr');
                        }}>
                          <Eye size={13} />
                        </button>
                        <button className="icon-action-btn" title="補責任人 / 附件" style={{ color: 'var(--cobalt-300)' }} onClick={() => {
                          setSelectedNcrId(n.id);
                          setShowNcrOwnershipModal(true);
                        }}>
                          <User size={13} />
                        </button>
                        {n.disposition !== 'released' && n.disposition !== 'scrapped' && (
                          <button className="icon-action-btn" title="釋放庫存" style={{ color: 'var(--emerald-300)' }} onClick={async () => {
                            await closeNcr(n.id, 'released');
                          }}>
                            <CheckCircle size={13} />
                          </button>
                        )}
                        <button className="icon-action-btn" title="報廢處置" style={{ color: 'var(--crimson-300)' }} onClick={async () => {
                          await closeNcr(n.id, 'scrapped');
                        }}>
                          <AlertTriangle size={13} />
                        </button>
                        <button className="icon-action-btn" title="查看"><Eye size={13} /></button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : null}
      </div>

      {(activeTab === 'ncr' && selectedNcr) && (
        <div className="glass-card" style={{ overflow: 'hidden' }}>
          <div style={{ padding: '18px 20px', borderBottom: '1px solid var(--navy-800)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>NCR 詳情</div>
              <h3 style={{ fontSize: 16, fontWeight: 700, color: 'var(--slate-100)' }}>{selectedNcr.ncrNumber}</h3>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="btn btn-outline btn-sm" onClick={() => setShowNcrOwnershipModal(true)}><User size={13} />更新責任人 / 附件</button>
              <button className="btn btn-outline btn-sm" onClick={() => setSelectedNcrId(null)}>關閉詳情</button>
            </div>
          </div>
          <div style={{ padding: '18px 20px', display: 'grid', gridTemplateColumns: 'repeat(4, minmax(0, 1fr))', gap: 12 }}>
            <div className="stat-card"><div className="stat-label">來源單號</div><div className="stat-value">{selectedNcr.sourceNumber ?? '—'}</div></div>
            <div className="stat-card"><div className="stat-label">責任人</div><div className="stat-value">{selectedNcr.responsiblePerson ?? '—'}</div></div>
            <div className="stat-card"><div className="stat-label">附件</div><div className="stat-value" style={{ fontSize: 12 }}>{selectedNcr.attachmentUrl ?? '—'}</div></div>
            <div className="stat-card"><div className="stat-label">結案原因</div><div className="stat-value" style={{ fontSize: 12 }}>{selectedNcr.closureReason ?? '—'}</div></div>
          </div>
          <div style={{ padding: '0 20px 20px' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1.1fr 0.9fr', gap: 16 }}>
              <div className="glass-card" style={{ padding: 16 }}>
                <div style={{ fontSize: 12, color: 'var(--slate-400)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}><History size={13} />審查軌跡</div>
                {renderTimeline(selectedNcr.timeline)}
              </div>
              <div className="glass-card" style={{ padding: 16 }}>
                <div style={{ fontSize: 12, color: 'var(--slate-400)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}><Paperclip size={13} />摘要</div>
                <div style={{ display: 'grid', gap: 10, fontSize: 12, color: 'var(--slate-300)' }}>
                  <div><span style={{ color: 'var(--slate-500)' }}>缺陷：</span>{selectedNcr.defectType ?? '—'}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>不良數量：</span>{selectedNcr.defectQty.toLocaleString()}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>成本影響：</span>{selectedNcr.costImpact.toLocaleString()}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>描述：</span>{selectedNcr.defectDescription ?? '—'}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {(activeTab === 'complaints' && selectedComplaint) && (
        <div className="glass-card" style={{ overflow: 'hidden' }}>
          <div style={{ padding: '18px 20px', borderBottom: '1px solid var(--navy-800)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>客訴詳情</div>
              <h3 style={{ fontSize: 16, fontWeight: 700, color: 'var(--slate-100)' }}>{selectedComplaint.complaintNumber}</h3>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="btn btn-outline btn-sm" onClick={() => setShowComplaintOwnershipModal(true)}><User size={13} />更新責任人 / 附件</button>
              <button className="btn btn-outline btn-sm" onClick={() => setShowCapaOwnershipModal(true)}><FileText size={13} />CAPA 維護</button>
              <button className="btn btn-outline btn-sm" onClick={() => setSelectedComplaintId(null)}>關閉詳情</button>
            </div>
          </div>
          <div style={{ padding: '18px 20px', display: 'grid', gridTemplateColumns: 'repeat(4, minmax(0, 1fr))', gap: 12 }}>
            <div className="stat-card"><div className="stat-label">供應商</div><div className="stat-value">{selectedComplaint.supplierName ?? '—'}</div></div>
            <div className="stat-card"><div className="stat-label">責任人</div><div className="stat-value">{selectedComplaint.responsiblePerson ?? '—'}</div></div>
            <div className="stat-card"><div className="stat-label">附件</div><div className="stat-value" style={{ fontSize: 12 }}>{selectedComplaint.attachmentUrl ?? '—'}</div></div>
            <div className="stat-card"><div className="stat-label">結案原因</div><div className="stat-value" style={{ fontSize: 12 }}>{selectedComplaint.closureReason ?? '—'}</div></div>
          </div>
          <div style={{ padding: '0 20px 20px' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1.1fr 0.9fr', gap: 16 }}>
              <div className="glass-card" style={{ padding: 16 }}>
                <div style={{ fontSize: 12, color: 'var(--slate-400)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}><History size={13} />審查軌跡</div>
                {renderTimeline(selectedComplaint.timeline)}
              </div>
              <div className="glass-card" style={{ padding: 16 }}>
                <div style={{ fontSize: 12, color: 'var(--slate-400)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}><Paperclip size={13} />摘要</div>
                <div style={{ display: 'grid', gap: 10, fontSize: 12, color: 'var(--slate-300)' }}>
                  <div><span style={{ color: 'var(--slate-500)' }}>品項：</span>{selectedComplaint.skuName ?? '—'}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>缺陷類型：</span>{selectedComplaint.defectType ?? '—'}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>PPM：</span>{selectedComplaint.ppm.toLocaleString()}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>狀態：</span>{selectedComplaint.status}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>CAPA 編號：</span>{selectedComplaint.capaNumber ?? '—'}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>CAPA 狀態：</span>{selectedComplaint.capaStatus ?? '—'}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>CAPA 責任人：</span>{selectedComplaint.capaResponsiblePerson ?? '—'}</div>
                  <div><span style={{ color: 'var(--slate-500)' }}>CAPA 結案原因：</span>{selectedComplaint.capaClosureReason ?? '—'}</div>
                </div>
              </div>
              <div className="glass-card" style={{ padding: 16 }}>
                <div style={{ fontSize: 12, color: 'var(--slate-400)', marginBottom: 10, display: 'flex', alignItems: 'center', gap: 8 }}><History size={13} />CAPA 軌跡</div>
                {renderTimeline(selectedComplaint.capaTimeline)}
              </div>
            </div>
          </div>
        </div>
      )}

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
        .module-stat-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
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
        open={showIqcModal}
        onClose={() => setShowIqcModal(false)}
        title="執行 IQC"
        fields={[
          {
            name: 'goodsReceiptId',
            label: '收貨單',
            type: 'select',
            required: true,
            options: pendingIqcReceipts.map((receipt) => ({
              value: receipt.id,
              label: `[${receipt.grNumber}] ${receipt.supplierName || '未指定供應商'}`
            })),
            description: '選擇待檢的收貨單，完成 IQC 後會同步更新收貨狀態與應付帳款匹配。'
          },
          {
            name: 'result',
            label: '檢驗結果',
            type: 'select',
            required: true,
            options: [
              { value: 'pass', label: '合格' },
              { value: 'conditional', label: '條件通過' },
              { value: 'fail', label: '不合格' }
            ]
          },
          { name: 'sampleQty', label: '抽樣數量', type: 'number', required: true, min: 0, defaultValue: 0 },
          { name: 'defectQty', label: '不良數量', type: 'number', required: true, min: 0, defaultValue: 0 },
          { name: 'lotNumber', label: '批號', type: 'text', placeholder: '可選填 Lot No.' },
          { name: 'notes', label: '檢驗備註', type: 'textarea', placeholder: '例如：外觀抽檢、尺寸驗證結果...' },
        ]}
        onSubmit={async (data) => {
          await qualityApi.createIqcReport({
            goodsReceiptId: data.goodsReceiptId,
            result: data.result,
            sampleQty: Number(data.sampleQty || 0),
            defectQty: Number(data.defectQty || 0),
            lotNumber: data.lotNumber,
            notes: data.notes,
          });
          await Promise.all([
            queryClient.invalidateQueries({ queryKey: ['quality-iqc'] }),
            queryClient.invalidateQueries({ queryKey: ['quality-complaints'] }),
            queryClient.invalidateQueries({ queryKey: ['quality-pending-iqc'] }),
            queryClient.invalidateQueries({ queryKey: ['accounts-payable'] }),
            queryClient.invalidateQueries({ queryKey: ['goods-receipts'] }),
          ]);
          setShowIqcModal(false);
        }}
      />

      <FormModal
        open={showComplaintModal}
        onClose={() => setShowComplaintModal(false)}
        title="新增客訴報告"
        fields={buildComplaintFields(suppliers)}
        onSubmit={async (data) => {
          if (data.defectQty != null && Number(data.defectQty) <= 0) {
            alert('不良數量必須大於 0');
            return;
          }
          await qualityApi.createComplaint(data);
          await queryClient.invalidateQueries({ queryKey: ['quality-complaints'] });
          setShowComplaintModal(false);
        }}
      />

      <FormModal
        open={showNcrOwnershipModal}
        onClose={() => setShowNcrOwnershipModal(false)}
        title="NCR 審查資訊"
        fields={[
          { name: 'responsiblePerson', label: '責任人', type: 'text', defaultValue: selectedNcr?.responsiblePerson ?? '' },
          { name: 'attachmentUrl', label: '附件網址', type: 'text', defaultValue: selectedNcr?.attachmentUrl ?? '', placeholder: '附件或證據連結' },
          { name: 'notes', label: '審查備註', type: 'textarea', defaultValue: selectedNcr?.closureReason ?? '' },
        ]}
        onSubmit={updateNcrOwnership}
      />

      <FormModal
        open={showComplaintOwnershipModal}
        onClose={() => setShowComplaintOwnershipModal(false)}
        title="客訴審查資訊"
        fields={[
          { name: 'responsiblePerson', label: '責任人', type: 'text', defaultValue: selectedComplaint?.responsiblePerson ?? '' },
          { name: 'attachmentUrl', label: '附件網址', type: 'text', defaultValue: selectedComplaint?.attachmentUrl ?? '', placeholder: '附件或證據連結' },
          { name: 'notes', label: '審查備註', type: 'textarea', defaultValue: selectedComplaint?.closureReason ?? '' },
        ]}
        onSubmit={updateComplaintOwnership}
      />

      <FormModal
        open={showCapaOwnershipModal}
        onClose={() => setShowCapaOwnershipModal(false)}
        title="CAPA 審查資訊"
        fields={[
          { name: 'responsiblePerson', label: '責任人', type: 'text', defaultValue: selectedComplaint?.responsiblePerson ?? '' },
          { name: 'attachmentUrl', label: '附件網址', type: 'text', defaultValue: selectedComplaint?.attachmentUrl ?? '', placeholder: 'CAPA 佐證或附件' },
          { name: 'notes', label: '結案原因 / 備註', type: 'textarea', defaultValue: selectedComplaint?.closureReason ?? '' },
        ]}
        onSubmit={updateCapaOwnership}
      />
    </div>
  );
}
