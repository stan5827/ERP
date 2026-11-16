import React, { useState, useEffect } from 'react';
import {
  Search, Megaphone, Eye, Loader2, Pin, AlertOctagon, Plus,
  Package, Factory, Activity, Truck, CheckSquare,
  RefreshCw, Calendar, Zap
} from 'lucide-react';
import { useAnnouncements, useCreateAnnouncement, useCrossDeptTasks } from '../hooks/useApi';
import { useActiveEmployees, useDepartments } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { mrpApi, apsApi, workOrdersApi, suppliersApi } from '../api/api';
import type { AnnouncementDto, CrossDeptTaskDto, MrpRequirementDto, ProductionScheduleDto, SupplierDto, WorkOrderDto } from '../api/api';

interface MrpResult extends MrpRequirementDto {
  grossRequirement?: number;
  onHand?: number;
  onOrder?: number;
}

interface ScheduleDisplayItem extends ProductionScheduleDto {
  workOrderNumber?: string;
  productName?: string;
  skuNumber?: string;
  workCenter?: string;
  machine?: string;
  startTime?: string;
  endTime?: string;
  progress?: number;
  priority?: string;
}

interface WorkOrderDisplayItem extends WorkOrderDto {
  workOrderNumber?: string;
  productName?: string;
  skuNumber?: string;
  plannedQuantity?: number;
  completedQuantity?: number;
}

const tabs = [
  { id: 'announcements', label: '公告中心' },
  { id: 'mrp', label: 'MRP 需求計劃' },
  { id: 'scheduling', label: '生產排程' },
  { id: 'mes', label: 'MES 即時監控' },
  { id: 'supplier', label: '供應商協作' },
  { id: 'tasks', label: '跨部門任務' },
];

const taskStatusConfig: Record<string, { label: string; color: string }> = {
  pending: { label: '待處理', color: 'var(--slate-500)' },
  in_progress: { label: '進行中', color: 'var(--cobalt-400)' },
  blocked: { label: '受阻', color: 'var(--amber-400)' },
  completed: { label: '完成', color: 'var(--emerald-400)' },
};

const urgencyConfig: Record<string, { label: string; color: string }> = {
  low: { label: '低', color: 'var(--slate-500)' },
  normal: { label: '普通', color: 'var(--cobalt-400)' },
  high: { label: '高', color: 'var(--amber-400)' },
  critical: { label: '緊急', color: 'var(--crimson-400)' },
};

function Badge({ color, children }: { color: string; children: React.ReactNode }) {
  return <span style={{ fontSize: 10, fontWeight: 700, padding: '2px 8px', borderRadius: 4, background: color + '18', color }}>{children}</span>;
}

function getTabIcon(id: string) {
  const icons: Record<string, React.ReactNode> = {
    announcements: <Megaphone size={14} />, mrp: <Package size={14} />, scheduling: <Factory size={14} />,
    mes: <Activity size={14} />, supplier: <Truck size={14} />, tasks: <CheckSquare size={14} />,
  };
  return icons[id] || null;
}

function AnnouncementsTab() {
  const { data: resp, isLoading, refetch } = useAnnouncements();
  const announcements = resp?.data ?? [];
  const [showCreate, setShowCreate] = useState(false);
  const createAnnouncement = useCreateAnnouncement();
  const { data: departmentResponse } = useDepartments({ isActive: 'true' });
  const {
    data: employees = [],
    isLoading: isEmployeesLoading,
    isError: isEmployeesError,
  } = useActiveEmployees();
  const departments = departmentResponse?.data ?? [];
  const announcementFields: FormField[] = [
    { name: 'title', label: '標題', type: 'text', required: true, description: '公告的標題名稱，將顯示於公告列表與首頁跑馬燈。' },
    { name: 'category', label: '分類', type: 'select', options: [
      { value: 'general', label: '一般公告' },
      { value: 'policy', label: '政策' },
      { value: 'hr', label: '人事' },
      { value: 'training', label: '教育訓練' },
      { value: 'it', label: '資訊' },
      { value: 'urgent', label: '緊急' },
    ], description: '公告的分類，方便同仁依類型篩選查看。' },
    { name: 'content', label: '內容', type: 'textarea', description: '公告的完整內容，可輸入文字、條列事項或說明。' },
    { name: 'isPinned', label: '釘選', type: 'checkbox', description: '勾選後公告將置頂顯示於列表最上方。' },
    { name: 'isUrgent', label: '緊急', type: 'checkbox', description: '勾選後將以跑馬燈或推播方式提醒全體注意。' },
    { name: 'targetAudience', label: '發布對象', type: 'select', required: true, options: [
      { value: 'all', label: '全體' },
      { value: 'department', label: '部門' },
      { value: 'custom', label: '自訂' },
    ], description: '指定此公告的閱讀對象範圍。' },
    {
      name: 'publishDeptId',
      label: '指定部門',
      type: 'select',
      required: true,
      searchable: true,
      visibleWhen: { field: 'targetAudience', equals: 'department' },
      options: departments.map(department => ({ value: department.id, label: `${department.deptCode} - ${department.deptName}` })),
      description: '選擇此公告要發布的部門；僅該部門在職員工可見。'
    },
    {
      name: 'recipientEmployeeIds',
      label: '指定收件人',
      type: 'multi-select',
      required: true,
      visibleWhen: { field: 'targetAudience', equals: 'custom' },
      options: employees.map(employee => ({
        value: employee.id,
        label: `${employee.employeeNo} - ${employee.chineseName}${employee.departmentName ? `（${employee.departmentName}）` : ''}`
      })),
      description: isEmployeesLoading
        ? '正在載入在職員工名單...'
        : isEmployeesError
          ? '員工名單載入失敗，請重新整理後再試。'
          : `搜尋並勾選一位或多位在職員工（共 ${employees.length} 位）；只有被勾選者可見。`
    },
    { name: 'publishDate', label: '發布日期', type: 'date', required: true, description: '公告正式對外發布的日期，可預設未來日期自動發布。' },
  ];

  const handleCreate = async (data: Record<string, unknown>) => {
    const targetAudience = String(data.targetAudience || 'all');
    const request = {
      ...data,
      targetAudience,
      publishDeptId: targetAudience === 'department' ? data.publishDeptId : null,
      recipientEmployeeIds: targetAudience === 'custom' ? data.recipientEmployeeIds : []
    };
    await createAnnouncement.mutateAsync(request);
    refetch();
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '14px 16px', borderBottom: '1px solid rgba(100,116,139,0.12)' }}>
          <Megaphone size={14} style={{ color: 'var(--cobalt-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>最新公告</span>
          <button className="btn btn-primary btn-sm" style={{ marginLeft: 'auto' }} onClick={() => setShowCreate(true)}><Plus size={12} /> 發布公告</button>
        </div>
        {isLoading ? (
          <div style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>
            <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
          </div>
        ) : (
          <div>
            {(announcements as AnnouncementDto[]).map(a => (
              <div key={a.id} style={{ display: 'grid', gridTemplateColumns: '36px 1fr auto auto', alignItems: 'center', gap: 10, padding: '12px 16px', borderBottom: '1px solid rgba(100,116,139,0.08)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  {a.isPinned && <Pin size={12} style={{ color: 'var(--cobalt-400)' }} />}
                  {a.isUrgent && <AlertOctagon size={12} style={{ color: 'var(--crimson-400)' }} />}
                </div>
                <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--slate-200)' }}>{a.title}</div>
                <span style={{ fontSize: 11, padding: '2px 8px', borderRadius: 4, background: 'rgba(45,110,245,0.1)', color: 'var(--cobalt-300)', fontWeight: 600 }}>{a.category}</span>
                <span className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{a.publishDate}</span>
              </div>
            ))}
            {announcements.length === 0 && <div style={{ textAlign: 'center', padding: 24, color: 'var(--slate-500)', fontSize: 13 }}>暫無公告</div>}
          </div>
        )}
      </div>
      <FormModal open={showCreate} title="發布公告" fields={announcementFields} onClose={() => setShowCreate(false)} onSubmit={handleCreate} />
    </div>
  );
}

function MrpTab() {
  const [period, setPeriod] = useState('week');
  const [mrpResults, setMrpResults] = useState<MrpResult[]>([]);
  const [mrpLoading, setMrpLoading] = useState(true);
  const riskColors: Record<string, string> = { high: 'var(--crimson-400)', medium: 'var(--amber-400)', low: 'var(--emerald-400)' };
  const riskLabels: Record<string, string> = { high: '高風險', medium: '中風險', low: '安全' };

  useEffect(() => {
    loadMrpData();
  }, []);

  const loadMrpData = async () => {
    setMrpLoading(true);
    try {
      const runsRes = await mrpApi.runs();
      const runs = runsRes.data?.data ?? [];
      if (runs.length > 0) {
        const latestRun = runs[0];
        const reqRes = await mrpApi.requirements(latestRun.id);
        setMrpResults(reqRes.data?.data ?? []);
      }
    } catch (err) {
      console.error('Failed to load MRP data:', err);
    } finally {
      setMrpLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
        {[
          { label: '毛需求', value: mrpResults.reduce((s, r) => s + (r.grossRequirement || 0), 0), color: 'var(--cobalt-400)' },
          { label: '現有庫存', value: mrpResults.reduce((s, r) => s + (r.onHand || 0), 0), color: 'var(--emerald-400)' },
          { label: '在途量', value: mrpResults.reduce((s, r) => s + (r.onOrder || 0), 0), color: 'var(--amber-400)' },
          { label: '淨需求', value: mrpResults.reduce((s, r) => s + (r.netRequirement || 0), 0), color: 'var(--crimson-400)' },
        ].map(d => (
          <div key={d.label} className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
            <div className="mono" style={{ fontSize: 24, fontWeight: 700, color: d.color }}>{d.value.toLocaleString()}</div>
            <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>{d.label}</div>
          </div>
        ))}
      </div>
      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '14px 16px', borderBottom: '1px solid rgba(100,116,139,0.12)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Package size={14} style={{ color: 'var(--cobalt-400)' }} />
            <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>MRP 運算結果</span>
          </div>
          <div style={{ display: 'flex', gap: 6 }}>
            {['day', 'week', 'month'].map(p => (
              <button key={p} onClick={() => setPeriod(p)} style={{ fontSize: 10, padding: '4px 10px', borderRadius: 4, border: 'none', cursor: 'pointer', background: period === p ? 'var(--cobalt-400)' : 'rgba(148,163,184,0.1)', color: period === p ? '#fff' : 'var(--slate-400)', fontWeight: 600 }}>
                {p === 'day' ? '日' : p === 'week' ? '週' : '月'}
              </button>
            ))}
            <button className="btn btn-primary btn-sm" style={{ marginLeft: 8 }} onClick={loadMrpData}><RefreshCw size={12} /> 重新運算</button>
          </div>
        </div>
        {mrpLoading ? (
          <div style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>
            <Loader2 size={20} style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...
          </div>
        ) : mrpResults.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>
            尚無 MRP 運算結果，請先執行 MRP 運算
          </div>
        ) : (
          <table className="data-table">
            <thead><tr><th>品號</th><th>品名</th><th style={{ textAlign: 'right' }}>毛需求</th><th style={{ textAlign: 'right' }}>現有庫存</th><th style={{ textAlign: 'right' }}>在途量</th><th style={{ textAlign: 'right' }}>淨需求</th><th>風險</th><th>建議動作</th></tr></thead>
            <tbody>
              {mrpResults.map((r, i) => {
                const risk = r.netRequirement > 0 ? 'high' : 'low';
                return (
                  <tr key={r.id || i}>
                    <td className="mono" style={{ fontSize: 12, color: 'var(--slate-300)' }}>{r.skuNumber || r.skuId}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{r.skuName || r.skuNumber}</td>
                    <td className="mono" style={{ textAlign: 'right', fontSize: 12 }}>{(r.grossRequirement ?? r.requiredQty).toLocaleString()}</td>
                    <td className="mono" style={{ textAlign: 'right', fontSize: 12, color: 'var(--emerald-400)' }}>{(r.onHand ?? r.availableQty).toLocaleString()}</td>
                    <td className="mono" style={{ textAlign: 'right', fontSize: 12, color: 'var(--amber-400)' }}>{(r.onOrder || 0).toLocaleString()}</td>
                    <td className="mono" style={{ textAlign: 'right', fontSize: 12, fontWeight: 700, color: risk === 'high' ? 'var(--crimson-400)' : 'var(--emerald-400)' }}>{r.netRequirement > 0 ? r.netRequirement.toLocaleString() : '—'}</td>
                    <td><Badge color={riskColors[risk]}>{riskLabels[risk]}</Badge></td>
                    <td style={{ fontSize: 12, color: risk === 'high' ? 'var(--crimson-400)' : 'var(--slate-400)' }}>{r.netRequirement > 0 ? '建議採購/生產' : '庫存充足'}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function SchedulingTab() {
  const [schedules, setSchedules] = useState<ScheduleDisplayItem[]>([]);
  const [loading, setLoading] = useState(true);
  const priorityColors: Record<string, string> = { urgent: 'var(--crimson-400)', high: 'var(--amber-400)', normal: 'var(--cobalt-400)', low: 'var(--slate-500)' };

  useEffect(() => {
    loadSchedules();
  }, []);

  const loadSchedules = async () => {
    setLoading(true);
    try {
      const res = await apsApi.schedules();
      setSchedules(res.data?.data ?? []);
    } catch (err) {
      console.error('Failed to load schedules:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}><Loader2 size={20} style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入排程資料...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div className="glass-card" style={{ padding: 16, overflow: 'auto' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
          <Calendar size={14} style={{ color: 'var(--cobalt-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>生產排程</span>
        </div>
        {schedules.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>尚無排程資料，請先執行 APS 排程運算</div>
        ) : (
          <table className="data-table">
            <thead><tr><th>工單</th><th>產品</th><th>機台</th><th>開始時間</th><th>結束時間</th><th>進度</th><th>優先級</th></tr></thead>
            <tbody>
              {schedules.map(s => (
                <tr key={s.id}>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--slate-300)' }}>{s.workOrderNumber || s.id}</td>
                  <td style={{ fontSize: 12, color: 'var(--slate-200)' }}>{s.productName || s.skuNumber}</td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>{s.workCenter || s.machine}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{s.startTime?.slice(0, 16) || '—'}</td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{s.endTime?.slice(0, 16) || '—'}</td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                      <div style={{ flex: 1, height: 6, background: 'rgba(148,163,184,0.15)', borderRadius: 3 }}>
                        <div style={{ height: '100%', width: `${s.progress || 0}%`, background: 'var(--cobalt-400)', borderRadius: 3 }} />
                      </div>
                      <span className="mono" style={{ fontSize: 10, color: 'var(--slate-400)' }}>{s.progress || 0}%</span>
                    </div>
                  </td>
                  <td><Badge color={priorityColors[s.priority || 'normal']}>{s.priority || '一般'}</Badge></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function MesTab() {
  const [orders, setOrders] = useState<WorkOrderDisplayItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadOrders();
  }, []);

  const loadOrders = async () => {
    setLoading(true);
    try {
      const res = await workOrdersApi.list({ pageSize: '20' });
      setOrders(res.data?.data ?? []);
    } catch (err) {
      console.error('Failed to load work orders:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}><Loader2 size={20} style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入工單資料...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '14px 16px', borderBottom: '1px solid rgba(100,116,139,0.12)' }}>
          <Zap size={14} style={{ color: 'var(--emerald-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>工單即時狀態</span>
        </div>
        {orders.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>尚無工單資料</div>
        ) : (
          <table className="data-table">
            <thead><tr><th>工單</th><th>產品</th><th>計劃數量</th><th>完成數量</th><th>進度</th><th>狀態</th></tr></thead>
            <tbody>
              {orders.map(o => {
                const progress = o.plannedQuantity > 0 ? Math.round((o.completedQuantity || 0) / o.plannedQuantity * 100) : 0;
                return (
                  <tr key={o.id}>
                    <td className="mono" style={{ fontSize: 12, color: 'var(--slate-300)' }}>{o.workOrderNumber}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-200)' }}>{o.productName || o.skuNumber}</td>
                    <td className="mono" style={{ fontSize: 12 }}>{o.plannedQuantity || 0}</td>
                    <td className="mono" style={{ fontSize: 12, color: 'var(--emerald-400)' }}>{o.completedQuantity || 0}</td>
                    <td style={{ width: 120 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                        <div style={{ flex: 1, height: 6, background: 'rgba(148,163,184,0.15)', borderRadius: 3 }}>
                          <div style={{ height: '100%', width: `${progress}%`, background: progress >= 100 ? 'var(--emerald-400)' : 'var(--cobalt-400)', borderRadius: 3 }} />
                        </div>
                        <span className="mono" style={{ fontSize: 10, color: 'var(--slate-400)' }}>{progress}%</span>
                      </div>
                    </td>
                    <td><Badge color={o.status === 'in_progress' ? 'var(--cobalt-400)' : 'var(--slate-500)'}>{o.status === 'in_progress' ? '生產中' : o.status === 'completed' ? '已完成' : '已派發'}</Badge></td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function SupplierTab() {
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadSuppliers();
  }, []);

  const loadSuppliers = async () => {
    setLoading(true);
    try {
      const res = await suppliersApi.list();
      setSuppliers(res.data?.data ?? []);
    } catch (err) {
      console.error('Failed to load suppliers:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}><Loader2 size={20} style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入供應商資料...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 }}>
        <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
          <div className="mono" style={{ fontSize: 28, fontWeight: 700, color: 'var(--cobalt-400)' }}>{suppliers.length}</div>
          <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>活躍供應商</div>
        </div>
        <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
          <div className="mono" style={{ fontSize: 28, fontWeight: 700, color: 'var(--amber-400)' }}>{suppliers.filter(s => s.rating === 'A' || s.rating === 'B').length}</div>
          <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>評級良好</div>
        </div>
        <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
          <div className="mono" style={{ fontSize: 28, fontWeight: 700, color: 'var(--emerald-400)' }}>{suppliers.filter(s => s.status === 'active').length}</div>
          <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>啟用中</div>
        </div>
      </div>
      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '14px 16px', borderBottom: '1px solid rgba(100,116,139,0.12)' }}>
          <Truck size={14} style={{ color: 'var(--cobalt-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>供應商列表</span>
        </div>
        {suppliers.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>尚無供應商資料</div>
        ) : (
          <table className="data-table">
            <thead><tr><th>供應商</th><th>編號</th><th>評級</th><th>付款條件</th><th>狀態</th></tr></thead>
            <tbody>
              {suppliers.map(s => (
                <tr key={s.id}>
                  <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{s.supplierName}</td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--slate-400)' }}>{s.supplierCode}</td>
                  <td>
                    {s.rating ? (
                      <Badge color={s.rating === 'A' ? 'var(--emerald-400)' : s.rating === 'B' ? 'var(--cobalt-400)' : 'var(--amber-400)'}>{s.rating} 級</Badge>
                    ) : <span style={{ color: 'var(--slate-600)' }}>—</span>}
                  </td>
                  <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{s.paymentTerms || '—'}</td>
                  <td><Badge color={s.status === 'active' ? 'var(--emerald-400)' : 'var(--slate-500)'}>{s.status === 'active' ? '啟用' : '停用'}</Badge></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function TasksTab() {
  const [search, setSearch] = useState('');
  const { data: tasksResp, isLoading } = useCrossDeptTasks();
  const tasks = (tasksResp?.data ?? []) as CrossDeptTaskDto[];
  const filteredTasks = tasks.filter(t => !search || (t.taskName ?? '').includes(search) || (t.ownerDeptName ?? '').includes(search));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div className="glass-card" style={{ padding: '10px 14px', display: 'flex', alignItems: 'center', gap: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1 }}>
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋任務名稱、部門..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        <button className="btn btn-primary btn-sm"><CheckSquare size={12} /> 新增任務</button>
      </div>
      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <table className="data-table">
          <thead><tr><th>任務名稱</th><th>負責部門</th><th>緊急度</th><th>預計完成</th><th>狀態</th><th>操作</th></tr></thead>
          <tbody>
            {isLoading ? (
              <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}><Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />載入中...</td></tr>
            ) : filteredTasks.map(t => {
              const sc = taskStatusConfig[t.status] ?? { label: t.status, color: 'var(--slate-500)' };
              const uc = urgencyConfig[t.urgency] ?? { label: t.urgency, color: 'var(--slate-500)' };
              return (
                <tr key={t.id}>
                  <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{t.taskName}</td>
                  <td style={{ fontSize: 13, color: 'var(--slate-400)' }}>{t.ownerDeptName ?? '—'}</td>
                  <td><Badge color={uc.color}>{uc.label}</Badge></td>
                  <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{t.expectedCompleteDate ?? '—'}</td>
                  <td><Badge color={sc.color}>{sc.label}</Badge></td>
                  <td><button style={{ width: 26, height: 26, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'transparent', border: 'none', borderRadius: 4, color: 'var(--slate-500)', cursor: 'pointer' }} title="查看"><Eye size={13} /></button></td>
                </tr>
              );
            })}
            {!isLoading && filteredTasks.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>暫無跨部門任務</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export function Collaboration() {
  const [activeTab, setActiveTab] = useState('announcements');
  const renderTab = () => {
    switch (activeTab) {
      case 'announcements': return <AnnouncementsTab />;
      case 'mrp': return <MrpTab />;
      case 'scheduling': return <SchedulingTab />;
      case 'mes': return <MesTab />;
      case 'supplier': return <SupplierTab />;
      case 'tasks': return <TasksTab />;
      default: return <AnnouncementsTab />;
    }
  };
  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">協作平台</h2>
          <p className="module-page-desc">跨部門協作、生產排程、MRP 需求計劃與供應商管理</p>
        </div>
      </div>
      <div style={{ display: 'flex', gap: 4, borderBottom: '1px solid rgba(100,116,139,0.12)' }}>
        {tabs.map(tab => (
          <button key={tab.id} onClick={() => setActiveTab(tab.id)}           style={{
            display: 'flex', alignItems: 'center', gap: 6, padding: '10px 16px',
            background: activeTab === tab.id ? 'rgba(45,110,245,0.1)' : 'transparent',
            borderTop: '0px', borderLeft: '0px', borderRight: '0px', borderBottom: activeTab === tab.id ? '2px solid var(--cobalt-400)' : '2px solid transparent',
            color: activeTab === tab.id ? 'var(--cobalt-400)' : 'var(--slate-500)',
            fontSize: 12, fontWeight: 600, cursor: 'pointer', transition: 'all 0.2s',
          }}>
            {getTabIcon(tab.id)} {tab.label}
          </button>
        ))}
      </div>
      {renderTab()}
      <style>{`
        .module-page { display: flex; flex-direction: column; gap: 16px; }
        .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; }
        .module-page-title { font-size: 20px; font-weight: 700; letter-spacing: -0.02em; color: var(--slate-100); }
        .module-page-desc { font-size: 12px; color: var(--slate-500); margin-top: 2px; font-family: var(--font-mono); }
        .search-input { background: transparent; border: none; outline: none; color: var(--slate-200); font-size: 13px; width: 200px; }
        .search-input::placeholder { color: var(--slate-500); }
      `}</style>
    </div>
  );
}
