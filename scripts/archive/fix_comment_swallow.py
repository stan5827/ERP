import os
import re

def fix_swallowed_tags():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix tags like /th> or /button> that are missing the leading <
    # We look for a character that is NOT < followed by / and then a tag name and >
    
    tags = ['th', 'td', 'button', 'div', 'span', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'tr', 'table', 'thead', 'tbody', 'option', 'select', 'label']
    
    for tag in tags:
        # Match anything that isn't a < followed by /tag>
        # We also need to handle the case where it's at the start of a line or after a newline
        pattern = r'([^<])(/' + tag + r'>)'
        content = re.sub(pattern, r'\1<\2', content)
        
        # Handle start of line
        content = re.sub(r'^/' + tag + r'>', r'</' + tag + r'>', content, flags=re.MULTILINE)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Swallowed tags fixed.")

fix_swallowed_tags()
