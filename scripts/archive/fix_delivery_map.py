import os

def fix_delivery_status_map():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Lines 972 to 977
    lines[971] = "      pending: '待處理',\n"
    lines[972] = "      picking: '揀貨中',\n"
    lines[973] = "      packing: '包裝中',\n"
    lines[974] = "      dispatched: '已發貨',\n"
    lines[975] = "      signed: '已簽收',\n"
    lines[976] = "      cancelled: '已取消'\n"
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Delivery status map fixed by index.")

fix_delivery_status_map()
