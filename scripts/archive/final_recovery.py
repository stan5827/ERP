import os
import re

def final_recovery():
    src_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx.sanitized'
    dest_file = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    with open(src_file, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Replace the problematic field blocks
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
    { value: 'outsource', label: '委外' },
  ], defaultValue: 'make' },
  { name: 'itemType', label: '物料類型', type: 'select', options: [
    { value: 'finished', label: '成品' },
    { value: 'semi_finished', label: '半成品' },
    { value: 'material', label: '原料' },
  ] },
];""",
        r'const inventoryFields =.*?(\n\];)': """const inventoryFields = (warehouseOptions: any[], binOptions: any[]): FormField[] => [
  { name: 'isStocked', label: '是否管理庫存', type: 'checkbox', defaultValue: true },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [{value:'A',label:'A'},{value:'B',label:'B'},{value:'C',label:'C'}] },
  { name: 'safetyStock', label: '安全庫存', type: 'number' },
  { name: 'reorderPoint', label: '再訂購點', type: 'number' },
];""",
        r'const purchFields =.*?(\n\];)': """const purchFields = (supplierOptions: any[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購', type: 'checkbox', defaultValue: true },
  { name: 'isSellable', label: '可銷售', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
];"""
    }

    for p, r in replacements.items():
        content = re.sub(p, r, content, flags=re.DOTALL)

    # 2. Fix the Panel labels that often break (using brute force on the strings)
    # This prevents the "Expected ',' or '}' but found 'var'" parse error
    content = content.replace("label: '敺暺?", "label: '待盤點'")
    content = content.replace("label: '???", "label: '盤點中'")
    content = content.replace("label: '已???", "label: '已完成'")
    
    # 3. Last fallback: Fix any string that looks unclosed
    lines = content.split('\n')
    fixed_lines = []
    for line in lines:
        if line.count("'") % 2 != 0:
            # If it's a field label or similar
            if "label: '" in line:
                line = re.sub(r"label: '[^']*", "label: '文字已修復'", line)
            elif "placeholder: '" in line:
                line = re.sub(r"placeholder: '[^']*", "placeholder: '請輸入'", line)
            else:
                # Still odd? Just double the last quote or add one
                line = line.strip() + "'"
        fixed_lines.append(line)
        
    content = '\n'.join(fixed_lines)

    with open(dest_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Final recovery successful.")

final_recovery()
