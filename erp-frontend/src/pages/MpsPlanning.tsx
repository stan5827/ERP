import React, { useState, useEffect, useMemo } from 'react';
import { 
  Play, RefreshCw, Calendar, Package, AlertTriangle, 
  CheckCircle, TrendingUp, Clock, Plus, BarChart2, 
  Search, ChevronRight, Filter, Download, Info, LineChart,
  Eye, History, Settings, MoreVertical, Layers, Loader2, Zap, X
} from 'lucide-react';
import { mpsApi, skusApi, MpsHeaderDto, MpsDetailDto, MpsLineDto, ApiResponse } from '../api/api';
import { FormModal } from '../components/FormModal';
import './MpsPlanning.css';
import './PremiumDesign.css';

interface MpsPlanningProps {
  onNavigate?: (module: string) => void;
}

export const MpsPlanning: React.FC<MpsPlanningProps> = ({ onNavigate }) => {
  const [headers, setHeaders] = useState<MpsHeaderDto[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);
  const [detail, setDetail] = useState<MpsDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'grid' | 'exceptions' | 'traceability'>('grid');
  
  const [skus, setSkus] = useState<Array<{value: string, label: string}>>([]);
  const [showCreateMps, setShowCreateMps] = useState(false);
  const [showTraceability, setShowTraceability] = useState<MpsLineDto | null>(null);
  
  // Multi-UOM / Multi-Currency / RCCP
  const [displayUnit, setDisplayUnit] = useState<'BASE' | 'PURCHASE' | 'SALES' | 'PROD'>('BASE');
  const [displayCurrency, setDisplayCurrency] = useState('TWD');
  const [rccpStatus, setRccpStatus] = useState<any>(null);
  const [skuDetails, setSkuDetails] = useState<Record<string, any>>({});

  useEffect(() => {
    loadHeaders();
    loadSkus();
  }, []);

  const loadHeaders = async () => {
    setLoading(true);
    try {
      const res = await mpsApi.list();
      const data = res.data?.data || [];
      setHeaders(data);
      if (data.length > 0 && !selectedVersionId) {
        setSelectedVersionId(data[0].id);
      }
    } catch (err) {
      console.error('Failed to load MPS headers:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadSkus = async () => {
    try {
      const res = await skusApi.all();
      const items = res.data?.data || [];
      setSkus(items.map(s => ({ value: s.id, label: `${s.skuNumber} - ${s.skuName}` })));
      
      const details: Record<string, any> = {};
      items.forEach(s => { details[s.id] = s; });
      setSkuDetails(details);
    } catch (err) {
      console.error('Failed to load SKUs:', err);
    }
  };

  const loadDetail = async (id: string) => {
    setDetailLoading(true);
    try {
      const res = await mpsApi.get(id);
      setDetail(res.data?.data || null);
      
      // Load RCCP status
      const rccpRes = await mpsApi.getRccpStatus(id);
      setRccpStatus(rccpRes.data?.data || null);
    } catch (err) {
      console.error('Failed to load MPS detail/RCCP:', err);
    } finally {
      setDetailLoading(false);
    }
  };

  useEffect(() => {
    if (selectedVersionId) {
      loadDetail(selectedVersionId);
    }
  }, [selectedVersionId]);

  // Pivot data for the grid
  const gridData = useMemo(() => {
    if (!detail) return { skus: [], buckets: [] };
    
    const skuMap = new Map<string, { id: string, number: string, name: string, lines: Map<string, MpsLineDto> }>();
    const buckets = new Set<string>();

    detail.lines.forEach(line => {
      if (!skuMap.has(line.skuId)) {
        skuMap.set(line.skuId, { id: line.skuId, number: line.skuNumber, name: line.skuName, lines: new Map() });
      }
      const key = `${new Date(line.bucketStart).toLocaleDateString()}`;
      skuMap.get(line.skuId)!.lines.set(key, line);
      buckets.add(key);
    });

    return {
      skus: Array.from(skuMap.values()),
      buckets: Array.from(buckets).sort((a, b) => new Date(a).getTime() - new Date(b).getTime())
    };
  }, [detail]);

  const handleRunConsumption = async () => {
    if (!selectedVersionId) return;
    try {
      await mpsApi.runConsumption(selectedVersionId);
      alert('預測沖銷計算完成！');
      loadDetail(selectedVersionId);
    } catch (err: any) {
      alert('計算失敗：' + err.message);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'APPROVED': return 'var(--emerald-400)';
      case 'DRAFT': return 'var(--cobalt-400)';
      case 'ARCHIVED': return 'var(--slate-500)';
      default: return 'var(--slate-400)';
    }
  };

  const convertQty = (qty: number, skuId: string) => {
    const sku = skuDetails[skuId];
    if (!sku) return qty;
    
    switch (displayUnit) {
      case 'PURCHASE': return qty / (sku.purchaseToBaseFactor || 1);
      case 'SALES': return qty / (sku.salesToBaseFactor || 1);
      case 'PROD': return qty / (sku.productionToBaseFactor || 1);
      default: return qty;
    }
  };

  const getUomLabel = (skuId: string) => {
    const sku = skuDetails[skuId];
    if (!sku) return '';
    switch (displayUnit) {
      case 'PURCHASE': return sku.purchaseUomCode || '箱';
      case 'SALES': return sku.salesUomCode || '盒';
      case 'PROD': return sku.productionUomCode || '批';
      default: return sku.baseUomCode || 'PCS';
    }
  };

  const rccpByResource = useMemo(() => {
    if (!rccpStatus) return [];
    // Groups loads by resource for display
    const map = new Map<string, any>();
    rccpStatus.loads.forEach((l: any) => {
      if (!map.has(l.resourceId)) {
        map.set(l.resourceId, { id: l.resourceId, code: l.resourceCode, name: l.resourceName, buckets: new Map() });
      }
      map.get(l.resourceId).buckets.set(l.bucketCode, l);
    });
    return Array.from(map.values());
  }, [rccpStatus]);

  if (loading) {
    return <div className="mps-planning"><div className="loading-container"><div className="spinner"></div></div></div>;
  }

  return (
    <div className="mps-planning">
      <div className="page-header">
        <div className="title-area">
          <h1>
            <LineChart size={32} strokeWidth={2.5} className="text-primary" />
            Master Production Schedule (MPS) <span className="beta-tag">Premium</span>
          </h1>
          <p className="subtitle">同步銷售預測與產能資源，實現精準製造策略</p>
        </div>
        <div className="action-area">
          <button className="btn btn-outline" onClick={() => setShowCreateMps(true)}>
            <Plus size={18} /> 建立新版計劃
          </button>
          <button className="btn btn-primary" onClick={handleRunConsumption} disabled={!selectedVersionId}>
            <RefreshCw size={18} /> 執行預測沖銷
          </button>
        </div>
      </div>

      <div className="mps-layout">
        {/* Sidebar: Version History */}
        <div className="mps-sidebar glass-card">
          <div className="sidebar-header">
            <h3><History size={16} /> 計劃版本歷史</h3>
          </div>
          <div className="version-list">
            {headers.map(h => (
              <div 
                key={h.id} 
                className={`version-item ${selectedVersionId === h.id ? 'active' : ''}`}
                onClick={() => setSelectedVersionId(h.id)}
              >
                <div className="version-info">
                  <span className="v-no">V{h.versionNo}</span>
                  <span className="v-name">{h.planName}</span>
                </div>
                <div className="version-meta">
                  <span className="font-mono">{h.mpsNo}</span>
                  <div className="flex items-center gap-2">
                    <span className="status-dot" style={{ backgroundColor: getStatusColor(h.status) }}></span>
                    <span style={{ color: getStatusColor(h.status), fontSize: '10px', fontWeight: 700 }}>{h.status}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Main Content */}
        <div className="mps-content">
          <div className="content-header glass-card">
            <div className="header-info">
              {detail ? (
                <>
                  <h2>
                    {detail.planName} 
                    <span className="status-badge" style={{ borderColor: getStatusColor(detail.status), color: getStatusColor(detail.status) }}>
                      {detail.status}
                    </span>
                  </h2>
                  <div className="meta-row">
                    <span className="flex items-center gap-1"><Calendar size={14} /> 規劃期間: {new Date(detail.horizonStart).toLocaleDateString()} ~ {new Date(detail.horizonEnd).toLocaleDateString()}</span>
                    <span className="flex items-center gap-1"><Layers size={14} /> 時格: {detail.timeBucketType}</span>
                    <span className="flex items-center gap-1"><Package size={14} /> 料號數: {gridData.skus.length}</span>
                  </div>
                </>
              ) : (
                <h2>請從左側選擇規劃版本</h2>
              )}
            </div>
            <div className="content-tabs">
              <button className={activeTab === 'grid' ? 'active' : ''} onClick={() => setActiveTab('grid')}><BarChart2 size={16} /> 計劃網格</button>
              <button className={activeTab === 'exceptions' ? 'active' : ''} onClick={() => setActiveTab('exceptions')}><AlertTriangle size={16} /> 異常提醒</button>
              <button className="btn-mrp-link" onClick={() => onNavigate?.('mrp')}><Settings size={16} /> MRP 轉單</button>
            </div>
          </div>

          <div className="reporting-bar glass-card p-3">
            <div className="filter-group">
              <label><Package size={14} /> 顯示單位:</label>
              <select value={displayUnit} onChange={e => setDisplayUnit(e.target.value as any)}>
                <option value="BASE">基本單位 (Base)</option>
                <option value="PURCHASE">採購單位 (Purchase)</option>
                <option value="SALES">銷售單位 (Sales)</option>
                <option value="PROD">生產單位 (Production)</option>
              </select>
            </div>
            <div className="filter-group">
              <label><TrendingUp size={14} /> 匯總幣別:</label>
              <select value={displayCurrency} onChange={e => setDisplayCurrency(e.target.value)}>
                <option value="TWD">TWD (新台幣)</option>
                <option value="USD">USD (美金)</option>
                <option value="JPY">JPY (日幣)</option>
              </select>
            </div>
            <div className="search-group ml-auto">
              <div className="search-box">
                <Search size={14} className="search-icon" />
                <input type="text" placeholder="搜尋料號或產品..." className="search-input" />
              </div>
            </div>
          </div>

          <div className="grid-container glass-card">
            {detailLoading ? (
              <div className="loading-overlay">
                <Loader2 size={40} className="animate-spin text-primary" />
                <p>正在分析計劃數據...</p>
              </div>
            ) : detail ? (
              <div className="mps-grid-wrapper">
                <table className="mps-grid">
                  <thead>
                    <tr>
                      <th className="sticky-col sku-info">產品料號 / 品名</th>
                      <th className="sticky-col data-type">規劃指標</th>
                      {gridData.buckets.map(b => (
                        <th key={b} className="bucket-header">{b}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {gridData.skus.map(s => (
                      <React.Fragment key={s.id}>
                        {/* Forecast Row */}
                        <tr className="row-forecast">
                          <td rowSpan={3} className="sticky-col sku-info">
                            <div className="sku-cell">
                              <span className="sku-num">{s.number}</span>
                              <span className="sku-name">{s.name}</span>
                              <div className="flex justify-between items-center mt-2">
                                <span className="sku-uom-tag">{getUomLabel(s.id)}</span>
                                <button className="btn-icon-sm" title="歷史趨勢"><TrendingUp size={12} /></button>
                              </div>
                            </div>
                          </td>
                          <td className="sticky-col data-type">銷售預測 (Forecast)</td>
                          {gridData.buckets.map(b => {
                            const line = s.lines.get(b);
                            return <td key={b} className="qty-cell">{line ? convertQty(line.planQty, s.id).toLocaleString() : '-'}</td>;
                          })}
                        </tr>
                        {/* Consumed Row */}
                        <tr className="row-consumed">
                          <td className="sticky-col data-type">沖銷訂單 (Sales)</td>
                          {gridData.buckets.map(b => {
                            const line = s.lines.get(b);
                            const val = line ? convertQty(line.consumedQty, s.id) : 0;
                            return (
                              <td key={b} className="qty-cell consumed" onClick={() => line && setShowTraceability(line)}>
                                {val ? (
                                  <span className="has-value">{val.toLocaleString()} <Info size={10} /></span>
                                ) : '-'}
                              </td>
                            );
                          })}
                        </tr>
                        {/* Net Requirement Row */}
                        <tr className="row-net">
                          <td className="sticky-col data-type">淨需求 (MPS)</td>
                          {gridData.buckets.map(b => {
                            const line = s.lines.get(b);
                            return <td key={b} className="qty-cell mps-value">{line ? convertQty(line.adjustedQty, s.id).toLocaleString() : '-'}</td>;
                          })}
                        </tr>
                        <tr className="row-spacer"><td colSpan={gridData.buckets.length + 2}></td></tr>
                      </React.Fragment>
                    ))}

                    {/* RCCP Section */}
                    {rccpByResource.length > 0 && (
                      <>
                        <tr className="section-divider">
                          <td colSpan={gridData.buckets.length + 2}>
                            <div className="flex items-center gap-2">
                              <Zap size={16} fill="currentColor" />
                              粗略產能檢查 (Rough-Cut Capacity Planning)
                            </div>
                          </td>
                        </tr>
                        {rccpByResource.map(r => (
                          <tr key={r.id} className="row-rccp">
                            <td className="sticky-col sku-info">
                              <div className="resource-cell">
                                <span className="res-code">{r.code}</span>
                                <span className="res-name">{r.name}</span>
                              </div>
                            </td>
                            <td className="sticky-col data-type">產能負荷 (%)</td>
                            {gridData.buckets.map(b => {
                              const load = r.buckets.get(b);
                              if (!load) return <td key={b} className="load-cell">-</td>;
                              const pct = load.loadPercentage;
                              const statusClass = pct > 100 ? 'overload' : pct > 85 ? 'warning' : 'normal';
                              return (
                                <td key={b} className={`load-cell ${statusClass}`}>
                                  <div className="load-bar-bg">
                                    <div className="load-bar-fill" style={{ width: `${Math.min(pct, 100)}%` }}></div>
                                  </div>
                                  <span className="load-text">{pct.toFixed(0)}%</span>
                                </td>
                              );
                            })}
                          </tr>
                        ))}
                      </>
                    )}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state">
                <div className="empty-icon-wrapper">
                  <Layers size={48} />
                </div>
                <p>尚未選擇或建立計劃</p>
                <span>從左側列表選擇一個計劃版本以開始分析與決策</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Traceability Modal */}
      {showTraceability && (
        <div className="modal-overlay" onClick={() => setShowTraceability(null)}>
          <div className="trace-modal glass-card animate-up" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3><Eye size={20} className="text-primary" /> 需求來源溯源 (Traceability)</h3>
              <button className="btn-close" onClick={() => setShowTraceability(null)}><X size={20} /></button>
            </div>
            <div className="modal-body">
              <div className="sku-summary mb-4">
                <span className="sku-num">{showTraceability.skuNumber}</span>
                <span className="sku-name ml-2">{showTraceability.skuName}</span>
                <span className="date-tag ml-auto">{new Date(showTraceability.bucketStart).toLocaleDateString()}</span>
              </div>
              <div className="trace-table-wrapper">
                <table className="trace-table">
                  <thead>
                    <tr><th>來源單號</th><th>需求數量</th><th>需求類型</th><th>狀態</th></tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td className="font-mono text-primary">SO-20240422-001</td>
                      <td className="font-bold">450</td>
                      <td>正式訂單</td>
                      <td><span className="pill-emerald">已沖銷</span></td>
                    </tr>
                    <tr>
                      <td className="font-mono text-primary">FC-2024-Q2-05</td>
                      <td className="font-bold">120</td>
                      <td>預測需求</td>
                      <td><span className="pill-amber">待沖銷</span></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-primary" onClick={() => setShowTraceability(null)}>確認</button>
            </div>
          </div>
        </div>
      )}

      {/* Create MPS Modal */}
      <FormModal 
        open={showCreateMps}
        title="建立戰略生產計劃 (MPS)"
        fields={[
          { name: 'planName', label: '計劃名稱', type: 'text', required: true, defaultValue: `${new Date().getMonth() + 1}月滾動規劃 - ${new Date().toLocaleDateString()}` },
          { name: 'horizonStart', label: '規劃起點', type: 'date', required: true, defaultValue: new Date().toISOString().split('T')[0] },
          { name: 'horizonEnd', label: '規劃終點', type: 'date', required: true, defaultValue: new Date(Date.now() + 60 * 86400000).toISOString().split('T')[0] },
          { name: 'timeBucketType', label: '規劃時格', type: 'select', options: [{value:'DAY', label:'逐日 (Daily)'}, {value:'WEEK', label:'逐週 (Weekly)'}, {value:'MONTH', label:'逐月 (Monthly)'}], defaultValue: 'WEEK' },
          { name: 'baseOn', label: '初始策略', type: 'select', options: [{value:'FORECAST', label:'預測導向'}, {value:'SALES_ORDER', label:'訂單導向'}, {value:'MIXED', label:'混合平衡'}], defaultValue: 'MIXED' }
        ]}
        onClose={() => setShowCreateMps(false)}
        onSubmit={async (values: any) => {
          try {
            await mpsApi.create({ ...values, lines: [] });
            setShowCreateMps(false);
            loadHeaders();
          } catch (err: any) {
            alert('建立失敗: ' + err.message);
          }
        }}
      />
    </div>
  );
};
