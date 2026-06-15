import os
import re

def fix_all_style_quotes():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix fontSize: '12 }} -> fontSize: 12 }}
    content = re.sub(r"fontSize: '([0-9]+) \}\}", r"fontSize: \1 }}", content)
    
    # Fix borderTop: '... }} -> borderTop: '...' }}
    content = re.sub(r"borderTop: '([^'}]+) \}\}", r"borderTop: '\1' }}", content)
    content = re.sub(r"borderBottom: '([^'}]+) \}\}", r"borderBottom: '\1' }}", content)
    content = re.sub(r"border: '([^'}]+) \}\}", r"border: '\1' }}", content)

    # Fix the crazy color ternary: color: 'condition' ? 'color1' : 'color2 }}
    content = re.sub(r"color: '([^']+) > 0' \? '([^']+)' : '([^']+) \}\}", r"color: \1 > 0 ? '\2' : '\3' }}", content)

    # Undo the damage from fix_map_syntax.py: ); })} -> ))}
    content = content.replace("); })}", " ))}")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("All style quotes fixed.")

fix_all_style_quotes()
