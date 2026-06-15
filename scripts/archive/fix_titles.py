import os
import re

def fix_unclosed_titles():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix title="... missing the closing "
    # Match title=" followed by any characters that are NOT " or < until a >
    content = re.sub(r'title="([^"<]+)>', r'title="\1">', content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Unclosed titles fixed.")

fix_unclosed_titles()
