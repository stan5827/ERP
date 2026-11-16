import { ArrowUpRight, Clock3, PackageCheck, ReceiptText, ShieldCheck } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useOutletContext } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { PurchaseOrderSummary, RfqSummary, SupplierPortalSession } from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

function formatDate(value: string | null) {
  return value
    ? new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(new Date(value))
    : '未指定';
}

export function DashboardPage() {
  const { session } = useOutletContext<{ session: SupplierPortalSession }>();
  const [rfqs, setRfqs] = useState<RfqSummary[]>([]);
  const [orders, setOrders] = useState<PurchaseOrderSummary[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function loadOverview() {
    setError(null);
    try {
      const [rfqList, orderList] = await Promise.all([
        portalApi.getRfqs(),
        portalApi.getPurchaseOrders(),
      ]);
      setRfqs(rfqList);
      setOrders(orderList);
    } catch (loadError) {
      setError(loadError instanceof PortalApiError ? loadError.message : '無法載入入口摘要，請稍後再試。');
    }
  }

  useEffect(() => {
    void loadOverview();
  }, []);

  const openRfqs = rfqs.filter(item =>
    item.status.toLowerCase() === 'open' && item.quotedItemCount < item.itemCount);
  const pendingOrders = orders.filter(item =>
    !item.latestResponseType
    || item.latestResponseVersion === null
    || item.latestResponseVersion < item.version
    || item.latestResponseType === 'change_requested');
  const upcoming = [...openRfqs]
    .filter(item => item.deadline)
    .sort((left, right) => new Date(left.deadline!).getTime() - new Date(right.deadline!).getTime())
    .slice(0, 4);

  return (
    <>
      <div className="page-heading">
        <div><span className="eyebrow">SUPPLIER WORKSPACE</span><h1>早安，{session.contactName || session.supplierName}</h1><p>以下是您目前需要關注的協作事項。</p></div>
        <div className="date-chip"><Clock3 size={16} />{new Intl.DateTimeFormat('zh-TW', { dateStyle: 'full' }).format(new Date())}</div>
      </div>
      {error && <ErrorState message={error} onRetry={() => void loadOverview()} />}
      <section className="stat-grid" aria-label="工作摘要">
        <article className="stat-card stat-blue"><span className="stat-icon"><ReceiptText size={20} /></span><span className="stat-label">待完成報價</span><strong>{openRfqs.length}</strong><span className="stat-caption">已指派且仍開放的詢價</span></article>
        <article className="stat-card stat-green"><span className="stat-icon"><PackageCheck size={20} /></span><span className="stat-label">待確認訂單</span><strong>{pendingOrders.length}</strong><span className="stat-caption">需要確認或處理變更</span></article>
        <article className="stat-card stat-violet"><span className="stat-icon"><ShieldCheck size={20} /></span><span className="stat-label">帳號安全</span><strong className="stat-status">已驗證</strong><span className="stat-caption">使用企業安全登入</span></article>
      </section>
      <section className="dashboard-grid">
        <article className="panel">
          <div className="panel-heading"><div><span className="eyebrow">ACTION REQUIRED</span><h2>近期詢價截止</h2></div><Link className="text-link" to="/rfqs">查看全部 <ArrowUpRight size={16} /></Link></div>
          {upcoming.length === 0
            ? <EmptyState title="目前沒有即將截止的詢價" detail="有新的詢價指派給您時，會顯示在這裡。" />
            : <div className="task-list">{upcoming.map(rfq => <Link className="task-row" to={`/rfqs/${rfq.rfqId}`} key={rfq.rfqId}><span className="task-mark task-mark-blue"><ReceiptText size={17} /></span><span className="task-info"><strong>{rfq.rfqNumber}</strong><small>{rfq.itemCount} 個品項 · 已報 {rfq.quotedItemCount}</small></span><span className="task-deadline"><small>報價截止</small><strong>{formatDate(rfq.deadline)}</strong></span><ArrowUpRight size={16} className="muted-icon" /></Link>)}</div>}
        </article>
        <article className="panel panel-side">
          <div className="panel-heading"><div><span className="eyebrow">PURCHASE ORDERS</span><h2>訂單協作</h2></div></div>
          {orders.length === 0
            ? <EmptyState title="尚無可檢視的訂單" detail="採購單送達後將會出現在這裡。" />
            : <div className="order-preview">{orders.slice(0, 4).map(order => <Link to={`/purchase-orders/${order.purchaseOrderId}`} className="order-preview-row" key={order.purchaseOrderId}><span><strong>{order.poNumber}</strong><small>{formatDate(order.poDate)}</small></span><span className="order-amount">{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(order.totalAmount)}<small>{order.latestResponseType || '待回覆'}</small></span></Link>)}</div>}
          <Link className="button button-secondary full-width" to="/purchase-orders">前往採購訂單 <ArrowUpRight size={16} /></Link>
        </article>
      </section>
    </>
  );
}
