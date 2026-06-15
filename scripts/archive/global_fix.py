
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    # Look for unclosed alert(' or confirm(' or console.log(' or console.error('
    # specifically those that end without a closing ' before the ); or at the end of line
    
    # regex for: alert('... followed by ); or just end of line but no ' in between
    match = re.search(r"(alert|confirm|console\.(log|error))\('([^']+)$", line.strip())
    if match:
        func = match.group(1)
        content = match.group(3)
        # remove any trailing characters that look like they were meant to be the end
        content = content.replace(");", "").replace(")", "").replace(";", "").strip()
        # Add a closing ' and );
        new_line = line.split(func)[0] + func + "('" + content + "');\n"
        new_lines.append(new_line)
    else:
        new_lines.append(line)

# Also fix the specific mojibake labels found in the last view
content = "".join(new_lines)

mangled_replacements = {
    "alert('倉庫撌脫?狀態?');": "alert('倉庫狀態已更新');",
    "console.error('?倉庫憭望?:', err);": "console.error('停用倉庫失敗:', err);",
    "alert('?倉庫憭望?');": "alert('停用倉庫失敗');",
    "alert('?儲位憭望?');": "alert('停用儲位失敗');",
    "alert('??儲位憭望?');": "alert('啟用儲位失敗');",
    "alert('??倉庫憭望?');": "alert('啟用倉庫失敗');",
}

for old, new in mangled_replacements.items():
    content = content.replace(old, new)

# Fix more general patterns
content = content.replace("?", "停用")
content = content.replace("??", "啟用")
content = content.replace("撌脫?", "已更新")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Global unclosed string and mojibake labels fix complete.")
