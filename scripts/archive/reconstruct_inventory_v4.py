import os

def reconstruct_full_file_v4():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    header = """import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Package, Search, Plus, Filter, ArrowRightLeft, 
  ChevronRight, MoreHorizontal, Edit2, Trash2, 
  Download, FileText, CheckCircle2, AlertCircle,
  TrendingUp, BarChart2, PieChart, Clock, ArrowUpRight, ArrowDownLeft,
  Warehouse, MapPin, Layers, Settings, RefreshCw
} from 'lucide-react';
import { 
  skusApi, 
  inventoryTransactionsApi, 
  inventoryTransfersApi, 
  completionOrdersApi, 
  deliveryOrdersApi,
  warehousesApi,
  binLocationsApi,
  uomsApi,
  suppliersApi
} from '../api/api';
import { FormModal, FormField } from '../components/FormModal';
import { AnalysisPanel } from './AnalysisPanel';

// Mock fields for FormModal
const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品碼 *', type: 'text', required: true, placeholder: '例: RM-001' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '例: 原材料A', description: '品項名稱' },
  { name: 'specification', label: '規格', type: 'text', placeholder: '例: 100x100mm' },
  { name: 'itemType', label: '類型', type: 'select', options: [
    { value: 'raw', label: '原材料' },
    { value: 'semi', label: '半成品' },
    { value: 'finished', label: '成品' }
  ], required: true }
];

const sourceTypeFields: FormField[] = [
  { name: 'sourceType', label: '來源類型', type: 'select', options: [
    { value: 'purchase', label: '外購' },
    { value: 'make', label: '自製' },
    { value: 'subcontract', label: '委外' }
  ], required: true }
];

const costFields = (uoms: any[]): FormField[] => [
  { name: 'uomId', label: '庫存單位', type: 'select', options: uoms },
  { name: 'costUomId', label: '成本單位', type: 'select', options: uoms }
];

const inventoryFields = (warehouses: any[], bins: any[]): FormField[] => [
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouses },
  { name: 'defaultBinId', label: '預設儲位', type: 'select', options: bins }
];

const salesFields: FormField[] = [
  { name: 'salesPrice', label: '預設售價', type: 'number' }
];
"""

    transactions_panel = """
const TransactionsPanel: React.FC = () => {
  const [search, setSearch] = useState("");
  const { data: transData } = useQuery({
    queryKey: ["inventory-transactions", search],
    queryFn: () => inventoryTransactionsApi.list({ search: search || undefined }).then(res => res.data)
  });
  const transactions = Array.isArray(transData?.data) ? transData.data : [];

  return (
    <div className="glass-card no-lift">
      <div style={{ padding: 16, borderBottom: "1px solid var(--glass-border)", display: "flex", gap: 12 }}>
        <div className="filter-search" style={{ flex: 1 }}>
          <Search size={14} style={{ color: "var(--slate-500)" }} />
          <input className="search-input" placeholder="搜尋交易單號、品碼..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>
      <table className="data-table">
        <thead>
          <tr>
            <th>日期</th>
            <th>類型</th>
            <th>品碼 / 品名</th>
            <th>數量</th>
            <th>倉庫</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((t: any) => (
            <tr key={t.id}>
              <td>{new Date(t.createdAt).toLocaleDateString()}</td>
              <td>
                <span className={`status-badge ${t.type === "in" ? "status-active" : "status-neutral"}`}>
                  {t.type === "in" ? "入庫" : "出庫"}
                </span>
              </td>
              <td>{t.skuNumber}</td>
              <td className="mono" style={{ color: t.quantity > 0 ? "var(--emerald-400)" : "var(--crimson-400)" }}>
                {t.quantity > 0 ? "+" : ""}{t.quantity}
              </td>
              <td>{t.warehouseName}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
"""

    other_panels = """
const ShippingPanel: React.FC = () => <div className="glass-card p-4">Shipping Management Module</div>;
const CountPanel: React.FC = () => <div className="glass-card p-4">Inventory Counting Module</div>;
const TransferPanel: React.FC = () => <div className="glass-card p-4">Warehouse Transfer Module</div>;
const CompletionPanel: React.FC = () => <div className="glass-card p-4">Production Completion Module</div>;

const BinLocationsPanel: React.FC = () => {
  const [showAddBin, setShowAddBin] = useState(false);
  const [showAddWh, setShowAddWh] = useState(false);
  const [editingWarehouse, setEditingWarehouse] = useState<any>(null);
  const [showInactiveWh, setShowInactiveWh] = useState(false);
  const [selectedWh, setSelectedWh] = useState<string>("");
  const [search, setSearch] = useState("");
  const queryClient = useQueryClient();

  const { data: warehouseData } = useQuery({
    queryKey: ["warehouses", showInactiveWh],
    queryFn: () => warehousesApi.list().then(res => res.data)
  });

  const { data: binData, refetch } = useQuery({
    queryKey: ["bin-locations", selectedWh],
    queryFn: () => binLocationsApi.list(selectedWh || undefined).then(res => res.data)
  });

  const warehouses = Array.isArray(warehouseData?.data) ? warehouseData.data : [];
  const bins = Array.isArray(binData?.data) ? binData.data : [];

  const filteredBins = bins.filter(b => 
    b.locationCode.toLowerCase().includes(search.toLowerCase())
  );

  const createBin = useMutation({
    mutationFn: (data: any) => binLocationsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bin-locations"] });
      setShowAddBin(false);
      refetch();
    }
  });

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
        <div className="filter-search" style={{ minWidth: 200 }}>
          <Search size={13} style={{ color: "var(--slate-500)" }} />
          <input className="search-input" placeholder="搜尋儲位..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        <select className="form-input" style={{ width: 180 }} value={selectedWh} onChange={e => setSelectedWh(e.target.value)}>
          <option value="">所有倉庫</option>
          {warehouses.map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}</option>)}
        </select>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAddBin(true)}><Plus size={13} /> 新增儲位</button>
      </div>

      <div className="glass-card no-lift">
        <table className="data-table">
          <thead>
            <tr><th>倉庫</th><th>儲位代碼</th><th>區域</th><th>通道</th><th>貨架</th><th>層</th><th>位置</th><th>狀態</th></tr>
          </thead>
          <tbody>
            {filteredBins.map((bin: any) => (
              <tr key={bin.id}>
                <td>{bin.warehouseName}</td>
                <td className="mono" style={{ color: "var(--cobalt-300)" }}>{bin.locationCode}</td>
                <td>{bin.zone || "-"}</td>
                <td>{bin.aisle || "-"}</td>
                <td>{bin.rack || "-"}</td>
                <td>{bin.level || "-"}</td>
                <td>{bin.position || "-"}</td>
                <td><span className={`status-badge ${bin.isActive ? "status-active" : "status-neutral"}`}>{bin.isActive ? "啟用" : "停用"}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showAddBin && (
        <FormModal 
          open={showAddBin} 
          title="新增儲位"
          fields={[
            { name: "warehouseId", label: "倉庫 *", type: "select", options: warehouses.map(w => ({ value: w.id, label: w.warehouseName })), required: true },
            { name: "locationCode", label: "儲位代碼 *", type: "text", required: true },
            { name: "zone", label: "區域", type: "text" },
            { name: "aisle", label: "通道", type: "text" },
            { name: "rack", label: "貨架", type: "text" },
            { name: "level", label: "層", type: "text" },
            { name: "position", label: "位置", type: "text" }
          ]}
          onClose={() => setShowAddBin(false)}
          onSubmit={async (data) => {
            await createBin.mutateAsync(data);
          }}
        />
      )}
    </div>
  );
};
"""

    inventory_component = """
export const Inventory: React.FC = () => {
  const queryClient = useQueryClient();
  const [searchSku, setSearchSku] = useState("");
  const [showAdd, setShowAdd] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editSku, setEditSku] = useState<any>(null);
  const [activeTab, setActiveTab] = useState<string>("skus");
  const [filterItemType, setFilterItemType] = useState("");
  const [warehouses, setWarehouses] = useState<any[]>([]);
  const [uoms, setUoms] = useState<any[]>([]);
  const [modalWarehouseId, setModalWarehouseId] = useState<string | null>(null);
  const [modalBinOptions, setModalBinOptions] = useState<any[]>([]);

  const { data: skusData } = useQuery({
    queryKey: ["skus", filterItemType, searchSku],
    queryFn: () => skusApi.list({ 
      pageSize: 50, 
      itemType: filterItemType || undefined,
      search: searchSku || undefined
    }).then(res => res.data)
  });

  const { data: binLocationsData } = useQuery({
    queryKey: ["bin-locations-modal", modalWarehouseId],
    queryFn: () => modalWarehouseId ? binLocationsApi.getByWarehouse(modalWarehouseId).then(res => res.data) : Promise.resolve({ data: [] }),
    enabled: !!modalWarehouseId
  });

  useEffect(() => {
    if (binLocationsData?.data) {
      setModalBinOptions(binLocationsData.data.map((bl: any) => ({
        value: bl.id,
        label: bl.locationCode
      })));
    }
  }, [binLocationsData]);

  useEffect(() => {
    warehousesApi.list().then(res => {
      setWarehouses((res.data?.data || []).map((w: any) => ({ value: w.id, label: w.warehouseName })));
    });
    uomsApi.list().then(res => {
      setUoms((res.data?.data || []).map((u: any) => ({ value: u.id, label: u.uomName })));
    });
  }, []);

  const skus = Array.isArray(skusData?.data) ? skusData.data : [];

  const tabs = [
    { id: "skus", label: "品碼清單", desc: "SKU" },
    { id: "transactions", label: "異動紀錄", desc: "Trans" },
    { id: "shipping", label: "出貨作業", desc: "Shipping" },
    { id: "count", label: "盤點作業", desc: "Count" },
    { id: "transfers", label: "調撥管理", desc: "Transfer" },
    { id: "completions", label: "完工入庫", desc: "Completion" },
    { id: "bins", label: "儲位管理", desc: "Bin" },
    { id: "analysis", label: "庫存分析", desc: "Analysis" },
  ];

  return (
    <div className="module-page">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">庫存管理中心</h2>
          <div className="module-page-desc">產品品碼、出入庫、調撥與盤點作業中心</div>
        </div>
        <button className="btn btn-primary" onClick={() => { setModalWarehouseId(null); setShowAdd(true); }}>
          <Plus size={16} /> 新增品碼
        </button>
      </div>

      <div className="inv-tab-bar">
        {tabs.map(tab => (
          <button 
            key={tab.id}
            className={`inv-tab-btn ${activeTab === tab.id ? "active" : ""}`}
            onClick={() => setActiveTab(tab.id)}
          >
            <span className="inv-tab-label">{tab.label}</span>
            <span className="inv-tab-desc">{tab.desc}</span>
          </button>
        ))}
      </div>

      <div style={{ marginTop: 16 }}>
        {activeTab === "skus" && (
          <div className="glass-card no-lift">
            <div style={{ padding: "12px 16px", borderBottom: "1px solid var(--glass-border)", display: "flex", gap: 12, alignItems: "center" }}>
              <div className="filter-search" style={{ flex: 1 }}>
                <Search size={14} style={{ color: "var(--slate-500)" }} />
                <input 
                  className="search-input" 
                  placeholder="搜尋品名、品碼或規格..." 
                  value={searchSku}
                  onChange={(e) => setSearchSku(e.target.value)}
                />
              </div>
              <select className="select-input" value={filterItemType} onChange={(e) => setFilterItemType(e.target.value)}>
                <option value="">所有類型</option>
                <option value="raw">原材料</option>
                <option value="finished">成品</option>
                <option value="semi">半成品</option>
              </select>
            </div>
            <table className="data-table">
              <thead>
                <tr>
                  <th>品碼 / 品名</th>
                  <th>類型</th>
                  <th>規格</th>
                  <th>在手數量</th>
                  <th>單位</th>
                  <th>預設倉庫</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {skus.map((sku: any) => (
                  <tr key={sku.id}>
                    <td>
                      <div style={{ fontWeight: 700, color: "var(--slate-100)" }}>{sku.skuNumber}</div>
                      <div style={{ fontSize: 11, color: "var(--slate-500)" }}>{sku.skuName}</div>
                    </td>
                    <td>{sku.itemType}</td>
                    <td>{sku.specification || "-"}</td>
                    <td className="mono" style={{ fontWeight: 700, color: "var(--cobalt-400)" }}>{sku.onHandQuantity}</td>
                    <td>{sku.uomName}</td>
                    <td>{sku.defaultWarehouseName || "-"}</td>
                    <td>
                      <button className="icon-action-btn" onClick={() => { 
                        setModalWarehouseId(sku.defaultWarehouseId);
                        setEditSku(sku); 
                        setShowEdit(true); 
                      }}><Edit2 size={13} /></button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {activeTab === "transactions" && <TransactionsPanel />}
        {activeTab === "shipping" && <ShippingPanel />}
        {activeTab === "count" && <CountPanel />}
        {activeTab === "transfers" && <TransferPanel />}
        {activeTab === "completions" && <CompletionPanel />}
        {activeTab === "bins" && <BinLocationsPanel />}
        {activeTab === "analysis" && <AnalysisPanel />}
      </div>

      {showAdd && (
        <FormModal 
          open={showAdd} 
          title="新增品碼 (SKU)"
          tabs={[
            { label: "基本資料", fields: [...basicInfoFields, ...sourceTypeFields] },
            { label: "庫存設定", fields: inventoryFields(warehouses, modalBinOptions) }
          ]}
          onClose={() => setShowAdd(false)}
          onChangeData={(values) => {
            if (values.defaultWarehouseId !== modalWarehouseId) {
              setModalWarehouseId(values.defaultWarehouseId as string);
            }
          }}
          onSubmit={async (data) => {
            await skusApi.create(data);
            queryClient.invalidateQueries({ queryKey: ["skus"] });
            setShowAdd(false);
          }}
        />
      )}

      {showEdit && editSku && (
        <FormModal 
          open={showEdit} 
          title={`編輯品碼: ${editSku.skuNumber}`}
          initialValues={editSku}
          tabs={[
            { label: "基本資料", fields: [...basicInfoFields, ...sourceTypeFields] },
            { label: "庫存設定", fields: inventoryFields(warehouses, modalBinOptions) }
          ]}
          onClose={() => { setShowEdit(false); setEditSku(null); }}
          onChangeData={(values) => {
            if (values.defaultWarehouseId !== modalWarehouseId) {
              setModalWarehouseId(values.defaultWarehouseId as string);
            }
          }}
          onSubmit={async (data) => {
            await skusApi.update(editSku.id as string, data);
            queryClient.invalidateQueries({ queryKey: ["skus"] });
            setShowEdit(false);
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
};"""

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(header)
        f.write(transactions_panel)
        f.write(other_panels)
        f.write(inventory_component)
    print("Full file reconstructed v4 fixed.")

reconstruct_full_file_v4()
