import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Search, Package, QrCode, Camera, CheckCircle, XCircle, ArrowRight, RotateCcw, Download, Printer, Box, Truck, History, Loader2 } from 'lucide-react';
import { barcodeApi } from '../api/api';
import './BarcodeSystem.css';

interface BarcodeRecord {
  id: string;
  barcode: string;
  barcodeType: string;
  refType?: string;
  refId?: string;
  refName?: string;
  locationCode?: string;
  qty?: number;
  lastScanTime: string;
  status: string;
}

interface ScanHistory {
  id: string;
  barcode: string;
  barcodeType: string;
  action: string;
  result: string;
  operator?: string;
  timestamp: string;
  details?: string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (isRecord(error)) {
    const response = isRecord(error.response) ? error.response : undefined;
    const data = isRecord(response?.data) ? response.data : undefined;
    if (typeof data?.message === 'string') return data.message;
    if (typeof error.message === 'string') return error.message;
  }
  return error instanceof Error ? error.message : fallback;
}

const barcodeTypeConfig: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
  sku: { label: '品項條碼', color: 'var(--cobalt-400)', icon: <Package size={12} /> },
  lot: { label: '批號條碼', color: 'var(--emerald-400)', icon: <Box size={12} /> },
  location: { label: '庫位條碼', color: 'var(--amber-400)', icon: <QrCode size={12} /> },
  box: { label: '箱號條碼', color: 'var(--purple-400)', icon: <Package size={12} /> },
  pallet: { label: '棧板條碼', color: 'var(--teal-400)', icon: <Box size={12} /> },
};

export const BarcodeSystem: React.FC = () => {
  const [scanInput, setScanInput] = useState('');
  const [activeTab, setActiveTab] = useState<'scan' | 'generate' | 'history'>('scan');
  const [scanResult, setScanResult] = useState<BarcodeRecord | null>(null);
  const [scanError, setScanError] = useState('');
  const [isScanning, setIsScanning] = useState(false);
  const [records, setRecords] = useState<BarcodeRecord[]>([]);
  const [history, setHistory] = useState<ScanHistory[]>([]);
  const [loading, setLoading] = useState(true);
  const inputRef = useRef<HTMLInputElement>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      if (activeTab === 'scan') {
        const res = await barcodeApi.records();
        setRecords(res.data?.data ?? []);
      } else if (activeTab === 'history') {
        const res = await barcodeApi.history();
        setHistory((res.data?.data?.data ?? []).map(record => ({
          ...record,
          operator: record.operatorName,
        })));
      }
    } catch (err) {
      console.error('Failed to load barcode data:', err);
    } finally {
      setLoading(false);
    }
  }, [activeTab]);

  useEffect(() => {
    loadData();
  }, [activeTab, loadData]);

  useEffect(() => {
    if (activeTab === 'scan' && inputRef.current) {
      inputRef.current.focus();
    }
  }, [activeTab]);

  const handleScan = async () => {
    if (!scanInput.trim()) return;
    setIsScanning(true);
    setScanError('');
    setScanResult(null);

    try {
      const res = await barcodeApi.lookup(scanInput.trim());
      if (res.data?.data) {
        setScanResult(res.data.data);
      } else {
        setScanResult(null);
        setScanError('條碼不存在或已失效');
      }
    } catch (err: unknown) {
      setScanResult(null);
      setScanError(getErrorMessage(err, '查詢失敗，請稍後再試'));
    } finally {
      setIsScanning(false);
    }
  };

  const handleGenerate = async (type: string) => {
    try {
      const res = await barcodeApi.generate({ barcodeType: type });
      if (res.data?.data) {
        alert(`條碼生成成功：${res.data.data.barcode}`);
        await loadData();
      }
    } catch (err: unknown) {
      alert(`生成失敗：${getErrorMessage(err, '發生未知錯誤')}`);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleScan();
    }
  };

  const stats = [
    { label: '今日掃描次數', value: history.length, icon: <Camera size={16} />, color: 'var(--cobalt-400)' },
    { label: '成功掃描', value: history.filter(h => h.result === 'success').length, icon: <CheckCircle size={16} />, color: 'var(--emerald-400)' },
    { label: '掃描失敗', value: history.filter(h => h.result === 'error').length, icon: <XCircle size={16} />, color: 'var(--crimson-400)' },
    { label: '有效條碼', value: records.filter(r => r.status === 'active').length, icon: <QrCode size={16} />, color: 'var(--amber-400)' },
  ];

  if (loading) {
    return (
      <div className="barcode-page">
        <div className="glass-card" style={{ textAlign: 'center', padding: 40 }}>
          <Loader2 size={24} style={{ animation: 'spin 1s linear infinite', color: 'var(--slate-500)' }} />
          <div style={{ fontSize: 12, color: 'var(--slate-500)', marginTop: 8 }}>載入條碼資料...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="barcode-page">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">條碼系統</h2>
          <p className="module-page-desc">條碼掃描 · 條碼生成 · 追溯查詢</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Printer size={13} />列印</button>
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
        </div>
      </div>

      <div className="module-stat-grid">
        {stats.map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div className="module-stat-icon" style={{ background: `${s.color}15`, color: s.color }}>
              {s.icon}
            </div>
            <div>
              <div className="module-stat-label">{s.label}</div>
              <div className="module-stat-value" style={{ color: s.color }}>{s.value}</div>
            </div>
          </div>
        ))}
      </div>

      <div className="glass-card module-filter-bar">
        <div className="filter-tabs">
          <button className={`tab-item ${activeTab === 'scan' ? 'active' : ''}`} onClick={() => setActiveTab('scan')}>條碼掃描</button>
          <button className={`tab-item ${activeTab === 'generate' ? 'active' : ''}`} onClick={() => setActiveTab('generate')}>條碼生成</button>
          <button className={`tab-item ${activeTab === 'history' ? 'active' : ''}`} onClick={() => setActiveTab('history')}>掃描紀錄</button>
        </div>
      </div>

      <div className="glass-card" style={{ padding: 0 }}>
        {activeTab === 'scan' && (
          <div style={{ padding: 24 }}>
            <div className="barcode-scan-controls">
              <div className="barcode-scan-field">
                <input
                  ref={inputRef}
                  className="barcode-scan-input"
                  placeholder="請掃描或輸入條碼..."
                  value={scanInput}
                  onChange={e => setScanInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                />
                <QrCode className="barcode-scan-icon" size={16} aria-hidden="true" />
              </div>
              <button className="btn btn-primary barcode-scan-button" onClick={handleScan} disabled={isScanning}>
                {isScanning ? <RotateCcw size={14} className="spin" /> : <Camera size={14} />}
                {isScanning ? '掃描中...' : '掃描'}
              </button>
            </div>

            {scanError && (
              <div className="scan-error">
                <XCircle size={16} />
                <span>{scanError}</span>
              </div>
            )}

            {scanResult && (
              <div className="scan-result">
                <div className="scan-result-header">
                  <CheckCircle size={20} />
                  <span>掃描成功</span>
                </div>
                <div className="scan-result-content">
                  <div className="result-row">
                    <span className="result-label">條碼</span>
                    <span className="result-value mono">{scanResult.barcode}</span>
                  </div>
                  <div className="result-row">
                    <span className="result-label">類型</span>
                    <span className="result-value">
                      <span className="type-badge" style={{ background: `${barcodeTypeConfig[scanResult.barcodeType].color}20`, color: barcodeTypeConfig[scanResult.barcodeType].color }}>
                        {barcodeTypeConfig[scanResult.barcodeType].icon}
                        {barcodeTypeConfig[scanResult.barcodeType].label}
                      </span>
                    </span>
                  </div>
                  {scanResult.refName && (
                    <div className="result-row">
                      <span className="result-label">關聯</span>
                      <span className="result-value">{scanResult.refName}</span>
                    </div>
                  )}
                  {scanResult.locationCode && (
                    <div className="result-row">
                      <span className="result-label">庫位</span>
                      <span className="result-value">{scanResult.locationCode}</span>
                    </div>
                  )}
                  {scanResult.qty !== undefined && (
                    <div className="result-row">
                      <span className="result-label">數量</span>
                      <span className="result-value">{scanResult.qty}</span>
                    </div>
                  )}
                  <div className="result-row">
                    <span className="result-label">狀態</span>
                    <span className={`status-badge status-${scanResult.status === 'active' ? 'active' : scanResult.status === 'used' ? 'warning' : 'neutral'}`}>
                      {scanResult.status === 'active' ? '有效' : scanResult.status === 'used' ? '已使用' : '已失效'}
                    </span>
                  </div>
                </div>
                <div className="scan-result-actions">
                  <button className="btn btn-ghost btn-sm" disabled title="功能開發中"><ArrowRight size={13} />入庫</button>
                  <button className="btn btn-ghost btn-sm" disabled title="功能開發中"><ArrowRight size={13} />出庫</button>
                  <button className="btn btn-ghost btn-sm" disabled title="功能開發中"><ArrowRight size={13} />調撥</button>
                  <button className="btn btn-ghost btn-sm" disabled title="功能開發中"><History size={13} />歷史</button>
                </div>
              </div>
            )}

            <div style={{ marginTop: 32 }}>
              <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 12 }}>快速掃描紀錄</div>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>條碼</th>
                    <th>類型</th>
                    <th>關聯品項</th>
                    <th>庫位</th>
                    <th>最後掃描</th>
                    <th>狀態</th>
                  </tr>
                </thead>
                <tbody>
                  {records.length === 0 ? (
                    <tr><td colSpan={6} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無條碼記錄</td></tr>
                  ) : records.slice(0, 5).map(r => (
                    <tr key={r.id} onClick={() => { setScanInput(r.barcode); handleScan(); }} style={{ cursor: 'pointer' }}>
                      <td className="mono" style={{ color: 'var(--theme-text)', fontSize: '13px', fontWeight: 600 }}>{r.barcode}</td>
                      <td>
                        <span style={{ color: r.barcodeType === 'sku' ? 'var(--theme-text)' : barcodeTypeConfig[r.barcodeType].color, fontSize: 13, fontWeight: 600 }}>
                          {barcodeTypeConfig[r.barcodeType].label}
                        </span>
                      </td>
                      <td style={{ fontSize: 12 }}>{r.refName ?? '—'}</td>
                      <td className="mono" style={{ fontSize: 11 }}>{r.locationCode ?? '—'}</td>
                      <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{r.lastScanTime}</td>
                      <td><span className={`status-badge status-${r.status === 'active' ? 'active' : r.status === 'used' ? 'warning' : 'neutral'}`}>{r.status === 'active' ? '有效' : r.status === 'used' ? '已使用' : '失效'}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'generate' && (
          <div style={{ padding: 24 }}>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 16 }}>
              {[
                { type: 'sku', label: '品項條碼', prefix: 'SKU-', desc: '為現有品項生成條碼' },
                { type: 'lot', label: '批號條碼', prefix: 'LOT-', desc: '為原料/成品批次生成條碼' },
                { type: 'location', label: '庫位條碼', prefix: 'LOC-', desc: '為庫位生成條碼' },
                { type: 'box', label: '箱號條碼', prefix: 'BOX-', desc: '為包裝箱生成條碼' },
                { type: 'pallet', label: '棧板條碼', prefix: 'PLT-', desc: '為棧板生成條碼' },
                { type: 'workorder', label: '工單條碼', prefix: 'WO-', desc: '為生產工單生成條碼' },
              ].map(item => (
                <div key={item.type} className="generate-card">
                  <div className="generate-card-header">
                    <QrCode size={20} />
                    <span>{item.label}</span>
                  </div>
                  <div className="generate-card-desc">{item.desc}</div>
                  <div className="generate-card-preview">
                    <span className="mono">{item.prefix}{Date.now().toString().slice(-6)}</span>
                  </div>
                  <button className="btn btn-primary btn-sm" style={{ width: '100%' }} onClick={() => handleGenerate(item.type)}>生成條碼</button>
                </div>
              ))}
            </div>
          </div>
        )}

        {activeTab === 'history' && (
          <table className="data-table">
            <thead>
              <tr>
                <th>時間</th>
                <th>條碼</th>
                <th>類型</th>
                <th>動作</th>
                <th>結果</th>
                <th>人員</th>
                <th>詳情</th>
              </tr>
            </thead>
            <tbody>
              {history.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無掃描紀錄</td></tr>
              ) : history.map(h => {
                const actionMap: Record<string, { label: string; icon: React.ReactNode }> = {
                  receive: { label: '入庫', icon: <Truck size={12} /> },
                  issue: { label: '出庫', icon: <Package size={12} /> },
                  transfer: { label: '調撥', icon: <ArrowRight size={12} /> },
                  inventory: { label: '盤點', icon: <History size={12} /> },
                  lookup: { label: '查詢', icon: <Search size={12} /> },
                };
                const action = actionMap[h.action];
                return (
                  <tr key={h.id}>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{h.timestamp}</td>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{h.barcode}</td>
                    <td><span style={{ fontSize: 12 }}>{h.barcodeType}</span></td>
                    <td><span className="action-badge">{action?.icon}{action?.label}</span></td>
                    <td><span className={`status-badge status-${h.result === 'success' ? 'active' : 'danger'}`}>{h.result === 'success' ? '成功' : '失敗'}</span></td>
                    <td style={{ fontSize: 12 }}>{h.operator ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{h.details ?? '—'}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      <style>{`
        .module-filter-bar { display: flex; align-items: center; gap: 12px; padding: 10px 14px; }
        .filter-tabs { display: flex; gap: 2px; background: var(--navy-850); padding: 3px; border-radius: var(--radius-md); flex: 1; }
        .tab-item { padding: 5px 12px; font-size: 12px; border-radius: 6px; }
        .scan-error { display: flex; align-items: center; gap: 8px; padding: 12px 16px; background: rgba(239, 68, 68, 0.1); border: 1px solid var(--crimson-700); border-radius: var(--radius-md); color: var(--crimson-300); margin-bottom: 16px; }
        .scan-result { background: var(--navy-850); border: 1px solid var(--emerald-700); border-radius: var(--radius-lg); overflow: hidden; }
        .scan-result-header { display: flex; align-items: center; gap: 8px; padding: 12px 16px; background: var(--emerald-900); color: var(--emerald-300); font-weight: 600; }
        .scan-result-content { padding: 16px; display: flex; flex-direction: column; gap: 10px; }
        .result-row { display: flex; align-items: center; justify-content: space-between; }
        .result-label { font-size: 12px; color: var(--slate-500); }
        .result-value { font-size: 14px; font-weight: 500; color: var(--slate-200); }
        .type-badge { display: inline-flex; align-items: center; gap: 4px; padding: 3px 8px; border-radius: 4px; font-size: 12px; font-weight: 500; }
        .scan-result-actions { display: flex; gap: 8px; padding: 12px 16px; border-top: 1px solid var(--navy-800); background: var(--navy-900); }
        .generate-card { padding: 16px; background: var(--navy-850); border: 1px solid var(--navy-800); border-radius: var(--radius-lg); }
        .generate-card-header { display: flex; align-items: center; gap: 8px; color: var(--cobalt-300); font-weight: 600; margin-bottom: 8px; }
        .generate-card-desc { font-size: 12px; color: 'var(--slate-500)'; margin-bottom: 12px; }
        .generate-card-preview { padding: 12px; background: var(--navy-900); border-radius: var(--radius-md); text-align: center; color: var(--slate-300); font-size: 14px; margin-bottom: 12px; }
        .action-badge { display: inline-flex; align-items: center; gap: 4px; padding: 3px 8px; background: var(--navy-800); border-radius: 4px; font-size: 12px; color: var(--slate-300); }
        @keyframes spin { to { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
};
