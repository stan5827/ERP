import React, { useState, useMemo, useEffect } from 'react';
import { 
  Plus, Search, Eye, Edit2, CheckCircle, 
  ArrowRight, Loader2, Lock, Unlock, Key,
  ClipboardCheck, Trash2, AlertCircle, TrendingUp,
  BarChart3, Truck, ShoppingBag, ShieldCheck, Activity, RefreshCw,
  XCircle, Send
} from 'lucide-react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { 
  purchaseOrdersApi, 
  purchaseRequisitionsApi, 
  suppliersApi,
  skusApi,
  warehousesApi,
  departmentsApi,
  mrpApi,
  planningApi
} from '../api/api';
import { supplierAsnsApi } from '../api/procurementApi';
import { FormModal, FormField } from '../components/FormModal';

// --- 配置與類型定義 ---

const statusConfig: Record<string, { label: string; cls: string }> = {
  'pending': { label: '待處理', cls: 'neutral' },
  'confirmed': { label: '已核准', cls: 'info' },
  'partial': { label: '部分收料', cls: 'warning' },
  'completed': { label: '已結案', cls: 'success' },
  'cancelled': { label: '已取消', cls: 'neutral' }
};

const ratingConfig: Record<string, { label: string; color: string; bg: string }> = {
  'S': { label: 'S 級 (優)', color: '#10b981', bg: 'rgba(16,185,129,0.1)' },
  'A': { label: 'A 級 (佳)', color: '#38bdf8', bg: 'rgba(56,189,248,0.1)' },
  'B': { label: 'B 級 (中)', color: '#fbbf24', bg: 'rgba(251,191,36,0.1)' },
  'C': { label: 'C 級 (弱)', color: '#f43f5e', bg: 'rgba(244,63,94,0.1)' },
};

const normalizeStatus = (status?: string | null) => (status || '').toLowerCase();

const poStatusBadge = (status?: string | null) => {
  switch (normalizeStatus(status)) {
    case 'draft':
      return 'neutral';
    case 'confirmed':
      return 'info';
    case 'partial':
      return 'warning';
    case 'received':
    case 'completed':
      return 'success';
    default:
      return 'neutral';
  }
};

const asnStatusBadge = (status?: string | null) => {
  switch (normalizeStatus(status)) {
    case 'pending':
      return 'info';
    case 'received':
      return 'success';
    default:
      return 'neutral';
  }
};

// --- 組件: KPI 卡片 ---
const StatCard = ({ title, value, icon: Icon, trend, color }: any) => (
  <div className="glass-card stat-card" style={{ borderLeft: `4px solid ${color}` }}>
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
      <div>
        <p className="stat-label">{title}</p>
        <h3 className="stat-value">{value}</h3>
      </div>
      <div className="stat-icon" style={{ background: color + '15', color: color }}>
        <Icon size={20} />
      </div>
    </div>
    {trend && (
      <div className="stat-trend">
        <TrendingUp size={12} style={{ marginRight: 4 }} />
        <span>{trend} vs 上期</span>
      </div>
    )}
  </div>
);

export const Procurement: React.FC = () => {
  const [view, setView] = useState<'orders' | 'asns' | 'requisitions' | 'suppliers' | 'mrp' | 'planning'>('orders');
  const [search, setSearch] = useState('');
  const [showAdd, setShowAdd] = useState(false);
  const [showAddPr, setShowAddPr] = useState(false);
  const [showAddSupplier, setShowAddSupplier] = useState(false);
  const [showEditPo, setShowEditPo] = useState(false);
  const [showPoDetail, setShowPoDetail] = useState(false);
  const [showEditSupplier, setShowEditSupplier] = useState(false);
  const [showPrDetail, setShowPrDetail] = useState(false);
  const [showEditPr, setShowEditPr] = useState(false);
  const [isFetchingDetail, setIsFetchingDetail] = useState(false);
  const [mrpOrders, setMrpOrders] = useState<any[]>([]);
  const [mrpLoading, setMrpLoading] = useState(false);
  const [showSupplierEval, setShowSupplierEval] = useState(false);
  
  const [selectedPo, setSelectedPo] = useState<any>(null);
  const [selectedPr, setSelectedPr] = useState<any>(null);
  const [selectedSupplierEval, setSelectedSupplierEval] = useState<any>(null);
  const [editingSupplier, setEditingSupplier] = useState<any>(null);
  const [editingSupplierId, setEditingSupplierId] = useState<string | null>(null);
  
  const [prItems, setPrItems] = useState<any[]>([]);
  const [poItems, setPoItems] = useState<any[]>([]);
  const [supplierPortalUsers, setSupplierPortalUsers] = useState<Record<string, any[]>>({});
  const [showPortalUserModal, setShowPortalUserModal] = useState(false);
  const [selectedPortalUser, setSelectedPortalUser] = useState<any>(null);
  const [showPrApproval, setShowPrApproval] = useState(false);
  const [prApprovalAction, setPrApprovalAction] = useState<'submit' | 'approve' | 'reject' | 'return'>('submit');
  const [prApprovalComments, setPrApprovalComments] = useState('');
  const [showAddPoFromPr, setShowAddPoFromPr] = useState(false);

  const queryClient = useQueryClient();

  // --- 資料獲取 ---
  const { data: poResponse, isLoading: poLoading } = useQuery({ 
    queryKey: ['purchase-orders'], 
    queryFn: () => purchaseOrdersApi.list().then(res => res.data)
  });

  const { data: ropResponse, isLoading: ropLoading, refetch: refetchRop } = useQuery({
    queryKey: ['rop-suggestions'],
    queryFn: () => planningApi.getSuggestions().then(res => res.data),
    enabled: view === 'planning'
  });
  
  const { data: prData, isLoading: prLoading } = useQuery({ 
    queryKey: ['purchase-requisitions'], 
    queryFn: () => purchaseRequisitionsApi.list().then(res => res.data) 
  });
  
  const { data: supplierData } = useQuery({ 
    queryKey: ['suppliers'], 
    queryFn: () => suppliersApi.list().then(res => res.data) 
  });
  const { data: asnData, isLoading: asnLoading } = useQuery({
    queryKey: ['supplier-asns'],
    queryFn: () => supplierAsnsApi.list().then(res => res.data)
  });
  
  const { data: skusData } = useQuery({ 
    queryKey: ['skus-purchasable'], 
    queryFn: () => skusApi.allPurchasable().then(res => res.data) 
  });
  
  const { data: warehousesData } = useQuery({ 
    queryKey: ['warehouses'], 
    queryFn: () => warehousesApi.list().then(res => res.data) 
  });

  const { data: departmentsData } = useQuery({
    queryKey: ['departments', 'flat'],
    queryFn: () => departmentsApi.flat().then(res => res.data)
  });

  // --- 資料轉換與篩選 ---
  const poList = useMemo(() => poResponse?.data || [], [poResponse]);
  const ropSuggestions = useMemo(() => ropResponse?.data || [], [ropResponse]);

  const suppliersMapped = useMemo(() => 
    (supplierData?.data || []).map((s: any) => ({ 
      value: s.id, 
      label: `[${s.supplierCode || s.supplier_code}] ${s.supplierName || s.supplier_name}` 
    })),
    [supplierData]
  );

  const skusMapped = useMemo(() => {
    const rawList = skusData?.data || [];
    return rawList.map((s: any) => ({ 
        value: s.id, 
        label: `[${s.sku_code || s.skuNumber}] ${s.sku_name || s.skuName}`,
        unit: s.uom || s.unit, 
        price: s.purchase_price || s.purchasePrice 
    }));
  }, [skusData]);

  const warehousesMapped = useMemo(() => 
    (warehousesData?.data || []).map(w => ({ 
      value: w.warehouseCode || 'MAIN', 
      label: w.warehouseName 
    })), 
    [warehousesData]
  );

  const departmentsMapped = useMemo(() => 
    (departmentsData?.data || []).map((d: any) => ({
      value: d.id,
      label: d.deptName
    })),
    [departmentsData]
  );

  const filteredPo = useMemo(() => 
    poList.filter((po: any) => 
      (po.poNumber || po.po_no || '').includes(search) || 
      (po.supplierCode || po.supplier_code || '').includes(search)
    ), 
    [poList, search]
  );

  const asnList = useMemo(() => asnData?.data || [], [asnData]);

  const [isProcessing, setIsProcessing] = useState(false);

  const handleCreatePo = async (values: any) => {
    setIsProcessing(true);
    try {
      await purchaseOrdersApi.create({
        supplierId: values.supplierId,
        poDate: new Date(values.poDate).toISOString(),
        items: poItems.map(it => ({
          skuId: it.skuId,
          quantity: it.quantity,
          unitPrice: it.unitPrice
        }))
      });
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      setShowAdd(false);
      setPoItems([]);
    } catch (err: any) {
      alert(`建立失敗: ${err.message}`);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleConfirmPo = async (poNumber: string) => {
    try {
      await purchaseOrdersApi.confirm(poNumber);
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
    } catch (err: any) { alert(`確認失敗: ${err.message}`); }
  };

  const handleCreateAsn = async (po: any) => {
    const expectedArrivalDate = prompt('請輸入預計到貨日 (YYYY-MM-DD):', new Date().toISOString().slice(0, 10));
    if (!expectedArrivalDate) return;
    const carrierName = prompt('請輸入物流商名稱:', '') || undefined;
    const trackingNumber = prompt('請輸入追蹤號碼:', '') || undefined;

    setIsProcessing(true);
    try {
      const poDetailResponse = await purchaseOrdersApi.get(po.id);
      const poDetail = poDetailResponse.data?.data ?? po;
      const remainingItems = (poDetail.items || [])
        .map((item: any) => ({
          skuId: item.skuId,
          shippedQty: Math.max(0, Number(item.quantity || 0) - Number(item.receivedQty || 0))
        }))
        .filter((item: any) => item.shippedQty > 0);

      if (remainingItems.length === 0) {
        alert('此採購單已無可建立 ASN 的未收數量。');
        return;
      }

      await supplierAsnsApi.create({
        purchaseOrderId: poDetail.id || po.id,
        expectedArrivalDate,
        carrierName,
        trackingNumber,
        items: remainingItems
      });
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      queryClient.invalidateQueries({ queryKey: ['supplier-asns'] });
      setView('asns');
      alert('ASN 已建立，請到 ASN / 收貨頁簽執行入庫。');
    } catch (err: any) { 
      alert(`建立 ASN 失敗: ${err.message}`); 
    } finally {
      setIsProcessing(false);
    }
  };

  const handleReceiveAsn = async (asnId: string) => {
    const warehouseCode = prompt('請輸入入庫倉庫代號 (預設 MAIN):', 'MAIN');
    if (warehouseCode === null) return;

    setIsProcessing(true);
    try {
      await supplierAsnsApi.receive(asnId, { warehouseCode: warehouseCode || 'MAIN' });
      queryClient.invalidateQueries({ queryKey: ['supplier-asns'] });
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      alert('ASN 收貨入庫完成。');
    } catch (err: any) {
      alert(`ASN 收貨失敗: ${err.message}`);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRunRop = async () => {
    setIsProcessing(true);
    try {
      await planningApi.persistSuggestions();
      refetchRop();
      alert('ROP 運算完成，已保存建議清單。');
    } catch (err: any) {
      alert(`運算失敗: ${err.message}`);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleConvertSuggestionToPo = (suggestion: any) => {
    setSelectedPo({
      supplier_code: '', // 需要用戶選擇
      poDate: new Date().toISOString().slice(0, 10),
    });
    setPoItems([{
      sku_code: suggestion.sku_code,
      quantity: suggestion.suggested_qty,
      unitPrice: 0 // 需要手動輸入或從 SKU 抓
    }]);
    setShowAdd(true);
  };

  const loadMrpOrders = async () => {
    setMrpLoading(true);
    try {
      const runsRes = await mrpApi.runs({ pageSize: 1 });
      if (runsRes.data?.success && runsRes.data.data?.length) {
        const lastRunId = runsRes.data.data[0].id;
        const ordersRes = await mrpApi.plannedOrders(lastRunId);
        if (ordersRes.data?.success) {
          setMrpOrders(ordersRes.data.data.filter((o: any) => o.orderType === 'purchase_requisition' || o.orderType === 'purchase_order'));
        }
      }
    } catch (err) { console.error('Failed to load MRP orders', err); }
    finally { setMrpLoading(false); }
  };

  useEffect(() => {
    if (view === 'mrp') {
      loadMrpOrders();
    }
  }, [view]);

  const handleConfirmMrp = async (orderId: string) => {
    try {
      const res = await mrpApi.confirmPlannedOrder(orderId);
      if (res.data?.success) {
        alert(`已成功轉化為 ${res.data.data?.documentNumber || '採購單據'}`);
        loadMrpOrders();
        queryClient.invalidateQueries({ queryKey:['purchase-requisitions'] });
      }
    } catch (err) { alert('確認失敗'); }
  };

  // --- 表單欄位定義 ---
  const poFields: FormField[] = [
    { name: 'supplierId', label: '供應商 *', type: 'select', required: true, options: suppliersMapped },
    { name: 'poDate', label: '下單日期', type: 'date', required: true, defaultValue: new Date().toISOString().slice(0,10) },
    { name: '_poItems', label: '', type: 'custom' }
  ];

  const prFields: FormField[] = [
    { name: 'departmentId', label: '請購單位/部門 *', type: 'select', required: true, options: departmentsMapped },
    { name: 'prDate', label: '請購日期 *', type: 'date', required: true, defaultValue: new Date().toISOString().slice(0,10) },
    { name: 'purpose', label: '摘要 / 用途 *', type: 'text', required: true, placeholder: '請輸入請購目的 (如：Q3 生產補貨)' },
    { name: 'priority', label: '緊急程度', type: 'select', options: [{ value: 'normal', label: '一般' }, { value: 'urgent', label: '緊急' }], defaultValue: 'normal' },
    { name: 'budgetAccount', label: '預算科目', type: 'text', placeholder: '生產耗材費' },
    { name: '_prItems', label: '', type: 'custom' }
  ];

  const supplierFields: FormField[] = [
    { name: 'supplierCode', label: '供應商代號 *', type: 'text', required: true },
    { name: 'supplierName', label: '供應商名稱 *', type: 'text', required: true },
    { name: 'taxId', label: '統一編號', type: 'text' },
    { name: 'contactPerson', label: '聯絡人', type: 'text' },
    { name: 'phone', label: '聯絡電話', type: 'text' },
    { name: 'email', label: 'Email', type: 'text' },
    { name: 'address', label: '地址', type: 'text' },
    { name: 'paymentTerms', label: '付款條件', type: 'text', placeholder: '如: 月結30天' },
    { name: 'rating', label: '評等', type: 'select', options: [{ value: 'A', label: 'A級 (優良)' }, { value: 'B', label: 'B級 (合格)' }, { value: 'C', label: 'C級 (待改善)' }, { value: 'D', label: 'D級 (不合格)' }] },
    { name: 'status', label: '狀態', type: 'select', options: [{ value: 'ACTIVE', label: '啟用' }, { value: 'INACTIVE', label: '停用' }], defaultValue: 'ACTIVE' },
  ];

  return (
    <div className="module-page">
      <div className="module-page-header">
        <div>
          <div className="breadcrumb">核心資源管理 / 採購模組</div>
          <h2 className="module-page-title">ProMES 智慧採購指揮中心</h2>
          <p className="module-page-desc">整合 MRP 補貨建議與自動化財務過帳，打造現代化 Procure-to-Pay 流程</p>
        </div>
        <div className="header-actions">
          <div className="search-box">
            <Search size={16} />
            <input type="text" placeholder="搜尋 PO 單號、供應商..." value={search} onChange={e => setSearch(e.target.value)} />
          </div>
          <button className="btn btn-primary" onClick={() => setShowAdd(true)}>
            <Plus size={16} style={{ marginRight: 6 }} /> 建立採購單
          </button>
        </div>
      </div>

      <div className="kpi-grid">
        <StatCard title="待收料金額" value={`$${filteredPo.filter((p: any) => p.status === 'CONFIRMED').reduce((acc: number, p: any) => acc + Number(p.totalAmount || p.total_amount || 0), 0).toLocaleString()}`} icon={ShoppingBag} color="var(--gold-400)" />
        <StatCard title="本月已收料" value={`$${filteredPo.filter((p: any) => p.status === 'RECEIVED').reduce((acc: number, p: any) => acc + Number(p.totalAmount || p.total_amount || 0), 0).toLocaleString()}`} icon={CheckCircle} color="var(--emerald-400)" />
        <StatCard title="待確認單據" value={filteredPo.filter((p: any) => p.status === 'DRAFT').length} icon={Activity} color="var(--amber-400)" />
        <StatCard title="供應商合規率" value="98.5%" icon={ShieldCheck} color="var(--cobalt-400)" />
      </div>

      <div className="view-selector">
        <button className={`view-tab ${view === 'orders' ? 'active' : ''}`} onClick={() => setView('orders')}>
          <ShoppingBag size={14} /> 採購單 (PO)
        </button>
        <button className={`view-tab ${view === 'asns' ? 'active' : ''}`} onClick={() => setView('asns')}>
          <Send size={14} /> ASN / 收貨
        </button>
        <button className={`view-tab ${view === 'requisitions' ? 'active' : ''}`} onClick={() => setView('requisitions')}>
          <BarChart3 size={14} /> 請購 (PR)
        </button>
        <button className={`view-tab ${view === 'planning' ? 'active' : ''}`} onClick={() => setView('planning')}>
          <Activity size={14} /> 採購計畫 (Smart ROP)
        </button>
        <button className={`view-tab ${view === 'suppliers' ? 'active' : ''}`} onClick={() => setView('suppliers')}>
          <Truck size={14} /> 供應商
        </button>
      </div>

      <div className="main-content-area">
        {view === 'requisitions' && (
          <div className="glass-card no-lift">
            <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h3 style={{ margin: 0, fontSize: '16px' }}>請購單列表 (PR)</h3>
                <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>採購申請單管理與審批流程</p>
              </div>
              <button className="btn btn-primary btn-sm" onClick={() => { setSelectedPr(null); setShowAddPr(true); }}>
                <Plus size={14} style={{ marginRight: 6 }} /> 新增請購單
              </button>
            </div>
            <table className="lux-table">
              <thead>
                <tr>
                  <th>請購單號</th><th>日期</th><th>部門</th><th>金額</th><th>狀態</th><th>操作</th>
                </tr>
              </thead>
              <tbody>
                {prLoading ? <tr><td colSpan={6} className="td-loading"><Loader2 className="spin" /></td></tr> : 
                  (prData?.data || []).length === 0 ? <tr><td colSpan={6} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>尚無請購單資料</td></tr> :
                  (prData?.data || []).map((pr: any) => (
                    <tr key={pr.id}>
                      <td className="mono-label">{pr.prNumber}</td>
                      <td className="date-label">{pr.prDate?.slice(0, 10)}</td>
                      <td className="emp-label">{pr.departmentName || '—'}</td>
                      <td className="amount-label">{Number(pr.totalEstimatedAmount || 0).toLocaleString()}</td>
                      <td>
                        <span className={`badge-lux badge-${
                          pr.status === 'DRAFT' ? 'neutral' : 
                          pr.status === 'SUBMITTED' || pr.status === 'UNDER_REVIEW' ? 'info' : 
                          pr.status === 'APPROVED' ? 'success' : 
                          pr.status === 'REJECTED' ? 'danger' : 'neutral'
                        }`}>
                          {pr.status === 'DRAFT' ? '草稿' : 
                           pr.status === 'SUBMITTED' ? '待審' :
                           pr.status === 'UNDER_REVIEW' ? '審核中' :
                           pr.status === 'APPROVED' ? '已核准' :
                           pr.status === 'REJECTED' ? '已駁回' :
                           pr.status === 'RETURNED' ? '退回' : pr.status}
                        </span>
                      </td>
                      <td>
                        <div className="action-row">
                          {pr.status === 'DRAFT' && (
                            <button className="btn-sm btn-primary" onClick={async () => {
                              await purchaseRequisitionsApi.submit(pr.id);
                              queryClient.invalidateQueries({ queryKey: ['purchase-requisitions'] });
                            }}>送出審批</button>
                          )}
                          {pr.status === 'SUBMITTED' && (
                            <>
                              <button className="btn-sm btn-primary" onClick={() => { setSelectedPr(pr); setPrApprovalAction('approve'); setShowPrApproval(true); }}>核准</button>
                              <button className="btn-sm btn-outline" style={{ marginLeft: 4 }} onClick={() => { setSelectedPr(pr); setPrApprovalAction('reject'); setShowPrApproval(true); }}>駁回</button>
                            </>
                          )}
                          {pr.status === 'APPROVED' && (
                            <button className="btn-sm btn-outline" onClick={() => { setSelectedPr(pr); setShowAddPoFromPr(true); }}>轉PO</button>
                          )}
                          <button className="action-btn" title="檢視" onClick={() => { setSelectedPr(pr); setShowEditPr(true); }}><Eye size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  ))
                }
              </tbody>
            </table>
          </div>
        )}

        {view === 'planning' && (
          <div className="glass-card no-lift">
            <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h3 style={{ margin: 0, fontSize: '16px' }}>庫存水位預警與建議</h3>
                <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>基於再訂購點 (ROP) 與安全庫存進行智動分析</p>
              </div>
              <button className="btn btn-sm btn-outline" onClick={handleRunRop} disabled={isProcessing}>
                {isProcessing ? <Loader2 className="spin" size={14} /> : <><RefreshCw size={14} style={{ marginRight: 6 }} /> 執行 ROP 運算</>}
              </button>
            </div>
            <table className="lux-table">
              <thead>
                <tr>
                  <th>物料名稱</th><th>現有庫存</th><th>ROP 水位</th><th>安全庫存</th><th>建議採購量</th><th>預警原因</th><th>操作</th>
                </tr>
              </thead>
              <tbody>
                {ropLoading ? <tr><td colSpan={7} className="td-loading"><Loader2 className="spin" /></td></tr> : 
                  ropSuggestions.length === 0 ? <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>目前所有物料水位正常。</td></tr> :
                  ropSuggestions.map((s: any, idx: number) => (
                    <tr key={idx}>
                      <td>
                        <div className="emp-label">{s.sku_name}</div>
                        <div className="mono-label">{s.sku_code}</div>
                      </td>
                      <td className="amount-label">{Number(s.on_hand_qty).toLocaleString()}</td>
                      <td className="amount-label" style={{ color: 'var(--amber-400)' }}>{Number(s.reorder_point).toLocaleString()}</td>
                      <td className="amount-label" style={{ color: 'var(--rose-400)' }}>{Number(s.safety_stock).toLocaleString()}</td>
                      <td className="amount-label" style={{ color: 'var(--emerald-400)', fontSize: '15px' }}>{Number(s.suggested_qty).toLocaleString()}</td>
                      <td>
                        <span className={`badge-lux ${s.reason === 'SAFETY_STOCK_ALERT' ? 'badge-warning' : 'badge-info'}`}>
                          {s.reason === 'SAFETY_STOCK_ALERT' ? '低於安全庫存' : '低於再訂購點'}
                        </span>
                      </td>
                      <td>
                        <button className="btn-sm btn-primary" onClick={() => handleConvertSuggestionToPo(s)}>
                          轉成採購單
                        </button>
                      </td>
                    </tr>
                  ))
                }
              </tbody>
            </table>
          </div>
        )}

        {view === 'asns' && (
          <div className="glass-card no-lift">
            <table className="lux-table">
              <thead>
                <tr>
                  <th>ASN 編號</th><th>PO 單號</th><th>預計到貨</th><th>物流商</th><th>狀態</th><th>操作</th>
                </tr>
              </thead>
              <tbody>
                {asnLoading ? <tr><td colSpan={6} className="td-loading"><Loader2 className="spin" /></td></tr> :
                  asnList.length === 0 ? <tr><td colSpan={6} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>尚無 ASN 資料</td></tr> :
                  asnList.map((asn: any) => (
                    <tr key={asn.asnId || asn.id}>
                      <td className="mono-label">{asn.asnNumber}</td>
                      <td className="mono-label">{asn.purchaseOrderNumber || '—'}</td>
                      <td className="date-label">{asn.expectedArrivalDate?.slice(0, 10)}</td>
                      <td>{asn.carrierName || '—'}</td>
                      <td>
                        <span className={`badge-lux badge-${asnStatusBadge(asn.status)}`}>
                          {asn.status}
                        </span>
                      </td>
                      <td>
                        <div className="action-row">
                          {normalizeStatus(asn.status) === 'pending' && (
                            <button className="btn-sm btn-primary" onClick={() => handleReceiveAsn(asn.asnId || asn.id)}>收貨入庫</button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                }
              </tbody>
            </table>
          </div>
        )}

        {view === 'orders' && (
          <div className="glass-card no-lift">
            <table className="lux-table">
              <thead>
                <tr>
                  <th>採購單號</th><th>供應商代碼</th><th>日期</th><th>總金額</th><th>狀態</th><th>操作</th>
                </tr>
              </thead>
              <tbody>
                {poLoading ? <tr><td colSpan={6} className="td-loading"><Loader2 className="spin" /></td></tr> : 
                  filteredPo.map((po: any) => (
                    <tr key={po.id || po.poNumber}>
                      <td className="mono-label">{po.poNumber || po.po_no}</td>
                      <td className="emp-label">{po.supplierCode || po.supplier_code}</td>
                      <td className="date-label">{(po.poDate || po.po_date)?.slice(0, 10)}</td>
                      <td className="amount-label">{Number(po.totalAmount || po.total_amount || 0).toLocaleString()}</td>
                      <td>
                        <span className={`badge-lux badge-${poStatusBadge(po.status)}`}>
                          {po.status}
                        </span>
                      </td>
                      <td>
                        <div className="action-row">
                          {normalizeStatus(po.status) === 'draft' && (
                            <button className="btn-sm btn-outline" onClick={() => handleConfirmPo(po.poNumber)}>確認</button>
                          )}
                          {normalizeStatus(po.status) === 'confirmed' && (
                            <button className="btn-sm btn-primary" onClick={() => handleCreateAsn(po)}>建立 ASN</button>
                          )}
                          <button className="action-btn" title="檢視"><Eye size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  ))
                }
              </tbody>
            </table>
          </div>
        )}

        {view === 'suppliers' && (
          <div className="glass-card no-lift">
            <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h3 style={{ margin: 0, fontSize: '16px' }}>供應商列表</h3>
                <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>供應商資料維護與管理</p>
              </div>
              <button className="btn btn-primary btn-sm" onClick={() => setShowAddSupplier(true)}>
                <Plus size={14} style={{ marginRight: 6 }} /> 新增供應商
              </button>
            </div>
            <table className="lux-table">
              <thead>
                <tr>
                  <th>供應商代號</th><th>名稱</th><th>聯絡人</th><th>電話</th><th>評等</th><th>狀態</th><th>操作</th>
                </tr>
              </thead>
              <tbody>
                {(supplierData?.data || []).length === 0 ? <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>尚無供應商資料</td></tr> :
                  (supplierData?.data || []).map((s: any) => (
                    <tr key={s.id}>
                      <td className="mono-label">{s.supplierCode}</td>
                      <td className="emp-label">{s.supplierName}</td>
                      <td>{s.contactPerson || '—'}</td>
                      <td>{s.phone || '—'}</td>
                      <td><span className={`badge-lux ${s.rating === 'A' ? 'badge-success' : s.rating === 'B' ? 'badge-info' : 'badge-neutral'}`}>{s.rating || '—'}</span></td>
                      <td><span className={`badge-lux ${s.status === 'ACTIVE' ? 'badge-success' : 'badge-neutral'}`}>{s.status === 'ACTIVE' ? '啟用' : '停用'}</span></td>
                      <td>
                        <div className="action-row">
                          <button className="btn-sm btn-outline" onClick={() => { setEditingSupplierId(s.id); setShowEditSupplier(true); }}>編輯</button>
                        </div>
                      </td>
                    </tr>
                  ))
                }
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* --- Modals --- */}
      <FormModal 
        open={showAdd || showEditPo} 
        title={selectedPo?.id ? `編輯採購單 ${selectedPo.poNumber}` : (selectedPr ? "從 PR 轉化採購指令 (三單匹配啟動)" : "智動化下達 PO")}
        width={1000}
        initialValues={selectedPo || undefined}
        onClose={() => { setShowAdd(false); setShowEditPo(false); setSelectedPr(null); setPoItems([]); setSelectedPo(null); }}
        fields={poFields}
        onSubmit={async (v) => {
           if (selectedPo?.id) {
             await purchaseOrdersApi.update(selectedPo.id, { ...v, items: poItems });
           } else {
             await purchaseOrdersApi.create({ ...v, items: poItems });
           }
           queryClient.invalidateQueries({ queryKey:['purchase-orders'] });
           setShowAdd(false);
           setShowEditPo(false);
        }}
        customRenderers={{
          _poItems: (
            <div className="items-editor">
              <div className="editor-header">
                <h4>採購品項明細 (BOM/MRP 聯動)</h4>
                <button type="button" className="btn-sm btn-outline" onClick={() => setPoItems([...poItems, { skuId: '', quantity: 1, unitPrice: 0 }])}>新增物料</button>
              </div>
              <div className="items-grid">
                <div className="item-row" style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', marginBottom: 12, paddingBottom: 8 }}>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)' }}>物料品號 / 規格描述 *</div>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)' }}>採購數量 *</div>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)' }}>建議單價 (TWD) *</div>
                  <div></div>
                </div>
                {poItems.map((item, i) => (
                  <div key={i} className="item-row">
                    <select value={item.skuId} onChange={e => {
                      const next = [...poItems];
                      next[i].skuId = e.target.value;
                      const sku = skusMapped.find(s => s.value === e.target.value);
                      if (sku) next[i].unitPrice = sku.price;
                      setPoItems(next);
                    }}>
                      <option value="">選擇物料</option>
                      {skusMapped.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                    </select>
                    <input type="number" value={item.quantity} onChange={e => {
                      const next = [...poItems];
                      next[i].quantity = Number(e.target.value);
                      setPoItems(next);
                    }} placeholder="數量" />
                    <input type="number" value={item.unitPrice} onChange={e => {
                      const next = [...poItems];
                      next[i].unitPrice = Number(e.target.value);
                      setPoItems(next);
                    }} placeholder="單價" />
                    <button type="button" className="btn-icon danger" onClick={() => setPoItems(poItems.filter((_, idx) => idx !== i))}><Trash2 size={14} /></button>
                  </div>
                ))}
              </div>
            </div>
          )
        }}
      />

      <FormModal
        open={showAddPr || showEditPr}
        title={showEditPr ? `編輯請購單 ${selectedPr?.prNumber}` : "預建立請購單 (PR) - 加寬模式"}
        width={1240}
        fields={prFields}
        initialValues={showEditPr ? selectedPr : undefined}
        onClose={() => { setShowAddPr(false); setShowEditPr(false); setSelectedPr(null); setPrItems([]); }}
        onSubmit={async (values) => {
          if (showEditPr && selectedPr) {
            await purchaseRequisitionsApi.update(selectedPr.id, { ...values, items: prItems });
          } else {
            await purchaseRequisitionsApi.create({ ...values, items: prItems });
          }
          queryClient.invalidateQueries({ queryKey: ['purchase-requisitions'] });
          setShowAddPr(false);
          setShowEditPr(false);
        }}
        customRenderers={{
          _prItems: (
            <div className="pr-items-container">
               <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                 <h4 style={{ margin: 0, fontSize: 13, color: 'var(--gold-400)', fontWeight: 700 }}>精確請購明細 (高寬度視窗)</h4>
                 <button type="button" className="btn btn-outline btn-sm" style={{ borderColor: 'var(--gold-400)', color: 'var(--gold-400)' }} onClick={() => setPrItems([...prItems, { skuId: '', quantity: 1, unit: 'PCS', requiredDate: new Date().toISOString().slice(0,10), estimatedUnitPrice: 0, leadTime: 7 }])}>
                   <Plus size={14} style={{ marginRight: 6 }} /> 增加下單品項
                 </button>
               </div>
               <div style={{ overflowX: 'auto', borderRadius: 8, border: '1px solid rgba(255,255,255,0.05)' }}>
                 <table className="pr-edit-table">
                   <thead>
                     <tr>
                       <th style={{ width: 420 }}>品號 / 物料描述 / 規格規格 *</th>
                       <th style={{ width: 100 }}>需求數量 *</th>
                       <th style={{ width: 80 }}>採購單位</th>
                       <th style={{ width: 160 }}>需求交期 *</th>
                       <th style={{ width: 120 }}>預估單價 (TWD)</th>
                       <th style={{ width: 90 }}>前置(天)</th>
                       <th style={{ width: 50 }}>操作</th>
                     </tr>
                   </thead>
                   <tbody>
                     {prItems.map((item, i) => (
                       <tr key={i}>
                         <td>
                           <select className="form-input mandatory" style={{ width: '100%' }} value={item.skuId} onChange={e => {
                             const next = [...prItems];
                             next[i].skuId = e.target.value;
                             const sku = skusMapped.find(s => s.value === e.target.value);
                             if (sku) {
                               next[i].unit = sku.unit;
                               next[i].estimatedUnitPrice = sku.price;
                             }
                             setPrItems(next);
                           }}>
                             <option value="">請點選此處搜尋採購品項...</option>
                             {skusMapped.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                           </select>
                         </td>
                         <td><input type="number" className="form-input mandatory" value={item.quantity} onChange={e => {
                           const next = [...prItems];
                           next[i].quantity = Number(e.target.value);
                           setPrItems(next);
                         }} /></td>
                         <td><input type="text" className="form-input secondary" value={item.unit} readOnly /></td>
                         <td><input type="date" className="form-input mandatory" value={item.requiredDate} onChange={e => {
                           const next = [...prItems];
                           next[i].requiredDate = e.target.value;
                           setPrItems(next);
                         }} /></td>
                         <td><input type="number" className="form-input" value={item.estimatedUnitPrice} onChange={e => {
                           const next = [...prItems];
                           next[i].estimatedUnitPrice = Number(e.target.value);
                           setPrItems(next);
                         }} /></td>
                         <td><input type="number" className="form-input" value={item.leadTime} onChange={e => {
                           const next = [...prItems];
                           next[i].leadTime = Number(e.target.value);
                           setPrItems(next);
                         }} /></td>
                         <td style={{ textAlign: 'center' }}>
                           <button type="button" className="btn-icon danger" onClick={() => setPrItems(prItems.filter((_, idx) => idx !== i))}><Trash2 size={15} /></button>
                         </td>
                       </tr>
                     ))}
                   </tbody>
                 </table>
               </div>
            </div>
          )
        }}
      />

      <FormModal
        open={showAddSupplier || showEditSupplier}
        title={showEditSupplier ? '編輯供應商' : '新增供應商'}
        fields={supplierFields}
        initialValues={showEditSupplier ? (supplierData?.data || []).find((s:any) => s.id === editingSupplierId) as unknown as Record<string, unknown> : undefined}
        onClose={() => { setShowAddSupplier(false); setShowEditSupplier(false); setEditingSupplierId(null); }}
        onSubmit={async (values) => {
          if (showEditSupplier && editingSupplierId) {
            await suppliersApi.update(editingSupplierId, values as Record<string, unknown>);
          } else {
            await suppliersApi.create(values as Record<string, unknown>);
          }
          queryClient.invalidateQueries({ queryKey: ['suppliers'] });
          setShowAddSupplier(false);
          setShowEditSupplier(false);
          setEditingSupplierId(null);
        }}
      />
      {/* --- Purchase Order Detail Read-only View --- */}
      <div style={{ display: showPoDetail ? 'flex' : 'none', position: 'fixed', inset: 0, zIndex: 1000, alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.85)', backdropFilter: 'blur(10px)' }} onClick={() => setShowPoDetail(false)}>
        <div style={{ width: 850, background: '#040811', borderRadius: 24, padding: 32, border: '1px solid var(--lux-border)', boxShadow: '0 40px 100px rgba(0,0,0,0.8)' }} onClick={e => e.stopPropagation()}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 24 }}>
             <div>
               <div style={{ fontSize: 11, color: 'var(--gold-400)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.1em' }}>Purchase Order Intel</div>
               <h2 style={{ margin: '4px 0', fontSize: 24, color: '#fff' }}>採購訂單詳情 <span style={{ color: 'var(--gold-500)', fontSize: 16, marginLeft: 8 }}>{selectedPo?.poNumber}</span></h2>
             </div>
             <button style={{ background: 'none', border: 'none', color: 'var(--slate-500)', cursor: 'pointer' }} onClick={() => setShowPoDetail(false)}><Plus size={24} style={{ transform: 'rotate(45deg)' }} /></button>
          </div>
          
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24, marginBottom: 32, padding: 20, background: 'rgba(255,255,255,0.02)', borderRadius: 16 }}>
             <div><label style={{ display: 'block', fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>供應商</label><div style={{ fontWeight: 600 }}>{selectedPo?.supplierName}</div></div>
             <div><label style={{ display: 'block', fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>下單日期</label><div>{selectedPo?.poDate?.slice(0,10)}</div></div>
             <div><label style={{ display: 'block', fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>單據狀態</label><div style={{ fontWeight: 700, color: 'var(--gold-400)' }}>{statusConfig[selectedPo?.status]?.label || '待處理'}</div></div>
          </div>

          <div style={{ maxHeight: 400, overflowY: 'auto' }}>
            <table className="lux-table" style={{ background: 'transparent' }}>
               <thead>
                 <tr>
                   <th>品號 / 名稱</th><th style={{ textAlign: 'right' }}>採購量</th><th>單位</th><th style={{ textAlign: 'right' }}>單價 (TWD)</th><th style={{ textAlign: 'right' }}>小計</th>
                 </tr>
               </thead>
               <tbody>
                 {selectedPo?.items?.map((it: any) => (
                   <tr key={it.id}>
                     <td className="emp-label">{it.skuName} <div style={{ fontSize: 10, color: 'var(--slate-500)' }}>{it.skuNumber}</div></td>
                     <td className="mono-label" style={{ color: '#fff', textAlign: 'right' }}>{it.quantity}</td>
                     <td>{it.unit}</td>
                     <td className="amount-label">{it.unitPrice?.toLocaleString()}</td>
                     <td className="amount-label" style={{ color: 'var(--gold-400)' }}>{(it.quantity * it.unitPrice).toLocaleString()}</td>
                   </tr>
                 ))}
               </tbody>
               <tfoot>
                 <tr>
                   <td colSpan={4} style={{ textAlign: 'right', fontWeight: 700, padding: 16, color: 'var(--slate-500)' }}>總計金額 (TWD)</td>
                   <td className="amount-label" style={{ fontSize: 18, borderTop: '2px solid var(--gold-500)' }}>{selectedPo?.totalAmount?.toLocaleString()}</td>
                 </tr>
               </tfoot>
            </table>
          </div>
          <div style={{ marginTop: 24, padding: 16, borderTop: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
             <p style={{ fontSize: 12, color: 'var(--slate-400)', margin: 0 }}>備註資訊：{selectedPo?.notes || '無備註'}</p>
             <div style={{ display: 'flex', gap: 12 }}>
                <button className="btn btn-sm btn-outline" onClick={() => window.print()}><TrendingUp size={14} style={{ marginRight: 6 }} /> 列印 / 下載 PDF</button>
                <button className="btn btn-sm btn-primary" onClick={() => alert('已模擬發送 API 給供應商：' + selectedPo?.supplierName)}><Truck size={14} style={{ marginRight: 6 }} /> 發送給供應商</button>
             </div>
          </div>
        </div>
      </div>


      <div style={{ display: showPrDetail ? 'flex' : 'none', position: 'fixed', inset: 0, zIndex: 1000, alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.85)', backdropFilter: 'blur(10px)' }} onClick={() => setShowPrDetail(false)}>
        <div style={{ width: 850, background: '#040811', borderRadius: 24, padding: 32, border: '1px solid var(--lux-border)', boxShadow: '0 40px 100px rgba(0,0,0,0.8)' }} onClick={e => e.stopPropagation()}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 24 }}>
             <div>
               <div style={{ fontSize: 11, color: 'var(--gold-400)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.1em' }}>Requisition Intel</div>
               <h2 style={{ margin: 0, color: '#fff' }}>請購單詳細資訊</h2>
             </div>
             <button style={{ background: 'none', border: 'none', color: 'var(--slate-500)', cursor: 'pointer' }} onClick={() => setShowPrDetail(false)}><Plus size={24} style={{ transform: 'rotate(45deg)' }} /></button>
          </div>
          
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16, marginBottom: 24, padding: 20, background: 'rgba(255,255,255,0.02)', borderRadius: 16, border: '1px solid rgba(255,255,255,0.05)' }}>
             <div><span style={{ fontSize: 11, color: 'var(--slate-500)' }}>請購單號</span><div style={{ fontWeight: 600, color: 'var(--cobalt-400)', fontFamily: 'monospace' }}>{selectedPr?.prNumber}</div></div>
             <div><span style={{ fontSize: 11, color: 'var(--slate-500)' }}>用途摘要</span><div style={{ color: '#fff' }}>{selectedPr?.purpose || '常規生產補貨'}</div></div>
             <div><span style={{ fontSize: 11, color: 'var(--slate-500)' }}>緊急程度</span><div><span className={`badge-lux badge-${selectedPr?.priority === 'urgent' ? 'warning' : 'info'}`}>{selectedPr?.priority === 'urgent' ? '緊急' : '一般'}</span></div></div>
             <div><span style={{ fontSize: 11, color: 'var(--slate-500)' }}>預計總額</span><div style={{ color: 'var(--gold-400)', fontWeight: 700 }}>{selectedPr?.totalEstimatedAmount?.toLocaleString()} TWD</div></div>
          </div>

          <div style={{ maxHeight: '45vh', overflow: 'auto' }}>
              <table className="lux-table">
                <thead>
                  <tr>
                    <th>物料規格</th>
                    <th style={{ textAlign: 'right' }}>數量</th>
                    <th>單位</th>
                    <th>交期要求</th>
                    <th style={{ textAlign: 'right' }}>預估單價</th>
                    <th style={{ textAlign: 'right' }}>小計</th>
                  </tr>
                </thead>
                <tbody>
                  {isFetchingDetail ? (
                    <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40 }}><Loader2 className="spin" size={24} style={{ color: 'var(--gold-400)' }} /></td></tr>
                  ) : (!selectedPr?.items || selectedPr.items.length === 0) ? (
                    <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>此請購單目前無明細資料</td></tr>
                  ) : (
                    selectedPr.items.map((it:any) => (
                      <tr key={it.id}>
                        <td style={{ fontWeight: 600, color: '#fff' }}>{it.skuName}</td>
                        <td className="mono-label" style={{ textAlign: 'right' }}>{it.quantity}</td>
                        <td>{it.unit}</td>
                        <td className="date-label">{it.requiredDate?.slice(0,10)}</td>
                        <td className="amount-label" style={{ textAlign: 'right' }}>{it.estimatedUnitPrice?.toLocaleString()}</td>
                        <td className="amount-label" style={{ textAlign: 'right', color: 'var(--gold-400)', fontWeight: 700 }}>
                          {(it.quantity * it.estimatedUnitPrice)?.toLocaleString()}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 32 }}>
            <button className="btn btn-ghost" onClick={() => setShowPrDetail(false)}>關閉窗口</button>
          </div>
        </div>
      </div>

      {/* PR 審批 Modal */}
      {showPrApproval && selectedPr && (
        <div className="modal-overlay" onClick={() => setShowPrApproval(false)}>
          <div className="modal-content" style={{ maxWidth: 400 }} onClick={e => e.stopPropagation()}>
            <h3 style={{ marginBottom: 20, display: 'flex', alignItems: 'center', gap: 8 }}>
              {prApprovalAction === 'approve' ? <CheckCircle size={20} color="var(--emerald-400)" /> : 
               prApprovalAction === 'reject' ? <XCircle size={20} color="var(--rose-400)" /> : 
               prApprovalAction === 'return' ? <AlertCircle size={20} color="var(--amber-400)" /> :
               <Send size={20} color="var(--cobalt-400)" />}
              {prApprovalAction === 'submit' ? '送出審批' : prApprovalAction === 'approve' ? '核准請購單' : prApprovalAction === 'reject' ? '駁回請購單' : '退回補件'}
            </h3>
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontSize: 13, color: 'var(--slate-400)' }}>請購單</div>
              <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--slate-100)' }}>{selectedPr.prNumber}</div>
            </div>
            <div style={{ marginBottom: 16 }}>
              <label style={{ fontSize: 12, color: 'var(--slate-500)', display: 'block', marginBottom: 6 }}>意見 (選填)</label>
              <textarea
                className="form-input"
                rows={3}
                value={prApprovalComments}
                onChange={e => setPrApprovalComments(e.target.value)}
                placeholder="輸入審批意見..."
                style={{ width: '100%', resize: 'none' }}
              />
            </div>
            <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowPrApproval(false)}>取消</button>
              <button 
                className={`btn btn-sm ${prApprovalAction === 'approve' ? 'btn-primary' : prApprovalAction === 'reject' ? 'btn-danger' : 'btn-warning'}`}
                onClick={async () => {
                  const employeeId = 'current-user-id';
                  try {
                    if (prApprovalAction === 'submit') {
                      await purchaseRequisitionsApi.submit(selectedPr.id);
                    } else if (prApprovalAction === 'approve') {
                      await purchaseRequisitionsApi.approve(selectedPr.id, { employeeId, comments: prApprovalComments });
                    } else if (prApprovalAction === 'reject') {
                      await purchaseRequisitionsApi.reject(selectedPr.id, { employeeId, comments: prApprovalComments });
                    } else {
                      await purchaseRequisitionsApi.returnForEdit(selectedPr.id, { employeeId, comments: prApprovalComments });
                    }
                    queryClient.invalidateQueries({ queryKey: ['purchase-requisitions'] });
                    setShowPrApproval(false);
                    setPrApprovalComments('');
                  } catch (err) {
                    console.error('PR審批操作失敗', err);
                  }
                }}
              >
                {prApprovalAction === 'submit' ? '確認送出' : prApprovalAction === 'approve' ? '確認核准' : prApprovalAction === 'reject' ? '確認駁回' : '確認退回'}
              </button>
            </div>
          </div>
        </div>
      )}

      <style>{`
        .module-page { color: var(--slate-200); position: relative; }
        .module-page-header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid rgba(255,255,255,0.05); }
        .breadcrumb { font-size: 11px; color: var(--gold-400); text-transform: uppercase; letter-spacing: 0.1em; margin-bottom: 8px; }
        .module-page-title { font-size: 24px; font-weight: 800; color: #fff; background: linear-gradient(90deg, #fff, var(--gold-400)); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
        
        /* KPI Cards */
        .kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px; }
        .stat-card { padding: 20px; transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1); background: #040811 !important; border-radius: 16px !important; }
        .stat-card:hover { transform: translateY(-4px); box-shadow: 0 10px 30px rgba(0,0,0,0.5); }
        .stat-label { font-size: 11px; color: var(--slate-500); font-weight: 700; text-transform: uppercase; margin-bottom: 8px; }
        .stat-value { font-size: 22px; font-weight: 800; color: #fff; letter-spacing: -0.02em; }
        .stat-icon { width: 40px; height: 40px; border-radius: 10px; display: flex; align-items: center; justify-content: center; }
        .stat-trend { margin-top: 12px; font-size: 11px; display: flex; align-items: center; font-weight: 600; }

        /* Tabs & Navigation */
        .view-selector { display: flex; gap: 8px; margin-bottom: 20px; background: rgba(0,0,0,0.3); padding: 4px; border-radius: 12px; width: fit-content; border: 1px solid rgba(255,255,255,0.03); }
        .view-tab { display: flex; align-items: center; gap: 8px; padding: 8px 16px; border: none; background: none; color: var(--slate-500); cursor: pointer; border-radius: 8px; font-size: 13px; font-weight: 600; transition: all 0.2s; }
        .view-tab.active { background: rgba(212,175,55,0.1); color: var(--lux-gold); border: 1px solid rgba(212,175,55,0.2); }
        
        /* Tables */
        .lux-table { width: 100%; border-collapse: separate; border-spacing: 0 0; }
        .lux-table th { padding: 14px 20px; text-align: left; font-size: 11px; font-weight: 700; color: var(--slate-500); text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid rgba(255,255,255,0.05); }
        .lux-table td { padding: 16px 20px; font-size: 13px; border-bottom: 1px solid rgba(255,255,255,0.02); }
        .mono-label { font-family: 'JetBrains Mono', monospace; font-size: 12px; color: var(--cobalt-400); }
        .amount-label { font-family: 'JetBrains Mono', monospace; color: var(--amber-400); font-weight: 600; text-align: right; }
        
        /* Badges */
        .badge-lux { padding: 4px 10px; border-radius: 6px; font-size: 11px; font-weight: 700; }
        .badge-info { background: rgba(56,189,248,0.1); color: #38bdf8; }
        .badge-warning { background: rgba(251,191,36,0.1); color: #fbbf24; }
        .badge-success { background: rgba(16,185,129,0.1); color: #10b981; }
        .rating-badge { display: inline-flex; padding: 3px 8px; border-radius: 4px; font-size: 12px; font-weight: 800; border: 1px solid rgba(255,255,255,0.05); }

        /* Actions */
        .action-row { display: flex; gap: 8px; align-items: center; }
        .action-btn { width: 28px; height: 28px; display: flex; align-items: center; justify-content: center; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05); border-radius: 6px; color: var(--slate-400); cursor: pointer; transition: all 0.2s; }
        .action-btn:hover { background: rgba(255,255,255,0.08); color: var(--lux-gold); }
        
        /* Forms & PR/PO Editors */
        .form-input.mandatory { border-left: 3px solid #fbbf24 !important; }
        .form-input.secondary { background: rgba(255,255,255,0.02); color: var(--slate-500); cursor: not-allowed; }
        .items-editor, .pr-items-container { padding: 16px; background: rgba(0,0,0,0.2); border-radius: 12px; margin-top: 12px; border: 1px solid rgba(255,255,255,0.05); }
        .editor-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; border-bottom: 1px solid rgba(255,255,255,0.05); padding-bottom: 8px; }
        .item-row { display: grid; grid-template-columns: 3fr 1fr 1.5fr 40px; gap: 12px; margin-bottom: 8px; }
        .pr-edit-table { width: 100%; border-collapse: collapse; }
        .pr-edit-table th { padding: 10px 12px; text-align: left; font-size: 11px; color: var(--slate-500); border-bottom: 2px solid rgba(255,255,255,0.05); text-transform: uppercase; }
        .pr-edit-table td { padding: 8px; }

        .spin { animation: spin 1s linear infinite; }
        @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
      `}</style>

      {/* 供應商新增 Modal */}
      <FormModal
        open={showAddSupplier}
        title="新增供應商"
        width={700}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['基本資料', '聯絡與地址', '交易條款'],
          getStepFields: (idx: number) => {
            const basic: FormField[] = [
              { name: 'supplierCode', label: '供應商代碼 *', type: 'text', required: true, placeholder: '如: SUP-001' },
              { name: 'supplierName', label: '供應商全名 *', type: 'text', required: true, placeholder: '輸入正式公司全銜' },
              { name: 'taxId', label: '統一編號', type: 'text', placeholder: '8 位數字' }
            ];
            const contact: FormField[] = [
              { name: 'contactPerson', label: '主要聯絡人', type: 'text', placeholder: '聯絡人姓名' },
              { name: 'phone', label: '聯繫電話', type: 'text', placeholder: '02-xxxx-xxxx' },
              { name: 'email', label: '電子郵件', type: 'text', placeholder: 'email@example.com' },
              { name: 'invoiceAddress', label: '發票地址', type: 'text', placeholder: '輸入完整地址' },
              { name: 'deliveryAddress', label: '送貨地址', type: 'text', placeholder: '若與發票地址相同可留空' }
            ];
            const payment: FormField[] = [
              { name: 'paymentTerms', label: '付款條件', type: 'select', options: [
                { value: '月結30天', label: '月結 30 天' },
                { value: '月��60天', label: '月結 60 天' },
                { value: '貨到付款', label: '貨到付款' },
                { value: '預付', label: '預付款 (TT)' }
              ]},
              { name: 'currency', label: '交易幣別', type: 'select', options: [
                { value: 'TWD', label: 'TWD - 新台幣' },
                { value: 'USD', label: 'USD - 美元' },
                { value: 'JPY', label: 'JPY - 日圓' },
                { value: 'EUR', label: 'EUR - 歐元' }
              ]}
            ];
            return [basic, contact, payment][idx];
          }
        }}
        onClose={() => setShowAddSupplier(false)}
        onSubmit={async (data) => {
          await suppliersApi.create(data);
          queryClient.invalidateQueries({ queryKey: ['suppliers'] });
          setShowAddSupplier(false);
        }}
      />

      {/* 供應商編輯 Modal */}
      <FormModal
        open={showEditSupplier}
        title="編輯供應商"
        width={700}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['基本資料', '聯絡與地址', '交易條款'],
          getStepFields: (idx: number) => {
            const basic: FormField[] = [
              { name: 'supplierCode', label: '供應商代碼 *', type: 'text', required: true },
              { name: 'supplierName', label: '供應商全名 *', type: 'text', required: true },
              { name: 'taxId', label: '統一編號', type: 'text' }
            ];
            const contact: FormField[] = [
              { name: 'contactPerson', label: '主要聯絡人', type: 'text' },
              { name: 'phone', label: '聯繫電話', type: 'text' },
              { name: 'email', label: '電子郵件', type: 'text' },
              { name: 'invoiceAddress', label: '發票地址', type: 'text' },
              { name: 'deliveryAddress', label: '送貨地址', type: 'text' }
            ];
            const payment: FormField[] = [
              { name: 'paymentTerms', label: '付款條件', type: 'select', options: [
                { value: '月結30天', label: '月結 30 天' },
                { value: '月結60天', label: '月結 60 天' },
                { value: '貨到付款', label: '貨到付款' },
                { value: '預付', label: '預付款 (TT)' }
              ]},
              { name: 'currency', label: '交易幣別', type: 'select', options: [
                { value: 'TWD', label: 'TWD - 新台幣' },
                { value: 'USD', label: 'USD - 美元' },
                { value: 'JPY', label: 'JPY - 日圓' },
                { value: 'EUR', label: 'EUR - 歐元' }
              ]}
            ];
            return [basic, contact, payment][idx];
          }
        }}
        onClose={() => { setShowEditSupplier(false); setEditingSupplierId(null); }}
        onSubmit={async (data) => {
          if (editingSupplierId) {
            await suppliersApi.update(editingSupplierId, data);
            queryClient.invalidateQueries({ queryKey: ['suppliers'] });
          }
          setShowEditSupplier(false);
        }}
      />
    </div>
  );
};
