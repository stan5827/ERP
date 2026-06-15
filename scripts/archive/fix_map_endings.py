import os
import re

def fix_map_endings_regex():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix the corrupted map endings: );\s*\}\)\} or );\s*\}\}
    # These were caused by fix_map_syntax.py replacing ))\} with ); })}
    content = re.sub(r'\);\s*\}\s*\}\s*\)', ' ))}', content)
    content = re.sub(r'\);\s*\}\s*\}', ' ))}', content)
    
    # Also fix some other common corruption patterns
    content = content.replace("); })}", " ))}")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Map endings fixed.")

fix_map_endings_regex()
