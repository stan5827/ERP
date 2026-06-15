using System.Text.Json;
using System.Net.Http;
using System.Text;
using Prometheus;

namespace ErpBackend.Services;

public interface ILlmService
{
    Task<LlmResponse> CompleteAsync(LlmRequest request);
    Task<string> GetModelName();
}

public class LlmRequest
{
    public string? Prompt { get; set; }
    public string? SystemPrompt { get; set; }
    public List<LlmChatMessage>? Messages { get; set; }
    public List<LlmTool>? Tools { get; set; }
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    public string Model { get; set; } = "gpt-4";
}

public class LlmChatMessage
{
    public string Role { get; set; } = null!;
    public string? Content { get; set; }
    public List<LlmToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
}

public class LlmTool
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public object Parameters { get; set; } = null!;
}

public class LlmToolCall
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Arguments { get; set; } = null!;
}

public class LlmResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public List<LlmToolCall>? ToolCalls { get; set; }
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
    public string? Model { get; set; }
}

public class LlmConfig
{
    public string Provider { get; set; } = "openai";
    public string ApiKey { get; set; } = null!;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string DefaultModel { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    public int TimeoutSeconds { get; set; } = 60;
}

public class OpenAiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmConfig _config;
    private readonly ILogger<OpenAiLlmService> _logger;

    public OpenAiLlmService(HttpClient httpClient, LlmConfig config, ILogger<OpenAiLlmService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    }

    public Task<string> GetModelName() => Task.FromResult(_config.DefaultModel);

    public async Task<LlmResponse> CompleteAsync(LlmRequest request)
    {
        try
        {
            var payload = new
            {
                model = string.IsNullOrEmpty(request.Model) ? _config.DefaultModel : request.Model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt ?? GetDefaultSystemPrompt() },
                    new { role = "user", content = request.Prompt ?? string.Empty }
                },
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : _config.MaxTokens,
                temperature = request.Temperature > 0 ? request.Temperature : _config.Temperature
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling LLM with model: {Model}", request.Model);

            var response = await _httpClient.PostAsync("/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("LLM API error: {Status} - {Error}", response.StatusCode, errorContent);
                
                return new LlmResponse
                {
                    Success = false,
                    Error = $"API Error: {response.StatusCode}",
                    Model = request.Model
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAiResponse>(responseJson);

            return new LlmResponse
            {
                Success = true,
                Content = result?.Choices?.FirstOrDefault()?.Message?.Content,
                TokensUsed = result?.Usage?.TotalTokens ?? 0,
                Model = result?.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM request failed");
            return new LlmResponse
            {
                Success = false,
                Error = ex.Message,
                Model = request.Model
            };
        }
    }

    private string GetDefaultSystemPrompt()
    {
        return @"你是一位專業的ERP AI規劃代理，擅長將複雜的商業任務分解為可執行的子任務。

你應該：
1. 理解用戶的任務需求
2. 分析任務複雜度
3. 將任務分解為邏輯清晰的子任務
4. 選擇適當的工具和方法
5. 產出結構化的執行計畫

輸出格式應為JSON，包含：
- tasks: 子任務清單
- strategy: 執行策略（parallel/serial/mixed）
- confidence: 信心度";
    }
}

public class OpenAiResponse
{
    public string? Id { get; set; }
    public string? Model { get; set; }
    public List<OpenAiChoice>? Choices { get; set; }
    public OpenAiUsage? Usage { get; set; }
}

public class OpenAiChoice
{
    public int Index { get; set; }
    public OpenAiMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class OpenAiMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class OpenAiUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class ClaudeLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmConfig _config;
    private readonly ILogger<ClaudeLlmService> _logger;

    public ClaudeLlmService(HttpClient httpClient, LlmConfig config, ILogger<ClaudeLlmService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public Task<string> GetModelName() => Task.FromResult(_config.DefaultModel);

    public async Task<LlmResponse> CompleteAsync(LlmRequest request)
    {
        try
        {
            var payload = new
            {
                model = string.IsNullOrEmpty(request.Model) ? _config.DefaultModel : request.Model,
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : _config.MaxTokens,
                temperature = request.Temperature > 0 ? request.Temperature : _config.Temperature,
                messages = new[]
                {
                    new { role = "user", content = $"{request.SystemPrompt ?? ""}\n\n{request.Prompt}" }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Claude with model: {Model}", request.Model);

            var response = await _httpClient.PostAsync("/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Claude API error: {Status} - {Error}", response.StatusCode, errorContent);

                return new LlmResponse
                {
                    Success = false,
                    Error = $"API Error: {response.StatusCode}",
                    Model = request.Model
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ClaudeResponse>(responseJson);

            return new LlmResponse
            {
                Success = true,
                Content = result?.Content?.FirstOrDefault()?.Text,
                TokensUsed = (result?.Usage?.InputTokens ?? 0) + (result?.Usage?.OutputTokens ?? 0),
                Model = result?.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude request failed");
            return new LlmResponse
            {
                Success = false,
                Error = ex.Message,
                Model = request.Model
            };
        }
    }
}

public class ClaudeResponse
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public List<ClaudeContent>? Content { get; set; }
    public string? Model { get; set; }
    public ClaudeUsage? Usage { get; set; }
}

public class ClaudeContent
{
    public string? Type { get; set; }
    public string? Text { get; set; }
}

public class ClaudeUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

public class LlmFactory
{
    public static ILlmService Create(LlmConfig config, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        return config.Provider.ToLower() switch
        {
            "anthropic" or "claude" => new ClaudeLlmService(httpClient, config, loggerFactory.CreateLogger<ClaudeLlmService>()),
            _ => new OpenAiLlmService(httpClient, config, loggerFactory.CreateLogger<OpenAiLlmService>())
        };
    }
}

public class LlmMetricsService
{
    private static readonly Counter LlmRequests = Metrics
        .CreateCounter("erp_llm_requests_total", "Total LLM API requests",
            new CounterConfiguration { LabelNames = new[] { "provider", "model", "status" } });

    private static readonly Histogram LlmLatency = Metrics
        .CreateHistogram("erp_llm_latency_seconds", "LLM API latency in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "provider", "model" },
                Buckets = new[] { 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0 }
            });

    private static readonly Counter LlmTokens = Metrics
        .CreateCounter("erp_llm_tokens_total", "Total LLM tokens used",
            new CounterConfiguration { LabelNames = new[] { "provider", "model" } });

    public static void RecordRequest(string provider, string model, bool success, double latencySeconds, int tokens)
    {
        LlmRequests.WithLabels(provider, model, success ? "success" : "error").Inc();
        LlmLatency.WithLabels(provider, model).Observe(latencySeconds);
        if (tokens > 0)
            LlmTokens.WithLabels(provider, model).Inc(tokens);
    }
}
