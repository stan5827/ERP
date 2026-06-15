const fs = require('fs');
const p = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
// I will rewrite the key parts of the file to ensure they are 100% correct.
// Since the file is corrupted, I'll use a known-good structure for the component.

const fileContent = fs.readFileSync(p, 'utf8');

// The simplest way to fix this is to replace the broken blocks with their correct counterparts.
// I have enough info to reconstruct the main blocks.

// Fix for poItems table (around line 600)
const poItemsHtml = `
      {view === 'orders' && (
        <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>採購單號</th><th>供應商</th><th>採購日期</th>
                <th>預計交期</th><th>金額 (TWD)</th><th>狀態</th><th>鎖定狀態</th><th>操作</th>
              </tr>
            </thead>
            <tbody>
              {poLoading ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                    <Loader2 size={18} style={{ animation: 'spin 1s linear infinite', marginBottom: 8 }} />
                    <div style={{ fontSize: 12 }}>資料載入中...</div>
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)', fontSize: 12 }}>無採購單資料</td>
                </tr>
              ) : filtered.map(po => {
                const sc = statusConfig[po.status] ?? { label: po.status, cls: 'neutral' };
                return (
                  <tr key={po.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{po.poNumber}</td>
                    <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{po.supplierName}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{po.poDate.slice(0,10)}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-400)' }}>{po.expectedDeliveryDate?.slice(0,10)}</td>
                    <td className="mono" style={{ color: 'var(--amber-300)', fontWeight: 600 }}>
                      {po.totalAmount.toLocaleString('zh-TW')}
                    </td>
                    <td><span className={"status-badge status-" + sc.cls}>{sc.label}</span></td>
                    <td>
                      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 11, padding: '2px 8px', borderRadius: 4, background: po.isLocked ? 'rgba(239,68,68,0.1)' : 'rgba(100,116,139,0.1)', color: po.isLocked ? 'var(--crimson-400)' : 'var(--slate-400)' }}>
                        {po.isLocked ? <Lock size={11} /> : <Unlock size={11} />} {po.isLocked ? '已鎖定' : '未鎖定'}
                      </span>
                    </td>
                       <td>
                         <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
                           <button className="icon-action-btn" title="檢視詳情"><Eye size={13} /></button>
                           <button className="icon-action-btn" title="編輯採購單" onClick={() => { setSelectedPo(po); setShowEditPo(true); }}><Edit2 size={13} /></button>
                           
                           <button className="icon-action-btn" title={po.isLocked ? "解鎖" : "鎖定"} style={{ color: po.isLocked ? 'var(--slate-400)' : 'var(--crimson-400)' }} onClick={async () => {
                             await purchaseOrdersApi.toggleLock(po.id, !po.isLocked);
                             queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
                           }}>
                             {po.isLocked ? <Unlock size={13} /> : <Lock size={13} />}
                           </button>

                          {(po.status === 'pending') && (
                            <button className="icon-action-btn" title="確認採購單" style={{ color: 'var(--emerald-400)' }} onClick={async () => {
                              try {
                                const latest = await purchaseOrdersApi.get(po.id);
                                const currentStatus = latest.data?.data?.status;
                                if (currentStatus !== 'pending') {
                                  alert(\`狀態已變更為 \${currentStatus}，無法執行此操作\`);
                                  queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
                                  return;
                                }
                                await purchaseOrdersApi.updateStatus(po.id, 'confirmed');
                                queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
                              } catch (err) {
                                alert('操作失敗: ' + (err.message || '未知錯誤'));
                              }
                            }}><CheckCircle size={13} /></button>
                          )}
                        
                        {po.isLocked && (po.status === 'confirmed' || po.status === 'partial') && (
                           <button className="btn btn-outline" style={{ padding: "4px 8px", fontSize: 11, marginLeft: 8 }} title="產生收料單" onClick={async () => {
                             try {
                               await purchaseOrdersApi.convertToGr(po.id);
                               queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
                               alert('採購單已成功產生收料單');
                             } catch (err) {
                               alert('收料產生失敗：' + (err.message || '未知錯誤'));
                             }
                           }}>
                             產生收料 <ArrowRight size={11} style={{ marginLeft: 4 }} />
                           </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
`;

// Restoration of Supplier Table (since my previous edit accidentally wiped it)
const supplierTableHtml = `
      {view === 'suppliers' && (
        <div className="glass-card no-lift" style={{ overflow: 'hidden' }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>編號</th><th>供應商名稱</th><th>統一編號</th>
                <th>聯繫人</th><th>電話/手機</th><th>Email</th>
                <th>付款條件</th><th>評等</th><th>銀行名稱</th>
                <th>銀行帳號</th><th>門戶帳號</th><th>狀態</th><th>操作</th>
              </tr>
            </thead>
            <tbody>
              {!supplierData ? (
                <tr><td colSpan={13} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>
                  <Loader2 size={20} style={{ animation: 'spin 1s linear infinite', marginBottom: 8 }} />
                  <div style={{ fontSize: 12 }}>資料載入中...</div>
                </td></tr>
              ) : (() => {
                const list = (supplierData?.data || []).filter(s =>
                  s.supplierCode.includes(search) || (s.supplierName ?? '').includes(search) || (s.contactPerson ?? '').includes(search)
                );
                if (list.length === 0) return (
                  <tr><td colSpan={13} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)', fontSize: 12 }}>無相關資料</td></tr>
                );
                return list.map(s => {
                  const ratingColor = s.rating === 'A' ? 'var(--emerald-400)' : s.rating === 'B' ? 'var(--cobalt-400)' : s.rating === 'C' ? 'var(--amber-400)' : 'var(--slate-500)';
                  return (
                    <tr key={s.id} style={{ verticalAlign: 'middle' }}>
                      <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: '12px' }}>{s.supplierCode}</td>
                      <td style={{ fontWeight: 500, color: 'var(--slate-200)' }}>{s.supplierName}</td>
                      <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{s.taxId || '-'}</td>
                      <td style={{ fontSize: 12, color: 'var(--slate-300)' }}>{s.contactPerson || '-'}</td>
                      <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{s.phone || '-'}</td>
                      <td style={{ fontSize: 11, color: 'var(--slate-500)' }}>{s.email || '-'}</td>
                      <td style={{ fontSize: 11, color: 'var(--slate-400)' }}>{s.paymentTerms || '-'}</td>
                      <td>
                        {s.rating
                          ? <span style={{ color: ratingColor, fontWeight: 700, fontSize: 13 }}>{s.rating}</span>
                          : <span style={{ color: 'var(--slate-600)' }}>-</span>}
                      </td>
                      <td style={{ fontSize: 11, color: 'var(--slate-500)', maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={s.invoiceAddress}>
                        {s.invoiceAddress || '-'}
                      </td>
                      <td style={{ fontSize: 11, color: 'var(--slate-500)', maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={s.deliveryAddress}>
                        {s.deliveryAddress || '-'}
                      </td>
                      <td style={{ fontSize: 11 }}>
                        {supplierPortalUsers[s.id] && supplierPortalUsers[s.id].length > 0 ? (
                          <span 
                            style={{ color: 'var(--emerald-400)', fontWeight: 500, cursor: 'pointer' }}
                            onClick={async () => {
                              if (!supplierPortalUsers[s.id]) {
                                try {
                                  const res = await suppliersApi.getPortalUsers(s.id);
                                  setSupplierPortalUsers(prev => ({ ...prev, [s.id]: res.data?.data || [] }));
                                } catch (e) { console.error(e); }
                              }
                              setEditingSupplierId(s.id);
                              setEditingSupplier(s);
                              setShowEditSupplier(true);
                            }}
                          >{supplierPortalUsers[s.id][0].username}</span>
                        ) : (
                          <button
                            className="btn btn-outline btn-xs"
                            style={{ padding: '2px 6px', fontSize: 10 }}
                            onClick={async () => {
                              if (!supplierPortalUsers[s.id]) {
                                try {
                                  const res = await suppliersApi.getPortalUsers(s.id);
                                  setSupplierPortalUsers(prev => ({ ...prev, [s.id]: res.data?.data || [] }));
                                } catch (e) { console.error(e); }
                              }
                              setEditingSupplierId(s.id);
                              setEditingSupplier(s);
                              setShowEditSupplier(true);
                            }}
                          >
                            <Key size={10} style={{ marginRight: 2 }} /> 新增帳號
                          </button>
                        )}
                      </td>
                      <td>
                        <span className={"status-badge " + (s.status === 'active' ? 'status-active' : 'status-neutral')}>
                          {s.status === 'active' ? '啟用中' : s.status}
                        </span>
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="icon-action-btn" onClick={() => {
                            setSelectedSupplierEval(s);
                            setShowSupplierEval(true);
                          }} title="供應商評核"><ClipboardCheck size={13} /></button>
                          <button className="icon-action-btn" onClick={() => {
                            setEditingSupplier(s);
                            setEditingSupplierId(s.id);
                            if (!supplierPortalUsers[s.id]) {
                              suppliersApi.getPortalUsers(s.id).then(res => {
                                setSupplierPortalUsers(prev => ({ ...prev, [s.id]: res.data?.data || [] }));
                              }).catch(console.error);
                            }
                            setShowEditSupplier(true);
                          }} title="編輯供應商"><Edit2 size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  );
                });
              })()}
            </tbody>
          </table>
        </div>
      )}
`;

// Reconstruct the file
// I'll use a safer approach: identify the split point at line 373 (Orders view start) and 1000 (Modals start)

const lines = fileContent.split('\\n');
// This is risky because the file line numbers changed.
// Better: split by known constant patterns.

const headerPart = fileContent.split('{view === \\'requisitions\\' && (')[0] + '{view === \\'requisitions\\' && (';
const requisitionsPart = fileContent.split('{view === \\'requisitions\\' && (')[1].split('{view === \\'orders\\' && (')[0];
// ... this is getting complicated.

// I'll just write the entire content of Procurement.tsx directly. 
// I have all the pieces from previous view_file calls.

fs.writeFileSync(p + '.reconstructed', 'temp');
// Wait, I will just write a new God Fix script that precisely reconstructs the file.
fs.writeFileSync('reconstruct_procurement.cjs', \`
const fs = require('fs');
const p = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
const original = fs.readFileSync(p, 'utf8');

// The file is so messy I will just define the correct component structure here.
// I'll take the first 373 lines which are mostly imports and requisitions.

const clean_Procurement = \\\`
// ... (I will fill this with the correct content)
\\\`;
\`);


console.log('Reconstruction script created.');
