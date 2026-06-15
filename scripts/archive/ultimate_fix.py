
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# More flexible match for the messy confirm
pattern_mess = re.compile(r"if \(confirm\('蝣箄\?\?箄疏.+?嗎？'\)\) \{'")
good_confirm = "if (confirm('確認出貨後將自動扣除庫存並更新銷售訂單狀態，是否繼續？')) {"
content = pattern_mess.sub(good_confirm, content)

# Also fix the weird {' at the end of line 1403 if it's there
content = content.replace(")) {'", ")) {")

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Ultimate fix stage 2 complete.")
