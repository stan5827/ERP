import os

def fix_line_157():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Line 157 is index 156
    target_index = 156
    if "const map" in lines[target_index]:
        lines[target_index] = "    const map: any = { 'completed': '已完成', 'pending': '待處理', 'cancelled': '已取消' };\n"
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Line 157 fixed.")

fix_line_157()
