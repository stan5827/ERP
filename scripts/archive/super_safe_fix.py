import os
import re

def super_safe_fix():
    src_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx.sanitized'
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    with open(src_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # Field Blocks
    replacements = {
        r'const basicInfoFields: FormField\[\] = \[.*?\];': """const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: 'M01-STL-001' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '產品名稱' },
  { name: 'specification', label: '規格描述', type: 'text' },
  { name: 'category', label: '產品類別', type: 'text' },
];""",
        r'const sourceTypeFields: FormField\[\] = \[.*?\];': """const sourceTypeFields: FormField[] = [
  { name: 'sourceType', label: '來源類型 *', type: 'select', required: true, options: [
    { value: 'make', label: '自製' },
    { value: 'buy', label: '採購' },
    { value: 'make_or_buy', label: '自製+採購' },
    { value: 'outsource', label: '委外' },
  ], defaultValue: 'make' },
  { name: 'itemType', label: '物料類型', type: 'select', options: [
    { value: 'finished', label: '成品' },
    { value: 'semi_finished', label: '半成品' },
    { value: 'material', label: '原料' },
  ] },
];""",
        r'const inventoryFields =.*?(\n\];)': """const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
  { name: 'isStocked', label: '管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{value:'A',label:'A'},{value:'B',label:'B'},{value:'C',label:'C'}] },
  { name: 'safetyStock', label: '安全庫存', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];""",
        r'const salesFields: FormField\[\] = \[.*?\];': """const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number' },
  { name: 'basePrice', label: '標準售價', type: 'number' },
  { name: 'isSellable', label: '可銷售', type: 'checkbox', defaultValue: true },
];""",
        r'const costFields =.*?(\n\];)': """const costFields = (uomOptions: any[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'costMethod', label: '成本計價', type: 'select', options: [{value:'standard',label:'標準'},{value:'average',label:'平均'}] },
];""",
        r'const purchFields =.*?(\n\];)': """const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購此品項', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];"""
    }

    for p, r in replacements.items():
        content = re.sub(p, r, content, flags=re.DOTALL)

    # Specific labels in panels (Transactions, Counts, etc.)
    content = content.replace("label: '敺暺?", "label: '待盤點'")
    content = content.replace("label: '???", "label: '盤點中'")
    content = content.replace("label: '已???", "label: '已完成'")

    # Final sweep to catch any single unclosed quote on a line
    lines = content.split('\n')
    fixed_lines = []
    for line in lines:
        if line.count("'") % 2 != 0:
            if "label: '" in line:
                line = re.sub(r"label: '[^']*", "label: '修復標籤'", line)
            elif "placeholder: '" in line:
                line = re.sub(r"placeholder: '[^']*", "placeholder: '輸入'", line)
            else:
                # Add a closing quote if missing
                line = line.strip() + "'"
        fixed_lines.append(line)

    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(fixed_lines))
    print("Super safe fix complete.")

super_safe_fix()
