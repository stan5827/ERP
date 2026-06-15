import os
import re

def fix_corruption_v3():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Broad regex to unwrap any single-quoted variable lists in destructuring
    content = re.sub(r"\{ data: '([^']+)' \}", r"{ data: \1 }", content)
    
    # Also handle cases with different spacing
    content = re.sub(r"\{data: '([^']+)'\}", r"{ data: \1 }", content)
    
    # Specific fix for the MapSkuToFormData corruption
    # formData: 'Record<string, unknown> = { ...sku' };
    content = re.sub(r"formData: 'Record<string, unknown> = \{ \.\.\.sku' \};", r"formData: Record<string, unknown> = { ...sku };", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Corruption V3 fixed.")

fix_corruption_v3()
