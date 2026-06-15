import os
import re

def fix_all_unclosed_vars():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Pattern: property: 'var(--name) } or property: 'var(--name) }}>
    # Fix to: property: 'var(--name)' } or property: 'var(--name)' }}>
    content = re.sub(r"([a-zA-Z]+):\s*'var\((--[a-zA-Z0-9-]+)\)\s*([\}]+)", r"\1: 'var(\2)' \3", content)
    
    # Also fix some other unclosed quotes in styles
    content = re.sub(r"([a-zA-Z]+):\s*'([^']+)\s*\}\}", r"\1: '\2' }}", content)
    
    # Specific fix for the ternary color thing
    content = re.sub(r"color:\s*'([^']+)'\s*\?\s*'([^']+)'\s*:\s*'([^']+)\s*\}\}", r"color: \1 ? '\2' : '\3' }}", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Unclosed vars fixed.")

fix_all_unclosed_vars()
