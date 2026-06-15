import os
import re

def final_double_quote_guard():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    new_lines = []
    for line in lines:
        if line.count('"') % 2 != 0:
            if 'title="' in line:
                line = re.sub(r'title="[^"]*', 'title="功能開發中"', line)
            else:
                line = line.strip() + '"\n'
        new_lines.append(line)
        
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Double quotes fixed.")

final_double_quote_guard()
