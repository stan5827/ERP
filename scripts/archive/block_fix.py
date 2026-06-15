import os
import re

def block_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Define the blocks to replace with clean data
    # We use very specific start anchors but very generic end anchors
    content = re.sub(r'const inventoryFields =.*?(\n\];)', """const inventoryFields = (warehouseOptions: { value: string; label: string }[], binOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'isStocked', label: '是否管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{ value: 'A', label: 'A' }, { value: 'B', label: 'B' }, { value: 'C', label: 'C' }] },
  { name: 'safetyStock', label: '安全庫存量', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
  { name: 'maxStock', label: '最高庫存量', type: 'number' },
  { name: 'avgDailyConsumption', label: '日均耗用量', type: 'number' },
  { name: 'purchasePriceCeiling', label: '價差容許(%)', type: 'number' },
  { name: 'lotControlled', label: '批號管理', type: 'checkbox' },
  { name: 'serialControlled', label: '序號管理', type: 'checkbox' },
  { name: 'iqcRequired', label: '進料檢驗(IQC)', type: 'checkbox', defaultValue: true },
  { name: 'oqcRequired', label: '出貨檢驗(OQC)', type: 'checkbox' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];""", content, flags=re.DOTALL)

    content = re.sub(r'const salesFields: FormField\[\] = \[.*?(\n\];)', """const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number' },
  { name: 'basePrice', label: '標準售價', type: 'number' },
  { name: 'priceA', label: '售價A', type: 'number' },
  { name: 'priceB', label: '售價B', type: 'number' },
  { name: 'priceC', label: '售價C', type: 'number' },
  { name: 'priceD', label: '售價D', type: 'number' },
  { name: 'priceE', label: '售價E', type: 'number' },
  { name: 'floorPrice', label: '最低底價', type: 'number' },
  { name: 'discountRate', label: '折扣率(%)', type: 'number', defaultValue: 0 },
  { name: 'commissionAccount', label: '佣金科目', type: 'text' },
  { name: 'currency', label: '幣別', type: 'select', options: [{ value: 'TWD', label: 'TWD' }], defaultValue: 'TWD' },
  { name: 'taxCode', label: '稅代碼', type: 'text', defaultValue: 'TX1' },
  { name: 'taxRate', label: '稅率', type: 'text', defaultValue: '5%' },
];""", content, flags=re.DOTALL)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Block fix applied.")

block_fix()
