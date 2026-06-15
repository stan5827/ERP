import os

def final_brute_force_literals():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Literals Fixes
    content = content.replace("useState(', )", "useState('')")
    content = content.replace("? '+' : ', }", "? '+' : ''}")
    content = content.replace("label: '資料修復''", "label: '狀態', className: '")
    content = content.replace("label: '文字已修正''", "label: '狀態', className: '")
    
    # Specifically fix the getStatusBadge / getNextAction patterns if they were missed
    content = content.replace("label: '資料修復'status-active'", "label: '已核准', className: 'status-active'")
    content = content.replace("label: '資料修復'info'", "label: '已完成', className: 'info'")
    content = content.replace("label: '資料修復'warning'", "label: '已取消', className: 'warning'")
    
    # Fix the button action labels
    content = content.replace("label: '資料修復'picking'", "label: '開始揀貨', nextStatus: 'picking'")
    content = content.replace("label: '資料修復'packing'", "label: '完成揀貨', nextStatus: 'packing'")
    content = content.replace("label: '資料修復'dispatched'", "label: '執行出貨', nextStatus: 'dispatched'")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Literals fix complete.")

final_brute_force_literals()
