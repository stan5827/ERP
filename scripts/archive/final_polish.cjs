const fs = require('fs');
const p = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let c = fs.readFileSync(p, 'utf8').split('\n');

// 1. Keep everything before the broken _summary
// Based on view_file, 1042 is the last clean line before _summary starts
const cleanLines = c.slice(0, 1042);

// 2. Add fixed _summary for EditPo modal
cleanLines.push(`            _summary: (
              <div style={{ padding: 20 }}>
                <h4 style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-200)', marginBottom: 12 }}>採購單匯總</h4>
                <div style={{ fontSize: 13, color: 'var(--slate-400)' }}>
                  <p>供應商：{selectedPo?.supplierName || '-'}</p>
                  <p>採購日期：{selectedPo?.poDate?.slice(0, 10) || '-'}</p>
                  <p style={{ marginTop: 8, color: 'var(--amber-400)', fontWeight: 700, fontSize: 16 }}>總計金額：NT$ {poItems.reduce((acc, item) => acc + (item.quantity * item.unitPrice), 0).toLocaleString('zh-TW')}</p>
                </div>
              </div>
            )
          }}
          onClose={() => { setShowEditPo(false); setSelectedPo(null); }}
          onSubmit={async (values) => {
            await purchaseOrdersApi.update(selectedPo!.id, values);
            queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
            setShowEditPo(false);
          }}
        />`);

// 3. Add Fixed PR Detail Modal
cleanLines.push(`
        {/* 請購單詳情彈窗 */}
        <div style={{
          display: showPrDetail ? 'flex' : 'none',
          position: 'fixed', inset: 0, zIndex: 1000,
          alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.7)',
        }} onClick={() => setShowPrDetail(false)}>
          <div style={{ width: 800, maxHeight: '80vh', background: 'var(--slate-900)', borderRadius: 12, display: 'flex', flexDirection: 'column' }} onClick={e => e.stopPropagation()}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 20px', borderBottom: '1px solid var(--glass-border)' }}>
              <h3 style={{ margin: 0, fontSize: 16, color: 'var(--slate-100)' }}>請購單詳情</h3>
              <button onClick={() => setShowPrDetail(false)} style={{ background: 'transparent', border: 'none', color: 'var(--slate-400)', fontSize: 20 }}>×</button>
            </div>
            <div style={{ padding: 20, overflow: 'auto', flex: 1 }}>
              {loadingPrDetail ? <div style={{ textAlign: 'center', padding: 20 }}>載入中...</div> : selectedPr && (
                <>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16, marginBottom: 20 }}>
                    <div><span style={{ color: 'var(--slate-500)', fontSize: 11 }}>請購單號</span><div>{selectedPr.prNumber}</div></div>
                    <div><span style={{ color: 'var(--slate-500)', fontSize: 11 }}>請購日期</span><div>{selectedPr.prDate?.slice(0,10)}</div></div>
                    <div><span style={{ color: 'var(--slate-500)', fontSize: 11 }}>狀態</span><div>{getPrStatusLabel(selectedPr.status)}</div></div>
                    <div><span style={{ color: 'var(--slate-500)', fontSize: 11 }}>總金額</span><div>{selectedPr.totalEstimatedAmount?.toLocaleString()}</div></div>
                  </div>
                  <table className="data-table">
                    <thead><tr><th>品名規格</th><th>數量</th><th>單位</th><th>預估單價</th></tr></thead>
                    <tbody>{(selectedPr.items || []).map((it:any) => (
                      <tr key={it.id}><td>{it.skuName}</td><td>{it.quantity}</td><td>{it.unit}</td><td>{it.estimatedUnitPrice}</td></tr>
                    ))}</tbody>
                  </table>
                </>
              )}
            </div>
          </div>
        </div>`);

// 4. Add Fixed Supplier Eval Modal
cleanLines.push(`
        <div style={{ display: showSupplierEval ? 'flex' : 'none', position: 'fixed', inset: 0, zIndex: 1000, alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.7)' }} onClick={() => setShowSupplierEval(false)}>
          <div style={{ width: 600, background: 'var(--slate-900)', borderRadius: 12 }} onClick={e => e.stopPropagation()}>
            <div style={{ padding: 20, borderBottom: '1px solid var(--glass-border)' }}>
              <h3 style={{ margin: 0 }}>供應商評核 - {selectedSupplierEval?.supplierName}</h3>
            </div>
            <div style={{ padding: 20 }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16 }}>
                <div style={{ textAlign: 'center' }}><div>交期</div><div style={{ fontSize: 24, fontWeight: 700 }}>92</div></div>
                <div style={{ textAlign: 'center' }}><div>品質</div><div style={{ fontSize: 24, fontWeight: 700 }}>88</div></div>
                <div style={{ textAlign: 'center' }}><div>配合度</div><div style={{ fontSize: 24, fontWeight: 700 }}>85</div></div>
              </div>
            </div>
            <div style={{ padding: 15, textAlign: 'right' }}><button className="btn btn-primary" onClick={() => setShowSupplierEval(false)}>關閉</button></div>
          </div>
        </div>`);

// 5. Append the remaining style and export blocks (from original truncated version)
cleanLines.push(\`
        <style>{\\\`
          .module-page { display: flex; flex-direction: column; gap: 16px; }
          .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; }
          .module-page-title { font-size: 20px; font-weight: 700; color: var(--slate-100); }
          .module-page-desc { font-size: 12px; color: var(--slate-500); }
          .module-stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
          .module-stat-card { padding: 14px 16px; min-height: 80px; }
          .module-stat-label { font-size: 11px; font-weight: 700; color: var(--slate-500); margin-bottom: 8px; }
          .module-stat-value { font-weight: 700; letter-spacing: -0.03em; }
          .icon-action-btn { width: 26px; height: 26px; display: flex; align-items: center; justify-content: center; background: transparent; border: none; cursor: pointer; color: var(--slate-500); }
          .icon-action-btn:hover { color: var(--cobalt-300); }
        \\\`}</style>
      </div>
    );
  };
export default Procurement;
\`);

fs.writeFileSync(p, cleanLines.join('\\n'), 'utf8');
console.log('Final Polish Complete');
