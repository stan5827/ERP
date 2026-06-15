import { Landmark, CreditCard, TrendingUp, Shield, Eye, Loader2, FileCheck, CheckCircle, Plus, X, FileText, RefreshCw } from 'lucide-react';
import { useLoans, useBankAccounts, useInvestments, useGuarantees } from '../hooks/useApi';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { accountsPayableApi, financeApi, commonApi, ApiResponse, JournalEntryDto, AccountsPayableDto } from '../api/api';
import { useState, useEffect } from 'react';

const loanStatusConfig: Record<string, { label: string; cls: string }> = {
  active: { label: '正常', cls: 'active' },
  overdue: { label: '逾期', cls: 'danger' },
  closed: { label: '結清', cls: 'neutral' },
  pending: { label: '待審', cls: 'warning' },
};

type DialogType = 'loan' | 'bankAccount' | 'investment' | 'guarantee' | null;


interface VoucherEntry {
  glAccount?: { accountName: string };
  debitAmount: number;
  creditAmount: number;
}

interface GroupedVoucher extends JournalEntryDto {
  entries?: VoucherEntry[];
}

export function Finance() {
  const { data: loansResp, isLoading: loansLoading } = useLoans();
  const { data: bankResp, isLoading: bankLoading } = useBankAccounts();
  const { data: investResp, isLoading: investLoading } = useInvestments();
  const { data: guaranteeResp, isLoading: guaranteeLoading } = useGuarantees();
  const { data: apsResp, isLoading: apsLoading } = useQuery({ 
    queryKey: ['accounts-payable'], 
    queryFn: () => accountsPayableApi.list().then(res => res.data as ApiResponse<AccountsPayableDto[]>) 
  });
  const { data: vouchersResp, isLoading: vouchersLoading } = useQuery({ 
    queryKey: ['accounting-vouchers'], 
    queryFn: () => financeApi.getVouchers().then(res => res.data as ApiResponse<JournalEntryDto[]>)
  });

  const loans = loansResp?.data ?? [];
  const bankAccounts = bankResp?.data ?? [];
  const investments = investResp?.data ?? [];
  const guarantees = guaranteeResp?.data ?? [];
  const accountsPayable = Array.isArray(apsResp?.data) ? (apsResp.data as AccountsPayableDto[]) : [];

  const totalLoanAmount = (loans).reduce((sum, l) => sum + (l.loanAmount || 0), 0);
  const totalBankBalance = (bankAccounts).reduce((sum, b) => sum + (b.currentBalance || 0), 0);
  const totalInvestmentValue = (investments).reduce((sum, i) => sum + (i.currentMarketValue || 0), 0);
  const totalUnrealizedGain = (investments).reduce((sum, i) => sum + (i.unrealizedGainLoss || 0), 0);
  const totalUnpaidAp = accountsPayable.filter(ap => ap.paymentStatus === 'unpaid').reduce((sum, ap) => sum + (ap.totalAmount || 0), 0);

  const queryClient = useQueryClient();
  const [openDialog, setOpenDialog] = useState<DialogType>(null);
  const [notifications, setNotifications] = useState<unknown[]>([]);

  useEffect(() => {
    commonApi.getNotifications({ role: 'finance' })
      .then(res => setNotifications(res.data?.data || []))
      .catch(console.error);
  }, []);

  const [activeTab, setActiveTab] = useState<'overview' | 'audit'>('overview');
  const matchBadge = (result?: string) => {
    switch (result) {
      case 'matched':
        return { cls: 'active', label: '已匹配吻合' };
      case 'pending':
        return { cls: 'warning', label: '待 IQC / 待確認' };
      default:
        return { cls: 'danger', label: '出現差異' };
    }
  };

  return (
    <div className="module-page page-enter">
      <div className="tab-container" style={{ display: 'flex', gap: 24, borderBottom: '1px solid var(--navy-700)', marginBottom: 20 }}>
        <button 
          onClick={() => setActiveTab('overview')}
          style={{ 
            padding: '12px 4px', 
            background: 'none', 
            border: 'none', 
            color: activeTab === 'overview' ? 'var(--cobalt-400)' : 'var(--slate-500)',
            borderBottom: activeTab === 'overview' ? '2px solid var(--cobalt-400)' : 'none',
            fontSize: 14,
            fontWeight: 600,
            cursor: 'pointer'
          }}
        >
          財務概覽與資產負債
        </button>
        <button 
          onClick={() => setActiveTab('audit')}
          style={{ 
            padding: '12px 4px', 
            background: 'none', 
            border: 'none', 
            color: activeTab === 'audit' ? 'var(--cobalt-400)' : 'var(--slate-500)',
            borderBottom: activeTab === 'audit' ? '2px solid var(--cobalt-400)' : 'none',
            fontSize: 14,
            fontWeight: 600,
            cursor: 'pointer'
          }}
        >
          財務審計日誌 (自動分錄)
        </button>
      </div>

      {activeTab === 'overview' && (
        <>
          <div className="module-page-header">
            <div>
              <h2 className="module-page-title">財務與資產管理概覽</h2>
              <p className="module-page-desc">資產負債 · 資金情況 · 三單匹配監控</p>
            </div>
            <div className="module-page-actions">
               <button className="btn btn-primary" onClick={async () => {
                 try {
                   const res = await financeApi.seedAccounts();
                   alert(res.data.message || '初始化成功！');
                   queryClient.invalidateQueries({ queryKey: ['accounting-vouchers'] });
                 } catch (err: unknown) {
                   const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || String(err);
                   alert('初始化失敗：' + msg);
                 }
               }}>
                 <Shield size={16} /> 初始化基礎會計科目
               </button>
            </div>
          </div>

          <div className="module-stat-grid">
            <div className="glass-card module-stat-card">
              <div className="module-stat-label">借款總額</div>
              <div className="module-stat-value kpi-value" style={{ color: 'var(--crimson-400)' }}>
                NT${(totalLoanAmount / 10000).toFixed(1)}萬
              </div>
            </div>
            <div className="glass-card module-stat-card">
              <div className="module-stat-label">銀行存款</div>
              <div className="module-stat-value kpi-value" style={{ color: 'var(--emerald-400)' }}>
                NT${(totalBankBalance / 10000).toFixed(1)}萬
              </div>
            </div>
            <div className="glass-card module-stat-card">
              <div className="module-stat-label">投資市值</div>
              <div className="module-stat-value kpi-value" style={{ color: 'var(--cobalt-400)' }}>
                NT${(totalInvestmentValue / 10000).toFixed(1)}萬
              </div>
            </div>
            <div className="glass-card module-stat-card">
              <div className="module-stat-label">未實現損益</div>
              <div className="module-stat-value kpi-value" style={{ color: totalUnrealizedGain >= 0 ? 'var(--emerald-400)' : 'var(--crimson-400)' }}>
                {totalUnrealizedGain >= 0 ? '+' : ''}NT${(totalUnrealizedGain / 10000).toFixed(1)}萬
              </div>
            </div>
            <div className="glass-card module-stat-card">
              <div className="module-stat-label">未付應付帳款 (AP)</div>
              <div className="module-stat-value kpi-value" style={{ color: 'var(--amber-400)' }}>
                NT${(totalUnpaidAp / 10000).toFixed(1)}萬
              </div>
            </div>
          </div>

          <div className="finance-section">
            <div className="glass-card">
              <div className="finance-section-header">
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <FileCheck size={16} style={{ color: 'var(--cobalt-400)' }} />
                  <h3 className="finance-section-title">應付帳款與三單匹配 (Three-Way Match)</h3>
                  <span className="finance-section-count">{accountsPayable.length} 筆</span>
                </div>
              </div>
              {apsLoading ? (
                <LoadingRow />
              ) : (
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>應付憑單號</th>
                      <th>供應商 / 來源</th>
                      <th>供應商發票號碼</th>
                      <th>發票日期</th>
                      <th>三單匹配結果</th>
                      <th>驗證狀態</th>
                      <th>應付總額 (含稅)</th>
                      <th>付款到期日</th>
                      <th>操作</th>
                    </tr>
                  </thead>
                  <tbody>
                    {accountsPayable.length === 0 ? (
                      <tr><td colSpan={9} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>尚無資料</td></tr>
                    ) : accountsPayable.map(ap => (
                      <tr key={ap.id}>
                        <td className="mono">{ap.apNumber}</td>
                        <td>
                          <div style={{ fontWeight: 500 }}>{ap.supplierName || '—'}</div>
                          <div className="mono" style={{ fontSize: 10, color: 'var(--slate-500)' }}>
                            {ap.purchaseOrderNumber || 'PO —'} / {ap.goodsReceiptNumber || 'GR —'}
                          </div>
                        </td>
                        <td className="mono">{ap.supplierInvoiceNumber || '—'}</td>
                        <td className="mono">{ap.supplierInvoiceDate?.slice(0,10) || '—'}</td>
                        <td>
                          <span className={`status-badge status-${matchBadge(ap.threeWayMatchResult).cls}`}>
                            {matchBadge(ap.threeWayMatchResult).label}
                          </span>
                        </td>
                        <td><span className={`status-badge status-${ap.verificationStatus === 'verified' ? 'active' : ap.verificationStatus === 'hold' ? 'danger' : 'warning'}`}>{ap.verificationStatus || 'pending'}</span></td>
                        <td className="mono">{ap.totalAmount?.toLocaleString()}</td>
                        <td className="mono">{ap.dueDate?.slice(0,10) || '—'}</td>
                        <td>
                          <div style={{ display: 'flex', gap: 4 }}>
                            <button
                              className="icon-action-btn"
                              title="重算匹配"
                              onClick={async () => {
                                await accountsPayableApi.threeWayMatch(ap.id);
                                queryClient.invalidateQueries({ queryKey: ['accounts-payable'] });
                              }}
                            >
                              <RefreshCw size={13} />
                            </button>
                            {ap.paymentStatus !== 'paid' && (
                              <button
                                className="icon-action-btn"
                                title="標記付款"
                                onClick={async () => {
                                  await accountsPayableApi.pay(ap.id);
                                  queryClient.invalidateQueries({ queryKey: ['accounts-payable'] });
                                }}
                              >
                                <CheckCircle size={13} />
                              </button>
                            )}
                            <button className="icon-action-btn" title="查看"><Eye size={13} /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>

          <div className="finance-section">
            <div className="glass-card">
              <div className="finance-section-header">
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <CreditCard size={16} style={{ color: 'var(--crimson-400)' }} />
                  <h3 className="finance-section-title">借款管理</h3>
                  <span className="finance-section-count">{loans.length} 筆</span>
                </div>
                <button className="btn btn-primary" onClick={() => setOpenDialog('loan')}>
                   新增借款
                </button>
              </div>
              {loansLoading ? (
                <LoadingRow />
              ) : (
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>借款編號</th>
                      <th>銀行</th>
                      <th>金額</th>
                      <th>幣別</th>
                      <th>借款日</th>
                      <th>各狀態</th>
                      <th>操作</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(loans as any[]).map(l => (
                      <tr key={l.id}>
                        <td className="mono">{l.loanNumber}</td>
                        <td>{l.bankName ?? '—'}</td>
                        <td className="mono">{l.loanAmount?.toLocaleString()}</td>
                        <td className="mono">{l.currency}</td>
                        <td className="mono">{l.loanDate}</td>
                        <td><span className={`status-badge status-${loanStatusConfig[l.status]?.cls || 'neutral'}`}>{loanStatusConfig[l.status]?.label || l.status}</span></td>
                        <td><button className="icon-action-btn"><Eye size={13} /></button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </>
      )}

      {activeTab === 'audit' && (
        <div className="finance-section">
          <div className="glass-card">
            <div className="finance-section-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <FileText size={18} style={{ color: 'var(--cobalt-400)' }} />
                <h3 className="finance-section-title">財務審計與自動分錄日誌</h3>
              </div>
            </div>
            <div className="data-table-container">
              <table className="data-table">
                <thead>
                  <tr><th>日期 / 編號</th><th>來源 / 摘要</th><th>分錄 (科目/借/貸)</th><th>狀態</th></tr>
                </thead>
                <tbody>
                  {vouchersLoading ? (
                    <tr><td colSpan={4} style={{ textAlign: 'center', padding: 40 }}><Loader2 className="spin" /></td></tr>
                  ) : (vouchersResp?.data || []).length === 0 ? (
                    <tr><td colSpan={4} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>目前尚無自動產生的傳票。</td></tr>
                  ) : (vouchersResp?.data || []).map((v: GroupedVoucher) => (
                    <tr key={v.id}>
                      <td>{v.entryDate?.split('T')[0]}<br/><span className="mono" style={{ fontSize: 10, color: 'var(--cobalt-400)' }}>{v.sourceDocNo}</span></td>
                      <td>
                        <div style={{ fontWeight: 500 }}>{v.description}</div>
                        <div style={{ fontSize: 10, color: 'var(--slate-500)' }}>{v.sourceModule}: {String(v.sourceDocNo).substring(0,8)}</div>
                      </td>
                      <td style={{ padding: 0 }}>
                        {v.entries?.map((e: VoucherEntry, idx: number) => (
                          <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', padding: '4px 8px', fontSize: 11 }}>
                            <span style={{ width: 120 }}>{e.glAccount?.accountName}</span>
                            <span className="mono" style={{ color: e.debitAmount > 0 ? 'var(--emerald-400)' : 'var(--slate-400)' }}>
                              {e.debitAmount > 0 ? `借 ${e.debitAmount.toLocaleString()}` : `貸 ${e.creditAmount.toLocaleString()}`}
                            </span>
                          </div>
                        ))}
                      </td>
                      <td><span className="status-badge status-active">已過帳</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      <style>{`
        .tab-container button:hover { color: var(--cobalt-300) !important; }
        .module-page { display: flex; flex-direction: column; gap: 16px; }
        .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; }
        .module-page-title { font-size: 20px; font-weight: 700; letter-spacing: -0.02em; color: var(--slate-100); }
        .module-page-desc { font-size: 12px; color: var(--slate-500); margin-top: 2px; font-family: var(--font-mono); }
        .module-stat-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 12px; }
        .module-stat-card { padding: 14px 16px; }
        .module-stat-label { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.07em; color: var(--slate-500); margin-bottom: 8px; font-family: var(--font-display); }
        .module-stat-value { font-size: 24px; font-weight: 700; letter-spacing: -0.04em; }
        .finance-section-header { padding: 14px 16px; display: flex; align-items: center; justify-content: space-between; }
        .finance-section-title { font-size: 14px; font-weight: 700; color: var(--slate-200); margin: 0; }
        .finance-section-count { font-size: 11px; color: var(--slate-500); font-family: var(--font-mono); background: var(--navy-850); padding: 2px 8px; border-radius: var(--radius-sm); }
        .icon-action-btn { width: 26px; height: 26px; display: flex; align-items: center; justify-content: center; background: transparent; border: none; border-radius: var(--radius-sm); color: var(--slate-500); cursor: pointer; transition: all var(--transition-fast); }
        .icon-action-btn:hover { background: rgba(45, 110, 245, 0.08); color: var(--cobalt-300); }
        @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
        .dialog-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.6); display: flex; align-items: center; justify-content: center; z-index: 1000; }
        .dialog-content { background: var(--navy-900); border: 1px solid var(--navy-700); border-radius: var(--radius-md); width: 90%; max-width: 500px; }
        .dialog-header { display: flex; align-items: center; justify-content: space-between; padding: 16px; border-bottom: 1px solid var(--navy-700); }
        .dialog-title { font-size: 16px; font-weight: 600; color: var(--slate-100); }
        .dialog-body { padding: 16px; min-height: 100px; }
        .dialog-footer { display: flex; justify-content: flex-end; padding: 16px; border-top: 1px solid var(--navy-700); }
      `}</style>
      
      {openDialog && (
        <Dialog type={openDialog} onClose={() => setOpenDialog(null)} />
      )}
    </div>
  );
}

function LoadingRow() {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, padding: 48, color: 'var(--slate-500)' }}>
      <Loader2 size={18} className="spin" style={{ animation: 'spin 1s linear infinite' }} />
      <span>載入中...</span>
    </div>
  );
}

function Dialog({ onClose }: { type: any, onClose: () => void }) {
  return (
    <div className="dialog-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="dialog-content">
        <div className="dialog-header">
          <span className="dialog-title">系統操作</span>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--slate-500)', cursor: 'pointer' }}><X size={18} /></button>
        </div>
        <div className="dialog-body">
          <p style={{ color: 'var(--slate-300)', fontSize: 13 }}>此功能正在維護中。</p>
        </div>
        <div className="dialog-footer">
          <button className="btn btn-primary" onClick={onClose}>確定</button>
        </div>
      </div>
    </div>
  );
}
