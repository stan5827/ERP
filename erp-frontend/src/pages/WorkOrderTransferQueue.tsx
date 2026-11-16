import { Fragment, useMemo, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { ChevronDown, ChevronUp, Factory, Loader2, RefreshCw, Search } from 'lucide-react';
import type { ApiResponse } from '../api/client';
import type { WorkOrderTransferCandidateDto } from '../api/mes';
import { workOrdersApi } from '../api/mes';
import './WorkOrderTransferQueue.css';

interface WorkOrderTransferQueueProps {
  canTransfer: boolean;
}

const getErrorMessage = (error: unknown) => {
  if (axios.isAxiosError<ApiResponse<unknown>>(error))
    return error.response?.data?.message || error.message;
  return error instanceof Error ? error.message : '發生未知錯誤';
};

const formatDate = (value?: string) => value ? value.slice(0, 10) : '—';
const formatQuantity = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value);

const getLineTransferStatus = (item: WorkOrderTransferCandidateDto['items'][number]) => {
  if (item.isAlreadyTransferred)
    return { label: '已建立工單', className: 'is-transferred' };
  if (item.willCreateWorkOrder)
    return { label: '預計轉工單', className: 'is-transferable' };
  if (item.needOutsourcing || item.isOutsourced)
    return { label: '委外，不轉工單', className: 'is-outsourced' };
  return { label: '不需轉工單', className: 'is-excluded' };
};

export const WorkOrderTransferQueue = ({ canTransfer }: WorkOrderTransferQueueProps) => {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [transferringId, setTransferringId] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [actionError, setActionError] = useState('');
  const { data: candidates = [], isLoading, isError, error, refetch } = useQuery({
    queryKey: ['work-order-transfer-candidates'],
    queryFn: async () => {
      const response = await workOrdersApi.salesOrderTransferCandidates();
      if (!response.data.success)
        throw new Error(response.data.message || '載入待轉發訂單失敗');
      return response.data.data ?? [];
    }
  });

  const filteredCandidates = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    if (!term) return candidates;
    return candidates.filter(order =>
      order.orderNumber.toLocaleLowerCase().includes(term)
      || (order.customerName ?? '').toLocaleLowerCase().includes(term)
      || order.items.some(item =>
        `${item.skuNumber ?? ''} ${item.skuName ?? ''} ${item.customerItemCode ?? ''}`.toLocaleLowerCase().includes(term)
      )
    );
  }, [candidates, search]);

  const handleTransfer = async (order: WorkOrderTransferCandidateDto) => {
    if (!canTransfer || transferringId) return;

    const itemsToTransfer = order.items.filter(item => item.willCreateWorkOrder);
    if (itemsToTransfer.length === 0) {
      setActionError('此訂單目前沒有可轉工單的品項，請重新載入並確認訂單內容。');
      return;
    }

    const itemSummary = itemsToTransfer
      .map(item => `第 ${item.lineNumber} 項 ${item.skuNumber || item.skuName || '未命名品項'} × ${formatQuantity(item.quantity)} ${item.unit ?? ''}`.trim())
      .join('\n');
    if (!window.confirm(
      `確認將訂單 ${order.orderNumber} 轉發工單？\n\n本次預計建立工單的品項：\n${itemSummary}\n\n請先核對畫面中的品項、數量與交期。`
    )) return;

    setTransferringId(order.salesOrderId);
    setNotice('');
    setActionError('');
    try {
      const response = await workOrdersApi.createFromSalesOrder(order.salesOrderId);
      if (!response.data.success)
        throw new Error(response.data.message || '工單轉發失敗');

      const workOrders = response.data.data ?? [];
      const workOrderNumbers = workOrders.map(workOrder => workOrder.woNumber).filter(Boolean);
      const message = response.data.message || '工單已建立並連結至銷售訂單';
      setNotice(workOrderNumbers.length > 0
        ? `${message}：${workOrderNumbers.join('、')}`
        : message);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['work-order-transfer-candidates'] }),
        queryClient.invalidateQueries({ queryKey: ['work-orders'] })
      ]);
    } catch (transferError: unknown) {
      setActionError(getErrorMessage(transferError));
    } finally {
      setTransferringId(null);
    }
  };

  return (
    <div className="work-order-transfer-page">
      <section className="work-order-transfer-banner">
        <div className="work-order-transfer-banner__icon"><Factory size={22} /></div>
        <div className="work-order-transfer-banner__copy">
          <p>生產執行 / 訂單交接</p>
          <h1>客戶訂單轉發工單</h1>
          <span>僅顯示已確認、含內製品項且尚未轉發的訂單。</span>
        </div>
        <span className="work-order-transfer-scope">限訂單轉發</span>
      </section>

      <section className="work-order-transfer-card">
        <header className="work-order-transfer-card__header">
          <div>
            <h2>待轉發訂單</h2>
            <p>{filteredCandidates.length} 筆符合條件</p>
          </div>
          <label className="work-order-transfer-search">
            <Search size={16} aria-hidden="true" />
            <input
              aria-label="搜尋訂單或客戶"
              placeholder="搜尋訂單或客戶"
              value={search}
              onChange={event => setSearch(event.target.value)}
            />
          </label>
        </header>

        {notice && <p className="work-order-transfer-message" role="status">{notice}</p>}
        {actionError && <p className="work-order-transfer-error" role="alert">{actionError}</p>}
        {isError ? (
          <div className="work-order-transfer-state work-order-transfer-state--error" role="alert">
            <span>載入訂單失敗：{getErrorMessage(error)}</span>
            <button type="button" className="work-order-transfer-retry" onClick={() => void refetch()}>
              <RefreshCw size={14} /> 重新載入
            </button>
          </div>
        ) : isLoading ? (
          <div className="work-order-transfer-state" role="status">
            <Loader2 className="work-order-transfer-spinner" size={18} />
            載入待轉發訂單…
          </div>
        ) : filteredCandidates.length === 0 ? (
          <div className="work-order-transfer-state" role="status">
            {search
              ? '找不到符合搜尋條件的訂單。'
              : '目前沒有已確認且含內製品項、尚未轉發的訂單。'}
          </div>
        ) : (
          <div className="work-order-transfer-table-wrap">
            <table className="work-order-transfer-table">
              <thead>
                <tr>
                  <th>訂單編號</th>
                  <th>客戶</th>
                  <th>訂單日期</th>
                  <th>預計交期</th>
                  <th>訂單內容</th>
                </tr>
              </thead>
              <tbody>
                {filteredCandidates.map(order => {
                  const isExpanded = expandedOrderId === order.salesOrderId;
                  const transferItemCount = order.items.filter(item => item.willCreateWorkOrder).length;
                  const detailsId = `work-order-transfer-details-${order.salesOrderId}`;
                  return (
                    <Fragment key={order.salesOrderId}>
                      <tr>
                        <td className="work-order-transfer-order-number">{order.orderNumber}</td>
                        <td>{order.customerName || '—'}</td>
                        <td>{formatDate(order.orderDate)}</td>
                        <td>{formatDate(order.expectedDeliveryDate)}</td>
                        <td>
                          <button
                            type="button"
                            className="work-order-transfer-view"
                            aria-expanded={isExpanded}
                            aria-controls={detailsId}
                            onClick={() => {
                              setExpandedOrderId(isExpanded ? null : order.salesOrderId);
                              setActionError('');
                            }}
                          >
                            {isExpanded ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
                            {isExpanded ? '收合內容' : `檢視內容（${order.items.length} 項）`}
                          </button>
                        </td>
                      </tr>
                      {isExpanded && (
                        <tr className="work-order-transfer-details-row">
                          <td id={detailsId} colSpan={5} className="work-order-transfer-details-cell">
                            <section className="work-order-transfer-details">
                              <header className="work-order-transfer-details__header">
                                <div>
                                  <h3>訂單品項明細</h3>
                                  <p>核對品號、規格、數量及預計轉工單項目後，再執行轉發。</p>
                                </div>
                                <span className="work-order-transfer-details__count">
                                  {order.items.length} 項品項・{transferItemCount} 項預計轉工單
                                </span>
                              </header>

                              {order.orderRemarks && (
                                <p className="work-order-transfer-order-remarks">
                                  <strong>訂單備註：</strong>{order.orderRemarks}
                                </p>
                              )}

                              {order.items.length === 0 ? (
                                <p className="work-order-transfer-empty-details" role="alert">
                                  此訂單沒有品項明細，為安全起見無法轉發。
                                </p>
                              ) : (
                                <div className="work-order-transfer-items-wrap">
                                  <table className="work-order-transfer-items">
                                    <thead>
                                      <tr>
                                        <th>項次</th>
                                        <th>品號／品名</th>
                                        <th>規格／客戶料號</th>
                                        <th>訂購數量</th>
                                        <th>預計完工</th>
                                        <th>轉單處理</th>
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {order.items.map(item => {
                                        const status = getLineTransferStatus(item);
                                        return (
                                          <tr key={`${order.salesOrderId}-${item.lineNumber}`}>
                                            <td>{item.lineNumber}</td>
                                            <td>
                                              <strong>{item.skuNumber || '—'}</strong>
                                              <span className="work-order-transfer-item-name">{item.skuName || '未命名品項'}</span>
                                              {item.lineRemarks && <span className="work-order-transfer-item-note">{item.lineRemarks}</span>}
                                            </td>
                                            <td>
                                              <span>{item.specification || '—'}</span>
                                              {item.customerItemCode && <span className="work-order-transfer-item-note">客戶料號：{item.customerItemCode}</span>}
                                            </td>
                                            <td className="work-order-transfer-item-quantity">
                                              {formatQuantity(item.quantity)} {item.unit ?? ''}
                                            </td>
                                            <td>{formatDate(item.plannedFinishDate || order.expectedDeliveryDate)}</td>
                                            <td>
                                              <span className={`work-order-transfer-line-status ${status.className}`}>
                                                {status.label}
                                              </span>
                                            </td>
                                          </tr>
                                        );
                                      })}
                                    </tbody>
                                  </table>
                                </div>
                              )}

                              <footer className="work-order-transfer-details__footer">
                                <span>系統會嘗試為標示「預計轉工單」的品項建立工單；若 BOM、路由或單位設定缺漏，會阻擋轉單並顯示原因。</span>
                                {canTransfer ? (
                                  <button
                                    type="button"
                                    className="work-order-transfer-action"
                                    disabled={transferringId !== null || transferItemCount === 0}
                                    onClick={() => void handleTransfer(order)}
                                  >
                                    {transferringId === order.salesOrderId
                                      ? <><Loader2 className="work-order-transfer-spinner" size={15} /> 轉發中</>
                                      : <><Factory size={15} /> 核對完成，轉發工單</>}
                                  </button>
                                ) : <span className="work-order-transfer-no-permission">唯讀：您沒有轉發權限</span>}
                              </footer>
                            </section>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
};
