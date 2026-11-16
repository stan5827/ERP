import { useCallback, useEffect, useState } from 'react';
import { hrApi } from '../api/system';
import type { PayrollPolicyVersionDto, SavePayrollPolicyRequest } from '../api/system';
import { useRole } from '../contexts/useRole';

type PayrollPolicyManagerProps = { mode: 'hr' | 'finance' };

const emptyPolicyJson = JSON.stringify({
  overtimeRules: [{ dayType: '', fromHour: null, toHour: null, multiplier: null, multiplierMode: '' }],
  insuranceTables: {
    laborInsurance: [],
    healthInsurance: [],
    employmentInsurance: [],
    occupationalAccidentInsurance: [],
  },
  pensionRules: { employerRate: null, employeeMinRate: null, employeeMaxRate: null },
  taxRules: {
    withholdingTable: [],
    supplementalHealthInsurance: { threshold: null, rate: null },
  },
  leaveRules: { dailyDivisor: null, hourlyDivisor: null, deductions: [] },
}, null, 2);

const statusLabels: Record<PayrollPolicyVersionDto['status'], string> = {
  draft: '草稿',
  pending_finance: '待財務覆核',
  approved: '已覆核（未啟用計薪）',
  rejected: '退回',
};

function getErrorMessage(error: unknown): string {
  if (error !== null && typeof error === 'object' && 'response' in error) {
    const response = (error as { response?: { data?: { message?: string; missingFields?: string[] } } }).response;
    const message = response?.data?.message;
    const missingFields = response?.data?.missingFields;
    if (message && missingFields?.length) return `${message}：${missingFields.join('、')}`;
    if (message) return message;
  }
  return error instanceof Error ? error.message : String(error);
}

export function PayrollPolicyManager({ mode }: PayrollPolicyManagerProps) {
  const { hasPermission } = useRole();
  const canCreate = mode === 'hr' && hasPermission('hr:payroll', 'create');
  const canEdit = mode === 'hr' && hasPermission('hr:payroll', 'edit');
  const canSubmit = mode === 'hr' && hasPermission('hr:payroll', 'approve');
  const canReview = mode === 'finance' && hasPermission('hr:payroll', 'approve');
  const [policies, setPolicies] = useState<PayrollPolicyVersionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [versionCode, setVersionCode] = useState('');
  const [effectiveFrom, setEffectiveFrom] = useState('2026-01-01');
  const [documentReference, setDocumentReference] = useState('');
  const [policyJson, setPolicyJson] = useState(emptyPolicyJson);
  const [reviewComments, setReviewComments] = useState<Record<string, string>>({});
  const [hrAttestations, setHrAttestations] = useState<Record<string, boolean>>({});
  const [financeAttestations, setFinanceAttestations] = useState<Record<string, boolean>>({});

  const loadPolicies = useCallback(async () => {
    if ((mode === 'hr' && !hasPermission('hr:payroll', 'view')) || (mode === 'finance' && !canReview)) {
      setPolicies([]);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const response = mode === 'hr'
        ? await hrApi.payroll.policies.list()
        : await hrApi.payroll.policies.pendingFinanceReview();
      setPolicies(response.data.data ?? []);
    } catch (loadError) {
      setError(`載入薪資政策失敗：${getErrorMessage(loadError)}`);
    } finally {
      setLoading(false);
    }
  }, [canReview, hasPermission, mode]);

  useEffect(() => {
    void loadPolicies();
  }, [loadPolicies]);

  const clearEditor = () => {
    setEditingId(null);
    setVersionCode('');
    setEffectiveFrom('2026-01-01');
    setDocumentReference('');
    setPolicyJson(emptyPolicyJson);
  };

  const editDraft = (policy: PayrollPolicyVersionDto) => {
    setEditingId(policy.id);
    setVersionCode(policy.versionCode);
    setEffectiveFrom(policy.effectiveFrom.slice(0, 10));
    setDocumentReference(policy.approvalDocumentReference);
    setPolicyJson(policy.policyJson);
  };

  const saveDraft = async () => {
    setSaving(true);
    setError('');
    const request: SavePayrollPolicyRequest = {
      versionCode: versionCode.trim(),
      effectiveFrom,
      approvalDocumentReference: documentReference.trim(),
      policyJson,
    };
    try {
      if (editingId) {
        await hrApi.payroll.policies.update(editingId, request);
      } else {
        await hrApi.payroll.policies.create(request);
      }
      clearEditor();
      await loadPolicies();
    } catch (saveError) {
      setError(`儲存草稿失敗：${getErrorMessage(saveError)}`);
    } finally {
      setSaving(false);
    }
  };

  const submitForReview = async (id: string) => {
    setError('');
    try {
      await hrApi.payroll.policies.submit(id, hrAttestations[id] === true);
      setHrAttestations(current => ({ ...current, [id]: false }));
      await loadPolicies();
    } catch (submitError) {
      setError(`送審失敗：${getErrorMessage(submitError)}`);
    }
  };

  const review = async (policy: PayrollPolicyVersionDto, approve: boolean) => {
    const comment = reviewComments[policy.id] ?? '';
    setError('');
    try {
      if (approve) {
        await hrApi.payroll.policies.approve(policy.id, comment, financeAttestations[policy.id] === true);
      } else {
        await hrApi.payroll.policies.reject(policy.id, comment, financeAttestations[policy.id] === true);
      }
      setReviewComments(current => ({ ...current, [policy.id]: '' }));
      setFinanceAttestations(current => ({ ...current, [policy.id]: false }));
      await loadPolicies();
    } catch (reviewError) {
      setError(`覆核失敗：${getErrorMessage(reviewError)}`);
    }
  };

  return (
    <section className="glass-card" style={{ padding: 18, display: 'grid', gap: 16 }}>
      <header>
        <h3 className="section-title" style={{ fontSize: 15, marginBottom: 4 }}>
          {mode === 'hr' ? '薪資政策版本管理' : '薪資政策待覆核'}
        </h3>
        <p style={{ color: 'var(--slate-400)', fontSize: 12, margin: 0 }}>
          HR 與財務覆核者必須分別確認官方政策文件。覆核僅建立核准紀錄，不會啟用計薪、產生薪資、過帳或發布薪資單。
          政策缺少官方級距、費率或核定依據時，請保留為草稿。
        </p>
      </header>

      {error && <div role="alert" style={{ color: 'var(--crimson-300)', fontSize: 13 }}>{error}</div>}

      {mode === 'hr' && canCreate && (
        <div style={{ display: 'grid', gap: 10, border: '1px solid var(--slate-700)', borderRadius: 8, padding: 14 }}>
          <strong style={{ fontSize: 13 }}>{editingId ? '編輯政策草稿' : '建立政策草稿'}</strong>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 10 }}>
            <label style={{ display: 'grid', gap: 4, fontSize: 12, color: 'var(--slate-300)' }}>
              版本代碼
              <input value={versionCode} onChange={event => setVersionCode(event.target.value)} maxLength={50} required
                placeholder="例如 TW-PAYROLL-2026-01" />
            </label>
            <label style={{ display: 'grid', gap: 4, fontSize: 12, color: 'var(--slate-300)' }}>
              生效日期
              <input type="date" value={effectiveFrom} onChange={event => setEffectiveFrom(event.target.value)} required />
            </label>
            <label style={{ display: 'grid', gap: 4, fontSize: 12, color: 'var(--slate-300)' }}>
              HR 核定文件編號／版本
              <input value={documentReference} onChange={event => setDocumentReference(event.target.value)} maxLength={200} required
                placeholder="尚未核定請勿填入假資料" />
            </label>
          </div>
          <label style={{ display: 'grid', gap: 4, fontSize: 12, color: 'var(--slate-300)' }}>
            政策資料（JSON；空區塊代表未完成，無預設費率）
            <textarea value={policyJson} onChange={event => setPolicyJson(event.target.value)} rows={10} spellCheck={false}
              style={{ width: '100%', fontFamily: 'var(--font-mono)', fontSize: 12, resize: 'vertical' }} />
          </label>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-primary btn-sm" disabled={saving || !versionCode.trim() || !documentReference.trim()}
              onClick={() => void saveDraft()}>{saving ? '儲存中...' : '儲存草稿'}</button>
            {editingId && <button className="btn btn-ghost btn-sm" onClick={clearEditor}>取消編輯</button>}
          </div>
        </div>
      )}

      {loading ? <div style={{ color: 'var(--slate-400)', fontSize: 13 }}>載入中...</div> : policies.length === 0 ? (
        <div style={{ color: 'var(--slate-400)', fontSize: 13 }}>
          {mode === 'finance' ? '目前沒有待財務覆核的薪資政策。' : '尚無薪資政策版本。'}
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table className="data-table">
            <thead>
              <tr><th>版本／生效日</th><th>核定文件</th><th>政策內容</th><th>狀態</th><th>HR 送審</th><th>財務覆核</th><th>審核留痕</th><th>操作</th></tr>
            </thead>
            <tbody>
              {policies.map(policy => (
                <tr key={policy.id}>
                  <td>
                    <strong>{policy.versionCode}</strong>
                    <div className="mono" style={{ fontSize: 11, color: 'var(--slate-400)' }}>{policy.effectiveFrom.slice(0, 10)}</div>
                  </td>
                  <td>{policy.approvalDocumentReference}</td>
                  <td>
                    <details>
                      <summary>檢視結構化政策</summary>
                      <pre style={{ maxWidth: 420, maxHeight: 240, overflow: 'auto', fontSize: 10 }}>{policy.policyJson}</pre>
                    </details>
                  </td>
                  <td>{statusLabels[policy.status]}</td>
                  <td>{policy.submittedByUsername ?? '—'}</td>
                  <td>{policy.financeReviewerUsername ?? '—'}</td>
                  <td>
                    <details>
                      <summary>{policy.auditTrail.length} 筆事件</summary>
                      {policy.auditTrail.map((event, index) => (
                        <div key={`${event.action}-${event.occurredAt}-${index}`} style={{ fontSize: 11, marginTop: 4 }}>
                          {event.action} · {event.actorUsername} · {new Date(event.occurredAt).toLocaleString()}
                          {event.details && <div style={{ color: 'var(--slate-400)' }}>{event.details}</div>}
                        </div>
                      ))}
                    </details>
                  </td>
                  <td>
                    {mode === 'hr' && policy.status === 'draft' && canEdit && (
                      <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => editDraft(policy)}>編輯</button>
                        {canSubmit && (
                          <div style={{ display: 'grid', gap: 4 }}>
                            <label style={{ fontSize: 10, color: 'var(--slate-400)' }}>
                              <input type="checkbox" checked={hrAttestations[policy.id] ?? false}
                                onChange={event => setHrAttestations(current => ({ ...current, [policy.id]: event.target.checked }))} />
                              已核對正式核定文件及各政策區塊完整
                            </label>
                            <button className="btn btn-primary btn-sm" disabled={!hrAttestations[policy.id]}
                              onClick={() => void submitForReview(policy.id)}>送財務覆核</button>
                          </div>
                        )}
                      </div>
                    )}
                    {mode === 'finance' && canReview && (
                      <div style={{ display: 'grid', gap: 6, minWidth: 200 }}>
                        <input value={reviewComments[policy.id] ?? ''}
                          onChange={event => setReviewComments(current => ({ ...current, [policy.id]: event.target.value }))}
                          placeholder="覆核意見；退回必填" maxLength={1000} />
                        <label style={{ fontSize: 10, color: 'var(--slate-400)' }}>
                          <input type="checkbox" checked={financeAttestations[policy.id] ?? false}
                            onChange={event => setFinanceAttestations(current => ({ ...current, [policy.id]: event.target.checked }))} />
                          已核對正式核定文件及各政策區塊完整
                        </label>
                        <div style={{ display: 'flex', gap: 6 }}>
                          <button className="btn btn-primary btn-sm"
                            disabled={!financeAttestations[policy.id] || !reviewComments[policy.id]?.trim()}
                            onClick={() => void review(policy, true)}>覆核記錄</button>
                          <button className="btn btn-secondary btn-sm"
                            disabled={!reviewComments[policy.id]?.trim()}
                            onClick={() => void review(policy, false)}>退回</button>
                        </div>
                      </div>
                    )}
                    {!policy.calculationEnabled && <div style={{ fontSize: 10, color: 'var(--amber-400)', marginTop: 4 }}>計薪仍封鎖</div>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
