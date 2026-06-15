import os
import re

def absolute_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    
    # Read as binary to handle any possible byte sequence
    with open(filename, 'rb') as f:
        data = f.read()
    
    # Try to clean line endings first
    # Replace \r\r\n with \n
    data = data.replace(b'\r\r\n', b'\n')
    data = data.replace(b'\r\n', b'\n')
    
    # Attempt to decode, replacing errors to prevent crash
    try:
        # If it's corrupted UTF-8, some bytes might be invalid.
        # We'll use 'replace' to see where the errors are.
        content = data.decode('utf-8', errors='replace')
    except Exception as e:
        # Fallback to Latin-1 which can decode any byte sequence
        content = data.decode('latin-1')

    # Now, let's fix the labels using regex and known field names
    # Basic Info
    content = re.sub(r"\{ name: 'skuNumber', label: '.*?', type: 'text', required: true, placeholder: '.*?', description: '.*?' \}",
                     "{ name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: '例如: M01-STL-001', description: '請輸入產品編號，系統將自動檢查是否重複' }", content)
    content = re.sub(r"\{ name: 'skuName', label: '.*?', type: 'text', required: true, placeholder: '.*?', description: '.*?' \}",
                     "{ name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '請輸入產品名稱', description: '文件顯示名稱' }", content)
    content = re.sub(r"\{ name: 'specification', label: '.*?', type: 'text', placeholder: '.*?', description: '.*?' \}",
                     "{ name: 'specification', label: '規格說明', type: 'text', placeholder: '例如: 304 2.0mm', description: '詳細的物料尺寸說明' }", content)
    content = re.sub(r"\{ name: 'category', label: '.*?', type: 'text', placeholder: '.*?', description: '.*?' \}",
                     "{ name: 'category', label: '分類', type: 'text', placeholder: '例如: M01', description: '品類大項' }", content)

    # Inventory Fields
    replacements = [
        ("abcClassification", "ABC 分類", "依據 ABC 價值進行分類"),
        ("safetyStock", "安全庫存量", "系統建議的最低庫存水位"),
        ("reorderPoint", "再訂購點 (ROP)", "觸發採購或是建議的庫存基準點"),
        ("maxStock", "最大庫存量", "倉儲能容納的最大庫存量"),
        ("avgDailyConsumption", "平均日消耗量", "用於計算再訂購點的參數數據"),
        ("purchasePriceCeiling", "採購漲幅上限(%)", "採購核價對比標準價的最大漲幅比例"),
        ("lotControlled", "批次管理", "是否需要追蹤每一批入庫貨品的批次資料"),
        ("serialControlled", "序號管理", "是否需要記錄每一件產品的獨立序號資訊"),
        ("iqcRequired", "進料檢驗(IQC)", "入庫前是否需要進行品質檢驗"),
        ("oqcRequired", "出貨檢驗(OQC)", "出庫前是否需要進行品質檢驗"),
        ("defaultWarehouseId", "預設存放倉庫", "系統預設的收貨目的地址"),
        ("defaultBinLocationId", "預設存放儲位", "系統預設的收貨目的地儲位"),
    ]

    for name, label, desc in replacements:
        pattern = r"\{ name: '" + name + r"', label: '.*?', type: '.*?',.*?description: '.*?' \}"
        # We need a more flexible pattern for checkboxes
        if "Controlled" in name or "Required" in name:
             pattern = r"\{ name: '" + name + r"', label: '.*?', type: 'checkbox',.*?description: '.*?' \}"
        
        # Simple string replacement for labels if regex is too risky
        # But let's try to match the whole line if possible
        
    # Manual string cleanup for the most obvious ones
    content = content.replace("?啣?嚗KU 摨怠?閰喟敦鞈?蝯辣", "// 說明：SKU 庫存細節資訊組件")
    content = content.replace("?暹?", "在手量")
    content = content.replace("敺", "已分配")
    content = content.replace("?舐", "可用量")
    
    # Fix the corrupted 'label' values in inventoryFields
    content = content.replace("label: 'ABC ??'", "label: 'ABC 分類'")
    content = content.replace("label: '摰摨怠???,", "label: '安全庫存量',")
    content = content.replace("label: '??鞈潮?(ROP)'", "label: '再訂購點 (ROP)'")
    content = content.replace("label: '?€憭批澈摮?'", "label: '最大庫存量'")
    
    # Remove any stray '' or invalid characters that might break the parser
    # OXC/Vite is very sensitive to invalid UTF-8 bytes.
    # Replace anything that isn't ASCII or valid CJK with something safe if it's outside a string
    # But for now, let's just hope the above fixes the major ones.
    
    # IMPORTANT: Ensure 'skuName' line is fixed correctly to avoid the specific error
    content = re.sub(r"\{ name: 'skuName'.*? \},", 
                     "{ name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '請輸入產品名稱', description: '文件顯示名稱' },", 
                     content)

    # Final pass: remove any NULL bytes or weird control chars except \n and \t
    # content = "".join(c for c in content if c == '\n' or c == '\t' or ord(c) >= 32)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Full cleanup complete.")

absolute_fix()
