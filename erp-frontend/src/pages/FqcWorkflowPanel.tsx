import { useCallback, useEffect, useState } from 'react';
import { binLocationsApi, warehousesApi } from '../api/api';
import { fqcWorkflowApi } from '../api/mes';
import type { FqcInspectionLotDto, ProductionReceiptDto, WorkOrderDto } from '../api/mes';
import type { BinLocationDto, WarehouseDto } from '../api/inventory';
import { LotQrCamera } from '../components/LotQrCamera';
import { parseLotQr } from '../utils/lotQr';

const getMessage = (error: unknown) =>
  (error as { response?: { data?: { message?: string } }; message?: string }).response?.data?.message ??
  (error as { message?: string }).message ??
  '操作失敗，請稍後再試';

export function FqcWorkflowPanel({ workOrders }: { workOrders: WorkOrderDto[] }) {
  const [workOrderId, setWorkOrderId] = useState('');
  const [lots, setLots] = useState<FqcInspectionLotDto[]>([]);
  const [receipts, setReceipts] = useState<ProductionReceiptDto[]>([]);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [bins, setBins] = useState<BinLocationDto[]>([]);
  const [lotNumber, setLotNumber] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [manualQr, setManualQr] = useState('');
  const [scannedSkuCode, setScannedSkuCode] = useState('');
  const [sampledQty, setSampledQty] = useState('1');
  const [defectiveQty, setDefectiveQty] = useState('0');
  const [criticalDefectFound, setCriticalDefectFound] = useState(false);
  const [defectCode, setDefectCode] = useState('');
  const [inspectorName, setInspectorName] = useState('');
  const [decisionBy, setDecisionBy] = useState('');
  const [approvedQty, setApprovedQty] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [binLocationId, setBinLocationId] = useState('');
  const [receiptQty, setReceiptQty] = useState('');
  const [selectedLotId, setSelectedLotId] = useState('');
  const [scrapReason, setScrapReason] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const selectedLot = lots.find(lot => lot.id === selectedLotId);

  const handleLotScan = useCallback((value: string) => {
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
      else setError(`已讀取 ${scanned.skuCode}，請選擇此料號對應的工單。`);
    } catch (scanError) {
      setLotNumber('');
      setExpiryDate('');
      setScannedSkuCode('');
      setError(scanError instanceof Error ? scanError.message : 'QR 格式錯誤');
    }
  }, [workOrders]);

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
    setSelectedLotId(currentId =>
      nextLots.some(lot => lot.id === currentId) ? currentId : nextLots[0]?.id ?? ''
    );
  }, []);

  useEffect(() => {
    if (!workOrderId && workOrders.length > 0)
      setWorkOrderId(workOrders[0].id);
  }, [workOrderId, workOrders]);

  useEffect(() => {
    void Promise.all([
      warehousesApi.list().then(response => setWarehouses((response.data?.data ?? []).filter(item => item.isActive))),
    ]).catch(loadError => setError(getMessage(loadError)));
  }, []);

  useEffect(() => {
    setError('');
    void refresh(workOrderId).catch(loadError => setError(getMessage(loadError)));
  }, [refresh, workOrderId]);

  useEffect(() => {
    setBins([]);
    setBinLocationId('');
    if (!warehouseId) return;
    void binLocationsApi.getByWarehouse(warehouseId)
      .then(response => setBins((response.data?.data ?? []).filter(bin => bin.isActive && bin.locationCode !== 'FQC-HOLD')))
      .catch(loadError => setError(getMessage(loadError)));
  }, [warehouseId]);

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
    if (!selectedLot || !warehouseId || !binLocationId) {
      setError('入庫前請選擇已放行的 FQC 批次、成品倉庫與儲位。');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await fqcWorkflowApi.createReceipt({
        fqcInspectionLotId: selectedLot.id,
        quantity: Number(receiptQty),
        warehouseId,
        binLocationId,
      });
      setReceiptQty('');
      await refresh(workOrderId);
    } catch (saveError) {
      setError(getMessage(saveError));
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
    if (!selectedLot || !scrapReason.trim()) {
      setError('請先選擇 FQC 批次並填寫報廢原因。');
      return;
    }
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const response = await fqcWorkflowApi.createScrapNote(selectedLot.id, scrapReason.trim());
      const scrapNote = response.data?.data;
      setNotice(scrapNote
        ? `報廢單 ${scrapNote.scrapNumber} 已建立，數量 ${scrapNote.quantity}；待會計核定，尚未異動庫存或入帳。`
        : '待會計核定報廢單已建立；尚未異動庫存或入帳。');
      setScrapReason('');
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
          <label>工單<select className="form-input" value={workOrderId} onChange={event => setWorkOrderId(event.target.value)}><option value="">選擇工單</option>{workOrders.map(order => <option key={order.id} value={order.id}>{order.woNumber} - {order.productSkuNumber}</option>)}</select>{scannedSkuCode && <small>QR 料號：{scannedSkuCode}</small>}</label>
          <label>成品批號<input className="form-input" maxLength={50} value={lotNumber} onChange={event => setLotNumber(event.target.value)} /></label>
          <label>效期<input className="form-input" type="date" value={expiryDate} onChange={event => setExpiryDate(event.target.value)} /></label>
          <label>抽樣數量<input className="form-input" type="number" min="0.01" step="0.01" value={sampledQty} onChange={event => setSampledQty(event.target.value)} /></label>
          <label>抽樣不良數<input className="form-input" type="number" min="0" step="0.01" value={defectiveQty} onChange={event => setDefectiveQty(event.target.value)} /></label>
          <label>主要不良代碼<input className="form-input" maxLength={50} value={defectCode} onChange={event => setDefectCode(event.target.value)} /></label>
          <label>檢驗人員<input className="form-input" maxLength={100} value={inspectorName} onChange={event => setInspectorName(event.target.value)} /></label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}><input type="checkbox" checked={criticalDefectFound} onChange={event => setCriticalDefectFound(event.target.checked)} />發現重大缺點（強制 Failed）</label>
        </div>
        <div><button className="btn btn-primary btn-sm" disabled={busy} onClick={() => void createFqcLot()}>{busy ? '處理中…' : '建立待判定 FQC 批次'}</button></div>
      </div>

      <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>整批判定與放行數量</h3>
        <div className="inspection-template-grid">
          <label>待判定批次<select className="form-input" value={selectedLotId} onChange={event => { const lot = lots.find(item => item.id === event.target.value); setSelectedLotId(event.target.value); setApprovedQty(lot ? String(Math.max(0, lot.producedQty - lot.defectiveQty)) : ''); }}><option value="">選擇 FQC 批次</option>{lots.map(lot => <option key={lot.id} value={lot.id}>{lot.inspectionNumber} · {lot.lotNumber} · {lot.inspectionStatus}</option>)}</select></label>
          <label>核准良品數<input className="form-input" type="number" min="0" step="0.01" value={approvedQty} onChange={event => setApprovedQty(event.target.value)} disabled={!selectedLot || selectedLot.inspectionStatus !== 'Pending'} /></label>
          <label>隔離／入庫倉庫<select className="form-input" value={warehouseId} onChange={event => setWarehouseId(event.target.value)}><option value="">選擇倉庫</option>{warehouses.map(warehouse => <option key={warehouse.id} value={warehouse.id}>{warehouse.warehouseCode} - {warehouse.warehouseName}</option>)}</select></label>
          <label>整批判定人員<input className="form-input" maxLength={100} value={decisionBy} onChange={event => setDecisionBy(event.target.value)} /></label>
        </div>
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
      </div>

      <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>FQC 放行批次完工入庫</h3>
        <div className="inspection-template-grid">
          <label>成品儲位<select className="form-input" value={binLocationId} onChange={event => setBinLocationId(event.target.value)} disabled={!warehouseId}><option value="">選擇良品儲位</option>{bins.map(bin => <option key={bin.id} value={bin.id}>{bin.locationCode}</option>)}</select></label>
          <label>本次入庫數量<input className="form-input" type="number" min="0.01" step="0.01" value={receiptQty} onChange={event => setReceiptQty(event.target.value)} /></label>
        </div>
        <div><button className="btn btn-primary btn-sm" disabled={busy || !selectedLot || !['Passed', 'Concession'].includes(selectedLot.inspectionStatus) || selectedLot.availableReceiptQty <= 0} onClick={() => void createReceipt()}>{busy ? '處理中…' : '確認完工入庫'}</button></div>
        <div style={{ overflowX: 'auto' }}>
          <table className="data-table">
            <thead><tr><th>入庫單</th><th>FQC 單號</th><th>批號</th><th>數量</th><th>金額</th><th>過帳時間</th></tr></thead>
            <tbody>{receipts.map(receipt => <tr key={receipt.id}><td className="mono">{receipt.receiptNumber}</td><td>{receipt.fqcInspectionNumber}</td><td>{receipt.lotNumber}</td><td>{receipt.quantity}</td><td>{receipt.totalCost.toLocaleString()}</td><td>{new Date(receipt.receivedAt).toLocaleString()}</td></tr>)}
              {receipts.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center', padding: 20, color: 'var(--slate-500)' }}>尚無完工入庫紀錄</td></tr>}</tbody>
          </table>
        </div>
      </div>

      <div className="glass-card" style={{ overflowX: 'auto' }}>
        {selectedLot && selectedLot.quarantinedQty > 0 && (
          <div style={{ display: 'grid', gap: 10, padding: 14 }}>
            <span style={{ color: 'var(--slate-300)', fontSize: 12 }}>
              隔離數量 {selectedLot.quarantinedQty}；返修工單會沿用原工序，隔離庫存仍維持鎖定。
              {selectedLot.reworkWorkOrderId && ` 返修工單：${selectedLot.reworkWorkOrderId}`}
            </span>
            <div className="inspection-template-grid">
              <button
                className="btn btn-ghost btn-sm"
                disabled={busy || selectedLot.inspectionStatus === 'Pending' || !!selectedLot.reworkWorkOrderId || selectedLot.dispositionStatus === 'ScrapPendingAccounting'}
                onClick={() => void createReworkWorkOrder()}
              >
                建立返修工單
              </button>
              <label>報廢原因<input className="form-input" maxLength={500} value={scrapReason} onChange={event => setScrapReason(event.target.value)} /></label>
              <button
                className="btn btn-ghost btn-sm"
                disabled={busy || selectedLot.inspectionStatus === 'Pending' || !!selectedLot.reworkWorkOrderId || selectedLot.dispositionStatus === 'ScrapPendingAccounting'}
                onClick={() => void createScrapNote()}
              >
                建立待核定報廢單
              </button>
            </div>
          </div>
        )}
        <table className="data-table">
          <thead><tr><th>FQC 批次</th><th>工單 / 成品批號</th><th>報工量</th><th>抽樣 / 不良</th><th>核准 / 已入庫</th><th>判定 / 隔離</th><th>處置狀態</th></tr></thead>
          <tbody>{lots.map(lot => <tr key={lot.id} onClick={() => { setSelectedLotId(lot.id); setApprovedQty(String(Math.max(0, lot.producedQty - lot.defectiveQty))); }} style={{ cursor: 'pointer' }}>
            <td className="mono">{lot.inspectionNumber}</td><td>{lot.workOrderNumber} / {lot.lotNumber}</td><td>{lot.producedQty}</td>
            <td>{lot.sampledQty} / {lot.defectiveQty}{lot.criticalDefectFound ? ' · 重大缺點' : ''}</td>
            <td>{lot.approvedQty} / {lot.receivedQty}</td><td>{lot.inspectionStatus} / 隔離 {lot.quarantinedQty}</td><td>{lot.dispositionStatus}</td>
          </tr>)}{lots.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--slate-500)' }}>此工單尚無 FQC 批次</td></tr>}</tbody>
        </table>
      </div>
    </div>
  );
}
