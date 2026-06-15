import os

def absolute_final_cleanup():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # The most common broken patterns from the previous run
    content = content.replace("', )", "'')")
    content = content.replace("', }", "''}")
    content = content.replace("', ,", "'',")
    content = content.replace("|| ', }", "|| '' }")
    content = content.replace("value={item.lotNumber || ', }", "value={item.lotNumber || ''}")
    content = content.replace("label: '資料修復'status-warning'", "label: '待處理', className: 'status-warning'")
    content = content.replace("label: '資料修復'status-neutral'", "label: '已取消', className: 'status-neutral'")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Absolute final cleanup complete.")

absolute_final_cleanup()
