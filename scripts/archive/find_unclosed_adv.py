import os

def find_unclosed_styles_adv():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()
    
    # Find all 'style={{' and the next '}}'
    # If it hits another 'style={{' before '}}', then it's unbalanced.
    pos = 0
    while True:
        pos = content.find('style={{', pos)
        if pos == -1: break
        
        next_close = content.find('}}', pos)
        next_open = content.find('style={{', pos + 8)
        
        if next_close == -1:
            print(f"Unclosed style at pos {pos}")
            break
        
        if next_open != -1 and next_open < next_close:
            print(f"Potential swallow at pos {pos} (next open {next_open} < next close {next_close})")
            snippet = content[pos:pos+100].replace('\n', ' ')
            print(f"Snippet: {snippet}")
        
        pos = next_close + 2

find_unclosed_styles_adv()
