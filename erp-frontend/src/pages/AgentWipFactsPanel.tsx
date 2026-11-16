import { useState } from 'react';
import type { FormEvent } from 'react';
import type {
  AgentWipDecimalFactDto,
  AgentWipFinalOperationDto,
  AgentWipIntFactDto,
  AgentWipNullableDecimalFactDto,
  AgentWipStringFactDto,
  AgentWipWorkOrderDto,
} from '../api/agentWip';
import { agentWipApi } from '../api/agentWip';
import { toAgentWipFactPresentation } from '../utils/agentWipFacts';

type FactValue =
  | AgentWipStringFactDto
  | AgentWipIntFactDto
  | AgentWipDecimalFactDto
  | AgentWipNullableDecimalFactDto;

function FactCell({ label, fact }: { label: string; fact: FactValue }) {
  const presentation = toAgentWipFactPresentation(fact);

  return (
    <div style={{
      minWidth: 0,
      padding: 12,
      borderRadius: 8,
      background: 'rgba(15, 23, 42, 0.55)',
      border: '1px solid rgba(148, 163, 184, 0.12)',
    }}>
      <dt style={{ color: 'var(--slate-400)', fontSize: 11, marginBottom: 8 }}>{label}</dt>
      <dd style={{
        margin: 0,
        color: 'var(--slate-100)',
        fontSize: 18,
        fontWeight: 700,
        overflowWrap: 'anywhere',
      }}>
        <span>{presentation.value}</span>
        {presentation.unit && (
          <span style={{ color: 'var(--slate-400)', fontSize: 12, marginLeft: 6 }}>
            {' '}{presentation.unit}
          </span>
        )}
      </dd>
      {presentation.notApplicableReason && (
        <div style={{ color: 'var(--slate-500)', fontSize: 10, marginTop: 4 }}>
          {presentation.notApplicableReason === 'zero_denominator' ? '分母為 0' : '不適用'}
        </div>
      )}
      <details style={{ color: 'var(--slate-500)', fontSize: 10, marginTop: 8 }}>
        <summary style={{ cursor: 'pointer' }}>事實來源</summary>
        <div style={{ overflowWrap: 'anywhere', marginTop: 4 }}>
          <div>FactId: {presentation.factId}</div>
          <div>來源: {presentation.source}</div>
          {presentation.asOf && <div>資料時間: {new Date(presentation.asOf).toLocaleString()}</div>}
        </div>
      </details>
    </div>
  );
}

function FactGrid({ facts }: { facts: Array<{ label: string; fact: FactValue }> }) {
  return (
    <dl style={{
      display: 'grid',
      gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
      gap: 10,
      margin: 0,
    }}>
      {facts.map(({ label, fact }) => <FactCell key={fact.factId} label={label} fact={fact} />)}
    </dl>
  );
}

function finalOperationFacts(operation: AgentWipFinalOperationDto) {
  return [
    { label: '工序序號', fact: operation.operationSequence },
    { label: '工序狀態', fact: operation.status },
    { label: '計畫量', fact: operation.plannedQuantity },
    { label: '良品完成量', fact: operation.goodQuantity },
    { label: '不良量', fact: operation.defectQuantity },
    { label: '不良率', fact: operation.defectRatePercent },
    { label: '剩餘量', fact: operation.remainingQuantity },
  ];
}

export function AgentWipFactsPanel() {
  const [workOrderNumber, setWorkOrderNumber] = useState('');
  const [facts, setFacts] = useState<AgentWipWorkOrderDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const requestedWorkOrder = workOrderNumber.trim();
    if (!requestedWorkOrder || isLoading) return;

    setFacts(null);
    setError(null);
    setIsLoading(true);
    try {
      const response = await agentWipApi.getWorkOrder(requestedWorkOrder, crypto.randomUUID());
      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.message || '後端未回傳可呈現的 WIP 事實。');
      }
      setFacts(response.data.data);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'WIP 查詢失敗，請稍後再試。');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <section className="glass-card" aria-labelledby="agent-wip-facts-title" style={{ padding: 20 }}>
      <div style={{ marginBottom: 14 }}>
        <h3 id="agent-wip-facts-title" style={{
          color: 'var(--slate-100)',
          fontSize: 14,
          fontWeight: 700,
          margin: 0,
        }}>
          WIP 結構化事實
        </h3>
        <p style={{ color: 'var(--slate-500)', fontSize: 11, margin: '6px 0 0' }}>
          數值與格式由後端提供；前端不執行換算或四捨五入。
        </p>
      </div>

      <form onSubmit={handleSubmit} style={{ display: 'flex', gap: 8, marginBottom: 14 }}>
        <label htmlFor="agent-wip-work-order" style={{ color: 'var(--slate-400)', fontSize: 12, alignSelf: 'center' }}>
          工單編號
        </label>
        <input
          id="agent-wip-work-order"
          className="form-input"
          value={workOrderNumber}
          disabled={isLoading}
          onChange={event => {
            setWorkOrderNumber(event.target.value);
            setFacts(null);
            setError(null);
          }}
          maxLength={30}
          autoComplete="off"
          style={{ minWidth: 0, flex: 1 }}
        />
        <button className="btn-cy" type="submit" disabled={!workOrderNumber.trim() || isLoading}>
          {isLoading ? '查詢中…' : '查詢'}
        </button>
      </form>

      {error ? (
        <p role="alert" style={{ color: 'var(--rose-300)', fontSize: 12, margin: 0 }}>
          {error}
        </p>
      ) : isLoading ? (
        <p role="status" style={{ color: 'var(--slate-400)', fontSize: 12, margin: 0 }}>
          正在向後端重新驗證權限並查詢本回合 WIP 事實…
        </p>
      ) : !facts ? (
        <p role="status" style={{ color: 'var(--slate-400)', fontSize: 12, margin: 0 }}>
          輸入工單編號後才會向後端發出唯讀查詢；權限、資料範圍與功能開關由後端驗證。
        </p>
      ) : (
        <div style={{ display: 'grid', gap: 14 }}>
          <div>
            <FactGrid facts={[
              { label: '工單編號', fact: facts.workOrderNumber },
              { label: '料號', fact: facts.productNumber },
              { label: '品名', fact: facts.productName },
              { label: '生產單位', fact: facts.productionUnit },
              { label: '工單狀態', fact: facts.status },
            ]} />
          </div>

          {facts.finalOperation && (
            <section aria-label="最終工序 WIP 數量">
              <h4 style={{ color: 'var(--slate-300)', fontSize: 12, margin: '0 0 8px' }}>最終工序</h4>
              <FactGrid facts={finalOperationFacts(facts.finalOperation)} />
            </section>
          )}

          <section aria-label="已授權工序">
            <h4 style={{ color: 'var(--slate-300)', fontSize: 12, margin: '0 0 8px' }}>已授權工序</h4>
            <FactGrid facts={facts.authorizedOperations.flatMap(operation => [
              { label: `工序 ${operation.operationSequence.displayValue}`, fact: operation.operationSequence },
              { label: `工序 ${operation.operationSequence.displayValue} 狀態`, fact: operation.status },
            ])} />
          </section>
        </div>
      )}
    </section>
  );
}
