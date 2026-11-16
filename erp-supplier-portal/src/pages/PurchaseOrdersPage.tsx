import { ArrowUpRight, Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { PurchaseOrderSummary } from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

export function PurchaseOrdersPage() {
  const [orders, setOrders] = useState<PurchaseOrderSummary[]>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setOrders(await portalApi.getPurchaseOrders());
    } catch (loadError) {
      setError(loadError instanceof PortalApiError ? loadError.message : '無法載入採購單。');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const filteredOrders = useMemo(() => orders.filter(order =>
    order.poNumber.toLowerCase().includes(search.trim().toLowerCase())), [orders, search]);
  const dateFormat = new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' });

  return (
    <>
      <div className="page-heading"><div><span className="eyebrow">PURCHASE ORDERS</span><h1>採購訂單</h1><p>檢視已發出的訂單，確認接單或回覆需要協調的項目。</p></div></div>
      <section className="panel">
        <div className="toolbar"><div className="toolbar-summary"><strong>{orders.length}</strong> 張可檢視訂單</div><label className="search-field"><Search size={17} /><span className="sr-only">搜尋採購單號</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="搜尋採購單號" /></label></div>
        {error && <ErrorState message={error} onRetry={() => void load()} />}
        {loading ? <div className="table-loading">正在載入採購單…</div>
          : filteredOrders.length === 0 ? <EmptyState title="目前沒有符合條件的採購單" detail="採購單送達後會顯示在這裡。" />
            : <div className="table-wrap"><table className="data-table"><thead><tr><th>採購單號</th><th>訂單日期</th><th>預計交期</th><th>品項數</th><th>訂單金額</th><th>最近回覆</th><th><span className="sr-only">操作</span></th></tr></thead><tbody>{filteredOrders.map(order => <tr key={order.purchaseOrderId}><td><strong>{order.poNumber}</strong></td><td>{dateFormat.format(new Date(order.poDate))}</td><td>{order.expectedDeliveryDate ? dateFormat.format(new Date(order.expectedDeliveryDate)) : '未指定'}</td><td>{order.itemCount}</td><td>{new Intl.NumberFormat('zh-TW', { style: 'currency', currency: order.currency }).format(order.totalAmount)}</td><td><span className={`status-pill ${order.latestResponseType ? 'status-muted' : 'status-pending'}`}>{order.latestResponseType || '待回覆'}</span></td><td><Link className="table-action" to={`/purchase-orders/${order.purchaseOrderId}`} aria-label={`查看 ${order.poNumber}`}><ArrowUpRight size={17} /></Link></td></tr>)}</tbody></table></div>}
      </section>
    </>
  );
}
