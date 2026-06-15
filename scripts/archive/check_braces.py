import os

def check_braces():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    balance = 0
    stack = []
    for i, line in enumerate(lines):
        for char in line:
            if char == '{':
                balance += 1
                stack.append(i + 1)
            elif char == '}':
                balance -= 1
                if stack:
                    stack.pop()
                if balance < 0:
                    print(f"Negative balance at line {i + 1}")
                    return
    
    print(f"Final balance: {balance}")
    if balance > 0:
        print(f"Unclosed braces started at lines: {stack[-5:]}")

check_braces()
