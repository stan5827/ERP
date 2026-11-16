import { useCallback, useEffect, useState } from 'react';
import { inventoryApi, warehousesApi } from '../api/inventory';
import type {
  InventoryStocktakeBalanceDto,
  InventoryStocktakeResultDto,
  WarehouseDto,
} from '../api/inventory';
import { LotQrCamera } from '../components/LotQrCamera';
import { parseLotQr } from '../utils/lotQr';

type LotQrData = ReturnType<typeof parseLotQr>;

const getErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error === 'object' && error !== null) {
    const response = (error as { response?: { data?: { message?: string } } }).response;
    if (response?.data?.message) return response.data.message;
    if (error instanceof Error) return error.message;
  }
  return fallback;
};

export function InventoryStocktakePanel() {
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [warehouseCode, setWarehouseCode] = useState('');
  const [rawQr, setRawQr] = useState('');
  const [lot, setLot] = useState<LotQrData | null>(null);
  const [balance, setBalance] = useState<InventoryStocktakeBalanceDto | null>(null);
  const [countedQty, setCountedQty] = useState('');
  const [result, setResult] = useState<InventoryStocktakeResultDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    void warehousesApi.list()
      .then(response => {
        const activeWarehouses = (response.data?.data ?? []).filter(item => item.isActive);
        setWarehouses(activeWarehouses);
        if (activeWarehouses.length === 1) setWarehouseCode(activeWarehouses[0].warehouseCode);
      })
      .catch(loadError => setError(getErrorMessage(loadError, '載入倉庫失敗')));
  }, []);

  const handleScan = useCallback((value: string) => {
    setRawQr(value);
    setBalance(null);
    setResult(null);
    setError('');
    try {
      setLot(parseLotQr(value));
    } catch (parseError) {
      setLot(null);
      setError(getErrorMessage(parseError, 'QR 格式錯誤'));
    }
  }, []);

  const lookupBalance = async () => {
    if (!lot || !warehouseCode) {
      setError('請先掃描 QR 並選擇倉庫');
      return;
    }
    setBusy(true);
    setError('');
    setResult(null);
    try {
      const response = await inventoryApi.getStocktakeBalance(lot.skuCode, warehouseCode, lot.lotNumber);
      if (!response.data?.success || !response.data.data)
        throw new Error(response.data?.message || '讀取帳面庫存失敗');
      setBalance(response.data.data);
      setCountedQty(String(response.data.data.systemQty));
    } catch (lookupError) {
      setError(getErrorMessage(lookupError, '讀取帳面庫存失敗'));
    } finally {
      setBusy(false);
    }
  };

  const postCount = async () => {
    if (!lot || !balance || !warehouseCode) return;
    const actualQty = Number(countedQty);
    if (!Number.isFinite(actualQty) || actualQty < 0) {
      setError('實盤數必須是大於或等於零的數字');
      return;
    }

    const difference = actualQty - balance.systemQty;
    const confirmation = [
      `料號：${lot.skuCode}`,
      `批號：${lot.lotNumber}`,
      `倉庫：${warehouseCode}`,
      `帳面數：${balance.systemQty}`,
      `實盤數：${actualQty}`,
      `差異：${difference >= 0 ? '+' : ''}${difference}`,
      `效期：${lot.expiryDate}`,
      '確認後將立即過帳庫存調整。',
    ].join('\n');
    if (!window.confirm(confirmation)) return;

    setBusy(true);
    setError('');
    try {
      const response = await inventoryApi.postStocktake({
        skuCode: lot.skuCode,
        warehouseCode,
        lotNumber: lot.lotNumber,
        expiryDate: lot.expiryDate,
        countedQty: actualQty,
        expectedSystemQty: balance.systemQty,
        notes: 'QR 盤點過帳',
      });
      if (!response.data?.success || !response.data.data)
        throw new Error(response.data?.message || '盤點過帳失敗');
      setResult(response.data.data);
      setBalance(current => current ? { ...current, systemQty: response.data.data!.qtyAfter } : current);
    } catch (postError) {
      setError(getErrorMessage(postError, '盤點過帳失敗'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div style={{ display: 'grid', gap: 16 }}>
      {error && <div role="alert" className="glass-card" style={{ padding: 12, color: 'var(--crimson-300)' }}>{error}</div>}
      {result && (
        <div role="status" className="glass-card" style={{ padding: 12, color: 'var(--emerald-300)' }}>
          盤點已過帳：{result.skuCode} / {result.lotNumber}，帳面 {result.qtyBefore}，實盤 {result.countedQty}，差異 {result.qtyChange >= 0 ? '+' : ''}{result.qtyChange}
          {result.trxId && `，異動單 ${result.trxId}`}
        </div>
      )}
      <div className="glass-card" style={{ padding: 18, display: 'grid', gap: 14 }}>
        <div>
          <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>QR 批號盤點</h3>
          <p style={{ color: 'var(--slate-400)', fontSize: 12, marginTop: 5 }}>格式：料號|批號|YYYY-MM-DD（日期為效期）。盤點差異須人工確認後才過帳。</p>
        </div>
        <LotQrCamera onScan={handleScan} />
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <input
            className="form-input"
            aria-label="QR 內容"
            placeholder="或輸入 QR 內容：料號|批號|YYYY-MM-DD"
            value={rawQr}
            onChange={event => setRawQr(event.target.value)}
            style={{ flex: '1 1 280px' }}
          />
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => handleScan(rawQr)}>解析 QR</button>
        </div>
        {lot && (
          <div className="inspection-template-grid">
            <label>料號<input className="form-input" value={lot.skuCode} readOnly /></label>
            <label>批號<input className="form-input" value={lot.lotNumber} readOnly /></label>
            <label>效期<input className="form-input" type="date" value={lot.expiryDate} readOnly /></label>
            <label>盤點倉庫
              <select className="form-input" value={warehouseCode} onChange={event => { setWarehouseCode(event.target.value); setBalance(null); setResult(null); }}>
                <option value="">選擇倉庫</option>
                {warehouses.map(warehouse => <option key={warehouse.id} value={warehouse.warehouseCode}>{warehouse.warehouseCode} - {warehouse.warehouseName}</option>)}
              </select>
            </label>
          </div>
        )}
        {lot && (
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <button type="button" className="btn btn-primary btn-sm" onClick={() => void lookupBalance()} disabled={busy || !warehouseCode}>
              {busy ? '讀取中…' : '讀取系統帳面數'}
            </button>
          </div>
        )}
        {balance && lot && (
          <div style={{ display: 'grid', gap: 12 }}>
            <div style={{ color: 'var(--slate-200)', fontSize: 13 }}>
              {balance.skuName} · {balance.warehouseCode} 帳面數 <strong>{balance.systemQty}</strong>
              {balance.expiryDate && balance.expiryDate.slice(0, 10) !== lot.expiryDate && (
                <span style={{ color: 'var(--amber-300)', marginLeft: 8 }}>系統批號效期為 {balance.expiryDate.slice(0, 10)}；過帳將更新為掃描效期。</span>
              )}
            </div>
            <div className="inspection-template-grid">
              <label>實盤數量<input className="form-input" type="number" min="0" step="0.01" value={countedQty} onChange={event => setCountedQty(event.target.value)} /></label>
              <label>盤點差異<input className="form-input" value={Number(countedQty || 0) - balance.systemQty} readOnly /></label>
            </div>
            <div>
              <button type="button" className="btn btn-primary btn-sm" onClick={() => void postCount()} disabled={busy}>
                {busy ? '過帳中…' : '確認差異並過帳'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
