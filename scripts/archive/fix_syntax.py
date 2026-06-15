import os
import re

def fix_syntax_errors():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()

    new_lines = []
    for line in lines:
        # Fix the specific line 157 map
        if "const map: any = { 'completed':" in line:
            line = "    const map: any = { 'completed': '已完成', 'pending': '待處理', 'cancelled': '已取消' };\n"
        
        # Fix other common maps
        line = line.replace("'已???,", "'已完成',")
        line = line.replace("'待???,", "'待處理',")
        line = line.replace("'已???,", "'已完成',")
        
        # Fix the '?' unclosed quotes
        # If a line has an odd number of single quotes, it might be broken
        # But that's too complex. Let's just fix the known ones.
        
        new_lines.append(line)

    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Syntax fix applied.")

fix_syntax_errors()
