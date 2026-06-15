import os

def fix_type_options_structural():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    target_found = False
    new_lines = []
    for i, line in enumerate(lines):
        if "const typeOptions = [" in line:
            new_lines.append("  const typeOptions = [\n")
            new_lines.append("    { value: 'all', label: '全部類型' },\n")
            new_lines.append("    { value: 'purchase_receipt', label: '採購入庫' },\n")
            new_lines.append("    { value: 'production_receipt', label: '完工入庫' },\n")
            new_lines.append("    { value: 'sales_issue', label: '銷售出庫' },\n")
            new_lines.append("    { value: 'production_issue', label: '領料出庫' },\n")
            new_lines.append("    { value: 'transfer', label: '調撥' },\n")
            new_lines.append("    { value: 'adjustment', label: '調整' },\n")
            new_lines.append("    { value: 'return', label: '退貨' },\n")
            new_lines.append("  ];\n")
            target_found = True
        elif target_found:
            if "];" in line:
                target_found = False
            continue
        else:
            new_lines.append(line)

    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Type Options structural fix complete.")

fix_type_options_structural()
