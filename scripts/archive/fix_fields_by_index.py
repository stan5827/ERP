import os

def fix_warehouse_fields():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Lines are 1-indexed. Line 1514 is index 1513.
    # We want to replace lines 1514 to 1517.
    lines[1513] = "             { name: 'warehouseName', label: '倉庫名稱 *', type: 'text', required: true },\n"
    lines[1514] = "             { name: 'type', label: '類型', type: 'select', options: [{ value: 'normal', label: '一般倉' }, { value: 'wip', label: '在製品倉' }, { value: 'scrap', label: '報廢倉' }] },\n"
    lines[1515] = "             { name: 'address', label: '地址', type: 'text' },\n"
    lines[1516] = "             { name: 'isActive', label: '啟用狀態', type: 'checkbox' }\n"
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Warehouse fields fixed by index.")

fix_warehouse_fields()
