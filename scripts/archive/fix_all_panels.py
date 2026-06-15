import os
import re

def fix_all_panels():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Fix the specific pattern that causes parse errors: label: '...' 'var(...)'
    # We'll try to restore the missing "value: 0, color: " or similar
    
    # Pattern 1: { label: '資料修復''var(--...)' }
    content = re.sub(r"label: '資料修復''var\((--[a-z]+-\d+)\)'", r"label: '庫存指標', value: 0, color: 'var(\1)'", content)
    
    # Pattern 2: { label: '資料修復''NT\$ [0-9,]+'", color: 'var(...)' }
    content = re.sub(r"label: '資料修復''NT\$ ([\d,]+)'", r"label: '總金額', value: 'NT$ \1'", content)

    # 3. Fix ExpiryAlertPanel labels
    content = content.replace("label: '資料修復', sub: '< 7", "label: '緊急過期', sub: '< 7")
    content = content.replace("label: '資料修復', sub: '< 30", "label: '即將過期', sub: '< 30")
    content = content.replace("label: '資料修復', sub: '< 90", "label: '一般警示', sub: '< 90")

    # 4. Fix AnalysisPanel ABC labels
    content = content.replace("{ cls: 'A', label: '資料修復', value: 0, color: 'var(--cobalt-400)'", "{ cls: 'A', label: '高價值(A)', pct: 80, items: 20, color: 'var(--cobalt-400)'")
    content = content.replace("{ cls: 'B', label: '資料修復', value: 0, color: 'var(--amber-400)'", "{ cls: 'B', label: '中價值(B)', pct: 15, items: 30, color: 'var(--amber-400)'")
    content = content.replace("{ cls: 'C', label: '資料修復', value: 0, color: 'var(--slate-400)'", "{ cls: 'C', label: '低價值(C)', pct: 5, items: 50, color: 'var(--slate-400)'")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Panels fixed.")

fix_all_panels()
