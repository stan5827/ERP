import os
import re

def absolute_final_restoration():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Fix CountPanel
    content = re.sub(r'function CountPanel\(\) \{.*?\}\n\n/\*', 
"""function CountPanel() {
  const [countMode, setCountMode] = useState<'pending' | 'in_progress' | 'completed'>('pending');
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', gap: 16 }}>
        {[
          { label: '待盤點', value: 0, color: 'var(--amber-400)' },
          { label: '盤點中', value: 0, color: 'var(--cobalt-400)' },
          { label: '已完成', value: 0, color: 'var(--emerald-400)' },
          { label: '待確認', value: 0, color: 'var(--crimson-400)' },
        ].map(s => (
          <div key={s.label} className="glass-card" style={{ flex: 1, padding: 16 }}>
            <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 4 }}>{s.label}</div>
            <div style={{ fontSize: 28, fontWeight: 700, color: s.color }}>{s.value}</div>
          </div>
        ))}
      </div>
      <div className="module-filter-bar glass-card no-lift" style={{ justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', gap: 8 }}>
          {['pending', 'in_progress', 'completed'].map(m => (
            <button key={m} className={`btn btn-sm ${countMode === m ? 'btn-primary' : 'btn-ghost'}`} onClick={() => setCountMode(m as any)}>
              {m === 'pending' ? '待盤點' : m === 'in_progress' ? '盤點中' : '已完成'}
            </button>
          ))}
        </div>
        <button className="btn btn-primary btn-sm" disabled title="功能開發中">
          <Plus size={13} />建立盤點計畫
        </button>
      </div>
    </div>
  );
}

/*""", content, flags=re.DOTALL)

    # 2. Fix ExpiryAlertPanel + AnalysisPanel summaryCards
    content = content.replace("{ label: '資料修復'', sub: '< 7", "{ label: '緊急過期', sub: '< 7")
    content = content.replace("{ label: '資料修復'', sub: '< 30", "{ label: '即將過期', sub: '< 30")
    content = content.replace("{ label: '資料修復'', value: '12 隞?'", "{ label: '低周轉品', value: '12 項'")
    content = content.replace("{ label: '資料修復'', value: '5 隞?'", "{ label: '超齡庫存', value: '5 項'")

    # 3. Last sweep for duplicated single quotes
    content = content.replace("''", "'") 

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Absolute final restoration complete.")

absolute_final_restoration()
