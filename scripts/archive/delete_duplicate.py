import os

def delete_duplicate_decl():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Check lines 1525 and 1526
    if "export const Inventory: React.FC" in lines[1524] and "export const Inventory: React.FC" in lines[1525]:
        print("Duplicate found. Deleting line 1525.")
        del lines[1524]
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Done.")

delete_duplicate_decl()
