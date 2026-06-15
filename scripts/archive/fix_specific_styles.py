import os

def fix_specific_styles():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix the missing quote before }} or }}>
    content = content.replace("var(--slate-500) }}>", "var(--slate-500)' }}>")
    content = content.replace("var(--slate-400) }}>", "var(--slate-400)' }}>")
    content = content.replace("var(--slate-300) }}>", "var(--slate-300)' }}>")
    content = content.replace("var(--slate-500) }", "var(--slate-500)' }")
    content = content.replace("var(--slate-400) }", "var(--slate-400)' }")
    content = content.replace("var(--slate-300) }", "var(--slate-300)' }")
    
    # Fix the textAlign: 'center' missing quote
    content = content.replace("textAlign: 'center }", "textAlign: 'center' }")
    content = content.replace("display: 'block }", "display: 'block' }")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Specific styles fixed.")

fix_specific_styles()
