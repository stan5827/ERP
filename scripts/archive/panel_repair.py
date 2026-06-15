import os
import re

def panel_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Define clean versions of the panels
    panels = {
        r'function TransactionsPanel.*?return.*?</div>\s+?;\s+?\}': """function TransactionsPanel() {
  const [search, setSearch] = useState('');
  const [filterType, setFilterType] = useState('all');
  const [filterWarehouse, setFilterWarehouse] = useState('all');
  const { data, isLoading } = useQuery({
    queryKey: ['inventory-transactions', filterType, filterWarehouse],
    queryFn: () => inventoryApi.transactions({ 
      type: filterType !== 'all' ? filterType : undefined,
      warehouseId: filterWarehouse !== 'all' ? filterWarehouse : undefined
    })
  });
  const transactions = data?.data?.data || [];

  const typeOptions = [
    { value: 'all', label: '全部類型' },
    { value: 'purchase_receipt', label: '採購收料' },
    { value: 'production_receipt', label: '完工入庫' },
    { value: 'sales_issue', label: '銷售出庫' },
    { value: 'production_issue', label: '領料出庫' },
    { value: 'transfer', label: '調撥' },
    { value: 'adjustment', label: '調整' },
    { value: 'return', label: '退貨' },
  ];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div className="module-filter-bar glass-card no-lift">
        <div className="filter-search">
          <Search size={14} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋單號或品號..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        <select className="form-input" style={{ width: 140 }} value={filterType} onChange={e => setFilterType(e.target.value)}>
          {typeOptions.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
        </select>
        <select className="form-input" style={{ width: 140 }} value={filterWarehouse} onChange={e => setFilterWarehouse(e.target.value)}>
          <option value="all">全部倉庫</option>
        </select>
      </div>

      <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>載入中...</div>
        ) : transactions.length === 0 ? (
          <div style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>無交易紀錄</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>交易單號</th>
                <th>類型</th>
                <th>品號</th>
                <th>品名</th>
                <th>變動量</th>
                <th>倉庫</th>
                <th>儲位</th>
                <th>日期</th>
                <th>執行人</th>
              </tr>
            </thead>
            <tbody>
              {transactions.filter(t => !search || t.skuNumber?.includes(search) || t.trxId?.includes(search)).map(t => (
                <tr key={t.id}>
                  <td style={{ color: 'var(--cobalt-300)', fontFamily: 'var(--font-mono)' }}>{t.trxId}</td>
                  <td>
                    <span className={`badge ${t.type === 'purchase_receipt' || t.type === 'production_receipt' ? 'success' : t.type === 'sales_issue' || t.type === 'production_issue' ? 'warning' : 'info'}`}>
                      {typeOptions.find(opt => opt.value === t.type)?.label || t.type}
                    </span>
                  </td>
                  <td>{t.skuNumber}</td>
                  <td>{t.skuName}</td>
                  <td style={{ fontWeight: 600, color: t.qtyChange > 0 ? 'var(--emerald-400)' : 'var(--crimson-400)' }}>
                    {t.qtyChange > 0 ? '+' : ''}{t.qtyChange}
                  </td>
                  <td>{t.warehouseName || '-'}</td>
                  <td>{t.binLocationCode || '-'}</td>
                  <td>{t.createdAt?.slice(0, 10)}</td>
                  <td>{t.createdBy || 'System'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}""",
        r'function CountPanel.*?return.*?</div>\s+?;\s+?\}': """function CountPanel() {
  const [countMode, setCountMode] = useState<'pending' | 'in_progress' | 'completed'>('pending');

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', gap: 16 }}>
        {[
          { label: '待盤點', value: 0, color: 'var(--amber-400)' },
          { label: '盤點中', value: 0, color: 'var(--cobalt-400)' },
          { label: '已完成', value: 0, color: 'var(--emerald-400)' },
          { label: '待確認', value: 0, color: 'var(--crimson-400)' },
        ].map(s => (
          <div key={s.label} className="glass-card" style={{ flex: 1, padding: 16 }}>
            <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>{s.label}</div>
            <div style={{ fontSize: 28, fontWeight: 700, color: s.color }}>{s.value}</div>
          </div>
        ))}
      </div>

      <div className="module-filter-bar glass-card no-lift" style={{ justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', gap: 8 }}>
          {['pending', 'in_progress', 'completed'].map(m => (
            <button key={m} className={`btn btn-sm ${countMode === m ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setCountMode(m as any)}>
              {m === 'pending' ? '待盤點' : m === 'in_progress' ? '盤點中' : '已完成'}
            </button>
          ))}
        </div>
        <button className="btn btn-primary btn-sm" disabled title="功能開發中">
          <Plus size={13} />建立盤點單
        </button>
      </div>

      <div className="glass-card no-lift" style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>
        盤點功能開發中，請期待後續更新。
      </div>
    </div>
  );
}"""
    }

    # Use a more flexible regex for panels that might be slightly different
    for pattern, repl in panels.items():
        # Clean the pattern of any backslashes that might have been mangled
        content = re.sub(pattern, repl, content, flags=re.DOTALL)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Panel fix complete.")

if __name__ == "__main__":
    panel_fix()
