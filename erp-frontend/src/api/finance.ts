import { api, ApiResponse } from './client';

export interface GlAccountDto { id: string; accountNo: string; accountName: string; accountType: string; isActive: boolean; }
export interface JournalEntryDto { id: string; entryDate: string; accountId: string; accountNo?: string; accountName?: string; amountDr: number; amountCr: number; sourceModule: string; sourceDocNo: string; description?: string; }
export interface FinanceGlAccountDto { id: string; accountCode: string; accountName: string; accountType: string; isActive: boolean; }
export interface AccountingVoucherLineDto {
  id: string;
  lineNumber: number;
  glAccountId: string;
  accountCode: string;
  accountName: string;
  costCenterId?: string;
  debitAmount: number;
  creditAmount: number;
  memo?: string;
}
export interface AccountingVoucherDto {
  id: string;
  voucherNumber: string;
  voucherDate: string;
  voucherType: string;
  description?: string;
  referenceType?: string;
  referenceId?: string;
  status: string;
  postedAt?: string;
  entries: AccountingVoucherLineDto[];
  debitTotal: number;
  creditTotal: number;
}
export interface SaveAccountingVoucherRequest {
  voucherDate: string;
  voucherType: string;
  description?: string;
  entries: Array<{
    glAccountId: string;
    debitAmount: number;
    creditAmount: number;
    costCenterId?: string;
    memo?: string;
  }>;
}
export interface JournalQueryResult {
  entries: JournalEntryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  debitTotal: number;
  creditTotal: number;
}
export interface GeneralLedgerEntryDto {
  entryId: string;
  voucherId: string;
  voucherNumber: string;
  voucherDate: string;
  voucherType: string;
  description?: string;
  referenceType?: string;
  referenceId?: string;
  lineNumber: number;
  debitAmount: number;
  creditAmount: number;
  runningDebitBalance: number;
  runningCreditBalance: number;
}
export interface GeneralLedgerResultDto {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: string;
  fromDate?: string;
  toDate?: string;
  openingDebitBalance: number;
  openingCreditBalance: number;
  periodDebitTotal: number;
  periodCreditTotal: number;
  closingDebitBalance: number;
  closingCreditBalance: number;
  totalCount: number;
  page: number;
  pageSize: number;
  entries: GeneralLedgerEntryDto[];
}
export interface AccountBalanceDto {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: string;
  openingDebitBalance: number;
  openingCreditBalance: number;
  periodDebitTotal: number;
  periodCreditTotal: number;
  closingDebitBalance: number;
  closingCreditBalance: number;
}
export interface TrialBalanceResultDto {
  fromDate?: string;
  toDate: string;
  accounts: AccountBalanceDto[];
  openingDebitTotal: number;
  openingCreditTotal: number;
  periodDebitTotal: number;
  periodCreditTotal: number;
  closingDebitTotal: number;
  closingCreditTotal: number;
  isBalanced: boolean;
}
export interface FinancialStatementsResultDto {
  fromDate: string;
  toDate: string;
  assets: AccountBalanceDto[];
  liabilities: AccountBalanceDto[];
  equity: AccountBalanceDto[];
  revenue: AccountBalanceDto[];
  expenses: AccountBalanceDto[];
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  periodNetIncome: number;
  cumulativeUnclosedProfit: number;
  balanceSheetDifference: number;
  isBalanceSheetBalanced: boolean;
}
export interface AccountingPeriodDto {
  id: string;
  periodCode: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
  closedAt?: string;
}
export interface PostingSetupDto { id: string; transactionType: string; itemGroup?: string; drAccountNo: string; crAccountNo: string; }
export interface LoanDto { id: string; loanNumber: string; bankName?: string; loanAmount: number; currency: string; annualInterestRate: number; loanDate: string; maturityDate: string; repaymentMethod: string; status: string; }
export interface BankAccountDto {
  id: string; bankCode?: string; bankName?: string; accountNumber?: string;
  glAccountId?: string; glAccountCode?: string; glAccountName?: string;
  currency: string; currentBalance: number; isActive: boolean;
}
export interface InvestmentDto { id: string; investmentName: string; investmentCategory: string; totalCost: number; currentMarketValue: number; unrealizedGainLoss: number; }

export const financeApi = {
  getChartOfAccounts: () => api.get<ApiResponse<FinanceGlAccountDto[]>>('/finance/coa'),
  getJournalEntries: (docNo: string) => api.get<ApiResponse<JournalEntryDto[]>>(`/finance/journals/${docNo}`),
  getJournalBook: (params?: Record<string, string>) =>
    api.get<ApiResponse<JournalQueryResult>>('/finance/journals', { params }),
  getGeneralLedger: (params: Record<string, string>) =>
    api.get<ApiResponse<GeneralLedgerResultDto>>('/finance/general-ledger', { params }),
  getTrialBalance: (params: Record<string, string>) =>
    api.get<ApiResponse<TrialBalanceResultDto>>('/finance/trial-balance', { params }),
  getFinancialStatements: (params: Record<string, string>) =>
    api.get<ApiResponse<FinancialStatementsResultDto>>('/finance/financial-statements', { params }),
  getAccountingPeriods: () =>
    api.get<ApiResponse<AccountingPeriodDto[]>>('/finance/accounting-periods'),
  createAccountingPeriod: (data: { periodCode: string; startDate: string; endDate: string }) =>
    api.post<ApiResponse<AccountingPeriodDto>>('/finance/accounting-periods', data),
  closeAccountingPeriod: (id: string) =>
    api.post<ApiResponse>(`/finance/accounting-periods/${id}/close`),
  getVouchers: (params?: Record<string, string>) =>
    api.get<ApiResponse<AccountingVoucherDto[]>>('/finance/vouchers', { params }),
  getVoucher: (id: string) => api.get<ApiResponse<AccountingVoucherDto>>(`/finance/vouchers/${id}`),
  createVoucher: (data: SaveAccountingVoucherRequest) =>
    api.post<ApiResponse<AccountingVoucherDto>>('/finance/vouchers', data),
  updateVoucher: (id: string, data: SaveAccountingVoucherRequest) =>
    api.put<ApiResponse<AccountingVoucherDto>>(`/finance/vouchers/${id}`, data),
  submitVoucher: (id: string) => api.post<ApiResponse<AccountingVoucherDto>>(`/finance/vouchers/${id}/submit`),
  approveVoucher: (id: string) => api.post<ApiResponse<AccountingVoucherDto>>(`/finance/vouchers/${id}/approve`),
  getPostingSetups: () => api.get<ApiResponse<PostingSetupDto[]>>('/finance/posting-setups'),
  seedAccounts: () => api.post<ApiResponse>('/finance/seed'),
  seedBankAccounts: () => api.post<ApiResponse<string>>('/finance/seed-accounts'),
  loans: (params?: Record<string, string>) => api.get<ApiResponse<LoanDto[]>>('/finance/loans', { params }),
  createLoan: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/loans', data),
  bankAccounts: () => api.get<ApiResponse<BankAccountDto[]>>('/finance/bank-accounts'),
  createBankAccount: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/bank-accounts', data),
  setBankAccountGlMapping: (id: string, glAccountId: string) =>
    api.patch<ApiResponse>(`/finance/bank-accounts/${id}/gl-account`, { glAccountId }),
  investments: (params?: Record<string, string>) => api.get<ApiResponse<InvestmentDto[]>>('/finance/investments', { params }),
  createInvestment: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/investments', data),
  transfers: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/finance/treasury-transfers', { params }),
  createTransfer: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/treasury-transfers', data),
  guarantees: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/finance/guarantees', { params }),
  createGuarantee: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/guarantees', data),
};
