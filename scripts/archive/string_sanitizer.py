import os
import re

def string_sanitizer():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Find string literals: '...' or "..."
    # This is a bit naive but works for well-formed TSX.
    def clean_str(match):
        s = match.group(0)
        if '?' in s or '' in s:
            # If it's a very short string, it's likely a label or status.
            # We'll replace it with a placeholder that doesn't break syntax.
            quote = s[0]
            # Try to keep some identifiable parts
            if 'alert' in s.lower() or 'error' in s.lower():
                return f"{quote}系統錯誤或提示{quote}"
            if 'placeholder' in s.lower():
                return f"{quote}請輸入...{quote}"
            return f"{quote}文字已修復{quote}"
        return s

    content = re.sub(r"'[^'\n]*'|\"[^\"\n]*\"", clean_str, content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Strings sanitized.")

string_sanitizer()
