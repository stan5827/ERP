import React, { useState, useEffect } from 'react';

import { useQueryClient, useQuery, useMutation } from '@tanstack/react-query';

import { Search, Plus, AlertTriangle, Download, Eye, TrendingDown, RefreshCw, Timer, ArrowLeftRight, Edit2, Trash2, ClipboardCheck, Check, Truck, Package, CheckCircle, Loader2 } from 'lucide-react';

import { useSkus, useLowStock, useWarehouses, useUoms, useBinLocations, useUomConversions } from '../hooks/useApi';

import { FormModal } from '../components/FormModal';

import type { FormField } from '../components/FormModal';

import { skusApi, suppliersApi, goodsReceiptsApi, binLocationsApi, warehousesApi, inventoryApi, inventoryTransfersApi, completionOrdersApi, deliveryOrdersApi, rccpApi } from '../api/api';

// 新增：SKU 庫存詳細資訊組件
const SkuInventoryDetail: React.FC<{ sku: string }> = ({ sku }) => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!sku) return;
    const load = async () => {
      setLoading(true);
      try {
        const res = await inventoryApi.checkATP(sku, 0);
        setData(res.data);
      } catch (e) {}
      setLoading(false);
    };
    load();
  }, [sku]);

  if (loading) return <Loader2 size={12} className="animate-spin" />;
  if (!data) return null;

  return (
    <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
      <div style={{ display: 'flex', flexDirection: 'column' }}>
        <span style={{ fontSize: '0.65rem', color: 'var(--slate-500)' }}>現有</span>
        <span style={{ fontSize: '0.85rem', fontWeight: 700, color: 'var(--slate-200)' }}>{data.on_hand}</span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column' }}>
        <span style={{ fontSize: '0.65rem', color: 'var(--amber-500)' }}>待出</span>
        <span style={{ fontSize: '0.85rem', fontWeight: 700, color: 'var(--amber-400)' }}>{data.allocated}</span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column' }}>
        <span style={{ fontSize: '0.65rem', color: 'var(--emerald-500)' }}>可用</span>
        <span style={{ fontSize: '0.9rem', fontWeight: 800, color: 'var(--lux-gold)' }}>{data.available}</span>
      </div>
    </div>
  );
};

const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '料號 *', type: 'text', required: true, placeholder: '例如M01-STL-001', description: '請輸入產品編號系統將自動檢查是否重複' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '請輸入產品品名', description: '元件顯示名稱' },
  { name: 'specification', label: '規格說明', type: 'text', placeholder: '例如304 2.0mm', description: '產品的物理或尺寸說明' },
  { name: 'category', label: '分類', type: 'text', placeholder: '例如M01', description: '物料大類' },
];

const sourceTypeFields: FormField[] = [
  { name: 'sourceType', label: '來源類型 *', type: 'select', required: true, options: [
    { value: 'make', label: '自製' },
    { value: 'buy', label: '外購' },
    { value: 'make_or_buy', label: '自製+外購' },
    { value: 'outsource', label: '委外加工' },
    { value: 'make_or_outsource', label: '自製+委外' },
  ], defaultValue: 'make', description: '定義該物料的主要來源方式' },
  { name: 'itemType', label: '品項類別', type: 'select', options: [
    { value: 'semi_finished', label: '半成品' },
    { value: 'finished', label: '成品' },
    { value: 'material', label: '原料' },
    { value: 'virtual', label: '虛擬品' },
  ], description: '定義品項在 BOM 與 MRP 中的處理邏輯' },
  { name: 'barcode', label: '條碼', type: 'text', placeholder: '例如EAN13', description: '產品外部條碼或二維碼' },
  { name: 'model', label: '型號', type: 'text', placeholder: '例如XL-2000', description: '產品的規格型號' },
  { name: 'brand', label: '品牌', type: 'text', placeholder: '例如SONY' },
  { name: 'color', label: '顏色', type: 'text', placeholder: '例如黑色' },
  { name: 'drawingNo', label: '圖號', type: 'text', placeholder: '例如DRW-001', description: '產品設計圖紙編號' },
  { name: 'drawingRevision', label: '圖面版次', type: 'text', placeholder: '例如V1.0', description: '設計圖紙版本控制' },
  { name: 'effectiveFrom', label: '生效日期', type: 'date', description: '產品開始生效日期' },
  { name: 'effectiveTo', label: '失效日期', type: 'date', description: '產品失效截止日期' },
  { name: 'shelfLifeDays', label: '保存期限 (天)', type: 'number', placeholder: '0', min: 0, defaultValue: 0, description: '產品效期天數' },
];

const inventoryFields = (warehouseOptions: { value: string; label: string }[], binOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'abcClassification', label: 'ABC 分類', type: 'select', options: [
    { value: 'A', label: 'A - 重要物料' },
    { value: 'B', label: 'B - 一般物料' },
    { value: 'C', label: 'C - 次要物料' },
  ], description: '依據 ABC 價值分析分類' },
  { name: 'safetyStock', label: '安全庫存量', type: 'number', placeholder: '0', min: 0, description: '系統建議最低庫存水位' },
  { name: 'reorderPoint', label: '再訂購點(ROP)', type: 'number', placeholder: '0', min: 0, description: '觸發採購或是建議的庫存量點' },
  { name: 'maxStock', label: '最大庫存量', type: 'number', placeholder: '0', min: 0, description: '倉儲可容納的最大庫存量' },
  { name: 'avgDailyConsumption', label: '平均日耗量', type: 'number', placeholder: '0', min: 0, step: 0.01, description: '用於計算再訂購點的參數數據' },
  { name: 'purchasePriceCeiling', label: '採購漲幅上限(%)', type: 'number', placeholder: '0', min: 0, max: 100, step: 0.1, description: '採購核價對比標準價的最大張幅比例' },
  { name: 'lotControlled', label: '批次管理', type: 'checkbox', placeholder: '啟用批次管制', description: '是否需要追蹤每一批入庫貨品的批次資料' },
  { name: 'serialControlled', label: '序號管理', type: 'checkbox', placeholder: '啟用序號管制', description: '是否需要紀錄每一件產品的獨立序號資訊' },
  { name: 'iqcRequired', label: '進料檢驗(IQC)', type: 'checkbox', placeholder: '啟用 IQC', defaultValue: true, description: '入庫前是否需要進行品質檢驗' },
  { name: 'oqcRequired', label: '出貨檢驗(OQC)', type: 'checkbox', placeholder: '啟用 OQC', description: '出庫前是否需要進行品質檢驗' },
  { name: 'defaultwarehouseId', label: '預設存放倉庫', type: 'select', options: warehouseOptions, description: '系統預設的收貨目的地址' },
  { name: 'defaultBinLocationId', label: '預設存放儲位', type: 'select', options: binOptions, description: '系統預設的收貨目的地儲位' },
];

const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'basePrice', label: '基本原價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceA', label: '價格 A (大宗客戶)', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceB', label: '價格 B (一般客戶)', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceC', label: '價格 C', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceD', label: '價格 D', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceE', label: '價格 E', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'floorPrice', label: '最低銷售價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'discountRate', label: '銷售折扣 (%)', type: 'number', placeholder: '0', min: 0, max: 100, step: 0.1, defaultValue: 0 },
  { name: 'commissionAccount', label: '提成帳戶', type: 'text', placeholder: '輸入帳戶' },
  { name: 'relatedSkuId', label: '關聯料號', type: 'text', placeholder: '輸入料號' },
  { name: 'customerItemCode', label: '客戶端料號', type: 'text' },
  { name: 'currency', label: '幣別', type: 'select', options: [
      { value: 'TWD', label: 'TWD - 新台幣' },
      { value: 'USD', label: 'USD - 美金' },
      { value: 'CNY', label: 'CNY - 人民幣' },
      { value: 'JPY', label: 'JPY - 日幣' },
  ], defaultValue: 'TWD', description: '報價使用幣別' },
  { name: 'taxCode', label: '稅別碼', type: 'text', placeholder: '例如 TX1', defaultValue: 'TX1' },
  { name: 'taxRate', label: '稅率 (%)', type: 'text', placeholder: '例如 5%', defaultValue: '5%' },
];

const costFields = (uomOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number', placeholder: '0', min: 0, step: 0.01, description: '預設採購成本' },
  { name: 'standardCost', label: '標準成本', type: 'number', placeholder: '0', min: 0, step: 0.01, description: '財務核算標準成本' },
  { name: 'latestPurchaseCost', label: '最近購買單價', type: 'number', placeholder: '0', min: 0, step: 0.01, description: '最後一筆進貨實績單價' },
  { name: 'costMethod', label: '成本計價方式', type: 'select', options: [
    { value: 'standard', label: '標準成本' },
    { value: 'ma', label: '移動平均' },
    { value: 'fifo', label: '先進先出' },
  ], defaultValue: 'ma' },
  { name: 'uomId', label: '基本單位', type: 'select', options: uomOptions, required: true },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'salesUomId', label: '銷售單位', type: 'select', options: uomOptions },
  { name: 'productionUomId', label: '生產單位', type: 'select', options: uomOptions },
];

function TransactionsPanel() {

  const [filterType, setFilterType] = useState('all');

  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery({

    queryKey: ['inventory-transactions', filterType],

    queryFn: () => inventoryApi.transactions(filterType !== 'all' ? { type: filterType } : undefined).then(res => res.data)

  });

  const transactions = data?.data || [];

  const getStatusLabel = (status: string) => {

    const map: any = { 'completed': '已完成', 'pending': '待處理', 'cancelled': '已取消' };

    return map[status] || status;

  };

  return (

    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', gap: 8 }}>

          {['all', 'in', 'out', 'transfer', 'adj'].map(t => (

            <button key={t} className={`btn btn-sm ${filterType === t ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setFilterType(t)}>

              {t === 'all' ? '全部' : t === 'in' ? '入庫' : t === 'out' ? '出庫' : t === 'transfer' ? '撥移' : '調整'}

            </button>

          ))}

        </div>

      </div>

      <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>

        <table className="data-table">

          <thead>

            <tr>

              <th>交易日期</th>

              <th>類型</th>

              <th>物料名稱</th>

              <th>變動數量</th>

              <th>來源/目的</th>

              <th>狀態</th>

            </tr>

          </thead>

          <tbody>

            {isLoading ? (

              <tr><td colSpan={6} style={{ textAlign: 'center', padding: 20 }}>載入中...</td></tr>
            ) : transactions.length === 0 ? (
              <tr><td colSpan={6} style={{ textAlign: 'center', padding: 20 }}>無交易紀錄</td></tr>

            ) : transactions.map((t: any) => (

              <tr key={t.id}>

                <td style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{new Date(t.createdAt).toLocaleDateString()}</td>
                <td><span className={`status-badge ${t.type}`}>{t.type === 'in' ? '入庫' : t.type === 'out' ? '出庫' : '異動'}</span></td>
                <td>{t.sku?.skuName || '-'}</td>
                <td style={{ fontWeight: 700, color: t.quantity > 0 ? 'var(--emerald-400)' : 'var(--crimson-400)' }}>{t.quantity > 0 ? '+' : ''}{t.quantity}</td>
                <td>{t.warehouse?.name || '-'}</td>
                <td>{getStatusLabel(t.status)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const CapacityBillManager: React.FC<{ skuId: string }> = ({ skuId }) => {
  const [items, setItems] = useState<any[]>([]);
  const [resources, setResources] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!skuId) return;
    load();
  }, [skuId]);

  const load = async () => {
    setLoading(true);
    try {
      const [resList, irList] = await Promise.all([
        rccpApi.listResources(),
        rccpApi.getItemResources(skuId)
      ]);
      setResources(resList.data?.data || []);
      setItems(irList.data?.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const addItem = () => {
    setItems([...items, { resourceId: '', usagePerUnit: 1, uom: 'Hrs' }]);
  };

  const removeItem = (idx: number) => {
    setItems(items.filter((_, i) => i !== idx));
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await rccpApi.updateItemResources(items.map(it => ({ ...it, skuId })));
      alert('產能設定已儲存');
    } catch (e) {
      alert('儲存失敗');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div style={{ padding: 20, textAlign: 'center' }}><Loader2 className="animate-spin" /></div>;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h4 style={{ fontSize: 13, color: 'var(--lux-gold)' }}>關鍵資源耗用 (Capacity Bill)</h4>
        <button type="button" className="btn btn-ghost btn-sm" onClick={addItem}><Plus size={12} /> 添加資源</button>
      </div>
      <table className="data-table" style={{ fontSize: 12 }}>
        <thead>
          <tr>
            <th>資源</th>
            <th>標準耗用</th>
            <th>單位</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item, idx) => (
            <tr key={idx}>
              <td>
                <select 
                  className="form-input" 
                  style={{ width: '100%', padding: '4px 8px' }}
                  value={item.resourceId}
                  onChange={e => {
                    const next = [...items];
                    next[idx].resourceId = e.target.value;
                    setItems(next);
                  }}
                >
                  <option value="">選擇資源</option>
                  {resources.map(r => <option key={r.id} value={r.id}>{r.resourceName}</option>)}
                </select>
              </td>
              <td>
                <input 
                  type="number" 
                  className="form-input" 
                  style={{ width: 80, padding: '4px 8px' }}
                  value={item.usagePerUnit}
                  onChange={e => {
                    const next = [...items];
                    next[idx].usagePerUnit = parseFloat(e.target.value) || 0;
                    setItems(next);
                  }}
                />
              </td>
              <td className="mono">{item.uom}</td>
              <td>
                <button type="button" className="btn-action" onClick={() => removeItem(idx)}><Trash2 size={12} /></button>
              </td>
            </tr>
          ))}
          {items.length === 0 && (
            <tr><td colSpan={4} style={{ textAlign: 'center', padding: 20, color: 'var(--slate-500)' }}>未設定產能資源耗用</td></tr>
          )}
        </tbody>
      </table>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 8 }}>
        <button type="button" className="btn btn-primary btn-sm" onClick={handleSave} disabled={saving}>
          {saving ? '儲存中...' : '儲存產能設定'}
        </button>
      </div>
    </div>
  );
};

function CountPanel() {
  const [countMode, setCountMode] = useState('pending');
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', gap: 8 }}>
          {['pending', 'in_progress', 'completed'].map(m => (
            <button key={m} className={`btn btn-sm ${countMode === m ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setCountMode(m as any)}>
              {m === 'pending' ? '待盤點' : m === 'in_progress' ? '盤點中' : '已完成'}
            </button>
          ))}
        </div>
        <button className="btn btn-primary btn-sm" disabled title="功能開發中">
          <Plus size={13} />建立盤點計畫
        </button>
      </div>
      <div className="glass-card no-lift" style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>
        請選擇盤點倉庫與日期範圍開始執行盤點作業
      </div>
    </div>
  );
}

function AnalysisPanel() {
  const abcData = [
    { cls: "A", label: "重要物料", pct: 80, items: 20, color: "var(--cobalt-400)", desc: "價值佔比約 80%需嚴格控管" },
    { cls: "B", label: "一般物料", pct: 15, items: 30, color: "var(--amber-400)", desc: "價值佔比約 15%定期盤點" },
    { cls: "C", label: "次要物料", pct: 5,  items: 50, color: "var(--slate-400)", desc: "品項數量佔 50%價值佔比 5%" },
  ];

  const summaryCards = [
    { label: '當前庫存總價值', value: 'NT$ 3,842,600', color: 'var(--cobalt-300)' },
    { label: '低於安全水位', value: '12 項', color: 'var(--amber-300)' },
    { label: '逾期品項警示', value: '5 項', color: 'var(--crimson-300)' },
    { label: '最後 MRP 運行', value: '今日 08:00', color: 'var(--emerald-300)' },
  ];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
        {summaryCards.map(c => (
          <div key={c.label} className="glass-card" style={{ padding: '14px 16px' }}>
            <div style={{ fontSize: 11, color: 'var(--slate-500)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 8 }}>{c.label}</div>
            <div style={{ fontSize: 18, fontWeight: 700, color: c.color, fontFamily: 'var(--font-mono)' }}>{c.value}</div>
          </div>
        ))}
      </div>

      <div className="glass-card no-lift" style={{ padding: 20 }}>
        <h3 style={{ fontSize: 14, fontWeight: 700, marginBottom: 16, color: "var(--slate-100)" }}>ABC 庫存分配分析 (價值佔比)</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {abcData.map(item => (
            <div key={item.cls}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
                <div style={{ width: 24, height: 24, borderRadius: 6, background: `${item.color}20`, border: `1px solid ${item.color}40`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 800, color: item.color }}>
                  {item.cls}
                </div>
                <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>{item.label}</span>
                <span style={{ fontSize: 11, color: 'var(--slate-500)', marginLeft: 4 }}>{item.desc}</span>
                <span style={{ marginLeft: 'auto', fontSize: 12, fontWeight: 700, color: item.color }}>{item.pct}%</span>
              </div>
              <div className="progress-bar">
                <div className="progress-fill" style={{ width: `${item.pct}%`, background: item.color }} />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="glass-card no-lift" style={{ padding: 20 }}>
        <h3 style={{ fontSize: 14, fontWeight: 700, marginBottom: 8, color: "var(--slate-100)" }}>死滯/低週轉警示
          <span style={{ fontSize: 11, fontWeight: 400, color: "var(--slate-500)", marginLeft: 8 }}>超過 180 天未異動品項</span>
        </h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {[].length === 0 && (
            <div style={{ padding: '20px', textAlign: 'center', color: 'var(--slate-500)', fontSize: 12 }}>
              目前無符合死滯預警的項目
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function TransferPanel() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [filterStatus, setFilterStatus] = useState('all');
  const [search, setSearch] = useState('');
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['inventory-transfers', filterStatus],
    queryFn: () => inventoryTransfersApi.list({ status: filterStatus !== 'all' ? filterStatus : undefined }).then(res => res.data)
  });

  const { data: warehouseData } = useWarehouses(false);
  const warehouses = warehouseData?.data || [];
  const transfers = data?.data || [];

  const createMutation = useMutation({
    mutationFn: (data: Record<string, unknown>) => inventoryTransfersApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory-transfers'] });
      setShowCreateModal(false);
      alert('調撥單已成功建立');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '建立失敗');
    }
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) => inventoryTransfersApi.approve(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory-transfers'] });
      alert('核准作業完成');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '核准失敗');
    }
  });

  const executeMutation = useMutation({
    mutationFn: (id: string) => inventoryTransfersApi.execute(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory-transfers'] });
      alert('調撥執行完成');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '執行失敗');
    }
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => inventoryTransfersApi.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory-transfers'] });
      alert('作廢作業完成');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '作廢失敗');
    }
  });

  const getStatusBadge = (status: string) => {
    const map: Record<string, { label: string; className: string }> = {
      draft: { label: '草稿', className: 'status-neutral' },
      approved: { label: '已核准', className: 'status-active' },
      completed: { label: '已完成', className: 'info' },
      cancelled: { label: '已取消', className: 'warning' }
    };
    return map[status] || { label: status, className: 'status-neutral' };
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
        <button className="btn btn-ghost btn-sm"><Download size={13} />匯出報表</button>
        <button className="btn btn-primary btn-sm" onClick={() => setShowCreateModal(true)}><Plus size={13} />新增調撥單</button>
      </div>

      <div className="module-filter-bar glass-card no-lift">
        <select className="form-input" style={{ width: 140 }} value={filterStatus} onChange={e => setFilterStatus(e.target.value)}>
          <option value="all">所有狀態</option>
          <option value="draft">草稿</option>
          <option value="approved">已核准</option>
          <option value="completed">已完成</option>
          <option value="cancelled">已取消</option>
        </select>
        <div className="filter-search">
          <Search size={14} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋調撥單號..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>載入中...</div>
        ) : transfers.length === 0 ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>目前無調撥紀錄</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>調撥單號</th>
                <th>狀態</th>
                <th>調出倉庫</th>
                <th>調入倉庫</th>
                <th>建立日期</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {transfers.filter((t: any) => !search || t.transferNumber?.includes(search)).map((t: any) => {
                const badge = getStatusBadge(t.status);
                return (
                  <tr key={t.id}>
                    <td style={{ color: 'var(--cobalt-300)', fontFamily: 'var(--font-mono)' }}>{t.transferNumber}</td>
                    <td><span className={`status-badge ${badge.className}`}>{badge.label}</span></td>
                    <td>{warehouses.find((w: any) => w.id === t.sourceWarehouseId)?.warehouseName || t.sourceWarehouseId}</td>
                    <td>{warehouses.find((w: any) => w.id === t.targetWarehouseId)?.warehouseName || t.targetWarehouseId}</td>
                    <td>{t.createdAt?.slice(0, 10)}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {t.status === 'draft' && (
                          <>
                            <button className="icon-action-btn" title="核准" onClick={() => approveMutation.mutate(t.id)}><ClipboardCheck size={13} /></button>
                            <button className="icon-action-btn" title="作廢" onClick={() => { if (confirm('確定要作廢此調撥單？')) cancelMutation.mutate(t.id) }}><Trash2 size={13} /></button>
                          </>
                        )}
                        {t.status === 'approved' && (
                          <button className="icon-action-btn" title="執行調撥" onClick={() => { if (confirm('確定要執行調撥？')) executeMutation.mutate(t.id) }}><ArrowLeftRight size={13} /></button>
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

      {showCreateModal && (
        <TransferCreateModal
          warehouses={warehouses}
          onClose={() => setShowCreateModal(false)}
          onSubmit={(data) => createMutation.mutate(data)}
          isSubmitting={createMutation.isPending}
        />
      )}
    </div>
  );
}

function TransferCreateModal({ warehouses, onClose, onSubmit, isSubmitting }: {
  warehouses: any[];
  onClose: () => void;
  onSubmit: (data: Record<string, unknown>) => void;
  isSubmitting: boolean;
}) {
  const [form, setForm] = useState({ sourceWarehouseId: '', targetWarehouseId: '', items: [] as any[] });
  const [showAddItem, setShowAddItem] = useState(false);

  const { data: skuData } = useQuery({ queryKey: ['skus'], queryFn: () => skusApi.list({ pageSize: '1000' }) });
  const skus = skuData?.data?.data || [];

  const addItem = (item: any) => {
    setForm({ ...form, items: [...form.items, { ...item, id: Date.now() }] });
    setShowAddItem(false);
  };

  const removeItem = (id: number) => {
    setForm({ ...form, items: form.items.filter((i: any) => i.id !== id) });
  };

  const handleSubmit = () => {
    if (!form.sourceWarehouseId || !form.targetWarehouseId || form.items.length === 0) {
      alert('請填寫完整資訊並添加明細');
      return;
    }
    onSubmit(form);
  };

  return (
    <div style={{ position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', paddingTop: 60 }}>
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.8)', backdropFilter: 'blur(4px)' }} onClick={onClose} />
      <div className="glass-card" style={{ position: 'relative', width: 600, zIndex: 1, padding: 24, maxHeight: '85vh', overflowY: 'auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <h3 style={{ fontSize: 16, fontWeight: 700, color: 'var(--slate-100)' }}>建立調撥單</h3>
          <button className="icon-action-btn" onClick={onClose}><Plus size={18} style={{ transform: 'rotate(45deg)' }} /></button>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div>
              <label style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, display: 'block' }}>調出倉庫 *</label>
              <select className="form-input" style={{ width: '100%' }} value={form.sourceWarehouseId} onChange={e => setForm({ ...form, sourceWarehouseId: e.target.value })}>
                <option value="">請選擇</option>
                {warehouses.map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}</option>)}
              </select>
            </div>
            <div>
              <label style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, display: 'block' }}>調入倉庫 *</label>
              <select className="form-input" style={{ width: '100%' }} value={form.targetWarehouseId} onChange={e => setForm({ ...form, targetWarehouseId: e.target.value })}>
                <option value="">請選擇</option>
                {warehouses.map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}</option>)}
              </select>
            </div>
          </div>
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
              <label style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-400)' }}>調撥明細 *</label>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowAddItem(true)}><Plus size={13} />選擇品項</button>
            </div>
            {form.items.length === 0 ? (
              <div style={{ padding: 20, textAlign: 'center', color: 'var(--slate-500)', background: 'rgba(45,55,72,0.3)', borderRadius: 'var(--radius-md)', fontSize: 13 }}>請添加品項...</div>
            ) : (
              <table className="data-table" style={{ fontSize: 12 }}>
                <thead><tr><th>料號</th><th>品名</th><th>數量</th><th>批號</th><th></th></tr></thead>
                <tbody>
                  {form.items.map((item: any) => (
                    <tr key={item.id}>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>{item.skuNumber}</td>
                      <td>{item.skuName}</td>
                      <td>
                        <input type="number" className="form-input" style={{ width: 60, padding: '2px 4px' }} value={item.transferQty} onChange={e => {
                          const val = Number(e.target.value);
                          setForm({ ...form, items: form.items.map(i => i.id === item.id ? { ...i, transferQty: val } : i) });
                        }} />
                      </td>
                      <td>{item.lotNumber || '-'}</td>
                      <td><button className="icon-action-btn" onClick={() => removeItem(item.id)}><Trash2 size={12} /></button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 24, paddingTop: 16, borderTop: '1px solid var(--slate-700)' }}>
          <button className="btn btn-ghost" onClick={onClose}>取消</button>
          <button className="btn btn-primary" onClick={handleSubmit} disabled={isSubmitting}>{isSubmitting ? '建立中...' : '建立調撥單'}</button>
        </div>
      </div>

      {showAddItem && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 1001, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.9)' }} onClick={() => setShowAddItem(false)} />
          <div className="glass-card" style={{ position: 'relative', width: 500, zIndex: 1, padding: 20 }}>
            <h4 style={{ marginBottom: 16 }}>選擇品項</h4>
            <div style={{ maxHeight: 300, overflowY: 'auto' }}>
              {skus.map((s: any) => (
                <div key={s.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 12px', borderBottom: '1px solid var(--slate-700)', cursor: 'pointer' }} onClick={() => addItem({ skuId: s.id, skuNumber: s.skuNumber, skuName: s.skuName, transferQty: 1, lotNumber: '' })}>
                  <span style={{ fontFamily: 'var(--font-mono)' }}>{s.skuNumber}</span>
                  <span>{s.skuName}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function CompletionPanel() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [filterStatus, setFilterStatus] = useState('all');
  const [search, setSearch] = useState('');
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['completion-orders', filterStatus],
    queryFn: () => completionOrdersApi.list({ status: filterStatus !== 'all' ? filterStatus : undefined }).then(res => res.data)
  });

  const { data: warehouseData } = useWarehouses(false);
  const warehouses = warehouseData?.data || [];
  const completions = data?.data || [];

  const createMutation = useMutation({
    mutationFn: (data: Record<string, unknown>) => completionOrdersApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['completion-orders'] });
      setShowCreateModal(false);
      alert('完工入庫單已成功建立');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '建立失敗');
    }
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => completionOrdersApi.complete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['completion-orders'] });
      alert('核准入庫完成');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '核准失敗');
    }
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => completionOrdersApi.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['completion-orders'] });
      alert('作廢作業完成');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '作廢失敗');
    }
  });

  const getStatusBadge = (status: string) => {
    const map: Record<string, { label: string; className: string }> = {
      draft: { label: '草稿', className: 'status-neutral' },
      completed: { label: '已完成', className: 'info' },
      cancelled: { label: '已取消', className: 'warning' }
    };
    return map[status] || { label: status, className: 'status-neutral' };
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
        <button className="btn btn-primary btn-sm" onClick={() => setShowCreateModal(true)}><Plus size={13} />建立完工報工</button>
      </div>

      <div className="module-filter-bar glass-card no-lift">
        <select className="form-input" style={{ width: 140 }} value={filterStatus} onChange={e => setFilterStatus(e.target.value)}>
          <option value="all">所有狀態</option>
          <option value="draft">草稿</option>
          <option value="completed">已完成</option>
          <option value="cancelled">已取消</option>
        </select>
        <div className="filter-search">
          <Search size={14} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋完工單號..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>載入中...</div>
        ) : completions.length === 0 ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>目前無完工紀錄</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>完工單號</th>
                <th>狀態</th>
                <th>工單編號</th>
                <th>目的倉庫</th>
                <th>建立日期</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {completions.filter((c: any) => !search || c.completionNumber?.includes(search)).map((c: any) => (
                <tr key={c.id}>
                  <td style={{ color: 'var(--cobalt-300)', fontFamily: 'var(--font-mono)' }}>{c.completionNumber}</td>
                  <td><span className={`status-badge ${getStatusBadge(c.status).className}`}>{getStatusBadge(c.status).label}</span></td>
                  <td style={{ fontFamily: 'var(--font-mono)' }}>{c.workOrderId}</td>
                  <td>{warehouses.find((w: any) => w.id === c.warehouseId)?.warehouseName || c.warehouseId}</td>
                  <td>{c.createdAt?.slice(0, 10)}</td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      {c.status === 'draft' && (
                        <>
                          <button className="icon-action-btn" title="確認入庫" onClick={() => { if (confirm('確定要執行完工入庫？')) completeMutation.mutate(c.id) }}><ClipboardCheck size={13} /></button>
                          <button className="icon-action-btn" title="作廢" onClick={() => { if (confirm('確定要作廢？')) cancelMutation.mutate(c.id) }}><Trash2 size={13} /></button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreateModal && (
        <CompletionCreateModal
          warehouses={warehouses}
          onClose={() => setShowCreateModal(false)}
          onSubmit={(data) => createMutation.mutate(data)}
          isSubmitting={createMutation.isPending}
        />
      )}
    </div>
  );
}

function ShippingPanel() {
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['delivery-orders', filterStatus],
    queryFn: () => deliveryOrdersApi.list({ status: filterStatus !== 'all' ? filterStatus : undefined }).then(res => res.data)
  });

  const deliveryOrders = Array.isArray(data?.data) ? data.data : [];

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string, status: string }) => deliveryOrdersApi.updateStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['delivery-orders'] });
      queryClient.invalidateQueries({ queryKey: ['skus'] });
      alert('出貨狀態已更新');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '更新失敗');
    }
  });

  const getStatusBadge = (status: string) => {
    const map: Record<string, { label: string; className: string; color: string }> = {
      pending:    { label: '待處理',  className: 'status-warning', color: 'var(--amber-400)' },
      picking:    { label: '揀貨中',  className: 'info',           color: 'var(--cobalt-300)' },
      packing:    { label: '包裝中',  className: 'info',           color: 'var(--indigo-300)' },
      dispatched: { label: '已發貨',  className: 'status-active',  color: 'var(--emerald-400)' },
      cancelled:  { label: '已作廢',  className: 'status-neutral', color: 'var(--slate-500)' }
    };
    return map[status] || { label: status, className: 'status-neutral', color: 'var(--slate-500)' };
  };

  const getNextAction = (status: string) => {
    switch (status) {
      case 'pending':    return { label: '開始揀貨', nextStatus: 'picking', icon: <ClipboardCheck size={14} /> };
      case 'picking':    return { label: '完成揀貨', nextStatus: 'packing', icon: <Package size={14} /> };
      case 'packing':    return { label: '確認出貨', nextStatus: 'dispatched', icon: <Truck size={14} /> };
      default:           return null;
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div className="module-filter-bar glass-card no-lift">
        <select className="form-input" style={{ width: 140 }} value={filterStatus} onChange={e => setFilterStatus(e.target.value)}>
          <option value="all">所有狀態</option>
          <option value="pending">待處理</option>
          <option value="picking">揀貨中</option>
          <option value="packing">包裝中</option>
          <option value="dispatched">已發貨</option>
          <option value="cancelled">已取消</option>
        </select>
        <div className="filter-search">
          <Search size={14} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋出貨單號..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>載入中...</div>
        ) : deliveryOrders.length === 0 ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>目前無出貨紀錄</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>出貨草稿單號</th>
                <th>狀態</th>
                <th>銷售單號</th>
                <th>預計日期</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {deliveryOrders.filter((do_order: any) => !search || do_order.deliveryNumber?.includes(search)).map((do_order: any) => {
                const action = getNextAction(do_order.status);
                const badge = getStatusBadge(do_order.status);
                return (
                  <tr key={do_order.id}>
                    <td style={{ color: 'var(--cobalt-300)', fontFamily: 'var(--font-mono)' }}>{do_order.deliveryNumber}</td>
                    <td>
                      <span className={`status-badge ${badge.className}`} style={{ borderColor: `${badge.color}40`, color: badge.color }}>
                        {badge.label}
                      </span>
                    </td>
                    <td className="mono" style={{ fontSize: 12 }}>{do_order.salesOrderNumber || '-'}</td>
                    <td>{do_order.deliveryDate?.slice(0, 10)}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 8 }}>
                        {action && (
                          <button 
                            className="btn btn-primary btn-xs" 
                            style={{ padding: '4px 8px', fontSize: 11, gap: 4 }}
                            onClick={() => {
                              if (action.nextStatus === 'dispatched') {
                                if (confirm('確認出貨後將自動扣除庫存並更新銷售單狀態，是否繼續？')) {
                                  updateStatusMutation.mutate({ id: do_order.id, status: action.nextStatus });
                                }
                              } else {
                                updateStatusMutation.mutate({ id: do_order.id, status: action.nextStatus });
                              }
                            }}
                            disabled={updateStatusMutation.isPending}
                          >
                            {action.icon}{action.label}
                          </button>
                        )}
                        <button className="icon-action-btn" title="查看詳情" onClick={() => { setSelectedId(do_order.id); setShowDetailModal(true); }}>
                          <Eye size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {showDetailModal && selectedId && (
        <DeliveryOrderDetailModal
          id={selectedId}
          onClose={() => { setShowDetailModal(false); setSelectedId(null); }}
        />
      )}
    </div>
  );
}

function DeliveryOrderDetailModal({ id, onClose }: { id: string; onClose: () => void }) {
  const { data: res, isLoading } = useQuery({
    queryKey: ['delivery-order', id],
    queryFn: () => deliveryOrdersApi.get(id).then(r => r.data)
  });

  const order: any = res?.data;

  const getStatusLabel = (status: string) => {
    const map: Record<string, string> = {
      pending: '待處理',
      picking: '揀貨中',
      packing: '包裝中',
      dispatched: '已發貨',
      signed: '已簽收',
      cancelled: '已取消'
    };
    return map[status] || status;
  };

  if (isLoading) {
    return (
      <div style={{ position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.8)', backdropFilter: 'blur(4px)' }} />
        <div className="glass-card" style={{ position: 'relative', padding: 40, color: 'var(--slate-400)' }}>載入中...</div>
      </div>
    );
  }

  return (
    <div style={{ position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', paddingTop: 60 }}>
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.8)', backdropFilter: 'blur(4px)' }} onClick={onClose} />
      <div className="glass-card" style={{ position: 'relative', width: 800, zIndex: 1, padding: 24, maxHeight: '85vh', overflowY: 'auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <h3 style={{ fontSize: 16, fontWeight: 700, color: 'var(--slate-100)' }}>出貨單詳情: {order?.deliveryNumber}</h3>
          <button className="btn btn-ghost btn-sm" onClick={onClose}>關閉</button>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 24 }}>
          <div className="info-item">
            <span style={{ fontSize: 12, color: 'var(--slate-500)' }}>交易狀態</span>
            <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--amber-400)' }}>{getStatusLabel(order?.status)}</span>
          </div>
          <div className="info-item">
            <span style={{ fontSize: 12, color: 'var(--slate-500)' }}>預計出貨日期</span>
            <span style={{ fontSize: 13, color: 'var(--slate-200)' }}>{order?.deliveryDate?.slice(0, 10)}</span>
          </div>
          <div className="info-item">
            <span style={{ fontSize: 12, color: 'var(--slate-500)' }}>運輸方式</span>
            <span style={{ fontSize: 13, color: 'var(--slate-200)' }}>{order?.shippingMethod || '-'}</span>
          </div>
          <div className="info-item">
            <span style={{ fontSize: 12, color: 'var(--slate-500)' }}>收貨地址</span>
            <span style={{ fontSize: 13, color: 'var(--slate-200)' }}>{order?.deliveryAddress || '-'}</span>
          </div>
        </div>

        <h4 style={{ fontSize: 14, fontWeight: 700, marginBottom: 12, color: 'var(--slate-300)' }}>明細列表</h4>
        <table className="data-table">
          <thead>
            <tr>
              <th>品名 / 料號</th>
              <th>數量</th>
              <th>儲位</th>
              <th>批號</th>
              <th>序號</th>
            </tr>
          </thead>
          <tbody>
            {order?.items?.map((item: any) => (
              <tr key={item.id}>
                <td>
                  <div style={{ fontSize: 13, fontWeight: 600 }}>{item.skuName}</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>{item.skuNumber}</div>
                </td>
                <td style={{ fontWeight: 600 }}>{item.deliveredQty}</td>
                <td className="mono">{item.binLocationId || '-'}</td>
                <td>{item.lotNumber || '-'}</td>
                <td>{item.serialNumber || '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function CompletionCreateModal({ warehouses, onClose, onSubmit, isSubmitting }: {
  warehouses: any[];
  onClose: () => void;
  onSubmit: (data: Record<string, unknown>) => void;
  isSubmitting: boolean;
}) {
  const [form, setForm] = useState({ workOrderId: '', warehouseId: '', items: [] as any[] });

  const { data: workOrdersData } = useQuery({
    queryKey: ['completable-workorders'],
    queryFn: () => completionOrdersApi.getCompletableWorkOrders().then(res => res.data)
  });
  const workOrders = workOrdersData?.data || [];

  const handleSelectWo = (wo: any) => {
    setForm({
      ...form,
      workOrderId: wo.workOrderNumber,
      items: [{
        skuId: wo.skuId,
        skuNumber: wo.sku?.skuNumber,
        skuName: wo.sku?.skuName,
        completedQty: wo.plannedQty,
        lotNumber: ''
      }]
    });
  };

  return (
    <div style={{ position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'flex-start', justifyContent: 'center', paddingTop: 60 }}>
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.8)', backdropFilter: 'blur(4px)' }} onClick={onClose} />
      <div className="glass-card" style={{ position: 'relative', width: 600, zIndex: 1, padding: 24, maxHeight: '85vh', overflowY: 'auto' }}>
        <h3 style={{ fontSize: 16, fontWeight: 700, color: 'var(--slate-100)', marginBottom: 20 }}>建立完工單</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div>
              <label style={{ fontSize: 12, color: 'var(--slate-500)', marginBottom: 6, display: 'block' }}>關聯工單</label>
              <select className="form-input" style={{ width: '100%' }} value={form.workOrderId} onChange={e => {
                const wo = workOrders.find((w: any) => w.workOrderNumber === e.target.value);
                if (wo) handleSelectWo(wo);
              }}>
                <option value="">選取待入庫工單</option>
                {workOrders.map((wo: any) => (
                  <option key={wo.id} value={wo.workOrderNumber}>{wo.workOrderNumber} - {wo.sku?.skuName}</option>
                ))}
              </select>
            </div>
            <div>
              <label style={{ fontSize: 12, color: 'var(--slate-500)', marginBottom: 6, display: 'block' }}>入庫目的地</label>
              <select className="form-input" style={{ width: '100%' }} value={form.warehouseId} onChange={e => setForm({...form, warehouseId: e.target.value})}>
                <option value="">選擇倉庫</option>
                {warehouses.map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}</option>)}
              </select>
            </div>
          </div>
          <div>
            <label style={{ fontSize: 12, color: 'var(--slate-500)', marginBottom: 8, display: 'block' }}>入庫明細</label>
            {form.items.length === 0 ? (
              <div style={{ padding: 20, textAlign: 'center', color: 'var(--slate-600)', background: 'rgba(30,41,59,0.5)', borderRadius: 8 }}>請選取工單...</div>
            ) : (
              <table className="data-table" style={{ fontSize: 12 }}>
                <thead><tr><th>品名/料號</th><th>完工數量</th><th>批號</th></tr></thead>
                <tbody>
                  {form.items.map((item, idx) => (
                    <tr key={idx}>
                      <td>
                        <div style={{ fontWeight: 600 }}>{item.skuName}</div>
                        <div style={{ fontSize: 10, color: 'var(--slate-500)' }}>{item.skuNumber}</div>
                      </td>
                      <td>
                        <input type="number" className="form-input" style={{ width: 80 }} value={item.completedQty} onChange={e => {
                          const val = parseFloat(e.target.value);
                          setForm({ ...form, items: form.items.map((it, i) => i === idx ? {...it, completedQty: val} : it) });
                        }} />
                      </td>
                      <td>
                        <input type="text" className="form-input" style={{ width: 120 }} value={item.lotNumber} onChange={e => {
                          setForm({ ...form, items: form.items.map((it, i) => i === idx ? {...it, lotNumber: e.target.value} : it) });
                        }} placeholder="輸入批號" />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 24 }}>
          <button className="btn btn-ghost" onClick={onClose}>取消</button>
          <button className="btn btn-primary" onClick={() => onSubmit(form)} disabled={isSubmitting || !form.warehouseId || form.items.length === 0}>{isSubmitting ? '建立中...' : '確認單據'}</button>
        </div>
      </div>
    </div>
  );
}


function BinLocationsPanel() {
  const [showAddBin, setShowAddBin] = useState(false);
  const [showAddWh, setShowAddWh] = useState(false);
  const [editingWarehouse, setEditingWarehouse] = useState<any>(null);
  const [showInactiveWh, setShowInactiveWh] = useState(false);
  const [selectedWh, setSelectedWh] = useState<string>('');
  const [search, setSearch] = useState('');
  const queryClient = useQueryClient();

  const { data: warehouseData } = useWarehouses(showInactiveWh);
  const { data: binData, refetch } = useBinLocations(selectedWh || undefined, undefined);

  const warehouses = warehouseData?.data || [];
  const bins = Array.isArray(binData?.data) ? binData.data : [];

  const filteredBins = bins.filter(b => 
    b.locationCode.toLowerCase().includes(search.toLowerCase()) ||
    warehouses.find(w => w.id === b.warehouseId)?.warehouseName?.toLowerCase().includes(search.toLowerCase())
  );

  const createBin = useMutation({
    mutationFn: (data: any) => binLocationsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bin-locations'] });
      setShowAddBin(false);
      refetch();
    }
  });

  const deleteBin = useMutation({
    mutationFn: (id: string) => binLocationsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bin-locations'] });
      refetch();
    }
  });

  const activateBin = useMutation({
    mutationFn: (id: string) => binLocationsApi.activate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bin-locations'] });
      refetch();
    }
  });

  const createWh = useMutation({
    mutationFn: (data: any) => warehousesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
      setShowAddWh(false);
      alert('倉庫已建立');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '建立失敗');
    }
  });

  const updateWh = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => warehousesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warehouses'] });
      setEditingWarehouse(null);
      alert('倉庫已更新');
    },
    onError: (err: any) => {
      alert(err?.response?.data?.message || '更新失敗');
    }
  });

  const [newBin, setNewBin] = useState({ warehouseId: '', locationCode: '', zone: '', aisle: '', rack: '', level: '', position: '' });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        <div className="filter-search" style={{ minWidth: 200 }}>
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋儲位..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <input type="checkbox" id="show-inactive-wh" checked={showInactiveWh} onChange={(e) => setShowInactiveWh(e.target.checked)} />
          <label htmlFor="show-inactive-wh" style={{ fontSize: 12, color: 'var(--slate-400)' }}>顯示停用倉庫</label>
        </div>
        <select className="form-input" style={{ width: 180 }} value={selectedWh} onChange={e => setSelectedWh(e.target.value)}>
          <option value="">所有倉庫</option>
          {warehouses.map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}{!w.isActive ? ' (停用)' : ''}</option>)}
        </select>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAddBin(true)}><Plus size={13} />新增儲位</button>
        <button className="btn btn-outline btn-sm" onClick={() => setShowAddWh(true)}><Plus size={13} />新增倉庫</button>
      </div>

      <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
        <table className="data-table">
          <thead>
            <tr><th>倉庫</th><th>儲位代碼</th><th>區域</th><th>走道</th><th>貨架</th><th>層</th><th>位置</th><th>狀態</th><th>操作</th></tr>
          </thead>
          <tbody>
            {filteredBins.map((bin: any) => (
              <tr key={bin.id}>
                <td style={{ fontWeight: 500 }}>{warehouses.find(w => w.id === bin.warehouseId)?.warehouseName || bin.warehouseId}</td>
                <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{bin.locationCode}</td>
                <td>{bin.zone || '-'}</td>
                <td>{bin.aisle || '-'}</td>
                <td>{bin.rack || '-'}</td>
                <td>{bin.level || '-'}</td>
                <td>{bin.position || '-'}</td>
                <td><span className={`status-badge ${bin.isActive ? 'status-active' : 'status-neutral'}`}>{bin.isActive ? '啟用' : '停用'}</span></td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="icon-action-btn" onClick={() => (bin.isActive ? deleteBin.mutate(bin.id) : activateBin.mutate(bin.id))}>
                      {bin.isActive ? <Trash2 size={13} /> : <RefreshCw size={13} />}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div style={{ marginTop: 20 }}>
        <h4 style={{ fontSize: 14, marginBottom: 12, color: 'var(--slate-300)' }}>倉庫清單</h4>
        <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
          <table className="data-table">
            <thead>
              <tr><th>代碼</th><th>名稱</th><th>類型</th><th>地址</th><th>狀態</th><th>操作</th></tr>
            </thead>
            <tbody>
              {warehouses.map((w: any) => (
                <tr key={w.id}>
                  <td className="mono">{w.warehouseCode}</td>
                  <td>{w.warehouseName}</td>
                  <td>{w.type}</td>
                  <td>{w.address || '-'}</td>
                  <td><span className={`status-badge ${w.isActive ? 'status-active' : 'status-neutral'}`}>{w.isActive ? '啟用' : '停用'}</span></td>
                  <td>
                    <button className="icon-action-btn" onClick={() => setEditingWarehouse(w)}><Edit2 size={13} /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showAddBin && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ position: 'fixed', inset: 0, background: 'rgba(4,8,15,0.8)' }} onClick={() => setShowAddBin(false)} />
          <div className="glass-card" style={{ position: 'relative', width: 400, padding: 24 }}>
            <h3 style={{ marginBottom: 16, fontSize: 16 }}>新增儲位</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <select className="form-input" style={{ width: '100%' }} value={newBin.warehouseId} onChange={e => setNewBin({ ...newBin, warehouseId: e.target.value })}>
                <option value="">選擇倉庫</option>
                {warehouses.filter(w => w.isActive).map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}</option>)}
              </select>
              <input className="form-input" placeholder="儲位代碼 (如 A-01-01)" value={newBin.locationCode} onChange={e => setNewBin({ ...newBin, locationCode: e.target.value })} />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 24 }}>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowAddBin(false)}>取消</button>
              <button className="btn btn-primary btn-sm" onClick={() => createBin.mutate(newBin)} disabled={createBin.isPending}>建立</button>
            </div>
          </div>
        </div>
      )}

      {showAddWh && (
        <FormModal
          open={showAddWh}
          title="新增倉庫"
          onClose={() => setShowAddWh(false)}
          fields={[
            { name: 'warehouseCode', label: '倉庫代碼 *', type: 'text', required: true },
            { name: 'warehouseName', label: '倉庫名稱 *', type: 'text', required: true },
            { name: 'type', label: '類型', type: 'select', options: [{ value: 'normal', label: '一般倉' }, { value: 'wip', label: '在製品倉' }, { value: 'scrap', label: '廢料倉' }] },
            { name: 'address', label: '地址', type: 'text' },
            { name: 'isActive', label: '啟用狀態', type: 'checkbox', defaultValue: true }
          ]}
          onSubmit={async (data) => createWh.mutate(data)}
        />
      )}

      {editingWarehouse && (
        <FormModal
          open={!!editingWarehouse}
          initialValues={editingWarehouse}
          title={`編輯倉庫: ${editingWarehouse.warehouseName}`}
          onClose={() => setEditingWarehouse(null)}
          fields={[
             { name: 'warehouseName', label: '倉庫名稱 *', type: 'text', required: true },
             { name: 'type', label: '類型', type: 'select', options: [{ value: 'normal', label: '一般倉' }, { value: 'wip', label: '在製品倉' }, { value: 'scrap', label: '廢料倉' }] },
             { name: 'address', label: '地址', type: 'text' },
             { name: 'isActive', label: '啟用狀態', type: 'checkbox' }
          ]}
          onSubmit={async (data) => updateWh.mutate({ id: editingWarehouse.id, data })}
        />
      )}
    </div>
  );
}

export const Inventory: React.FC = () => {
  const queryClient = useQueryClient();
  const [searchSku, setSearchSku] = useState('');
  const [showAdd, setShowAdd] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editSku, setEditSku] = useState<Record<string, unknown> | null>(null);
  const [activeTab, setActiveTab] = useState<'skus' | 'receipts' | 'expiry' | 'transfers' | 'completions' | 'bins' | 'analysis' | 'transactions' | 'count' | 'shipping'>('skus');
  const [hiddenFields, setHiddenFields] = useState<string[]>([]);
  const [showInactive, setShowInactive] = useState(false);
  const [filterItemType, setFilterItemType] = useState('');
  const [filterSourceType, setFilterSourceType] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [warehouses, setWarehouses] = useState<{ value: string; label: string }[]>([]);
  const [suppliers, setSuppliers] = useState<{ value: string; label: string }[]>([]);
  const [uoms, setUoms] = useState<{ value: string; label: string }[]>([]);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string | null>(null);
  const [modalWarehouseId, setModalWarehouseId] = useState<string | null>(null);
  const [modalBinOptions, setModalBinOptions] = useState<{ value: string; label: string }[]>([]);

  const mapSkuToFormData = (sku: Record<string, unknown>) => {
    const formData: Record<string, unknown> = { ...sku };
    const s = sku as any;
    formData.isPurchasable = s.isPurchasable ?? s.IsPurchasable;
    formData.isSellable = s.isSellable ?? s.IsSellable;
    formData.isMto = s.isMto ?? s.IsMto;
    formData.isMts = s.isMts ?? s.IsMts;
    formData.isCustom = s.isCustom ?? s.IsCustom;
    formData.isManufacturable = s.isManufacturable ?? s.IsManufacturable;
    formData.isOutsourced = s.isOutsourced ?? s.IsOutsourced;
    return formData;
  };

  const { data: skusData, isLoading } = useSkus({ 
    pageSize: '10', 
    page: currentPage.toString(), 
    status: showInactive ? 'inactive' : 'active', 
    itemType: filterItemType || undefined, 
    sourceType: filterSourceType || undefined, 
    search: searchSku || undefined 
  });

  const { data: warehouseData } = useWarehouses();
  const { data: uomData } = useUoms();
  const { data: conversionData } = useUomConversions();

  const receiptsQuery = useQuery({ 
    queryKey: ['goods-receipts'], 
    queryFn: () => goodsReceiptsApi.list().then(res => res.data),
    retry: false
  });

  React.useEffect(() => {
    if (warehouseData?.data) {
      const whList = warehouseData.data.map((w: any) => ({
        value: w.id,
        label: w.warehouseName
      }));
      setWarehouses(whList);
      if (!selectedWarehouseId && whList.length > 0) {
        setSelectedWarehouseId(whList[0].value);
      }
    }
  }, [warehouseData, selectedWarehouseId]);

  React.useEffect(() => {
    if (modalWarehouseId) {
      binLocationsApi.getByWarehouse(modalWarehouseId).then(r => {
        if (r.data?.data) {
          setModalBinOptions(r.data.data.map((bl: any) => ({
            value: bl.id,
            label: `${bl.locationCode} - ${bl.warehouseName}`
          })));
        }
      });
    } else {
      setModalBinOptions([]);
    }
  }, [modalWarehouseId]);

  React.useEffect(() => {
    if (uomData?.data) {
      setUoms(uomData.data.map((u: any) => ({
        value: u.id,
        label: `${u.uomCode} - ${u.uomName}`
      })));
    }
  }, [uomData]);

  React.useEffect(() => {
    suppliersApi.list().then(res => {
      const data = res.data?.data || [];
      if (Array.isArray(data)) {
        setSuppliers(data.map((s: any) => ({
          value: s.id,
          label: s.supplierName || s.supplierCode || '未命名供應商'
        })));
      }
    });
  }, []);

  const skus = Array.isArray(skusData?.data) ? skusData.data : [];
  const receipts = Array.isArray(receiptsQuery.data?.data) ? receiptsQuery.data.data : [];

  const tabs = [
    { id: 'skus', label: '品碼清單', desc: 'SKU' },
    { id: 'transactions', label: '異動明細', desc: 'Trans' },
    { id: 'shipping', label: '出貨作業', desc: 'Shipping' },
    { id: 'count', label: '盤點作業', desc: 'Count' },
    { id: 'receipts', label: '收貨紀錄', desc: 'Receipts' },
    { id: 'transfers', label: '調撥管理', desc: 'Transfer' },
    { id: 'completions', label: '完工入庫', desc: 'Completion' },
    { id: 'bins', label: '儲位管理', desc: 'Bin' },
    { id: 'analysis', label: '庫存分析', desc: 'ABC/Aging' },
  ] as const;

  return (
    <div className="module-page">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">庫存管理中心</h2>
          <p className="module-page-desc">SKU 基礎資料 · 異動追蹤 · 調撥管理 · 盤點作業</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出報告</button>
          {activeTab === 'skus' && (
            <button className="btn btn-primary btn-sm" onClick={() => { setModalWarehouseId(null); setShowAdd(true); }}><Plus size={13} />新增品碼</button>
          )}
        </div>
      </div>

      <div className="inv-tab-bar">
        {tabs.map(tab => (
          <button
            key={tab.id}
            className={`inv-tab-btn ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => setActiveTab(tab.id as any)}
          >
            <span className="inv-tab-label">{tab.label}</span>
            <span className="inv-tab-desc">{tab.desc}</span>
          </button>
        ))}
      </div>

      <div className="module-filter-bar glass-card no-lift" style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, width: '100%' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--lux-gold)' }}>SITE</span>
            <select className="form-input" style={{ width: 120 }}>
              <option value="HQ">總部工廠</option>
              <option value="WH2">二號倉庫</option>
            </select>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--lux-gold)' }}>WH</span>
            <select className="form-input" style={{ width: 140 }} value={selectedWarehouseId || ''} onChange={e => setSelectedWarehouseId(e.target.value)}>
              <option value="">所有倉庫</option>
              {warehouses.map(wh => <option key={wh.value} value={wh.value}>{wh.label}</option>)}
            </select>
          </div>
          <div style={{ marginLeft: 'auto', display: 'flex', gap: 8 }}>
             <button className="btn btn-ghost btn-sm" onClick={() => queryClient.invalidateQueries()}><RefreshCw size={13} /> 重新整理庫存</button>
          </div>
        </div>
      </div>

      <div style={{ marginTop: 0 }}>
        {activeTab === 'skus' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="module-filter-bar glass-card no-lift">
              <div className="filter-search">
                <Search size={14} style={{ color: 'var(--slate-500)' }} />
                <input 
                  className="search-input" 
                  placeholder="搜尋料號或品名..." 
                  value={searchSku} 
                  onChange={e => { setSearchSku(e.target.value); setCurrentPage(1); }} 
                />
              </div>
              <select className="form-input" style={{ width: 150 }} value={filterItemType} onChange={e => { setFilterItemType(e.target.value); setCurrentPage(1); }}>
                <option value="">所有品項類別</option>
                <option value="finished">成品</option>
                <option value="semi_finished">半成品</option>
                <option value="material">原料</option>
              </select>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginLeft: 'auto' }}>
                <input type="checkbox" id="show-inactive-sku" checked={showInactive} onChange={e => { setShowInactive(e.target.checked); setCurrentPage(1); }} />
                <label htmlFor="show-inactive-sku" style={{ fontSize: 13, color: 'var(--slate-400)' }}>顯示停用品項</label>
              </div>
            </div>
            <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>料號</th>
                    <th>品名</th>
                    <th>規格</th>
                    <th>庫存數量</th>
                    <th>標準成本</th>
                    <th>狀態</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {skus.map((sku: any) => (
                    <tr key={sku.id}>
                      <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{sku.skuNumber}</td>
                      <td style={{ fontWeight: 500 }}>{sku.skuName}</td>
                      <td>{sku.specification || '-'}</td>
                      <td>
                        <SkuInventoryDetail sku={sku.skuNumber} />
                      </td>
                      <td className="mono">{sku.standardCost?.toLocaleString()}</td>
                      <td>
                        <span className={`status-badge ${sku.status === 'active' ? 'status-active' : 'status-neutral'}`}>
                          {sku.status === 'active' ? '啟用' : '停用'}
                        </span>
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: 6 }}>
                          <button className="icon-action-btn" title="編輯" onClick={() => { setEditSku(mapSkuToFormData(sku)); setModalWarehouseId(sku.defaultWarehouseId || null); setShowEdit(true); }}>
                            <Edit2 size={13} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {skus.length === 0 && (
                    <tr><td colSpan={7} style={{ textAlign: 'center', padding: 30, color: 'var(--slate-500)' }}>無符合資料</td></tr>
                  )}
                </tbody>
              </table>
              <div style={{ padding: '12px 16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderTop: '1px solid var(--glass-border)' }}>
                <span style={{ fontSize: 13, color: 'var(--slate-500)' }}>共 {skusData?.totalCount || 0} 筆資料</span>
                <div style={{ display: 'flex', gap: 8 }}>
                  <button className="btn btn-ghost btn-xs" disabled={currentPage <= 1} onClick={() => setCurrentPage(p => p - 1)}>上一頁</button>
                  <button className="btn btn-ghost btn-xs" disabled={currentPage >= Math.ceil((skusData?.totalCount || 0) / 10)} onClick={() => setCurrentPage(p => p + 1)}>下一頁</button>
                </div>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'receipts' && (
          <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>收貨單號</th>
                  <th>日期</th>
                  <th>採購單號</th>
                  <th>狀態</th>
                  <th>檢驗結果</th>
                </tr>
              </thead>
              <tbody>
                {receipts.map((r: any) => (
                  <tr key={r.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{r.grNumber}</td>
                    <td>{new Date(r.createdAt).toLocaleDateString()}</td>
                    <td className="mono">{r.purchaseOrderId || '-'}</td>
                    <td><span className="status-badge status-active">{r.status}</span></td>
                    <td><span className={`status-badge ${r.iqcStatus === 'passed' ? 'status-active' : 'status-warning'}`}>{r.iqcStatus || 'pending'}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {activeTab === 'transactions' && <TransactionsPanel />}
        {activeTab === 'shipping' && <ShippingPanel />}
        {activeTab === 'count' && <CountPanel />}
        {activeTab === 'transfers' && <TransferPanel />}
        {activeTab === 'completions' && <CompletionPanel />}
        {activeTab === 'bins' && <BinLocationsPanel />}
        {activeTab === 'analysis' && <AnalysisPanel />}
      </div>

      {showAdd && (
        <FormModal 
          open={showAdd} 
          title="新增品碼 (SKU)"
          tabs={[
            { label: '基本資料', fields: [...basicInfoFields, ...sourceTypeFields] },
            { label: '單位與成本', fields: costFields(uoms) },
            { label: '庫存設定', fields: inventoryFields(warehouses, modalBinOptions) },
            { label: '銷售資訊', fields: salesFields }
          ]}
          onClose={() => setShowAdd(false)}
          onChangeData={(values) => {
            if (values.defaultwarehouseId !== modalWarehouseId) {
              setModalWarehouseId(values.defaultwarehouseId as string);
            }
          }}
          onSubmit={async (data) => {
            try {
              await skusApi.create(data);
              queryClient.invalidateQueries({ queryKey: ['skus'] });
              setShowAdd(false);
            } catch (err: any) {
              alert(err.response?.data?.message || '新增失敗');
            }
          }}
        />
      )}

      {showEdit && editSku && (
        <FormModal 
          open={showEdit} 
          title={`編輯品碼: ${editSku.skuNumber}`}
          initialValues={editSku}
          tabs={[
            { label: '基本資料', fields: [...basicInfoFields, ...sourceTypeFields] },
            { label: '單位與成本', fields: costFields(uoms) },
            { label: '庫存設定', fields: inventoryFields(warehouses, modalBinOptions) },
            { label: '銷售資訊', fields: salesFields },
            { label: '產能規劃', fields: [{ name: 'capacityBill', type: 'custom' }] }
          ]}
          customRenderers={{
            capacityBill: <CapacityBillManager skuId={editSku.id as string} />
          }}
          onClose={() => { setShowEdit(false); setEditSku(null); }}
          onChangeData={(values) => {
            if (values.defaultwarehouseId !== modalWarehouseId) {
              setModalWarehouseId(values.defaultwarehouseId as string);
            }
          }}
          onSubmit={async (data) => {
            try {
              await skusApi.update(editSku.id as string, data);
              queryClient.invalidateQueries({ queryKey: ['skus'] });
              setShowEdit(false);
              setEditSku(null);
            } catch (err: any) {
              alert(err.response?.data?.message || '更新失敗');
            }
          }}
        />
      )}

      <style>{`
        .module-page { display: flex; flex-direction: column; gap: 16px; min-height: 100vh; }
        .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 8px; }
        .module-page-title { font-size: 24px; font-weight: 800; color: var(--slate-100); letter-spacing: -0.02em; }
        .module-page-desc { font-size: 13px; color: var(--slate-500); margin-top: 4px; }
        .inv-tab-bar { display: flex; gap: 4px; border-bottom: 1px solid var(--slate-800); margin-top: 12px; padding-bottom: 2px; overflow-x: auto; }
        .inv-tab-btn { display: flex; flex-direction: column; align-items: flex-start; padding: 10px 20px; background: transparent; border: none; border-bottom: 2px solid transparent; color: var(--slate-500); cursor: pointer; transition: all 0.2s; white-space: nowrap; }
        .inv-tab-btn:hover { color: var(--slate-200); background: rgba(255,255,255,0.03); }
        .inv-tab-btn.active { color: var(--cobalt-400); border-bottom-color: var(--cobalt-400); background: rgba(59,130,246,0.05); }
        .inv-tab-label { font-size: 14px; font-weight: 700; }
        .inv-tab-desc { font-size: 10px; opacity: 0.6; font-family: var(--font-mono); text-transform: uppercase; margin-top: 2px; }
      `}</style>
    </div>
  );
};


