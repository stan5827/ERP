
import os

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'rb') as f:
    content = f.read()

# Replace "??{skusData.totalCount} 蝑??桀?蝚?{currentPage}" etc is already fixed?
# I'll check what failed:
# <th>?嗉疏日期</th>
# <th>?∟頃單號</th>
# <th>瑼ａ?狀態</th>
# 撠?脰疏??

replacements = [
    (b'<th>?\xcd\xf4\xca\xe4\xc0\xfa\xb4\xfb</th>', b'<th>\xca\xe4\xc0\xfa\xb4\xfb</th>'), # This is hard to guess
]

# Better: use string find and replace if possible
try:
    s_content = content.decode('utf-8')
    s_content = s_content.replace('<th>?嗉疏日期</th>', '<th>進貨日期</th>')
    s_content = s_content.replace('<th>?∟頃單號</th>', '<th>採購單號</th>')
    s_content = s_content.replace('<th>瑼ａ?狀態</th>', '<th>檢驗狀態</th>')
    s_content = s_content.replace('撠?脰疏??', '查無接收紀錄')
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(s_content)
    print("Fixed with Python!")
except Exception as e:
    print(f"Error: {e}")
