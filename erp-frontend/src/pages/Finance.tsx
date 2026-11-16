import { Landmark, CreditCard, Shield, Eye, Loader2, FileCheck, CheckCircle, Plus, X, FileText, Pencil, Send, Search } from 'lucide-react';
import { useLoans, useBankAccounts, useInvestments } from '../hooks/useApi';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { accountsPayableApi, financeApi, ApiResponse } from '../api/api';
import {
  AccountBalanceDto,
  AccountingPeriodDto,
  AccountingVoucherDto,
  FinanceGlAccountDto,
  GeneralLedgerEntryDto,
  JournalEntryDto,
  SaveAccountingVoucherRequest,
} from '../api/finance';
import { AccountsPayableDto } from '../api/procurement';
import { useState } from 'react';
import './Finance.css';

const loanStatusConfig: Record<string, { label: string; cls: string }> = {
  active: { label: '正常', cls: 'active' },
  overdue: { label: '逾期', cls: 'danger' },
  closed: { label: '結清', cls: 'neutral' },
  pending: { label: '待審', cls: 'warning' },
};

type DialogType = 'loan' | 'bankAccount' | 'investment' | 'guarantee' | null;


interface VoucherLineForm {
  glAccountId: string;
  debitAmount: string;
  creditAmount: string;
  memo: string;
}

const createEmptyVoucherLine = (): VoucherLineForm => ({
  glAccountId: '',
  debitAmount: '',
  creditAmount: '',
  memo: '',
});

export function Finance() {
  const [activeTab, setActiveTab] = useState<'overview' | 'audit'>('overview');
  const [voucherDialogOpen, setVoucherDialogOpen] = useState(false);
  const [editingVoucherId, setEditingVoucherId] = useState<string | null>(null);
  const [voucherDate, setVoucherDate] = useState(new Date().toISOString().slice(0, 10));
  const [voucherType, setVoucherType] = useState('Journal');
  const [voucherDescription, setVoucherDescription] = useState('');
  const [voucherLines, setVoucherLines] = useState<VoucherLineForm[]>([
    createEmptyVoucherLine(),
    createEmptyVoucherLine(),
  ]);
  const [journalFromDate, setJournalFromDate] = useState('');
  const [journalToDate, setJournalToDate] = useState('');
  const [journalAccountId, setJournalAccountId] = useState('');
  const [journalVoucherNumber, setJournalVoucherNumber] = useState('');
  const [journalPage, setJournalPage] = useState(1);
  const [ledgerPage, setLedgerPage] = useState(1);
  const [periodCode, setPeriodCode] = useState('');
  const [periodStartDate, setPeriodStartDate] = useState('');
  const [periodEndDate, setPeriodEndDate] = useState('');
  const [selectedVoucherId, setSelectedVoucherId] = useState<string | null>(null);
  const { data: loansResp, isLoading: loansLoading } = useLoans();
  const { data: bankResp } = useBankAccounts();
  const { data: investResp } = useInvestments();
  const { data: apsResp, isLoading: apsLoading } = useQuery({ 
    queryKey: ['accounts-payable'], 
    queryFn: () => accountsPayableApi.list().then(res => res.data as ApiResponse<AccountsPayableDto[]>) 
  });
  const { data: vouchersResp, isLoading: vouchersLoading } = useQuery({ 
    queryKey: ['accounting-vouchers'], 
    queryFn: () => financeApi.getVouchers().then(res => res.data as ApiResponse<AccountingVoucherDto[]>)
  });
  const { data: chartOfAccountsResp } = useQuery({
    queryKey: ['finance-chart-of-accounts'],
    queryFn: () => financeApi.getChartOfAccounts().then(res => res.data as ApiResponse<FinanceGlAccountDto[]>),
  });
  const journalParams: Record<string, string> = {
    page: String(journalPage),
    pageSize: '50',
  };
  if (journalFromDate) journalParams.fromDate = journalFromDate;
  if (journalToDate) journalParams.toDate = journalToDate;
  if (journalAccountId) journalParams.accountId = journalAccountId;
  if (journalVoucherNumber.trim()) journalParams.voucherNumber = journalVoucherNumber.trim();
  const { data: journalResp, isLoading: journalLoading } = useQuery({
    queryKey: ['accounting-journal', journalParams],
    queryFn: () => financeApi.getJournalBook(journalParams).then(res => res.data),
    enabled: activeTab === 'audit',
  });
  const reportParams: Record<string, string> = {};
  if (journalFromDate) reportParams.fromDate = journalFromDate;
  if (journalToDate) reportParams.toDate = journalToDate;
  const { data: trialBalanceResp, isLoading: trialBalanceLoading } = useQuery({
    queryKey: ['accounting-trial-balance', reportParams],
    queryFn: () => financeApi.getTrialBalance(reportParams).then(res => res.data),
    enabled: activeTab === 'audit' && Boolean(journalToDate),
  });
  const { data: statementsResp, isLoading: statementsLoading } = useQuery({
    queryKey: ['accounting-financial-statements', journalFromDate, journalToDate],
    queryFn: () => financeApi.getFinancialStatements(reportParams).then(res => res.data),
    enabled: activeTab === 'audit' && Boolean(journalFromDate && journalToDate),
  });
  const ledgerParams: Record<string, string> = {
    accountId: journalAccountId,
    page: String(ledgerPage),
    pageSize: '100',
  };
  if (journalFromDate) ledgerParams.fromDate = journalFromDate;
  if (journalToDate) ledgerParams.toDate = journalToDate;
  const { data: ledgerResp, isLoading: ledgerLoading } = useQuery({
    queryKey: ['accounting-general-ledger', ledgerParams],
    queryFn: () => financeApi.getGeneralLedger(ledgerParams).then(res => res.data),
    enabled: activeTab === 'audit' && Boolean(journalAccountId),
  });
  const { data: periodsResp } = useQuery({
    queryKey: ['accounting-periods'],
    queryFn: () => financeApi.getAccountingPeriods().then(res => res.data),
    enabled: activeTab === 'audit',
  });
  const { data: selectedVoucherResp } = useQuery({
    queryKey: ['accounting-voucher', selectedVoucherId],
    queryFn: () => financeApi.getVoucher(selectedVoucherId!).then(res => res.data),
    enabled: Boolean(selectedVoucherId),
  });

  const loans = loansResp?.data ?? [];
  const bankAccounts = bankResp?.data ?? [];
  const investments = investResp?.data ?? [];
  const accountsPayable = Array.isArray(apsResp?.data) ? (apsResp.data as AccountsPayableDto[]) : [];

  const totalLoanAmount = (loans).reduce((sum, l) => sum + (l.loanAmount || 0), 0);
  const totalBankBalance = (bankAccounts).reduce((sum, b) => sum + (b.currentBalance || 0), 0);
  const totalInvestmentValue = (investments).reduce((sum, i) => sum + (i.currentMarketValue || 0), 0);
  const totalUnrealizedGain = (investments).reduce((sum, i) => sum + (i.unrealizedGainLoss || 0), 0);
  const totalUnpaidAp = accountsPayable.filter(ap => ap.paymentStatus === 'unpaid').reduce((sum, ap) => sum + (ap.totalAmount || 0), 0);

  const queryClient = useQueryClient();
  const [openDialog, setOpenDialog] = useState<DialogType>(null);
  const chartOfAccounts = (chartOfAccountsResp?.data ?? []).filter(account => account.isActive);
  const voucherList = vouchersResp?.data ?? [];
  const accountingPeriods = periodsResp?.data ?? [];
  const selectedVoucher = selectedVoucherResp?.data;

  const resetVoucherForm = () => {
    setEditingVoucherId(null);
    setVoucherDate(new Date().toISOString().slice(0, 10));
    setVoucherType('Journal');
    setVoucherDescription('');
    setVoucherLines([createEmptyVoucherLine(), createEmptyVoucherLine()]);
    setVoucherDialogOpen(false);
  };

  const startNewVoucher = () => {
    setEditingVoucherId(null);
    setVoucherDate(new Date().toISOString().slice(0, 10));
    setVoucherType('Journal');
    setVoucherDescription('');
    setVoucherLines([createEmptyVoucherLine(), createEmptyVoucherLine()]);
    setVoucherDialogOpen(true);
  };

  const editVoucher = (voucher: AccountingVoucherDto) => {
    setEditingVoucherId(voucher.id);
    setVoucherDate(voucher.voucherDate.slice(0, 10));
    setVoucherType(voucher.voucherType);
    setVoucherDescription(voucher.description ?? '');
    setVoucherLines(voucher.entries.map(entry => ({
      glAccountId: entry.glAccountId,
      debitAmount: entry.debitAmount ? String(entry.debitAmount) : '',
      creditAmount: entry.creditAmount ? String(entry.creditAmount) : '',
      memo: entry.memo ?? '',
    })));
    setVoucherDialogOpen(true);
  };

  const saveManualVoucher = async () => {
    if (!voucherDate || voucherLines.length < 2 || voucherLines.some(line => !line.glAccountId)) {
      alert('請填寫傳票日期、至少兩筆分錄及各筆會計科目。');
      return;
    }

    const request: SaveAccountingVoucherRequest = {
      voucherDate,
      voucherType,
      description: voucherDescription.trim() || undefined,
      entries: voucherLines.map(line => ({
        glAccountId: line.glAccountId,
        debitAmount: Number(line.debitAmount) || 0,
        creditAmount: Number(line.creditAmount) || 0,
        memo: line.memo.trim() || undefined,
      })),
    };

    try {
      if (editingVoucherId) {
        await financeApi.updateVoucher(editingVoucherId, request);
      } else {
        await financeApi.createVoucher(request);
      }
      await queryClient.invalidateQueries({ queryKey: ['accounting-vouchers'] });
      resetVoucherForm();
    } catch (error: unknown) {
      const message = (error as { response?: { data?: { message?: string } } })
        ?.response?.data?.message || String(error);
      alert(`傳票草稿儲存失敗：${message}`);
    }
  };

  const submitManualVoucher = async (voucher: AccountingVoucherDto) => {
    if (!window.confirm(`確定將傳票 ${voucher.voucherNumber} 送交覆核？`))
      return;
    try {
      await financeApi.submitVoucher(voucher.id);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['accounting-vouchers'] }),
      ]);
    } catch (error: unknown) {
      const message = (error as { response?: { data?: { message?: string } } })
        ?.response?.data?.message || String(error);
      alert(`傳票送審失敗：${message}`);
    }
  };

  const approveManualVoucher = async (voucher: AccountingVoucherDto) => {
    if (!window.confirm(`確定核准傳票 ${voucher.voucherNumber} 並正式過帳？過帳後不可直接修改。`))
      return;
    try {
      await financeApi.approveVoucher(voucher.id);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['accounting-vouchers'] }),
        queryClient.invalidateQueries({ queryKey: ['accounting-journal'] }),
      ]);
    } catch (error: unknown) {
      const message = (error as { response?: { data?: { message?: string } } })
        ?.response?.data?.message || String(error);
      alert(`傳票核准失敗：${message}`);
    }
  };

  const createAccountingPeriod = async () => {
    if (!periodCode.trim() || !periodStartDate || !periodEndDate) {
      alert('請輸入期間代碼、起日及迄日。');
      return;
    }
    try {
      await financeApi.createAccountingPeriod({
        periodCode: periodCode.trim(),
        startDate: periodStartDate,
        endDate: periodEndDate,
      });
      setPeriodCode('');
      setPeriodStartDate('');
      setPeriodEndDate('');
      await queryClient.invalidateQueries({ queryKey: ['accounting-periods'] });
    } catch (error: unknown) {
      const message = (error as { response?: { data?: { message?: string } } })
        ?.response?.data?.message || String(error);
      alert(`會計期間建立失敗：${message}`);
    }
  };

  const closeAccountingPeriod = async (period: AccountingPeriodDto) => {
    if (!window.confirm(`確定關閉 ${period.periodCode}？關帳前必須完成期間內所有傳票，且已過帳傳票借貸平衡。`))
      return;
    try {
      await financeApi.closeAccountingPeriod(period.id);
      await queryClient.invalidateQueries({ queryKey: ['accounting-periods'] });
    } catch (error: unknown) {
      const message = (error as { response?: { data?: { message?: string } } })
        ?.response?.data?.message || String(error);
      alert(`會計期間關帳失敗：${message}`);
    }
  };

  const updateVoucherLine = (index: number, patch: Partial<VoucherLineForm>) => {
    setVoucherLines(current => current.map((line, lineIndex) =>
      lineIndex === index ? { ...line, ...patch } : line));
  };

  return (
    <div className={`module-page page-enter finance-page ${activeTab === 'audit' ? 'finance-audit' : ''}`}>
      <div className="tab-container finance-tabs">
        <button 
          onClick={() => setActiveTab('overview')}
          className={`finance-tab ${activeTab === 'overview' ? 'is-active' : ''}`}
        >
          財務概覽與資產負債
        </button>
        <button 
          onClick={() => setActiveTab('audit')}
          className={`finance-tab ${activeTab === 'audit' ? 'is-active' : ''}`}
        >
          財務帳簿與月結
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
                      <th>供應商發票號碼</th>
                      <th>發票日期</th>
                      <th>三單匹配結果</th>
                      <th>應付總額 (含稅)</th>
                      <th>付款到期日</th>
                      <th>操作</th>
                    </tr>
                  </thead>
                  <tbody>
                    {accountsPayable.length === 0 ? (
                      <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>尚無資料</td></tr>
                    ) : accountsPayable.map(ap => (
                      <tr key={ap.id}>
                        <td className="mono">{ap.apNumber}</td>
                        <td className="mono">{ap.supplierInvoiceNumber || '—'}</td>
                        <td className="mono">{ap.supplierInvoiceDate?.slice(0,10) || '—'}</td>
                        <td>
                          <span className={`status-badge status-${ap.threeWayMatchResult === 'matched' ? 'active' : 'danger'}`}>
                            {ap.threeWayMatchResult === 'matched' ? '已匹配吻合' : '出現差異'}
                          </span>
                        </td>
                        <td className="mono">{ap.totalAmount?.toLocaleString()}</td>
                        <td className="mono">{ap.dueDate?.slice(0,10) || '—'}</td>
                        <td>
                          <button className="icon-action-btn"><Eye size={13} /></button>
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
                    {loans.map(l => (
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
        <>
          <section className="finance-audit-intro">
            <div className="finance-audit-intro__icon"><Landmark size={22} /></div>
            <div>
              <p className="finance-audit-intro__eyebrow">FINANCE · GENERAL LEDGER</p>
              <h1>財務帳簿與月結</h1>
              <p>從人工傳票到總帳與財務報表，追蹤每一筆已過帳的會計資料。</p>
            </div>
            <div className="finance-audit-intro__note"><Shield size={15} /> 僅統計已過帳分錄</div>
          </section>

          <div className="finance-section finance-audit-section finance-vouchers">
            <div className="glass-card">
              <div className="finance-section-header">
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <FileText size={18} style={{ color: 'var(--cobalt-400)' }} />
                  <h3 className="finance-section-title">會計傳票</h3>
                  <span className="finance-section-count">{voucherList.length} 筆（最近 100 筆）</span>
                </div>
                <button className="btn btn-primary" onClick={startNewVoucher}>
                  <Plus size={15} /> 新增人工傳票
                </button>
              </div>
              <div className="data-table-container">
                <table className="data-table">
                  <thead>
                    <tr><th>日期 / 傳票編號</th><th>類型 / 摘要</th><th>借方總額</th><th>貸方總額</th><th>狀態</th><th>操作</th></tr>
                  </thead>
                  <tbody>
                    {vouchersLoading ? (
                      <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40 }}><Loader2 className="spin" /></td></tr>
                    ) : voucherList.length === 0 ? (
                      <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>目前尚無會計傳票。</td></tr>
                    ) : voucherList.map((voucher: AccountingVoucherDto) => (
                      <tr key={voucher.id}>
                        <td>{voucher.voucherDate.slice(0, 10)}<br /><span className="mono">{voucher.voucherNumber}</span></td>
                        <td>
                          <div>{voucher.voucherType}</div>
                          <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{voucher.description || voucher.referenceType || '—'}</div>
                        </td>
                        <td className="mono">{voucher.debitTotal.toLocaleString()}</td>
                        <td className="mono">{voucher.creditTotal.toLocaleString()}</td>
                        <td>
                          <span className={`status-badge status-${voucher.status === 'Posted' ? 'active' : 'warning'}`}>
                            {voucher.status === 'Posted' ? '已過帳' : voucher.status === 'PendingApproval' ? '待覆核' : '草稿'}
                          </span>
                        </td>
                        <td style={{ display: 'flex', gap: 6 }}>
                          {voucher.status === 'Draft' && (
                            <>
                              <button className="icon-action-btn" title="編輯草稿" onClick={() => editVoucher(voucher)}><Pencil size={14} /></button>
                              <button className="icon-action-btn" title="送交覆核" onClick={() => void submitManualVoucher(voucher)}><Send size={14} /></button>
                            </>
                          )}
                          {voucher.status === 'PendingApproval' && (
                            <button className="icon-action-btn" title="核准並過帳" onClick={() => void approveManualVoucher(voucher)}><CheckCircle size={14} /></button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div className="finance-section finance-audit-section finance-journal">
            <div className="glass-card">
              <div className="finance-section-header">
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <Search size={18} style={{ color: 'var(--cobalt-400)' }} />
                  <h3 className="finance-section-title">日記帳</h3>
                </div>
              </div>
              <div className="finance-filter-bar">
                <input aria-label="日記帳起日" type="date" value={journalFromDate} onChange={event => { setJournalPage(1); setLedgerPage(1); setJournalFromDate(event.target.value); }} />
                <input aria-label="日記帳迄日" type="date" value={journalToDate} onChange={event => { setJournalPage(1); setLedgerPage(1); setJournalToDate(event.target.value); }} />
                <select aria-label="日記帳會計科目" value={journalAccountId} onChange={event => { setJournalPage(1); setLedgerPage(1); setJournalAccountId(event.target.value); }}>
                  <option value="">全部科目</option>
                  {chartOfAccounts.map(account => <option key={account.id} value={account.id}>{account.accountCode} {account.accountName}</option>)}
                </select>
                <input aria-label="日記帳傳票號碼" placeholder="傳票號碼" value={journalVoucherNumber} onChange={event => { setJournalPage(1); setJournalVoucherNumber(event.target.value); }} />
              </div>
              <div className="data-table-container">
                <table className="data-table">
                  <thead>
                    <tr><th>過帳日期</th><th>傳票編號</th><th>摘要</th><th>科目</th><th>借方</th><th>貸方</th></tr>
                  </thead>
                  <tbody>
                    {journalLoading ? (
                      <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40 }}><Loader2 className="spin" /></td></tr>
                    ) : (journalResp?.data?.entries ?? []).length === 0 ? (
                      <tr><td colSpan={6} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>查無已過帳分錄。</td></tr>
                    ) : journalResp?.data?.entries.map((entry: JournalEntryDto) => (
                      <tr key={entry.id}>
                        <td>{entry.entryDate.slice(0, 10)}</td>
                        <td className="mono">{entry.sourceDocNo}</td>
                        <td>{entry.description || entry.sourceModule || '—'}</td>
                        <td>{entry.accountNo} {entry.accountName}</td>
                        <td className="mono">{entry.amountDr ? entry.amountDr.toLocaleString() : '—'}</td>
                        <td className="mono">{entry.amountCr ? entry.amountCr.toLocaleString() : '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                  {journalResp?.data && (
                    <tfoot>
                      <tr>
                        <th colSpan={4}>篩選結果合計（{journalResp.data.totalCount} 筆）</th>
                        <th className="mono">{journalResp.data.debitTotal.toLocaleString()}</th>
                        <th className="mono">{journalResp.data.creditTotal.toLocaleString()}</th>
                      </tr>
                    </tfoot>
                  )}
                </table>
              </div>
              {journalResp?.data && journalResp.data.totalCount > journalResp.data.pageSize && (
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 10, padding: 12 }}>
                  <button className="btn btn-secondary" disabled={journalPage <= 1} onClick={() => setJournalPage(page => page - 1)}>上一頁</button>
                  <span>{journalResp.data.page} / {Math.ceil(journalResp.data.totalCount / journalResp.data.pageSize)}</span>
                  <button className="btn btn-secondary" disabled={journalPage >= Math.ceil(journalResp.data.totalCount / journalResp.data.pageSize)} onClick={() => setJournalPage(page => page + 1)}>下一頁</button>
                </div>
              )}
            </div>
          </div>

          <div className="finance-section finance-audit-section finance-ledger">
            <div className="glass-card">
              <div className="finance-section-header">
                <h3 className="finance-section-title">總帳明細</h3>
                <span className="finance-section-count">
                  {journalAccountId ? '依上方選取科目查詢' : '請先於日記帳篩選器選取科目'}
                </span>
              </div>
              {!journalAccountId ? (
                <p style={{ padding: '0 16px 16px', color: 'var(--slate-500)' }}>總帳按科目彙總已過帳分錄，包含期初、期間借貸、期末餘額及可追溯原始傳票的明細。</p>
              ) : (
                <>
                  {ledgerResp?.data && (
                    <div className="module-stat-grid">
                      <div className="glass-card module-stat-card">
                        <div className="module-stat-label">期初借方餘額</div>
                        <div className="module-stat-value">{ledgerResp.data.openingDebitBalance.toLocaleString()}</div>
                      </div>
                      <div className="glass-card module-stat-card">
                        <div className="module-stat-label">期初貸方餘額</div>
                        <div className="module-stat-value">{ledgerResp.data.openingCreditBalance.toLocaleString()}</div>
                      </div>
                      <div className="glass-card module-stat-card">
                        <div className="module-stat-label">期間借方／貸方</div>
                        <div className="module-stat-value">{ledgerResp.data.periodDebitTotal.toLocaleString()} / {ledgerResp.data.periodCreditTotal.toLocaleString()}</div>
                      </div>
                      <div className="glass-card module-stat-card">
                        <div className="module-stat-label">期末借方／貸方餘額</div>
                        <div className="module-stat-value">{ledgerResp.data.closingDebitBalance.toLocaleString()} / {ledgerResp.data.closingCreditBalance.toLocaleString()}</div>
                      </div>
                    </div>
                  )}
                  <div className="data-table-container">
                    <table className="data-table">
                      <thead><tr><th>日期</th><th>傳票</th><th>摘要</th><th>借方</th><th>貸方</th><th>累計借方餘額</th><th>累計貸方餘額</th></tr></thead>
                      <tbody>
                        {ledgerLoading ? (
                          <tr><td colSpan={7} style={{ textAlign: 'center', padding: 32 }}><Loader2 className="spin" /></td></tr>
                        ) : (ledgerResp?.data?.entries ?? []).length === 0 ? (
                          <tr><td colSpan={7} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>此期間沒有已過帳總帳分錄。</td></tr>
                        ) : ledgerResp?.data?.entries.map((entry: GeneralLedgerEntryDto) => (
                          <tr key={entry.entryId}>
                            <td>{entry.voucherDate.slice(0, 10)}</td>
                            <td><button className="btn btn-secondary" onClick={() => setSelectedVoucherId(entry.voucherId)}>{entry.voucherNumber}</button></td>
                            <td>{entry.description || entry.referenceType || '—'}</td>
                            <td className="mono">{entry.debitAmount ? entry.debitAmount.toLocaleString() : '—'}</td>
                            <td className="mono">{entry.creditAmount ? entry.creditAmount.toLocaleString() : '—'}</td>
                            <td className="mono">{entry.runningDebitBalance ? entry.runningDebitBalance.toLocaleString() : '—'}</td>
                            <td className="mono">{entry.runningCreditBalance ? entry.runningCreditBalance.toLocaleString() : '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  {ledgerResp?.data && ledgerResp.data.totalCount > ledgerResp.data.pageSize && (
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 10, padding: 12 }}>
                      <button className="btn btn-secondary" disabled={ledgerPage <= 1} onClick={() => setLedgerPage(page => page - 1)}>上一頁</button>
                      <span>{ledgerResp.data.page} / {Math.ceil(ledgerResp.data.totalCount / ledgerResp.data.pageSize)}</span>
                      <button className="btn btn-secondary" disabled={ledgerPage >= Math.ceil(ledgerResp.data.totalCount / ledgerResp.data.pageSize)} onClick={() => setLedgerPage(page => page + 1)}>下一頁</button>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>

          <div className="finance-section finance-audit-section finance-trial-balance">
            <div className="glass-card">
              <div className="finance-section-header">
                <div>
                  <h3 className="finance-section-title">試算表</h3>
                  <span className="finance-section-count">依上方起迄日期計算；未輸入起日則自系統最早分錄起累計</span>
                </div>
              </div>
              {!journalToDate ? (
                <p style={{ padding: '0 16px 16px', color: 'var(--slate-500)' }}>請先於日記帳篩選器設定迄日以產生試算表。</p>
              ) : trialBalanceLoading ? (
                <LoadingRow />
              ) : trialBalanceResp?.data ? (
                <>
                  <div style={{ padding: '0 16px 12px' }}>
                    <span className={`status-badge status-${trialBalanceResp.data.isBalanced ? 'active' : 'danger'}`}>
                      {trialBalanceResp.data.isBalanced ? '期末借貸平衡' : '期末借貸不平衡'}
                    </span>
                  </div>
                  <div className="data-table-container">
                    <table className="data-table">
                      <thead><tr><th>科目</th><th>期初借方</th><th>期初貸方</th><th>本期借方</th><th>本期貸方</th><th>期末借方</th><th>期末貸方</th></tr></thead>
                      <tbody>
                        {trialBalanceResp.data.accounts.map((account: AccountBalanceDto) => (
                          <tr key={account.accountId}>
                            <td>{account.accountCode} {account.accountName}</td>
                            <td className="mono">{account.openingDebitBalance || '—'}</td>
                            <td className="mono">{account.openingCreditBalance || '—'}</td>
                            <td className="mono">{account.periodDebitTotal || '—'}</td>
                            <td className="mono">{account.periodCreditTotal || '—'}</td>
                            <td className="mono">{account.closingDebitBalance || '—'}</td>
                            <td className="mono">{account.closingCreditBalance || '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                      <tfoot><tr>
                        <th>合計</th>
                        <th className="mono">{trialBalanceResp.data.openingDebitTotal.toLocaleString()}</th>
                        <th className="mono">{trialBalanceResp.data.openingCreditTotal.toLocaleString()}</th>
                        <th className="mono">{trialBalanceResp.data.periodDebitTotal.toLocaleString()}</th>
                        <th className="mono">{trialBalanceResp.data.periodCreditTotal.toLocaleString()}</th>
                        <th className="mono">{trialBalanceResp.data.closingDebitTotal.toLocaleString()}</th>
                        <th className="mono">{trialBalanceResp.data.closingCreditTotal.toLocaleString()}</th>
                      </tr></tfoot>
                    </table>
                  </div>
                </>
              ) : null}
            </div>
          </div>

          <div className="finance-section finance-audit-section finance-statements">
            <div className="glass-card">
              <div className="finance-section-header">
                <h3 className="finance-section-title">損益表與資產負債表</h3>
                <span className="finance-section-count">損益依所選期間；資產負債表為迄日餘額</span>
              </div>
              {!journalFromDate || !journalToDate ? (
                <p style={{ padding: '0 16px 16px', color: 'var(--slate-500)' }}>請先於日記帳篩選器設定完整起迄日期。</p>
              ) : statementsLoading ? (
                <LoadingRow />
              ) : statementsResp?.data ? (
                <>
                  <div style={{ padding: '0 16px 12px' }}>
                    <span className={`status-badge status-${statementsResp.data.isBalanceSheetBalanced ? 'active' : 'danger'}`}>
                      {statementsResp.data.isBalanceSheetBalanced ? '資產負債表平衡' : `資產負債差額 ${statementsResp.data.balanceSheetDifference.toLocaleString()}`}
                    </span>
                    <span style={{ marginLeft: 12 }}>本期損益：{statementsResp.data.periodNetIncome.toLocaleString()}</span>
                    <span style={{ marginLeft: 12 }}>累計未結轉損益：{statementsResp.data.cumulativeUnclosedProfit.toLocaleString()}</span>
                  </div>
                  {[
                    { title: '資產', accounts: statementsResp.data.assets, total: statementsResp.data.totalAssets, amount: (account: AccountBalanceDto) => account.closingDebitBalance - account.closingCreditBalance },
                    { title: '負債', accounts: statementsResp.data.liabilities, total: statementsResp.data.totalLiabilities, amount: (account: AccountBalanceDto) => account.closingCreditBalance - account.closingDebitBalance },
                    { title: '權益', accounts: statementsResp.data.equity, total: statementsResp.data.totalEquity, amount: (account: AccountBalanceDto) => account.closingCreditBalance - account.closingDebitBalance },
                    { title: '收入（本期）', accounts: statementsResp.data.revenue, total: statementsResp.data.revenue.reduce((sum, account) => sum + account.periodCreditTotal - account.periodDebitTotal, 0), amount: (account: AccountBalanceDto) => account.periodCreditTotal - account.periodDebitTotal },
                    { title: '費用（本期）', accounts: statementsResp.data.expenses, total: statementsResp.data.expenses.reduce((sum, account) => sum + account.periodDebitTotal - account.periodCreditTotal, 0), amount: (account: AccountBalanceDto) => account.periodDebitTotal - account.periodCreditTotal },
                  ].map(group => (
                    <div key={group.title} className="data-table-container">
                      <table className="data-table">
                        <thead><tr><th>{group.title}科目</th><th>金額</th></tr></thead>
                        <tbody>
                          {group.accounts.map(account => (
                            <tr key={account.accountId}><td>{account.accountCode} {account.accountName}</td><td className="mono">{group.amount(account).toLocaleString()}</td></tr>
                          ))}
                        </tbody>
                        <tfoot><tr><th>合計</th><th className="mono">{group.total.toLocaleString()}</th></tr></tfoot>
                      </table>
                    </div>
                  ))}
                  <p style={{ padding: '8px 16px', color: 'var(--slate-500)' }}>月結不自動結轉收入與費用；累計未結轉損益列於資產負債核對，年度結轉待會計政策確認後另行處理。</p>
                </>
              ) : null}
            </div>
          </div>

          <div className="finance-section finance-audit-section finance-periods">
            <div className="glass-card">
              <div className="finance-section-header"><h3 className="finance-section-title">會計期間與月結</h3></div>
              <div className="finance-filter-bar finance-period-form">
                <input aria-label="會計期間代碼" placeholder="期間代碼" value={periodCode} onChange={event => setPeriodCode(event.target.value)} />
                <input aria-label="會計期間起日" type="date" value={periodStartDate} onChange={event => setPeriodStartDate(event.target.value)} />
                <input aria-label="會計期間迄日" type="date" value={periodEndDate} onChange={event => setPeriodEndDate(event.target.value)} />
                <button className="btn btn-primary" onClick={() => void createAccountingPeriod()}>新增期間</button>
              </div>
              <div className="data-table-container">
                <table className="data-table">
                  <thead><tr><th>期間</th><th>起日</th><th>迄日</th><th>狀態</th><th>關帳時間</th><th>操作</th></tr></thead>
                  <tbody>
                    {accountingPeriods.length === 0 ? (
                      <tr><td colSpan={6} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>尚未建立會計期間。</td></tr>
                    ) : accountingPeriods.map((period: AccountingPeriodDto) => (
                      <tr key={period.id}>
                        <td>{period.periodCode}</td><td>{period.startDate}</td><td>{period.endDate}</td>
                        <td>{period.isClosed ? '已關帳（不結轉損益）' : '開放中'}</td><td>{period.closedAt?.slice(0, 19).replace('T', ' ') || '—'}</td>
                        <td>{!period.isClosed && <button className="btn btn-secondary" onClick={() => void closeAccountingPeriod(period)}>檢核並關帳</button>}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p style={{ padding: '8px 16px', color: 'var(--slate-500)' }}>關帳會拒絕期間內尚未過帳的傳票，並確認每張已過帳傳票及期間總借貸相等；關帳後期間不可再修改或入帳。</p>
            </div>
          </div>
        </>
      )}

      {selectedVoucherId && (
        <div className="dialog-overlay" role="presentation">
          <div className="dialog-content" role="dialog" aria-modal="true" aria-labelledby="posted-voucher-title" style={{ maxWidth: 900 }}>
            <div className="dialog-header">
              <div id="posted-voucher-title" className="dialog-title">原始會計傳票</div>
              <button onClick={() => setSelectedVoucherId(null)} aria-label="關閉" style={{ background: 'none', border: 'none', color: 'var(--slate-500)', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            {selectedVoucher ? (
              <div className="dialog-body">
                <p>{selectedVoucher.voucherNumber} · {selectedVoucher.voucherDate.slice(0, 10)} · {selectedVoucher.description || selectedVoucher.voucherType}</p>
                <div className="data-table-container">
                  <table className="data-table">
                    <thead><tr><th>項次</th><th>科目</th><th>摘要</th><th>借方</th><th>貸方</th></tr></thead>
                    <tbody>{selectedVoucher.entries.map(entry => (
                      <tr key={entry.id}>
                        <td>{entry.lineNumber}</td><td>{entry.accountCode} {entry.accountName}</td><td>{entry.memo || '—'}</td>
                        <td className="mono">{entry.debitAmount.toLocaleString()}</td><td className="mono">{entry.creditAmount.toLocaleString()}</td>
                      </tr>
                    ))}</tbody>
                    <tfoot><tr><th colSpan={3}>借貸合計</th><th className="mono">{selectedVoucher.debitTotal.toLocaleString()}</th><th className="mono">{selectedVoucher.creditTotal.toLocaleString()}</th></tr></tfoot>
                  </table>
                </div>
              </div>
            ) : <LoadingRow />}
          </div>
        </div>
      )}

      {voucherDialogOpen && (
        <div className="dialog-overlay" role="presentation">
          <div className="dialog-content" role="dialog" aria-modal="true" aria-labelledby="voucher-dialog-title" style={{ maxWidth: 1050 }}>
            <div className="dialog-header">
              <div id="voucher-dialog-title" className="dialog-title">{editingVoucherId ? '編輯傳票草稿' : '新增人工傳票'}</div>
              <button onClick={resetVoucherForm} aria-label="關閉" style={{ background: 'none', border: 'none', color: 'var(--slate-500)', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <div className="dialog-body" style={{ maxHeight: '70vh', overflow: 'auto' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 12, marginBottom: 16 }}>
                <label>傳票日期<input aria-label="傳票日期" type="date" value={voucherDate} onChange={event => setVoucherDate(event.target.value)} style={{ display: 'block', width: '100%' }} /></label>
                <label>傳票類型
                  <select aria-label="傳票類型" value={voucherType} onChange={event => setVoucherType(event.target.value)} style={{ display: 'block', width: '100%' }}>
                    <option value="Journal">轉帳傳票</option>
                    <option value="CashReceipt">現金收入傳票</option>
                    <option value="CashPayment">現金支出傳票</option>
                  </select>
                </label>
                <label>摘要<input aria-label="傳票摘要" maxLength={200} value={voucherDescription} onChange={event => setVoucherDescription(event.target.value)} style={{ display: 'block', width: '100%' }} /></label>
              </div>
              <div className="data-table-container">
                <table className="data-table">
                  <thead><tr><th>項次</th><th>會計科目</th><th>借方</th><th>貸方</th><th>分錄摘要</th><th>操作</th></tr></thead>
                  <tbody>
                    {voucherLines.map((line, index) => (
                      <tr key={index}>
                        <td>{index + 1}</td>
                        <td>
                          <select aria-label={`第 ${index + 1} 筆會計科目`} value={line.glAccountId} onChange={event => updateVoucherLine(index, { glAccountId: event.target.value })}>
                            <option value="">選擇科目</option>
                            {chartOfAccounts.map(account => <option key={account.id} value={account.id}>{account.accountCode} {account.accountName}</option>)}
                          </select>
                        </td>
                        <td><input aria-label={`第 ${index + 1} 筆借方金額`} type="number" min="0" step="0.01" value={line.debitAmount} onChange={event => updateVoucherLine(index, { debitAmount: event.target.value, creditAmount: '' })} /></td>
                        <td><input aria-label={`第 ${index + 1} 筆貸方金額`} type="number" min="0" step="0.01" value={line.creditAmount} onChange={event => updateVoucherLine(index, { creditAmount: event.target.value, debitAmount: '' })} /></td>
                        <td><input aria-label={`第 ${index + 1} 筆分錄摘要`} maxLength={200} value={line.memo} onChange={event => updateVoucherLine(index, { memo: event.target.value })} /></td>
                        <td><button className="icon-action-btn" aria-label={`刪除第 ${index + 1} 筆分錄`} disabled={voucherLines.length <= 2} onClick={() => setVoucherLines(lines => lines.filter((_, lineIndex) => lineIndex !== index))}><X size={14} /></button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <button className="btn btn-secondary" style={{ marginTop: 12 }} onClick={() => setVoucherLines(lines => [...lines, createEmptyVoucherLine()])}>
                <Plus size={14} /> 新增分錄
              </button>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 20, marginTop: 16, fontWeight: 600 }}>
                <span>借方：{voucherLines.reduce((sum, line) => sum + (Number(line.debitAmount) || 0), 0).toLocaleString()}</span>
                <span>貸方：{voucherLines.reduce((sum, line) => sum + (Number(line.creditAmount) || 0), 0).toLocaleString()}</span>
              </div>
              <p style={{ color: 'var(--slate-500)', fontSize: 12, marginTop: 8 }}>草稿可先儲存未平衡分錄；送審時須借貸平衡且會計期間未關帳。核准者不可為傳票建立者。</p>
            </div>
            <div className="dialog-footer" style={{ gap: 8 }}>
              <button className="btn btn-secondary" onClick={resetVoucherForm}>取消</button>
              <button className="btn btn-primary" onClick={() => void saveManualVoucher()}>儲存草稿</button>
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

function Dialog({ onClose }: { type: DialogType; onClose: () => void }) {
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
