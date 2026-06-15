import os
import re

def fix_map_syntax():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Change implicit return to explicit return in skus.map
    old_start = r'\{skus\.map\(\(sku: any\) => \('
    new_start = '{skus.map((sku: any) => { return ('
    content = re.sub(old_start, new_start, content)

    # Change ))\} to ); \})\}
    # Actually we need to be careful with the number of parens
    # Old: ))\} 
    # New: ); })}
    content = content.replace("))}", "); })}")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Map syntax fixed.")

fix_map_syntax()
