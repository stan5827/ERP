import os

def final_analysis_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Restoration of AnalysisPanel items
    content = content.replace("{ cls: 'A', label: '高價??, pct: 80, items: 20", "{ cls: 'A', label: '高價值', pct: 80, items: 20")
    content = content.replace("{ cls: 'B', label: '中價??, pct: 15, items: 30", "{ cls: 'B', label: '中價值', pct: 15, items: 30")
    content = content.replace("{ cls: 'C', label: '低價??, pct: 5,  items: 50", "{ cls: 'C', label: '低價值', pct: 5,  items: 50")
    content = content.replace("{ label: '庫?總估??, value: 'NT$ 3,842,600'", "{ label: '庫存總估', value: 'NT$ 3,842,600'")
    content = content.replace("{ label: '?滯??', value: '12 ?", "{ label: '呆滯品', value: '12 項'")
    content = content.replace("{ label: '超???', value: '5 ?", "{ label: '超齡品', value: '5 項'")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Analysis panel fixed.")

final_analysis_fix()
