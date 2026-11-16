import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  Download,
  FileText,
  Send,
  Trash2,
  Upload,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useParams, useOutletContext } from 'react-router-dom';
import { portalApi, PortalApiError } from '../api/client';
import type {
  SupplierApSubmissionAttachment,
  SupplierApSubmissionDetail,
  SupplierPortalSession,
} from '../api/types';
import { EmptyState, ErrorState } from '../components/Feedback';
import {
  ApSubmissionEditor,
  createApSubmissionFormValue,
} from './ApSubmissionEditor';

const statusLabels: Record<string, string> = {
  draft: '草稿',
  submitted: '審核中',
  returned: '待補件',
  approved: '已核准',
  rejected: '已拒絕',
  cancelled: '已取消',
};

const actionLabels: Record<string, string> = {
  approve: '核准',
  approved: '核准',
  return: '退回補件',
  returned: '退回補件',
  reject: '拒絕',
  rejected: '拒絕',
  'supplier_ap_submission.cancelled': '供應商取消',
};

const discrepancyLabels: Record<string, string> = {
  invoice_amount_mismatch: '發票未稅金額與明細加總不一致',
  tax_amount_mismatch: '發票稅額與明細加總不一致',
  total_amount_mismatch: '發票含稅總額與明細加總不一致',
};

function message(error: unknown, fallback: string) {
  return error instanceof PortalApiError
    ? error.message
    : error instanceof Error ? error.message : fallback;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function formatAmount(value: number, currency: string) {
  return `${currency} ${value.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function formatDate(value: string | null) {
  if (!value)
    return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-TW', {
        dateStyle: 'medium',
        timeStyle: value.includes('T') ? 'short' : undefined,
      }).format(date);
}

function statusStyle(status: string) {
  if (status === 'approved')
    return 'status-open';
  if (status === 'submitted' || status === 'returned')
    return 'status-pending';
  return 'status-muted';
}

function scanLabel(status: string) {
  const labels: Record<string, string> = {
    clean: '安全掃描通過',
    pending_scan: '等待安全掃描',
    scan_failed: '掃描服務無法完成',
    rejected: '未通過安全掃描',
    revoked: '已撤銷',
  };
  return labels[status] || '掃描狀態未知';
}

function discrepancyLabel(value: string) {
  const base = value.split(':')[0] ?? value;
  return discrepancyLabels[base] || '來源或金額資料需要進一步核對';
}

function downloadFile(file: Blob, fileName: string) {
  const url = URL.createObjectURL(file);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function ApSubmissionDetailPage() {
  const { submissionId = '' } = useParams();
  const location = useLocation();
  const { session } = useOutletContext<{ session: SupplierPortalSession }>();
  const [detail, setDetail] = useState<SupplierApSubmissionDetail | null>(null);
  const [etag, setEtag] = useState<string | null>(null);
  const [attachments, setAttachments] = useState<SupplierApSubmissionAttachment[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(
    isRecord(location.state) && typeof location.state.notice === 'string'
      ? location.state.notice
      : null,
  );

  async function loadAttachments() {
    try {
      setAttachments(await portalApi.getApSubmissionAttachments(submissionId));
      setAttachmentError(null);
    } catch (loadError) {
      setAttachments([]);
      setAttachmentError(message(loadError, '無法載入請款附件。'));
    }
  }

  async function load(showLoading = true) {
    if (showLoading)
      setLoading(true);
    setError(null);
    try {
      const response = await portalApi.getApSubmission(submissionId);
      setDetail(response.data);
      setEtag(response.etag);
      await loadAttachments();
    } catch (loadError) {
      setEtag(null);
      setError(message(loadError, '無法載入請款案件。'));
    } finally {
      if (showLoading)
        setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [submissionId]);

  const canEdit = Boolean(
    detail
      && session.role.toLowerCase() === 'sales'
      && ['draft', 'returned'].includes(detail.status),
  );
  const editableWithVersion = canEdit && Boolean(etag);
  const currentAttachments = useMemo(
    () => attachments.filter(item => item.revisionNumber === detail?.currentRevisionNumber),
    [attachments, detail?.currentRevisionNumber],
  );
  const cleanAttachments = currentAttachments.filter(item => item.scanStatus.toLowerCase() === 'clean');
  const hasAttachmentBlocker = currentAttachments.some(item => item.scanStatus.toLowerCase() !== 'clean');

  const formValue = detail
    ? createApSubmissionFormValue(
        detail.purchaseOrderId,
        detail.purchaseOrderNumber,
        detail.goodsReceiptId,
        detail.goodsReceiptNumber,
        detail.currency,
        detail.lines.map(line => ({
          goodsReceiptItemId: line.goodsReceiptItemId,
          skuNumber: line.skuNumber,
          skuName: line.skuName,
          unit: '—',
          acceptedQuantity: line.acceptedQuantity,
          availableQuantity: line.acceptedQuantity,
          unitPrice: line.unitPrice,
          invoicedQuantity: line.invoicedQuantity,
          taxAmount: line.taxAmount,
        })),
        {
          supplierInvoiceNumber: detail.supplierInvoiceNumber,
          supplierInvoiceDate: detail.supplierInvoiceDate,
        },
      )
    : null;

  async function saveDraft(body: Parameters<typeof portalApi.updateApSubmissionDraft>[1]) {
    if (!etag)
      return;
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await portalApi.updateApSubmissionDraft(submissionId, body, etag);
      await load(false);
      setNotice('請款資料已儲存。');
    } catch (saveError) {
      if (saveError instanceof PortalApiError && saveError.status === 409)
        await load(false);
      setError(message(saveError, '無法儲存請款資料。'));
    } finally {
      setSaving(false);
    }
  }

  async function submitForReview() {
    if (!etag || !detail)
      return;
    if (cleanAttachments.length === 0 || hasAttachmentBlocker) {
      setAttachmentError('目前版本須至少有一份安全掃描通過的附件，且所有有效附件皆須通過掃描。');
      return;
    }
    if (!window.confirm('送出後請款內容與附件將轉為唯讀，並交由內部審核。確定送審？'))
      return;
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await portalApi.submitApSubmission(submissionId, etag);
      await load(false);
      setNotice('請款已送交內部審核。');
    } catch (submitError) {
      if (submitError instanceof PortalApiError && submitError.status === 409)
        await load(false);
      setError(message(submitError, '無法提交請款。'));
    } finally {
      setSaving(false);
    }
  }

  async function cancelSubmission() {
    if (!etag || !detail)
      return;
    if (!window.confirm('確定取消此請款案件？取消後無法恢復。'))
      return;
    const enteredReason = window.prompt('取消原因（選填）：');
    if (enteredReason === null)
      return;
    if (enteredReason.length > 2000) {
      setError('取消原因不可超過 2000 個字元。');
      return;
    }
    const reason = enteredReason;
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await portalApi.cancelApSubmission(submissionId, etag, reason);
      await load(false);
      setNotice('請款案件已取消。');
    } catch (cancelError) {
      if (cancelError instanceof PortalApiError && cancelError.status === 409)
        await load(false);
      setError(message(cancelError, '無法取消請款案件。'));
    } finally {
      setSaving(false);
    }
  }

  async function upload(file?: File) {
    if (!file || !detail || !canEdit)
      return;
    if (file.size === 0 || file.size > 20 * 1024 * 1024) {
      setAttachmentError('附件必須大於 0，且不可超過 20 MB。');
      return;
    }
    if (!/\.(pdf|jpe?g|png)$/i.test(file.name)) {
      setAttachmentError('僅允許 PDF、JPEG 或 PNG 格式；伺服器仍會檢查檔案內容及安全性。');
      return;
    }
    if (currentAttachments.length >= 10) {
      setAttachmentError('目前版本有效附件已達 10 個上限。');
      return;
    }
    setUploading(true);
    setAttachmentError(null);
    setNotice(null);
    try {
      const uploaded = await portalApi.uploadApSubmissionAttachment(submissionId, file);
      setNotice(uploaded.scanStatus.toLowerCase() === 'clean'
        ? `附件「${uploaded.originalFileName}」已完成安全掃描。`
        : `附件「${uploaded.originalFileName}」尚未通過安全掃描。`);
    } catch (uploadError) {
        setAttachmentError(message(uploadError, '附件上傳失敗；請確認檔案及安全掃描服務狀態。'));
    } finally {
      await loadAttachments();
      setUploading(false);
    }
  }

  async function revoke(attachment: SupplierApSubmissionAttachment) {
    if (!canEdit || attachment.revisionNumber !== detail?.currentRevisionNumber)
      return;
    if (!window.confirm(`確定撤銷附件「${attachment.originalFileName}」？`))
      return;
    setAttachmentError(null);
    setNotice(null);
    try {
      await portalApi.revokeApSubmissionAttachment(submissionId, attachment.id);
      await loadAttachments();
      setNotice('附件已撤銷。');
    } catch (revokeError) {
      setAttachmentError(message(revokeError, '無法撤銷附件。'));
    }
  }

  async function download(attachment: SupplierApSubmissionAttachment) {
    setAttachmentError(null);
    try {
      const file = await portalApi.downloadApSubmissionAttachment(submissionId, attachment.id);
      downloadFile(file, attachment.originalFileName);
    } catch (downloadError) {
      setAttachmentError(message(downloadError, '附件下載失敗。'));
    }
  }

  if (loading)
    return <div className="table-loading">正在載入請款案件…</div>;
  if (error && !detail) {
    return (
      <>
        <Link className="back-link" to="/ap-submissions"><ArrowLeft size={17} />返回請款案件</Link>
        <ErrorState message={error} onRetry={() => void load()} />
      </>
    );
  }
  if (!detail || !formValue)
    return <EmptyState title="找不到請款案件" detail="案件不存在或不在目前帳號的授權範圍內。" />;

  const editableStatus = ['draft', 'returned'].includes(detail.status);
  const canCancel = canEdit && editableStatus;
  const canSubmit = canEdit && editableStatus;
  const matchResult = detail.matchResult === 'matched'
    ? '來源與金額核對相符'
    : detail.matchResult === 'variance' ? '存在待處理差異' : '等待核對';

  return (
    <>
      <Link className="back-link" to="/ap-submissions"><ArrowLeft size={17} />返回請款案件</Link>
      <div className="page-heading detail-heading">
        <div><span className="eyebrow">AP SUBMISSION / REVISION {detail.currentRevisionNumber}</span><h1>{detail.submissionNumber}</h1><p>建立日期 {formatDate(detail.createdAt)} · 目前版本 {detail.version}</p></div>
        <span className={`status-pill ${statusStyle(detail.status)}`}>{statusLabels[detail.status] || detail.status}</span>
      </div>
      {notice && <div className="notice notice-success" role="status"><CheckCircle2 size={18} />{notice}</div>}
      {error && <ErrorState message={error} />}
      {!etag && editableStatus && <div className="notice notice-error"><AlertCircle size={18} />伺服器未提供版本標籤，為避免覆蓋其他使用者的更新，目前停用修改與送審。</div>}
      {session.role.toLowerCase() !== 'sales' && editableStatus && <div className="notice notice-info"><AlertCircle size={18} />目前帳號僅能檢視，請由供應商業務聯絡人處理此請款。</div>}
      {detail.status === 'approved' && <div className="notice notice-success"><CheckCircle2 size={18} />案件已核准；核准不代表已付款，Portal 目前不提供付款進度。</div>}

      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">INVOICE SUMMARY</span><h2>發票與採購來源</h2></div><span className="status-pill status-muted">{matchResult}</span></div>
        <div className="capa-facts ap-facts">
          <div><small>供應商發票號碼</small><strong>{detail.supplierInvoiceNumber}</strong></div>
          <div><small>發票日期</small><strong>{formatDate(detail.supplierInvoiceDate)}</strong></div>
          <div><small>採購單</small><strong>{detail.purchaseOrderNumber}</strong></div>
          <div><small>收貨單</small><strong>{detail.goodsReceiptNumber}</strong></div>
          <div><small>未稅金額</small><strong>{formatAmount(detail.invoiceAmount, detail.currency)}</strong></div>
          <div><small>稅額</small><strong>{formatAmount(detail.taxAmount, detail.currency)}</strong></div>
          <div><small>含稅總額</small><strong>{formatAmount(detail.totalAmount, detail.currency)}</strong></div>
          {detail.accountsPayableNumber && <div><small>正式 AP 編號</small><strong>{detail.accountsPayableNumber}</strong></div>}
          {detail.approvedAt && <div><small>核准時間</small><strong>{formatDate(detail.approvedAt)}</strong></div>}
        </div>
      </section>

      {detail.discrepancies.length > 0 && (
        <section className="panel">
          <div className="panel-heading"><div><span className="eyebrow">MATCH REVIEW</span><h2>核對提示</h2></div></div>
          <ul className="ap-discrepancies">{detail.discrepancies.map(item => <li key={item}>{discrepancyLabel(item)}</li>)}</ul>
        </section>
      )}

      {canEdit && (
        <>
          <ApSubmissionEditor
            key={`${detail.id}-${detail.currentRevisionNumber}`}
            initialValue={formValue}
            disabled={!editableWithVersion}
            saving={saving || uploading}
            submitLabel="儲存請款資料"
            onSubmit={saveDraft}
          />
          {canCancel && <div className="ap-cancel-row">
            <span>確認內容有誤或重複建立時，可取消此草稿。</span>
            <button className="button button-danger-quiet" onClick={() => void cancelSubmission()} disabled={saving || uploading || !etag}><Trash2 size={15} />取消案件</button>
          </div>}
        </>
      )}
      {!canEdit && <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">INVOICE LINES</span><h2>請款明細</h2></div></div>
        <div className="table-wrap">
          <table className="data-table">
            <thead><tr><th>品項</th><th>驗收數量</th><th>請款數量</th><th>單價</th><th>未稅金額</th><th>稅額</th><th>含稅金額</th></tr></thead>
            <tbody>{detail.lines.map(line => (
              <tr key={line.goodsReceiptItemId}>
                <td><strong>{line.skuNumber || '未提供料號'}</strong><small className="table-subtext">{line.skuName || '未提供品名'}</small></td>
                <td>{line.acceptedQuantity.toLocaleString('zh-TW')}</td>
                <td>{line.invoicedQuantity.toLocaleString('zh-TW')}</td>
                <td>{formatAmount(line.unitPrice, detail.currency)}</td>
                <td>{formatAmount(line.netAmount, detail.currency)}</td>
                <td>{formatAmount(line.taxAmount, detail.currency)}</td>
                <td>{formatAmount(line.totalAmount, detail.currency)}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      </section>}

      <section className="panel">
        <div className="panel-heading">
          <div><span className="eyebrow">SUPPORTING DOCUMENTS</span><h2>請款附件</h2><p>每檔上限 20 MB，每版本最多 10 個有效附件；提交前須至少一份 clean 且不得有未通過掃描的附件。</p></div>
        </div>
        {attachmentError && <ErrorState message={attachmentError} onRetry={() => void loadAttachments()} />}
        {canEdit && <label className={`button button-secondary capa-upload${uploading ? ' is-disabled' : ''}`}>
          <Upload size={15} />{uploading ? '正在上傳及掃描…' : '上傳發票憑證'}
          <input
            type="file"
            accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
            disabled={saving || uploading || currentAttachments.length >= 10}
            onChange={event => {
              const file = event.currentTarget.files?.[0];
              event.currentTarget.value = '';
              void upload(file);
            }}
          />
        </label>}
        {canSubmit && currentAttachments.length === 0 && <div className="notice notice-info"><AlertCircle size={17} />目前版本尚無附件。至少上傳一份安全掃描通過的發票憑證後才能送審。</div>}
        {attachments.length === 0 ? <EmptyState title="尚無請款附件" detail="上傳發票或其他請款憑證後，掃描狀態會顯示在這裡。" />
          : <ul className="capa-attachment-list ap-attachment-list">
            {attachments.map(attachment => {
              const currentRevision = attachment.revisionNumber === detail.currentRevisionNumber;
              const clean = attachment.scanStatus.toLowerCase() === 'clean';
              const revoked = attachment.scanStatus.toLowerCase() === 'revoked';
              return (
                <li key={attachment.id}>
                  <span className={`attachment-mark${clean ? ' is-clean' : ''}`}><FileText size={17} /></span>
                  <span className="attachment-name"><strong>{attachment.originalFileName}</strong><small>第 {attachment.revisionNumber} 版 · {(attachment.sizeBytes / 1024 / 1024).toFixed(2)} MB · {formatDate(attachment.uploadedAt)}</small></span>
                  <span className={`status-pill ${clean ? 'status-open' : 'status-pending'}`}>{scanLabel(attachment.scanStatus)}</span>
                  {clean && <button className="table-action" onClick={() => void download(attachment)} aria-label={`下載 ${attachment.originalFileName}`}><Download size={15} /></button>}
                  {canEdit && currentRevision && !revoked && <button className="button button-quiet attachment-remove" onClick={() => void revoke(attachment)} aria-label={`撤銷 ${attachment.originalFileName}`}><Trash2 size={15} /></button>}
                </li>
              );
            })}
          </ul>}
        {canSubmit && <div className="ap-submit-row">
          <div><strong>送交內部審核</strong><small>{cleanAttachments.length} 份 clean 附件；請款提交後資料及附件將轉為唯讀。</small></div>
          <button className="button button-primary" onClick={() => void submitForReview()} disabled={saving || uploading || !etag || cleanAttachments.length === 0 || hasAttachmentBlocker}><Send size={15} />送審</button>
        </div>}
      </section>

      <section className="panel">
        <div className="panel-heading"><div><span className="eyebrow">REVIEW HISTORY</span><h2>審核歷程</h2></div></div>
        {detail.reviews.length === 0 ? <EmptyState title="尚無審核紀錄" detail="案件送審或進行內部審核後，相關紀錄會顯示在這裡。" />
          : <div className="history-list">
            {detail.reviews.map(review => (
              <div className="history-entry" key={review.id}>
                <span className="history-icon"><CheckCircle2 size={15} /></span>
                <div><strong>{actionLabels[review.action] || '案件處理紀錄'}</strong><small>{formatDate(review.createdAt)} · {review.matchResult === 'matched' ? '核對相符' : review.matchResult === 'variance' ? '存在差異' : '待核對'}</small>{review.reason && <p>{review.reason}</p>}</div>
              </div>
            ))}
          </div>}
      </section>
    </>
  );
}
