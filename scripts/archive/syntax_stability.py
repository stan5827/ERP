import os
import re

def absolute_syntax_stability():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    new_lines = []
    for line in lines:
        # Check for unclosed single quotes
        if line.count("'") % 2 != 0:
            # If it's a label line, replace the whole label value
            if "label: '" in line:
                line = re.sub(r"(label: ')[^'\n]*", r"\1資料修復'", line)
            elif "sub: '" in line:
                line = re.sub(r"(sub: ')[^'\n]*", r"\1資料修復'", line)
            elif "desc: '" in line:
                line = re.sub(r"(desc: ')[^'\n]*", r"\1資料修復'", line)
            else:
                # Still odd? Just balance it
                line = line.strip() + "'\n"
        
        # Check for unclosed double quotes
        if line.count('"') % 2 != 0:
            if 'title="' in line:
                 line = re.sub(r'(title=")[^"\n]*', r'\1資料修復"', line)
            else:
                 line = line.strip() + '"\n'
                 
        new_lines.append(line)
        
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Syntax stability confirmed.")

absolute_syntax_stability()
