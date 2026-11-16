import { useCallback, useEffect, useRef, useState } from 'react';
import { fqcWorkflowApi } from '../api/mes';
import type {
  FqcBinLocationOptionDto,
  FqcInspectionLotDto,
  FqcWarehouseOptionDto,
  FqcWorkOrderOptionDto,
  ProductionReceiptDto,
} from '../api/mes';
import { LotQrCamera } from '../components/LotQrCamera';
import { useRole } from '../contexts/useRole';
import { useCurrentUser } from '../hooks/useApi';
import { parseLotQr } from '../utils/lotQr';
import { formatWorkOrderOption, formatWorkOrderStatus } from '../utils/workOrderDisplay';

const getMessage = (error: unknown) =>
  (error as { response?: { data?: { message?: string } }; message?: string }).response?.data?.message ??
  (error as { message?: string }).message ??
  '操作失敗，請稍後再試';

const inspectionStatusLabels: Record<string, string> = {
  pending: '待判定',
  passed: '合格',
  failed: '不合格',
  concession: '特採',
};

const dispositionStatusLabels: Record<string, string> = {
  pending: '待處置',
  released: '已放行',
  concession: '特採放行',
  rework: '返修處理中',
  scrappendingqualityreview: '報廢待品保判定',
  scrappendingfinancereview: '報廢待財務核定',
  scrapreturned: '報廢已退回申請人',
  scrapapprovedpendingexecution: '報廢已核定待實體處理',
  reinspectionpending: '返修待複檢',
  reworkcompleted: '返修已結案',
};

const getStatusLabel = (status: string, labels: Record<string, string>) =>
  labels[status.trim().toLowerCase()] ?? status;

export function FqcWorkflowPanel() {
  const { hasPermission } = useRole();
  const currentUser = useCurrentUser();
  const canCreateFqc = hasPermission('quality', 'create');
  const canApproveFqc = hasPermission('quality', 'approve');
  const canApproveFinance = hasPermission('finance', 'approve');
  const canCloseRework = hasPermission('manufacturing', 'approve');
  const canReceiveFqc = hasPermission('inventory', 'execute');
  const [workOrders, setWorkOrders] = useState<FqcWorkOrderOptionDto[]>([]);
  const [workOrdersLoading, setWorkOrdersLoading] = useState(true);
  const [workOrdersError, setWorkOrdersError] = useState('');
  const [workOrderSearch, setWorkOrderSearch] = useState('');
  const [workOrderId, setWorkOrderId] = useState('');
  const [defaultLotLoading, setDefaultLotLoading] = useState(false);
  const [defaultLotMessage, setDefaultLotMessage] = useState('');
  const [defaultLotError, setDefaultLotError] = useState('');
  const defaultLotRequestId = useRef(0);
  const [lots, setLots] = useState<FqcInspectionLotDto[]>([]);
  const [receipts, setReceipts] = useState<ProductionReceiptDto[]>([]);
  const [warehouses, setWarehouses] = useState<FqcWarehouseOptionDto[]>([]);
  const [warehouseLoading, setWarehouseLoading] = useState(true);
  const [warehouseError, setWarehouseError] = useState('');
  const [bins, setBins] = useState<FqcBinLocationOptionDto[]>([]);
  const [binsLoading, setBinsLoading] = useState(false);
  const [binsError, setBinsError] = useState('');
  const [lotNumber, setLotNumber] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [manualQr, setManualQr] = useState('');
  const [scannedSkuCode, setScannedSkuCode] = useState('');
  const [sampledQty, setSampledQty] = useState('1');
  const [defectiveQty, setDefectiveQty] = useState('0');
  const [criticalDefectFound, setCriticalDefectFound] = useState(false);
  const [defectCode, setDefectCode] = useState('');
  const [inspectorName, setInspectorName] = useState(
    () => currentUser?.employeeName?.trim() || currentUser?.username || '',
  );
  const [decisionBy, setDecisionBy] = useState(
    () => currentUser?.employeeName?.trim() || currentUser?.username || '',
  );
  const [approvedQty, setApprovedQty] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [receiptWarehouseId, setReceiptWarehouseId] = useState('');
  const [binLocationId, setBinLocationId] = useState('');
  const [receiptQty, setReceiptQty] = useState('');
  const [receiptLotId, setReceiptLotId] = useState('');
  const [receiptError, setReceiptError] = useState('');
  const [selectedLotId, setSelectedLotId] = useState('');
  const [scrapReasonCode, setScrapReasonCode] = useState('03');
  const [scrapReason, setScrapReason] = useState('');
  const [scrapReviewComments, setScrapReviewComments] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const selectedLot = lots.find(lot => lot.id === selectedLotId);
  const selectedWorkOrder = workOrders.find(order => order.id === workOrderId);
  const receivableLots = lots.filter(lot =>
    ['Passed', 'Concession'].includes(lot.inspectionStatus) && lot.availableReceiptQty > 0
  );
  const selectedReceiptLot = receivableLots.find(lot => lot.id === receiptLotId);
  const parsedReceiptQty = Number(receiptQty);
  const validReceiptQty = Number.isFinite(parsedReceiptQty)
    && parsedReceiptQty > 0
    && !!selectedReceiptLot
    && parsedReceiptQty <= selectedReceiptLot.availableReceiptQty;

  const selectWorkOrder = useCallback(async (id: string) => {
    setWorkOrderId(id);
    setLotNumber('');
    setDefaultLotMessage('');
    setDefaultLotError('');
    const requestId = ++defaultLotRequestId.current;
    if (!id) {
      setDefaultLotLoading(false);
      return;
    }

    setDefaultLotLoading(true);
    try {
      const response = await fqcWorkflowApi.defaultFinishedLot(id);
      if (requestId !== defaultLotRequestId.current)
        return;
      const defaultLot = response.data?.data;
      if (defaultLot) {
        setLotNumber(defaultLot.lotNumber);
        setDefaultLotMessage(`已帶入最近一筆最後工序核准批號；報工良品數 ${defaultLot.producedQty}。`);
      } else {
        setDefaultLotMessage('查無尚未建立 FQC 的最後工序核准批號；請先完成最後工序報工，或依現場批號規則手動確認輸入。');
      }
    } catch (loadError) {
      if (requestId === defaultLotRequestId.current)
        setDefaultLotError(`無法查詢工單成品批號：${getMessage(loadError)}`);
    } finally {
      if (requestId === defaultLotRequestId.current)
        setDefaultLotLoading(false);
    }
  }, []);

  const handleLotScan = useCallback((value: string) => {
    defaultLotRequestId.current += 1;
    setDefaultLotLoading(false);
    setDefaultLotMessage('');
    setDefaultLotError('');
    setManualQr(value);
    setError('');
    try {
      const scanned = parseLotQr(value);
      setLotNumber(scanned.lotNumber);
      setExpiryDate(scanned.expiryDate);
      setScannedSkuCode(scanned.skuCode);
      const matchingOrder = workOrders.find(
        order => order.productSkuNumber?.toLowerCase() === scanned.skuCode.toLowerCase()
      );
      if (matchingOrder) setWorkOrderId(matchingOrder.id);
      else {
        setWorkOrderSearch(scanned.skuCode);
        setWorkOrderId('');
        setError(`已讀取料號 ${scanned.skuCode}，請從搜尋結果選擇工單。`);
      }
    } catch (scanError) {
      setLotNumber('');
      setExpiryDate('');
      setScannedSkuCode('');
      setError(scanError instanceof Error ? scanError.message : 'QR 格式錯誤');
    }
  }, [workOrders]);

  useEffect(() => {
    let cancelled = false;
    const timer = window.setTimeout(() => {
      setWorkOrdersLoading(true);
      setWorkOrdersError('');
      void fqcWorkflowApi.workOrders(workOrderSearch)
        .then(response => {
          if (!cancelled)
            setWorkOrders(response.data?.data ?? []);
        })
        .catch(loadError => {
          if (!cancelled) {
            setWorkOrders([]);
            setWorkOrdersError(getMessage(loadError));
          }
        })
        .finally(() => {
          if (!cancelled)
            setWorkOrdersLoading(false);
        });
    }, 250);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [workOrderSearch]);

  const refresh = useCallback(async (id: string) => {
    if (!id) {
      setLots([]);
      setReceipts([]);
      return;
    }
    const [lotResponse, receiptResponse] = await Promise.all([
      fqcWorkflowApi.list({ workOrderId: id }),
      fqcWorkflowApi.listReceipts({ workOrderId: id }),
    ]);
    const nextLots = lotResponse.data?.data ?? [];
    setLots(nextLots);
    setReceipts(receiptResponse.data?.data ?? []);
    const nextReceivableLots = nextLots.filter(lot =>
      ['Passed', 'Concession'].includes(lot.inspectionStatus) && lot.availableReceiptQty > 0
    );
    setSelectedLotId(currentId =>
      nextLots.some(lot => lot.id === currentId) ? currentId : nextLots[0]?.id ?? ''
    );
    setReceiptLotId(currentId =>
      nextReceivableLots.some(lot => lot.id === currentId) ? currentId : nextReceivableLots[0]?.id ?? ''
    );
  }, []);

  useEffect(() => {
    let cancelled = false;
    setWarehouseLoading(true);
    setWarehouseError('');
    void fqcWorkflowApi.warehouses()
      .then(response => {
        if (!cancelled)
          setWarehouses(response.data?.data ?? []);
      })
      .catch(loadError => {
        if (!cancelled) {
          setWarehouses([]);
          setWarehouseError(getMessage(loadError));
        }
      })
      .finally(() => {
        if (!cancelled)
          setWarehouseLoading(false);
      });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    setError('');
    void refresh(workOrderId).catch(loadError => setError(getMessage(loadError)));
  }, [refresh, workOrderId]);

  useEffect(() => {
    if (selectedReceiptLot)
      setReceiptQty(String(selectedReceiptLot.availableReceiptQty));
  }, [selectedReceiptLot?.id, selectedReceiptLot?.availableReceiptQty]);

  useEffect(() => {
    setBins([]);
    setBinLocationId('');
    setBinsError('');
    if (!receiptWarehouseId) {
      setBinsLoading(false);
      return;
    }
    let cancelled = false;
    setBinsLoading(true);
    void fqcWorkflowApi.binLocations(receiptWarehouseId)
      .then(response => {
        if (!cancelled)
          setBins(response.data?.data ?? []);
      })
      .catch(loadError => {
        if (!cancelled) {
          setBins([]);
          setBinsError(getMessage(loadError));
        }
      })
      .finally(() => {
        if (!cancelled)
          setBinsLoading(false);
      });
    return () => { cancelled = true; };
  }, [receiptWarehouseId]);

  const createFqcLot = async () => {
    if (!workOrderId || !lotNumber.trim() || !inspectorName.trim()) {
      setError('請選擇工單並填寫成品批號與檢驗人員。');
      return;
    }
    const selectedWorkOrder = workOrders.find(order => order.id === workOrderId);
    if (scannedSkuCode && selectedWorkOrder?.productSkuNumber?.toLowerCase() !== scannedSkuCode.toLowerCase()) {
      setError('所選工單料號與 QR 料號不符，請重新掃描或選擇正確工單。');
      return;
    }
    if ((Number(defectiveQty) > 0 || criticalDefectFound) && !defectCode.trim()) {
      setError('有抽樣不良或重大缺點時，請填寫主要不良代碼。');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await fqcWorkflowApi.create({
        workOrderId,
        lotNumber: lotNumber.trim(),
        expiryDate: expiryDate || undefined,
        sampledQty: Number(sampledQty),
        defectiveQty: Number(defectiveQty),
        criticalDefectFound,
        defectCode: defectCode.trim() || undefined,
        inspectorName: inspectorName.trim(),
      });
      setLotNumber('');
      setDefectiveQty('0');
      setCriticalDefectFound(false);
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  const decideLot = async (status: 'Passed' | 'Failed' | 'Concession') => {
    if (!selectedLot || !decisionBy.trim()) {
      setError('請先選擇 FQC 批次並填寫整批判定人員。');
      return;
    }
    const quarantineQty = selectedLot.producedQty - (status === 'Failed' ? 0 : Number(approvedQty));
    if (quarantineQty > 0 && !warehouseId) {
      setError('有需隔離數量時，請選擇隔離倉庫。');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await fqcWorkflowApi.decide(selectedLot.id, {
        inspectionStatus: status,
        approvedQty: status === 'Failed' ? 0 : Number(approvedQty),
        warehouseId,
        decisionBy: decisionBy.trim(),
      });
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  const createReceipt = async () => {
    if (!selectedReceiptLot) {
      setReceiptError('請選擇有剩餘可入庫數量的合格或特採放行批次。');
      return;
    }
    if (!Number.isFinite(parsedReceiptQty) || parsedReceiptQty <= 0) {
      setReceiptError('本次入庫數量必須大於 0。');
      return;
    }
    if (parsedReceiptQty > selectedReceiptLot.availableReceiptQty) {
      setReceiptError(`本次入庫數量不可超過可入庫數量 ${selectedReceiptLot.availableReceiptQty}。`);
      return;
    }
    if (!receiptWarehouseId || !binLocationId) {
      setReceiptError('請先選擇成品倉庫與成品儲位。');
      return;
    }
    setBusy(true);
    setReceiptError('');
    setNotice('');
    try {
      const response = await fqcWorkflowApi.createReceipt({
        fqcInspectionLotId: selectedReceiptLot.id,
        quantity: parsedReceiptQty,
        warehouseId: receiptWarehouseId,
        binLocationId,
      });
      const receipt = response.data?.data;
      setNotice(receipt
        ? `完工入庫已過帳：入庫單 ${receipt.receiptNumber}，數量 ${receipt.quantity}。`
        : '完工入庫已過帳。');
      setReceiptQty('');
      await refresh(workOrderId);
    } catch (saveError) {
      setReceiptError(`完工入庫失敗：${getMessage(saveError)}`);
    } finally {
      setBusy(false);
    }
  };

  const createReworkWorkOrder = async () => {
    if (!selectedLot) return;
    setBusy(true);
    setError('');
    try {
      await fqcWorkflowApi.createReworkWorkOrder(selectedLot.id);
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  const createScrapNote = async () => {
    if (!selectedLot || !scrapReason.trim() || !scrapReasonCode) {
      setError('請先選擇 FQC 批次、報廢原因分類並填寫原因。');
      return;
    }
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const response = await fqcWorkflowApi.createScrapNote(selectedLot.id, scrapReasonCode, scrapReason.trim());
      const scrapNote = response.data?.data;
      setNotice(scrapNote
        ? `報廢單 ${scrapNote.scrapNumber} 已建立，數量 ${scrapNote.quantity}；等待品保判定。`
        : '報廢申請已建立，等待品保判定。');
      setScrapReason('');
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  const resubmitScrapNote = async () => {
    const scrapNote = selectedLot?.scrapNote;
    if (!scrapNote || !scrapReason.trim()) {
      setError('請填寫補充原因後重新送審。');
      return;
    }
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await fqcWorkflowApi.resubmitScrapNote(scrapNote.id, scrapReasonCode, scrapReason.trim());
      setNotice('報廢申請已重新送交品保判定。');
      setScrapReason('');
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  const reviewScrapNote = async (stage: 'quality-review' | 'finance-approval', approve: boolean) => {
    const scrapNote = selectedLot?.scrapNote;
    if (!scrapNote) return;
    if (!approve && !scrapReviewComments.trim()) {
      setError('退回報廢申請時必須填寫原因。');
      return;
    }
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await fqcWorkflowApi.reviewScrapNote(
        scrapNote.id, stage, approve, scrapReviewComments.trim() || undefined,
      );
      setNotice(approve
        ? stage === 'quality-review' ? '品保已完成判定，送交財務核定。' : '財務已核定；庫存與會計帳務未自動異動。'
        : '報廢申請已退回原申請人。');
      setScrapReviewComments('');
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  const closeReworkWorkOrder = async () => {
    if (!selectedLot) return;
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await fqcWorkflowApi.closeReworkWorkOrder(selectedLot.workOrderId);
      setNotice('返修工單已結案。');
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div style={{ display: 'grid', gap: 16 }}>
      {error && <div className="glass-card" role="alert" style={{ padding: 12, color: 'var(--crimson-300)' }}>{error}</div>}
      {notice && <div className="glass-card" role="status" style={{ padding: 12, color: 'var(--emerald-300)' }}>{notice}</div>}
      <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>查詢工單 FQC 批次</h3>
        <div className="inspection-template-grid">
          <label>搜尋工單／料號／品名<input className="form-input" value={workOrderSearch} onChange={event => { defaultLotRequestId.current += 1; setWorkOrderSearch(event.target.value); setWorkOrderId(''); setLotNumber(''); setDefaultLotMessage(''); setDefaultLotError(''); }} placeholder="輸入工單編號、成品料號或品名" /></label>
          <label style={{ gridColumn: '1 / -1' }}>工單<select className="form-input" value={workOrderId} onChange={event => void selectWorkOrder(event.target.value)} disabled={workOrdersLoading || !!workOrdersError}><option value="">{workOrdersLoading ? '載入工單中…' : '選擇工單'}</option>{workOrders.map(order => <option key={order.id} value={order.id}>{formatWorkOrderOption(order)}</option>)}</select></label>
        </div>
        {selectedWorkOrder && (
          <div className="glass-card" role="status" style={{ display: 'flex', flexWrap: 'wrap', gap: '8px 20px', padding: 12, color: 'var(--slate-300)' }}>
            <span>工單：{selectedWorkOrder.woNumber}</span>
            <span>成品料號：{selectedWorkOrder.productSkuNumber || '未提供'}</span>
            <span>品名：{selectedWorkOrder.productSkuName || '未提供'}</span>
            <span>狀態：{formatWorkOrderStatus(selectedWorkOrder.status)}</span>
          </div>
        )}
        {workOrdersError && <p role="alert" style={{ color: 'var(--crimson-300)' }}>無法載入工單：{workOrdersError}</p>}
        {!workOrdersLoading && !workOrdersError && workOrders.length === 0 && <p role="status" style={{ color: 'var(--slate-400)' }}>找不到符合條件的工單，請確認搜尋內容或工單資料。</p>}
      </div>

      {canCreateFqc && <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <div>
          <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>建立 FQC 整批檢驗</h3>
          <p style={{ color: 'var(--slate-400)', fontSize: 12, marginTop: 5 }}>IPQC 工序抽樣不會自動放行整批；成品批號需先在工單最後工序報工。</p>
        </div>
        <LotQrCamera onScan={handleLotScan} />
        <div style={{ display: 'flex', gap: 8 }}>
          <input className="form-input" aria-label="FQC QR 內容" placeholder="或輸入 QR：料號|批號|YYYY-MM-DD" value={manualQr} onChange={event => setManualQr(event.target.value)} />
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => handleLotScan(manualQr)}>解析 QR</button>
        </div>
        <div className="inspection-template-grid">
          {scannedSkuCode && <p>QR 料號：{scannedSkuCode}</p>}
          <label>成品批號<input className="form-input" maxLength={50} value={lotNumber} onChange={event => { defaultLotRequestId.current += 1; setLotNumber(event.target.value); setDefaultLotMessage(''); setDefaultLotError(''); }} />{defaultLotLoading && <small>查詢最後工序已核准批號中…</small>}{defaultLotMessage && <small role="status">{defaultLotMessage}</small>}{defaultLotError && <small role="alert" style={{ color: 'var(--crimson-300)' }}>{defaultLotError}</small>}</label>
          <label>效期<input className="form-input" type="date" value={expiryDate} onChange={event => setExpiryDate(event.target.value)} /></label>
          <label>抽樣數量<input className="form-input" type="number" min="0.01" step="0.01" value={sampledQty} onChange={event => setSampledQty(event.target.value)} /></label>
          <label>抽樣不良數<input className="form-input" type="number" min="0" step="0.01" value={defectiveQty} onChange={event => setDefectiveQty(event.target.value)} /></label>
          <label>主要不良代碼<input className="form-input" maxLength={50} aria-describedby="fqc-defect-code-help" value={defectCode} onChange={event => setDefectCode(event.target.value)} placeholder="依公司不良代碼表填寫" /><small id="fqc-defect-code-help">填寫公司核定的不良代碼，不是自由描述。例：若代碼表將「外觀刮傷」編為 VIS-SCRATCH，請填 VIS-SCRATCH；此代碼僅為格式示例，請以公司代碼表為準。常見不良類別包括外觀刮傷、尺寸超差、污染。</small></label>
          <label>檢驗人員<input className="form-input" maxLength={100} value={inspectorName} onChange={event => setInspectorName(event.target.value)} /><small>預設帶入登入帳號姓名；如由他人代檢，請改填實際執行檢驗者。</small></label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}><input type="checkbox" checked={criticalDefectFound} onChange={event => setCriticalDefectFound(event.target.checked)} />發現重大缺點（強制 Failed）</label>
        </div>
        <div><button className="btn btn-primary btn-sm" disabled={busy} onClick={() => void createFqcLot()}>{busy ? '處理中…' : '建立待判定 FQC 批次'}</button></div>
      </div>}

      {canApproveFqc && <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>整批判定與放行數量</h3>
        <div className="inspection-template-grid">
          <label>待判定批次<select className="form-input" value={selectedLotId} onChange={event => { const lot = lots.find(item => item.id === event.target.value); setSelectedLotId(event.target.value); setApprovedQty(lot ? String(Math.max(0, lot.producedQty - lot.defectiveQty)) : ''); }}><option value="">選擇 FQC 批次</option>{lots.map(lot => <option key={lot.id} value={lot.id}>{lot.inspectionNumber} · {lot.lotNumber} · {getStatusLabel(lot.inspectionStatus, inspectionStatusLabels)}</option>)}</select></label>
          <label>核准良品數<input className="form-input" type="number" min="0" step="0.01" value={approvedQty} onChange={event => setApprovedQty(event.target.value)} disabled={!selectedLot || selectedLot.inspectionStatus !== 'Pending'} /></label>
          <label>隔離／入庫倉庫<select className="form-input" value={warehouseId} onChange={event => setWarehouseId(event.target.value)} disabled={warehouseLoading || !!warehouseError}><option value="">{warehouseLoading ? '載入倉庫中…' : '選擇倉庫'}</option>{warehouses.map(warehouse => <option key={warehouse.id} value={warehouse.id}>{warehouse.warehouseCode} - {warehouse.warehouseName}</option>)}</select></label>
          <label>整批判定人員<input className="form-input" maxLength={100} value={decisionBy} onChange={event => setDecisionBy(event.target.value)} /></label>
        </div>
        {warehouseError && <p role="alert" style={{ color: 'var(--crimson-300)' }}>無法載入倉庫：{warehouseError}</p>}
        {!warehouseLoading && !warehouseError && warehouses.length === 0 && <p role="status" style={{ color: 'var(--slate-400)' }}>目前沒有啟用中的倉庫可供選擇。</p>}
        {selectedLot && (
          <div style={{ color: 'var(--slate-300)', fontSize: 12 }}>
            批號報工良品 {selectedLot.producedQty} · 抽樣 {selectedLot.sampledQty} · 抽樣不良 {selectedLot.defectiveQty} · 已入庫 {selectedLot.receivedQty} · 可入庫 {selectedLot.availableReceiptQty}
          </div>
        )}
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <button className="btn btn-primary btn-sm" disabled={busy || !selectedLot || selectedLot.inspectionStatus !== 'Pending' || selectedLot.criticalDefectFound} onClick={() => void decideLot('Passed')}>整批合格放行</button>
          <button className="btn btn-ghost btn-sm" disabled={busy || !selectedLot || selectedLot.inspectionStatus !== 'Pending'} onClick={() => void decideLot('Failed')}>整批不合格並隔離</button>
          <button className="btn btn-ghost btn-sm" disabled={busy || !selectedLot || selectedLot.inspectionStatus !== 'Pending' || selectedLot.criticalDefectFound} onClick={() => void decideLot('Concession')}>特採放行</button>
        </div>
      </div>}

      {canReceiveFqc && <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>FQC 放行批次完工入庫</h3>
        <p style={{ color: 'var(--slate-400)', fontSize: 12, margin: 0 }}>由倉管人員選取已合格或特採放行的 FQC 批次，指定成品儲位並輸入本次實際入庫量；系統會以登入帳號記錄入庫人，送出後即過帳。</p>
        <div className="inspection-template-grid">
          <label>可入庫 FQC 批次<select className="form-input" value={receiptLotId} onChange={event => {
            const lot = receivableLots.find(item => item.id === event.target.value);
            setReceiptLotId(event.target.value);
            setReceiptQty(lot ? String(lot.availableReceiptQty) : '');
            setReceiptError('');
          }} disabled={receivableLots.length === 0}><option value="">選擇已放行且有餘額的批次</option>{receivableLots.map(lot => <option key={lot.id} value={lot.id}>{lot.inspectionNumber} · {lot.workOrderNumber} · {lot.lotNumber} · 可入庫 {lot.availableReceiptQty}</option>)}</select></label>
          <label>成品倉庫<select className="form-input" value={receiptWarehouseId} onChange={event => { setReceiptWarehouseId(event.target.value); setBinLocationId(''); }} disabled={warehouseLoading || !!warehouseError}><option value="">{warehouseLoading ? '載入倉庫中…' : '選擇成品倉庫'}</option>{warehouses.map(warehouse => <option key={warehouse.id} value={warehouse.id}>{warehouse.warehouseCode} - {warehouse.warehouseName}</option>)}</select></label>
          <label>成品儲位<select className="form-input" value={binLocationId} onChange={event => setBinLocationId(event.target.value)} disabled={!receiptWarehouseId || binsLoading || !!binsError}><option value="">{binsLoading ? '載入儲位中…' : '選擇良品儲位'}</option>{bins.map(bin => <option key={bin.id} value={bin.id}>{bin.locationCode}</option>)}</select></label>
          <label>本次入庫數量<input className="form-input" type="number" min="0.01" step="0.01" max={selectedReceiptLot?.availableReceiptQty} value={receiptQty} onChange={event => { setReceiptQty(event.target.value); setReceiptError(''); }} disabled={!selectedReceiptLot} /><small>{selectedReceiptLot ? `尚可入庫 ${selectedReceiptLot.availableReceiptQty}；可分次入庫。` : '請先選擇有可入庫餘額的 FQC 批次。'}</small></label>
        </div>
        {receivableLots.length === 0 && <p role="status" style={{ color: 'var(--slate-400)' }}>目前沒有已合格／特採放行且尚有可入庫數量的 FQC 批次。</p>}
        {warehouseError && <p role="alert" style={{ color: 'var(--crimson-300)' }}>無法載入成品倉庫：{warehouseError}</p>}
        {!warehouseLoading && !warehouseError && warehouses.length === 0 && <p role="status" style={{ color: 'var(--slate-400)' }}>目前沒有啟用中的成品倉庫可供選擇。</p>}
        {binsError && <p role="alert" style={{ color: 'var(--crimson-300)' }}>無法載入成品儲位：{binsError}</p>}
        {receiptWarehouseId && !binsLoading && !binsError && bins.length === 0 && <p role="status" style={{ color: 'var(--slate-400)' }}>所選成品倉庫目前沒有可用的良品儲位。</p>}
        {receiptError && <p role="alert" style={{ color: 'var(--crimson-300)', margin: 0 }}>{receiptError}</p>}
        <div><button className="btn btn-primary btn-sm" disabled={busy || !validReceiptQty || !receiptWarehouseId || !binLocationId || binsLoading} onClick={() => void createReceipt()}>{busy ? '入庫過帳中…' : '確認完工入庫'}</button></div>
        <div style={{ overflowX: 'auto' }}>
          <table className="data-table">
            <thead><tr><th>入庫單</th><th>FQC 單號</th><th>批號</th><th>數量</th><th>金額</th><th>過帳時間</th></tr></thead>
            <tbody>{receipts.map(receipt => <tr key={receipt.id}><td className="mono">{receipt.receiptNumber}</td><td>{receipt.fqcInspectionNumber}</td><td>{receipt.lotNumber}</td><td>{receipt.quantity}</td><td>{receipt.totalCost.toLocaleString()}</td><td>{new Date(receipt.receivedAt).toLocaleString()}</td></tr>)}
              {receipts.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center', padding: 20, color: 'var(--slate-500)' }}>尚無完工入庫紀錄</td></tr>}</tbody>
          </table>
        </div>
      </div>}

      <div className="glass-card" style={{ overflowX: 'auto' }}>
        {selectedLot && (
          <div style={{ display: 'grid', gap: 10, padding: 14 }}>
            {selectedLot.quarantinedQty > 0 && <span style={{ color: 'var(--slate-300)', fontSize: 12 }}>
              隔離數量 {selectedLot.quarantinedQty}；返修工單會沿用原工序，隔離庫存仍維持鎖定。
              {selectedLot.reworkWorkOrderId && ` 返修工單：${selectedLot.reworkWorkOrderId}`}
            </span>}
            {selectedLot.quarantinedQty > 0 && <div className="inspection-template-grid">
              {canApproveFqc && !selectedLot.reworkSourceInspectionLotId && !selectedLot.reworkWorkOrderId && !selectedLot.scrapNote && (
                <button className="btn btn-ghost btn-sm" disabled={busy || selectedLot.inspectionStatus === 'Pending'} onClick={() => void createReworkWorkOrder()}>
                  建立返修工單
                </button>
              )}
              {canCreateFqc && !selectedLot.scrapNote && !selectedLot.reworkWorkOrderId && selectedLot.inspectionStatus !== 'Pending' && (
                <>
                  <label>報廢原因分類<select className="form-input" value={scrapReasonCode} onChange={event => setScrapReasonCode(event.target.value)}>
                    <option value="01">庫存過期</option><option value="02">製程損毀</option><option value="03">生產品質異常</option><option value="04">ECN 設變淘汰</option><option value="05">呆滯物料</option>
                  </select></label>
                  <label>報廢原因<input className="form-input" maxLength={500} value={scrapReason} onChange={event => setScrapReason(event.target.value)} /></label>
                  <button className="btn btn-ghost btn-sm" disabled={busy} onClick={() => void createScrapNote()}>送交品保判定</button>
                </>
              )}
            </div>}
            {selectedLot.scrapNote && <div style={{ display: 'grid', gap: 8, color: 'var(--slate-300)', fontSize: 12 }}>
              <strong>報廢單 {selectedLot.scrapNote.scrapNumber} · {getStatusLabel(selectedLot.scrapNote.status, {
                pending_quality_review: '待品保判定',
                pending_finance_review: '待財務核定',
                returned_to_requester: '退回申請人',
                approved_pending_disposal: '已核定待實體處理',
              })} · 數量 {selectedLot.scrapNote.quantity}</strong>
              <span>原因：{selectedLot.scrapNote.reasonCode} / {selectedLot.scrapNote.reason}</span>
              {selectedLot.scrapNote.approvalEvents.map(event => (
                <span key={event.id}>{event.occurredAt ? new Date(event.occurredAt).toLocaleString() : ''} · {event.actorName} · {event.stage} / {event.decision}{event.comments ? ` · ${event.comments}` : ''}</span>
              ))}
              <label>簽核／退回意見<input className="form-input" maxLength={1000} value={scrapReviewComments} onChange={event => setScrapReviewComments(event.target.value)} /></label>
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                {canApproveFqc && selectedLot.scrapNote.status === 'pending_quality_review' && <>
                  <button className="btn btn-primary btn-sm" disabled={busy} onClick={() => void reviewScrapNote('quality-review', true)}>品保判定通過</button>
                  <button className="btn btn-ghost btn-sm" disabled={busy} onClick={() => void reviewScrapNote('quality-review', false)}>退回申請人</button>
                </>}
                {canApproveFinance && selectedLot.scrapNote.status === 'pending_finance_review' && <>
                  <button className="btn btn-primary btn-sm" disabled={busy} onClick={() => void reviewScrapNote('finance-approval', true)}>財務核定</button>
                  <button className="btn btn-ghost btn-sm" disabled={busy} onClick={() => void reviewScrapNote('finance-approval', false)}>退回申請人</button>
                </>}
                {canCreateFqc && currentUser?.userId === selectedLot.scrapNote.requestedByUserId
                  && selectedLot.scrapNote.status === 'returned_to_requester' && <>
                    <label>補充報廢原因<input className="form-input" maxLength={500} value={scrapReason} onChange={event => setScrapReason(event.target.value)} /></label>
                    <button className="btn btn-ghost btn-sm" disabled={busy} onClick={() => void resubmitScrapNote()}>原申請人重新送審</button>
                  </>}
              </div>
            </div>}
            {canCloseRework && selectedLot.reworkSourceInspectionLotId
              && selectedLot.workOrderStatus === 'pending_fqc'
              && selectedLot.inspectionStatus !== 'Pending' && (
                <button className="btn btn-primary btn-sm" disabled={busy} onClick={() => void closeReworkWorkOrder()}>
                  檢查複檢、入庫及報廢核定數量後結案
                </button>
              )}
          </div>
        )}
        <table className="data-table">
          <thead><tr><th>FQC 批次</th><th>工單 / 成品批號</th><th>報工量</th><th>抽樣 / 不良</th><th>核准 / 已入庫</th><th>判定 / 隔離</th><th>處置狀態</th></tr></thead>
          <tbody>{lots.map(lot => <tr key={lot.id} onClick={() => { setSelectedLotId(lot.id); setApprovedQty(String(Math.max(0, lot.producedQty - lot.defectiveQty))); }} style={{ cursor: 'pointer' }}>
            <td className="mono">{lot.inspectionNumber}</td><td>{lot.workOrderNumber} / {lot.lotNumber}</td><td>{lot.producedQty}</td>
            <td>{lot.sampledQty} / {lot.defectiveQty}{lot.criticalDefectFound ? ' · 重大缺點' : ''}</td>
            <td>{lot.approvedQty} / {lot.receivedQty}</td><td>{getStatusLabel(lot.inspectionStatus, inspectionStatusLabels)} / 隔離 {lot.quarantinedQty}</td><td>{getStatusLabel(lot.dispositionStatus, dispositionStatusLabels)}</td>
          </tr>)}{lots.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--slate-500)' }}>此工單尚無 FQC 批次</td></tr>}</tbody>
        </table>
      </div>
    </div>
  );
}
