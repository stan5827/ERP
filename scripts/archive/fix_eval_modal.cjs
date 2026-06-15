const fs = require('fs');
const p = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let c = fs.readFileSync(p, 'utf8').split('\n');

// Help functions to find blocks
const findLine = (pattern) => c.findIndex(l => l.includes(pattern));

// 1. Fix the selectedSupplierEval block
const evalStart = findLine("供應商季評核詳情");
const evalEnd = findLine("編輯供應商彈窗");

if (evalStart !== -1 && evalEnd !== -1) {
    const cleanEval = `              <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600, color: 'var(--slate-100)' }}>
                供應商季評核詳情 - {selectedSupplierEval?.supplierName}
              </h3>
              <button onClick={() => setShowSupplierEval(false)} style={{ background: 'transparent', border: 'none', color: 'var(--slate-400)', cursor: 'pointer', fontSize: 20 }}>×</button>
            </div>
            
            <div style={{ padding: 20, overflow: 'auto', flex: 1 }}>
              {selectedSupplierEval && (
                <>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16, marginBottom: 24 }}>
                    <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
                      <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 8 }}>交期達成率</div>
                      <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--emerald-400)' }}>92</div>
                    </div>
                    <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
                      <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 8 }}>品質合格率</div>
                      <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--cobalt-400)' }}>88</div>
                    </div>
                    <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
                      <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 8 }}>配合度評分</div>
                      <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--amber-400)' }}>85</div>
                    </div>
                  </div>
                  
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 20 }}>
                    <div><span style={{ color: 'var(--slate-500)', fontSize: 12 }}>當前等級</span><div style={{ color: (selectedSupplierEval?.rating === 'A' ? 'var(--emerald-400)' : 'var(--amber-400)'), fontWeight: 700, fontSize: 20 }}>{selectedSupplierEval?.rating || 'A'}</div></div>
                    <div><span style={{ color: 'var(--slate-500)', fontSize: 12 }}>最後評核日</span><div style={{ color: 'var(--slate-200)' }}>2026-03-15</div></div>
                  </div>
                  
                  <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-200)', marginBottom: 12 }}>歷史評核記錄</div>
                  <table className="data-table" style={{ background: 'var(--slate-800)', borderRadius: 6 }}>
                    <thead>
                      <tr>
                        <th>評核日期</th>
                        <th>交期</th>
                        <th>品質</th>
                        <th>配合度</th>
                        <th>平均</th>
                        <th>等級</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td>2026-03-15</td>
                        <td>92</td>
                        <td>88</td>
                        <td>85</td>
                        <td>88.3</td>
                        <td><span className="badge success">A</span></td>
                      </tr>
                      <tr>
                        <td>2025-12-20</td>
                        <td>85</td>
                        <td>82</td>
                        <td>90</td>
                        <td>85.7</td>
                        <td><span className="badge info">B</span></td>
                      </tr>
                    </tbody>
                  </table>
                </>
              )}
            </div>
          </div>
        </div>
`;
    c.splice(evalStart + 1, evalEnd - evalStart - 2, cleanEval);
}

// 2. Fix the file end and balancing
let content = c.join('\n');
// Strip the previous brute-force additions
content = content.split('export default Procurement;')[0] + 'export default Procurement;';

// Clean up any extra closing braces before export
while (content.includes('}\n}\n}\nexport default Procurement;')) {
    content = content.replace('}\n}\n}\nexport default Procurement;', '}\n}\nexport default Procurement;');
}

fs.writeFileSync(p, content, 'utf8');
console.log('Fixed Eval Modal and cleaned up duplicates.');
