import os
import re

def find_unbalanced_styles():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    for i, line in enumerate(lines):
        if 'style={{' in line:
            # Count opening and closing braces on THIS line
            # This is a heuristic but good for finding the swallows
            if line.count('{{') > line.count('}}'):
                print(f"Potential unbalanced style at line {i+1}: {line.strip()}")

find_unbalanced_styles()
