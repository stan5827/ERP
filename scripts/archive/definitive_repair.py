import os
import re

def definitive_repair():
    src_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx.sanitized'
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    with open(src_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # Define ALL blocks to restore. This MUST be 100% correct.
    restorations = {
        # 1. basicInfoFields
        r'const basicInfoFields: FormField\[\] = \[.*?\];': """const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: '例如: M01-STL-001' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '輸入產品名稱' },
  { name: 'specification', label: '規格描述', type: 'text' },
  { name: 'category', label: '產品類別', type: 'text' },
];""",
        # 2. sourceTypeFields
        r'const sourceTypeFields: FormField\[\] = \[.*?\];': """const sourceTypeFields: FormField[] = [
  { name: 'sourceType', label: '來源類型 *', type: 'select', required: true, options: [
    { value: 'make', label: '自製' },
    { value: 'buy', label: '採購' },
    { value: 'make_or_buy', label: '自製+採購' },
    { value: 'outsource', label: '委外加工' },
  ], defaultValue: 'make' },
  { name: 'itemType', label: '物料類型', type: 'select', options: [
    { value: 'semi_finished', label: '半成品' },
    { value: 'finished', label: '成品' },
    { value: 'material', label: '原料' },
  ] },
];""",
        # 3. inventoryFields
        r'const inventoryFields =.*?(\n\];)': """const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
  { name: 'isStocked', label: '是否管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{value:'A',label:'A'},{value:'B',label:'B'},{value:'C',label:'C'}] },
  { name: 'safetyStock', label: '安全庫存量', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];""",
        # 4. salesFields
        r'const salesFields: FormField\[\] = \[.*?\];': """const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number', placeholder: '0' },
  { name: 'basePrice', label: '標準售價', type: 'number', placeholder: '0' },
  { name: 'isSellable', label: '可銷售此品項', type: 'checkbox', defaultValue: true },
];""",
        # 5. costFields
        r'const costFields =.*?(\n\];)': """const costFields = (uomOptions: any[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'costMethod', label: '成本計價', type: 'select', options: [{value:'standard',label:'標準'},{value:'average',label:'平均'}] },
];""",
        # 6. productionFields
        r'const productionFields: FormField\[\] = \[.*?\];': """const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造項目', type: 'checkbox' },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [{value:'manufacture',label:'生產'},{value:'kit',label:'組合'}] },
];""",
        # 7. purchFields
        r'const purchFields =.*?(\n\];)': """const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購此品項', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];""",
        # 8. Panels
        r'function CountPanel\(\) \{.*?\}\n\n/\*': """function CountPanel() {
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
          <Plus size={13} />建立盤點計畫
        </button>
      </div>
    </div>
  );
}

/*""",
        # 9. getStatusBadge
        r'const getStatusBadge = \(status: string\) => \{.*?return map\[status\] \|\| \{ label: status, className: \'status-neutral\', color: \'var\(--slate-500\)\' \};\n  \};':
"""const getStatusBadge = (status: string) => {
    const map: Record<string, { label: string; className: string; color: string }> = {
      pending:    { label: '待處理',  className: 'status-warning', color: 'var(--amber-400)' },
      approved:   { label: '已核准',  className: 'status-active',  color: 'var(--emerald-400)' },
      completed:  { label: '已完成',  className: 'status-info',    color: 'var(--cobalt-400)' },
      cancelled:  { label: '已取消',  className: 'status-neutral', color: 'var(--slate-500)' },
      dispatched: { label: '已出貨',  className: 'status-active',  color: 'var(--emerald-400)' },
    };
    return map[status] || { label: status, className: 'status-neutral', color: 'var(--slate-500)' };
  };"""
    }

    for p, r in restorations.items():
        content = re.sub(p, r, content, flags=re.DOTALL)

    # SECURE QUOTES: Balance every line but DON'T truncate labels
    lines = content.split('\n')
    fixed_lines = []
    for line in lines:
        if line.count("'") % 2 != 0:
            if "label: '" in line:
                line = re.sub(r"label: '[^'\n]*", "label: '資料修復'", line)
            elif "useState('" in line:
                line = "  const [search, setSearch] = useState('');" if "search" in line else line + "'"
            else:
                line = line.strip() + "'"
        fixed_lines.append(line)

    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(fixed_lines))
    print("Definitive repair complete.")

definitive_repair()
