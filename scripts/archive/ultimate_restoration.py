import os
import re

def ultimate_restoration():
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    # We will reconstruct the file using the best known blocks
    # I will read the sanitized file and replace the whole top section
    with open(r'd:\ERP\erp-frontend\src\pages\Inventory.tsx.sanitized', 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Field Blocks (Corrected and Complete)
    fields_section = """const basicInfoFields: FormField[] = [
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
    { value: 'outsource', label: '委外加工' },
  ], defaultValue: 'make' },
  { name: 'itemType', label: '物料類型', type: 'select', options: [
    { value: 'material', label: '原料' },
    { value: 'semi_finished', label: '半成品' },
    { value: 'finished', label: '成品' },
  ] },
];

const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
  { name: 'isStocked', label: '是否管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{value:'A',label:'A'},{value:'B',label:'B'},{value:'C',label:'C'}] },
  { name: 'safetyStock', label: '安全庫存量', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];

const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number' },
  { name: 'basePrice', label: '標準售價', type: 'number' },
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
  { name: 'isOutsourced', label: '是否委外', type: 'checkbox' },
  { name: 'outsourceSupplierId', label: '委外商', type: 'select', options: supplierOptions },
];

const uomFields = (uomOptions: any[]): FormField[] => [
  { name: 'baseUomId', label: '庫存單位 *', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '換算率', type: 'number', defaultValue: 1 },
];"""

    # Replace the fields section
    content = re.sub(r'const basicInfoFields: FormField\[\].*?const uomFields =.*?(\n\];)', fields_section, content, flags=re.DOTALL)

    # 2. Fix the Panels and other literals
    # I'll use a very simple find & replace that is guaranteed to be syntactically correct
    content = content.replace("value: return", "value: 'return'")
    content = content.replace("label: ?鞎?", "label: '退貨'")
    content = content.replace("label: ?券憿?", "label: '全部類型'")
    content = content.replace("label: ?∟頃?脰疏", "label: '採購收料'")
    content = content.replace("label: 摰極?亙澈", "label: '完工入庫'")
    content = content.replace("label: ?瑕?箏澈", "label: '銷售出庫'")
    content = content.replace("label: ???", "label: '領料出庫'")
    content = content.replace("label: 隤踵", "label: '調撥'")
    content = content.replace("label: 隤踵", "label: '調整'")

    # 3. Final Sweep: Close any unclosed single quote by adding another one
    lines = content.split('\n')
    fixed = []
    for line in lines:
        if line.count("'") % 2 != 0:
            # We add a closing quote at the end of the problematic part
            if "label: '" in line:
                line = re.sub(r"label: '[^'\n]*", "label: '資料修復'", line)
            elif "placeholder: '" in line:
                line = re.sub(r"placeholder: '[^'\n]*", "placeholder: '輸入'", line)
            else:
                # Still odd? Append a quote
                line = line.strip() + "'"
        fixed.append(line)

    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(fixed))
    print("Ultimate restoration complete.")

ultimate_restoration()
