import os
import re

def fix_type_quotes():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix (v: 'type) -> (v: type)
    content = re.sub(r": '([a-zA-Z0-9]+)\)", r": \1)", content)
    
    # Fix ' }}> -> }}>
    content = content.replace("' }}>", " }}>")
    
    # Fix '}); -> });
    content = content.replace("'});", "});")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Type quotes fixed.")

fix_type_quotes()
