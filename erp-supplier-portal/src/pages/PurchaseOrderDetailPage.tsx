import { ArrowLeft, CheckCircle2, Clock3 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useOutletContext, useParams } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { PurchaseOrderDetail, PurchaseOrderResponseSubmission } from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

type ReplyMode = 'accepted' | 'change_requested' | 'rejected';

export function PurchaseOrderDetailPage() {
  const { purchaseOrderId = '' } = useParams();
  const { session } = useOutletContext<{ session: { role: string } }>();
  const [order, setOrder] = useState<PurchaseOrderDetail | null>(null);
  const [mode, setMode] = useState<ReplyMode | null>(null);
  const [reason, setReason] = useState('');
  const [proposal, setProposal] = useState<Record<string, { quantity: string; date: string }>>({});
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const detail = await portalApi.getPurchaseOrder(purchaseOrderId);
      setOrder(detail);
      setProposal(Object.fromEntries(detail.items.map(item => [item.purchaseOrderItemId, {
        quantity: '',
        date: '',
      }])));
    } catch (loadError) {
      setError(loadError instanceof PortalApiError ? loadError.message : '無法載入採購單明細。');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [purchaseOrderId]);

  async function submitResponse(event: React.FormEvent) {
    event.preventDefault();
    if (!order || !mode)
      return;
    if ((mode === 'rejected' || mode === 'change_requested') && !reason.trim()) {
      setError(mode === 'rejected' ? '請填寫拒絕原因。' : '請填寫變更原因。');
      return;
    }

    const body: PurchaseOrderResponseSubmission = { responseType: mode };
    if (reason.trim())
      body.reason = reason.trim();
    if (mode === 'change_requested') {
      body.items = Object.entries(proposal).flatMap(([purchaseOrderItemId, value]) => {
        const proposedQuantity = value.quantity ? Number(value.quantity) : undefined;
        const proposedDeliveryDate = value.date
          ? new Date(`${value.date}T00:00:00Z`).toISOString()
          : undefined;
        if (proposedQuantity === undefined && proposedDeliveryDate === undefined)
          return [];
        return [{
          purchaseOrderItemId,
          ...(proposedQuantity !== undefined ? { proposedQuantity } : {}),
          ...(proposedDeliveryDate ? { proposedDeliveryDate } : {}),
        }];
      });
      if (!body.items?.length) {
        setError('請至少提出一項品項的替代數量或交期。');
        return;
      }
    }

    if (!window.confirm(mode === 'accepted'
      ? `確認接受採購單 ${order.poNumber}（版本 ${order.version ?? '目前'}）？`
      : mode === 'rejected'
        ? `確認拒絕採購單 ${order.poNumber}？`
        : `確認送出 ${order.poNumber} 的品項變更提案？`))
      return;

    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await portalApi.submitPurchaseOrderResponse(order.purchaseOrderId, body);
      setMode(null);
      setReason('');
      setSuccess(true);
      await load();
    } catch (submitError) {
      setError(submitError instanceof PortalApiError ? submitError.message : '訂單回覆未能送出，請稍後重試。');
    } finally {
      setSaving(false);
    }
  }

  if (loading)
    return <div className="table-loading">正在載入採購單明細…</div>;
  if (error && !order)
    return <><Link className="back-link" to="/purchase-orders"><ArrowLeft size={17} />返回採購單</Link><ErrorState message={error} onRetry={() => void load()} /></>;
  if (!order)
    return <EmptyState title="找不到採購單" detail="資料不存在或不在目前帳號的授權範圍內。" />;

  const latestResponse = order.responseHistory[0];
  const alreadyRespondedToVersion = latestResponse
    && latestResponse.purchaseOrderVersion >= order.version
    && (latestResponse.responseType === 'accepted' || latestResponse.responseType === 'rejected');
  const canReply = order.status.toLowerCase() === 'confirmed'
    && !alreadyRespondedToVersion
    && session.role.toLowerCase() === 'sales';
  const dateFormat = new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' });

  return (
    <>
      <Link className="back-link" to="/purchase-orders"><ArrowLeft size={17} />返回採購單</Link>
      <div className="page-heading detail-heading"><div><span className="eyebrow">PURCHASE ORDER</span><h1>{order.poNumber}</h1><p>訂單日期 {dateFormat.format(new Date(order.poDate))} <span className="heading-divider">·</span> 版本 {order.version}</p></div><span className="status-pill status-open">{order.status}</span></div>
      {success && <div className="notice notice-success"><CheckCircle2 size={18} />回覆已記錄，訂單歷程已更新。</div>}
      {error && <ErrorState message={error} />}
      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">ORDER ITEMS</span><h2>訂單品項</h2></div><span className="count-chip">{order.items.length} 個品項</span></div>
        <div className="table-wrap"><table className="data-table"><thead><tr><th>品號／品名</th><th>訂購數量</th><th>單價</th><th>小計</th></tr></thead><tbody>{order.items.map(item => <tr key={item.purchaseOrderItemId}><td><strong>{item.skuName || '未命名品項'}</strong><small className="table-subtext">{item.skuNumber || item.skuId}</small></td><td>{item.quantity.toLocaleString('zh-TW')} {item.unit || ''}</td><td>{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(item.unitPrice)}</td><td>{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(item.subtotal)}</td></tr>)}</tbody></table></div>
        <div className="totals-row"><span>未稅金額 <strong>{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(order.subtotal)}</strong></span><span>稅額 <strong>{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(order.taxAmount)}</strong></span><span className="total-emphasis">訂單總額 <strong>{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(order.totalAmount)}</strong></span></div>
      </section>
      {(order.paymentTerms || order.tradeTerms || order.expectedDeliveryDate || order.notes) && <section className="panel order-terms"><div className="panel-heading"><div><span className="eyebrow">DELIVERY & TERMS</span><h2>交貨與條件</h2></div></div><div className="terms-grid">{order.expectedDeliveryDate && <div><small>預計交期</small><strong>{dateFormat.format(new Date(order.expectedDeliveryDate))}</strong></div>}{order.paymentTerms && <div><small>付款條件</small><strong>{order.paymentTerms}</strong></div>}{order.tradeTerms && <div><small>貿易條件</small><strong>{order.tradeTerms}</strong></div>}{order.notes && <div className="term-note"><small>備註</small><strong>{order.notes}</strong></div>}</div></section>}
      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">RESPONSE HISTORY</span><h2>供應商回覆歷程</h2></div></div>
        {order.responseHistory.length === 0 ? <EmptyState title="尚無回覆紀錄" detail="您可以在下方確認接單或提出變更。" /> : <div className="history-list">{order.responseHistory.map(item => <article className="history-entry" key={item.responseId}><span className="history-icon"><Clock3 size={16} /></span><div><strong>{item.responseType === 'accepted' ? '已確認接單' : item.responseType === 'rejected' ? '已拒絕' : '已提出變更'}</strong><small>{dateFormat.format(new Date(item.submittedAt))} · 訂單版本 {item.purchaseOrderVersion}</small>{item.reason && <p>{item.reason}</p>}{item.items.map(change => <p key={change.purchaseOrderItemId}>品項變更：{change.proposedQuantity !== null ? `數量 ${change.proposedQuantity}` : ''}{change.proposedDeliveryDate ? ` · 交期 ${dateFormat.format(new Date(change.proposedDeliveryDate))}` : ''}</p>)}</div></article>)}</div>}
      </section>
      {order.status.toLowerCase() === 'confirmed' && !alreadyRespondedToVersion
        && session.role.toLowerCase() !== 'sales'
        && <div className="notice notice-error">此帳號目前沒有回覆採購單的權限，請聯絡供應商帳號管理人員。</div>}
      {canReply && <section className="panel response-panel">
        <div className="panel-heading"><div><span className="eyebrow">YOUR RESPONSE</span><h2>回覆採購單</h2><p>您的回覆會記錄在訂單歷程，不會直接修改採購單內容。</p></div></div>
        {!mode ? <div className="response-actions"><button className="button button-primary" onClick={() => setMode('accepted')}>確認接單</button><button className="button button-secondary" onClick={() => setMode('change_requested')}>提出品項變更</button><button className="button button-danger-quiet" onClick={() => setMode('rejected')}>拒絕訂單</button></div> : <form onSubmit={submitResponse} className="response-form">
          <div className="response-mode-title">{mode === 'accepted' ? '確認接受此採購單' : mode === 'rejected' ? '拒絕此採購單' : '提出逐品項變更'}</div>
          {mode === 'change_requested' && <div className="proposal-list">{order.items.map(item => <div className="proposal-row" key={item.purchaseOrderItemId}><div><strong>{item.skuName || item.skuNumber || '採購品項'}</strong><small>原訂購數量 {item.quantity} · {item.skuNumber || item.skuId}</small></div><label className="field"><span>替代數量</span><input type="number" min="0.01" step="0.01" value={proposal[item.purchaseOrderItemId]?.quantity || ''} onChange={event => setProposal(current => ({ ...current, [item.purchaseOrderItemId]: { ...current[item.purchaseOrderItemId], quantity: event.target.value } }))} placeholder="不變更請留空" /></label><label className="field"><span>替代交期</span><input type="date" value={proposal[item.purchaseOrderItemId]?.date || ''} onChange={event => setProposal(current => ({ ...current, [item.purchaseOrderItemId]: { ...current[item.purchaseOrderItemId], date: event.target.value } }))} /></label></div>)}</div>}
          {(mode !== 'accepted') && <label className="field"><span>原因 <em>必填</em></span><textarea rows={3} maxLength={500} required value={reason} onChange={event => setReason(event.target.value)} placeholder="請說明無法配合或希望調整的原因" /></label>}
          <div className="form-actions"><button type="button" className="button button-quiet" onClick={() => { setMode(null); setError(null); }}>取消</button><button type="submit" className={mode === 'rejected' ? 'button button-danger' : 'button button-primary'} disabled={saving}>{saving ? '正在送出…' : '確認送出'}</button></div>
        </form>}
      </section>}
    </>
  );
}
