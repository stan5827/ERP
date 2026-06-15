
import sys

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
found = False
# Search around 2097 (0-indexed 2096)
for i, line in enumerate(lines):
    if 2080 < i < 2110 and "alert('" in line and "));'" in line:
        new_lines.append("      alert('請選擇倉庫並輸入儲位代碼');\n")
        found = True
        print(f"Fixed line {i+1} successfully.")
    else:
        new_lines.append(line)

if found:
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
else:
    print("Could not find any line matching the pattern in the target range.")
