using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class PlanningRequest
{
    public string TaskDescription { get; set; } = null!;
    public string? Context { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string? ModelId { get; set; }
}

public class PlanningResponse
{
    public bool Success { get; set; }
    public string? PlanId { get; set; }
    public List<PlanTask> Tasks { get; set; } = new();
    public string Strategy { get; set; } = "serial";
    public decimal Confidence { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? Reasoning { get; set; }
}

public class PlanTask
{
    public string Id { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<string> Dependencies { get; set; } = new();
    public int EstimatedMinutes { get; set; }
    public List<string> ToolsNeeded { get; set; } = new();
    public string Status { get; set; } = "pending";
    public int Priority { get; set; } = 50;
}

public class ExecutionRequest
{
    public string PlanId { get; set; } = null!;
    public string? TaskId { get; set; }
    public bool ParallelExecution { get; set; } = false;
}

public class ExecutionResponse
{
    public bool Success { get; set; }
    public string? ExecutionId { get; set; }
    public List<TaskResult> Results { get; set; } = new();
    public decimal Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public AgentState FinalState { get; set; } = new();
}

public class TaskResult
{
    public string TaskId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Output { get; set; }
    public string? Error { get; set; }
    public decimal Duration { get; set; }
    public string? ToolUsed { get; set; }
}

public class ReviewRequest
{
    public string ExecutionId { get; set; } = null!;
    public string? Feedback { get; set; }
    public bool AutoApproval { get; set; } = true;
}

public class ReviewResponse
{
    public bool Approved { get; set; }
    public decimal QualityScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public string? Recommendations { get; set; }
    public List<string>? SuggestedFixes { get; set; }
}

public class KpiValidationResult
{
    public decimal AccuracyRate { get; set; }
    public decimal CurrentRoi { get; set; }
    public bool TargetMet { get; set; }
    public Dictionary<string, decimal> Metrics { get; set; } = new();
    public Dictionary<string, decimal> Trends { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class AgentState
{
    public string ExecutionId { get; set; } = null!;
    public string CurrentNode { get; set; } = "idle";
    public string Status { get; set; } = "pending";
    public Dictionary<string, object> Context { get; set; } = new();
    public List<string> History { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PlanningGraph
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public Dictionary<string, object> Config { get; set; } = new();
}

public class GraphEdge
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
    public string? Condition { get; set; }
}

public class ErpTool
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string Category { get; set; } = null!;
}

public class ToolExecuteRequestDto
{
    public string ToolId { get; set; } = null!;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class PlanningHealthDto
{
    public string Status { get; set; } = "healthy";
    public string Service { get; set; } = "AIPlanningService";
    public string Version { get; set; } = "1.0";
    public List<string> Features { get; set; } = new();
}

public class AgentExecutionRecord
{
    public string ExecutionId { get; set; } = null!;
    public string PlanId { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal SuccessRate { get; set; }
    public List<TaskResult> Tasks { get; set; } = new();
}
