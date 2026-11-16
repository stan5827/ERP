using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/cash-receipts")]
public class CashReceiptsController : BaseController
{
    private readonly ICashReceiptService _cashReceiptService;

    public CashReceiptsController(ICashReceiptService cashReceiptService) => _cashReceiptService = cashReceiptService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CashReceiptDto>>>> GetCashReceipts([FromQuery] Guid? customerId)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var items = await _cashReceiptService.GetCashReceiptsAsync(customerId, page, pageSize);
        return Ok(items, totalCount: items.Count);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CashReceiptDto>>> CreateCashReceipt([FromBody] CreateCashReceiptRequest req)
    {
        var (dto, status) = await _cashReceiptService.CreateAsync(req);
        return status switch
        {
            "invalid" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("�ݭn�ܤ֤@���o���־P����")),
            "insufficient" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("�־P�`���B�W�L�������ڪ��B")),
            "receipt_amount_mismatch" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("沖帳現金不可超過實收金額，且折讓總額必須等於各發票折讓分配")),
            "cash_account_mapping_required" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("收款帳戶尚未對應有效的現金／銀行總帳科目")),
            "ar_account_missing" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("未設定有效的應收帳款科目 1131")),
            "unapplied_account_mapping_required" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("未設定有效的客戶預收款／未沖款總帳科目映射")),
            "discount_policy_pending" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("折讓過帳暫停，待財務確認銷項稅額處理政策")),
            "currency_mismatch" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("收款、銀行帳戶與發票幣別必須一致")),
            "not_found_invoice" => BadRequest(ApiHelper.FailOf<CashReceiptDto>($"�䤣��o��")),
            "customer_mismatch" => BadRequest(ApiHelper.FailOf<CashReceiptDto>($"�o�����ݩ󦹫Ȥ�")),
            "invoice_not_approved" => BadRequest(ApiHelper.FailOf<CashReceiptDto>($"�o�����A�D�w�֭�A�L�k����")),
            "over_allocation" => BadRequest(ApiHelper.FailOf<CashReceiptDto>($"�־P���B�����W�L�ݦ��l�B")),
            _ => Ok(dto!, status switch
            {
                "paid" => "Receipt created; one or more invoices are fully settled",
                "unapplied" => "Receipt created with an unapplied balance",
                _ => "Receipt created"
            })
        };
    }

    [HttpPost("{receiptId:guid}/apply-unapplied")]
    public async Task<ActionResult<ApiResponse<CashReceiptDto>>> ApplyUnapplied(
        Guid receiptId,
        [FromBody] ApplyCashReceiptUnappliedRequestDto req)
    {
        var (dto, status) = await _cashReceiptService.ApplyUnappliedAsync(receiptId, req);
        return status switch
        {
            "invalid" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("發票與沖轉金額必須有效，金額須大於零")),
            "not_found_receipt" => NotFound(ApiHelper.FailOf<CashReceiptDto>("Cash receipt not found")),
            "not_found_invoice" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("Sales invoice not found")),
            "over_unapplied" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("沖轉金額超過收款單未沖款餘額")),
            "over_allocation" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("沖轉金額超過發票未收餘額")),
            "customer_mismatch" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("收款客戶與發票客戶不一致")),
            "invoice_not_approved" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("發票尚未核准，無法沖轉")),
            "currency_mismatch" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("收款與發票幣別必須一致")),
            "unapplied_account_mapping_required" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("未設定有效的客戶預收款／未沖款總帳科目映射")),
            "ar_account_missing" => BadRequest(ApiHelper.FailOf<CashReceiptDto>("未設定有效的應收帳款科目 1131")),
            _ => Ok(dto!, status == "paid"
                ? "Unapplied balance applied; invoice fully settled"
                : "Unapplied balance applied")
        };
    }

    [HttpGet("pending-invoices/{customerId}")]
    public async Task<ActionResult<ApiResponse<List<PendingInvoiceDto>>>> GetPendingInvoices(Guid customerId)
    {
        var invoices = await _cashReceiptService.GetPendingInvoicesAsync(customerId);
        return Ok(invoices);
    }
}
