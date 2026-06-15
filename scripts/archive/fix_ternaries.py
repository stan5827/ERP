import os
import re

def fix_ternary_strings():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix ternary strings like ? 'value : ...
    # We look for ? ' followed by non-quote characters and then : or }
    content = re.sub(r"\? '([^':}]+) :", r"? '\1' :", content)
    content = re.sub(r": ([^':}]+) \?", r": '\1' ?", content)
    content = re.sub(r": ([^':}]+) }", r": '\1' }", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Ternary strings fixed.")

fix_ternary_strings()
