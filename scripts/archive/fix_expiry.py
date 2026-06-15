import os

def fix_expiry_panel():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    new_lines = []
    for line in lines:
        if "label:" in line and "sub: '< 7" in line:
            new_lines.append("    { label: '緊急過期', sub: '< 7 天到期', color: 'var(--crimson-400)', bg: 'rgba(232,51,82,0.08)', border: 'rgba(232,51,82,0.25)', icon: <AlertTriangle size={14} />, items: [] },\n")
        elif "label:" in line and "sub: '< 30" in line:
            new_lines.append("    { label: '即將過期', sub: '< 30 天到期', color: 'var(--amber-400)', bg: 'rgba(245,151,10,0.08)', border: 'rgba(245,151,10,0.25)', icon: <AlertTriangle size={14} />, items: [] },\n")
        else:
            new_lines.append(line)

    with open(filename, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    print("Expiry panel fixed.")

fix_expiry_panel()
