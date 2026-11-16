using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/finance")]
public class FinanceController : BaseController
{
    private readonly IFinanceService _financeService;
    private readonly IAccountingService _accountingService;
    private readonly IAccountingVoucherService _accountingVoucherService;
    private readonly IAccountingReportingService _accountingReportingService;

    public FinanceController(
        IFinanceService financeService,
        IAccountingService accountingService,
        IAccountingVoucherService accountingVoucherService,
        IAccountingReportingService accountingReportingService)
    {
        _financeService = financeService;
        _accountingService = accountingService;
        _accountingVoucherService = accountingVoucherService;
        _accountingReportingService = accountingReportingService;
    }

    /// <summary>
    /// 取得會計科目表 (Chart of Accounts)
    /// </summary>
    [Authorize]
    [HttpGet("coa")]
    public async Task<ActionResult<ApiResponse<List<GLAccountDto>>>> GetChartOfAccounts()
    {
        var accounts = await _financeService.GetChartOfAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("accounting-mappings")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<GLAccountMappingDto>>>> GetAccountingMappings()
    {
        var mappings = await _financeService.GetAccountingMappingsAsync();
        return Ok(mappings);
    }

    [HttpPut("accounting-mappings/{mappingKey}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> SetAccountingMapping(
        string mappingKey,
        [FromBody] SetGLAccountMappingRequestDto request)
    {
        var (success, status) = await _financeService.SetAccountingMappingAsync(
            mappingKey,
            request.GLAccountId);
        if (!success)
        {
            var message = status switch
            {
                "unknown_mapping" => "不支援此總帳科目映射類型",
                "invalid_account" => "指定科目不存在、已停用，或科目類型不符合映射要求",
                _ => "總帳科目映射設定失敗"
            };
            return BadRequest(ApiHelper.Fail(message));
        }

        return Ok("總帳科目映射已更新");
    }

    [HttpGet("accounting-periods")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<AccountingPeriodDto>>>> GetAccountingPeriods()
    {
        return Ok(await _financeService.GetAccountingPeriodsAsync());
    }

    [HttpPost("accounting-periods")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AccountingPeriodDto>>> CreateAccountingPeriod(
        [FromBody] CreateAccountingPeriodRequestDto request)
    {
        var (success, status, period) = await _financeService.CreateAccountingPeriodAsync(request);
        if (!success)
        {
            var message = status switch
            {
                "invalid_period_code" => "會計期間代碼不可空白且最多 30 個字元",
                "invalid_date_range" => "會計期間起日不可晚於迄日",
                "period_code_exists" => "會計期間代碼已存在",
                "period_overlap" => "會計期間日期不可與既有期間重疊",
                _ => "會計期間建立失敗"
            };
            return BadRequest(ApiHelper.Fail(message));
        }

        return Ok(period!);
    }

    [HttpPost("accounting-periods/{periodId:guid}/close")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> CloseAccountingPeriod(Guid periodId)
    {
        var (success, status) = await _financeService.CloseAccountingPeriodAsync(periodId);
        if (!success)
        {
            var message = status switch
            {
                "not_found" => "找不到會計期間",
                "already_closed" => "會計期間已關帳",
                "unposted_vouchers" => "期間內仍有草稿或待覆核傳票，請先完成處理",
                "unbalanced_vouchers" => "期間內有未平衡的已過帳傳票，無法關帳",
                _ => "會計期間關帳失敗"
            };
            return BadRequest(ApiHelper.Fail(message));
        }

        return Ok("會計期間已關帳");
    }

    [HttpPost("vouchers/{voucherId:guid}/reverse")]
    [Authorize]
    public async Task<IActionResult> ReverseVoucher(
        Guid voucherId,
        [FromBody] ReverseAccountingVoucherRequestDto request)
    {
        var (success, status, voucher) = await _accountingService.ReverseVoucherAsync(
            voucherId,
            request.ReversalDate,
            request.Reason);
        if (!success)
        {
            var message = status switch
            {
                "not_found" => "找不到原始傳票",
                "not_posted" => "只有已過帳傳票可以沖銷",
                "reversal_cannot_be_reversed" => "沖銷傳票不可再次沖銷",
                "already_reversed" => $"傳票已沖銷：{voucher?.VoucherNumber}",
                "reason_required" => "請填寫沖銷原因",
                "reason_too_long" => "沖銷原因最多 150 個字元",
                "reversal_date_required" => "請提供有效的沖銷日期",
                "period_closed" => "沖銷日期所屬會計期間已關帳",
                _ => "建立沖銷傳票失敗"
            };
            return status == "already_reversed"
                ? base.Conflict(ApiHelper.Fail(message))
                : base.BadRequest(ApiHelper.Fail(message));
        }

        return new OkObjectResult(ApiResponse<object>.Ok(voucher!));
    }

    /// <summary>
    /// 依傳票號碼取得會計分錄
    /// </summary>
    [Authorize]
    [HttpGet("journals/{docNo}")]
    public async Task<ActionResult<ApiResponse<List<JournalEntryDto>>>> GetJournalEntries(string docNo)
    {
        var entries = await _financeService.GetJournalEntriesAsync(docNo);
        return Ok(entries);
    }

    /// <summary>
    /// 依條件查詢已過帳日記帳分錄
    /// </summary>
    [Authorize]
    [HttpGet("journals")]
    public async Task<ActionResult<ApiResponse<JournalQueryResultDto>>> GetJournal(
        [FromQuery] JournalQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.FromDate.HasValue && request.ToDate.HasValue &&
            request.FromDate.Value > request.ToDate.Value)
            return BadRequest(ApiHelper.Fail<JournalQueryResultDto>("查詢起日不可晚於迄日"));

        var result = await _accountingVoucherService.GetJournalAsync(request, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("general-ledger")]
    public async Task<ActionResult<ApiResponse<GeneralLedgerResultDto>>> GetGeneralLedger(
        [FromQuery] GeneralLedgerQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.AccountId == Guid.Empty)
            return BadRequest(ApiHelper.Fail<GeneralLedgerResultDto>("請指定總帳科目"));
        if (request.FromDate.HasValue && request.ToDate.HasValue &&
            request.FromDate.Value > request.ToDate.Value)
            return BadRequest(ApiHelper.Fail<GeneralLedgerResultDto>("查詢起日不可晚於迄日"));

        var result = await _accountingReportingService.GetGeneralLedgerAsync(
            request,
            cancellationToken);
        return result is null
            ? NotFound(ApiHelper.Fail<GeneralLedgerResultDto>("找不到指定的總帳科目"))
            : Ok(result);
    }

    [Authorize]
    [HttpGet("trial-balance")]
    public async Task<ActionResult<ApiResponse<TrialBalanceResultDto>>> GetTrialBalance(
        [FromQuery] TrialBalanceQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!request.ToDate.HasValue)
            return BadRequest(ApiHelper.Fail<TrialBalanceResultDto>("試算表必須指定截止日期"));
        if (request.FromDate.HasValue && request.FromDate.Value > request.ToDate.Value)
            return BadRequest(ApiHelper.Fail<TrialBalanceResultDto>("查詢起日不可晚於迄日"));

        var result = await _accountingReportingService.GetTrialBalanceAsync(request, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("financial-statements")]
    public async Task<ActionResult<ApiResponse<FinancialStatementsResultDto>>> GetFinancialStatements(
        [FromQuery] TrialBalanceQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!request.FromDate.HasValue || !request.ToDate.HasValue)
            return BadRequest(ApiHelper.Fail<FinancialStatementsResultDto>("財務報表必須指定起日與迄日"));
        if (request.FromDate.Value > request.ToDate.Value)
            return BadRequest(ApiHelper.Fail<FinancialStatementsResultDto>("查詢起日不可晚於迄日"));

        var result = await _accountingReportingService.GetFinancialStatementsAsync(
            request.FromDate.Value,
            request.ToDate.Value,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 取得分組傳票 (Vouchers)
    /// </summary>
    [Authorize]
    [HttpGet("vouchers")]
    public async Task<ActionResult<ApiResponse<List<AccountingVoucherDto>>>> GetVouchers(
        CancellationToken cancellationToken)
    {
        var vouchers = await _accountingVoucherService.GetVouchersAsync(cancellationToken);
        return Ok(vouchers);
    }

    /// <summary>
    /// 取得傳票明細
    /// </summary>
    [Authorize]
    [HttpGet("vouchers/{voucherId:guid}")]
    public async Task<ActionResult<ApiResponse<AccountingVoucherDto>>> GetVoucherById(
        Guid voucherId,
        CancellationToken cancellationToken)
    {
        var voucher = await _accountingVoucherService.GetVoucherAsync(voucherId, cancellationToken);
        return voucher is null
            ? NotFound(ApiHelper.Fail<AccountingVoucherDto>("找不到會計傳票"))
            : Ok(voucher);
    }

    /// <summary>
    /// 建立人工傳票草稿
    /// </summary>
    [Authorize]
    [HttpPost("vouchers")]
    public async Task<ActionResult<ApiResponse<AccountingVoucherDto>>> CreateVoucher(
        [FromBody] SaveAccountingVoucherRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail<AccountingVoucherDto>("無法識別目前登入使用者"));

        var (success, status, voucher) = await _accountingVoucherService.CreateDraftAsync(
            request, userId, cancellationToken);
        if (!success)
            return BadRequest(ApiHelper.Fail<AccountingVoucherDto>(GetVoucherErrorMessage(status)));

        return CreatedAtAction(
            nameof(GetVoucherById),
            new { voucherId = voucher!.Id },
            ApiResponse<AccountingVoucherDto>.Ok(voucher));
    }

    /// <summary>
    /// 更新尚未過帳的人工傳票草稿
    /// </summary>
    [Authorize]
    [HttpPut("vouchers/{voucherId:guid}")]
    public async Task<ActionResult<ApiResponse<AccountingVoucherDto>>> UpdateVoucher(
        Guid voucherId,
        [FromBody] SaveAccountingVoucherRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail<AccountingVoucherDto>("無法識別目前登入使用者"));

        var (success, status, voucher) = await _accountingVoucherService.UpdateDraftAsync(
            voucherId, request, userId, cancellationToken);
        if (!success)
            return VoucherError<AccountingVoucherDto>(status);

        return Ok(voucher!);
    }

    /// <summary>
    /// 將人工傳票草稿送交主管覆核
    /// </summary>
    [Authorize]
    [HttpPost("vouchers/{voucherId:guid}/submit")]
    public async Task<ActionResult<ApiResponse<AccountingVoucherDto>>> SubmitVoucher(
        Guid voucherId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail<AccountingVoucherDto>("無法識別目前登入使用者"));

        var (success, status, voucher) = await _accountingVoucherService.SubmitDraftAsync(
            voucherId, userId, cancellationToken);
        if (!success)
            return VoucherError<AccountingVoucherDto>(status);

        return Ok(voucher!);
    }

    /// <summary>
    /// 核准待覆核傳票並正式過帳
    /// </summary>
    [Authorize]
    [HttpPost("vouchers/{voucherId:guid}/approve")]
    public async Task<ActionResult<ApiResponse<AccountingVoucherDto>>> ApproveVoucher(
        Guid voucherId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail<AccountingVoucherDto>("無法識別目前登入使用者"));

        var (success, status, voucher) = await _accountingVoucherService.ApproveVoucherAsync(
            voucherId, userId, cancellationToken);
        if (!success)
            return VoucherError<AccountingVoucherDto>(status);

        return Ok(voucher!);
    }

    /// <summary>
    /// 取得過帳設定
    /// </summary>
    [Authorize]
    [HttpGet("posting-setups")]
    public async Task<ActionResult<ApiResponse<List<PostingSetupDto>>>> GetPostingSetups()
    {
        var setups = await _financeService.GetCostingSetupsAsync();
        return Ok(setups);
    }

    /// <summary>
    /// 初始化會計科目 (Seed)
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult<ApiResponse>> SeedAccounts()
    {
        await _financeService.SeedAccountsAsync();
        return Ok("財務科目已初始化");
    }

    /// <summary>
    /// 初始化銀行帳戶
    /// </summary>
    [HttpPost("seed-accounts")]
    public async Task<ActionResult<ApiResponse<string>>> SeedBankAccounts()
    {
        await _financeService.SeedBankAccountsAsync();
        return Ok<string>("銀行帳戶已初始化");
    }

    [HttpGet("loans")]
    public async Task<ActionResult<ApiResponse<List<LoanDto>>>> GetLoans([FromQuery] string? status)
    {
        var loans = await _financeService.GetLoansAsync(status);
        return Ok(loans);
    }

    [HttpPost("loans")]
    public async Task<ActionResult<ApiResponse>> CreateLoan([FromBody] CreateLoanRequest request)
    {
        await _financeService.CreateLoanAsync(request);
        return Ok("貸款已建立");
    }

    [HttpGet("bank-accounts")]
    public async Task<ActionResult<ApiResponse<List<BankAccountDto>>>> GetBankAccounts()
    {
        var accounts = await _financeService.GetBankAccountsAsync();
        return Ok(accounts);
    }

    [HttpPost("bank-accounts")]
    public async Task<ActionResult<ApiResponse>> CreateBankAccount([FromBody] CreateBankAccountRequest request)
    {
        if (!await _financeService.CreateBankAccountAsync(request))
            return BadRequest(ApiHelper.Fail("收款帳戶只能對應有效的現金／銀行總帳科目"));
        return Ok("銀行帳戶已建立");
    }

    [HttpPatch("bank-accounts/{id:guid}/gl-account")]
    public async Task<ActionResult<ApiResponse>> SetBankAccountGlMapping(
        Guid id,
        [FromBody] UpdateBankAccountGlMappingDto request)
    {
        if (!await _financeService.SetBankAccountGlMappingAsync(id, request.GLAccountId))
            return BadRequest(ApiHelper.Fail("銀行帳戶不存在，或指定科目不是有效的現金／銀行科目"));
        return Ok("收款帳戶總帳科目對應已更新");
    }

    [HttpGet("investments")]
    public async Task<ActionResult<ApiResponse<List<InvestmentDto>>>> GetInvestments([FromQuery] string? category)
    {
        var investments = await _financeService.GetInvestmentsAsync(category);
        return Ok(investments);
    }

    [HttpPost("investments")]
    public async Task<ActionResult<ApiResponse>> CreateInvestment([FromBody] CreateInvestmentRequest request)
    {
        await _financeService.CreateInvestmentAsync(request);
        return Ok("投資已建立");
    }

    [HttpGet("treasury-transfers")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetTreasuryTransfers()
    {
        var transfers = await _financeService.GetTreasuryTransfersAsync();
        return Ok(transfers);
    }

    [HttpPost("treasury-transfers")]
    public async Task<ActionResult<ApiResponse>> CreateTreasuryTransfer([FromBody] CreateTreasuryTransferRequest request)
    {
        await _financeService.CreateTreasuryTransferAsync(request);
        return Ok("資金調撥已建立");
    }

    [HttpGet("guarantees")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetGuarantees()
    {
        var guarantees = await _financeService.GetGuaranteesAsync();
        return Ok(guarantees);
    }

    [HttpPost("guarantees")]
    public async Task<ActionResult<ApiResponse>> CreateGuarantee([FromBody] CreateGuaranteeRequest request)
    {
        await _financeService.CreateGuaranteeAsync(request);
        return Ok("保證函已建立");
    }

    private ActionResult<ApiResponse<T>> VoucherError<T>(string status)
    {
        var message = GetVoucherErrorMessage(status);
        var response = ApiHelper.Fail<T>(message);
        return status switch
        {
            "not_found" => NotFound(response),
            "creator_cannot_approve" => StatusCode(StatusCodes.Status403Forbidden, response),
            "not_draft" or "not_pending_approval" => Conflict(response),
            _ => BadRequest(response)
        };
    }

    private static string GetVoucherErrorMessage(string status) =>
        status switch
        {
            "invalid_date" => "請提供有效的傳票日期",
            "invalid_type" => "傳票類型不支援",
            "invalid_entries" => "傳票至少需要兩筆分錄，且借貸金額不可為負或同時輸入",
            "invalid_account" => "分錄科目不存在或已停用",
            "invalid_cash_account" => "現金收入／支出傳票必須包含有效的現金或銀行科目",
            "invalid_cost_center" => "成本中心已停用",
            "period_closed" => "傳票日期所屬會計期間已關帳，禁止新增或過帳",
            "unbalanced" => "傳票借貸不平衡，無法過帳",
            "not_found" => "找不到會計傳票",
            "not_draft" => "只有草稿傳票可以修改或過帳",
            "not_pending_approval" => "只有待覆核傳票可以核准過帳",
            "creator_cannot_approve" => "傳票建立者不可核准自己的傳票",
            _ => "傳票處理失敗"
        };
}
