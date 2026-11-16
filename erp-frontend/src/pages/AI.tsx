import React, { useState, useMemo } from 'react';
import {
  AreaChart, Area, BarChart, Bar, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, PieChart, Pie, Cell
} from 'recharts';
import {
  Activity, AlertTriangle, CheckCircle, Clock, Cpu, GitBranch,
  RefreshCw, Server, Zap, Shield, ChevronDown, Eye, Play, Settings, BookOpen
} from 'lucide-react';
import { useAgentMetricsSummary, useAgentLatencyHistory, useAgentQpsHistory, useAgentErrorHistory, useAgentFlowExecutions, useAgentTopology, usePlanningCreatePlan, usePlanningExecutePlan, usePlanningToolExecute } from '../hooks/useApi';
import { AgentWipFactsPanel } from './AgentWipFactsPanel';

// ── Mock data generators (fallback when API unavailable) ──
function generateMockLatencyData(hours: number) {
  const data = [];
  const now = Date.now();
  for (let i = hours * 4; i >= 0; i--) {
    const ts = new Date(now - i * 15 * 60000);
    data.push({
      timestamp: ts.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit' }),
      p50: 120 + Math.random() * 80,
      p95: 250 + Math.random() * 150,
      p99: 400 + Math.random() * 200,
    });
  }
  return data;
}

function generateMockQpsData(hours: number) {
  const data = [];
  const now = Date.now();
  for (let i = hours * 4; i >= 0; i--) {
    const ts = new Date(now - i * 15 * 60000);
    data.push({
      timestamp: ts.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit' }),
      qps: Math.max(0, 45 + Math.random() * 30 - 5),
    });
  }
  return data;
}

function generateMockErrorData(hours: number) {
  const data = [];
  const now = Date.now();
  for (let i = hours * 4; i >= 0; i--) {
    const ts = new Date(now - i * 15 * 60000);
    data.push({
      timestamp: ts.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit' }),
      count: Math.floor(Math.random() * 5),
      rate: Math.random() * 0.02,
    });
  }
  return data;
}

const mockFlowExecutions = [
  {
    id: 'flow-001', flowType: 'anomaly_dispatch' as const, flowName: '產線異常派工',
    status: 'success' as const, triggerSource: 'IoT Sensor #A3-12',
    startTime: '2026-03-25 14:32:10', endTime: '2026-03-25 14:32:18', duration: 8.2,
    businessImpact: '減少停機時間 25 分鐘',
    steps: [
      { id: 's1', name: 'AI 異常偵測', status: 'success' as const, duration: 1.2 },
      { id: 's2', name: '建立維修單', status: 'success' as const, duration: 0.8 },
      { id: 's3', name: '查詢可用維修人員', status: 'success' as const, duration: 0.5 },
      { id: 's4', name: '派工並回寫 ERP', status: 'success' as const, duration: 1.1 },
      { id: 's5', name: 'LINE 推播通知', status: 'success' as const, duration: 0.6 },
    ]
  },
  {
    id: 'flow-002', flowType: 'customer_inquiry' as const, flowName: '客服詢價自動化',
    status: 'success' as const, triggerSource: 'LINE 官方帳號',
    startTime: '2026-03-25 14:28:45', endTime: '2026-03-25 14:28:52', duration: 7.1,
    businessImpact: '回應時間縮短至 7 秒',
    steps: [
      { id: 's1', name: 'NLP 解析需求', status: 'success' as const, duration: 1.5 },
      { id: 's2', name: '查詢庫存', status: 'success' as const, duration: 0.6 },
      { id: 's3', name: '比對供應鏈交期', status: 'success' as const, duration: 0.9 },
      { id: 's4', name: '生成報價方案', status: 'success' as const, duration: 2.1 },
      { id: 's5', name: '回傳 LINE', status: 'success' as const, duration: 0.8 },
    ]
  },
  {
    id: 'flow-003', flowType: 'inventory_replenishment' as const, flowName: '庫存預警補貨',
    status: 'failed' as const, triggerSource: '定時排程 (每小時)',
    startTime: '2026-03-25 14:00:00', endTime: '2026-03-25 14:00:45', duration: 45.3,
    businessImpact: '觸發人工審核',
    steps: [
      { id: 's1', name: '掃描低庫存', status: 'success' as const, duration: 2.1 },
      { id: 's2', name: 'AI 預測補貨量', status: 'success' as const, duration: 5.8 },
      { id: 's3', name: '比對供應商價格', status: 'success' as const, duration: 3.2 },
      { id: 's4', name: '生成採購申請', status: 'failed' as const, duration: 1.5, error: '供應商 API 超時' },
      { id: 's5', name: '審批流程', status: 'skipped' as const },
    ]
  },
  {
    id: 'flow-004', flowType: 'anomaly_dispatch' as const, flowName: '產線異常派工',
    status: 'running' as const, triggerSource: 'AI Vision 檢測',
    startTime: '2026-03-25 14:35:00',
    businessImpact: '處理中',
    steps: [
      { id: 's1', name: 'AI 異常偵測', status: 'success' as const, duration: 1.8 },
      { id: 's2', name: '建立維修單', status: 'success' as const, duration: 0.9 },
      { id: 's3', name: '查詢可用維修人員', status: 'running' as const },
      { id: 's4', name: '派工並回寫 ERP', status: 'pending' as const },
      { id: 's5', name: 'LINE 推播通知', status: 'pending' as const },
    ]
  },
];

const mockTopology = [
  { id: 'agent-dispatch', name: '異常派工 Agent', type: 'agent', status: 'online', connections: ['erp-wo', 'line'] },
  { id: 'agent-inquiry', name: '客服詢價 Agent', type: 'agent', status: 'online', connections: ['erp-inv', 'erp-quote', 'line'] },
  { id: 'agent-inventory', name: '庫存預警 Agent', type: 'agent', status: 'degraded', connections: ['erp-inv', 'erp-po'] },
  { id: 'erp-wo', name: 'ERP 工單模組', type: 'erp', status: 'online', connections: ['mes'] },
  { id: 'erp-inv', name: 'ERP 庫存模組', type: 'erp', status: 'online', connections: [] },
  { id: 'erp-quote', name: 'ERP 報價模組', type: 'erp', status: 'online', connections: [] },
  { id: 'erp-po', name: 'ERP 採購模組', type: 'erp', status: 'online', connections: [] },
  { id: 'mes', name: 'MES 系統', type: 'mes', status: 'online', connections: ['sensor'] },
  { id: 'line', name: 'LINE 官方帳號', type: 'line', status: 'online', connections: [] },
  { id: 'sensor', name: 'IoT 感測器', type: 'sensor', status: 'online', connections: [] },
];

// ── Status helpers ──
const statusColors: Record<string, string> = {
  healthy: 'var(--emerald-400)', online: 'var(--emerald-400)',
  degraded: 'var(--amber-400)', warning: 'var(--amber-400)',
  critical: 'var(--crimson-400)', offline: 'var(--crimson-400)', failed: 'var(--crimson-400)',
  running: 'var(--cobalt-400)',
  success: 'var(--emerald-400)',
  pending: 'var(--slate-500)',
  skipped: 'var(--slate-600)',
};

const flowTypeLabels: Record<string, string> = {
  anomaly_dispatch: '產線異常派工',
  customer_inquiry: '客服詢價自動化',
  inventory_replenishment: '庫存預警補貨',
};

const statusLabels: Record<string, string> = {
  healthy: '健康', degraded: '降級', critical: '嚴重',
  running: '執行中', success: '成功', failed: '失敗', pending: '待處理', skipped: '已跳過',
  online: '在線', offline: '離線',
};

const CHART_COLORS = {
  cobalt: '#2d6ef5',
  emerald: '#10b981',
  amber: '#f59e0b',
  crimson: '#c71f3d',
  slate: '#64748b',
};

// ── Custom Tooltip ──
const CustomTooltip = ({ active, payload, label }: { active?: boolean; payload?: Array<{ name: string; value: number; color: string }>; label?: string }) => {
  if (!active || !payload?.length) return null;
  return (
    <div className="glass-card no-lift" style={{ padding: '10px 14px', fontSize: 12 }}>
      <div style={{ color: 'var(--slate-400)', marginBottom: 6, fontFamily: 'var(--font-mono)' }}>{label}</div>
      {payload.map((p, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 2 }}>
          <div style={{ width: 8, height: 8, borderRadius: 2, background: p.color }} />
          <span style={{ color: 'var(--slate-400)' }}>{p.name}</span>
          <span style={{ color: 'var(--slate-100)', marginLeft: 'auto', fontFamily: 'var(--font-mono)' }}>
            {typeof p.value === 'number' ? (p.value < 1 ? (p.value * 100).toFixed(2) + '%' : p.value < 100 ? p.value.toFixed(1) : p.value.toFixed(0)) : p.value}
          </span>
        </div>
      ))}
    </div>
  );
};

// ── Health Score Gauge ──
function HealthGauge({ score, label, size = 100 }: { score: number; label: string; size?: number }) {
  const color = score >= 0.95 ? CHART_COLORS.emerald : score >= 0.85 ? CHART_COLORS.amber : CHART_COLORS.crimson;
  const radius = size / 2 - 8;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference * (1 - score);
  return (
    <div style={{ textAlign: 'center' }}>
      <svg width={size} height={size} style={{ transform: 'rotate(-90deg)' }}>
        <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke="rgba(148,163,184,0.1)" strokeWidth={6} />
        <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke={color} strokeWidth={6}
          strokeDasharray={circumference} strokeDashoffset={offset} strokeLinecap="round"
          style={{ transition: 'stroke-dashoffset 0.8s ease' }} />
      </svg>
      <div style={{
        position: 'relative', marginTop: -(size / 2 + 12), height: size, display: 'flex',
        alignItems: 'center', justifyContent: 'center', flexDirection: 'column',
      }}>
        <span className="mono" style={{ fontSize: 22, fontWeight: 700, color }}>{(score * 100).toFixed(1)}%</span>
      </div>
      <div style={{ marginTop: 8, fontSize: 11, color: 'var(--slate-400)', fontWeight: 600 }}>{label}</div>
    </div>
  );
}

// ── Flow Step Badge ──
function StepBadge({ status }: { status: string }) {
  const icons: Record<string, React.ReactNode> = {
    success: <CheckCircle size={12} />, failed: <AlertTriangle size={12} />,
    running: <RefreshCw size={12} className="spin" />, pending: <Clock size={12} />, skipped: <Clock size={12} />,
  };
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 10, fontWeight: 700,
      padding: '2px 8px', borderRadius: 4,
      background: `${statusColors[status]}18`, color: statusColors[status],
      textTransform: 'uppercase', letterSpacing: '0.05em',
    }}>
      {icons[status]} {statusLabels[status]}
    </span>
  );
}

// ── Flow Execution Card ──
function FlowCard({ flow }: { flow: typeof mockFlowExecutions[0] }) {
  const [expanded, setExpanded] = useState(false);
  return (
    <div className="glass-card" style={{ padding: 16, cursor: 'pointer', transition: 'all var(--transition-fast)' }}
      onClick={() => setExpanded(!expanded)}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <StepBadge status={flow.status} />
          <span style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-100)' }}>{flow.flowName}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {flow.duration && <span className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{flow.duration.toFixed(1)}s</span>}
          <ChevronDown size={14} style={{ color: 'var(--slate-500)', transform: expanded ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }} />
        </div>
      </div>
      <div style={{ display: 'flex', gap: 16, fontSize: 11, color: 'var(--slate-500)', marginBottom: expanded ? 12 : 0 }}>
        <span>觸發: {flow.triggerSource}</span>
        <span>時間: {flow.startTime}</span>
        {flow.businessImpact && <span style={{ color: 'var(--emerald-400)' }}>影響: {flow.businessImpact}</span>}
      </div>
      {expanded && (
        <div style={{ borderTop: '1px solid rgba(148,163,184,0.1)', paddingTop: 12, marginTop: 8 }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {flow.steps.map((step, i) => (
              <div key={step.id} style={{ display: 'flex', alignItems: 'center', gap: 10, paddingLeft: 8 }}>
                <div style={{
                  width: 20, height: 20, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  background: `${statusColors[step.status]}20`, color: statusColors[step.status], fontSize: 10, fontWeight: 700,
                }}>{i + 1}</div>
                <span style={{ fontSize: 12, color: 'var(--slate-300)', flex: 1 }}>{step.name}</span>
                <StepBadge status={step.status} />
                {(step as { duration?: number }).duration && <span className="mono" style={{ fontSize: 10, color: 'var(--slate-500)' }}>{((step as { duration: number }).duration).toFixed(1)}s</span>}
                {(step as { error?: string }).error && <span style={{ fontSize: 10, color: 'var(--crimson-400)' }}>{(step as { error: string }).error}</span>}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Topology Mini View ──
function TopologyView({ nodes }: { nodes: typeof mockTopology }) {
  const typeIcons: Record<string, React.ReactNode> = {
    agent: <Cpu size={14} />, erp: <Server size={14} />, mes: <Activity size={14} />,
    line: <Zap size={14} />, sensor: <Eye size={14} />,
  };
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
      {nodes.map(node => (
        <div key={node.id} className="glass-card" style={{
          padding: '10px 14px', display: 'flex', alignItems: 'center', gap: 8,
          borderLeft: `3px solid ${statusColors[node.status]}`,
          minWidth: 140,
        }}>
          <span style={{ color: statusColors[node.status] }}>{typeIcons[node.type]}</span>
          <div>
            <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--slate-200)' }}>{node.name}</div>
            <div style={{ fontSize: 9, color: statusColors[node.status], fontWeight: 700, textTransform: 'uppercase' }}>
              {statusLabels[node.status]}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Main Dashboard ──
export function AI() {
  const [timeRange, setTimeRange] = useState(24);
  const [flowFilter, setFlowFilter] = useState<string>('all');

  const { data: summaryResp } = useAgentMetricsSummary();
  const { data: latencyResp } = useAgentLatencyHistory(timeRange);
  const { data: qpsResp } = useAgentQpsHistory(timeRange);
  const { data: errorResp } = useAgentErrorHistory(timeRange);
  const { data: flowResp } = useAgentFlowExecutions();
  const { data: topologyResp } = useAgentTopology();

  // Use mock data when API unavailable
  const latencyData = latencyResp?.data || generateMockLatencyData(timeRange);
  const qpsData = qpsResp?.data || generateMockQpsData(timeRange);
  const errorData = errorResp?.data || generateMockErrorData(timeRange);
  const flowExecutions = (flowResp?.data || mockFlowExecutions) as typeof mockFlowExecutions;
  const topology = topologyResp?.data || mockTopology;

  const summary = summaryResp?.data || {
    totalRequests: 12847, successRate: 0.9923, avgLatencyMs: 156,
    p95LatencyMs: 342, errorRate: 0.0077, tokenUsage24h: 284500,
    activeAgents: 3,
    healthScores: [
      { agentType: '異常派工', score: 0.987, status: 'healthy' as const, uptime: 99.8, lastCheck: '14:35:12' },
      { agentType: '客服詢價', score: 0.995, status: 'healthy' as const, uptime: 99.9, lastCheck: '14:35:10' },
      { agentType: '庫存預警', score: 0.872, status: 'degraded' as const, uptime: 96.2, lastCheck: '14:35:08' },
    ]
  };

  const filteredFlows = useMemo(() => {
    if (flowFilter === 'all') return flowExecutions;
    return flowExecutions.filter(f => f.flowType === flowFilter);
  }, [flowExecutions, flowFilter]);

  const pieData = useMemo(() => {
    const counts: Record<string, number> = {};
    flowExecutions.forEach(f => { counts[f.status] = (counts[f.status] || 0) + 1; });
    return Object.entries(counts).map(([name, value]) => ({ name, value }));
  }, [flowExecutions]);

  return (
    <div className="module-page page-enter">
      {/* ── Header ── */}
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">AI Agent 監控中心</h2>
          <p className="module-page-desc">即時監控 AI Agent 效能、流程執行與系統健康狀態</p>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <select className="form-input" style={{ width: 100, fontSize: 12, padding: '6px 10px' }}
            value={timeRange} onChange={e => setTimeRange(Number(e.target.value))}>
            <option value={6}>近 6 小時</option>
            <option value={12}>近 12 小時</option>
            <option value={24}>近 24 小時</option>
            <option value={72}>近 3 天</option>
          </select>
        </div>
      </div>

      <AgentWipFactsPanel />

      {/* ── Top Health Bar ── */}
      <div className="glass-card" style={{ padding: '20px 24px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
          <Shield size={15} style={{ color: 'var(--emerald-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)', letterSpacing: '0.02em' }}>系統健康總覽</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-around', flexWrap: 'wrap', gap: 16 }}>
          <HealthGauge score={summary.successRate} label="可用率" size={110} />
          <HealthGauge score={1 - summary.errorRate} label="無錯率" size={110} />
          <div style={{ textAlign: 'center' }}>
            <div className="mono" style={{ fontSize: 28, fontWeight: 700, color: 'var(--cobalt-400)' }}>{summary.avgLatencyMs}</div>
            <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>平均延遲 (ms)</div>
          </div>
          <div style={{ textAlign: 'center' }}>
            <div className="mono" style={{ fontSize: 28, fontWeight: 700, color: 'var(--amber-400)' }}>{(summary.tokenUsage24h / 1000).toFixed(1)}K</div>
            <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>Token 消耗 (24h)</div>
          </div>
          <div style={{ textAlign: 'center' }}>
            <div className="mono" style={{ fontSize: 28, fontWeight: 700, color: 'var(--emerald-400)' }}>{summary.activeAgents}</div>
            <div style={{ fontSize: 11, color: 'var(--slate-400)', marginTop: 4 }}>活躍 Agent</div>
          </div>
        </div>
      </div>

      {/* ── Agent Health Scores Row ── */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 12 }}>
        {summary.healthScores.map(hs => (
          <div key={hs.agentType} className="glass-card" style={{ padding: 14, display: 'flex', alignItems: 'center', gap: 12 }}>
            <div style={{
              width: 8, height: 8, borderRadius: '50%', flexShrink: 0,
              background: statusColors[hs.status], boxShadow: `0 0 8px ${statusColors[hs.status]}80`,
            }} />
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--slate-200)' }}>{hs.agentType} Agent</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
                <div style={{ flex: 1, height: 4, background: 'rgba(148,163,184,0.15)', borderRadius: 2 }}>
                  <div style={{ height: '100%', width: `${hs.score * 100}%`, background: statusColors[hs.status], borderRadius: 2, transition: 'width 0.5s' }} />
                </div>
                <span className="mono" style={{ fontSize: 11, color: statusColors[hs.status], fontWeight: 700 }}>{(hs.score * 100).toFixed(1)}%</span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* ── Charts Row ── */}
      <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 12 }}>
        {/* Latency Chart */}
        <div className="glass-card no-lift" style={{ padding: 16 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
            <div>
              <h3 style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>API 延遲趨勢</h3>
              <div style={{ fontSize: 10, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>P50 / P95 / P99</div>
            </div>
            <div style={{ display: 'flex', gap: 12, fontSize: 10 }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><div style={{ width: 8, height: 2, background: CHART_COLORS.emerald }} /> P50</span>
              <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><div style={{ width: 8, height: 2, background: CHART_COLORS.amber }} /> P95</span>
              <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><div style={{ width: 8, height: 2, background: CHART_COLORS.crimson }} /> P99</span>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={latencyData} margin={{ top: 5, right: 8, bottom: 0, left: -20 }}>
              <defs>
                <linearGradient id="gP50" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor={CHART_COLORS.emerald} stopOpacity={0.2} /><stop offset="100%" stopColor={CHART_COLORS.emerald} stopOpacity={0} /></linearGradient>
                <linearGradient id="gP95" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor={CHART_COLORS.amber} stopOpacity={0.2} /><stop offset="100%" stopColor={CHART_COLORS.amber} stopOpacity={0} /></linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.08)" />
              <XAxis dataKey="timestamp" tick={{ fill: 'var(--slate-500)', fontSize: 10 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
              <YAxis tick={{ fill: 'var(--slate-500)', fontSize: 10 }} axisLine={false} tickLine={false} unit="ms" />
              <Tooltip content={<CustomTooltip />} />
              <Area type="monotone" dataKey="p50" name="P50" stroke={CHART_COLORS.emerald} fill="url(#gP50)" strokeWidth={1.5} dot={false} />
              <Area type="monotone" dataKey="p95" name="P95" stroke={CHART_COLORS.amber} fill="url(#gP95)" strokeWidth={1.5} dot={false} />
              <Line type="monotone" dataKey="p99" name="P99" stroke={CHART_COLORS.crimson} strokeWidth={1} dot={false} strokeDasharray="4 4" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* QPS Chart */}
        <div className="glass-card no-lift" style={{ padding: 16 }}>
          <div style={{ marginBottom: 12 }}>
            <h3 style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>ERP QPS</h3>
            <div style={{ fontSize: 10, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>每秒 API 呼叫量</div>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={qpsData} margin={{ top: 5, right: 8, bottom: 0, left: -20 }}>
              <defs>
                <linearGradient id="gQps" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor={CHART_COLORS.cobalt} stopOpacity={0.3} /><stop offset="100%" stopColor={CHART_COLORS.cobalt} stopOpacity={0} /></linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.08)" />
              <XAxis dataKey="timestamp" tick={{ fill: 'var(--slate-500)', fontSize: 10 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
              <YAxis tick={{ fill: 'var(--slate-500)', fontSize: 10 }} axisLine={false} tickLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Area type="monotone" dataKey="qps" name="QPS" stroke={CHART_COLORS.cobalt} fill="url(#gQps)" strokeWidth={1.5} dot={false} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* ── Error Rate Chart ── */}
      <div className="glass-card no-lift" style={{ padding: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
          <div>
            <h3 style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>錯誤率趨勢</h3>
            <div style={{ fontSize: 10, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>目標: &lt;1%</div>
          </div>
          <div style={{
            display: 'flex', alignItems: 'center', gap: 4, fontSize: 11, fontWeight: 700, padding: '4px 10px', borderRadius: 4,
            background: summary.errorRate < 0.01 ? 'rgba(16,185,129,0.15)' : 'rgba(199,31,61,0.15)',
            color: summary.errorRate < 0.01 ? 'var(--emerald-400)' : 'var(--crimson-400)',
          }}>
            {summary.errorRate < 0.01 ? <CheckCircle size={12} /> : <AlertTriangle size={12} />}
            當前: {(summary.errorRate * 100).toFixed(2)}%
          </div>
        </div>
        <ResponsiveContainer width="100%" height={150}>
          <BarChart data={errorData} margin={{ top: 5, right: 8, bottom: 0, left: -20 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.08)" />
            <XAxis dataKey="timestamp" tick={{ fill: 'var(--slate-500)', fontSize: 10 }} axisLine={false} tickLine={false} interval="preserveStartEnd" />
            <YAxis tick={{ fill: 'var(--slate-500)', fontSize: 10 }} axisLine={false} tickLine={false} />
            <Tooltip content={<CustomTooltip />} />
            <Bar dataKey="count" name="錯誤數" fill={CHART_COLORS.crimson} radius={[2, 2, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* ── Agent Flow Executions ── */}
      <div style={{ display: 'grid', gridTemplateColumns: '3fr 1fr', gap: 12 }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <GitBranch size={14} style={{ color: 'var(--cobalt-400)' }} />
              <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>Agent Flow 執行紀錄</span>
            </div>
            <div style={{ display: 'flex', gap: 6 }}>
              {['all', 'anomaly_dispatch', 'customer_inquiry', 'inventory_replenishment'].map(ft => (
                <button key={ft} onClick={() => setFlowFilter(ft)} style={{
                  fontSize: 10, padding: '4px 10px', borderRadius: 4, border: 'none', cursor: 'pointer',
                  background: flowFilter === ft ? 'var(--cobalt-400)' : 'rgba(148,163,184,0.1)',
                  color: flowFilter === ft ? '#fff' : 'var(--slate-400)', fontWeight: 600,
                  transition: 'all 0.2s',
                }}>
                  {ft === 'all' ? '全部' : flowTypeLabels[ft]}
                </button>
              ))}
            </div>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {filteredFlows.map(flow => <FlowCard key={flow.id} flow={flow} />)}
            {filteredFlows.length === 0 && (
              <div className="glass-card" style={{ textAlign: 'center', padding: 24, color: 'var(--slate-500)', fontSize: 12 }}>無執行紀錄</div>
            )}
          </div>
        </div>

        {/* Flow Status Pie */}
        <div className="glass-card no-lift" style={{ padding: 16, height: 'fit-content' }}>
          <h3 style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)', marginBottom: 12 }}>執行狀態分佈</h3>
          <ResponsiveContainer width="100%" height={180}>
            <PieChart>
              <Pie data={pieData} cx="50%" cy="50%" innerRadius={45} outerRadius={70} paddingAngle={3} dataKey="value">
                {pieData.map((entry) => (
                  <Cell key={entry.name} fill={statusColors[entry.name] || CHART_COLORS.slate} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 8 }}>
            {pieData.map(d => (
              <div key={d.name} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11 }}>
                <div style={{ width: 8, height: 8, borderRadius: 2, background: statusColors[d.name] }} />
                <span style={{ color: 'var(--slate-400)' }}>{statusLabels[d.name]}</span>
                <span className="mono" style={{ color: 'var(--slate-200)', marginLeft: 'auto' }}>{d.value}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── System Topology ── */}
      <div className="glass-card no-lift" style={{ padding: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
          <Server size={14} style={{ color: 'var(--cobalt-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>系統拓撲圖</span>
          <span style={{ fontSize: 10, color: 'var(--slate-500)', marginLeft: 'auto' }}>Agent ↔ ERP ↔ MES 連線狀態</span>
        </div>
        <TopologyView nodes={topology as typeof mockTopology} />
      </div>

      {/* ── AI Planning Test Panel ── */}
      <PlanningTestPanel />
    </div>
  );
}

// ── Planning Test Panel ──
interface PlanTaskResult {
  id: string;
  description: string;
  toolsNeeded?: string[];
}

interface PlanResult {
  success?: boolean;
  planId?: string;
  strategy?: string;
  confidence?: number;
  tasks?: PlanTaskResult[];
  errorMessage?: string;
  error?: string;
}

interface ExecutionResult {
  success?: boolean;
  executionId?: string;
  duration?: number;
  results?: Array<{ status: string; taskId: string; output?: string }>;
  errorMessage?: string;
  error?: string;
}

interface ToolExecutionResult {
  success?: boolean;
  elapsedMs?: number;
  data?: unknown;
  error?: string;
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

function PlanningTestPanel() {
  const [taskDescription, setTaskDescription] = useState('建立 100 個 A 產品的工單');
  const [activeTab, setActiveTab] = useState<'plan' | 'execute' | 'tools'>('plan');
  const [planResult, setPlanResult] = useState<PlanResult | null>(null);
  const [executeResult, setExecuteResult] = useState<ExecutionResult | null>(null);
  const [toolResult, setToolResult] = useState<ToolExecutionResult | null>(null);
  const [selectedTool, setSelectedTool] = useState('erp_inventory_search');
  const [toolParams, setToolParams] = useState('{}');

  const createPlan = usePlanningCreatePlan();
  const executePlan = usePlanningExecutePlan();
  const executeTool = usePlanningToolExecute();

  const handleCreatePlan = async () => {
    try {
      const result = await createPlan.mutateAsync({
        taskDescription,
        context: 'test',
      });
      setPlanResult(result?.data as PlanResult | undefined ?? null);
    } catch (err: unknown) {
      setPlanResult({ error: getErrorMessage(err) });
    }
  };

  const handleExecutePlan = async () => {
    if (!planResult?.planId) return;
    try {
      const result = await executePlan.mutateAsync({
        planId: planResult.planId,
        parallelExecution: false,
      });
      setExecuteResult(result?.data as ExecutionResult | undefined ?? null);
    } catch (err: unknown) {
      setExecuteResult({ error: getErrorMessage(err) });
    }
  };

  const handleToolExecute = async () => {
    try {
      let params: Record<string, unknown>;
      try {
        const parsed: unknown = JSON.parse(toolParams);
        params = typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)
          ? parsed as Record<string, unknown>
          : {};
      } catch {
        params = {};
      }
      const result = await executeTool.mutateAsync({
        toolId: selectedTool,
        parameters: params,
      });
      setToolResult(result?.data as ToolExecutionResult | undefined ?? null);
    } catch (err: unknown) {
      setToolResult({ error: getErrorMessage(err) });
    }
  };

  const tools = [
    { id: 'erp_inventory_search', name: '庫存搜尋' },
    { id: 'erp_low_stock_check', name: '低庫存檢查' },
    { id: 'erp_po_search', name: '採購訂單搜尋' },
    { id: 'erp_so_search', name: '銷售訂單搜尋' },
    { id: 'erp_sku_search', name: '料號搜尋' },
    { id: 'erp_bom_search', name: 'BOM搜尋' },
    { id: 'erp_workorder_search', name: '工單搜尋' },
    { id: 'mrp_run', name: 'MRP執行' },
    { id: 'calculator', name: '計算機' },
    { id: 'erp_wo_create', name: '建立工單' },
    { id: 'erp_pr_create', name: '建立採購申請' },
    { id: 'report_inventory', name: '庫存報告' },
  ];

  return (
    <div className="glass-card" style={{ padding: 20, marginTop: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
        <BookOpen size={16} style={{ color: 'var(--cobalt-400)' }} />
        <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--slate-200)' }}>AI 規劃測試區</span>
        <span style={{ fontSize: 10, color: 'var(--slate-500)', marginLeft: 'auto' }}>測試 Planner → Executor → Reviewer 工作流程</span>
      </div>

      <div style={{ display: 'flex', gap: 4, marginBottom: 16 }}>
        {(['plan', 'execute', 'tools'] as const).map(tab => (
          <button key={tab} onClick={() => setActiveTab(tab)} style={{
            padding: '6px 14px', borderRadius: 4, border: 'none', cursor: 'pointer',
            background: activeTab === tab ? 'var(--cobalt-400)' : 'rgba(148,163,184,0.15)',
            color: activeTab === tab ? '#fff' : 'var(--slate-400)', fontSize: 12, fontWeight: 600,
            transition: 'all 0.2s',
          }}>
            {tab === 'plan' ? '📋 規劃' : tab === 'execute' ? '▶️ 執行' : '🔧 工具'}
          </button>
        ))}
      </div>

      {activeTab === 'plan' && (
        <div>
          <div style={{ marginBottom: 12 }}>
            <label style={{ fontSize: 11, color: 'var(--slate-400)', display: 'block', marginBottom: 6 }}>任務描述</label>
            <textarea
              value={taskDescription}
              onChange={e => setTaskDescription(e.target.value)}
              className="form-input"
              style={{ width: '100%', height: 60, fontSize: 13, resize: 'vertical' }}
              placeholder="輸入任務描述，例如：建立 100 個 A 產品的工單"
            />
          </div>
          <button
            onClick={handleCreatePlan}
            disabled={createPlan.isPending}
            style={{
              padding: '8px 20px', borderRadius: 4, border: 'none', cursor: createPlan.isPending ? 'not-allowed' : 'pointer',
              background: createPlan.isPending ? 'var(--slate-600)' : 'var(--emerald-500)', color: '#fff', fontSize: 12, fontWeight: 600,
              display: 'flex', alignItems: 'center', gap: 6,
            }}
          >
            {createPlan.isPending ? <RefreshCw size={14} className="spin" /> : <Play size={14} />}
            建立規劃
          </button>

          {planResult && (
            <div style={{ marginTop: 16, padding: 12, background: 'rgba(0,0,0,0.2)', borderRadius: 6, fontSize: 11 }}>
              <div style={{ color: planResult.success ? 'var(--emerald-400)' : 'var(--crimson-400)', fontWeight: 600, marginBottom: 8 }}>
                規劃結果: {planResult.success ? '成功' : '失敗'}
              </div>
              {planResult.success ? (
                <>
                  <div style={{ color: 'var(--slate-300)', marginBottom: 4 }}>Plan ID: {planResult.planId}</div>
                  <div style={{ color: 'var(--slate-400)', marginBottom: 4 }}>Strategy: {planResult.strategy}</div>
                  <div style={{ color: 'var(--cobalt-400)', marginBottom: 8 }}>Confidence: {(planResult.confidence * 100).toFixed(1)}%</div>
                  <div style={{ color: 'var(--slate-500)', marginBottom: 6 }}>Tasks ({planResult.tasks?.length || 0}):</div>
                  {planResult.tasks?.map((task, i) => (
                    <div key={i} style={{ padding: '4px 8px', background: 'rgba(45,110,245,0.1)', borderRadius: 3, marginBottom: 4, fontSize: 10 }}>
                      <span style={{ color: 'var(--slate-200)' }}>{task.id}</span>: {task.description}
                      <span style={{ color: 'var(--slate-500)', marginLeft: 8 }}>[{task.toolsNeeded?.join(', ')}]</span>
                    </div>
                  ))}
                  {planResult.planId && (
                    <button onClick={handleExecutePlan} disabled={executePlan.isPending} style={{
                      marginTop: 12, padding: '6px 14px', borderRadius: 4, border: 'none', cursor: executePlan.isPending ? 'not-allowed' : 'pointer',
                      background: 'var(--cobalt-400)', color: '#fff', fontSize: 11, fontWeight: 600,
                    }}>
                      {executePlan.isPending ? '執行中...' : '▶ 執行規劃'}
                    </button>
                  )}
                </>
              ) : (
                <div style={{ color: 'var(--crimson-400)' }}>{planResult.errorMessage || planResult.error}</div>
              )}
            </div>
          )}

          {executeResult && (
            <div style={{ marginTop: 12, padding: 12, background: 'rgba(0,0,0,0.2)', borderRadius: 6, fontSize: 11 }}>
              <div style={{ color: executeResult.success ? 'var(--emerald-400)' : 'var(--crimson-400)', fontWeight: 600, marginBottom: 8 }}>
                執行結果: {executeResult.success ? '成功' : '失敗'}
              </div>
              {executeResult.success ? (
                <>
                  <div style={{ color: 'var(--slate-300)', marginBottom: 4 }}>Execution ID: {executeResult.executionId}</div>
                  <div style={{ color: 'var(--slate-400)', marginBottom: 8 }}>Duration: {executeResult.duration?.toFixed(2)}s</div>
                  <div style={{ color: 'var(--slate-500)', marginBottom: 6 }}>Results ({executeResult.results?.length || 0}):</div>
                  {executeResult.results?.map((r, i) => (
                    <div key={i} style={{ padding: '4px 8px', background: r.status === 'success' ? 'rgba(16,185,129,0.1)' : 'rgba(199,31,61,0.1)', borderRadius: 3, marginBottom: 4, fontSize: 10 }}>
                      <span style={{ color: r.status === 'success' ? 'var(--emerald-400)' : 'var(--crimson-400)' }}>{r.status}</span> - {r.taskId}
                      {r.output && <span style={{ color: 'var(--slate-500)', marginLeft: 6 }}>{r.output}</span>}
                    </div>
                  ))}
                </>
              ) : (
                <div style={{ color: 'var(--crimson-400)' }}>{executeResult.errorMessage || executeResult.error}</div>
              )}
            </div>
          )}
        </div>
      )}

      {activeTab === 'tools' && (
        <div>
          <div style={{ marginBottom: 12 }}>
            <label style={{ fontSize: 11, color: 'var(--slate-400)', display: 'block', marginBottom: 6 }}>選擇工具</label>
            <select
              value={selectedTool}
              onChange={e => setSelectedTool(e.target.value)}
              className="form-input"
              style={{ width: '100%', fontSize: 12 }}
            >
              {tools.map(t => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>
          <div style={{ marginBottom: 12 }}>
            <label style={{ fontSize: 11, color: 'var(--slate-400)', display: 'block', marginBottom: 6 }}>參數 (JSON)</label>
            <textarea
              value={toolParams}
              onChange={e => setToolParams(e.target.value)}
              className="form-input"
              style={{ width: '100%', height: 60, fontSize: 11, fontFamily: 'var(--font-mono)' }}
              placeholder='{"sku_code": "A001"}'
            />
          </div>
          <button
            onClick={handleToolExecute}
            disabled={executeTool.isPending}
            style={{
              padding: '8px 20px', borderRadius: 4, border: 'none', cursor: executeTool.isPending ? 'not-allowed' : 'pointer',
              background: executeTool.isPending ? 'var(--slate-600)' : 'var(--amber-500)', color: '#fff', fontSize: 12, fontWeight: 600,
              display: 'flex', alignItems: 'center', gap: 6,
            }}
          >
            {executeTool.isPending ? <RefreshCw size={14} className="spin" /> : <Settings size={14} />}
            執行工具
          </button>

          {toolResult && (
            <div style={{ marginTop: 16, padding: 12, background: 'rgba(0,0,0,0.2)', borderRadius: 6, fontSize: 11 }}>
              <div style={{ color: toolResult.success ? 'var(--emerald-400)' : 'var(--crimson-400)', fontWeight: 600, marginBottom: 8 }}>
                工具結果: {toolResult.success ? '成功' : '失敗'} ({toolResult.elapsedMs}ms)
              </div>
              {toolResult.success ? (
                <pre style={{ color: 'var(--slate-400)', whiteSpace: 'pre-wrap', fontSize: 10, maxHeight: 200, overflow: 'auto' }}>
                  {typeof toolResult.data === 'string'
                    ? toolResult.data
                    : toolResult.data == null
                      ? '執行成功，無回傳資料'
                      : JSON.stringify(toolResult.data, null, 2)}
                </pre>
              ) : (
                <div style={{ color: 'var(--crimson-400)' }}>{toolResult.error}</div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
