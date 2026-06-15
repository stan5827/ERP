import os
import re

def nuclear_quote_cleanup():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    new_lines = []
    for line in lines:
        if line.count("'") % 2 != 0:
            # If there's an odd quote followed by a comma or paren, it's missing the closing quote
            line = re.sub(r"':? ([^'\n,)]*)([,)])", r"'\1'\2", line)
            # Re-check
            if line.count("'") % 2 != 0:
                line = line.replace("'", "''") # Desperate attempt to balance
        new_lines.append(line)
        
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Nuclear cleanup complete.")

nuclear_quote_cleanup()
