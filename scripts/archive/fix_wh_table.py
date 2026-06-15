import os
import re

def fix_warehouse_table():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix the ternary in the table
    pattern = r"\{w\.type === 'normal' \? '銝€\?砍€\? : w\.type === 'wip' \? '\?刻ˊ\?€\? : w\.type === 'scrap' \? '撱Ｘ\?\?\? : w\.type \|\| '-'\} "
    # Actually it's easier to just match the start of the line
    
    new_td = "<td>{w.type === 'normal' ? '一般倉' : w.type === 'wip' ? '在製品倉' : w.type === 'scrap' ? '報廢倉' : w.type || '-'}</td>"
    content = re.sub(r"<td>\{w\.type === 'normal' \? '.*?\}</td>", new_td, content)

    # Also fix active status labels
    content = content.replace("{w.isActive ? '?' : '?'}", "{w.isActive ? '啟用' : '停用'}")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Warehouse table fixed.")

fix_warehouse_table()
