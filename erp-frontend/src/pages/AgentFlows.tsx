import React, { useState } from 'react';
import {
  GitBranch, Play, CheckCircle, AlertTriangle, Clock, RefreshCw,
  ChevronDown, Zap, Truck, Package, Loader2
} from 'lucide-react';
import { useAgentFlowExecutions, useTriggerAgentFlow } from '../hooks/useApi';

// ── Config ──
const flowConfigs = [
  {
    type: 'anomaly_dispatch',
    name: '產線異常派工',
    icon: <Zap size={16} />,
    color: 'var(--crimson-400)',
    description: 'AI 偵測產線異常 → 自動建立維修單 → 派工 → LINE 通知',
    steps: ['AI 異常偵測', '建立維修單', '查詢可用維修人員', '派工並回寫 ERP', 'LINE 推播通知'],
    triggerLabel: '觸發來源',
    triggerPlaceholder: '如: IoT Sensor #A3-12',
  },
  {
    type: 'customer_inquiry',
    name: '客服詢價自動化',
    icon: <Truck size={16} />,
    color: 'var(--cobalt-400)',
    description: '客戶詢問 → NLP 解析 → 查庫存 → 生成報價 → 回傳 LINE',
    steps: ['NLP 解析需求', '查詢庫存', '比對供應鏈交期', '生成報價方案', '回傳 LINE'],
    triggerLabel: '客戶訊息',
    triggerPlaceholder: '如: 請問精密馬達 A 型 100 套的報價?',
  },
  {
    type: 'inventory_replenishment',
    name: '庫存預警補貨',
    icon: <Package size={16} />,
    color: 'var(--emerald-400)',
    description: '低庫存偵測 → AI 預測補貨量 → 生成採購申請 → 審批',
    steps: ['掃描低庫存', 'AI 預測補貨量', '比對供應商價格', '生成採購申請', '審批流程'],
    triggerLabel: 'SKU 編號',
    triggerPlaceholder: '如: RAW-001',
  },
];

const statusColors: Record<string, string> = {
  success: 'var(--emerald-400)', failed: 'var(--crimson-400)',
  running: 'var(--cobalt-400)', pending: 'var(--slate-500)', skipped: 'var(--slate-600)',
};

const statusLabels: Record<string, string> = {
  success: '成功', failed: '失敗', running: '執行中', pending: '待處理', skipped: '已跳過',
};

const flowTypeLabels: Record<string, string> = {
  anomaly_dispatch: '產線異常派工',
  customer_inquiry: '客服詢價自動化',
  inventory_replenishment: '庫存預警補貨',
};

// ── Step Badge ──
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

// ── Flow Template Card ──
function FlowTemplateCard({ config, onTrigger }: { config: typeof flowConfigs[0]; onTrigger: (type: string, data: Record<string, string>) => void }) {
  const [expanded, setExpanded] = useState(false);
  const [triggerValue, setTriggerValue] = useState('');
  const [isRunning, setIsRunning] = useState(false);

  const handleTrigger = async () => {
    setIsRunning(true);
    await onTrigger(config.type, { triggerSource: triggerValue || '手動觸發' });
    setIsRunning(false);
    setTriggerValue('');
  };

  return (
    <div className="glass-card" style={{ padding: 20, borderLeft: `3px solid ${config.color}` }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ color: config.color }}>{config.icon}</div>
          <div>
            <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--slate-100)' }}>{config.name}</div>
            <div style={{ fontSize: 11, color: 'var(--slate-500)', marginTop: 2 }}>{config.description}</div>
          </div>
        </div>
        <button onClick={() => setExpanded(!expanded)} style={{
          background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-500)',
          transform: expanded ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s',
        }}>
          <ChevronDown size={16} />
        </button>
      </div>

      {/* Flow Steps Preview */}
      <div style={{ display: 'flex', gap: 4, marginBottom: 12, flexWrap: 'wrap' }}>
        {config.steps.map((step, i) => (
          <React.Fragment key={i}>
            <div style={{
              fontSize: 10, padding: '4px 10px', borderRadius: 4,
              background: 'rgba(148,163,184,0.08)', color: 'var(--slate-400)', fontWeight: 600,
            }}>{step}</div>
            {i < config.steps.length - 1 && <span style={{ color: 'var(--slate-600)', fontSize: 10, alignSelf: 'center' }}>→</span>}
          </React.Fragment>
        ))}
      </div>

      {expanded && (
        <div style={{ borderTop: '1px solid rgba(148,163,184,0.1)', paddingTop: 16, marginTop: 4 }}>
          <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end' }}>
            <div style={{ flex: 1 }}>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                {config.triggerLabel}
              </label>
              <input
                className="form-input"
                style={{ width: '100%', fontSize: 13 }}
                placeholder={config.triggerPlaceholder}
                value={triggerValue}
                onChange={e => setTriggerValue(e.target.value)}
              />
            </div>
            <button
              className="btn btn-primary btn-sm"
              onClick={handleTrigger}
              disabled={isRunning}
              style={{ display: 'flex', alignItems: 'center', gap: 6, whiteSpace: 'nowrap' }}
            >
              {isRunning ? <Loader2 size={13} className="spin" /> : <Play size={13} />}
              {isRunning ? '執行中...' : '立即觸發'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Execution History Card ──
function ExecutionCard({ flow }: { flow: { id: string; flowType: string; flowName: string; status: string; triggerSource: string; startTime: string; endTime?: string; duration?: number; businessImpact?: string; steps: Array<{ id: string; name: string; status: string; duration?: number; error?: string }> } }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="glass-card" style={{ padding: 16, cursor: 'pointer', transition: 'all var(--transition-fast)' }}
      onClick={() => setExpanded(!expanded)}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <StepBadge status={flow.status} />
          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>{flow.flowName}</span>
          <span style={{ fontSize: 10, color: 'var(--slate-500)', padding: '2px 6px', background: 'rgba(148,163,184,0.08)', borderRadius: 4 }}>
            {flowTypeLabels[flow.flowType]}
          </span>
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
                {step.duration && <span className="mono" style={{ fontSize: 10, color: 'var(--slate-500)' }}>{step.duration.toFixed(1)}s</span>}
                {step.error && <span style={{ fontSize: 10, color: 'var(--crimson-400)' }}>{step.error}</span>}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Main Page ──
export function AgentFlows() {
  const [flowFilter, setFlowFilter] = useState<string>('all');
  const { data: flowResp, isLoading } = useAgentFlowExecutions();
  const triggerFlow = useTriggerAgentFlow();

  const flowExecutions = flowResp?.data || [];

  const filteredFlows = flowFilter === 'all'
    ? flowExecutions
    : flowExecutions.filter((f: { flowType: string }) => f.flowType === flowFilter);

  const handleTrigger = async (flowType: string, data: Record<string, string>) => {
    try {
      await triggerFlow.mutateAsync({ flowType, data });
    } catch {
      // Trigger failed
    }
  };

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">Agent Flow 自動化流程</h2>
          <p className="module-page-desc">設計、觸發與監控 AI Agent 自動化工作流程</p>
        </div>
      </div>

      {/* Flow Templates */}
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
          <GitBranch size={14} style={{ color: 'var(--cobalt-400)' }} />
          <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>流程模板</span>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {flowConfigs.map(config => (
            <FlowTemplateCard key={config.type} config={config} onTrigger={handleTrigger} />
          ))}
        </div>
      </div>

      {/* Execution History */}
      <div>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Clock size={14} style={{ color: 'var(--cobalt-400)' }} />
            <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--slate-200)' }}>執行紀錄</span>
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

        {isLoading ? (
          <div className="glass-card" style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>
            <Loader2 size={20} className="spin" style={{ animation: 'spin 1s linear infinite', display: 'inline', marginRight: 8 }} />
            載入中...
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {filteredFlows.map((flow: { id: string; flowType: string; flowName: string; status: string; triggerSource: string; startTime: string; endTime?: string; duration?: number; businessImpact?: string; steps: Array<{ id: string; name: string; status: string; duration?: number; error?: string }> }) => (
              <ExecutionCard key={flow.id} flow={flow} />
            ))}
            {filteredFlows.length === 0 && (
              <div className="glass-card" style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)', fontSize: 12 }}>
                無執行紀錄
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
