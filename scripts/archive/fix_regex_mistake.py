import os
import re

def fix_regex_mistake():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix my mistake: ({ id, data }): { ... }) -> ({ id, data }: { ... })
    content = content.replace("({ id, data }): { id: string; data: any })", "({ id, data }: { id: string; data: any })")
    content = content.replace("({ warehouseId, binId }): { warehouseId: string; binId: string })", "({ warehouseId, binId }: { warehouseId: string; binId: string })")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Regex mistake fixed.")

fix_regex_mistake()
