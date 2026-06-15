import os

def final_cleanup_v3():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Fix the double quote issue specifically
    content = content.replace("label: '資料修復''", "label: '資料修復'")
    
    # Fix remaining mojibake in ExpiryAlertPanel labels if any from sanitized
    content = content.replace("label: '銝€?祆釣??'", "label: '一般警示'")
    
    # Fix AnalysisPanel summaryCards labels
    content = content.replace("label: '資料修復', value: '12 隞?'", "label: '低周轉品', value: '12 項'")
    content = content.replace("label: '資料修復', value: '5 隞?'", "label: '超齡庫存', value: '5 項'")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Final cleanup v3 complete.")

final_cleanup_v3()
