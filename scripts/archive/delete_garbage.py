import os

def delete_garbage():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Lines 432 to 491 (0-indexed: 431 to 490)
    # Actually, line 431 is the ); of the return. 432 is the } of the function.
    # Garbage starts at 432 (index 431).
    # TransferPanel starts at 492 (index 491).
    # So we want to delete index 432 to 490.
    
    del lines[432:491]
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Garbage deleted.")

delete_garbage()
