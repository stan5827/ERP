import os
import re

def fix_analysis_panel():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    new_panel = """function AnalysisPanel() {
  const abcData = [
    { cls: "A", label: "高價值存貨", pct: 80, items: 20, color: "var(--cobalt-400)", desc: "價值佔比 80%, 數量僅佔 20%" },
    { cls: "B", label: "中價值存貨", pct: 15, items: 30, color: "var(--amber-400)", desc: "價值佔比 15%, 數量佔 30%" },
    { cls: "C", label: "低價值存貨", pct: 5,  items: 50, color: "var(--slate-400)", desc: "價值佔比 5%, 數量佔 50%" },
  ];

  const summaryCards = [
    { label: '總庫存資產市值', value: 'NT$ 3,842,600', color: 'var(--cobalt-300)' },
    { label: '低於安全庫存', value: '12 筆', color: 'var(--amber-300)' },
    { label: '在手量零值警示', value: '5 筆', color: 'var(--crimson-300)' },
    { label: '最後一次 MRP 執行', value: '今日 08:00', color: 'var(--emerald-300)' },
  ];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16' }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '12' }}>
        {summaryCards.map(c => (
          <div key={c.label} className="glass-card" style={{ padding: '14px 16px' }}>
            <div style={{ fontSize: 11, color: 'var(--slate-500)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: '8' }}>{c.label}</div>
            <div style={{ fontSize: 18, fontWeight: 700, color: c.color, fontFamily: 'var(--font-mono)' }}>{c.value}</div>
          </div>
        ))}
      </div>

      <div className="glass-card no-lift" style={{ padding: '20' }}>
        <h3 style={{ fontSize: 14, fontWeight: 700, marginBottom: 16, color: "var(--slate-100)" }}>ABC 庫存分級分析 (價值佔比)</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '14' }}>
          {abcData.map(item => (
            <div key={item.cls}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: '6' }}>
                <div style={{ width: 24, height: 24, borderRadius: 6, background: `${item.color}20`, border: `1px solid ${item.color}40`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, fontWeight: 800, color: item.color }}>
                  {item.cls}
                </div>
                <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>{item.label}</span>
                <span style={{ fontSize: 11, color: 'var(--slate-500)', marginLeft: '4' }}>{item.desc}</span>
                <span style={{ marginLeft: 'auto', fontSize: 12, fontWeight: 700, color: item.color }}>{item.pct}%</span>
              </div>
              <div className="progress-bar">
                <div className="progress-fill" style={{ width: `${item.pct}%`, background: item.color }} />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="glass-card no-lift" style={{ padding: '20' }}>
        <h3 style={{ fontSize: 14, fontWeight: 700, marginBottom: 8, color: "var(--slate-100)" }}>週轉率 / 呆滯量警示
          <span style={{ fontSize: 11, fontWeight: 400, color: "var(--slate-500)", marginLeft: '8' }}>超過 180 天未異動庫存</span>
        </h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6' }}>
          {[].length === 0 && (
            <div style={{ padding: '20px', textAlign: 'center', color: 'var(--slate-500)', fontSize: '12' }}>
              尚無符合條件的呆滯庫存紀錄
            </div>
          )}
        </div>
      </div>
    </div>
  );
}"""

    # Replace the whole function
    content = re.sub(r'function AnalysisPanel\(\) \{.*?\}', new_panel, content, flags=re.DOTALL)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("AnalysisPanel fixed.")

fix_analysis_panel()
