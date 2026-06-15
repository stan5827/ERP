import os
import re

def absolute_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Define ALL field blocks with clean, professional labels
    replacements = {
        r'const basicInfoFields: FormField\[\] = \[.*?\];': """const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: 'M01-STL-001' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '產品名稱' },
  { name: 'specification', label: '規格描述', type: 'text' },
  { name: 'category', label: '類別', type: 'text' },
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
  { name: 'currency', label: '幣別', type: 'select', options: [{value:'TWD',label:'TWD'}], defaultValue: 'TWD' },
];""",
        r'const costFields =.*?(\n\];)': """const costFields = (uomOptions: any[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'costMethod', label: '成本計價', type: 'select', options: [{value:'standard',label:'標準'},{value:'average',label:'平均'}] },
];""",
        r'const productionFields: FormField\[\] = \[.*?\];': """const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造', type: 'checkbox' },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [{value:'manufacture',label:'生產'},{value:'kit',label:'組合'}] },
];""",
        r'const purchFields =.*?(\n\];)': """const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購此品項', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];""",
        r'const outsourceFields =.*?(\n\];)': """const outsourceFields = (warehouseOptions: any[], supplierOptions: any[]): FormField[] => [
  { name: 'isOutsourced', label: '外協加工', type: 'checkbox' },
  { name: 'outsourceSupplierId', label: '加工商', type: 'select', options: supplierOptions },
];""",
        r'const uomFields =.*?(\n\];)': """const uomFields = (uomOptions: any[]): FormField[] => [
  { name: 'baseUomId', label: '庫存單位 *', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '採->庫換算率', type: 'number', defaultValue: 1 },
];"""
    }

    # Replace all blocks
    for p, r in replacements.items():
        content = re.sub(p, r, content, flags=re.DOTALL)

    # Clean up any duplicated single quotes that might have been created
    content = content.replace("''", "'")
    # Fix the common "mangled" labels in panels once more
    content = content.replace("'文字已修復'", "'資料已復原'")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Absolute fix complete.")

absolute_fix()
