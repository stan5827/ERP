import os
import re

def fix_unclosed_quotes():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Find unclosed single quotes in label values
    # Match label: ' followed by any characters that are NOT ' until a , or } or newline
    # and replace with label: 'corrected'
    
    # Example: label: '銝€?砍€? } -> label: '一般倉' }
    
    # We'll do specific replacements for these common ones
    replacements = [
        ("label: '銝€?砍€? }", "label: '一般倉' }"),
        ("label: '?刻ˊ?€? }", "label: '在製品倉' }"),
        ("label: '撱Ｘ??? }", "label: '報廢倉' }"),
        ("label: '倉庫?迂 *'", "label: '倉庫名稱 *'"),
        ("label: '?啣?'", "label: '地址'"),
        ("label: '??€??", "label: '啟用狀態'"),
        ("label: '憿?'", "label: '類型'"),
    ]

    for old, new in replacements:
        content = content.replace(old, new)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Unclosed quotes fixed.")

fix_unclosed_quotes()
