import os

def fix_warehouse_add_fields():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Lines 1494 to 1501
    lines[1493] = '          title="新增倉庫"\n'
    lines[1496] = "            { name: 'warehouseCode', label: '倉庫代碼 *', type: 'text', required: true },\n"
    lines[1497] = "            { name: 'warehouseName', label: '倉庫名稱 *', type: 'text', required: true },\n"
    lines[1498] = "            { name: 'type', label: '類型', type: 'select', options: [{ value: 'normal', label: '一般倉' }, { value: 'wip', label: '在製品倉' }, { value: 'scrap', label: '報廢倉' }] },\n"
    lines[1499] = "            { name: 'address', label: '地址', type: 'text' },\n"
    lines[1500] = "            { name: 'isActive', label: '啟用狀態', type: 'checkbox', defaultValue: true }\n"
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Warehouse add fields fixed by index.")

fix_warehouse_add_fields()
