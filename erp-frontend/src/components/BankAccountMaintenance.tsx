import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { X } from 'lucide-react';
import { createPortal } from 'react-dom';
import { financeApi } from '../api/api';
import type { BankAccountDto, FinanceGlAccountDto, SaveBankAccountRequest } from '../api/finance';
import { useRole } from '../contexts/useRole';
import './BankAccountMaintenance.css';

type BankAccountForm = {
  bankCode: string;
  bankName: string;
  branchName: string;
  accountNumber: string;
  accountName: string;
  glAccountId: string;
  currency: string;
};

const emptyForm: BankAccountForm = {
  bankCode: '',
  bankName: '',
  branchName: '',
  accountNumber: '',
  accountName: '',
  glAccountId: '',
  currency: 'TWD',
};

const getErrorMessage = (error: unknown) => {
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const response = (error as { response?: { data?: { message?: string } } }).response;
    if (response?.data?.message) return response.data.message;
  }
  if (error instanceof Error) return error.message;
  return '作業失敗，請稍後重試。';
};

export function BankAccountMaintenance() {
  const queryClient = useQueryClient();
  const { hasPermission } = useRole();
  const canCreate = hasPermission('finance:treasury', 'create');
  const canEdit = hasPermission('finance:treasury', 'edit');
  const [form, setForm] = useState<BankAccountForm>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);

  const accountsQuery = useQuery({
    queryKey: ['finance-bank-accounts'],
    queryFn: async () => {
      const response = await financeApi.bankAccounts();
      if (!response.data.success) throw new Error(response.data.message || '載入銀行帳戶失敗');
      const accounts = response.data.data;
      if (accounts !== undefined && !Array.isArray(accounts)) {
        throw new Error('銀行帳戶資料格式錯誤，請重新載入。');
      }
      return accounts ?? [];
    },
  });
  const glAccountsQuery = useQuery({
    queryKey: ['finance-chart-of-accounts'],
    queryFn: async () => {
      const response = await financeApi.getChartOfAccounts();
      if (!response.data.success) throw new Error(response.data.message || '載入總帳科目失敗');
      const accounts = response.data.data;
      if (accounts !== undefined && !Array.isArray(accounts)) {
        throw new Error('總帳科目資料格式錯誤，請重新載入。');
      }
      return accounts ?? [];
    },
  });

  const saveMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string | null; data: SaveBankAccountRequest }) => {
      if (id) return financeApi.updateBankAccount(id, data);
      return financeApi.createBankAccount(data);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['finance-bank-accounts'] });
      setDialogOpen(false);
      setEditingId(null);
      setForm(emptyForm);
    },
  });
  const statusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      financeApi.setBankAccountStatus(id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['finance-bank-accounts'] }),
  });

  const openNewAccount = () => {
    setEditingId(null);
    setForm(emptyForm);
    setDialogOpen(true);
  };

  const openEditAccount = (account: BankAccountDto) => {
    setEditingId(account.id);
    setForm({
      bankCode: account.bankCode ?? '',
      bankName: account.bankName ?? '',
      branchName: account.branchName ?? '',
      accountNumber: account.accountNumber ?? '',
      accountName: account.accountName ?? '',
      glAccountId: account.glAccountId ?? '',
      currency: account.currency || 'TWD',
    });
    setDialogOpen(true);
  };

  const submitForm = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data: SaveBankAccountRequest = {
      bankCode: form.bankCode.trim() || undefined,
      bankName: form.bankName.trim(),
      branchName: form.branchName.trim() || undefined,
      accountNumber: form.accountNumber.trim(),
      accountName: form.accountName.trim() || undefined,
      glAccountId: form.glAccountId || null,
      currency: form.currency.trim().toUpperCase(),
    };
    saveMutation.mutate({ id: editingId, data });
  };

  const accounts = accountsQuery.data ?? [];
  const cashAccounts = (glAccountsQuery.data ?? []).filter(
    (account: FinanceGlAccountDto) => account.isActive && account.isCashAccount,
  );
  const editingAccount = accounts.find((account: BankAccountDto) => account.id === editingId);
  const hasValidEditingMapping = !editingAccount?.glAccountId
    || cashAccounts.some((account: FinanceGlAccountDto) => account.id === editingAccount.glAccountId);
  const hasQueryError = accountsQuery.isError || glAccountsQuery.isError;
  const mutationError = statusMutation.error;

  return (
    <section className="finance-section">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">銀行帳戶維護</h2>
          <p className="module-page-desc">維護公司收款帳戶及其現金／銀行總帳科目對應。</p>
        </div>
        {canCreate && (
          <button className="btn btn-primary" onClick={openNewAccount}>
            新增銀行帳戶
          </button>
        )}
      </div>

      {hasQueryError && (
        <div className="glass-card" role="alert">
          {getErrorMessage(accountsQuery.error ?? glAccountsQuery.error)}
        </div>
      )}
      {mutationError && (
        <div className="glass-card" role="alert">
          {getErrorMessage(mutationError)}
        </div>
      )}

      <div className="glass-card">
        {accountsQuery.isLoading ? (
          <div style={{ padding: 24 }}>載入銀行帳戶中…</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>銀行／帳戶名稱</th>
                <th>分行</th>
                <th>帳號</th>
                <th>幣別</th>
                <th>總帳科目</th>
                <th>帳務餘額</th>
                <th>狀態</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {accounts.length === 0 ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: 32 }}>
                    尚未建立銀行帳戶。
                  </td>
                </tr>
              ) : accounts.map((account: BankAccountDto) => (
                <tr key={account.id}>
                  <td>{account.bankName || '—'}{account.accountName ? `／${account.accountName}` : ''}</td>
                  <td>{account.branchName || '—'}</td>
                  <td className="mono">{account.accountNumber || '—'}</td>
                  <td>{account.currency}</td>
                  <td>
                    {!account.glAccountId
                      ? <span className="status-badge">尚未對應科目</span>
                      : account.glAccountIsActive && account.glAccountIsCashAccount
                        ? `${account.glAccountCode ?? ''} ${account.glAccountName ?? ''}`.trim()
                        : <span className="status-badge">科目需修正</span>}
                  </td>
                  <td className="mono">{(account.currentBalance ?? 0).toLocaleString()}</td>
                  <td><span className="status-badge">{account.isActive ? '啟用' : '停用'}</span></td>
                  <td>
                    {(canEdit || (canCreate && !account.isActive)) && (
                      <div style={{ display: 'flex', gap: 8 }}>
                        {canEdit && (
                          <button className="btn btn-secondary" onClick={() => openEditAccount(account)}>
                            編輯
                          </button>
                        )}
                        {canEdit && (
                          <button
                            className={account.isActive ? 'btn btn-danger' : 'btn btn-primary'}
                            disabled={statusMutation.isPending}
                            onClick={() => statusMutation.mutate({ id: account.id, isActive: !account.isActive })}
                          >
                            {account.isActive ? '停用' : '啟用'}
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {dialogOpen && createPortal(
        <div className="dialog-overlay bank-account-dialog-overlay" role="presentation">
          <div className="dialog-content bank-account-dialog" role="dialog" aria-modal="true" aria-labelledby="bank-account-title">
            <div className="bank-account-dialog__header">
              <div>
                <p className="bank-account-dialog__eyebrow">財務管理・收款帳戶</p>
                <h2 id="bank-account-title">{editingId ? '編輯銀行帳戶' : '新增銀行帳戶'}</h2>
                <p>建立帳戶資料並設定對應的現金／銀行總帳科目。</p>
              </div>
              <button
                type="button"
                className="bank-account-dialog__close"
                onClick={() => setDialogOpen(false)}
                aria-label="關閉"
              >
                <X size={17} aria-hidden="true" />
                <span>關閉</span>
              </button>
            </div>
            <form className="bank-account-form" onSubmit={submitForm}>
              <div className="bank-account-form__body">
                <section className="bank-account-form__section" aria-labelledby="bank-account-details-title">
                  <div className="bank-account-form__section-heading">
                    <h3 id="bank-account-details-title">帳戶基本資料</h3>
                    <p>標示 <span className="bank-account-form__required">*</span> 為必填欄位</p>
                  </div>
                  <div className="bank-account-form__grid">
                    <label className="bank-account-form__field" htmlFor="bank-account-name">
                      <span className="bank-account-form__label-text">
                        銀行名稱 <span className="bank-account-form__required">*</span>
                      </span>
                      <input
                        id="bank-account-name"
                        required
                        maxLength={100}
                        placeholder="例如：台灣銀行"
                        value={form.bankName}
                        onChange={event => setForm({ ...form, bankName: event.target.value })}
                      />
                    </label>
                    <label className="bank-account-form__field" htmlFor="bank-account-code">
                      <span className="bank-account-form__label-text">銀行代碼</span>
                      <input
                        id="bank-account-code"
                        maxLength={10}
                        placeholder="例如：004"
                        value={form.bankCode}
                        onChange={event => setForm({ ...form, bankCode: event.target.value })}
                      />
                    </label>
                    <label className="bank-account-form__field" htmlFor="bank-account-branch">
                      <span className="bank-account-form__label-text">分行</span>
                      <input
                        id="bank-account-branch"
                        maxLength={100}
                        placeholder="例如：台北分行"
                        value={form.branchName}
                        onChange={event => setForm({ ...form, branchName: event.target.value })}
                      />
                    </label>
                    <label className="bank-account-form__field" htmlFor="bank-account-holder">
                      <span className="bank-account-form__label-text">帳戶名稱</span>
                      <input
                        id="bank-account-holder"
                        maxLength={100}
                        placeholder="例如：公司營運帳戶"
                        value={form.accountName}
                        onChange={event => setForm({ ...form, accountName: event.target.value })}
                      />
                    </label>
                    <label className="bank-account-form__field" htmlFor="bank-account-number">
                      <span className="bank-account-form__label-text">
                        帳號 <span className="bank-account-form__required">*</span>
                      </span>
                      <input
                        id="bank-account-number"
                        required
                        maxLength={30}
                        placeholder="請輸入銀行帳號"
                        value={form.accountNumber}
                        onChange={event => setForm({ ...form, accountNumber: event.target.value })}
                      />
                    </label>
                    <label className="bank-account-form__field" htmlFor="bank-account-currency">
                      <span className="bank-account-form__label-text">
                        幣別 <span className="bank-account-form__required">*</span>
                      </span>
                      <input
                        id="bank-account-currency"
                        required
                        maxLength={10}
                        placeholder="TWD"
                        value={form.currency}
                        onChange={event => setForm({ ...form, currency: event.target.value })}
                      />
                    </label>
                  </div>
                </section>

                <section className="bank-account-form__section bank-account-form__section--mapping" aria-labelledby="bank-account-mapping-title">
                  <div className="bank-account-form__section-heading">
                    <h3 id="bank-account-mapping-title">帳務設定</h3>
                    <p>選擇收款時使用的會計科目</p>
                  </div>
                  <label className="bank-account-form__field" htmlFor="bank-account-gl-account">
                    <span className="bank-account-form__label-text">收款總帳科目</span>
                    <select
                      id="bank-account-gl-account"
                      value={form.glAccountId}
                      onChange={event => setForm({ ...form, glAccountId: event.target.value })}
                      disabled={glAccountsQuery.isLoading}
                      aria-describedby="bank-account-gl-account-help"
                    >
                      <option value="">尚未設定</option>
                      {!hasValidEditingMapping && editingAccount?.glAccountId && (
                        <option value={editingAccount.glAccountId}>
                          {editingAccount.glAccountCode} {editingAccount.glAccountName}（停用或非現金科目，請修正）
                        </option>
                      )}
                      {glAccountsQuery.data?.length === 0 && (
                        <option value="" disabled>目前沒有可選用的現金／銀行科目</option>
                      )}
                      {cashAccounts.map(account => (
                        <option key={account.id} value={account.id}>
                          {account.accountCode} — {account.accountName}
                        </option>
                      ))}
                    </select>
                    <span id="bank-account-gl-account-help" className="bank-account-form__help">
                      {glAccountsQuery.isLoading
                        ? '正在載入可用科目…'
                        : '僅能選擇已啟用的現金／銀行科目；未設定科目的帳戶不會出現在登錄收款選單。'}
                    </span>
                  </label>
                </section>
              </div>
              {saveMutation.error && (
                <div className="bank-account-form__error" role="alert">
                  {getErrorMessage(saveMutation.error)}
                </div>
              )}
              <div className="bank-account-form__actions">
                <button type="button" className="btn btn-secondary" onClick={() => setDialogOpen(false)}>
                  取消
                </button>
                <button type="submit" className="btn btn-primary" disabled={saveMutation.isPending || glAccountsQuery.isError}>
                  {saveMutation.isPending ? '儲存中…' : '儲存帳戶'}
                </button>
              </div>
            </form>
          </div>
        </div>,
        document.body,
      )}
    </section>
  );
}
