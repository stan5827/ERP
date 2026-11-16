import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, X } from 'lucide-react';
import {
  binLocationsApi,
  materialIssuesApi,
  warehousesApi,
} from '../api/inventory';
import { workOrdersApi } from '../api/mes';
import type {
  BinLocationDto,
  MaterialIssueDto,
  MaterialIssueItemDto,
  WarehouseDto,
} from '../api/inventory';

interface MaterialIssueWorkflowProps {
  workOrderId: string;
  workOrderNumber: string;
  plannedQty: number;
  productionUomCode?: string;
  onClose: () => void;
}

interface AllocationDraft {
  warehouseId: string;
  binLocationId: string;
  lotNumber: string;
  issuedQty: string;
}

const statusLabels: Record<string, string> = {
  draft: '草稿',
  issued: '已過帳',
  partially_issued: '部分發料',
  reversed: '已沖銷',
};

const defaultAllocation = (item: MaterialIssueItemDto): AllocationDraft => ({
  warehouseId: item.defaultWarehouseId ?? '',
  binLocationId: item.defaultBinLocationId ?? '',
  lotNumber: '',
  issuedQty: String(item.defaultIssueQty),
});

const inputStyle: React.CSSProperties = {
  minHeight: 36,
  padding: '6px 8px',
  borderRadius: 6,
  border: '1px solid rgba(148, 163, 184, 0.3)',
  background: '#111c31',
  color: '#f8fafc',
};

export const MaterialIssueWorkflow: React.FC<MaterialIssueWorkflowProps> = ({
  workOrderId,
  workOrderNumber,
  plannedQty,
  productionUomCode,
  onClose,
}) => {
  const queryClient = useQueryClient();
  const [selectedIssueId, setSelectedIssueId] = useState('');
  const [allocationDrafts, setAllocationDrafts] = useState<Record<string, AllocationDraft[]>>({});
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [recalculationQty, setRecalculationQty] = useState(String(plannedQty));

  const issuesQuery = useQuery({
    queryKey: ['material-issues', workOrderId],
    queryFn: async () => {
      const response = await materialIssuesApi.list(workOrderId);
      if (!response.data.success)
        throw new Error(response.data.message || '載入領料單失敗');
      return response.data.data ?? [];
    },
  });
  const activeIssueId = selectedIssueId || issuesQuery.data?.[0]?.id || '';
  const issueQuery = useQuery({
    queryKey: ['material-issue', activeIssueId],
    queryFn: async () => {
      const response = await materialIssuesApi.get(activeIssueId);
      if (!response.data.success || !response.data.data)
        throw new Error(response.data.message || '載入領料單明細失敗');
      return response.data.data;
    },
    enabled: Boolean(activeIssueId),
  });
  const warehousesQuery = useQuery({
    queryKey: ['material-issue-warehouses'],
    queryFn: async () => {
      const response = await warehousesApi.list();
      if (!response.data.success)
        throw new Error(response.data.message || '載入倉庫失敗');
      return (response.data.data ?? []).filter((warehouse: WarehouseDto) => warehouse.isActive);
    },
  });
  const binsQuery = useQuery({
    queryKey: ['material-issue-bin-locations'],
    queryFn: async () => {
      const response = await binLocationsApi.list();
      if (!response.data.success)
        throw new Error(response.data.message || '載入儲位失敗');
      return (response.data.data ?? []).filter((bin: BinLocationDto) => bin.isActive);
    },
  });

  const refreshIssues = async (issue?: MaterialIssueDto) => {
    if (issue)
      setSelectedIssueId(issue.id);
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['material-issues', workOrderId] }),
      queryClient.invalidateQueries({ queryKey: ['material-issue', issue?.id ?? selectedIssueId] }),
      queryClient.invalidateQueries({ queryKey: ['work-order', workOrderId] }),
    ]);
  };
  const refreshInventory = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['inventory-balance'] }),
      queryClient.invalidateQueries({ queryKey: ['inventory-atp'] }),
      queryClient.invalidateQueries({ queryKey: ['inventory-transactions'] }),
      queryClient.invalidateQueries({ queryKey: ['skus'] }),
    ]);
  };

  const createDraft = useMutation({
    mutationFn: async () => {
      const response = await materialIssuesApi.createDraft(workOrderId);
      if (!response.data.success || !response.data.data)
        throw new Error(response.data.message || '建立領料單草稿失敗');
      return response.data.data;
    },
    onSuccess: async (issue) => {
      setError('');
      setMessage('領料單草稿已建立。');
      await refreshIssues(issue);
    },
    onError: (reason: Error) => setError(reason.message),
  });
  const postIssue = useMutation({
    mutationFn: async (issue: MaterialIssueDto) => {
      const allocations = issue.items.flatMap(item =>
        (allocationDrafts[item.id] ?? [defaultAllocation(item)])
          .filter(row => Number(row.issuedQty) > 0)
          .map(row => ({
            issueItemId: item.id,
            warehouseId: row.warehouseId,
            binLocationId: row.binLocationId || undefined,
            lotNumber: row.lotNumber.trim() || undefined,
            issuedQty: Number(row.issuedQty),
          })),
      );
      if (allocations.some(allocation => !allocation.warehouseId))
        throw new Error('每筆實發分配都必須指定倉庫。');
      if (issue.items.some(item => item.serialControlled && Number(
        (allocationDrafts[item.id] ?? [defaultAllocation(item)]).reduce((sum, row) => sum + Number(row.issuedQty || 0), 0),
      ) > 0))
        throw new Error('序號管制品目前尚未支援此領料流程，請先完成序號追溯功能。');
      const response = await materialIssuesApi.post(issue.id, allocations);
      if (!response.data.success || !response.data.data)
        throw new Error(response.data.message || '領料單過帳失敗');
      return response.data.data;
    },
    onSuccess: async (issue) => {
      setError('');
      setMessage('領料單已過帳，庫存與工單實領數量已同步。');
      await Promise.all([refreshIssues(issue), refreshInventory()]);
    },
    onError: (reason: Error) => setError(reason.message),
  });
  const cancelDraft = useMutation({
    mutationFn: async (id: string) => {
      const response = await materialIssuesApi.cancel(id);
      if (!response.data.success || !response.data.data)
        throw new Error(response.data.message || '取消草稿失敗');
      return response.data.data;
    },
    onSuccess: async (issue) => {
      setMessage('領料單草稿已取消。');
      setError('');
      await refreshIssues(issue);
    },
    onError: (reason: Error) => setError(reason.message),
  });
  const recalculateMaterials = useMutation({
    mutationFn: async () => {
      const quantity = Number(recalculationQty);
      if (!Number.isFinite(quantity) || quantity <= 0)
        throw new Error('重算計畫量必須是大於零的數字。');
      if (!window.confirm(
        `將工單 ${workOrderNumber} 的計畫量設為 ${quantity} ${productionUomCode || ''}，依工單綁定 BOM 重算用料，取消未過帳草稿並建立新草稿。若已有報工或過帳領料，系統會拒絕。是否繼續？`,
      ))
        return false;
      const response = await workOrdersApi.recalculateMaterials([workOrderId], quantity);
      if (!response.data.success)
        throw new Error(response.data.message || '重算工單用料需求失敗');
      return true;
    },
    onSuccess: async (didRecalculate) => {
      if (!didRecalculate)
        return;
      setError('');
      setMessage('工單需求已重算，舊草稿已取消並建立新草稿；請核對數量、倉庫、儲位與批號後再過帳。');
      await queryClient.invalidateQueries({ queryKey: ['work-order', workOrderId] });
      const refreshed = await issuesQuery.refetch();
      const newDraft = refreshed.data?.find(candidate => candidate.status === 'draft');
      if (newDraft)
        setSelectedIssueId(newDraft.id);
      await refreshIssues(newDraft);
    },
    onError: (reason: Error) => setError(reason.message),
  });
  const reverseIssue = useMutation({
    mutationFn: async (id: string) => {
      const response = await materialIssuesApi.reverse(id);
      if (!response.data.success || !response.data.data)
        throw new Error(response.data.message || '領料單沖銷失敗');
      return response.data.data;
    },
    onSuccess: async (issue) => {
      setMessage('已建立反向庫存異動並沖回工單已領數量。');
      setError('');
      await Promise.all([refreshIssues(issue), refreshInventory()]);
    },
    onError: (reason: Error) => setError(reason.message),
  });
  const closeShortage = useMutation({
    mutationFn: async (input: { issue: MaterialIssueDto; item: MaterialIssueItemDto }) => {
      const quantity = window.prompt(`請輸入「${input.item.skuNumber}」的短少結案數量`, String(input.item.shortageQty));
      if (quantity === null)
        return null;
      const reason = window.prompt('請輸入短少結案原因');
      if (reason === null)
        return null;
      const response = await materialIssuesApi.closeShortage(
        input.issue.id,
        input.item.id,
        Number(quantity),
        reason,
      );
      if (!response.data.success || !response.data.data)
        throw new Error(response.data.message || '短少結案失敗');
      return response.data.data;
    },
    onSuccess: async (issue) => {
      if (!issue)
        return;
      setMessage('短少數量已結案，不再列入待領需求。');
      setError('');
      await refreshIssues(issue);
    },
    onError: (reason: Error) => setError(reason.message),
  });

  const issues = issuesQuery.data ?? [];
  const issue = issueQuery.data;
  const warehouses = warehousesQuery.data ?? [];
  const bins = binsQuery.data ?? [];

  const getAllocationDrafts = (item: MaterialIssueItemDto) =>
    allocationDrafts[item.id] ?? (issue?.status === 'draft' ? [defaultAllocation(item)] : []);
  const getInitialAllocations = (itemId: string) => {
    const item = issue?.items.find(candidate => candidate.id === itemId);
    return item ? [defaultAllocation(item)] : [];
  };

  const updateAllocation = (
    itemId: string,
    index: number,
    field: keyof AllocationDraft,
    value: string,
  ) => {
    setAllocationDrafts(current => ({
      ...current,
      [itemId]: (current[itemId] ?? getInitialAllocations(itemId)).map((row, rowIndex) =>
        rowIndex === index ? { ...row, [field]: value } : row),
    }));
  };

  const addAllocation = (item: MaterialIssueItemDto) => {
    setAllocationDrafts(current => ({
      ...current,
      [item.id]: [
        ...(current[item.id] ?? []),
        {
          warehouseId: item.defaultWarehouseId ?? '',
          binLocationId: '',
          lotNumber: '',
          issuedQty: '',
        },
      ],
    }));
  };

  const removeAllocation = (itemId: string, index: number) => {
    setAllocationDrafts(current => ({
      ...current,
      [itemId]: (current[itemId] ?? []).filter((_, rowIndex) => rowIndex !== index),
    }));
  };

  const isMutating = createDraft.isPending || postIssue.isPending
    || cancelDraft.isPending || reverseIssue.isPending || closeShortage.isPending
    || recalculateMaterials.isPending;
  const handleCreateDraft = () => {
    setError('');
    setMessage('');
    createDraft.mutate();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="工單領料單"
      onClick={onClose}
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 1200,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 20,
        background: 'rgba(0,0,0,0.82)',
      }}
    >
      <section
        onClick={event => event.stopPropagation()}
        style={{
          width: 1200,
          maxWidth: '96vw',
          maxHeight: 'calc(100vh - 40px)',
          boxSizing: 'border-box',
          overflowY: 'auto',
          padding: 20,
          color: '#e2e8f0',
          background: '#0b1426',
          border: '1px solid rgba(56, 189, 248, 0.35)',
          borderRadius: 12,
        }}
      >
        <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 16 }}>
          <div>
            <div style={{ color: '#38bdf8', fontSize: 12, textTransform: 'uppercase' }}>Material Issue</div>
            <h2 style={{ margin: '6px 0 16px', color: '#fff' }}>工單領料 - {workOrderNumber}</h2>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            {issuesQuery.isSuccess && issues.length === 0 && !issue && (
              <button
                type="button"
                className="btn-cy"
                disabled={isMutating}
                onClick={handleCreateDraft}
              >
                {createDraft.isPending ? '建立中...' : '建立領料單'}
              </button>
            )}
          <button
            type="button"
            aria-label="關閉"
            title="關閉"
            onClick={onClose}
            className="btn-cy"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 32,
              height: 32,
              minWidth: 32,
              minHeight: 32,
              padding: 0,
            }}
          >
            <X size={16} />
          </button>
          </div>
        </header>

        {(error || message) && (
          <div role={error ? 'alert' : 'status'} style={{
            padding: 10,
            marginBottom: 12,
            borderRadius: 6,
            color: error ? '#fecaca' : '#bbf7d0',
            background: error ? 'rgba(239,68,68,0.12)' : 'rgba(34,197,94,0.12)',
          }}>
            {error || message}
          </div>
        )}

        {issuesQuery.isError && <p role="alert">{(issuesQuery.error as Error).message}</p>}
        {warehousesQuery.isError && <p role="alert">{(warehousesQuery.error as Error).message}</p>}
        {binsQuery.isError && <p role="alert">{(binsQuery.error as Error).message}</p>}

        <div style={{
          display: 'flex',
          alignItems: 'end',
          flexWrap: 'wrap',
          gap: 8,
          marginBottom: 16,
          padding: 10,
          border: '1px solid rgba(245, 158, 11, 0.3)',
          borderRadius: 8,
        }}>
          <label style={{ display: 'flex', flexDirection: 'column', gap: 4, color: '#fbbf24' }}>
            重算計畫量（{productionUomCode || '生產單位'}）
            <input
              aria-label="重算計畫生產數量"
              type="number"
              min="0.01"
              step="0.01"
              value={recalculationQty}
              onChange={event => setRecalculationQty(event.target.value)}
              style={{ ...inputStyle, width: 150 }}
            />
          </label>
          <button
            type="button"
            className="btn-cy"
            disabled={isMutating}
            onClick={() => recalculateMaterials.mutate()}
          >
            {recalculateMaterials.isPending ? '重算中...' : '重算需求並重建草稿'}
          </button>
          <span style={{ color: '#94a3b8', fontSize: 12 }}>
            僅限尚未報工/領料的工單；系統依工單綁定 BOM 計算。
          </span>
        </div>

        {issuesQuery.isSuccess && issues.length === 0 && !issue && (
          <p role="status" style={{ color: '#94a3b8', margin: '0 0 14px' }}>
            此工單尚無領料單，請按上方「建立領料單」產生待領物料草稿。
          </p>
        )}

        <div style={{ marginBottom: 18 }}>
          <label style={{ display: 'block' }}>
            領料單
            <select
              value={activeIssueId}
              onChange={event => {
                setSelectedIssueId(event.target.value);
                setAllocationDrafts({});
                setError('');
              }}
              style={{ ...inputStyle, display: 'block', width: '100%', marginTop: 5, boxSizing: 'border-box' }}
            >
              {issues.length === 0 && <option value="">尚無領料單</option>}
              {issues.map(candidate => (
                <option key={candidate.id} value={candidate.id}>
                  {candidate.issueNumber} - {statusLabels[candidate.status] ?? candidate.status}
                </option>
              ))}
            </select>
          </label>
        </div>

        {issueQuery.isLoading && <p>載入領料單明細中...</p>}
        {issueQuery.isError && <p role="alert">{(issueQuery.error as Error).message}</p>}
        {!issue && !issueQuery.isLoading && !issueQuery.isError && issues.length > 0 && (
          <footer style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 18 }}>
            <button
              className="btn-cy"
              disabled={isMutating || issuesQuery.isLoading || issuesQuery.isError}
              onClick={handleCreateDraft}
            >
              {createDraft.isPending ? '建立中...' : '建立／續開領料草稿'}
            </button>
          </footer>
        )}
        {issue && (
          <>
            <div style={{ marginBottom: 12, color: '#94a3b8', fontSize: 13 }}>
              BOM 版本快照：{issue.bomVersion || '未記錄'} 狀態：{statusLabels[issue.status] ?? issue.status}
              {issue.issuedAt && ` 過帳時間：${new Date(issue.issuedAt).toLocaleString()}`}
            </div>
            <div style={{ overflowX: 'auto' }}>
              <table style={{
                width: '100%',
                minWidth: 1200,
                tableLayout: 'fixed',
                borderCollapse: 'collapse',
                fontSize: 13,
              }}>
                <colgroup>
                  <col style={{ width: '20%' }} />
                  <col style={{ width: '8%' }} />
                  <col style={{ width: '8%' }} />
                  <col style={{ width: '8%' }} />
                  <col style={{ width: '14%' }} />
                  <col style={{ width: '10%' }} />
                  <col style={{ width: '9%' }} />
                  <col style={{ width: '10%' }} />
                  <col style={{ width: '13%' }} />
                </colgroup>
                <thead>
                  <tr style={{ color: '#7dd3fc', textAlign: 'left', verticalAlign: 'bottom' }}>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>料號／品名</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>應發量</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>已發量</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>欠料</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>倉庫</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>儲位</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>批號</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap' }}>實領</th>
                    <th style={{ padding: 8, whiteSpace: 'nowrap', textAlign: 'center' }}>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {issue.items.map(item => (
                    <tr key={item.id} style={{
                      borderTop: '1px solid rgba(148,163,184,0.16)',
                      verticalAlign: 'middle',
                    }}>
                      <td style={{ padding: 8, overflowWrap: 'anywhere' }}>
                        <strong>{item.skuNumber}</strong><br />
                        <span style={{ color: '#94a3b8' }}>{item.skuName}</span>
                        <div style={{ color: '#64748b', marginTop: 4 }}>庫存：{item.onHandQty} {item.inventoryUnit}</div>
                      </td>
                      <td style={{ padding: 8, whiteSpace: 'nowrap' }}>{item.requiredQty} {item.unit}</td>
                      <td style={{ padding: 8, whiteSpace: 'nowrap' }}>{item.issuedQty} {item.unit}</td>
                      <td style={{ padding: 8, whiteSpace: 'nowrap' }}>{item.shortageQty} {item.unit}</td>
                      {issue.status === 'draft' ? (
                        <>
                          <td style={{ padding: 8 }}>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                              {getAllocationDrafts(item).map((row, index) => (
                                <select
                                  key={`${item.id}-warehouse-${index}`}
                                  aria-label={`${item.skuNumber} 倉庫`}
                                  value={row.warehouseId}
                                  onChange={event => {
                                    updateAllocation(item.id, index, 'warehouseId', event.target.value);
                                    updateAllocation(item.id, index, 'binLocationId', '');
                                  }}
                                  style={{ ...inputStyle, boxSizing: 'border-box', width: '100%', minWidth: 0 }}
                                >
                                  <option value="">選擇倉庫</option>
                                  {warehouses.map((warehouse: WarehouseDto) => (
                                    <option key={warehouse.id} value={warehouse.id}>
                                      {warehouse.warehouseCode} {warehouse.warehouseName}
                                    </option>
                                  ))}
                                </select>
                              ))}
                            </div>
                          </td>
                          <td style={{ padding: 8 }}>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                              {getAllocationDrafts(item).map((row, index) => (
                                <select
                                  key={`${item.id}-bin-${index}`}
                                  aria-label={`${item.skuNumber} 儲位`}
                                  value={row.binLocationId}
                                  onChange={event => updateAllocation(item.id, index, 'binLocationId', event.target.value)}
                                  style={{ ...inputStyle, boxSizing: 'border-box', width: '100%', minWidth: 0 }}
                                >
                                  <option value="">不指定儲位</option>
                                  {bins.filter((bin: BinLocationDto) => bin.warehouseId === row.warehouseId).map((bin: BinLocationDto) => (
                                    <option key={bin.id} value={bin.id}>{bin.locationCode}</option>
                                  ))}
                                </select>
                              ))}
                            </div>
                          </td>
                          <td style={{ padding: 8 }}>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                              {getAllocationDrafts(item).map((row, index) => (
                                <input
                                  key={`${item.id}-lot-${index}`}
                                  aria-label={`${item.skuNumber} 批號`}
                                  placeholder={item.lotControlled ? '必填批號' : '批號 (選填)'}
                                  value={row.lotNumber}
                                  onChange={event => updateAllocation(item.id, index, 'lotNumber', event.target.value)}
                                  style={{ ...inputStyle, boxSizing: 'border-box', width: '100%', minWidth: 0 }}
                                />
                              ))}
                            </div>
                          </td>
                          <td style={{ padding: 8 }}>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                              {getAllocationDrafts(item).map((row, index) => (
                                <div
                                  key={`${item.id}-qty-${index}`}
                                  style={{ display: 'flex', alignItems: 'center', gap: 6 }}
                                >
                                  <input
                                    aria-label={`${item.skuNumber} 實領量 (${item.inventoryUnit})`}
                                    type="number"
                                    min="0"
                                    step="0.01"
                                    value={row.issuedQty}
                                    onChange={event => updateAllocation(item.id, index, 'issuedQty', event.target.value)}
                                    style={{ ...inputStyle, boxSizing: 'border-box', width: '100%', minWidth: 0 }}
                                  />
                                  <span style={{ flexShrink: 0, color: '#94a3b8' }}>{item.inventoryUnit}</span>
                                </div>
                              ))}
                            </div>
                          </td>
                        </>
                      ) : (
                        <>
                          {[0, 1, 2, 3].map((columnIndex) => (
                            <td key={`${item.id}-posted-${columnIndex}`} style={{ padding: 8 }}>
                              <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                                {item.allocations.map(allocation => (
                                  <span key={`${allocation.id}-${columnIndex}`} style={{ color: '#cbd5e1', minHeight: 36, display: 'flex', alignItems: 'center' }}>
                                    {columnIndex === 0 ? allocation.warehouseCode
                                      : columnIndex === 1 ? allocation.binLocationCode || '未指定'
                                        : columnIndex === 2 ? allocation.lotNumber || '—'
                                          : `${allocation.issuedQty} ${item.inventoryUnit}`}
                                  </span>
                                ))}
                              </div>
                            </td>
                          ))}
                        </>
                      )}
                      <td style={{ padding: 8, verticalAlign: 'middle', textAlign: 'center' }}>
                        {issue.status === 'draft' ? (
                          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
                            <button
                              type="button"
                              aria-label={`${item.skuNumber} 新增分配`}
                              title="新增分配"
                              disabled={isMutating}
                              onClick={() => addAllocation(item)}
                              style={{
                                display: 'inline-flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                width: 36,
                                height: 36,
                                padding: 0,
                                border: '1px solid rgba(56, 189, 248, 0.35)',
                                borderRadius: 6,
                                background: 'rgba(14, 165, 233, 0.1)',
                                color: '#7dd3fc',
                                cursor: isMutating ? 'not-allowed' : 'pointer',
                              }}
                            >
                              <Plus size={17} />
                            </button>
                            {getAllocationDrafts(item).map((_, index) => (
                              <button
                                key={`${item.id}-remove-${index}`}
                                type="button"
                                aria-label={`${item.skuNumber} 移除第 ${index + 1} 筆分配`}
                                disabled={isMutating}
                                onClick={() => removeAllocation(item.id, index)}
                                style={{
                                  display: 'inline-flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  width: 36,
                                  height: 36,
                                  padding: 0,
                                  border: '1px solid rgba(248, 113, 113, 0.35)',
                                  borderRadius: 6,
                                  background: 'rgba(239, 68, 68, 0.1)',
                                  color: '#fca5a5',
                                  cursor: isMutating ? 'not-allowed' : 'pointer',
                                }}
                              >
                                <Trash2 size={16} />
                              </button>
                            ))}
                          </div>
                        ) : issue.status === 'partially_issued' && item.shortageQty > 0 ? (
                          <button
                            className="btn-cy"
                            disabled={isMutating}
                            onClick={() => closeShortage.mutate({ issue, item })}
                          >
                            短少結案
                          </button>
                        ) : null}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <footer style={{ display: 'flex', justifyContent: 'flex-end', flexWrap: 'wrap', gap: 10, marginTop: 18 }}>
              {issue.status !== 'draft' && (
                <button
                  className="btn-cy"
                  disabled={isMutating || issuesQuery.isLoading}
                  onClick={() => {
                    setError('');
                    setMessage('');
                    createDraft.mutate();
                  }}
                >
                  {createDraft.isPending ? '建立中...' : '建立／續開領料草稿'}
                </button>
              )}
              {issue.status === 'draft' && (
                <>
                  <button className="btn-cy" disabled={isMutating} onClick={() => postIssue.mutate(issue)}>
                    {postIssue.isPending ? '過帳中...' : '確認發料並過帳'}
                  </button>
                  <button
                    className="btn-cy"
                    disabled={isMutating}
                    onClick={() => {
                      if (window.confirm('確定取消此領料草稿？'))
                        cancelDraft.mutate(issue.id);
                    }}
                  >
                    取消草稿
                  </button>
                </>
              )}
            </footer>
            {(issue.status === 'issued' || issue.status === 'partially_issued')
              && issue.items.some(item => item.allocations.length > 0) && (
              <footer style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 18 }}>
                <button
                  className="btn-cy"
                  disabled={isMutating}
                  onClick={() => {
                    if (window.confirm('確定全額沖銷此領料單？系統會建立反向庫存異動。'))
                      reverseIssue.mutate(issue.id);
                  }}
                >
                  建立全額沖銷
                </button>
              </footer>
            )}
          </>
        )}
      </section>
    </div>
  );
};
