import os

def fix_tabs_english():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Replace the tabs array with English labels
    for i in range(len(lines)):
        if 'const tabs = [' in lines[i]:
            lines[i+1] = '    { id: "skus", label: "SKU List", desc: "SKU" },\n'
            lines[i+2] = '    { id: "transactions", label: "Transactions", desc: "Trans" },\n'
            lines[i+3] = '    { id: "shipping", label: "Shipping", desc: "Shipping" },\n'
            lines[i+4] = '    { id: "count", label: "Inventory Count", desc: "Count" },\n'
            lines[i+5] = '    { id: "receipts", label: "Receipts", desc: "Receipts" },\n'
            lines[i+6] = '    { id: "transfers", label: "Transfers", desc: "Transfer" },\n'
            lines[i+7] = '    { id: "completions", label: "Completions", desc: "Completion" },\n'
            lines[i+8] = '    { id: "bins", label: "Bin Management", desc: "Bin" },\n'
            lines[i+9] = '    { id: "analysis", label: "Inventory Analysis", desc: "Analysis" },\n'
            break

    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Tabs changed to English.")

fix_tabs_english()
