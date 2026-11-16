import { ArrowLeft, CalendarDays, CheckCircle2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useOutletContext, useParams } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { QuoteLineSubmission, RfqDetail } from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

interface QuoteFormLine {
  unitPrice: string;
  leadTimeDays: string;
  minimumOrderQuantity: string;
  promisedDeliveryDate: string;
  tradeTerms: string;
  notes: string;
}

function initialLine(item: RfqDetail['items'][number]): QuoteFormLine {
  return {
    unitPrice: item.unitPrice?.toString() ?? '',
    leadTimeDays: item.leadTimeDays?.toString() ?? '',
    minimumOrderQuantity: item.minimumOrderQuantity?.toString() ?? '',
    promisedDeliveryDate: item.promisedDeliveryDate?.slice(0, 10) ?? '',
    tradeTerms: item.tradeTerms ?? '',
    notes: item.notes ?? '',
  };
}

export function RfqDetailPage() {
  const { rfqId = '' } = useParams();
  const { session } = useOutletContext<{ session: { role: string } }>();
  const [rfq, setRfq] = useState<RfqDetail | null>(null);
  const [lines, setLines] = useState<Record<string, QuoteFormLine>>({});
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const detail = await portalApi.getRfq(rfqId);
      setRfq(detail);
      setLines(Object.fromEntries(detail.items.map(item => [item.skuId, initialLine(item)])));
    } catch (loadError) {
      setError(loadError instanceof PortalApiError ? loadError.message : '無法載入詢價明細。');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [rfqId]);

  function updateLine(skuId: string, key: keyof QuoteFormLine, value: string) {
    setLines(current => ({ ...current, [skuId]: { ...current[skuId], [key]: value } }));
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (!rfq)
      return;
    const items: QuoteLineSubmission[] = [];
    for (const item of rfq.items) {
      const line = lines[item.skuId];
      if (!line.unitPrice)
        continue;
      const unitPrice = Number(line.unitPrice);
      const leadTimeDays = line.leadTimeDays ? Number(line.leadTimeDays) : 0;
      const minimumOrderQuantity = line.minimumOrderQuantity ? Number(line.minimumOrderQuantity) : 0;
      if (!Number.isFinite(unitPrice) || unitPrice <= 0
        || !Number.isInteger(leadTimeDays) || leadTimeDays < 0
        || !Number.isFinite(minimumOrderQuantity) || minimumOrderQuantity < 0) {
        setError(`請檢查品號 ${item.skuNumber || item.skuId} 的單價、交期與最低訂購量。`);
        return;
      }
      items.push({
        skuId: item.skuId,
        unitPrice,
        leadTimeDays,
        minimumOrderQuantity,
        promisedDeliveryDate: line.promisedDeliveryDate
          ? new Date(`${line.promisedDeliveryDate}T00:00:00Z`).toISOString()
          : null,
        tradeTerms: line.tradeTerms.trim() || null,
        notes: line.notes.trim() || null,
      });
    }
    if (items.length === 0) {
      setError('請至少填寫一項報價單價。');
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await portalApi.submitQuote(rfqId, items);
      setSuccess(true);
      await load();
    } catch (submitError) {
      setError(submitError instanceof PortalApiError ? submitError.message : '報價未能送出，請稍後重試。');
    } finally {
      setSaving(false);
    }
  }

  if (loading)
    return <div className="table-loading">正在載入詢價明細…</div>;
  if (error && !rfq)
    return <><Link className="back-link" to="/rfqs"><ArrowLeft size={17} />返回詢價清單</Link><ErrorState message={error} onRetry={() => void load()} /></>;
  if (!rfq)
    return <EmptyState title="找不到詢價資料" detail="資料不存在或不在目前帳號的授權範圍內。" />;

  const isOpen = rfq.status.toLowerCase() === 'open'
    && (!rfq.deadline || new Date(rfq.deadline).getTime() >= Date.now());
  const canRespond = session.role.toLowerCase() === 'sales';
  const dateFormat = new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' });

  return (
    <>
      <Link className="back-link" to="/rfqs"><ArrowLeft size={17} />返回詢價清單</Link>
      <div className="page-heading detail-heading"><div><span className="eyebrow">RFQ DETAIL</span><h1>{rfq.rfqNumber}</h1><p>發出日期 {dateFormat.format(new Date(rfq.rfqDate))} <span className="heading-divider">·</span> 截止日期 {rfq.deadline ? dateFormat.format(new Date(rfq.deadline)) : '未指定'}</p></div><span className={`status-pill ${isOpen ? 'status-open' : 'status-muted'}`}>{isOpen ? '開放報價' : '已截止或關閉'}</span></div>
      {success && <div className="notice notice-success"><CheckCircle2 size={18} />報價已送出，清單已更新。</div>}
      {error && <ErrorState message={error} />}
      <form onSubmit={submit}>
        <section className="panel">
          <div className="panel-heading"><div><span className="eyebrow">REQUESTED ITEMS</span><h2>詢價品項</h2></div><span className="count-chip">{rfq.items.length} 個品項</span></div>
          {rfq.items.length === 0 ? <EmptyState title="此詢價尚無品項" detail="請聯絡採購窗口確認詢價內容。" /> : (
            <div className="quote-items">
              {rfq.items.map((item, index) => {
                const line = lines[item.skuId] ?? initialLine(item);
                return (
                  <article className="quote-item" key={`${item.skuId}-${index}`}>
                    <div className="quote-item-heading"><span className="item-index">{String(index + 1).padStart(2, '0')}</span><div><h3>{item.skuName || '未命名品項'}</h3><span>{item.skuNumber || item.skuId}</span></div><div className="requested-quantity"><small>需求數量</small><strong>{item.quantity.toLocaleString('zh-TW')} {item.unit || ''}</strong></div></div>
                    {item.specifications && <p className="item-spec">{item.specifications}</p>}
                    <div className="form-grid quote-form-grid">
                      <label className="field"><span>報價單價</span><div className="input-with-prefix"><span>{rfq.currency}</span><input type="number" min="0.0001" step="0.0001" disabled={!isOpen || !canRespond} value={line.unitPrice} onChange={event => updateLine(item.skuId, 'unitPrice', event.target.value)} placeholder="未報價請留空" /></div></label>
                      <label className="field"><span>預計交貨日期</span><div className="input-with-icon"><CalendarDays size={16} /><input type="date" disabled={!isOpen || !canRespond} value={line.promisedDeliveryDate} onChange={event => updateLine(item.skuId, 'promisedDeliveryDate', event.target.value)} /></div></label>
                      <label className="field"><span>交期（天）</span><input type="number" min="0" max="3650" step="1" disabled={!isOpen || !canRespond} value={line.leadTimeDays} onChange={event => updateLine(item.skuId, 'leadTimeDays', event.target.value)} placeholder="例如 14" /></label>
                      <label className="field"><span>最低訂購量（MOQ）</span><input type="number" min="0" step="0.01" disabled={!isOpen || !canRespond} value={line.minimumOrderQuantity} onChange={event => updateLine(item.skuId, 'minimumOrderQuantity', event.target.value)} placeholder="例如 10" /></label>
                      <label className="field"><span>貿易條件</span><input maxLength={50} disabled={!isOpen || !canRespond} value={line.tradeTerms} onChange={event => updateLine(item.skuId, 'tradeTerms', event.target.value)} placeholder="例如 FOB" /></label>
                      <label className="field field-wide"><span>備註</span><textarea rows={2} maxLength={500} disabled={!isOpen || !canRespond} value={line.notes} onChange={event => updateLine(item.skuId, 'notes', event.target.value)} placeholder="補充交貨或規格說明（選填）" /></label>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
          {isOpen && !canRespond && <div className="notice notice-error">此帳號目前沒有提交報價的權限，請聯絡供應商帳號管理人員。</div>}
          {isOpen && canRespond && rfq.items.length > 0 && <div className="form-actions"><span>可逐項報價；至少填寫一項單價。送出後仍可於截止前更新。</span><button type="submit" className="button button-primary" disabled={saving}>{saving ? '正在送出…' : '送出報價'}</button></div>}
        </section>
      </form>
    </>
  );
}
