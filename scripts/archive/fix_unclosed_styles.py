import os
import re

def fix_unclosed_styles():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix unclosed border strings
    content = content.replace("borderTop: '1px solid var(--glass-border) }", "borderTop: '1px solid var(--glass-border)' }")
    content = content.replace("borderBottom: '1px solid var(--glass-border) }", "borderBottom: '1px solid var(--glass-border)' }")
    content = content.replace("border: '1px solid var(--slate-700) }", "border: '1px solid var(--slate-700)' }")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Unclosed styles fixed.")

fix_unclosed_styles()
