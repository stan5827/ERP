using Prometheus;

namespace ErpBackend.Services;

public class AgentMetricsService
{
    private static readonly Histogram AgentLatency = Metrics
        .CreateHistogram("erp_ai_agent_latency_seconds", "AI Agent processing latency in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "agent_type", "task_type" },
                Buckets = new[] { 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0 }
            });

    private static readonly Counter AgentErrors = Metrics
        .CreateCounter("erp_ai_agent_errors_total", "Total AI Agent errors",
            new CounterConfiguration { LabelNames = new[] { "agent_type", "error_type" } });

    private static readonly Counter AgentTokens = Metrics
        .CreateCounter("erp_ai_agent_tokens_total", "Total AI Agent token consumption",
            new CounterConfiguration { LabelNames = new[] { "agent_type", "model" } });

    private static readonly Gauge AgentHealthScore = Metrics
        .CreateGauge("erp_ai_agent_health_score", "AI Agent health score (0-1)",
            new GaugeConfiguration { LabelNames = new[] { "agent_type" } });

    private static readonly Counter AgentRequests = Metrics
        .CreateCounter("erp_ai_agent_requests_total", "Total AI Agent requests",
            new CounterConfiguration { LabelNames = new[] { "agent_type", "status" } });

    private static readonly Gauge WorkOrdersActive = Metrics
        .CreateGauge("erp_work_orders_active", "Currently active work orders");

    private static readonly Gauge InventoryLowStock = Metrics
        .CreateGauge("erp_inventory_low_stock_count", "Number of low stock items");

    private static readonly Counter AgentFlowExecutions = Metrics
        .CreateCounter("erp_agent_flow_executions_total", "Agent Flow execution count",
            new CounterConfiguration { LabelNames = new[] { "flow_type", "status" } });

    private static readonly Histogram AgentFlowDuration = Metrics
        .CreateHistogram("erp_agent_flow_duration_seconds", "Agent Flow execution duration",
            new HistogramConfiguration
            {
                LabelNames = new[] { "flow_type" },
                Buckets = new[] { 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0 }
            });

    public void RecordAgentLatency(string agentType, string taskType, double seconds)
    {
        AgentLatency.WithLabels(agentType, taskType).Observe(seconds);
    }

    public void RecordAgentError(string agentType, string errorType)
    {
        AgentErrors.WithLabels(agentType, errorType).Inc();
    }

    public void RecordAgentTokens(string agentType, string model, int tokens)
    {
        AgentTokens.WithLabels(agentType, model).Inc(tokens);
    }

    public void SetAgentHealthScore(string agentType, double score)
    {
        AgentHealthScore.WithLabels(agentType).Set(score);
    }

    public void RecordAgentRequest(string agentType, string status)
    {
        AgentRequests.WithLabels(agentType, status).Inc();
    }

    public void UpdateWorkOrdersActive(int count)
    {
        WorkOrdersActive.Set(count);
    }

    public void UpdateInventoryLowStock(int count)
    {
        InventoryLowStock.Set(count);
    }

    public void RecordAgentFlowExecution(string flowType, string status, double durationSeconds)
    {
        AgentFlowExecutions.WithLabels(flowType, status).Inc();
        AgentFlowDuration.WithLabels(flowType).Observe(durationSeconds);
    }
}
