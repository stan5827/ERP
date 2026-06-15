import os
import re

def fix_analysis_data():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    new_abc = """  const abcData = [
    { cls: "A", label: "高價值存貨", pct: 80, items: 20, color: "var(--cobalt-400)", desc: "價值佔比 80%, 數量僅佔 20%" },
    { cls: "B", label: "中價值存貨", pct: 15, items: 30, color: "var(--amber-400)", desc: "價值佔比 15%, 數量佔 30%" },
    { cls: "C", label: "低價值存貨", pct: 5,  items: 50, color: "var(--slate-400)", desc: "價值佔比 5%, 數量佔 50%" },
  ];"""

    new_summary = """  const summaryCards = [
    { label: '總庫存資產市值', value: 'NT$ 3,842,600', color: 'var(--cobalt-300)' },
    { label: '低於安全庫存', value: '12 筆', color: 'var(--amber-300)' },
    { label: '在手量零值警示', value: '5 筆', color: 'var(--crimson-300)' },
    { label: '最後一次 MRP 執行', value: '今日 08:00', color: 'var(--emerald-300)' },
  ];"""

    # Replace the blocks
    content = re.sub(r'  const abcData = \[.*?\];', new_abc, content, flags=re.DOTALL)
    content = re.sub(r'  const summaryCards = \[.*?\];', new_summary, content, flags=re.DOTALL)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Analysis data fixed.")

fix_analysis_data()
