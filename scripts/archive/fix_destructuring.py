import os
import re

def fix_destructuring():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix { data: 'variableName' } -> { data: variableName }
    content = re.sub(r'\{ data: \'([a-zA-Z0-9]+)\' \}', r'{ data: \1 }', content)
    
    # Also fix { data: "variableName" }
    content = re.sub(r'\{ data: "([a-zA-Z0-9]+)" \}', r'{ data: \1 }', content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Destructuring fixed.")

fix_destructuring()
