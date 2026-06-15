import os

def final_polish():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    
    # Restore the component declaration
    # In the current file, line 1526 is 'const queryClient...' (index 1525)
    lines.insert(1525, "export const Inventory: React.FC = () => {\n")
    
    # Fix the CSS at the end
    # We want to remove those stray quotes added by the global regex
    for i in range(len(lines) - 20, len(lines)):
        lines[i] = lines[i].replace("'auto;'", "auto")
        lines[i] = lines[i].replace("'nowrap;'", "nowrap")
        lines[i] = lines[i].replace("'rgba(255,255,255,0.03);'", "rgba(255,255,255,0.03)")
        lines[i] = lines[i].replace("'rgba(59,130,246,0.05);'", "rgba(59,130,246,0.05)")
        lines[i] = lines[i].replace("'700;'", "700")
        lines[i] = lines[i].replace("'2px;'", "2px")
        lines[i] = lines[i].replace("'16;'", "16")
        lines[i] = lines[i].replace("'12;'", "12")
        lines[i] = lines[i].replace("'8;'", "8")
        lines[i] = lines[i].replace("'14;'", "14")
        lines[i] = lines[i].replace("'20;'", "20")
        lines[i] = lines[i].replace("'6;'", "6")

    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print("Final polish applied.")

final_polish()
