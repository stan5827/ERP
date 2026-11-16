import { api, ApiResponse } from './client';

export interface GlAccountDto { id: string; accountNo: string; accountName: string; accountType: string; isActive: boolean; }
export interface JournalEntryDto { id: string; entryDate: string; accountId: string; accountNo?: string; accountName?: string; amountDr: number; amountCr: number; sourceModule: string; sourceDocNo: string; description?: string; }
export interface FinanceGlAccountDto { id: string; accountCode: string; accountName: string; accountType: string; isActive: boolean; isCashAccount: boolean; parentAccountId?: string; }
export interface CreateGLAccountRequest {
  accountCode: string;
  accountName: string;
  accountType: string;
  isCashAccount: boolean;
  parentAccountId?: string;
}
export interface AccountingVoucherLineDto {
  id: string;
  lineNumber: number;
  glAccountId: string;
  accountCode: string;
  accountName: string;
  costCenterId?: string;
  workOrderId?: string;
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
  branchName?: string; accountName?: string;
  glAccountId?: string; glAccountCode?: string; glAccountName?: string;
  glAccountIsActive?: boolean; glAccountIsCashAccount?: boolean;
  currency: string; currentBalance: number; isActive: boolean;
}
export interface SaveBankAccountRequest {
  bankCode?: string;
  bankName: string;
  branchName?: string;
  accountNumber: string;
  accountName?: string;
  glAccountId?: string | null;
  currency: string;
}
export interface InvestmentDto { id: string; investmentName: string; investmentCategory: string; totalCost: number; currentMarketValue: number; unrealizedGainLoss: number; }

export interface SupplierApSubmissionSummaryDto {
  id: string;
  submissionNumber: string;
  status: string;
  currentRevisionNumber: number;
  supplierInvoiceNumber: string;
  supplierInvoiceDate: string;
  currency: string;
  totalAmount: number;
  matchResult: string;
  version: number;
  createdAt: string;
  submittedAt?: string;
}

export interface SupplierApSubmissionPageDto {
  items: SupplierApSubmissionSummaryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface SupplierApSubmissionDetailDto extends SupplierApSubmissionSummaryDto {
  invoiceAmount: number;
  taxAmount: number;
  purchaseOrderId: string;
  purchaseOrderNumber: string;
  goodsReceiptId: string;
  goodsReceiptNumber: string;
  discrepancies: string[];
  approvedAt?: string;
  accountsPayableId?: string;
  accountsPayableNumber?: string;
  lines: Array<{
    goodsReceiptItemId: string;
    skuNumber?: string;
    skuName?: string;
    acceptedQuantity: number;
    invoicedQuantity: number;
    unitPrice: number;
    netAmount: number;
    taxAmount: number;
    totalAmount: number;
  }>;
  attachments: Array<{
    id: string;
    originalFileName: string;
    contentType: string;
    sizeBytes: number;
    scanStatus: string;
    revisionNumber: number;
  }>;
  reviews: Array<{
    id: string;
    action: string;
    reason?: string;
    matchResult: string;
    createdAt: string;
  }>;
}

export const financeApi = {
  getChartOfAccounts: () => api.get<ApiResponse<FinanceGlAccountDto[]>>('/finance/coa'),
  createGLAccount: (data: CreateGLAccountRequest) =>
    api.post<ApiResponse<FinanceGlAccountDto>>('/finance/coa', data),
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
  createBankAccount: (data: SaveBankAccountRequest) => api.post<ApiResponse>('/finance/bank-accounts', data),
  updateBankAccount: (id: string, data: SaveBankAccountRequest) =>
    api.put<ApiResponse>(`/finance/bank-accounts/${id}`, data),
  setBankAccountStatus: (id: string, isActive: boolean) =>
    api.patch<ApiResponse>(`/finance/bank-accounts/${id}/status`, { isActive }),
  setBankAccountGlMapping: (id: string, glAccountId: string) =>
    api.patch<ApiResponse>(`/finance/bank-accounts/${id}/gl-account`, { glAccountId }),
  investments: (params?: Record<string, string>) => api.get<ApiResponse<InvestmentDto[]>>('/finance/investments', { params }),
  createInvestment: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/investments', data),
  transfers: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/finance/treasury-transfers', { params }),
  createTransfer: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/treasury-transfers', data),
  guarantees: (params?: Record<string, string>) => api.get<ApiResponse<unknown[]>>('/finance/guarantees', { params }),
  createGuarantee: (data: Record<string, unknown>) => api.post<ApiResponse>('/finance/guarantees', data),
  supplierApSubmissions: (status = 'Submitted', page = 1) =>
    api.get<SupplierApSubmissionPageDto>('/supplier-ap-submissions', {
      params: { status, page, pageSize: 50 },
    }),
  getSupplierApSubmission: (id: string) =>
    api.get<SupplierApSubmissionDetailDto>(`/supplier-ap-submissions/${id}`),
  returnSupplierApSubmission: (id: string, version: number, reason: string) =>
    api.post(`/supplier-ap-submissions/${id}/return`, { reason }, {
      headers: { 'If-Match': `"${version}"` },
    }),
  rejectSupplierApSubmission: (id: string, version: number, reason: string) =>
    api.post(`/supplier-ap-submissions/${id}/reject`, { reason }, {
      headers: { 'If-Match': `"${version}"` },
    }),
  approveSupplierApSubmission: (id: string, version: number, reason: string) =>
    api.post(`/supplier-ap-submissions/${id}/approve`, { reason }, {
      headers: { 'If-Match': `"${version}"` },
    }),
};
