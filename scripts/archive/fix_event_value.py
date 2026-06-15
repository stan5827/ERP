import os
import re

def fix_event_value():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix e.target.value being quoted
    content = content.replace("'e.target.value'", "e.target.value")
    content = content.replace('"e.target.value"', "e.target.value")
    
    # Also fix searchSku: 'e.target.value'
    content = re.sub(r": 'e\.target\.value'", r": e.target.value", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Event values fixed.")

fix_event_value()
