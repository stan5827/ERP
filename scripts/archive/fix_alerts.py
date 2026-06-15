import os
import re

def fix_unclosed_alerts():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix alert('...); missing the closing '
    # Match alert(' followed by non-quote characters and then );
    content = re.sub(r"alert\('([^']+)\);", r"alert('\1');", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Unclosed alerts fixed.")

fix_unclosed_alerts()
