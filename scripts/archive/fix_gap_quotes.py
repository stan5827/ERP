import os
import re

def fix_gap_quotes():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix unclosed gap strings: gap: '10 } -> gap: 10 } (or gap: '10' })
    # In my reconstruction I used numbers, but here it's strings.
    content = content.replace("gap: '10 }", "gap: 10 }")
    content = content.replace("gap: '12 }", "gap: 12 }")
    content = content.replace("gap: '14 }", "gap: 14 }")
    content = content.replace("gap: '16 }", "gap: 16 }")
    content = content.replace("gap: '20 }", "gap: 20 }")
    content = content.replace("gap: '24 }", "gap: 24 }")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Gap quotes fixed.")

fix_gap_quotes()
