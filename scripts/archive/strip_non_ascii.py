import os

def strip_non_ascii():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'rb') as f:
        data = f.read()
    
    # Keep only ASCII bytes
    clean = bytes(b for b in data if 32 <= b <= 126 or b in [10, 13, 9])
    
    with open(filename, 'wb') as f:
        f.write(clean)
    print("Non-ASCII stripped.")

strip_non_ascii()
