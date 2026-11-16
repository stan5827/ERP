import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, CheckCircle2, Loader2, RefreshCw } from 'lucide-react';
import { cashReceiptsApi, customersApi, financeApi } from '../api/api';
import type { BankAccountDto } from '../api/finance';
import type {
  CreateCashReceiptRequest
} from '../api/sales';

const formatAmount = (amount: number, currency: string) =>
  `${currency} ${amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
const matchesCurrency = (invoiceCurrency: string, currency?: string) =>
  !currency || invoiceCurrency.toUpperCase() === currency.toUpperCase();

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : '收款作業失敗，請稍後重試';

export const CashReceiptsPanel: React.FC = () => {
  const queryClient = useQueryClient();
  const [customerId, setCustomerId] = useState('');
  const [bankAccountId, setBankAccountId] = useState('');
  const [receivedAmount, setReceivedAmount] = useState('');
  const [initialInvoiceId, setInitialInvoiceId] = useState('');
  const [initialAllocationAmount, setInitialAllocationAmount] = useState('');
  const [referenceNumber, setReferenceNumber] = useState('');
  const [selectedReceiptId, setSelectedReceiptId] = useState('');
  const [targetInvoiceId, setTargetInvoiceId] = useState('');
  const [applicationAmount, setApplicationAmount] = useState('');
  const [createError, setCreateError] = useState('');
  const [applicationError, setApplicationError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const customersQuery = useQuery({
    queryKey: ['cash-receipt-customers'],
    queryFn: async () => {
      const response = await customersApi.list({ page: '1', pageSize: '500' });
      if (!response.data.success) throw new Error(response.data.message || '載入客戶失敗');
      return response.data.data ?? [];
    }
  });
  const bankAccountsQuery = useQuery({
    queryKey: ['cash-receipt-bank-accounts'],
    queryFn: async () => {
      const response = await financeApi.bankAccounts();
      if (!response.data.success) throw new Error(response.data.message || '載入收款帳戶失敗');
      return response.data.data ?? [];
    }
  });
  const receiptsQuery = useQuery({
    queryKey: ['cash-receipts'],
    queryFn: async () => {
      const response = await cashReceiptsApi.list({ page: '1', pageSize: '100' });
      if (!response.data.success) throw new Error(response.data.message || '載入收款紀錄失敗');
      return response.data.data ?? [];
    }
  });

  const customers = customersQuery.data ?? [];
  const bankAccounts = (bankAccountsQuery.data ?? []).filter(account =>
    account.isActive && !!account.glAccountId
  );
  const receipts = receiptsQuery.data ?? [];
  const selectedReceipt = receipts.find(receipt => receipt.id === selectedReceiptId);
  const activeAccount = bankAccounts.find(account => account.id === bankAccountId);
  const invoiceCustomerId = selectedReceipt?.customerId || customerId;

  const pendingInvoicesQuery = useQuery({
    queryKey: ['cash-receipt-pending-invoices', invoiceCustomerId],
    enabled: !!invoiceCustomerId,
    queryFn: async () => {
      const response = await cashReceiptsApi.pendingInvoices(invoiceCustomerId);
      if (!response.data.success) throw new Error(response.data.message || '載入未結發票失敗');
      return response.data.data ?? [];
    }
  });

  const initialInvoices = (pendingInvoicesQuery.data ?? []).filter(invoice =>
    invoice.outstanding > 0 && matchesCurrency(invoice.currency, activeAccount?.currency)
  );
  const applicationInvoices = (pendingInvoicesQuery.data ?? []).filter(invoice =>
    invoice.outstanding > 0 && matchesCurrency(invoice.currency, selectedReceipt?.currency)
  );

  const createReceipt = useMutation({
    mutationFn: (request: CreateCashReceiptRequest) => cashReceiptsApi.create(request),
    onSuccess: async response => {
      if (!response.data.success) {
        setCreateError(response.data.message || '建立收款單失敗');
        return;
      }
      setCreateError('');
      setSuccessMessage('收款單已建立，未沖款餘額已更新。');
      setCustomerId('');
      setBankAccountId('');
      setReceivedAmount('');
      setInitialInvoiceId('');
      setInitialAllocationAmount('');
      setReferenceNumber('');
      await queryClient.invalidateQueries({ queryKey: ['cash-receipts'] });
      await queryClient.invalidateQueries({ queryKey: ['cash-receipt-pending-invoices'] });
    },
    onError: error => setCreateError(getErrorMessage(error))
  });

  const applyUnapplied = useMutation({
    mutationFn: (request: { receiptId: string; salesInvoiceId: string; amount: number }) =>
      cashReceiptsApi.applyUnapplied(request.receiptId, {
        salesInvoiceId: request.salesInvoiceId,
        amount: request.amount
      }),
    onSuccess: async response => {
      if (!response.data.success) {
        setApplicationError(response.data.message || '分配未沖款失敗');
        return;
      }
      setApplicationError('');
      setSuccessMessage('未沖款已分配，收款餘額與發票未收餘額已更新。');
      setApplicationAmount('');
      setTargetInvoiceId('');
      await queryClient.invalidateQueries({ queryKey: ['cash-receipts'] });
      await queryClient.invalidateQueries({ queryKey: ['cash-receipt-pending-invoices'] });
    },
    onError: error => setApplicationError(getErrorMessage(error))
  });

  const handleCreateReceipt = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSuccessMessage('');
    setCreateError('');

    const amount = Number(receivedAmount);
    const allocated = initialInvoiceId ? Number(initialAllocationAmount) : 0;
    const invoice = initialInvoices.find(item => item.id === initialInvoiceId);
    if (!customerId || !activeAccount || !Number.isFinite(amount) || amount <= 0) {
      setCreateError('請選擇客戶、有效收款帳戶，並輸入大於零的實收金額。');
      return;
    }
    if (allocated < 0 || !Number.isFinite(allocated) || allocated > amount ||
        (initialInvoiceId && (!invoice || allocated <= 0 || allocated > invoice.outstanding))) {
      setCreateError('初始沖帳金額必須大於零，且不可超過實收金額或發票未收餘額。');
      return;
    }

    createReceipt.mutate({
      receiptDate: new Date().toISOString(),
      customerId,
      paymentMethod: 'wire',
      receivedAmount: amount,
      discountAmount: 0,
      currency: activeAccount.currency,
      bankAccountId,
      referenceNumber: referenceNumber.trim() || undefined,
      allocations: invoice ? [{
        salesInvoiceId: invoice.id,
        allocatedAmount: allocated,
        discountAmount: 0
      }] : []
    });
  };

  const handleApplyUnapplied = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSuccessMessage('');
    setApplicationError('');
    const amount = Number(applicationAmount);
    const invoice = applicationInvoices.find(item => item.id === targetInvoiceId);
    if (!selectedReceipt || !invoice || !Number.isFinite(amount) || amount <= 0 ||
        amount > selectedReceipt.unappliedAmount || amount > invoice.outstanding) {
      setApplicationError('分配金額須大於零，且不可超過未沖款餘額或發票未收餘額。');
      return;
    }
    applyUnapplied.mutate({
      receiptId: selectedReceipt.id,
      salesInvoiceId: invoice.id,
      amount
    });
  };

  const initialInvoice = initialInvoices.find(invoice => invoice.id === initialInvoiceId);
  const targetInvoice = applicationInvoices.find(invoice => invoice.id === targetInvoiceId);
  const isLoading = customersQuery.isLoading || bankAccountsQuery.isLoading || receiptsQuery.isLoading;
  const queryError = customersQuery.error || bankAccountsQuery.error || receiptsQuery.error;

  return (
    <section aria-label="收款管理" style={{ display: 'grid', gap: 24 }}>
      {successMessage && (
        <div role="status" style={{ color: '#10b981', display: 'flex', alignItems: 'center', gap: 8 }}>
          <CheckCircle2 size={18} /> {successMessage}
        </div>
      )}
      {queryError && (
        <div role="alert" style={{ color: '#f87171', display: 'flex', alignItems: 'center', gap: 8 }}>
          <AlertCircle size={18} /> {getErrorMessage(queryError)}
        </div>
      )}

      <form onSubmit={handleCreateReceipt} style={panelStyle}>
        <h2 style={headingStyle}>登錄收款</h2>
        <div style={formGridStyle}>
          <label style={labelStyle}>
            客戶
            <select aria-label="收款客戶" required value={customerId} onChange={event => {
              setCustomerId(event.target.value);
              setInitialInvoiceId('');
              setInitialAllocationAmount('');
            }} style={inputStyle}>
              <option value="">選擇客戶</option>
              {customers.map(customer => (
                <option key={customer.id} value={customer.id}>{customer.customerCode} - {customer.customerName}</option>
              ))}
            </select>
          </label>
          <label style={labelStyle}>
            收款帳戶
            <select aria-label="收款帳戶" required value={bankAccountId} onChange={event => {
              setBankAccountId(event.target.value);
              setInitialInvoiceId('');
              setInitialAllocationAmount('');
            }} style={inputStyle}>
              <option value="">選擇有效的銀行／現金帳戶</option>
              {bankAccounts.map((account: BankAccountDto) => (
                <option key={account.id} value={account.id}>
                  {account.bankCode || account.bankName || '收款帳戶'} - {account.accountNumber || account.currency} ({account.currency})
                </option>
              ))}
            </select>
          </label>
          <label style={labelStyle}>
            實收金額
            <input aria-label="實收金額" type="number" min="0.01" step="0.01" required value={receivedAmount}
              onChange={event => setReceivedAmount(event.target.value)} style={inputStyle} />
          </label>
          <label style={labelStyle}>
            匯款／交易參考號
            <input aria-label="匯款參考號" value={referenceNumber} onChange={event => setReferenceNumber(event.target.value)}
              style={inputStyle} />
          </label>
          <label style={labelStyle}>
            初始沖帳發票（可留空）
            <select aria-label="初始沖帳發票" value={initialInvoiceId} disabled={!customerId || !activeAccount}
              onChange={event => {
                const invoiceId = event.target.value;
                setInitialInvoiceId(invoiceId);
                const invoice = initialInvoices.find(item => item.id === invoiceId);
                setInitialAllocationAmount(invoice
                  ? String(Math.min(Number(receivedAmount) || 0, invoice.outstanding) || '')
                  : '');
              }} style={inputStyle}>
              <option value="">先保留為未沖款</option>
              {initialInvoices.map(invoice => (
                <option key={invoice.id} value={invoice.id}>
                  {invoice.invoiceNumber} - 未收 {formatAmount(invoice.outstanding, invoice.currency)}
                </option>
              ))}
            </select>
          </label>
          <label style={labelStyle}>
            初始沖帳金額
            <input aria-label="初始沖帳金額" type="number" min="0.01" step="0.01"
              max={Math.min(Number(receivedAmount) || 0, initialInvoice?.outstanding ?? 0)}
              disabled={!initialInvoiceId} value={initialAllocationAmount}
              onChange={event => setInitialAllocationAmount(event.target.value)} style={inputStyle} />
          </label>
        </div>
        {pendingInvoicesQuery.isLoading && invoiceCustomerId && (
          <p style={mutedStyle}>正在載入同客戶未結發票…</p>
        )}
        {pendingInvoicesQuery.error && invoiceCustomerId && (
          <p role="alert" style={errorStyle}>{getErrorMessage(pendingInvoicesQuery.error)}</p>
        )}
        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 18 }}>
          <button type="submit" disabled={createReceipt.isPending || isLoading || bankAccounts.length === 0}
            style={buttonStyle}>
            {createReceipt.isPending ? <Loader2 size={16} className="animate-spin" /> : null}
            建立收款單
          </button>
        </div>
        {createError && <p role="alert" style={errorStyle}>{createError}</p>}
      </form>

      <div style={panelStyle}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
          <h2 style={headingStyle}>未沖款餘額</h2>
          <button type="button" aria-label="重新載入收款紀錄" onClick={() =>
            queryClient.invalidateQueries({ queryKey: ['cash-receipts'] })} style={refreshButtonStyle}>
            <RefreshCw size={15} /> 重新載入
          </button>
        </div>
        {isLoading ? (
          <p style={mutedStyle}>載入收款紀錄中…</p>
        ) : receipts.filter(receipt => receipt.unappliedAmount > 0).length === 0 ? (
          <p style={mutedStyle}>目前沒有待分配的未沖款。</p>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table className="lux-table">
              <thead>
                <tr>
                  <th>收款單號</th>
                  <th>客戶</th>
                  <th>收款日期</th>
                  <th>實收金額</th>
                  <th>未沖款餘額</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {receipts.filter(receipt => receipt.unappliedAmount > 0).map(receipt => (
                  <tr key={receipt.id} className="lux-row">
                    <td>{receipt.receiptNumber}</td>
                    <td>{receipt.customerName || customers.find(customer => customer.id === receipt.customerId)?.customerName || receipt.customerId}</td>
                    <td>{receipt.receiptDate.slice(0, 10)}</td>
                    <td>{formatAmount(receipt.receivedAmount, receipt.currency)}</td>
                    <td style={{ color: '#fbbf24', fontWeight: 700 }}>{formatAmount(receipt.unappliedAmount, receipt.currency)}</td>
                    <td>
                      <button type="button" aria-label={`分配收款單 ${receipt.receiptNumber}`}
                        onClick={() => {
                          setSelectedReceiptId(receipt.id);
                          setTargetInvoiceId('');
                          setApplicationAmount('');
                          setApplicationError('');
                        }} style={buttonStyle}>
                        分配未沖款
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {selectedReceipt && (
        <form onSubmit={handleApplyUnapplied} style={panelStyle}>
          <h2 style={headingStyle}>分配未沖款：{selectedReceipt.receiptNumber}</h2>
          <p style={mutedStyle}>
            可分配餘額：<strong style={{ color: '#fbbf24' }}>
              {formatAmount(selectedReceipt.unappliedAmount, selectedReceipt.currency)}
            </strong>
          </p>
          <div style={formGridStyle}>
            <label style={labelStyle}>
              同客戶、同幣別未結發票
              <select aria-label="未沖款目標發票" required value={targetInvoiceId} onChange={event => {
                const invoiceId = event.target.value;
                setTargetInvoiceId(invoiceId);
                const invoice = applicationInvoices.find(item => item.id === invoiceId);
                setApplicationAmount(invoice
                  ? String(Math.min(selectedReceipt.unappliedAmount, invoice.outstanding))
                  : '');
              }} style={inputStyle}>
                <option value="">選擇發票</option>
                {applicationInvoices.map(invoice => (
                  <option key={invoice.id} value={invoice.id}>
                    {invoice.invoiceNumber} - 未收 {formatAmount(invoice.outstanding, invoice.currency)}
                  </option>
                ))}
              </select>
            </label>
            <label style={labelStyle}>
              本次分配金額
              <input aria-label="本次分配金額" type="number" min="0.01" step="0.01"
                max={Math.min(selectedReceipt.unappliedAmount, targetInvoice?.outstanding ?? 0)}
                required disabled={!targetInvoiceId} value={applicationAmount}
                onChange={event => setApplicationAmount(event.target.value)} style={inputStyle} />
            </label>
          </div>
          {pendingInvoicesQuery.isLoading && <p style={mutedStyle}>正在載入可分配發票…</p>}
          {pendingInvoicesQuery.error && <p role="alert" style={errorStyle}>{getErrorMessage(pendingInvoicesQuery.error)}</p>}
          {!pendingInvoicesQuery.isLoading && !pendingInvoicesQuery.error && applicationInvoices.length === 0 && (
            <p style={mutedStyle}>此客戶目前沒有相同幣別且已核准的未結發票。</p>
          )}
          {applicationError && <p role="alert" style={errorStyle}>{applicationError}</p>}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 18 }}>
            <button type="button" onClick={() => setSelectedReceiptId('')} style={refreshButtonStyle}>取消</button>
            <button type="submit" disabled={applyUnapplied.isPending || !targetInvoiceId} style={buttonStyle}>
              {applyUnapplied.isPending ? <Loader2 size={16} className="animate-spin" /> : null}
              確認分配
            </button>
          </div>
        </form>
      )}
    </section>
  );
};

const panelStyle: React.CSSProperties = {
  padding: 24,
  borderRadius: 12,
  border: '1px solid var(--lux-border)',
  background: 'var(--lux-surface)'
};
const headingStyle: React.CSSProperties = { margin: 0, fontSize: 18, color: 'var(--lux-gold)' };
const formGridStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: 16,
  marginTop: 18
};
const labelStyle: React.CSSProperties = { display: 'grid', gap: 8, fontSize: 13, color: '#cbd5e1' };
const inputStyle: React.CSSProperties = {
  width: '100%',
  minHeight: 40,
  padding: '8px 10px',
  borderRadius: 6,
  border: '1px solid rgba(255,255,255,0.15)',
  background: '#07111f',
  color: '#fff'
};
const buttonStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 8,
  padding: '9px 14px',
  border: 0,
  borderRadius: 6,
  background: 'var(--lux-gold)',
  color: '#07111f',
  fontWeight: 700,
  cursor: 'pointer'
};
const refreshButtonStyle: React.CSSProperties = {
  ...buttonStyle,
  background: 'rgba(255,255,255,0.08)',
  color: '#cbd5e1'
};
const mutedStyle: React.CSSProperties = { color: '#94a3b8', fontSize: 13 };
const errorStyle: React.CSSProperties = { color: '#f87171', fontSize: 13 };
