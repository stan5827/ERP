
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix mangled rgba strings like 'rgba(4',8,15,0.8)' -> 'rgba(4,8,15,0.8)'
def fix_rgba(match):
    # Match something like 'rgba( numbers', then other numbers ')
    # e.g. 'rgba(4',8,15,0.8)'
    prefix = match.group(1) # 'rgba(
    num1 = match.group(2)   # 4
    rest = match.group(3)   # ,8,15,0.8)
    return f"'{prefix}{num1}{rest}'"

# Searching for patterns where a quote is inside rgba parentheses
# 'rgba(\d+)',([\d\.,\s]+)'
# We match: 'rgba(  followed by digit(s)  then ', then more digits/dots/commas then )'
new_content = re.sub(r"'rgba\((\d+)',([\d\.,\s]+)\)'", r"'rgba(\1,\2)'", content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("RGBA string repair complete.")
