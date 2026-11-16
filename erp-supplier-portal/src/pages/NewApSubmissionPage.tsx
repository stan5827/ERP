import { AlertCircle, ArrowLeft } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useOutletContext } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { SupplierApEligibleSource, SupplierPortalSession } from '../api/types';
import { ErrorState } from '../components/Feedback';
import {
  ApSubmissionEditor,
  createApSubmissionFormValue,
} from './ApSubmissionEditor';

function message(error: unknown, fallback: string) {
  return error instanceof PortalApiError
    ? error.message
    : error instanceof Error ? error.message : fallback;
}

function newIdempotencyKey() {
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

export function NewApSubmissionPage() {
  const navigate = useNavigate();
  const { session } = useOutletContext<{ session: SupplierPortalSession }>();
  const [sources, setSources] = useState<SupplierApEligibleSource[]>([]);
  const [selectedReceiptId, setSelectedReceiptId] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const idempotencyKey = useRef(newIdempotencyKey());
  const lastPayload = useRef<string | null>(null);

  async function loadSources() {
    setLoading(true);
    setError(null);
    try {
      const available = await portalApi.getEligibleApSources();
      setSources(available);
      setSelectedReceiptId(current =>
        available.some(source => source.goodsReceiptId === current)
          ? current
          : available[0]?.goodsReceiptId ?? '');
    } catch (loadError) {
      setError(message(loadError, '無法載入可請款來源。'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadSources();
  }, []);

  const selectedSource = sources.find(source => source.goodsReceiptId === selectedReceiptId) ?? null;
  const canWrite = session.role.toLowerCase() === 'sales';
  const initialValue = selectedSource
    ? createApSubmissionFormValue(
        selectedSource.purchaseOrderId,
        selectedSource.purchaseOrderNumber,
        selectedSource.goodsReceiptId,
        selectedSource.goodsReceiptNumber,
        selectedSource.currency,
        selectedSource.lines.map(line => ({
          goodsReceiptItemId: line.goodsReceiptItemId,
          skuNumber: line.skuNumber,
          skuName: line.skuName,
          unit: line.unit,
          acceptedQuantity: line.acceptedQuantity,
          availableQuantity: line.availableQuantity,
          unitPrice: line.purchaseOrderUnitPrice,
          invoicedQuantity: 0,
          taxAmount: 0,
        })),
      )
    : null;

  async function createDraft(body: Parameters<typeof portalApi.createApSubmission>[0]) {
    const serialized = JSON.stringify(body);
    if (lastPayload.current !== null && lastPayload.current !== serialized)
      idempotencyKey.current = newIdempotencyKey();
    lastPayload.current = serialized;
    setSaving(true);
    setError(null);
    try {
      const response = await portalApi.createApSubmission(body, idempotencyKey.current);
      navigate(`/ap-submissions/${encodeURIComponent(response.data.submissionId)}`, {
        state: { notice: '請款草稿已建立，請接續上傳發票憑證。' },
      });
    } catch (createError) {
      setError(message(createError, '無法建立請款草稿。'));
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <Link className="back-link" to="/ap-submissions"><ArrowLeft size={17} />返回請款案件</Link>
      <div className="page-heading">
        <div><span className="eyebrow">NEW AP SUBMISSION</span><h1>新增請款</h1><p>選擇符合資格的採購／收貨來源，填寫完整發票資料以建立草稿。</p></div>
      </div>
      {error && <ErrorState message={error} onRetry={() => void loadSources()} />}
      {!canWrite && <div className="notice notice-info"><AlertCircle size={18} />目前帳號僅能檢視，請由供應商業務聯絡人建立請款。</div>}
      {loading ? <div className="table-loading">正在載入可請款來源…</div>
        : sources.length === 0 ? (
          <section className="panel">
            <div className="empty-state"><div className="empty-state-mark">—</div><h2>目前沒有可請款來源</h2><p>只有符合採購、收貨與驗收條件且仍有可請款數量的來源會列在這裡。</p></div>
          </section>
        ) : (
          <>
            <section className="panel">
              <div className="panel-heading"><div><span className="eyebrow">ELIGIBLE SOURCE</span><h2>選擇採購與收貨來源</h2><p>每一請款案件僅能對應一張採購單及一張收貨單。</p></div></div>
              <label className="field ap-source-select">
                採購單／收貨單
                <select value={selectedReceiptId} onChange={event => setSelectedReceiptId(event.target.value)} disabled={saving}>
                  {sources.map(source => (
                    <option key={source.goodsReceiptId} value={source.goodsReceiptId}>
                      {source.purchaseOrderNumber} / {source.goodsReceiptNumber} · {source.currency}
                    </option>
                  ))}
                </select>
              </label>
              {selectedSource && <div className="ap-source-lines">
                {selectedSource.lines.map(line => (
                  <div key={line.goodsReceiptItemId}>
                    <strong>{line.skuNumber || '未提供料號'} · {line.skuName || '未提供品名'}</strong>
                    <span>已驗收 {line.acceptedQuantity.toLocaleString('zh-TW')} {line.unit}　已請款 {line.previouslyInvoicedQuantity.toLocaleString('zh-TW')}　可請款 {line.availableQuantity.toLocaleString('zh-TW')}　採購單價 {line.purchaseOrderUnitPrice.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}</span>
                  </div>
                ))}
              </div>}
            </section>
            {initialValue && (
              <ApSubmissionEditor
                key={selectedReceiptId}
                initialValue={initialValue}
                disabled={!canWrite}
                saving={saving}
                submitLabel="建立請款草稿"
                onSubmit={createDraft}
              />
            )}
          </>
        )}
    </>
  );
}
