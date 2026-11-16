using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class LlmRequest
{
    public string Prompt { get; set; } = null!;
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    public string Model { get; set; } = "gpt-4";
    public string DataClassification { get; set; } = "unclassified";
}

public class LlmResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
    public string? Model { get; set; }
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
