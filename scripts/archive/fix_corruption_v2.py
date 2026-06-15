import os
import re

def fix_corruption_v2():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Fix quoted destructuring with multiple variables
    content = re.sub(r"\{ data: '([^']+)' \}", r"{ data: \1 }", content)
    
    # Fix quoted type annotations
    content = re.sub(r": 'Record<string, unknown> = \{ \.\.\.sku' \};", r": Record<string, unknown> = { ...sku };", content)
    
    # Fix pageSize: '10' etc if they were corrupted
    # Actually let's just unwrap ALL quoted property values that end with Data, isLoading, warehouses, etc.
    
    # Fix the specific one on 1558
    content = content.replace("{ data: 'skusData, isLoading' }", "{ data: skusData, isLoading }")
    content = content.replace("pageSize: '10'", "pageSize: 10")
    content = content.replace("page: currentPage.toString()", "page: currentPage.toString()") # this one is okay

    # Fix the Warehouse panel useQuery calls
    content = re.sub(r"\{ data: 'warehouseData' \}", r"{ data: warehouseData }", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Corruption V2 fixed.")

fix_corruption_v2()
