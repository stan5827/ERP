import os

def brute_force_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Targets identified from Select-String
    content = content.replace("'??", "'退貨'")
    content = content.replace("'待盤?", "'待盤點'")
    content = content.replace("???", "盤點中")
    content = content.replace("'已???,", "'已完成',")
    content = content.replace("'差異待確?", "'待確認'")
    content = content.replace("已??", "已取消")
    content = content.replace("已???", "已完成")
    content = content.replace("?稿", "草稿")
    content = content.replace("?設?別", "預設倉庫")
    content = content.replace("?設??", "預設儲位")

    # Fix the missing quotes if any pattern was missed
    # This is a bit risky but let's try to close any line ending in ?
    lines = content.split('\n')
    new_lines = []
    for line in lines:
        if line.strip().endswith('?'):
            # Potentially unclosed quote before the ?
            if line.count("'") % 2 != 0:
                line = line + "'"
        new_lines.append(line)
    
    content = '\n'.join(new_lines)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Brute force fix complete.")

if __name__ == "__main__":
    brute_force_fix()
