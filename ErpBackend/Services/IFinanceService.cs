using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using System.Collections.Generic;

namespace ErpBackend.Services;

public interface IFinanceService
{
    Task<List<GLAccountDto>> GetChartOfAccountsAsync();
    Task<(bool success, string status, GLAccountDto? account)> CreateGLAccountAsync(
        CreateGLAccountRequestDto request);
    Task<List<GLAccountMappingDto>> GetAccountingMappingsAsync();
    Task<(bool success, string status)> SetAccountingMappingAsync(string mappingKey, Guid glAccountId);
    Task<List<AccountingPeriodDto>> GetAccountingPeriodsAsync();
    Task<(bool success, string status, AccountingPeriodDto? period)> CreateAccountingPeriodAsync(
        CreateAccountingPeriodRequestDto request);
    Task<(bool success, string status)> CloseAccountingPeriodAsync(Guid periodId);
    Task<List<JournalEntryDto>> GetJournalEntriesAsync(string docNo);
    Task<List<PostingSetupDto>> GetCostingSetupsAsync();
    Task SeedAccountsAsync();
    Task SeedBankAccountsAsync();
    Task<List<LoanDto>> GetLoansAsync(string? status);
    Task CreateLoanAsync(CreateLoanRequest request);
    Task<List<BankAccountDto>> GetBankAccountsAsync();
    Task<bool> CreateBankAccountAsync(CreateBankAccountRequest request);
    Task<(bool success, string status)> UpdateBankAccountAsync(Guid bankAccountId, UpdateBankAccountRequestDto request);
    Task<bool> SetBankAccountStatusAsync(Guid bankAccountId, bool isActive);
    Task<bool> SetBankAccountGlMappingAsync(Guid bankAccountId, Guid glAccountId);
    Task<List<InvestmentDto>> GetInvestmentsAsync(string? category);
    Task CreateInvestmentAsync(CreateInvestmentRequest request);
    Task<List<object>> GetTreasuryTransfersAsync();
    Task CreateTreasuryTransferAsync(CreateTreasuryTransferRequest request);
    Task<List<object>> GetGuaranteesAsync();
    Task CreateGuaranteeAsync(CreateGuaranteeRequest request);
}
