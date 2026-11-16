import { ArrowUpRight, Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { RfqSummary } from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

const dateFormat = new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' });

export function RfqsPage() {
  const [rfqs, setRfqs] = useState<RfqSummary[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'all' | 'pending' | 'quoted'>('all');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setRfqs(await portalApi.getRfqs());
    } catch (loadError) {
      setError(loadError instanceof PortalApiError ? loadError.message : '無法載入詢價清單。');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const filtered = useMemo(() => rfqs.filter(rfq => {
    const matchesSearch = rfq.rfqNumber.toLowerCase().includes(search.trim().toLowerCase());
    const isQuoted = rfq.quotedItemCount >= rfq.itemCount && rfq.itemCount > 0;
    return matchesSearch && (status === 'all' || (status === 'quoted' ? isQuoted : !isQuoted));
  }), [rfqs, search, status]);

  return (
    <>
      <div className="page-heading"><div><span className="eyebrow">RFQ COLLABORATION</span><h1>詢價與報價</h1><p>查看採購團隊指派給您的詢價並提交報價。</p></div></div>
      <section className="panel">
        <div className="toolbar">
          <div className="filter-tabs" role="group" aria-label="詢價狀態">
            <button className={status === 'all' ? 'filter-tab is-selected' : 'filter-tab'} onClick={() => setStatus('all')}>全部 <span>{rfqs.length}</span></button>
            <button className={status === 'pending' ? 'filter-tab is-selected' : 'filter-tab'} onClick={() => setStatus('pending')}>待報價</button>
            <button className={status === 'quoted' ? 'filter-tab is-selected' : 'filter-tab'} onClick={() => setStatus('quoted')}>已報價</button>
          </div>
          <label className="search-field"><Search size={17} /><span className="sr-only">搜尋詢價編號</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="搜尋詢價編號" /></label>
        </div>
        {error && <ErrorState message={error} onRetry={() => void load()} />}
        {loading ? <div className="table-loading">正在載入詢價資料…</div>
          : filtered.length === 0 ? <EmptyState title="沒有符合條件的詢價" detail="請調整搜尋條件，或稍後再查看新的指派。" />
            : <div className="table-wrap"><table className="data-table"><thead><tr><th>詢價單號</th><th>發出日期</th><th>報價截止</th><th>品項進度</th><th>狀態</th><th><span className="sr-only">操作</span></th></tr></thead><tbody>{filtered.map(rfq => <tr key={rfq.rfqId}><td><strong>{rfq.rfqNumber}</strong></td><td>{dateFormat.format(new Date(rfq.rfqDate))}</td><td>{rfq.deadline ? dateFormat.format(new Date(rfq.deadline)) : '未指定'}</td><td><span className="progress-label">{rfq.quotedItemCount} / {rfq.itemCount} 項</span><span className="progress-track"><span style={{ width: `${rfq.itemCount ? Math.min(100, rfq.quotedItemCount / rfq.itemCount * 100) : 0}%` }} /></span></td><td><span className={`status-pill ${rfq.status.toLowerCase() === 'open' ? 'status-open' : 'status-muted'}`}>{rfq.status.toLowerCase() === 'open' ? '開放中' : rfq.status}</span></td><td><Link className="table-action" to={`/rfqs/${rfq.rfqId}`} aria-label={`查看 ${rfq.rfqNumber}`}><ArrowUpRight size={17} /></Link></td></tr>)}</tbody></table></div>}
      </section>
    </>
  );
}
