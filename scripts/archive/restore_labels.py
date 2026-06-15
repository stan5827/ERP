import os
import re

def restore_clean_labels():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Restore the Field definition blocks with clean labels
    # Note: the ultimate_fix turned everything to '文字已修復'
    
    # We use regex to find the blocks based on the field names (which are ASCII)
    # Basic Info
    content = re.sub(r'\{ name: \'skuNumber\', label: \'.*?\', type: \'.*?\',.*? \}', 
                     "{ name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: '例如: M01-STL-001' }", content)
    content = re.sub(r'\{ name: \'skuName\', label: \'.*?\', type: \'.*?\',.*? \}', 
                     "{ name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '輸入產品名稱' }", content)
    
    # Source Type
    content = re.sub(r'\{ name: \'sourceType\', label: \'.*?\', type: \'.*?\',.*?options: \[', 
                     "{ name: 'sourceType', label: '來源類型 *', type: 'select', required: true, options: [", content)
    
    # Purch Checkboxes
    content = re.sub(r'\{ name: \'isPurchasable\', label: \'.*?\', type: \'.*?\' \}', 
                     "{ name: 'isPurchasable', label: '可採購', type: 'checkbox' }", content)
    content = re.sub(r'\{ name: \'isSellable\', label: \'.*?\', type: \'.*?\' \}', 
                     "{ name: 'isSellable', label: '可銷售', type: 'checkbox' }", content)

    # Status Configs in Panels
    content = content.replace("'文字已修復'in_progress'文字已修復'", "'盤點中'")
    content = content.replace("{ label: '文字已修復', value: 0, color: 'var(--amber-400)' }", "{ label: '待盤點', value: 0, color: 'var(--amber-400)' }")
    content = content.replace("{ label: '文字已修復', value: 0, color: 'var(--emerald-400)' }", "{ label: '已完成', value: 0, color: 'var(--emerald-400)' }")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Labels restored.")

restore_clean_labels()
