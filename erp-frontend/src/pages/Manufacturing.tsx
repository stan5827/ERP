import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Search, Plus, Filter, Download, Eye, Edit2, 
  ChevronUp, ChevronDown, Send, Users, Loader2, 
  Check, X, Factory, Activity, Clock, AlertCircle,
  BarChart3, Layers, Zap, Cpu, Box, Calendar, Calculator, Package
} from 'lucide-react';
import { 
  useWorkOrders, useCreateWorkOrder, useUpdateWorkOrder, useUpdateWorkOrderStatus, 
  useStartWorkOrder, useCompleteWorkOrder, useSkus, useBoms,
  useWorkOrder, useCreateWipReport, useExplodeRouting, useCreateSupplyOrders
} from '../hooks/useApi';
import type { WorkOrderDto } from '../api/api';
import type { ModuleId } from '../types';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { useQueryClient } from '@tanstack/react-query';
import './styles/Manufacturing-Cyber.css';
import { WorkOrderCostReport } from './WorkOrderCostReport';

const statusDisplay: Record<string, { label: string; className: string }> = {
  pending: { label: '待發放', className: 'cy-status-pending' },
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

interface ManufacturingProps {
  onNavigate?: (module: ModuleId) => void;
}

export const Manufacturing: React.FC<ManufacturingProps> = ({ onNavigate }) => {
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [showForm, setShowForm] = useState(false);
  const [selectedWo, setSelectedWo] = useState<WorkOrderDto | null>(null);
  const [showDetails, setShowDetails] = useState(false);
  const [showCostReport, setShowCostReport] = useState(false);
  const [showReportForm, setShowReportForm] = useState(false);
  const [selectedOp, setSelectedOp] = useState<any>(null);
  const [reportingMode, setReportingMode] = useState(false);
  
  const queryClient = useQueryClient();
  const { data, isLoading } = useWorkOrders();
  const workOrders: WorkOrderDto[] = data?.data ?? [];
  const { data: skuData } = useSkus({ pageSize: '9999' });
  const skus = skuData?.data ?? [];
  const { data: bomData } = useBoms();
  const boms = bomData?.data ?? [];

  const createMutation = useCreateWorkOrder();
  const updateMutation = useUpdateWorkOrder();
  const updateStatusMutation = useUpdateWorkOrderStatus();
  const startMutation = useStartWorkOrder();
  const completeMutation = useCompleteWorkOrder();
  const createWipMutation = useCreateWipReport();
  const explodeMutation = useExplodeRouting();
  const supplyMutation = useCreateSupplyOrders();
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create');

  // 添加錯誤處理
  const handleMutationError = (err: any) => {
    const msg = err.response?.data?.message || err.message || '操作失敗';
    alert(msg);
  };
  
  // 獲取選中工單的詳細資料（含工序）
  const { data: woDetail } = useWorkOrder(selectedWo?.id || '');
  const detailedWo = woDetail?.data as any;

  const filtered = workOrders.filter(wo =>
    (filterStatus === 'all' || wo.status === filterStatus) &&
    (wo.woNumber.includes(search) || (wo.productSkuName ?? '').includes(search))
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

  // 智慧報工引導邏輯
  React.useEffect(() => {
    if (reportingMode && detailedWo && detailedWo.id === selectedWo?.id) {
      const ops = detailedWo.operations || [];
      if (ops.length === 1) {
        setSelectedOp(ops[0]);
        setShowReportForm(true);
        setReportingMode(false);
        setShowDetails(false); // 直接報工，不需要看詳情
      }
    }
  }, [detailedWo, reportingMode, selectedWo]);

  const formFields: FormField[] = [
    { name: 'woType', label: '類型', type: 'select', options: [
      { value: 'regular', label: '一般' }, { value: 'rework', label: '重工' }
    ] },
    { name: 'productSkuId', label: '產品', type: 'select', required: true,
      options: skus.map((s: any) => ({ value: s.id, label: `${s.skuName} (${s.skuNumber})` }))
    },
    { name: 'plannedQty', label: '計劃數量', type: 'number', required: true, min: 1 },
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
          <button className="btn-cy" style={{ background: 'transparent', border: '1px solid rgba(255,255,255,0.1)' }}>
            <Download size={16} /> 導出數據
          </button>
          <button className="btn-cy" onClick={() => { setFormMode('create'); setSelectedWo(null); setShowForm(true); }}>
            <Plus size={18} /> 建立工單
          </button>
        </div>
      </header>

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
            {['all', 'pending', 'in_progress', 'completed'].map(s => (
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
                <th>產品規格 / 品名</th>
                <th>優先等級</th>
                <th>執行進度</th>
                <th>預計交付</th>
                <th>當前狀態</th>
                <th style={{ textAlign: 'right' }}>系統操作</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '3rem' }}>系統同步中...</td></tr>
              ) : filtered.map((wo) => (
                <tr key={wo.id} className="cy-row">
                  <td><span className="cy-wo-number">{wo.woNumber}</span></td>
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
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                      {wo.status === 'pending' && (
                        <button className="cy-action-btn" style={{ color: 'var(--cobalt-400)' }} onClick={() => updateStatusMutation.mutate({ id: wo.id, status: 'released' })} title="發放工單">
                          <Send size={14} />
                        </button>
                      )}
                      
                      {wo.status === 'released' && (
                        <button className="cy-action-btn" style={{ color: 'var(--emerald-400)' }} onClick={() => startMutation.mutate(wo.id)} title="開始生產">
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
                  <label style={{ display: 'block', fontSize: '0.7rem', color: '#64748b', textTransform: 'uppercase', marginBottom: '0.5rem' }}>當前狀態</label>
                  <span className={`cy-status ${statusDisplay[selectedWo.status]?.className}`}>{statusDisplay[selectedWo.status]?.label}</span>
                </div>

                <div style={{ gridColumn: 'span 2' }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem', borderBottom: '1px solid rgba(255,255,255,0.05)', paddingBottom: '0.5rem' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <Package size={16} style={{ color: 'var(--cy-cyan)' }} />
                      <span style={{ fontSize: '0.9rem', fontWeight: 600, color: '#fff' }}>生產需料清單 (Material Requirements)</span>
                    </div>
                    
                    {detailedWo?.materials?.some((m: any) => m.status === 'shortage') && (
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
                  
                  {(() => {
                    const activeBom = boms?.find(b => b.productSkuId === selectedWo.productSkuId && b.status === 'active');
                    if (!activeBom) return <div style={{ fontSize: '0.85rem', color: '#64748b', padding: '1rem', textAlign: 'center', background: 'rgba(255,255,255,0.02)', borderRadius: '0.5rem' }}>此產品尚未設定生效的 BOM 表，無法計算需料。</div>;

                    return (
                      <table style={{ width: '100%', fontSize: '0.85rem', borderCollapse: 'collapse' }}>
                        <thead>
                          <tr style={{ color: '#64748b', textAlign: 'left', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                            <th style={{ padding: '0.5rem' }}>料號/品名</th>
                            <th style={{ padding: '0.5rem' }}>單位用量</th>
                            <th style={{ padding: '0.5rem' }}>需求總量</th>
                            <th style={{ padding: '0.5rem' }}>目前庫存</th>
                            <th style={{ padding: '0.5rem' }}>狀態</th>
                          </tr>
                        </thead>
                        <tbody>
                          {activeBom.items.map((it: any) => {
                            const sku = skuData?.data.find(s => s.id === it.childSkuId);
                            const requiredTotal = it.quantity * selectedWo.plannedQty;
                            const currentStock = sku?.inventoryQty || 0;
                            const isShortage = currentStock < requiredTotal;

                            return (
                              <tr key={it.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                                <td style={{ padding: '0.75rem 0.5rem' }}>
                                  <div style={{ color: '#fff' }}>{it.childSkuNumber}</div>
                                  <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{it.childSkuName}</div>
                                </td>
                                <td style={{ padding: '0.75rem 0.5rem', color: '#94a3b8' }}>{it.quantity} {it.unit}</td>
                                <td style={{ padding: '0.75rem 0.5rem', color: '#fff', fontWeight: 600 }}>{requiredTotal} {it.unit}</td>
                                <td style={{ padding: '0.75rem 0.5rem', color: isShortage ? '#ef4444' : '#10b981' }}>{currentStock}</td>
                                <td style={{ padding: '0.75rem 0.5rem' }}>
                                  {isShortage ? (
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
                  
                  {detailedWo?.operations && detailedWo.operations.length > 0 ? (
                    <table style={{ width: '100%', fontSize: '0.85rem', borderCollapse: 'collapse' }}>
                      <thead>
                        <tr style={{ color: '#64748b', textAlign: 'left', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                          <th style={{ padding: '0.5rem' }}>序號</th>
                          <th style={{ padding: '0.5rem' }}>工序名稱</th>
                          <th style={{ padding: '0.5rem' }}>工作中心</th>
                          <th style={{ padding: '0.5rem' }}>計畫/完成</th>
                          <th style={{ padding: '0.5rem' }}>狀態</th>
                          <th style={{ padding: '0.5rem', textAlign: 'right' }}>操作</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detailedWo.operations.map((op: any) => (
                          <tr key={op.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                            <td style={{ padding: '0.75rem 0.5rem', color: 'var(--cy-cyan)', fontWeight: 'bold' }}>{op.operationSeq}</td>
                            <td style={{ padding: '0.75rem 0.5rem', color: '#fff' }}>{op.operationName}</td>
                            <td style={{ padding: '0.75rem 0.5rem', color: '#94a3b8' }}>{op.workCenterName}</td>
                            <td style={{ padding: '0.75rem 0.5rem', color: '#fff' }}>
                              {op.completedQty} / {op.plannedQty}
                            </td>
                            <td style={{ padding: '0.75rem 0.5rem' }}>
                              <span className={`status-badge status-${op.status === 'completed' ? 'active' : op.status === 'in_progress' ? 'warning' : 'neutral'}`}>
                                {op.status === 'completed' ? '已完成' : op.status === 'in_progress' ? '進行中' : '待生產'}
                              </span>
                            </td>
                            <td style={{ padding: '0.75rem 0.5rem', textAlign: 'right' }}>
                              {selectedWo.status === 'in_progress' && op.status !== 'completed' && (
                                <button className="btn-cy" style={{ padding: '2px 8px', fontSize: '0.7rem' }} onClick={() => { setSelectedOp(op); setShowReportForm(true); }}>
                                  報工
                                </button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  ) : detailedWo?.operations ? (
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
        initialValues={selectedWo as any}
        onClose={() => { setShowForm(false); setSelectedWo(null); }}
        onSubmit={handleSubmitForm}
        width={500}
      />

      <FormModal
        open={showReportForm}
        title={`工序報工: Seq ${selectedOp?.operationSeq} - ${selectedOp?.operationName}`}
        fields={[
          { name: 'completedQty', label: '完工良品數', type: 'number', required: true },
          { name: 'defectQty', label: '不良品數', type: 'number' },
          { name: 'scrapQty', label: '報廢數', type: 'number' },
          { name: 'actualManHours', label: '投入工時 (hr)', type: 'number', required: true },
          { name: 'actualMachineHours', label: '機台工時 (hr)', type: 'number' },
          { name: 'employeeId', label: '操作人員', type: 'select', options: [{ value: '7bd09cc3-3c03-4fe4-a38a-842f3fac18ea', label: 'Admin' }] }, // 簡化處理
          { name: 'notes', label: '異常說明', type: 'textarea' },
        ]}
        onClose={() => setShowReportForm(false)}
        onSubmit={async (values) => {
          createWipMutation.mutate({
            workOrderId: selectedWo?.id,
            operationSeq: selectedOp?.operationSeq,
            ...values,
            completedQty: Number(values.completedQty),
            defectQty: Number(values.defectQty || 0),
            scrapQty: Number(values.scrapQty || 0),
            actualManHours: Number(values.actualManHours),
            actualMachineHours: Number(values.actualMachineHours || 0),
          }, {
            onError: handleMutationError,
            onSuccess: () => {
              setShowReportForm(false);
              queryClient.invalidateQueries({ queryKey: ['work-orders'] });
              alert('報工成功！');
            }
          });
        }}
        width={400}
      />
    </div>
  );
};


