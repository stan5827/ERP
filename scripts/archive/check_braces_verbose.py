import os

def check_braces_verbose():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    balance = 0
    for i, line in enumerate(lines):
        balance += line.count('{') - line.count('}')
        print(f"Line {i+1}: balance {balance}")
    print(f"Final balance: {balance}")

check_braces_verbose()
