import os
import re

def final_grand_restoration():
    # We use the Sanitized file as the PURE baseline
    src_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx.sanitized'
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    with open(src_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Complete Fields Reconstruction
    fields_code = """const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: 'M01-001' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '產品名稱' },
  { name: 'specification', label: '規格描述', type: 'text' },
  { name: 'category', label: '類別', type: 'text' },
];

const sourceTypeFields: FormField[] = [
  { name: 'sourceType', label: '來源類型 *', type: 'select', required: true, options: [
    { value: 'make', label: '自製' },
    { value: 'buy', label: '採購' },
    { value: 'make_or_buy', label: '自製+採購' },
    { value: 'outsource', label: '委外' },
  ], defaultValue: 'make' },
  { name: 'itemType', label: '物料類型', type: 'select', options: [
    { value: 'material', label: '原料' },
    { value: 'semi_finished', label: '半成品' },
    { value: 'finished', label: '成品' },
  ] },
];

const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
  { name: 'isStocked', label: '管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{value:'A',label:'A'},{value:'B',label:'B'},{value:'C',label:'C'}] },
  { name: 'safetyStock', label: '安全庫存', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];

const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number' },
  { name: 'isSellable', label: '可銷售', type: 'checkbox', defaultValue: true },
];

const costFields = (uomOptions: any[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'costMethod', label: '成本計價', type: 'select', options: [{value:'standard',label:'標準'},{value:'average',label:'平均'}] },
];

const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造項目', type: 'checkbox' },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [{value:'manufacture',label:'生產'},{value:'kit',label:'組合'}] },
];

const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購此品項', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];

const outsourceFields = (warehouseOptions: any[], supplierOptions: any[]): FormField[] => [
  { name: 'isOutsourced', label: '委外加工', type: 'checkbox' },
  { name: 'outsourceSupplierId', label: '加工商', type: 'select', options: supplierOptions },
];

const uomFields = (uomOptions: any[]): FormField[] => [
  { name: 'baseUomId', label: '庫存單位 *', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '換算率', type: 'number', defaultValue: 1 },
];"""

    content = re.sub(r'const basicInfoFields: FormField\[\].*?const uomFields =.*?(\n\];)', fields_code, content, flags=re.DOTALL)

    # 2. Panels Restoration
    content = re.sub(r'function CountPanel\(\) \{.*?\}\n\n/\*', 
"""function CountPanel() {
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
              {m === 'pending' ? '待處理' : m === 'in_progress' ? '盤點中' : '已完成'}
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

/*""", content, flags=re.DOTALL)

    # 3. Status Badge Mapper Restoration
    content = re.sub(r'const getStatusBadge = \(status: string\) => \{.*?return map\[status\] \|\| \{ label: status, className: \'status-neutral\', color: \'var\(--slate-500\)\' \};\n  \};',
"""const getStatusBadge = (status: string) => {
    const map: Record<string, { label: string; className: string; color: string }> = {
      pending:    { label: '待處理',  className: 'status-warning', color: 'var(--amber-400)' },
      approved:   { label: '已核准',  className: 'status-active',  color: 'var(--emerald-400)' },
      completed:  { label: '已完成',  className: 'status-info',    color: 'var(--cobalt-400)' },
      cancelled:  { label: '已取消',  className: 'status-neutral', color: 'var(--slate-500)' },
      dispatched: { label: '已出貨',  className: 'status-active',  color: 'var(--emerald-400)' },
      picking:    { label: '揀貨中',  className: 'status-info',    color: 'var(--cobalt-300)' },
      packing:    { label: '包裝中',  className: 'status-info',    color: 'var(--indigo-300)' },
    };
    return map[status] || { label: status, className: 'status-neutral', color: 'var(--slate-500)' };
  };""", content, flags=re.DOTALL)

    # 4. Final Cleanup: Fix any broken Chinese characters or remaining single quotes that seem out of place
    # Since we use the Sanitized file as base, we mostly just need to ensure the overall syntax is correct.
    
    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Grand restoration complete.")

final_grand_restoration()
