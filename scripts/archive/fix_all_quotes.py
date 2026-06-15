import os
import re

def fix_all_quotes():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix quoted parameter lists: '({ id, data' } -> ({ id, data })
    content = re.sub(r"mutationFn: '\(\{ ([^']+)' \}: \{ ([^}]+) \}", r"mutationFn: ({ \1 }): { \2 }", content)
    
    # Fix any remaining quoted types in destructuring
    content = re.sub(r"data: '([^']+)'", r"data: \1", content)
    
    # Fix any remaining alert quotes
    # Actually the alerts seem okay now, but let's check for any others
    
    # Specific fix for the line 1204
    content = content.replace("mutationFn: '({ id, data' }: { id: string; data: 'any' })", "mutationFn: ({ id, data }: { id: string; data: any })")

    # Fix line 1219 range if similar
    content = re.sub(r"mutationFn: '\(\{ warehouseId, binId' \}: \{ ([^}]+) \}", r"mutationFn: ({ warehouseId, binId }): { \1 }", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("All quotes fixed.")

fix_all_quotes()
