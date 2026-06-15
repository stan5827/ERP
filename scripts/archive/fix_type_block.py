import os

def fix_type_block_final():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # Lines 111 to 127 are the target
    # We replace from the first { value: 'all' to the closure ];
    
    new_lines = []
    skip = False
    for i, line in enumerate(lines):
        if "{ value: 'all'" in line:
            new_lines.append("    { value: 'all', label: '全部類型' },\n")
            new_lines.append("    { value: 'purchase_receipt', label: '採購入庫' },\n")
            new_lines.append("    { value: 'production_receipt', label: '完工入庫' },\n")
            new_lines.append("    { value: 'sales_issue', label: '銷售出庫' },\n")
            new_lines.append("    { value: 'production_issue', label: '領料出庫' },\n")
            new_lines.append("    { value: 'transfer', label: '調撥' },\n")
            new_lines.append("    { value: 'adjustment', label: '調整' },\n")
            new_lines.append("    { value: 'return', label: '退貨' },\n")
            new_lines.append("  ];\n")
            skip = True
        elif skip:
            if "];" in line:
                skip = False
            continue
        else:
            new_lines.append(line)

    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Type block fixed by line insertion.")

fix_type_block_final()
