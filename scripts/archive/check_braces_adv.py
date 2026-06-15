import os

def check_braces_advanced():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()
    
    balance = 0
    stack = []
    in_string = None
    in_template = False
    in_comment = False
    
    i = 0
    while i < len(content):
        char = content[i]
        
        if in_comment:
            if char == '\n':
                in_comment = False
        elif in_template:
            if char == '`':
                in_template = False
            elif char == '$' and i + 1 < len(content) and content[i+1] == '{':
                balance += 1
                stack.append(('template', i))
                i += 1
        elif in_string:
            if char == in_string:
                in_string = None
            elif char == '\\':
                i += 1
        else:
            if char == '`':
                in_template = True
            elif char in ["'", '"']:
                in_string = char
            elif char == '/' and i + 1 < len(content) and content[i+1] == '/':
                in_comment = True
                i += 1
            elif char == '{':
                balance += 1
                stack.append(('brace', i))
            elif char == '}':
                balance -= 1
                if stack:
                    stack.pop()
                if balance < 0:
                    print(f"Negative balance at index {i}")
                    return
        i += 1
    
    print(f"Final balance: {balance}")
    if balance > 0:
        for typ, idx in stack[-5:]:
            # Find line number
            line_no = content.count('\n', 0, idx) + 1
            print(f"Unclosed {typ} at line {line_no}")

check_braces_advanced()
