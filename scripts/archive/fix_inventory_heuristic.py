import os

def fix_mojibake():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'rb') as f:
        data = f.read()
    
    # Try to decode what PowerShell Set-Content (UTF8 with BOM) produced
    # and what it might have misread initially.
    # Usually it's UTF-8 -> Latin1/Big5 -> UTF-8 corruption.
    
    try:
        text = data.decode('utf-8-sig')
        # This is the corrupted text. We need to find the inverse mapping.
        # But since I have the target labels, I'll just do direct string replacement for known corrupted strings.
        
        replacements = [
            ("ABC ??", "ABC 分類"),
            ("????", "重要物料"),
            ("一?物??", "一般物料"),
            ("次???", "次要物料"),
            ("??購?(ROP)", "再訂購點 (ROP)"),
            ("?次管?", "批次管理"),
            ("序?管?", "序號管理"),
            ("??檢?(IQC)", "進料檢驗(IQC)"),
            ("?貨檢?(OQC)", "出貨檢驗(OQC)"),
            ("?設存放?庫", "預設存放倉庫"),
            ("?設存放??", "預設存放儲位"),
            ("安全庫???,", "安全庫存量',"),
            ("?大庫存?", "最大庫存量"),
            ("平??耗?", "平均日消耗量"),
        ]
        
        for old, new in replacements:
            text = text.replace(old, new)
            
        with open(filename, 'w', encoding='utf-8') as f:
            f.write(text)
        print("Heuristic fix applied.")
    except Exception as e:
        print(f"Error: {e}")

fix_mojibake()
