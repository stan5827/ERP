import React, { useState, useMemo, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Search, Plus, Eye, Edit, X, Save, Trash2, 
  ChevronRight, Loader2, Filter, ShoppingBag, 
  FileText, CheckCircle2, TrendingUp, DollarSign, 
  Calendar, BarChart2, Check, XCircle, Truck, 
  Factory, ArrowRightLeft, Sparkles, UserPlus, Users,
  ArrowUpRight, Target, Activity, Download, Mail, Phone, MapPin, CreditCard, Layers
} from 'lucide-react';
import { useSalesOrders, useQuotes, useCustomers, useOrderToCash, useSkus, useUoms } from '../hooks/useApi';
import { FormModal, type FormTab, type FormField } from '../components/FormModal';
import { salesOrdersApi, customersApi, skusApi, invoicesApi, quotesApi, deliveryOrdersApi, workOrdersApi, toPascalCase, inventoryApi } from '../api/api';
import { useQueryClient } from '@tanstack/react-query';
import './styles/Sales-Luxury.css';

// 庫存狀態組件
const StockStatus: React.FC<{ sku: string; qty: number }> = ({ sku, qty }) => {
  const [atp, setAtp] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!sku) return;
    const check = async () => {
      setLoading(true);
      try {
        const res = await inventoryApi.checkATP(sku, qty);
        setAtp(res.data);
      } catch (e) {}
      setLoading(false);
    };
    check();
  }, [sku, qty]);

  if (loading) return <Loader2 size={12} className="animate-spin" />;
  if (!atp) return <span style={{ color: '#64748b' }}>-</span>;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <span style={{ 
        color: atp.is_sufficient ? '#10b981' : '#ef4444', 
        fontSize: '0.7rem', 
        fontWeight: 700 
      }}>
        {atp.is_sufficient ? '庫存充足' : '缺貨'}
      </span>
      <span style={{ fontSize: '0.6rem', color: '#94a3b8' }}>
        可用: {atp.available}
      </span>
    </div>
  );
};


const statusConfig: Record<string, { label: string; color: string; bg: string }> = {
  draft:         { label: '初步草稿', color: '#94a3b8', bg: 'rgba(148, 163, 184, 0.1)' },
  pending:       { label: '待審核中', color: '#fbbf24', bg: 'rgba(251, 191, 36, 0.1)' },
  confirmed:     { label: '已轉正式', color: '#60a5fa', bg: 'rgba(96, 165, 250, 0.1)' },
  shipped:       { label: '物流出貨', color: '#10b981', bg: 'rgba(16, 185, 129, 0.1)' },
  completed:     { label: '交易完成', color: '#d4af37', bg: 'rgba(212, 175, 55, 0.1)' },
  approved:      { label: '核准通過', color: '#10b981', bg: 'rgba(16, 185, 129, 0.1)' },
  rejected:      { label: '駁回終止', color: '#ef4444', bg: 'rgba(239, 68, 68, 0.1)' },
  converted:     { label: '轉訂單中', color: '#8b5cf6', bg: 'rgba(139, 92, 246, 0.1)' },
};

const pipelineConfig: Record<string, { label: string; color: string; icon: any }> = {
  pending:           { label: '待處理', color: '#94a3b8', icon: Calendar },
  in_progress:       { label: '生產/執行中', color: '#60a5fa', icon: Activity },
  shipped:           { label: '已發貨', color: '#10b981', icon: Truck },
  in_delivery:       { label: '物流配送中', color: '#34d399', icon: Truck },
  invoiced:          { label: '已開立發票', color: '#22d3ee', icon: FileText },
  partial_collection: { label: '部分收款', color: '#fbbf24', icon: DollarSign },
  completed:         { label: '結案完成', color: '#d4af37', icon: CheckCircle2 },
  cancelled:         { label: '已取消', color: '#ef4444', icon: XCircle },
};

const StatusBadge: React.FC<{ status: string }> = ({ status }) => {
  const conf = statusConfig[status] || { label: status, color: '#94a3b8', bg: 'rgba(255,255,255,0.05)' };
  return (
    <span className="lux-badge" style={{ color: conf.color, backgroundColor: conf.bg, border: `1px solid ${conf.color}20` }}>
      {conf.label}
    </span>
  );
};

interface ItemsTableProps {
  items: any[];
  setItems: React.Dispatch<React.SetStateAction<any[]>>;
  isQuote?: boolean;
  skus: any[];
  uoms: any;
}

const ItemsTable: React.FC<ItemsTableProps> = ({ items, setItems, isQuote = false, skus, uoms }) => {
  const total = items.reduce((acc, cur) => acc + (cur.quantity * cur.unitPrice || 0), 0);

  return (
    <div className="lux-items-editor">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--lux-gold)', display: 'flex', alignItems: 'center', gap: 8 }}>
          <Layers size={14} /> {isQuote ? '報價明細清單' : '訂購產品明細'}
        </div>
        <button type="button" className="btn btn-outline btn-sm" onClick={() => setItems([...items, { skuId: '', quantity: 1, unit: 'PCS', unitPrice: 0 }])}>
          <Plus size={12} style={{ marginRight: 4 }} /> 新增品項
        </button>
      </div>
      
      <div className="lux-items-grid">
        <div className="lux-items-header">
          <span style={{ flex: 3 }}>產品 / 品號 *</span>
          <span style={{ flex: 1, textAlign: 'center' }}>數量 *</span>
          <span style={{ flex: 1, textAlign: 'center' }}>庫存狀態</span>
          <span style={{ flex: 1, textAlign: 'center' }}>單位</span>
          <span style={{ flex: 1.5, textAlign: 'right' }}>單價 *</span>
          <span style={{ flex: 1.5, textAlign: 'right' }}>小計</span>
          <span style={{ width: 40 }}></span>
        </div>
        {items.length === 0 ? (
          <div style={{ padding: 30, textAlign: 'center', color: '#64748b', fontSize: 13, background: 'rgba(255,255,255,0.02)', borderRadius: 8 }}>
            尚無明細資料，請點擊上方按鈕新增品項
          </div>
        ) : items.map((item, idx) => (
          <div key={idx} className="lux-item-row">
            <div style={{ flex: 3 }}>
              <select 
                className="form-input" 
                style={{ width: '100%' }}
                value={item.skuId || item.productId}
                onChange={e => {
                  const sku = skus.find(s => s.id === e.target.value);
                  const news = [...items];
                  news[idx] = { 
                    ...news[idx], 
                    skuId: e.target.value, 
                    productId: e.target.value,
                    skuNumber: sku?.skuNumber,
                    productName: sku?.skuName,
                    unit: sku?.salesUomCode || sku?.unit || 'PCS',
                    unitPrice: sku?.suggestedPrice || sku?.purchasePrice || 0
                  };
                  setItems(news);
                }}
              >
                <option value="">選擇產品...</option>
                {skus.filter(s => s.isSellable).map(s => <option key={s.id} value={s.id}>{s.skuNumber} - {s.skuName}</option>)}
              </select>
            </div>
            <div style={{ flex: 1 }}>
              <input 
                type="number" className="form-input" style={{ textAlign: 'center' }} 
                value={item.quantity} min={1}
                onChange={e => {
                  const news = [...items];
                  news[idx].quantity = parseFloat(e.target.value) || 0;
                  setItems(news);
                }}
              />
            </div>
            <div style={{ flex: 1, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
              <StockStatus sku={item.skuNumber} qty={item.quantity} />
            </div>
            <div style={{ flex: 1 }}>
              <input 
                type="text"
                className="form-input" 
                style={{ textAlign: 'center', width: '100%', cursor: 'not-allowed', opacity: 0.8, color: 'var(--lux-gold)', fontWeight: 600 }} 
                value={(uoms?.data || []).find(u => u.uomCode === item.unit)?.uomName || item.unit || '---'}
                readOnly
              />
            </div>
            <div style={{ flex: 1.5 }}>
              <input 
                type="number" className="form-input" style={{ textAlign: 'right' }} 
                value={item.unitPrice}
                onChange={e => {
                  const news = [...items];
                  news[idx].unitPrice = parseFloat(e.target.value) || 0;
                  setItems(news);
                }}
              />
            </div>
            <div className="lux-item-subtotal" style={{ flex: 1.5, textAlign: 'right', fontFamily: 'monospace' }}>
              ${(item.quantity * item.unitPrice).toLocaleString()}
            </div>
            <div style={{ width: 40, textAlign: 'center' }}>
              <button type="button" className="icon-action-btn" onClick={() => setItems(items.filter((_, i) => i !== idx))}>
                <Trash2 size={14} color="#ef4444" />
              </button>
            </div>
          </div>
        ))}
      </div>
      
      <div className="lux-items-footer">
        <div style={{ color: 'var(--lux-gold)', fontWeight: 800, fontSize: 16 }}>
          總金額合計: <span style={{ fontFamily: 'monospace', marginLeft: 12 }}>${total.toLocaleString()}</span>
        </div>
      </div>
    </div>
  );
};

export const Sales: React.FC = () => {
  const queryClient = useQueryClient();
  const [view, setView] = useState<'orders' | 'quotes' | 'customers' | 'process'>('process');
  const [search, setSearch] = useState("");
  
  const [showOrderForm, setShowOrderForm] = useState(false);
  const [showQuoteForm, setShowQuoteForm] = useState(false);
  const [showCustomerForm, setShowCustomerForm] = useState(false);
  
  const [selectedOrder, setSelectedOrder] = useState<any>(null);
  const [selectedQuote, setSelectedQuote] = useState<any>(null);
  const [selectedCustomer, setSelectedCustomer] = useState<any>(null);
  
  // 明細項狀態
  const [orderItems, setOrderItems] = useState<any[]>([]);
  const [quoteItems, setQuoteItems] = useState<any[]>([]);
  const [expItems, setExpItems] = useState<Set<string>>(new Set());
  
  const { data: ordResp, isLoading: ordersLoading } = useSalesOrders();
  const { data: quoResp, isLoading: quotesLoading } = useQuotes();
  const { data: custResp, isLoading: customersLoading } = useCustomers();
  const { data: procResp, isLoading: processLoading } = useOrderToCash();
  const { data: skusResp } = useSkus({ pageSize: '1000' });
  const { data: uoms } = useUoms();

  const orders = ordResp?.data || [];
  const quotes = quoResp?.data || [];
  const customers = custResp?.data || [];
  const skus = skusResp?.data || [];
  const processRows = (procResp?.data as any)?.rows || [];
  const processSummary = (procResp?.data as any)?.summary || {};
  
  const isLoading = ordersLoading || quotesLoading || customersLoading || processLoading;

  const filteredOrders = useMemo(() => orders.filter((o: any) => 
    o.orderNumber?.toLowerCase().includes(search.toLowerCase()) || 
    (o.customerName || '').toLowerCase().includes(search.toLowerCase())
  ), [orders, search]);

  const filteredQuotes = useMemo(() => quotes.filter((q: any) => 
    q.quoteNumber?.toLowerCase().includes(search.toLowerCase()) || 
    (q.customerName || '').toLowerCase().includes(search.toLowerCase())
  ), [quotes, search]);

  const filteredCustomers = useMemo(() => customers.filter((c: any) => 
    c.customerCode?.toLowerCase().includes(search.toLowerCase()) || 
    c.customerName?.toLowerCase().includes(search.toLowerCase())
  ), [customers, search]);

  const stats = useMemo(() => ({
    revenue: orders.reduce((acc: number, cur: any) => acc + (cur.totalAmount || 0), 0),
    count: orders.length,
    growth: "+14.2%"
  }), [orders]);

  // 控制表單開啟時重置明細
  useEffect(() => {
    if (showOrderForm) {
      setOrderItems(selectedOrder?.items || []);
    }
  }, [showOrderForm, selectedOrder]);

  useEffect(() => {
    if (showQuoteForm) {
      const items = selectedQuote ? (selectedQuote.items || selectedQuote.quoteItems || []) : [];
      setQuoteItems(items);
    }
  }, [showQuoteForm, selectedQuote]);

  const handleWorkflow = async (type: string, data: any) => {
    try {
      if (type === 'approve') await quotesApi.approve(data.id);
      else if (type === 'reject') await quotesApi.reject(data.id);
      else if (type === 'convert') await quotesApi.convertToOrder(data.id);
      else if (type === 'confirm_order') {
        const res = await salesOrdersApi.confirm(data.orderNumber);
        const journal = (res.data as any)?.data?.journal;
        if (journal) {
          console.log('[FINANCE] 產生的會計分錄:', journal);
          alert(`訂單已確認！\n已自動產生會計分錄：${journal.journal_no}\n總額：${journal.total_amount}`);
        } else {
          alert('訂單已確認');
        }
      }
      else if (type === 'ship') {
        const p = { sales_order_no: data.orderNumber, delivery_date: new Date().toISOString(), items: data.items.map((i:any)=>({ sku_code: i.skuNumber, delivered_qty: i.quantity })) };
        await deliveryOrdersApi.create(p);
      } else if (type === 'produce') {
        for (const item of (data.items || [])) {
          await workOrdersApi.create({ 
            sku_code: item.skuNumber, 
            sales_order_no: data.orderNumber, 
            planned_qty: item.quantity, 
            priority: 'normal', 
            wo_type: 'regular',
            planned_end_date: data.expectedDeliveryDate
          });
        }
        // 更新訂單的製造狀態
        await salesOrdersApi.updateManufacturingStatus(data.id, 'sent');
      }
      alert('操作執行成功');
      queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
      queryClient.invalidateQueries({ queryKey: ['quotes'] });
    } catch (err: any) {
      alert('操作失敗: ' + (err.response?.data?.message || err.message));
    }
  };

  const toggleExpand = (id: string) => {
    const next = new Set(expItems);
    if (next.has(id)) next.delete(id); else next.add(id);
    setExpItems(next);
  };

  // --- 表單欄位配置 ---
  const customerTabs: FormTab[] = [
    { label: '基本資料', fields: [
      { name: 'customerCode', label: '客戶代碼', type: 'text', required: true, placeholder: 'CUST-000' },
      { name: 'customerName', label: '客戶全銜', type: 'text', required: true, placeholder: '輸入企業正式名稱' },
      { name: 'taxId', label: '統一編號', type: 'text', placeholder: '8 位數字統編' },
      { name: 'contactPerson', label: '首要聯繫人', type: 'text', placeholder: '姓名' },
    ]},
    { label: '聯繫地址', fields: [
      { name: 'phone', label: '公司電話', type: 'text', placeholder: '02-xxxx-xxxx' },
      { name: 'email', label: '電子郵件', type: 'text', placeholder: 'corp@example.com' },
      { name: 'invoiceAddress', label: '發票掛帳地址', type: 'text', placeholder: '發票開立地址' },
      { name: 'deliveryAddress', label: '預設送貨地址', type: 'text', placeholder: '物流配送地址', sameAs: 'invoiceAddress' },
    ]},
    { label: '財務條件', fields: [
      { name: 'paymentTerms', label: '付款條件', type: 'select', options: [
        { value: '月結30天', label: '月結 30 天' },
        { value: '月結60天', label: '月結 60 天' },
        { value: '現結', label: '貨到現結' },
        { value: '預付', label: '訂金預付' }
      ]},
      { name: 'currency', label: '預設幣別', type: 'select', options: [
        { value: 'TWD', label: 'TWD - 新台幣' },
        { value: 'USD', label: 'USD - 美元' },
        { value: 'JPY', label: 'JPY - 日圓' }
      ]},
      { name: 'creditLimit', label: '信用額度控管', type: 'number', placeholder: '0 代表不限額' }
    ]}
  ];



  return (
    <div className="sales-container">
      <header className="lux-header">
        <div className="lux-title-group">
          <div className="lux-subtitle">全球企業營收動態</div>
          <h1>銷售運營總覽</h1>
        </div>
        <div style={{ display: 'flex', gap: '1.5rem' }}>
          <div className="lux-tabs">
            <button className={`lux-tab-btn ${view === 'process' ? 'active' : ''}`} onClick={() => setView('process')}>流程總覽</button>
            <button className={`lux-tab-btn ${view === 'quotes' ? 'active' : ''}`} onClick={() => setView('quotes')}>報價管理</button>
            <button className={`lux-tab-btn ${view === 'orders' ? 'active' : ''}`} onClick={() => setView('orders')}>訂單管理</button>
            <button className={`lux-tab-btn ${view === 'customers' ? 'active' : ''}`} onClick={() => setView('customers')}>客戶管理</button>
          </div>
          {view !== 'process' && (
            <button className="lux-btn-primary" onClick={() => { 
              if(view === 'orders') { setSelectedOrder(null); setOrderItems([]); setShowOrderForm(true); }
              else if(view === 'quotes') { setSelectedQuote(null); setQuoteItems([]); setShowQuoteForm(true); }
              else { setSelectedCustomer(null); setShowCustomerForm(true); }
            }}>
              <Plus size={20} /> 
              {view === 'orders' ? '新增訂單' : view === 'quotes' ? '新增報價' : '新增客戶'}
            </button>
          )}
        </div>
      </header>

      {/* 看板、搜尋區塊保持不變 (略過以節省空間，請保持原始邏輯) */}
      <section className="lux-stats-grid">
         <div className="lux-stat-card">
           <div className="lux-stat-label">年度累計營收 (TWD)</div>
           <div className="lux-stat-value">${stats.revenue.toLocaleString()}</div>
           <div style={{ color: 'var(--lux-emerald)', fontSize: '0.75rem', marginTop: '0.5rem', display: 'flex', alignItems: 'center', gap: 4 }}>
             <ArrowUpRight size={14} /> {stats.growth} 比去年同期
           </div>
         </div>
         <div className="lux-stat-card">
           <div className="lux-stat-label">活躍商務合約</div>
           <div className="lux-stat-value">{stats.count}</div>
           <div style={{ color: '#64748b', fontSize: '0.75rem', marginTop: '0.5rem' }}>指令轉化率 88.5%</div>
         </div>
         <div className="lux-stat-card">
           <div className="lux-stat-label">目標達成進度</div>
           <div className="lux-stat-value">92<span style={{ fontSize: '1rem' }}>%</span></div>
           <div style={{ width: '100%', height: 2, background: 'rgba(255,255,255,0.05)', marginTop: 12, position: 'relative' }}>
              <div style={{ position: 'absolute', top: 0, left: 0, height: '100%', width: '92%', background: 'var(--lux-gold)', boxShadow: '0 0 10px var(--lux-gold)' }} />
           </div>
         </div>
         <div className="lux-stat-card">
           <div className="lux-stat-label">平均客單價值</div>
           <div className="lux-stat-value">${Math.round(stats.revenue / (stats.count || 1)).toLocaleString()}</div>
           <div style={{ color: '#64748b', fontSize: '0.75rem', marginTop: '0.5rem' }}>高價值客戶佔比 42%</div>
         </div>
      </section>

      <div className="lux-controls">
        <div className="lux-search-box">
          <Search size={18} color="var(--lux-gold)" />
          <input 
            placeholder={`搜尋${view === 'customers' ? '客戶名稱或代碼' : view === 'process' ? '流程單號' : '單號或客戶主體'}...`} 
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
      </div>

      <div className="lux-table-wrapper">
        <table className="lux-table">
          {view === 'process' ? (
            <>
              <thead>
                <tr>
                  <th>訂單編號</th>
                  <th>客戶名稱</th>
                  <th>金額 (TWD)</th>
                  <th style={{ textAlign: 'center' }}>流程生命週期 (Order-to-Cash)</th>
                  <th>階段狀態</th>
                  <th style={{ textAlign: 'right' }}>回收率</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={6} style={{ textAlign: 'center', padding: '4rem' }}>正在優化流程追蹤地圖...</td></tr>
                ) : processRows.map((row: any) => (
                  <tr key={row.orderId} className="lux-row">
                    <td style={{ fontFamily: 'monospace', color: 'var(--lux-gold)', fontWeight: 600 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        {row.orderNumber}
                        {row.gapDays > 0 && row.pipelineStatus !== 'completed' && (
                          <span title={`逾期 ${row.gapDays} 天`} style={{ color: '#ef4444' }}><Activity size={12} /></span>
                        )}
                      </div>
                    </td>
                    <td style={{ fontWeight: 700 }}>{row.customerName}</td>
                    <td style={{ fontFamily: 'monospace' }}>
                       <div>${row.orderAmount?.toLocaleString()}</div>
                       <div style={{ fontSize: '0.7rem', color: '#64748b' }}>實收: ${row.collectedAmount?.toLocaleString()}</div>
                    </td>
                    <td>
                      <div className="lux-pipeline-container">
                        {[
                          { key: 'pending', label: '下單', tip: `日期: ${row.orderDate}` },
                          { key: 'in_progress', label: '生產', tip: '製造執行中' },
                          { key: 'shipped', label: '出貨', tip: row.deliveryDate ? `日期: ${row.deliveryDate}` : '未發貨' },
                          { key: 'invoiced', label: '帳單', tip: `發票數: ${row.invoiceCount}` },
                          { key: 'completed', label: '結案', tip: row.collectionRate === 100 ? '收款完成' : '餘額未清' }
                        ].map((s, idx, arr) => (
                           <React.Fragment key={s.key}>
                             <div 
                                className={`lux-pipeline-point ${row.pipelineStatus === s.key ? 'active' : ''} ${arr.findIndex(x => x.key === row.pipelineStatus) >= idx ? 'reached' : ''}`} 
                                title={`${s.label}: ${s.tip}`} 
                             />
                             {idx < arr.length - 1 && (
                               <div className={`lux-pipeline-line ${arr.findIndex(x => x.key === row.pipelineStatus) > idx ? 'reached' : ''}`} />
                             )}
                           </React.Fragment>
                        ))}
                      </div>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        {React.createElement(pipelineConfig[row.pipelineStatus]?.icon || Activity, { size: 14, color: pipelineConfig[row.pipelineStatus]?.color })}
                        <span style={{ color: pipelineConfig[row.pipelineStatus]?.color, fontSize: '0.85rem', fontWeight: 600 }}>
                          {pipelineConfig[row.pipelineStatus]?.label}
                        </span>
                      </div>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'flex-end' }}>
                        <div style={{ width: 60, height: 4, background: 'rgba(255,255,255,0.05)', borderRadius: 2, overflow: 'hidden' }}>
                           <div style={{ width: `${row.collectionRate}%`, height: '100%', background: row.collectionRate === 100 ? 'var(--lux-emerald)' : 'var(--lux-gold)' }} />
                        </div>
                        <span style={{ fontSize: '0.75rem', fontFamily: 'monospace', color: row.collectionRate === 100 ? 'var(--lux-emerald)' : '#fff' }}>
                          {row.collectionRate}%
                        </span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </>
          ) : view === 'orders' ? (
            <>
              <thead>
                <tr>
                  <th style={{ width: 40 }}></th>
                  <th>單據編號</th>
                  <th>客戶主體</th>
                  <th>合約金額</th>
                  <th>下單時間</th>
                  <th>運營狀態</th>
                  <th style={{ textAlign: 'right' }}>操作特權</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: '4rem' }}>正在同步數據中心...</td></tr>
                ) : filteredOrders.map((so) => (
                  <React.Fragment key={so.id}>
                    <tr className="lux-row" style={{ cursor: 'pointer' }} onClick={() => toggleExpand(so.id)}>
                      <td style={{ textAlign: 'center' }}>
                         <ChevronRight size={14} style={{ transform: expItems.has(so.id) ? 'rotate(90deg)' : 'none', transition: '0.2s' }} />
                      </td>
                      <td style={{ fontFamily: 'monospace', color: 'var(--lux-gold)', fontWeight: 600 }}>{so.orderNumber}</td>
                      <td style={{ fontWeight: 700 }}>{so.customerName}</td>
                      <td style={{ fontFamily: 'monospace', fontWeight: 700 }}>${so.totalAmount?.toLocaleString()}</td>
                      <td>{so.orderDate?.slice(0, 10)}</td>
                      <td><StatusBadge status={so.status} /></td>
                      <td style={{ textAlign: 'right' }} onClick={e => e.stopPropagation()}>
                         <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                           {(so.status === 'draft' || so.status === 'pending') && (
                             <button className="lux-tab-btn" title="確認並生效訂單" onClick={() => handleWorkflow('confirm_order', so)} style={{ color: '#10b981' }}><Check size={16} /></button>
                           )}
                           {(so.status === 'confirmed' || so.status === 'partial') && (
                             <>
                               {so.manufacturingStatus === 'sent' ? (
                                 <button className="lux-tab-btn" title="已轉發工單" style={{ color: '#10b981', cursor: 'default' }}><CheckCircle2 size={16} /></button>
                               ) : (
                                 <button className="lux-tab-btn" title="轉發工單" onClick={() => handleWorkflow('produce', so)} style={{ color: '#8b5cf6' }} ripple-color="rgba(139, 92, 246, 0.2)"><Factory size={16} /></button>
                               )}
                               <button className="lux-tab-btn" title="立即發貨" onClick={() => handleWorkflow('ship', so)} style={{ color: '#10b981' }}><Truck size={16} /></button>
                             </>
                           )}
                           <button className="lux-tab-btn" onClick={() => { setSelectedOrder(so); setShowOrderForm(true); }}><Edit size={16} /></button>
                         </div>
                      </td>
                    </tr>
                    {expItems.has(so.id) && (
                      <tr className="lux-detail-row">
                        <td colSpan={7} style={{ background: 'rgba(0,0,0,0.2)', padding: '1.5rem 3rem' }}>
                           <motion.div 
                              initial={{ opacity: 0, x: -10 }}
                              animate={{ opacity: 1, x: 0 }}
                              style={{ borderLeft: '2px solid var(--lux-gold)', paddingLeft: '2rem' }}
                            >
                              <div style={{ fontSize: '0.75rem', color: 'var(--lux-gold)', fontWeight: 800, textTransform: 'uppercase', marginBottom: '1.5rem', letterSpacing: '0.2em', display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
                                 <FileText size={16} /> 銷售訂單詳細清單 / Contract Detail
                              </div>
                              <div style={{ 
                                 display: 'grid', 
                                 gridTemplateColumns: 'minmax(250px, 3fr) 100px 80px 120px 120px', 
                                 gap: '1.5rem', 
                                 padding: '0.8rem 1.2rem', 
                                 background: 'rgba(212, 175, 55, 0.06)', 
                                 borderRadius: '8px',
                                 marginBottom: '0.5rem', 
                                 fontSize: '0.75rem', 
                                 fontWeight: 800, 
                                 color: 'var(--lux-gold)', 
                                 textTransform: 'uppercase', 
                                 letterSpacing: '0.15em',
                                 border: '1px solid rgba(212, 175, 55, 0.1)'
                              }}>
                                 <div>產品品號與品名</div>
                                 <div style={{ textAlign: 'right' }}>訂單數量</div>
                                 <div style={{ textAlign: 'center' }}>單位</div>
                                 <div style={{ textAlign: 'right' }}>單價</div>
                                 <div style={{ textAlign: 'right' }}>合計</div>
                              </div>
                              {so.items?.map((item: any, idx: number) => (
                                <div key={idx} style={{ display: 'grid', gridTemplateColumns: 'minmax(250px, 3fr) 100px 80px 120px 120px', gap: '1.5rem', padding: '1.2rem', borderBottom: '1px solid rgba(255,255,255,0.03)', fontSize: '0.9rem', alignItems: 'center' }}>
                                   <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                      <span style={{ color: '#fff', fontWeight: 600, fontSize: '0.95rem' }}>{item.productName || item.skuNumber}</span>
                                      <span style={{ fontSize: '0.75rem', color: '#64748b', fontFamily: 'var(--font-mono)', letterSpacing: '0.05em' }}>
                                         品號: {item.skuNumber || item.skuId || '---'}
                                      </span>
                                   </div>
                                   <div style={{ textAlign: 'right', fontWeight: 600, color: '#fff', fontFamily: 'var(--font-mono)' }}>{item.quantity?.toLocaleString()}</div>
                                   <div style={{ textAlign: 'center', color: '#94a3b8', fontSize: '0.8rem' }}>
                                     {(uoms?.data || []).find(u => u.uomCode === item.unit)?.uomName || item.unit}
                                   </div>
                                   <div style={{ textAlign: 'right', color: '#94a3b8', fontFamily: 'var(--font-mono)' }}>${item.unitPrice?.toLocaleString()}</div>
                                   <div style={{ textAlign: 'right', fontWeight: 700, color: 'var(--lux-gold)', fontFamily: 'var(--font-mono)', fontSize: '1rem' }}>
                                      ${((item.quantity || 0) * (item.unitPrice || 0)).toLocaleString()}
                                   </div>
                                </div>
                              ))}
                           </motion.div>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                ))}
              </tbody>
            </>
          ) : view === 'quotes' ? (
            <>
              <thead>
                <tr>
                  <th style={{ width: 40 }}></th>
                  <th>報價編號</th>
                  <th>目標客戶</th>
                  <th>預估價值</th>
                  <th>報價日期</th>
                  <th>轉化狀態</th>
                  <th style={{ textAlign: 'right' }}>操作特權</th>
                </tr>
              </thead>
              <tbody>
                {filteredQuotes.map((q: any) => (
                  <React.Fragment key={q.id}>
                    <tr className="lux-row" style={{ cursor: 'pointer' }} onClick={() => toggleExpand(q.id)}>
                      <td style={{ textAlign: 'center' }}>
                         <ChevronRight size={14} style={{ transform: expItems.has(q.id) ? 'rotate(90deg)' : 'none', transition: '0.2s' }} />
                      </td>
                      <td style={{ fontFamily: 'monospace', color: '#fbbf24', fontWeight: 600 }}>{q.quoteNumber}</td>
                      <td style={{ fontWeight: 700 }}>{q.customerName}</td>
                      <td style={{ fontFamily: 'monospace' }}>${q.finalAmount?.toLocaleString()}</td>
                      <td>{q.quoteDate?.slice(0, 10)}</td>
                      <td><StatusBadge status={q.status} /></td>
                      <td style={{ textAlign: 'right' }} onClick={e => e.stopPropagation()}>
                         <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                           {q.status === 'draft' && (
                             <>
                               <button className="lux-tab-btn" title="核准報價" onClick={() => handleWorkflow('approve', q)} style={{ color: '#10b981' }}><Check size={16} /></button>
                               <button className="lux-tab-btn" title="駁回報價" onClick={() => handleWorkflow('reject', q)} style={{ color: '#ef4444' }}><X size={16} /></button>
                             </>
                           )}
                           {q.status === 'approved' && (
                             <button className="lux-tab-btn" title="轉成訂單" onClick={() => handleWorkflow('convert', q)} style={{ color: '#60a5fa' }}><ArrowRightLeft size={16} /></button>
                           )}
                           <button className="lux-tab-btn" onClick={() => { setSelectedQuote(q); setShowQuoteForm(true); }}><Edit size={16} /></button>
                         </div>
                      </td>
                    </tr>
                    {expItems.has(q.id) && (
                      <tr className="lux-detail-row">
                        <td colSpan={7} style={{ background: 'rgba(0,0,0,0.2)', padding: '1.5rem 3rem' }}>
                           <motion.div 
                              initial={{ opacity: 0, x: -10 }}
                              animate={{ opacity: 1, x: 0 }}
                              style={{ borderLeft: '2px solid #fbbf24', paddingLeft: '2rem' }}
                            >
                              <div style={{ fontSize: '0.75rem', color: '#fbbf24', fontWeight: 800, textTransform: 'uppercase', marginBottom: '1.5rem', letterSpacing: '0.2em', display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
                                 <FileText size={16} /> 報價詳細清單 / Estimation Detail
                              </div>
                              <div style={{ 
                                 display: 'grid', 
                                 gridTemplateColumns: 'minmax(250px, 3fr) 100px 80px 120px 120px', 
                                 gap: '1.5rem', 
                                 padding: '0.8rem 1.2rem', 
                                 background: 'rgba(251, 191, 36, 0.06)', 
                                 borderRadius: '8px',
                                 marginBottom: '0.5rem', 
                                 fontSize: '0.75rem', 
                                 fontWeight: 800, 
                                 color: '#fbbf24', 
                                 textTransform: 'uppercase', 
                                 letterSpacing: '0.15em',
                                 border: '1px solid rgba(251, 191, 36, 0.1)'
                              }}>
                                 <div>產品品號與品名</div>
                                 <div style={{ textAlign: 'right' }}>報價數量</div>
                                 <div style={{ textAlign: 'center' }}>單位</div>
                                 <div style={{ textAlign: 'right' }}>單價</div>
                                 <div style={{ textAlign: 'right' }}>預估合計</div>
                              </div>
                              {(q.items || q.quoteItems || [])?.map((item: any, idx: number) => (
                                <div key={idx} style={{ display: 'grid', gridTemplateColumns: 'minmax(250px, 3fr) 100px 80px 120px 120px', gap: '1.5rem', padding: '1.2rem', borderBottom: '1px solid rgba(255,255,255,0.03)', fontSize: '0.9rem', alignItems: 'center' }}>
                                   <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                      <span style={{ color: '#fff', fontWeight: 600, fontSize: '0.95rem' }}>{item.productName || item.skuNumber}</span>
                                      <span style={{ fontSize: '0.75rem', color: '#64748b', fontFamily: 'var(--font-mono)', letterSpacing: '0.05em' }}>
                                         品號: {item.skuNumber || item.skuId || '---'}
                                      </span>
                                   </div>
                                   <div style={{ textAlign: 'right', fontWeight: 600, color: '#fff', fontFamily: 'var(--font-mono)' }}>{item.quantity?.toLocaleString()}</div>
                                   <div style={{ textAlign: 'center', color: '#94a3b8', fontSize: '0.8rem' }}>
                                     {(uoms?.data || []).find(u => u.uomCode === item.unit)?.uomName || item.unit}
                                   </div>
                                   <div style={{ textAlign: 'right', color: '#94a3b8', fontFamily: 'var(--font-mono)' }}>${item.unitPrice?.toLocaleString()}</div>
                                   <div style={{ textAlign: 'right', fontWeight: 700, color: '#fbbf24', fontFamily: 'var(--font-mono)', fontSize: '1rem' }}>
                                      ${((item.quantity || 0) * (item.unitPrice || 0)).toLocaleString()}
                                   </div>
                                </div>
                              ))}
                           </motion.div>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                ))}
              </tbody>
            </>
          ) : (
            <>
              <thead>
                <tr>
                  <th>客戶代碼</th>
                  <th>企業全稱</th>
                  <th>核心聯繫人</th>
                  <th>信用等級</th>
                  <th style={{ textAlign: 'right' }}>操作特權</th>
                </tr>
              </thead>
              <tbody>
                {filteredCustomers.map((c: any) => (
                  <tr key={c.id} className="lux-row">
                    <td style={{ fontFamily: 'monospace', color: 'var(--lux-gold)' }}>{c.customerCode}</td>
                    <td style={{ fontWeight: 700 }}>{c.customerName}</td>
                    <td>{c.contactPerson || '---'}</td>
                    <td>
                      <span className="lux-badge" style={{ 
                        background: c.creditRating?.includes('AAA') || c.creditRating?.includes('AA') 
                          ? 'rgba(16, 185, 129, 0.1)' 
                          : c.creditRating?.includes('A ') 
                            ? 'rgba(96, 165, 250, 0.1)' 
                            : 'rgba(239, 68, 68, 0.1)', 
                        color: c.creditRating?.includes('AAA') || c.creditRating?.includes('AA') 
                          ? '#10b981' 
                          : c.creditRating?.includes('A ') 
                            ? '#60a5fa' 
                            : '#ef4444' 
                      }}>
                        {c.creditRating || 'B 級普通信用'}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                       <button className="icon-action-btn" onClick={() => { setSelectedCustomer(c); setShowCustomerForm(true); }}><Edit size={16} /></button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </>
          )}
        </table>
      </div>

      {/* 銷售訂單表單 */}
      <FormModal
        open={showOrderForm}
        title={selectedOrder ? "編輯銷售訂單 " : "建立銷售訂單"}
        onClose={() => setShowOrderForm(false)}
        initialValues={selectedOrder || undefined}
        width={800}
        tabs={[
          { label: '訂單標頭', fields: [
            { name: 'orderNumber', label: '訂單編號', type: 'text', required: true, placeholder: '系統自動編號', description: '留空由系統配號' },
            { name: 'customerId', label: '簽屬客戶 *', type: 'select', required: true, options: customers.map(c => ({ value: c.id, label: `${c.customerCode} - ${c.customerName}` })) },
            { name: 'orderDate', label: '下單日期', type: 'date', required: true },
            { name: 'expectedDeliveryDate', label: '預計交貨期', type: 'date' },
            { name: 'currency', label: '交易幣別', type: 'select', options: [{ value: 'TWD', label: 'TWD' }, { value: 'USD', label: 'USD' }] },
            { name: 'paymentTerms', label: '付款條件', type: 'text', placeholder: '如: 月結30天' },
          ]},
          { label: '產品明細', fields: [
            { name: 'items_editor', type: 'custom' }
          ] },
          { label: '備註條款', fields: [
            { name: 'orderRemarks', label: '合約與備註', type: 'textarea' }
          ]}
        ]}
        customRenderers={{
          items_editor: <ItemsTable items={orderItems} setItems={setOrderItems} skus={skus} uoms={uoms} />
        }}
        onSubmit={async (values) => {
          const payload = { ...values, items: orderItems };
          if (selectedOrder) await salesOrdersApi.update(selectedOrder.id, payload);
          else await salesOrdersApi.create(payload);
          queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
          setShowOrderForm(false);
        }}
      />

      {/* 報價單表單 */}
      <FormModal
        open={showQuoteForm}
        title={selectedQuote ? "編輯報價單" : "建立報價單"}
        onClose={() => setShowQuoteForm(false)}
        initialValues={selectedQuote || undefined}
        width={800}
        tabs={[
          { label: '報價標頭', fields: [
            { name: 'quoteNumber', label: '報價編號', type: 'text', required: true, placeholder: 'QUOT-YYYYMM-XXX' },
            { name: 'customerId', label: '目標客戶 *', type: 'select', required: true, options: customers.map(c => ({ value: c.id, label: c.customerName })) },
            { name: 'quoteDate', label: '報價日期', type: 'date', required: true },
            { name: 'validUntil', label: '效期至', type: 'date' },
            { name: 'currency', label: '幣別', type: 'select', options: [{ value: 'TWD', label: 'TWD' }, { value: 'USD', label: 'USD' }] },
          ]},
          { label: '報價明細', fields: [
            { name: 'quote_items_editor', type: 'custom' }
          ] },
          { label: '備註', fields: [
            { name: 'notes', label: '報價說明', type: 'textarea' }
          ]}
        ]}
        customRenderers={{
          quote_items_editor: <ItemsTable items={quoteItems} setItems={setQuoteItems} isQuote skus={skus} uoms={uoms} />
        }}
        onSubmit={async (values) => {
          const payload = { ...values, items: quoteItems };
          if (selectedQuote) await quotesApi.update(selectedQuote.id, payload);
          else await quotesApi.create(payload);
          queryClient.invalidateQueries({ queryKey: ['quotes'] });
          setShowQuoteForm(false);
        }}
      />

      {/* 客戶主檔表單 */}
      <FormModal
        open={showCustomerForm}
        title={selectedCustomer ? "編輯客戶資料" : "核心客戶建檔"}
        onClose={() => setShowCustomerForm(false)}
        initialValues={selectedCustomer || undefined}
        width={700}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['基本資料', '聯繫資訊', '財務與信用'],
          getStepFields: (idx) => customerTabs[idx].fields
        }}
        onSubmit={async (values) => {
          if (selectedCustomer) await customersApi.update(selectedCustomer.id, values);
          else await customersApi.create(values);
          queryClient.invalidateQueries({ queryKey: ['customers'] });
          setShowCustomerForm(false);
        }}
      />
    </div>
  );
};

