import React, { useState, useMemo, useEffect } from 'react';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Search, Plus, Edit, X, Trash2,
  ChevronRight, ChevronDown, Loader2,
  FileText, CheckCircle2, DollarSign,
  Calendar, Check, XCircle, Truck,
  Factory, ArrowRightLeft, ArrowUpRight, Activity, Layers
} from 'lucide-react';
import { useSalesOrders, useQuotes, useCustomers, useOrderToCash, useUoms } from '../hooks/useApi';
import { FormModal, type FormTab } from '../components/FormModal';
import { salesOrdersApi, customersApi, skusApi, quotesApi, deliveryOrdersApi, workOrdersApi, inventoryApi, warehousesApi, binLocationsApi } from '../api/api';
import type { BinLocationDto, SkuDto, WarehouseDto } from '../api/inventory';
import type { CustomerDto, SalesOrderConfirmationDto, SalesOrderDto } from '../api/sales';
import { CashReceiptsPanel } from './CashReceiptsPanel';
import './styles/Sales-Luxury.css';

type SalesLineItem = {
  skuId?: string;
  productId?: string;
  skuNumber?: string;
  productName?: string;
  skuName?: string;
  quantity: number;
  deliveredQty?: number;
  unit?: string;
  unitPrice: number;
  needManufacturing?: boolean;
  needOutsourcing?: boolean;
};

type SalesQuoteDto = {
  id: string;
  quoteNumber?: string;
  customerName?: string;
  customerId?: string;
  quoteDate?: string;
  finalAmount?: number;
  status: string;
  items?: SalesLineItem[];
  quoteItems?: SalesLineItem[];
};

type SalesProcessRow = {
  orderId: string;
  orderNumber: string;
  customerName?: string;
  orderAmount?: number;
  collectedAmount?: number;
  gapDays: number;
  pipelineStatus: string;
  orderDate?: string;
  deliveryDate?: string;
  invoiceCount?: number;
  collectionRate: number;
};

type SalesProcessSummary = {
  totalAmount?: number;
  totalOrders?: number;
};

type SalesCustomerDto = CustomerDto & {
  creditRating?: string;
};

type SalesProcessResponse = {
  rows?: SalesProcessRow[];
  summary?: SalesProcessSummary;
  rowLimit?: number;
};

type SalesWorkflowError = {
  response?: { data?: { message?: string } };
  message?: string;
};

const EMPTY_SALES_ORDERS: SalesOrderDto[] = [];
const EMPTY_SALES_QUOTES: SalesQuoteDto[] = [];
const EMPTY_CUSTOMERS: CustomerDto[] = [];
const EMPTY_PROCESS_ROWS: SalesProcessRow[] = [];
const EMPTY_PROCESS_SUMMARY: SalesProcessSummary = {};

type SalesSkuSearchProps = {
  selectedLabel?: string;
  onSelect: (sku: SkuDto) => void;
};

const SalesSkuSearch: React.FC<SalesSkuSearchProps> = ({ selectedLabel, onSelect }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    const timeout = window.setTimeout(() => setDebouncedSearchTerm(searchTerm.trim()), 300);
    return () => window.clearTimeout(timeout);
  }, [searchTerm]);

  const { data: searchResults = [], isFetching, isError } = useQuery({
    queryKey: ['sellable-sku-search', debouncedSearchTerm],
    queryFn: async () => {
      const params: Record<string, string> = {
        page: '1',
        pageSize: '25',
        isSellable: 'true'
      };
      if (debouncedSearchTerm) params.searchTerm = debouncedSearchTerm;
      const response = await skusApi.list(params);
      if (!response.data.success) {
        throw new Error(response.data.message || '搜尋銷售商品失敗');
      }
      return response.data.data ?? [];
    },
    enabled: isOpen,
    staleTime: 30_000
  });

  const openOptions = () => {
    if (selectedLabel) {
      setSearchTerm('');
      setDebouncedSearchTerm('');
    }
    setIsOpen(true);
  };

  return (
    <div style={{ position: 'relative' }}>
      <div style={{ display: 'flex', position: 'relative' }}>
        <input
          type="search"
          className="form-input"
          value={isOpen ? searchTerm : selectedLabel || ''}
          onFocus={openOptions}
          onChange={event => {
            setSearchTerm(event.target.value);
            setIsOpen(true);
          }}
          onBlur={() => setIsOpen(false)}
          placeholder="選擇商品或輸入品號、品名搜尋"
          autoComplete="off"
          style={{ width: '100%', paddingRight: 36 }}
        />
        <button
          type="button"
          aria-label={isOpen ? '關閉商品清單' : '開啟商品清單'}
          onMouseDown={event => event.preventDefault()}
          onClick={() => isOpen ? setIsOpen(false) : openOptions()}
          style={{
            position: 'absolute',
            right: 0,
            top: 0,
            bottom: 0,
            width: 36,
            border: 0,
            background: 'transparent',
            color: 'var(--slate-400)',
            cursor: 'pointer'
          }}
        >
          <ChevronDown size={16} />
        </button>
      </div>
      {isOpen && (
        <div style={{
          position: 'absolute',
          top: '100%',
          left: 0,
          right: 0,
          zIndex: 20,
          maxHeight: 240,
          overflowY: 'auto',
          background: '#07111f',
          border: '1px solid rgba(255,255,255,0.15)',
          borderRadius: 6
        }}>
          {isFetching ? (
            <div style={{ padding: 10, color: 'var(--slate-400)', fontSize: 12 }}>搜尋中…</div>
          ) : isError ? (
            <div style={{ padding: 10, color: 'var(--rose-400)', fontSize: 12 }}>搜尋商品失敗，請稍後重試</div>
          ) : searchResults.length === 0 ? (
            <div style={{ padding: 10, color: 'var(--slate-500)', fontSize: 12 }}>找不到符合的可銷售商品</div>
          ) : searchResults.map(sku => (
            <button
              key={sku.id}
              type="button"
              onMouseDown={event => event.preventDefault()}
              onClick={() => {
                onSelect(sku);
                setIsOpen(false);
                setSearchTerm('');
                setDebouncedSearchTerm('');
              }}
              style={{
                display: 'block',
                width: '100%',
                padding: '9px 10px',
                textAlign: 'left',
                color: '#fff',
                background: 'transparent',
                border: 0,
                cursor: 'pointer'
              }}
            >
              <span>{sku.skuNumber} - {sku.skuName}</span>
              {sku.specification && <span style={{ display: 'block', color: 'var(--slate-400)', fontSize: 11 }}>{sku.specification}</span>}
              <span style={{ display: 'block', color: 'var(--slate-500)', fontSize: 11 }}>
                銷售單位：{sku.salesUomCode || sku.salesUomName || sku.unit || '未設定'}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
};

// 庫存狀態組件
const StockStatus: React.FC<{ sku: string; qty: number }> = ({ sku, qty }) => {
  const [atp, setAtp] = useState<{ available: number; is_sufficient?: boolean } | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!sku) return;
    const check = async () => {
      setLoading(true);
      try {
        const res = await inventoryApi.checkATP(sku, qty);
        setAtp(res.data.data ?? null);
      } catch {
        setAtp(null);
      } finally {
        setLoading(false);
      }
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

const pipelineConfig: Record<string, { label: string; color: string; icon: React.ElementType }> = {
  pending:           { label: '待處理', color: '#94a3b8', icon: Calendar },
  in_progress:       { label: '生產/執行中', color: '#60a5fa', icon: Activity },
  shipped:           { label: '已發貨', color: '#10b981', icon: Truck },
  in_delivery:       { label: '物流配送中', color: '#34d399', icon: Truck },
  invoiced:          { label: '已開立發票', color: '#22d3ee', icon: FileText },
  partial_collection: { label: '部分收款', color: '#fbbf24', icon: DollarSign },
  completed:         { label: '結案完成', color: '#d4af37', icon: CheckCircle2 },
  cancelled:         { label: '已取消', color: '#ef4444', icon: XCircle },
};

const formatDateInput = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;

const StatusBadge: React.FC<{ status: string }> = ({ status }) => {
  const conf = statusConfig[status] || { label: status, color: '#94a3b8', bg: 'rgba(255,255,255,0.05)' };
  return (
    <span className="lux-badge" style={{ color: conf.color, backgroundColor: conf.bg, border: `1px solid ${conf.color}20` }}>
      {conf.label}
    </span>
  );
};

export const Sales: React.FC = () => {
  const queryClient = useQueryClient();
  const [view, setView] = useState<'orders' | 'quotes' | 'customers' | 'process' | 'receipts'>('process');
  const [search, setSearch] = useState("");
  const [dateRange, setDateRange] = useState(() => {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setMonth(startDate.getMonth() - 3);
    return { startDate: formatDateInput(startDate), endDate: formatDateInput(endDate) };
  });
  const [includeAllOrders, setIncludeAllOrders] = useState(false);
  const validDateRange = includeAllOrders || dateRange.startDate <= dateRange.endDate;
  
  const [showOrderForm, setShowOrderForm] = useState(false);
  const [showQuoteForm, setShowQuoteForm] = useState(false);
  const [showCustomerForm, setShowCustomerForm] = useState(false);
  const [showShipmentForm, setShowShipmentForm] = useState(false);
  
  const [selectedOrder, setSelectedOrder] = useState<SalesOrderDto | null>(null);
  const [selectedQuote, setSelectedQuote] = useState<SalesQuoteDto | null>(null);
  const [selectedCustomer, setSelectedCustomer] = useState<SalesCustomerDto | null>(null);
  const [selectedShipmentOrder, setSelectedShipmentOrder] = useState<SalesOrderDto | null>(null);
  const [shipmentLines, setShipmentLines] = useState<Array<SalesLineItem & { skuId: string; remainingQty: number }>>([]);
  
  // 明細項狀態
  const [orderItems, setOrderItems] = useState<SalesLineItem[]>([]);
  const [quoteItems, setQuoteItems] = useState<SalesLineItem[]>([]);
  const [expItems, setExpItems] = useState<Set<string>>(new Set());
  
  const { data: ordResp, isLoading: ordersLoading } = useSalesOrders();
  const { data: quoResp, isLoading: quotesLoading } = useQuotes();
  const { data: custResp, isLoading: customersLoading } = useCustomers();
  const { data: shippingWarehouses = [] } = useQuery({
    queryKey: ['shipping-warehouses'],
    queryFn: async () => {
      const response = await warehousesApi.list();
      if (!response.data.success) throw new Error(response.data.message || '載入倉庫失敗');
      return response.data.data ?? [];
    }
  });
  const { data: shippingBins = [] } = useQuery({
    queryKey: ['shipping-bin-locations'],
    queryFn: async () => {
      const response = await binLocationsApi.list();
      if (!response.data.success) throw new Error(response.data.message || '載入儲位失敗');
      return response.data.data ?? [];
    }
  });
  const orderToCashParams = useMemo(() => includeAllOrders
    ? { includeAll: 'true' }
    : { startDate: dateRange.startDate, endDate: dateRange.endDate }, [dateRange, includeAllOrders]);
  const {
    data: procResp,
    isLoading: processLoading,
    isError: processError,
    error: processQueryError,
    refetch: refetchOrderToCash
  } = useOrderToCash(orderToCashParams, validDateRange);
  const { data: uoms } = useUoms();

  const orders = ordResp?.data ?? EMPTY_SALES_ORDERS;
  const quotes = (quoResp?.data ?? EMPTY_SALES_QUOTES) as SalesQuoteDto[];
  const customers = (custResp?.data ?? EMPTY_CUSTOMERS) as SalesCustomerDto[];
  const processResponse = procResp?.data as SalesProcessResponse | undefined;
  const processRows = processResponse?.rows ?? EMPTY_PROCESS_ROWS;
  const processSummary = processResponse?.summary ?? EMPTY_PROCESS_SUMMARY;
  const processRowLimit = processResponse?.rowLimit ?? 200;
  
  const isLoading = ordersLoading || quotesLoading || customersLoading || processLoading;

  const filteredOrders = useMemo(() => orders.filter(o =>
    o.orderNumber?.toLowerCase().includes(search.toLowerCase()) || 
    (o.customerName || '').toLowerCase().includes(search.toLowerCase())
  ), [orders, search]);

  const filteredQuotes = useMemo(() => quotes.filter(q =>
    q.quoteNumber?.toLowerCase().includes(search.toLowerCase()) || 
    (q.customerName || '').toLowerCase().includes(search.toLowerCase())
  ), [quotes, search]);

  const filteredCustomers = useMemo(() => customers.filter(c =>
    c.customerCode?.toLowerCase().includes(search.toLowerCase()) || 
    c.customerName?.toLowerCase().includes(search.toLowerCase())
  ), [customers, search]);

  const filteredProcessRows = useMemo(() => processRows.filter(row =>
    row.orderNumber?.toLowerCase().includes(search.toLowerCase()) ||
    (row.customerName || '').toLowerCase().includes(search.toLowerCase())
  ), [processRows, search]);

  const stats = useMemo(() => ({
    revenue: view === 'process'
      ? processSummary.totalAmount || 0
      : orders.reduce((acc, cur) => acc + (cur.totalAmount || 0), 0),
    count: view === 'process' ? processSummary.totalOrders || 0 : orders.length,
    growth: "+14.2%"
  }), [orders, processSummary, view]);

  const handleWorkflow = async (
    type: 'ship' | 'approve' | 'reject' | 'convert' | 'confirm_order' | 'produce',
    data: SalesOrderDto | SalesQuoteDto
  ) => {
    if (type === 'ship' && 'orderNumber' in data) {
      const lines = (data.items ?? [])
        .map(item => ({
          ...item,
          skuId: item.skuId || item.productId,
          remainingQty: Math.max(0, Number(item.quantity || 0) - Number(item.deliveredQty || 0))
        }))
        .filter((item): item is typeof item & { skuId: string } => Boolean(item.skuId && item.remainingQty > 0));
      if (lines.length === 0) {
        alert('訂單沒有尚未出貨的品項。');
        return;
      }
      setSelectedShipmentOrder(data);
      setShipmentLines(lines);
      setShowShipmentForm(true);
      return;
    }

    try {
      let successMessage = '操作執行成功';
      if (type === 'approve') await quotesApi.approve(data.id);
      else if (type === 'reject') await quotesApi.reject(data.id);
      else if (type === 'convert') await quotesApi.convertToOrder(data.id);
      else if (type === 'confirm_order') {
        const order = data as SalesOrderDto;
        const res = await salesOrdersApi.confirm(order.orderNumber);
        const confirmation = res.data.data;
        const journal = (confirmation as (SalesOrderConfirmationDto & {
          journal?: { journal_no: string; total_amount: number };
        }) | undefined)?.journal;
        if (journal) {
          console.log('[FINANCE] 產生的會計分錄:', journal);
          successMessage = `訂單已確認並生效！\n已自動產生會計分錄：${journal.journal_no}\n總額：${journal.total_amount}`;
        } else {
          successMessage = '訂單已確認並生效';
        }
        const warnings: string[] = [];
        if (confirmation?.creditLimitExceeded) {
          warnings.push(
            `信用額度提醒：訂單後占用 ${confirmation.creditUsageAfterOrder.toLocaleString()}，額度 ${confirmation.creditLimit.toLocaleString()}；依目前政策仍允許確認。`
          );
        }
        if (confirmation?.inventoryWarnings.length) {
          warnings.push(
            '成品庫存不足（訂單仍已確認）：\n' +
            confirmation.inventoryWarnings.map(item =>
              `第 ${item.lineNumber} 項 ${item.skuNumber || item.skuName || ''}：需求 ${item.requiredQty}、可用 ${item.availableQty}、缺少 ${item.shortageQty}`
            ).join('\n')
          );
        }
        if (warnings.length > 0) successMessage += `\n\n${warnings.join('\n\n')}`;
      }
      else if (type === 'produce') {
        const response = await workOrdersApi.createFromSalesOrder(data.id);
        const workOrders = response.data.data ?? [];
        const workOrderNumbers = workOrders.map(workOrder => workOrder.woNumber).filter(Boolean);
        const message = response.data.message || '工單已建立並連結至銷售訂單';
        successMessage = workOrderNumbers.length > 0
          ? `${message}：${workOrderNumbers.join('、')}（狀態：未發料／未開工）`
          : message;
        const shortages = workOrders.flatMap(workOrder =>
          (workOrder.materials ?? [])
            .filter(material => material.shortageQty > 0)
            .map(material =>
              `${material.skuNumber || material.skuName || '未命名物料'}：需求 ${material.requiredQty}、可用 ${material.availableQty}、缺料 ${material.shortageQty}`
            )
        );
        if (shortages.length > 0) {
          successMessage += `\n\n缺料提醒（工單仍已建立，未自動產生請購單）：\n${shortages.join('\n')}`;
        }
      }
      alert(successMessage);
      queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
      queryClient.invalidateQueries({ queryKey: ['quotes'] });
      queryClient.invalidateQueries({ queryKey: ['work-orders'] });
    } catch (error: unknown) {
      const workflowError = error as SalesWorkflowError;
      alert('操作失敗: ' + (workflowError.response?.data?.message || workflowError.message || '未知錯誤'));
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

  const ItemsTable = ({ items, setItems, isQuote = false }: {
    items: SalesLineItem[];
    setItems: React.Dispatch<React.SetStateAction<SalesLineItem[]>>;
    isQuote?: boolean;
  }) => {
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
            {!isQuote && <span style={{ flex: 0.8, textAlign: 'center' }}>需製造</span>}
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
                <SalesSkuSearch
                  selectedLabel={item.skuNumber ? `${item.skuNumber} - ${item.productName || ''}` : ''}
                  onSelect={sku => {
                    const news = [...items];
                    news[idx] = { 
                      ...news[idx], 
                      skuId: sku.id,
                      productId: sku.id,
                      skuNumber: sku.skuNumber,
                      productName: sku.skuName,
                      unit: sku.salesUomCode || sku.salesUomName || sku.unit || 'PCS',
                      unitPrice: sku.suggestedPrice || sku.purchasePrice || 0,
                      needManufacturing: sku.isManufacturable ?? false,
                      needOutsourcing: sku.isOutsourced ?? false
                    };
                    setItems(news);
                  }}
                />
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
              {!isQuote && (
                <div style={{ flex: 0.8, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                  <input
                    type="checkbox"
                    aria-label={`${item.skuNumber || item.productName || '品項'}需製造`}
                    checked={!!item.needManufacturing}
                    disabled={!!item.needOutsourcing || !!selectedOrder}
                    title={selectedOrder ? '既有訂單的製造設定請於建立訂單時設定' : undefined}
                    onChange={event => {
                      const news = [...items];
                      news[idx] = { ...news[idx], needManufacturing: event.target.checked };
                      setItems(news);
                    }}
                  />
                </div>
              )}
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
            <button className={`lux-tab-btn ${view === 'receipts' ? 'active' : ''}`} onClick={() => setView('receipts')}>收款管理</button>
          </div>
          {view !== 'process' && view !== 'receipts' && (
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
           <div className="lux-stat-label">訂單金額 (TWD)</div>
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

      {view !== 'receipts' && <div className="lux-controls">
        <div className="lux-search-box">
          <Search size={18} color="var(--lux-gold)" />
          <input 
            placeholder={`搜尋${view === 'customers' ? '客戶名稱或代碼' : view === 'process' ? '流程單號' : '單號或客戶主體'}...`} 
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        {view === 'process' && (
          <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 10, marginLeft: 'auto' }}>
            <span style={{ color: '#94a3b8', fontSize: 12 }}>訂單日期</span>
            <input
              aria-label="訂單起始日期"
              type="date"
              value={dateRange.startDate}
              disabled={includeAllOrders}
              onChange={event => {
                setIncludeAllOrders(false);
                setDateRange(range => ({ ...range, startDate: event.target.value }));
              }}
              className="form-input"
              style={{ width: 150 }}
            />
            <span style={{ color: '#64748b' }}>至</span>
            <input
              aria-label="訂單結束日期"
              type="date"
              value={dateRange.endDate}
              disabled={includeAllOrders}
              onChange={event => {
                setIncludeAllOrders(false);
                setDateRange(range => ({ ...range, endDate: event.target.value }));
              }}
              className="form-input"
              style={{ width: 150 }}
            />
            <button
              type="button"
              className="lux-btn-primary"
              onClick={() => {
                const endDate = new Date();
                const startDate = new Date();
                startDate.setMonth(startDate.getMonth() - 3);
                setDateRange({ startDate: formatDateInput(startDate), endDate: formatDateInput(endDate) });
                setIncludeAllOrders(false);
              }}
            >
              近三個月
            </button>
            <button
              type="button"
              className="lux-btn-primary"
              aria-pressed={includeAllOrders}
              onClick={() => setIncludeAllOrders(true)}
            >
              不限期間
            </button>
          </div>
        )}
      </div>}
      {view === 'process' && (
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, margin: '-12px 0 18px', color: '#94a3b8', fontSize: 12 }}>
          <span>
            {includeAllOrders ? '不限日期' : `${dateRange.startDate} 至 ${dateRange.endDate}`}
            {' · '}總覽統計與列表使用相同日期範圍
          </span>
          {procResp?.success && (
            <span>
              共 {processSummary.totalOrders ?? 0} 筆
              {processSummary.totalOrders > processRows.length ? `，目前顯示最新 ${processRows.length} 筆（上限 ${processRowLimit}）` : ''}
            </span>
          )}
        </div>
      )}

      {view === 'receipts' ? <CashReceiptsPanel /> : (
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
                {!validDateRange ? (
                  <tr><td colSpan={6} role="alert" style={{ textAlign: 'center', padding: '4rem', color: '#fbbf24' }}>起始日期不可晚於結束日期。</td></tr>
                ) : processLoading ? (
                  <tr><td colSpan={6} style={{ textAlign: 'center', padding: '4rem' }}>正在優化流程追蹤地圖...</td></tr>
                ) : processError ? (
                  <tr>
                    <td colSpan={6} role="alert" style={{ textAlign: 'center', padding: '3rem', color: '#fca5a5' }}>
                      <div>銷售流程資料載入失敗：{processQueryError instanceof Error ? processQueryError.message : '發生未知錯誤'}</div>
                      <button type="button" className="lux-btn-primary" onClick={() => refetchOrderToCash()} style={{ marginTop: 12 }}>重新載入</button>
                    </td>
                  </tr>
                ) : filteredProcessRows.length === 0 ? (
                  <tr>
                    <td colSpan={6} role="status" style={{ textAlign: 'center', padding: '4rem', color: '#94a3b8' }}>
                      {search
                        ? '目前日期範圍內沒有符合搜尋條件的訂單。'
                        : '目前日期範圍內查無銷售訂單；可調整日期或選擇「不限期間」。'}
                    </td>
                  </tr>
                ) : filteredProcessRows.map((row) => (
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
                           <button className="lux-tab-btn" onClick={() => { setSelectedOrder(so); setOrderItems(so.items || []); setShowOrderForm(true); }}><Edit size={16} /></button>
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
                              {so.items?.map((item, idx) => (
                                <div key={idx} style={{ display: 'grid', gridTemplateColumns: 'minmax(250px, 3fr) 100px 80px 120px 120px', gap: '1.5rem', padding: '1.2rem', borderBottom: '1px solid rgba(255,255,255,0.03)', fontSize: '0.9rem', alignItems: 'center' }}>
                                   <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                      <span style={{ color: '#fff', fontWeight: 600, fontSize: '0.95rem' }}>{item.productName || item.skuNumber}</span>
                                      <span style={{ fontSize: '0.75rem', color: '#64748b', fontFamily: 'var(--font-mono)', letterSpacing: '0.05em' }}>
                                         品號: {item.skuNumber || item.skuId || '---'}
                                      </span>
                                   </div>
                                   <div style={{ textAlign: 'right', fontWeight: 600, color: '#fff', fontFamily: 'var(--font-mono)' }}>{item.quantity?.toLocaleString()}</div>
                                   <div style={{ textAlign: 'center', color: '#94a3b8', fontSize: '0.8rem' }}>
                                     {(uoms?.data || []).find(u => u.uomCode === item.unit)?.uomName || item.unit || '—'}
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
                {filteredQuotes.map((q) => (
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
                           <button className="lux-tab-btn" onClick={() => { setSelectedQuote(q); setQuoteItems(q.items || q.quoteItems || []); setShowQuoteForm(true); }}><Edit size={16} /></button>
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
                              {(q.items || q.quoteItems || []).map((item, idx) => (
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
                {filteredCustomers.map((c) => (
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
      )}

      {/* 銷售訂單表單 */}
      <FormModal
        open={showOrderForm}
        title={selectedOrder ? "編輯銷售訂單 " : "建立銷售訂單"}
        onClose={() => setShowOrderForm(false)}
        initialValues={selectedOrder ? { ...selectedOrder } : undefined}
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
          items_editor: ItemsTable({ items: orderItems, setItems: setOrderItems })
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
            { name: 'customerId', label: '目標客戶 *', type: 'select', required: true, options: customers.map(c => ({ value: c.id, label: c.customerName })), description: '報價編號將於建立時依年月自動產生。' },
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
          quote_items_editor: ItemsTable({ items: quoteItems, setItems: setQuoteItems, isQuote: true })
        }}
        onSubmit={async (values) => {
          const payload = { ...values, validUntil: values.validUntil || null, items: quoteItems };
          if (selectedQuote) await quotesApi.update(selectedQuote.id, payload);
          else {
            const response = await quotesApi.create(payload);
            const quoteNumber = response.data.data?.quoteNumber;
            if (!quoteNumber) throw new Error('報價建立成功，但系統未回傳報價編號。');
            alert(`報價單已建立\n報價編號：${quoteNumber}`);
          }
          queryClient.invalidateQueries({ queryKey: ['quotes'] });
          setShowQuoteForm(false);
        }}
      />

      {/* 客戶主檔表單 */}
      <FormModal
        open={showShipmentForm}
        title={`建立出貨單 ${selectedShipmentOrder?.orderNumber || ''}`}
        onClose={() => setShowShipmentForm(false)}
        width={760}
        fields={shipmentLines.flatMap((line, index) => [
          {
            name: `shipment-${index}-quantity`,
            label: `${line.skuNumber || line.skuId} - ${line.productName || line.skuName || ''}；本次出貨量（剩餘 ${line.remainingQty}）`,
            type: 'number' as const,
            min: 0.01,
            max: line.remainingQty,
            step: 0.01,
            defaultValue: line.remainingQty,
            required: true
          },
          {
            name: `shipment-${index}-warehouseId`,
            label: '出貨倉庫',
            type: 'select' as const,
            options: shippingWarehouses.map((warehouse: WarehouseDto) => ({
              value: warehouse.id,
              label: `${warehouse.warehouseCode} - ${warehouse.warehouseName}`
            })),
            required: true
          },
          {
            name: `shipment-${index}-binLocationId`,
            label: '出貨儲位',
            type: 'select' as const,
            options: shippingBins.map((bin: BinLocationDto) => ({
              value: bin.id,
              label: `${bin.warehouseCode} / ${bin.locationCode}`
            })),
            required: true
          },
          {
            name: `shipment-${index}-lotNumber`,
            label: '批號',
            type: 'text' as const,
            placeholder: '輸入實際出貨批號',
            required: true
          }
        ])}
        onSubmit={async values => {
          const items = shipmentLines.map((line, index) => {
            const quantity = Number(values[`shipment-${index}-quantity`]);
            if (!Number.isFinite(quantity) || quantity <= 0 || quantity > line.remainingQty) {
              throw new Error(`${line.skuNumber || line.skuId} 出貨數量無效或超過未出貨量`);
            }
            const warehouseId = String(values[`shipment-${index}-warehouseId`] || '');
            const binLocationId = String(values[`shipment-${index}-binLocationId`] || '');
            const lotNumber = String(values[`shipment-${index}-lotNumber`] || '').trim();
            if (!warehouseId || !binLocationId || !lotNumber) {
              throw new Error(`${line.skuNumber || line.skuId} 必須指定倉庫、儲位與批號`);
            }
            const binLocation = shippingBins.find(bin => bin.id === binLocationId);
            if (!binLocation || binLocation.warehouseId !== warehouseId) {
              throw new Error(`${line.skuNumber || line.skuId} 所選儲位不屬於指定倉庫`);
            }
            return { skuId: line.skuId, quantity, warehouseId, binLocationId, lotNumber };
          });
          await deliveryOrdersApi.create({
            salesOrderId: selectedShipmentOrder.id,
            deliveryDate: new Date().toISOString(),
            items
          });
          queryClient.invalidateQueries({ queryKey: ['delivery-orders'] });
          setShowShipmentForm(false);
          setSelectedShipmentOrder(null);
          setShipmentLines([]);
        }}
      />

      {/* 客戶主檔表單 */}
      <FormModal
        open={showCustomerForm}
        title={selectedCustomer ? "編輯客戶資料" : "核心客戶建檔"}
        onClose={() => setShowCustomerForm(false)}
        initialValues={selectedCustomer ? { ...selectedCustomer } : undefined}
        width={700}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['基本資料', '聯繫資訊', '財務與信用'],
          getStepFields: (idx) => customerTabs[idx].fields
        }}
        onSubmit={async (values) => {
          const payload = { ...values, creditLimit: Number(values.creditLimit) || 0 };
          if (selectedCustomer) await customersApi.update(selectedCustomer.id, payload);
          else await customersApi.create(payload);
          queryClient.invalidateQueries({ queryKey: ['customers'] });
          setShowCustomerForm(false);
        }}
      />
    </div>
  );
};
