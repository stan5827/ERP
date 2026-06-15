import os
import re

def comprehensive_restoration_v2():
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(dest_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # Restoration of the key blocks with FLEXIBLE argument matching
    blocks = {
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
];""",
        r'const inventoryFields = .*? => \[.*?\];': """const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
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
];""",
        r'const costFields = .*? => \[.*?\];': """const costFields = (uomOptions: any[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'costMethod', label: '成本計價', type: 'select', options: [{value:'standard',label:'標準'},{value:'average',label:'平均'}] },
];""",
        r'const productionFields: FormField\[\] = \[.*?\];': """const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造項目', type: 'checkbox' },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [{value:'manufacture',label:'生產'},{value:'kit',label:'組合'}] },
];""",
        r'const purchFields = .*? => \[.*?\];': """const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購此品項', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];""",
        r'const outsourceFields = .*? => \[.*?\];': """const outsourceFields = (warehouseOptions: any[], supplierOptions: any[]): FormField[] => [
  { name: 'isOutsourced', label: '委外加工', type: 'checkbox' },
  { name: 'outsourceSupplierId', label: '加工廠商', type: 'select', options: supplierOptions },
];""",
        r'const uomFields = .*? => \[.*?\];': """const uomFields = (uomOptions: any[]): FormField[] => [
  { name: 'baseUomId', label: '庫存單位 *', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '換算率', type: 'number', defaultValue: 1 },
];"""
    }

    for p, r in blocks.items():
        content = re.sub(p, r, content, flags=re.DOTALL)

    # SECURE QUOTES: Balance every line
    lines = content.split('\n')
    fixed_lines = []
    for line in lines:
        if line.count("'") % 2 != 0:
            # If it's mangled, we close it
            line = line.strip() + "'"
        fixed_lines.append(line)

    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(fixed_lines))
    print("Full block restoration success.")

comprehensive_restoration_v2()
