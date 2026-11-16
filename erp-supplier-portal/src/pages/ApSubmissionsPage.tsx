import { ArrowLeft, ArrowRight, Eye, Plus, RefreshCw, Search } from 'lucide-react';
import type { FormEvent } from 'react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type { SupplierApSubmissionPage, SupplierApSubmissionSummary } from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

const pageSize = 20;

const statusLabels: Record<string, string> = {
  draft: '草稿',
  submitted: '審核中',
  returned: '待補件',
  approved: '已核准',
  rejected: '已拒絕',
  cancelled: '已取消',
};

function statusStyle(status: string) {
  if (status === 'approved')
    return 'status-open';
  if (status === 'returned' || status === 'submitted')
    return 'status-pending';
  return 'status-muted';
}

function amount(value: number, currency: string) {
  return `${currency} ${value.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function shortDate(value: string | null) {
  if (!value)
    return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(date);
}

function matchLabel(result: string) {
  if (result === 'matched')
    return '核對相符';
  if (result === 'variance')
    return '有差異';
  return '待核對';
}

function SubmissionRow({ item }: { item: SupplierApSubmissionSummary }) {
  return (
    <tr>
      <td><strong>{item.submissionNumber}</strong><small className="table-subtext">第 {item.currentRevisionNumber} 版</small></td>
      <td><strong>{item.supplierInvoiceNumber}</strong><small className="table-subtext">{shortDate(item.supplierInvoiceDate)}</small></td>
      <td>{amount(item.totalAmount, item.currency)}</td>
      <td><span className={`status-pill ${statusStyle(item.status)}`}>{statusLabels[item.status] || item.status}</span></td>
      <td>{matchLabel(item.matchResult)}</td>
      <td>{shortDate(item.submittedAt || item.createdAt)}</td>
      <td><Link className="table-action" to={`/ap-submissions/${encodeURIComponent(item.id)}`} aria-label={`檢視 ${item.submissionNumber}`}><Eye size={16} /></Link></td>
    </tr>
  );
}

export function ApSubmissionsPage() {
  const [result, setResult] = useState<SupplierApSubmissionPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [statusInput, setStatusInput] = useState('');
  const [fromInput, setFromInput] = useState('');
  const [toInput, setToInput] = useState('');
  const [numberInput, setNumberInput] = useState('');
  const [filters, setFilters] = useState({ status: '', from: '', to: '', number: '' });

  async function load(targetPage = page) {
    setLoading(true);
    setError(null);
    const query = new URLSearchParams({
      page: String(targetPage),
      pageSize: String(pageSize),
    });
    if (filters.status)
      query.set('status', filters.status);
    if (filters.from)
      query.set('invoiceDateFrom', filters.from);
    if (filters.to)
      query.set('invoiceDateTo', filters.to);
    if (filters.number)
      query.set('submissionNumber', filters.number);
    try {
      setResult(await portalApi.getApSubmissions(query));
    } catch (loadError) {
      setError(loadError instanceof PortalApiError
        ? loadError.message
        : loadError instanceof Error ? loadError.message : '無法載入請款案件。');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load(page);
  }, [page, filters]);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (fromInput && toInput && fromInput > toInput) {
      setError('發票日期起日不可晚於迄日。');
      return;
    }
    setPage(1);
    setFilters({
      status: statusInput,
      from: fromInput,
      to: toInput,
      number: numberInput.trim(),
    });
  }

  const currentItems = result?.items ?? [];
  const totalPages = Math.max(1, Math.ceil((result?.totalCount ?? 0) / pageSize));

  return (
    <>
      <div className="page-heading">
        <div><span className="eyebrow">ACCOUNTS PAYABLE</span><h1>請款送審</h1><p>建立供應商請款案件，補充憑證並追蹤審核結果。</p></div>
        <Link className="button button-primary" to="/ap-submissions/new"><Plus size={16} />新增請款</Link>
      </div>
      <div className="notice notice-info ap-info">
        本頁顯示請款審核狀態；核准不代表已付款，目前不提供付款進度或預計匯款日。
      </div>
      <section className="panel">
        <form className="ap-filters" onSubmit={applyFilters}>
          <label className="field">
            案件狀態
            <select value={statusInput} onChange={event => setStatusInput(event.target.value)}>
              <option value="">全部狀態</option>
              {Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="field">發票日期起<input type="date" value={fromInput} onChange={event => setFromInput(event.target.value)} /></label>
          <label className="field">發票日期迄<input type="date" value={toInput} onChange={event => setToInput(event.target.value)} /></label>
          <label className="field ap-filter-number">案件編號<input value={numberInput} onChange={event => setNumberInput(event.target.value)} placeholder="輸入案件編號" /></label>
          <button type="submit" className="button button-secondary"><Search size={15} />套用篩選</button>
        </form>

        {error && <ErrorState message={error} onRetry={() => void load()} />}
        {loading ? <div className="table-loading">正在載入請款案件…</div>
          : currentItems.length === 0 ? <EmptyState
            title={filters.status || filters.from || filters.to || filters.number ? '找不到符合條件的請款案件' : '目前沒有請款案件'}
            detail={filters.status || filters.from || filters.to || filters.number ? '請調整篩選條件後再試。' : '建立請款案件後，案件狀態與審核結果會顯示在這裡。'}
          />
            : <>
              <div className="toolbar">
                <div className="toolbar-summary">共 <strong>{result?.totalCount ?? 0}</strong> 筆案件</div>
                <button className="button button-quiet" onClick={() => void load()}><RefreshCw size={14} />重新載入</button>
              </div>
              <div className="table-wrap">
                <table className="data-table">
                  <thead><tr><th>案件編號</th><th>供應商發票</th><th>含稅總額</th><th>案件狀態</th><th>核對結果</th><th>送審／建立日期</th><th><span className="sr-only">操作</span></th></tr></thead>
                  <tbody>{currentItems.map(item => <SubmissionRow key={item.id} item={item} />)}</tbody>
                </table>
              </div>
              <div className="ap-pagination">
                <span>第 {result?.page ?? page} / {totalPages} 頁</span>
                <div>
                  <button className="button button-secondary" disabled={page <= 1} onClick={() => setPage(current => current - 1)}><ArrowLeft size={14} />上一頁</button>
                  <button className="button button-secondary" disabled={page >= totalPages} onClick={() => setPage(current => current + 1)}>下一頁<ArrowRight size={14} /></button>
                </div>
              </div>
            </>}
      </section>
    </>
  );
}
