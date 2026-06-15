import os
import re

def final_block_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    replacements = {
        r'const costFields =.*?(\n\];)': """const costFields = (uomOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number' },
  { name: 'standardCost', label: '標準成本', type: 'number' },
  { name: 'latestPurchaseCost', label: '最新進價', type: 'number' },
  { name: 'costMethod', label: '成本計價方式', type: 'select', options: [
    { value: 'standard', label: '標準成本' },
    { value: 'average', label: '移動平均' }
  ]},
  { name: 'costUomId', label: '成本單位', type: 'select', options: uomOptions },
];""",
        r'const productionFields: FormField\[\] = \[.*?(\n\];)': """const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造項目', type: 'checkbox' },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [{ value: 'manufacture', label: '正式' }, { value: 'kit', label: '組合' }] },
  { name: 'isBomTop', label: 'BOM頂層', type: 'checkbox' },
  { name: 'isBomComponent', label: 'BOM子件', type: 'checkbox' },
  { name: 'isPhantom', label: '虛擬件', type: 'checkbox' },
  { name: 'isMto', label: '接單生產', type: 'checkbox' },
  { name: 'isMts', label: '計畫生產', type: 'checkbox', defaultValue: true },
  { name: 'isCustom', label: '特殊客訂', type: 'checkbox' },
  { name: 'canSubstitute', label: '可替代', type: 'checkbox' },
  { name: 'isAlternative', label: '備選件', type: 'checkbox' },
  { name: 'substituteSkuId', label: '替代品品號', type: 'text' },
  { name: 'routingId', label: '製程代碼', type: 'text' },
];""",
        r'const purchFields =.*?(\n\];)': """const purchFields = (supplierOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購', type: 'checkbox', defaultValue: true },
  { name: 'isSellable', label: '可銷售', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
  { name: 'supplierItemCode', label: '供方品號', type: 'text' },
  { name: 'leadTimeDays', label: '採購前置天數', type: 'number' },
];""",
        r'const outsourceFields =.*?(\n\];)': """const outsourceFields = (warehouseOptions: { value: string; label: string }[], supplierOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'isOutsourced', label: '外協加工', type: 'checkbox' },
  { name: 'outsourceSupplierId', label: '加工廠商', type: 'select', options: supplierOptions },
  { name: 'outsourceWarehouseId', label: '委外倉庫', type: 'select', options: warehouseOptions },
];""",
        r'const uomFields =.*?(\n\];)': """const uomFields = (uomOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'baseUomId', label: '基本單位', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '換算率(採->庫)', type: 'number', defaultValue: 1 },
] ;"""
    }

    for pattern, repl in replacements.items():
        content = re.sub(pattern, repl, content, flags=re.DOTALL)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Final blocks fixed.")

final_block_fix()
