import os

def check_braces_per_line():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    balance = 0
    for i, line in enumerate(lines):
        # Count { and } but ignore strings/comments if possible
        # For simplicity, just count them but watch for the dip
        balance += line.count('{') - line.count('}')
        if balance < 0:
            print(f"Negative balance at line {i + 1}: {balance}")
            # print(line)
            # break
    print(f"Final balance: {balance}")

check_braces_per_line()
