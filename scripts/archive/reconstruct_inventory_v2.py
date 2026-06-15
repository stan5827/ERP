import os

def reconstruct_inventory_v2():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Inventory starts at 1526 (index 1525)
    # We'll replace everything from index 1525 to the end
    
    new_component = """export const Inventory: React.FC = () => {
  const queryClient = useQueryClient();
  const [searchSku, setSearchSku] = useState("");
  const [showAdd, setShowAdd] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editSku, setEditSku] = useState<any>(null);
  const [activeTab, setActiveTab] = useState<string>("skus");
  const [showInactive, setShowInactive] = useState(false);
  const [filterItemType, setFilterItemType] = useState("");
  const [filterSourceType, setFilterSourceType] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [warehouses, setWarehouses] = useState<any[]>([]);
  const [suppliers, setSuppliers] = useState<any[]>([]);
  const [uoms, setUoms] = useState<any[]>([]);
  const [modalWarehouseId, setModalWarehouseId] = useState<string | null>(null);
  const [modalBinOptions, setModalBinOptions] = useState<any[]>([]);

  const { data: skusData } = useQuery({
    queryKey: ["skus", currentPage, showInactive, filterItemType, filterSourceType, searchSku],
    queryFn: () => skusApi.list({ 
      pageSize: 10, 
      page: currentPage.toString(), 
      status: showInactive ? "inactive" : "active",
      itemType: filterItemType || undefined,
      sourceType: filterSourceType || undefined,
      search: searchSku || undefined
    }).then(res => res.data)
  });

  const { data: binLocationsData } = useQuery({
    queryKey: ["bin-locations-modal", modalWarehouseId],
    queryFn: () => modalWarehouseId ? binLocationsApi.getByWarehouse(modalWarehouseId).then(res => res.data) : Promise.resolve({ data: [] }),
    enabled: !!modalWarehouseId
  });

  React.useEffect(() => {
    if (binLocationsData?.data) {
      setModalBinOptions(binLocationsData.data.map((bl: any) => ({
        value: bl.id,
        label: bl.locationCode
      })));
    }
  }, [binLocationsData]);

  React.useEffect(() => {
    warehousesApi.list().then(res => {
      setWarehouses((res.data?.data || []).map((w: any) => ({ value: w.id, label: w.warehouseName })));
    });
    uomsApi.list().then(res => {
      setUoms((res.data?.data || []).map((u: any) => ({ value: u.id, label: u.uomName })));
    });
    suppliersApi.list().then(res => {
      setSuppliers((res.data?.data || []).map((s: any) => ({ value: s.id, label: s.supplierName })));
    });
  }, []);

  const skus = Array.isArray(skusData?.data) ? skusData.data : [];

  const tabs = [
    { id: "skus", label: "品碼清單", desc: "SKU" },
    { id: "transactions", label: "異動紀錄", desc: "Trans" },
    { id: "shipping", label: "出貨作業", desc: "Shipping" },
    { id: "count", label: "盤點作業", desc: "Count" },
    { id: "receipts", label: "收料清單", desc: "Receipts" },
    { id: "transfers", label: "調撥管理", desc: "Transfer" },
    { id: "completions", label: "完工入庫", desc: "Completion" },
    { id: "bins", label: "儲位管理", desc: "Bin" },
    { id: "analysis", label: "庫存分析", desc: "ABC/Aging" },
  ] as const;

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

        {activeTab === "receipts" && <div style={{ padding: 20 }}>收料清單組件載入中...</div>}
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
            { label: "基本資料", fields: [...(typeof basicInfoFields !== 'undefined' ? basicInfoFields : []), ...(typeof sourceTypeFields !== 'undefined' ? sourceTypeFields : [])] },
            { label: "採購與成本", fields: typeof costFields === 'function' ? costFields(uoms) : [] },
            { label: "庫存設定", fields: typeof inventoryFields === 'function' ? inventoryFields(warehouses, modalBinOptions) : [] },
            { label: "銷售資料", fields: typeof salesFields !== 'undefined' ? salesFields : [] }
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
            { label: "基本資料", fields: [...(typeof basicInfoFields !== 'undefined' ? basicInfoFields : []), ...(typeof sourceTypeFields !== 'undefined' ? sourceTypeFields : [])] },
            { label: "採購與成本", fields: typeof costFields === 'function' ? costFields(uoms) : [] },
            { label: "庫存設定", fields: typeof inventoryFields === 'function' ? inventoryFields(warehouses, modalBinOptions) : [] },
            { label: "銷售資料", fields: typeof salesFields !== 'undefined' ? salesFields : [] }
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

    # Replace from index 1525 (line 1526) to end
    lines = lines[:1525] + [new_component + "\n"]
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Inventory component reconstructed v2.")

reconstruct_inventory_v2()
