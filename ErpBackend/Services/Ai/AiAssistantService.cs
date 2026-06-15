using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Services.Ai;

public record AiChatMessage(string Role, string Content);
public record AiChatResponse(string Content, List<string>? Citations = null);

public interface IAiAssistantService
{
    Task<AiChatResponse> AskAsync(string prompt, List<AiChatMessage>? history = null);
    Task<string> SummarizeOrderAsync(Guid orderId);
}

public class AiAssistantService : IAiAssistantService
{
    private readonly ILogger<AiAssistantService> _logger;
    private readonly ILlmService _llmService;
    private readonly ErpGeneralDbContext _db;

    public AiAssistantService(ILogger<AiAssistantService> logger, ILlmService llmService, ErpGeneralDbContext db)
    {
        _logger = logger;
        _llmService = llmService;
        _db = db;
    }

    public async Task<AiChatResponse> AskAsync(string prompt, List<AiChatMessage>? history = null)
    {
        _logger.LogInformation("AI Assistant processing prompt: {Prompt}", prompt);
        var stopwatch = Stopwatch.StartNew();

        var tools = new List<LlmTool>
        {
            new LlmTool 
            { 
                Name = "check_inventory", 
                Description = "檢查特定 SKU 的庫存數量", 
                Parameters = new { type = "object", properties = new { sku_code = new { type = "string", description = "SKU 代碼" } }, required = new[] { "sku_code" } } 
            },
            new LlmTool 
            { 
                Name = "get_sales_summary", 
                Description = "取得過去幾天的銷售總額摘要", 
                Parameters = new { type = "object", properties = new { days = new { type = "integer", description = "天數" } }, required = new[] { "days" } } 
            },
            new LlmTool
            {
                Name = "get_purchase_order_status",
                Description = "查詢採購單 (PO) 的狀態與金額",
                Parameters = new { type = "object", properties = new { po_number = new { type = "string", description = "採購單號" } }, required = new[] { "po_number" } }
            },
            new LlmTool
            {
                Name = "get_supplier_info",
                Description = "查詢供應商的聯繫資訊",
                Parameters = new { type = "object", properties = new { supplier_code = new { type = "string", description = "供應商編碼" } }, required = new[] { "supplier_code" } }
            }
        };

        var messages = new List<LlmChatMessage>();
        if (history != null)
        {
            messages.AddRange(history.Select(h => new LlmChatMessage { Role = h.Role.ToLower(), Content = h.Content }));
        }
        messages.Add(new LlmChatMessage { Role = "user", Content = prompt });

        LlmResponse? lastResponse = null;
        int turns = 0;
        const int maxTurns = 5;

        while (turns < maxTurns)
        {
            var llmRequest = new LlmRequest
            {
                SystemPrompt = "你是一位專業的 ERP AI 助手。你可以調用工具來獲取即時數據。請務必提供準確的資訊。",
                Messages = messages,
                Tools = tools,
                Model = "gpt-4"
            };

            lastResponse = await _llmService.CompleteAsync(llmRequest);
            turns++;

            if (!lastResponse.Success || lastResponse.ToolCalls == null || !lastResponse.ToolCalls.Any())
                break;

            messages.Add(new LlmChatMessage 
            { 
                Role = "assistant", 
                Content = lastResponse.Content,
                ToolCalls = lastResponse.ToolCalls 
            });

            foreach (var toolCall in lastResponse.ToolCalls)
            {
                var result = await ExecuteToolAsync(toolCall);
                messages.Add(new LlmChatMessage 
                { 
                    Role = "tool", 
                    ToolCallId = toolCall.Id, 
                    Content = result 
                });
            }
        }

        stopwatch.Stop();
        if (lastResponse != null)
        {
            LlmMetricsService.RecordRequest("ERPunknown", lastResponse.Model ?? "gpt-4", lastResponse.Success, stopwatch.Elapsed.TotalSeconds, lastResponse.TokensUsed);
        }

        if (lastResponse == null || !lastResponse.Success)
        {
            return new AiChatResponse("執行過程中發生錯誤，請稍後再試。");
        }

        return new AiChatResponse(lastResponse.Content ?? "未收到回應。");
    }

    private async Task<string> ExecuteToolAsync(LlmToolCall toolCall)
    {
        _logger.LogInformation("Executing tool: {ToolName} with args: {Args}", toolCall.Name, toolCall.Arguments);
        
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.Arguments, options);
            
            switch (toolCall.Name)
            {
                case "check_inventory":
                {
                    if (args == null || !args.TryGetValue("sku_code", out var skuCodeObj)) return "錯誤：缺少 sku_code 參數";
                    var skuCode = skuCodeObj.ToString();
                    var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuCode == skuCode);
                    return sku != null ? $"SKU: {sku.SkuName} ({sku.SkuCode}), 庫存量: {sku.CurrentStock}" : "找不到該 SKU";
                }

                case "get_sales_summary":
                {
                    if (args == null || !args.TryGetValue("days", out var daysObj)) return "錯誤：缺少 days 參數";
                    var days = int.Parse(daysObj.ToString()!);
                    var startDate = DateTime.UtcNow.AddDays(-days);
                    var total = await _db.SalesOrders
                        .Where(o => o.OrderDate >= startDate && o.Status != "cancelled")
                        .SumAsync(o => o.TotalAmount);
                    return $"過去 {days} 天的銷售總額為: TWD {total:N0}";
                }

                case "get_purchase_order_status":
                {
                    if (args == null || !args.TryGetValue("po_number", out var poNumObj)) return "錯誤：缺少 po_number 參數";
                    var poNum = poNumObj.ToString();
                    var po = await _db.PurchaseOrders
                        .Include(p => p.Supplier)
                        .FirstOrDefaultAsync(p => p.PoNumber == poNum);
                    if (po == null) return "找不到該採購單";
                    return $"單號: {po.PoNumber}, 供應商: {po.Supplier.SupplierName}, 狀態: {po.Status}, 總額: {po.TotalAmount:N2}";
                }

                case "get_supplier_info":
                {
                    if (args == null || !args.TryGetValue("supplier_code", out var codeObj)) return "錯誤：缺少 supplier_code 參數";
                    var code = codeObj.ToString();
                    var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierCode == code);
                    if (supplier == null) return "找不到該供應商";
                    return $"名稱: {supplier.SupplierName}, 聯繫人: {supplier.ContactPerson}, 電話: {supplier.Phone}, 評級: {supplier.Rating}";
                }

                default:
                    return "未知工具";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed");
            return $"錯誤: {ex.Message}";
        }
    }

    public async Task<string> SummarizeOrderAsync(Guid orderId)
    {
        var stopwatch = Stopwatch.StartNew();
        var order = await _db.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return "找不到指定的訂單。";

        var orderDetails = $"訂單號碼: {order.OrderNumber}, 客戶: {order.CustomerId}, 總額: {order.TotalAmount}, 狀態: {order.Status}. " +
                           $"品項數: {order.Items.Count}.";

        var llmRequest = new LlmRequest
        {
            SystemPrompt = "請摘要以下 ERP 銷售訂單資訊，並給出專業建議。",
            Prompt = $"訂單資料：{orderDetails}"
        };

        var response = await _llmService.CompleteAsync(llmRequest);
        stopwatch.Stop();

        if (response.Success)
        {
            LlmMetricsService.RecordRequest("ERPunknown", response.Model ?? "gpt-4", true, stopwatch.Elapsed.TotalSeconds, response.TokensUsed);
            return response.Content ?? "摘要失敗。";
        }
        
        return "摘要生成時發生錯誤。";
    }
}
