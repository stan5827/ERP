import {
  AlertCircle,
  ArrowDownToLine,
  ArrowLeft,
  CheckCircle2,
  ClipboardCheck,
  FileUp,
  History,
  RotateCw,
  Send,
  Trash2,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link, useOutletContext, useParams } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type {
  CapaAttachment,
  CapaResponseSubmission,
  SupplierComplaintDetail,
  SupplierComplaintSummary,
  SupplierPortalSession,
} from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';

const disciplines = [
  { key: 'd1Team', label: 'D1｜組成改善團隊', hint: '列出負責人、職責與跨部門成員。', maxLength: 500 },
  { key: 'd2ProblemDescription', label: 'D2｜問題描述', hint: '說明問題發生的位置、時間、數量與影響範圍。', maxLength: 8000 },
  { key: 'd3InterimContainment', label: 'D3｜暫時圍堵措施', hint: '描述隔離、篩選或其他防止問題擴大的措施。', maxLength: 8000 },
  { key: 'd4RootCause', label: 'D4｜根本原因分析', hint: '說明問題成因及確認根因所使用的證據。', maxLength: 8000 },
  { key: 'd5PermanentAction', label: 'D5｜永久矯正措施', hint: '描述針對根因執行的永久改善措施。', maxLength: 8000 },
  { key: 'd6Verification', label: 'D6｜矯正措施驗證', hint: '說明驗證方法、結果及有效性證據。', maxLength: 8000 },
  { key: 'd7Prevention', label: 'D7｜預防再發措施', hint: '描述如何將改善成果延伸至其他產品或流程。', maxLength: 8000 },
  { key: 'd8Closure', label: 'D8｜結案與經驗回饋', hint: '記錄結案條件、經驗回饋及後續追蹤。', maxLength: 8000 },
] as const satisfies ReadonlyArray<{
  key: keyof CapaResponseSubmission;
  label: string;
  hint: string;
  maxLength: number;
}>;

const actionLabels: Record<string, string> = {
  draft_saved: '已儲存草稿',
  submitted: '已送交品質審查',
};

function displayDate(value: string | null) {
  if (!value)
    return '未指定';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '未指定'
    : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(date);
}

function statusLabel(status: string | null) {
  const labels: Record<string, string> = {
    open: '待處理',
    in_progress: '草稿進行中',
    revision_requested: '待補正',
    submitted: '已送審',
    under_review: '審查中',
    approved: '已核准',
    closed: '已結案',
    cancelled: '已取消',
  };
  return status ? labels[status.toLowerCase()] || status : '尚未建立 CAPA';
}

function statusClass(status: string | null) {
  const normalized = status?.toLowerCase();
  if (normalized === 'revision_requested' || normalized === 'open')
    return 'status-pending';
  if (normalized === 'in_progress')
    return 'status-open';
  return 'status-muted';
}

function initialResponse(detail: SupplierComplaintDetail): CapaResponseSubmission {
  return {
    d1Team: detail.d1Team || '',
    d2ProblemDescription: detail.d2ProblemDescription || '',
    d3InterimContainment: detail.d3InterimContainment || '',
    d4RootCause: detail.d4RootCause || '',
    d5PermanentAction: detail.d5PermanentAction || '',
    d6Verification: detail.d6Verification || '',
    d7Prevention: detail.d7Prevention || '',
    d8Closure: detail.d8Closure || '',
  };
}

function canEditCapa(detail: SupplierComplaintDetail, role: string) {
  const status = (detail.capaStatus || 'open').toLowerCase();
  const complaintStatus = detail.status.toLowerCase();
  return role.toLowerCase() === 'sales'
    && ['open', 'in_progress', 'revision_requested'].includes(status)
    && !['closed', 'cancelled', 'canceled'].includes(complaintStatus);
}

function needsSupplierResponse(status: string | null) {
  return !status || ['open', 'in_progress', 'revision_requested'].includes(status.toLowerCase());
}

function isResponseOverdue(responseDue: string | null, status: string | null) {
  return Boolean(responseDue
    && needsSupplierResponse(status)
    && new Date(responseDue).getTime() < Date.now());
}

function complaintError(error: unknown, fallback: string) {
  return error instanceof PortalApiError ? error.message
    : error instanceof Error ? error.message
      : fallback;
}

export function CapaPage() {
  const [complaints, setComplaints] = useState<SupplierComplaintSummary[]>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setComplaints(await portalApi.getComplaints());
    } catch (loadError) {
      setError(complaintError(loadError, '無法載入客訴清單。'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const filtered = useMemo(() => {
    const query = search.trim().toLocaleLowerCase();
    return complaints.filter(item =>
      !query
      || item.complaintNumber.toLocaleLowerCase().includes(query)
      || (item.capaNumber || '').toLocaleLowerCase().includes(query)
      || (item.defectType || '').toLocaleLowerCase().includes(query));
  }, [complaints, search]);

  const pendingCount = complaints.filter(item => needsSupplierResponse(item.capaStatus)).length;

  return (
    <>
      <div className="page-heading">
        <div><span className="eyebrow">QUALITY COLLABORATION</span><h1>客訴與 CAPA</h1><p>檢視品質客訴、追蹤回覆期限，並與品質團隊協作改善。</p></div>
      </div>
      <div className="capa-summary">
        <div className="capa-summary-icon"><ClipboardCheck size={19} /></div>
        <div><span>待處理案件</span><strong>{pendingCount}</strong></div>
        <p>所有資料僅限目前登入供應商檢視。</p>
      </div>
      <section className="panel">
        <div className="toolbar">
          <div className="toolbar-summary"><strong>{complaints.length}</strong> 筆品質客訴</div>
          <label className="search-field">
            <ClipboardCheck size={17} />
            <span className="sr-only">搜尋客訴或 CAPA 編號</span>
            <input value={search} onChange={event => setSearch(event.target.value)} placeholder="搜尋客訴或 CAPA 編號" />
          </label>
        </div>
        {error && <ErrorState message={error} onRetry={() => void load()} />}
        {loading ? <div className="table-loading">正在載入客訴資料…</div>
          : filtered.length === 0 ? <EmptyState
            title={search ? '找不到符合條件的客訴' : '目前沒有品質客訴'}
            detail={search ? '請調整搜尋條件後再試。' : '新的品質客訴送達後會顯示在這裡。'}
          />
            : <div className="table-wrap">
              <table className="data-table capa-table">
                <thead><tr><th>客訴編號</th><th>缺陷類型</th><th>缺陷數量</th><th>優先等級</th><th>回覆期限</th><th>CAPA 狀態</th><th><span className="sr-only">操作</span></th></tr></thead>
                <tbody>
                  {filtered.map(item => (
                    <tr key={item.complaintId}>
                      <td><strong>{item.complaintNumber}</strong>{item.capaNumber && <small className="table-subtext">{item.capaNumber}</small>}</td>
                      <td>{item.defectType || '未分類'}</td>
                      <td>{item.defectQuantity.toLocaleString('zh-TW')}</td>
                      <td><span className="status-pill status-muted">{item.priority}</span></td>
                      <td className={isResponseOverdue(item.responseDue, item.capaStatus) ? 'capa-overdue' : ''}>
                        {displayDate(item.responseDue)}
                        {isResponseOverdue(item.responseDue, item.capaStatus) && <small className="table-subtext">已逾期</small>}
                      </td>
                      <td><span className={`status-pill ${statusClass(item.capaStatus)}`}>{statusLabel(item.capaStatus)}</span></td>
                      <td><Link className="table-action" to={`/capa/${item.complaintId}`} aria-label={`開啟 ${item.complaintNumber}`}><ClipboardCheck size={16} /></Link></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>}
      </section>
    </>
  );
}

export function CapaDetailPage() {
  const { complaintId = '' } = useParams();
  const { session } = useOutletContext<{ session: SupplierPortalSession }>();
  const [detail, setDetail] = useState<SupplierComplaintDetail | null>(null);
  const [response, setResponse] = useState<CapaResponseSubmission | null>(null);
  const [attachments, setAttachments] = useState<CapaAttachment[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);

  async function loadAttachments() {
    try {
      setAttachments(await portalApi.getCapaAttachments(complaintId));
      setAttachmentError(null);
    } catch (loadError) {
      setAttachmentError(complaintError(loadError, '無法載入附件清單。'));
    }
  }

  async function load() {
    setLoading(true);
    setError(null);
    setDetail(null);
    setResponse(null);
    setAttachments([]);
    try {
      const current = await portalApi.getComplaint(complaintId);
      setDetail(current);
      setResponse(initialResponse(current));
      await loadAttachments();
    } catch (loadError) {
      setError(complaintError(loadError, '無法載入客訴明細。'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [complaintId]);

  function updateResponse(key: keyof CapaResponseSubmission, value: string) {
    setResponse(current => current ? { ...current, [key]: value } : current);
    setNotice(null);
  }

  async function save(submit: boolean) {
    if (!detail || !response)
      return;
    if (submit) {
      const missing = disciplines.find(field => !response[field.key].trim());
      if (missing) {
        setError(`送審前請完成 ${missing.label}。`);
        return;
      }
      if (!window.confirm('送出 CAPA 後將交由品質人員審查，您確認內容已完整且正確？'))
        return;
    }

    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      const result = submit
        ? await portalApi.submitCapaResponse(complaintId, response)
        : await portalApi.saveCapaDraft(complaintId, response);
      await load();
      setNotice(`第 ${result.revisionNumber} 版${submit ? '回覆已送交品質審查' : '草稿已儲存'}。`);
    } catch (saveError) {
      if (saveError instanceof PortalApiError && saveError.status === 409) {
        await load();
      }
      setError(complaintError(saveError, submit ? 'CAPA 未能送審。' : 'CAPA 草稿未能儲存。'));
    } finally {
      setSaving(false);
    }
  }

  async function upload(file?: File) {
    if (!file)
      return;
    if (file.size > 20 * 1024 * 1024) {
      setAttachmentError('單一附件不可超過 20 MB。');
      return;
    }
    if (!/\.(pdf|jpe?g|png)$/i.test(file.name)) {
      setAttachmentError('僅允許 PDF、JPEG 或 PNG 格式。');
      return;
    }

    setUploading(true);
    setAttachmentError(null);
    try {
      const result = await portalApi.uploadCapaAttachment(complaintId, file);
      setNotice(`附件「${result.originalFileName}」已完成安全掃描。`);
    } catch (uploadError) {
      setAttachmentError(complaintError(uploadError, '附件上傳失敗。'));
    } finally {
      await loadAttachments();
      setUploading(false);
    }
  }

  async function download(attachment: CapaAttachment) {
    setAttachmentError(null);
    try {
      const file = await portalApi.downloadCapaAttachment(complaintId, attachment.attachmentId);
      const url = URL.createObjectURL(file);
      const link = document.createElement('a');
      link.href = url;
      link.download = attachment.originalFileName;
      document.body.append(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (downloadError) {
      setAttachmentError(complaintError(downloadError, '附件下載失敗。'));
    }
  }

  async function revoke(attachment: CapaAttachment) {
    if (!window.confirm(`確定移除附件「${attachment.originalFileName}」？`))
      return;
    setAttachmentError(null);
    try {
      await portalApi.revokeCapaAttachment(complaintId, attachment.attachmentId);
      await loadAttachments();
    } catch (revokeError) {
      setAttachmentError(complaintError(revokeError, '附件無法移除。'));
    }
  }

  if (loading)
    return <div className="table-loading">正在載入 CAPA 明細…</div>;
  if (error && !detail)
    return <><Link className="back-link" to="/capa"><ArrowLeft size={17} />返回客訴清單</Link><ErrorState message={error} onRetry={() => void load()} /></>;
  if (!detail || !response)
    return <EmptyState title="找不到客訴資料" detail="資料不存在或不在目前帳號的授權範圍內。" />;

  const editable = canEditCapa(detail, session.role);
  const attachmentEditable = editable && Boolean(detail.capaId);
  const activeAttachments = attachments.filter(item => item.scanStatus.toLowerCase() !== 'revoked');
  const cleanAttachments = activeAttachments.every(item => item.scanStatus.toLowerCase() === 'clean');
  const canSubmit = disciplines.every(field => response[field.key].trim()) && cleanAttachments;
  const overdue = isResponseOverdue(detail.responseDue, detail.capaStatus);

  return (
    <>
      <Link className="back-link" to="/capa"><ArrowLeft size={17} />返回客訴清單</Link>
      <div className="page-heading detail-heading">
        <div><span className="eyebrow">COMPLAINT / CAPA</span><h1>{detail.complaintNumber}</h1><p>建立日期 {displayDate(detail.createdAt)}{detail.capaNumber && <> <span className="heading-divider">·</span> CAPA {detail.capaNumber}</>}</p></div>
        <span className={`status-pill ${statusClass(detail.capaStatus)}`}>{statusLabel(detail.capaStatus)}</span>
      </div>

      {notice && <div className="notice notice-success" role="status"><CheckCircle2 size={18} />{notice}</div>}
      {error && <ErrorState message={error} />}

      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">QUALITY NOTICE</span><h2>客訴內容</h2></div>{overdue && <span className="status-pill status-danger">回覆已逾期</span>}</div>
        <div className="capa-facts">
          <div><small>缺陷類型</small><strong>{detail.defectType || '未分類'}</strong></div>
          <div><small>優先等級</small><strong>{detail.priority}</strong></div>
          <div><small>缺陷數量</small><strong>{detail.defectQuantity.toLocaleString('zh-TW')}</strong></div>
          <div><small>PPM</small><strong>{detail.ppm.toLocaleString('zh-TW')}</strong></div>
          <div><small>批號</small><strong>{detail.lotNumber || '未提供'}</strong></div>
          <div><small>回覆期限</small><strong>{displayDate(detail.responseDue)}</strong></div>
        </div>
        {detail.description && <p className="capa-description">{detail.description}</p>}
      </section>

      <form onSubmit={event => { event.preventDefault(); void save(true); }}>
        <section className="panel">
          <div className="panel-heading"><div><span className="eyebrow">CORRECTIVE ACTION RESPONSE</span><h2>矯正與預防措施（D1–D8）</h2><p>草稿可分次儲存；送審時須完整填寫八個改善項目。</p></div></div>
          {!editable && <div className="notice notice-info"><AlertCircle size={18} />目前狀態為「{statusLabel(detail.capaStatus)}」，供應商暫時無法修改；如需補充，請聯絡品質窗口。</div>}
          {session.role.toLowerCase() !== 'sales' && <div className="notice notice-info"><AlertCircle size={18} />目前帳號僅能檢視，請由供應商業務聯絡人填寫 CAPA。</div>}
          <div className="capa-disciplines">
            {disciplines.map((field, index) => (
              <label className="field capa-discipline" key={field.key}>
                <span><b>{String(index + 1).padStart(2, '0')}</b>{field.label}</span>
                <small>{field.hint}</small>
                <textarea
                  maxLength={field.maxLength}
                  disabled={!editable || saving}
                  value={response[field.key]}
                  onChange={event => updateResponse(field.key, event.target.value)}
                  placeholder="請填寫改善措施與佐證內容"
                  rows={field.key === 'd1Team' ? 2 : 4}
                />
              </label>
            ))}
          </div>
          {editable && <div className="form-actions capa-form-actions">
            <span>儲存草稿不會送交審查。提交後需由品質人員退回才能再次修改。</span>
            <div>
              <button type="button" className="button button-secondary" disabled={saving} onClick={() => void save(false)}>
                <CheckCircle2 size={16} />{saving ? '處理中…' : '儲存草稿'}
              </button>
              <button type="submit" className="button button-primary" disabled={saving || !canSubmit}>
                <Send size={16} />{saving ? '送審中…' : '提交品質審查'}
              </button>
            </div>
          </div>}
          {editable && !cleanAttachments && <p className="capa-inline-help">附件必須完成安全掃描後才能提交 CAPA。</p>}
        </section>
      </form>

      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">SUPPORTING EVIDENCE</span><h2>改善佐證附件</h2><p>僅接受 PDF、JPEG、PNG，每個 CAPA 最多 10 個有效附件，單檔上限 20 MB。</p></div><span className="count-chip">{activeAttachments.length} / 10</span></div>
        {attachmentError && <ErrorState message={attachmentError} />}
        {attachmentEditable && activeAttachments.length < 10 && <label className={`button button-secondary capa-upload${uploading ? ' is-disabled' : ''}`}>
          <FileUp size={16} />{uploading ? '上傳及掃描中…' : '選擇附件'}
          <input
            type="file"
            accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
            disabled={uploading}
            onChange={event => {
              const file = event.currentTarget.files?.[0];
              event.currentTarget.value = '';
              void upload(file);
            }}
          />
        </label>}
        {!detail.capaId && editable && <p className="capa-inline-help">請先儲存至少一項 CAPA 草稿，再上傳佐證附件。</p>}
        {activeAttachments.length === 0 ? <EmptyState title="尚無附件" detail="可上傳檢驗結果、改善照片或其他佐證資料。" />
          : <ul className="capa-attachment-list">
            {activeAttachments.map(attachment => {
              const clean = attachment.scanStatus.toLowerCase() === 'clean';
              const canRevoke = attachmentEditable && attachment.revisionNumber === null;
              return <li key={attachment.attachmentId}>
                <span className={`attachment-mark${clean ? ' is-clean' : ''}`}><ClipboardCheck size={17} /></span>
                <span className="attachment-name"><strong>{attachment.originalFileName}</strong><small>{(attachment.sizeBytes / 1024 / 1024).toFixed(2)} MB · {displayDate(attachment.uploadedAt)}</small></span>
                <span className={`status-pill ${clean ? 'status-open' : 'status-pending'}`}>{clean ? '安全' : attachment.scanStatus === 'rejected' ? '未通過掃描' : '掃描未完成'}</span>
                {clean && <button className="table-action" type="button" aria-label={`下載 ${attachment.originalFileName}`} onClick={() => void download(attachment)}><ArrowDownToLine size={16} /></button>}
                {canRevoke && <button className="table-action attachment-remove" type="button" aria-label={`移除 ${attachment.originalFileName}`} onClick={() => void revoke(attachment)}><Trash2 size={16} /></button>}
              </li>;
            })}
          </ul>}
        {editable && !detail.capaId && <p className="capa-inline-help">附件會在 CAPA 提交時綁定至該回覆版本；已提交版本附件不可撤回。</p>}
        {attachmentError && attachmentError.includes('無法載入') && <button className="button button-quiet" type="button" onClick={() => void loadAttachments()}><RotateCw size={15} />重新載入附件</button>}
      </section>

      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">RESPONSE HISTORY</span><h2>回覆修訂歷程</h2></div><History size={19} className="muted-icon" /></div>
        {detail.revisions.length === 0 ? <EmptyState title="尚無回覆紀錄" detail="儲存第一版 CAPA 草稿後，修訂歷程會顯示在這裡。" />
          : <div className="history-list">
            {detail.revisions.map(revision => {
              const filledDisciplines = disciplines.filter(field => revision[field.key]);
              return <article className="history-entry capa-history-entry" key={revision.revisionNumber}>
                <span className="history-icon"><History size={15} /></span>
                <div className="history-content">
                  <strong>第 {revision.revisionNumber} 版 · {actionLabels[revision.action] || revision.action}</strong>
                  <small>{displayDate(revision.createdAt)}</small>
                  <div className="history-disciplines">
                    {filledDisciplines.map(field => <div key={field.key}><b>{field.label}</b><p>{revision[field.key]}</p></div>)}
                  </div>
                </div>
              </article>;
            })}
          </div>}
      </section>
    </>
  );
}
