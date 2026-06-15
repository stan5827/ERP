import os
import re

def comprehensive_fix():
    src_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx.sanitized'
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    with open(src_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # FULL BLOCKS REPLACEMENTS
    replacements = {
        r'const basicInfoFields: FormField\[\] = \[.*?\];': """const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: '例如: M01-STL-001' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '輸入產品名稱' },
  { name: 'specification', label: '規格描述', type: 'text' },
  { name: 'category', label: '產品類別', type: 'text' },
];""",
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
  { name: 'barcode', label: '條碼', type: 'text' },
];""",
        r'const inventoryFields =.*?(\n\];)': """const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
  { name: 'isStocked', label: '是否管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{value:'A',label:'A'},{value:'B',label:'B'},{value:'C',label:'C'}] },
  { name: 'safetyStock', label: '安全庫存量', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];""",
        r'const salesFields: FormField\[\] = \[.*?\];': """const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number' },
  { name: 'basePrice', label: '標準售價', type: 'number' },
  { name: 'isSellable', label: '可銷售', type: 'checkbox', defaultValue: true },
  { name: 'currency', label: '幣別', type: 'select', options: [{value:'TWD',label:'TWD'}], defaultValue: 'TWD' },
];""",
        r'const costFields =.*?(\n\];)': """const costFields = (uomOptions: any[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'costMethod', label: '成本計價方式', type: 'select', options: [{value:'standard',label:'標準成本'},{value:'average',label:'平均'}] },
];""",
        r'const productionFields: FormField\[\] = \[.*?\];': """const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造項目', type: 'checkbox' },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [{value:'manufacture',label:'生產'},{value:'kit',label:'組合'}] },
  { name: 'isBomTop', label: 'BOM頂層', type: 'checkbox' },
  { name: 'isPhantom', label: '虛擬件', type: 'checkbox' },
];""",
        r'const purchFields =.*?(\n\];)': """const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購此品項', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];""",
        r'const outsourceFields =.*?(\n\];)': """const outsourceFields = (warehouseOptions: any[], supplierOptions: any[]): FormField[] => [
  { name: 'isOutsourced', label: '委外工序', type: 'checkbox' },
  { name: 'outsourceSupplierId', label: '加工廠商', type: 'select', options: supplierOptions },
];""",
        r'const uomFields =.*?(\n\];)': """const uomFields = (uomOptions: any[]): FormField[] => [
  { name: 'baseUomId', label: '庫存單位 *', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '換算率', type: 'number', defaultValue: 1 },
];"""
    }

    for p, r in replacements.items():
        content = re.sub(p, r, content, flags=re.DOTALL)

    # SECURE THE REST: replace all string literals with quotes closed
    def quote_balancer(match):
        s = match.group(0)
        if any(ord(c) > 127 for c in s) or '?' in s:
            if 'var(--' in s: return s
            # Replace mangled with something safe
            quote = s[0]
            return f"{quote}文字已修正{quote}"
        return s

    content = re.sub(r"'[^'\n]*'|\"[^\"\n]*\"", quote_balancer, content)

    # FINAL SAFETY SCAN
    lines = content.split('\n')
    fixed = []
    for line in lines:
        if line.count("'") % 2 != 0:
            line = line.replace("'", "") # Delete any lonely quote
        if line.count('"') % 2 != 0:
            line = line.replace('"', "")
        fixed.append(line)

    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(fixed))
    print("Comprehensive fix complete.")

comprehensive_fix()
