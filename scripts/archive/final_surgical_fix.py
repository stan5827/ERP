import os
import re

def final_surgical_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Restoration of Badge/Action functions that are mangled
    # 1. getStatusBadge (General)
    content = re.sub(r'const getStatusBadge = \(status: string\) => \{.*?return map\[status\] \|\| \{ label: status, className: \'status-neutral\', color: \'var\(--slate-500\)\' \};\n  \};', 
"""const getStatusBadge = (status: string) => {
    const map: Record<string, { label: string; className: string; color: string }> = {
      pending:    { label: '待處理',  className: 'status-warning', color: 'var(--amber-400)' },
      approved:   { label: '已核准',  className: 'status-active',  color: 'var(--emerald-400)' },
      completed:  { label: '已完成',  className: 'status-info',    color: 'var(--cobalt-400)' },
      cancelled:  { label: '已取消',  className: 'status-neutral', color: 'var(--slate-500)' },
      dispatched: { label: '已出貨',  className: 'status-active',  color: 'var(--emerald-400)' },
      picking:    { label: '揀貨中',  className: 'status-info',    color: 'var(--cobalt-300)' },
      packing:    { label: '包裝中',  className: 'status-info',    color: 'var(--indigo-300)' },
    };
    return map[status] || { label: status, className: 'status-neutral', color: 'var(--slate-500)' };
  };""", content, flags=re.DOTALL)

    # 2. getNextAction
    content = re.sub(r'const getNextAction = \(status: string\) => \{.*?default:           return null;\n    \}\n  \};',
"""const getNextAction = (status: string) => {
    switch (status) {
      case 'pending':    return { label: '開始揀貨', nextStatus: 'picking', icon: <ClipboardCheck size={14} /> };
      case 'picking':    return { label: '完成揀貨', nextStatus: 'packing', icon: <Package size={14} /> };
      case 'packing':    return { label: '執行出貨', nextStatus: 'dispatched', icon: <Truck size={14} /> };
      default:           return null;
    }
  };""", content, flags=re.DOTALL)

    # 3. Warehouse types options
    content = content.replace("{ name: 'type', label: '資料修復'select', options: [", "{ name: 'type', label: '倉庫類型', type: 'select', options: [")
    content = content.replace("{ name: 'warehouseName', label: '資料修復', type: 'text', required: true, placeholder: '輸入倉庫名稱' }", "{ name: 'warehouseName', label: '倉庫名稱', type: 'text', required: true, placeholder: '輸入倉庫名稱' }")

    # 4. Clean up any duplicated quotes remaining
    content = content.replace("''", "', ") # This might fix things like label: '...'', className: '...'
    
    # Actually, the most reliable way to fix label: '...''CLASS' is:
    content = re.sub(r"label: '([^']*)''([^']*)'", r"label: '\1', className: '\2'", content)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Surgical fix complete.")

final_surgical_fix()
