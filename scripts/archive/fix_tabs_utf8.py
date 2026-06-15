import os

def fix_tabs_unicode():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Replace the corrupted tabs array
    # We'll use a regex or just replace the whole block
    import re
    tabs_block = """  const tabs = [
    { id: 'skus', label: '品碼清單', desc: 'SKU' },
    { id: 'transactions', label: '異動紀錄', desc: 'Trans' },
    { id: 'shipping', label: '出貨作業', desc: 'Shipping' },
    { id: 'count', label: '盤點作業', desc: 'Count' },
    { id: 'receipts', label: '收料清單', desc: 'Receipts' },
    { id: 'transfers', label: '調撥管理', desc: 'Transfer' },
    { id: 'completions', label: '完工入庫', desc: 'Completion' },
    { id: 'bins', label: '儲位管理', desc: 'Bin' },
    { id: 'analysis', label: '庫存分析', desc: 'ABC/Aging' },
  ] as const;"""

    content = re.sub(r"const tabs = \[.*?\] as const;", tabs_block, content, flags=re.DOTALL)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Tabs fixed with clean UTF-8.")

fix_tabs_unicode()
