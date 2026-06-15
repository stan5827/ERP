import React, { useState, useEffect } from 'react';
import { Search, Plus, AlertTriangle, CheckCircle, Clock, Wrench, Package, Truck, Calendar, Activity, Play, XCircle, Loader2, X } from 'lucide-react';
import { equipmentApi, workCentersApi, suppliersApi, api } from '../api/api';
import './TpmMaintenance.css';

interface Equipment {
  id: string;
  equipmentCode: string;
  equipmentName: string;
  equipmentType: string;
  workCenterName?: string;
  supplierName?: string;
  status: string;
  maintenanceStatus?: string;
  lastMaintenanceDate?: string;
  nextMaintenanceDate?: string;
  workCenterId?: string;
  supplierId?: string;
}

interface MaintenanceRecord {
  id: string;
  recordNumber: string;
  equipmentCode: string;
  equipmentName: string;
  maintenanceType: string;
  startTime: string;
  endTime?: string;
  performedBy?: string;
  result: string;
  laborHours: number;
  partsCost: number;
  totalCost: number;
}

interface DowntimeRecord {
  id: string;
  downtimeNumber: string;
  equipmentCode: string;
  equipmentName: string;
  downtimeType: string;
  startTime: string;
  endTime?: string;
  cause?: string;
  status: string;
}

interface WorkCenter {
  id: string;
  workCenterCode: string;
  workCenterName: string;
}

interface Supplier {
  id: string;
  supplierCode: string;
  supplierName: string;
}

const equipmentTypeConfig: Record<string, { label: string; color: string }> = {
  machine: { label: '加工設備', color: 'var(--cobalt-400)' },
  inspection: { label: '檢測設備', color: 'var(--emerald-400)' },
  assembly: { label: '組裝設備', color: 'var(--amber-400)' },
  others: { label: '其他設備', color: 'var(--slate-400)' },
};

const maintenanceTypeConfig: Record<string, { label: string; cls: string }> = {
  preventive: { label: '預防性', cls: 'active' },
  corrective: { label: '修復性', cls: 'warning' },
  predictive: { label: '預測性', cls: 'info' },
  emergency: { label: '緊急', cls: 'danger' },
};

type DialogType = 'equipment' | 'maintenance' | 'downtime' | null;

export const TpmMaintenance: React.FC = () => {
  const [search, setSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'equipment' | 'maintenance' | 'downtime'>('equipment');
  const [selectedEquipment, setSelectedEquipment] = useState<Equipment | null>(null);
  const [equipment, setEquipment] = useState<Equipment[]>([]);
  const [maintenance, setMaintenance] = useState<MaintenanceRecord[]>([]);
  const [downtime, setDowntime] = useState<DowntimeRecord[]>([]);
  const [stats, setStats] = useState<any>({});
  const [loading, setLoading] = useState(true);
  
  const [openDialog, setOpenDialog] = useState<DialogType>(null);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadData();
  }, [activeTab]);

  useEffect(() => {
    loadWorkCenters();
    loadSuppliers();
  }, []);

  const loadSuppliers = async () => {
    try {
      const res = await suppliersApi.list({ pageSize: '999' });
      const data = (res.data?.data as Supplier[]) || [];
      setSuppliers(data);
    } catch (err) {
      console.error('Failed to load suppliers:', err);
    }
  };

  const loadWorkCenters = async () => {
    try {
      const res = await workCentersApi.list({ pageSize: '999' });
      const data = (res.data?.data as WorkCenter[]) || [];
      setWorkCenters(data);
    } catch (err) {
      console.error('Failed to load work centers:', err);
    }
  };

  const loadData = async () => {
    setLoading(true);
    try {
      if (activeTab === 'equipment') {
        const [eqRes, statsRes] = await Promise.all([
          equipmentApi.list(),
          equipmentApi.stats(),
        ]);
        setEquipment(eqRes.data?.data ?? []);
        setStats(statsRes.data?.data ?? {});
      } else if (activeTab === 'maintenance') {
        const res = await equipmentApi.maintenanceRecords();
        setMaintenance(res.data?.data ?? []);
      } else if (activeTab === 'downtime') {
        const res = await equipmentApi.downtime();
        setDowntime(res.data?.data ?? []);
      }
    } catch (err) {
      console.error('Failed to load TPM data:', err);
    } finally {
      setLoading(false);
    }
  };

  const filteredEquipment = equipment.filter(e =>
    e.equipmentCode.includes(search) || e.equipmentName.includes(search)
  );

  const displayStats = [
    { label: '設備總數', value: stats.totalCount ?? equipment.length, icon: Package, color: 'var(--cobalt-400)' },
    { label: '待維修', value: stats.repairCount ?? equipment.filter(e => e.status === 'repair').length, icon: AlertTriangle, color: 'var(--crimson-400)' },
    { label: '即將到期', value: stats.warningCount ?? equipment.filter(e => e.maintenanceStatus === 'warning').length, icon: Clock, color: 'var(--amber-400)' },
    { label: '本月維修成本', value: stats.monthlyMaintenanceCost != null ? `$${stats.monthlyMaintenanceCost.toLocaleString()}` : '$0', icon: Activity, color: 'var(--emerald-400)' },
  ];

  const handleCreateEquipment = async (data: any) => {
    setSubmitting(true);
    try {
      await equipmentApi.create(data);
      await loadData();
      setOpenDialog(null);
      alert('設備建立成功！');
    } catch (err: any) {
      alert('建立失敗：' + (err.message || String(err)));
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateMaintenance = async (data: any) => {
    setSubmitting(true);
    try {
      await equipmentApi.createMaintenanceRecord(data);
      await loadData();
      setOpenDialog(null);
      alert('維修記錄建立成功！');
    } catch (err: any) {
      alert('建立失敗：' + (err.message || String(err)));
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateDowntime = async (data: any) => {
    setSubmitting(true);
    try {
      await equipmentApi.createDowntime(data);
      await loadData();
      setOpenDialog(null);
      alert('異常停機建立成功！');
    } catch (err: any) {
      alert('建立失敗：' + (err.message || String(err)));
    } finally {
      setSubmitting(false);
    }
  };

  const getAddButton = () => {
    if (activeTab === 'equipment') {
      return (
        <button className="btn btn-primary btn-sm" onClick={() => setOpenDialog('equipment')}>
          <Plus size={13} />新增設備
        </button>
      );
    } else if (activeTab === 'maintenance') {
      return (
        <button className="btn btn-primary btn-sm" onClick={() => setOpenDialog('maintenance')}>
          <Plus size={13} />新增維護記錄
        </button>
      );
    } else if (activeTab === 'downtime') {
      return (
        <button className="btn btn-primary btn-sm" onClick={() => setOpenDialog('downtime')}>
          <Plus size={13} />新增異常停機
        </button>
      );
    }
    return null;
  };

  if (loading) {
    return (
      <div className="tpm-page">
        <div className="glass-card" style={{ textAlign: 'center', padding: 40 }}>
          <Loader2 size={24} style={{ animation: 'spin 1s linear infinite', color: 'var(--slate-500)' }} />
          <div style={{ fontSize: 12, color: 'var(--slate-500)', marginTop: 8 }}>載入設備資料...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="tpm-page">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">設備維護管理 TPM</h2>
          <p className="module-page-desc">設備台帳 · 維護記錄 · 異常停機</p>
        </div>
        <div className="module-page-actions">
          {getAddButton()}
        </div>
      </div>

      <div className="module-stat-grid">
        {displayStats.map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div className="module-stat-icon" style={{ background: `${s.color}15`, color: s.color }}>
              <s.icon size={16} />
            </div>
            <div>
              <div className="module-stat-label">{s.label}</div>
              <div className="module-stat-value" style={{ color: s.color }}>{s.value}</div>
            </div>
          </div>
        ))}
      </div>

      <div className="glass-card module-filter-bar">
        <div className="filter-search">
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋設備編號、名稱..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        <div className="filter-tabs">
          <button className={`tab-item ${activeTab === 'equipment' ? 'active' : ''}`} onClick={() => setActiveTab('equipment')}>設備台帳</button>
          <button className={`tab-item ${activeTab === 'maintenance' ? 'active' : ''}`} onClick={() => setActiveTab('maintenance')}>維護記錄</button>
          <button className={`tab-item ${activeTab === 'downtime' ? 'active' : ''}`} onClick={() => setActiveTab('downtime')}>異常停機</button>
        </div>
      </div>

      <div className="glass-card" style={{ overflow: 'hidden' }}>
        {activeTab === 'equipment' && (
          <table className="data-table">
            <thead>
              <tr>
                <th>設備編號</th>
                <th>設備名稱</th>
                <th>類型</th>
                <th>所屬工段</th>
                <th>供應商</th>
                <th>狀態</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {filteredEquipment.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無設備資料</td></tr>
              ) : filteredEquipment.map(e => {
                const statusMap: Record<string, { label: string; cls: string }> = {
                  active: { label: '正常', cls: 'active' },
                  maintenance: { label: '維護中', cls: 'warning' },
                  repair: { label: '維修中', cls: 'danger' },
                  inactive: { label: '停用', cls: 'neutral' },
                };
                const sc = statusMap[e.status] ?? { label: e.status, cls: 'neutral' };
                const tc = equipmentTypeConfig[e.equipmentType] ?? equipmentTypeConfig.others;
                return (
                  <tr key={e.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{e.equipmentCode}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{e.equipmentName}</td>
                    <td><span style={{ color: tc.color, fontSize: 12 }}>{tc.label}</span></td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{e.workCenterName ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{e.supplierName ?? '—'}</td>
                    <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button className="icon-action-btn" title="查看"><Clock size={13} /></button>
                        <button className="icon-action-btn" title="維護記錄"><Wrench size={13} /></button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}

        {activeTab === 'maintenance' && (
          <table className="data-table">
            <thead>
              <tr>
                <th>維護單號</th>
                <th>設備</th>
                <th>維護類型</th>
                <th>開始時間</th>
                <th>結束時間</th>
                <th>人員</th>
                <th>結果</th>
                <th>工時</th>
                <th>成本</th>
              </tr>
            </thead>
            <tbody>
              {maintenance.length === 0 ? (
                <tr><td colSpan={9} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無維護記錄</td></tr>
              ) : maintenance.map(m => {
                const mc = maintenanceTypeConfig[m.maintenanceType] ?? { label: m.maintenanceType, cls: 'neutral' };
                const rc = m.result === 'completed' ? { label: '完成', cls: 'active' } : m.result === 'pending' ? { label: '待處理', cls: 'warning' } : { label: '取消', cls: 'neutral' };
                return (
                  <tr key={m.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{m.recordNumber}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{m.equipmentName}</td>
                    <td><span className={`status-badge status-${mc.cls}`}>{mc.label}</span></td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{m.startTime}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{m.endTime ?? '—'}</td>
                    <td style={{ fontSize: 12 }}>{m.performedBy ?? '—'}</td>
                    <td><span className={`status-badge status-${rc.cls}`}>{rc.label}</span></td>
                    <td className="mono" style={{ fontSize: 12 }}>{m.laborHours}h</td>
                    <td className="mono" style={{ fontSize: 12, color: 'var(--emerald-300)' }}>${m.totalCost.toLocaleString()}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}

        {activeTab === 'downtime' && (
          <table className="data-table">
            <thead>
              <tr>
                <th>停機單號</th>
                <th>設備</th>
                <th>停機類型</th>
                <th>開始時間</th>
                <th>結束時間</th>
                <th>原因</th>
                <th>狀態</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {downtime.length === 0 ? (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無異常停機記錄</td></tr>
              ) : downtime.map(d => {
                const typeMap: Record<string, { label: string; cls: string }> = {
                  breakdown: { label: '故障停機', cls: 'danger' },
                  setup: { label: '換模停機', cls: 'info' },
                  maintenance: { label: '保養停機', cls: 'warning' },
                };
                const statusMap: Record<string, { label: string; cls: string }> = {
                  open: { label: '待處理', cls: 'danger' },
                  in_progress: { label: '處理中', cls: 'warning' },
                  closed: { label: '已結案', cls: 'active' },
                };
                const tc = typeMap[d.downtimeType] ?? { label: d.downtimeType, cls: 'neutral' };
                const sc = statusMap[d.status];
                return (
                  <tr key={d.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{d.downtimeNumber}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{d.equipmentName}</td>
                    <td><span className={`status-badge status-${tc.cls}`}>{tc.label}</span></td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{d.startTime}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{d.endTime ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{d.cause ?? '—'}</td>
                    <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {d.status === 'open' && <button className="icon-action-btn" title="開始處理"><Play size={13} /></button>}
                        {d.status === 'in_progress' && <button className="icon-action-btn" title="完成"><CheckCircle size={13} /></button>}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      <style>{`
        .module-filter-bar { display: flex; align-items: center; gap: 12px; padding: 10px 14px; }
        .filter-search { display: flex; align-items: center; gap: 8px; min-width: 220px; }
        .filter-tabs { display: flex; gap: 2px; background: var(--navy-850); padding: 3px; border-radius: var(--radius-md); flex: 1; }
        .tab-item { padding: 5px 12px; font-size: 12px; border-radius: 6px; }
        .icon-action-btn { width: 26px; height: 26px; display: flex; align-items: center; justify-content: center; background: transparent; border: none; border-radius: var(--radius-sm); color: var(--slate-500); cursor: pointer; transition: all var(--transition-fast); }
        .icon-action-btn:hover { background: rgba(45, 110, 245, 0.08); color: var(--cobalt-300); }
        @keyframes spin { to { transform: rotate(360deg); } }
        .dialog-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.6); display: flex; align-items: center; justify-content: center; z-index: 1000; }
        .dialog-content { background: var(--navy-900); border: 1px solid var(--navy-700); border-radius: var(--radius-md); width: 90%; max-width: 500px; max-height: 90vh; overflow-y: auto; }
        .dialog-header { display: flex; align-items: center; justify-content: space-between; padding: 16px; border-bottom: 1px solid var(--navy-700); }
        .dialog-title { font-size: 16px; font-weight: 600; color: var(--slate-100); }
        .dialog-close { background: none; border: none; color: var(--slate-500); cursor: pointer; padding: 4px; }
        .dialog-close:hover { color: var(--slate-300); }
        .dialog-body { padding: 16px; }
        .form-group { margin-bottom: 16px; }
        .form-label { display: block; font-size: 12px; font-weight: 500; color: var(--slate-400); margin-bottom: 6px; }
        .form-input { width: 100%; padding: 8px 12px; background: var(--navy-850); border: 1px solid var(--navy-600); border-radius: var(--radius-sm); color: var(--slate-200); font-size: 13px; box-sizing: border-box; }
        .form-input:focus { outline: none; border-color: var(--cobalt-500); }
        .form-select { width: 100%; padding: 8px 12px; background: var(--navy-850); border: 1px solid var(--navy-600); border-radius: var(--radius-sm); color: var(--slate-200); font-size: 13px; box-sizing: border-box; }
        .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
        .dialog-footer { display: flex; justify-content: flex-end; gap: 8px; padding: 16px; border-top: 1px solid var(--navy-700); }
      `}</style>

      {openDialog && (
        <Dialog
          type={openDialog}
          onClose={() => setOpenDialog(null)}
          equipment={equipment}
          workCenters={workCenters}
          suppliers={suppliers}
          submitting={submitting}
          onSubmitEquipment={handleCreateEquipment}
          onSubmitMaintenance={handleCreateMaintenance}
          onSubmitDowntime={handleCreateDowntime}
        />
      )}
    </div>
  );
};

interface DialogProps {
  type: 'equipment' | 'maintenance' | 'downtime';
  onClose: () => void;
  equipment: Equipment[];
  workCenters: WorkCenter[];
  suppliers: Supplier[];
  submitting: boolean;
  onSubmitEquipment: (data: Record<string, unknown>) => Promise<void>;
  onSubmitMaintenance: (data: Record<string, unknown>) => Promise<void>;
  onSubmitDowntime: (data: Record<string, unknown>) => Promise<void>;
}

function Dialog({ type, onClose, equipment, workCenters, suppliers, submitting, onSubmitEquipment, onSubmitMaintenance, onSubmitDowntime }: DialogProps) {
  const [equipmentForm, setEquipmentForm] = useState({
    equipmentCode: `EQ-${Date.now()}`,
    equipmentName: '',
    equipmentType: 'machine',
    workCenterId: '',
    serialNumber: '',
    model: '',
    manufacturer: '',
    supplierId: '',
    purchaseDate: '',
    purchasePrice: ''
  });

  const [maintenanceForm, setMaintenanceForm] = useState({
    equipmentId: '',
    maintenanceType: 'preventive',
    startTime: new Date().toISOString().slice(0, 16),
    endTime: '',
    performedBy: '',
    workDescription: '',
    laborHours: '0',
    partsCost: '0',
    result: 'completed'
  });

  const [downtimeForm, setDowntimeForm] = useState({
    equipmentId: '',
    downtimeType: 'breakdown',
    startTime: new Date().toISOString().slice(0, 16),
    cause: '',
    description: '',
    reportedBy: ''
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (type === 'equipment') {
      await onSubmitEquipment({
        equipmentCode: equipmentForm.equipmentCode,
        equipmentName: equipmentForm.equipmentName,
        equipmentType: equipmentForm.equipmentType,
        workCenterId: equipmentForm.workCenterId || undefined,
        serialNumber: equipmentForm.serialNumber || undefined,
        model: equipmentForm.model || undefined,
        manufacturer: equipmentForm.manufacturer || undefined,
        supplierId: equipmentForm.supplierId || undefined,
        purchaseDate: equipmentForm.purchaseDate || undefined,
        purchasePrice: equipmentForm.purchasePrice ? parseFloat(equipmentForm.purchasePrice) : undefined
      });
    } else if (type === 'maintenance') {
      await onSubmitMaintenance({
        equipmentId: maintenanceForm.equipmentId,
        maintenanceType: maintenanceForm.maintenanceType,
        startTime: maintenanceForm.startTime,
        endTime: maintenanceForm.endTime || undefined,
        performedBy: maintenanceForm.performedBy || undefined,
        workDescription: maintenanceForm.workDescription || undefined,
        laborHours: parseFloat(maintenanceForm.laborHours) || 0,
        partsCost: parseFloat(maintenanceForm.partsCost) || 0,
        result: maintenanceForm.result
      });
    } else if (type === 'downtime') {
      await onSubmitDowntime({
        equipmentId: downtimeForm.equipmentId,
        downtimeType: downtimeForm.downtimeType,
        startTime: downtimeForm.startTime,
        cause: downtimeForm.cause || undefined,
        description: downtimeForm.description || undefined,
        reportedBy: downtimeForm.reportedBy || undefined
      });
    }
  };

  const titleMap = { equipment: '新增設備', maintenance: '新增維護記錄', downtime: '新增異常停機' };

  return (
    <div className="dialog-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="dialog-content">
        <div className="dialog-header">
          <span className="dialog-title">{titleMap[type]}</span>
          <button className="dialog-close" onClick={onClose}><X size={18} /></button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="dialog-body">
            {type === 'equipment' && (
              <>
                <div className="form-group">
                  <label className="form-label">設備編號 <span style={{ color: 'var(--crimson-400)' }}>*</span></label>
                  <input className="form-input" value={equipmentForm.equipmentCode} onChange={e => setEquipmentForm({ ...equipmentForm, equipmentCode: e.target.value })} required />
                </div>
                <div className="form-group">
                  <label className="form-label">設備名稱 <span style={{ color: 'var(--crimson-400)' }}>*</span></label>
                  <input className="form-input" value={equipmentForm.equipmentName} onChange={e => setEquipmentForm({ ...equipmentForm, equipmentName: e.target.value })} required />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">設備類型</label>
                    <select className="form-select" value={equipmentForm.equipmentType} onChange={e => setEquipmentForm({ ...equipmentForm, equipmentType: e.target.value })}>
                      <option value="machine">加工設備</option>
                      <option value="inspection">檢測設備</option>
                      <option value="assembly">組裝設備</option>
                      <option value="others">其他設備</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">所屬工段</label>
                    <select className="form-select" value={equipmentForm.workCenterId} onChange={e => setEquipmentForm({ ...equipmentForm, workCenterId: e.target.value })}>
                      <option value="">請選擇</option>
                      {workCenters.map(wc => (
                        <option key={wc.id} value={wc.id}>{wc.workCenterName}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">序號</label>
                    <input className="form-input" value={equipmentForm.serialNumber} onChange={e => setEquipmentForm({ ...equipmentForm, serialNumber: e.target.value })} />
                  </div>
                  <div className="form-group">
                    <label className="form-label">型號</label>
                    <input className="form-input" value={equipmentForm.model} onChange={e => setEquipmentForm({ ...equipmentForm, model: e.target.value })} />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">供應商</label>
                    <select className="form-select" value={equipmentForm.supplierId} onChange={e => setEquipmentForm({ ...equipmentForm, supplierId: e.target.value })}>
                      <option value="">請選擇</option>
                      {suppliers.map(s => (
                        <option key={s.id} value={s.id}>{s.supplierCode} - {s.supplierName}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">製造商</label>
                    <input className="form-input" value={equipmentForm.manufacturer} onChange={e => setEquipmentForm({ ...equipmentForm, manufacturer: e.target.value })} placeholder="設備製造商" />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">購買日期</label>
                    <input className="form-input" type="date" value={equipmentForm.purchaseDate} onChange={e => setEquipmentForm({ ...equipmentForm, purchaseDate: e.target.value })} />
                  </div>
                  <div className="form-group">
                    <label className="form-label">購買價格</label>
                    <input className="form-input" type="number" value={equipmentForm.purchasePrice} onChange={e => setEquipmentForm({ ...equipmentForm, purchasePrice: e.target.value })} />
                  </div>
                </div>
              </>
            )}

            {type === 'maintenance' && (
              <>
                <div className="form-group">
                  <label className="form-label">設備 <span style={{ color: 'var(--crimson-400)' }}>*</span></label>
                  <select className="form-select" value={maintenanceForm.equipmentId} onChange={e => setMaintenanceForm({ ...maintenanceForm, equipmentId: e.target.value })} required>
                    <option value="">請選擇設備</option>
                    {equipment.map(eq => (
                      <option key={eq.id} value={eq.id}>{eq.equipmentCode} - {eq.equipmentName}</option>
                    ))}
                  </select>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">維護類型</label>
                    <select className="form-select" value={maintenanceForm.maintenanceType} onChange={e => setMaintenanceForm({ ...maintenanceForm, maintenanceType: e.target.value })}>
                      <option value="preventive">預防性</option>
                      <option value="corrective">修復性</option>
                      <option value="predictive">預測性</option>
                      <option value="emergency">緊急</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">結果</label>
                    <select className="form-select" value={maintenanceForm.result} onChange={e => setMaintenanceForm({ ...maintenanceForm, result: e.target.value })}>
                      <option value="completed">完成</option>
                      <option value="pending">待處理</option>
                      <option value="cancelled">取消</option>
                    </select>
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">開始時間</label>
                    <input className="form-input" type="datetime-local" value={maintenanceForm.startTime} onChange={e => setMaintenanceForm({ ...maintenanceForm, startTime: e.target.value })} />
                  </div>
                  <div className="form-group">
                    <label className="form-label">結束時間</label>
                    <input className="form-input" type="datetime-local" value={maintenanceForm.endTime} onChange={e => setMaintenanceForm({ ...maintenanceForm, endTime: e.target.value })} />
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-label">執行人員</label>
                  <input className="form-input" value={maintenanceForm.performedBy} onChange={e => setMaintenanceForm({ ...maintenanceForm, performedBy: e.target.value })} />
                </div>
                <div className="form-group">
                  <label className="form-label">工作描述</label>
                  <textarea className="form-input" value={maintenanceForm.workDescription} onChange={e => setMaintenanceForm({ ...maintenanceForm, workDescription: e.target.value })} />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">工時 (小時)</label>
                    <input className="form-input" type="number" value={maintenanceForm.laborHours} onChange={e => setMaintenanceForm({ ...maintenanceForm, laborHours: e.target.value })} />
                  </div>
                  <div className="form-group">
                    <label className="form-label">零件費用</label>
                    <input className="form-input" type="number" value={maintenanceForm.partsCost} onChange={e => setMaintenanceForm({ ...maintenanceForm, partsCost: e.target.value })} />
                  </div>
                </div>
              </>
            )}

            {type === 'downtime' && (
              <>
                <div className="form-group">
                  <label className="form-label">設備 <span style={{ color: 'var(--crimson-400)' }}>*</span></label>
                  <select className="form-select" value={downtimeForm.equipmentId} onChange={e => setDowntimeForm({ ...downtimeForm, equipmentId: e.target.value })} required>
                    <option value="">請選擇設備</option>
                    {equipment.map(eq => (
                      <option key={eq.id} value={eq.id}>{eq.equipmentCode} - {eq.equipmentName}</option>
                    ))}
                  </select>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">停機類型</label>
                    <select className="form-select" value={downtimeForm.downtimeType} onChange={e => setDowntimeForm({ ...downtimeForm, downtimeType: e.target.value })}>
                      <option value="breakdown">故障停機</option>
                      <option value="setup">換模停機</option>
                      <option value="maintenance">保養停機</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">開始時間</label>
                    <input className="form-input" type="datetime-local" value={downtimeForm.startTime} onChange={e => setDowntimeForm({ ...downtimeForm, startTime: e.target.value })} />
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-label">原因</label>
                  <input className="form-input" value={downtimeForm.cause} onChange={e => setDowntimeForm({ ...downtimeForm, cause: e.target.value })} />
                </div>
                <div className="form-group">
                  <label className="form-label">描述</label>
                  <textarea className="form-input" value={downtimeForm.description} onChange={e => setDowntimeForm({ ...downtimeForm, description: e.target.value })} />
                </div>
                <div className="form-group">
                  <label className="form-label">報告人</label>
                  <input className="form-input" value={downtimeForm.reportedBy} onChange={e => setDowntimeForm({ ...downtimeForm, reportedBy: e.target.value })} />
                </div>
              </>
            )}
          </div>
          <div className="dialog-footer">
            <button type="button" className="btn btn-outline" onClick={onClose}>取消</button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>{submitting ? '儲存中...' : '儲存'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
