import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { COLORS } from '../constants/config';
import { getApiErrorMessage, operationsApi } from '../services/api';
import { LotQrCamera } from '../components/LotQrCamera';
import { parseLotQr, type LotQrData } from '../utils/lotQr';

interface Warehouse {
  id: string;
  warehouseCode: string;
  warehouseName: string;
  isActive: boolean;
}

interface WorkOrder {
  id: string;
  woNumber: string;
  productSkuNumber?: string;
  actualQty: number;
  status: string;
}

interface StocktakeBalance {
  skuCode: string;
  skuName: string;
  warehouseCode: string;
  lotNumber: string;
  expiryDate?: string;
  systemQty: number;
}

interface StocktakeResult {
  trxId?: string;
  qtyBefore: number;
  countedQty: number;
  qtyChange: number;
  qtyAfter: number;
}

function unwrap<T>(payload: unknown, fallback: string): T {
  if (!payload || typeof payload !== 'object') throw new Error(fallback);
  const response = payload as { success?: boolean; data?: T; message?: string };
  if (!response.success || response.data === undefined)
    throw new Error(response.message || fallback);
  return response.data;
}

export const OperationsScreen: React.FC = () => {
  const { user } = useAuth();
  const permissions = user?.permissions ?? [];
  const canStocktake = permissions.includes('inventory:view') && permissions.includes('inventory:execute');
  const canFqc = permissions.includes('quality:create') && permissions.includes('manufacturing:view');
  const [workflow, setWorkflow] = useState<'stocktake' | 'fqc'>(canStocktake ? 'stocktake' : 'fqc');
  const [qr, setQr] = useState<LotQrData | null>(null);
  const [rawQr, setRawQr] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);

  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [warehouseCode, setWarehouseCode] = useState('');
  const [balance, setBalance] = useState<StocktakeBalance | null>(null);
  const [countedQty, setCountedQty] = useState('');
  const [stocktakeResult, setStocktakeResult] = useState<StocktakeResult | null>(null);

  const [workOrders, setWorkOrders] = useState<WorkOrder[]>([]);
  const [workOrdersLoaded, setWorkOrdersLoaded] = useState(false);
  const [workOrderId, setWorkOrderId] = useState('');
  const [sampledQty, setSampledQty] = useState('1');
  const [defectiveQty, setDefectiveQty] = useState('0');
  const [criticalDefectFound, setCriticalDefectFound] = useState(false);
  const [defectCode, setDefectCode] = useState('');
  const [inspectorName, setInspectorName] = useState(user?.employeeName || user?.username || '');

  const visibleWorkOrders = useMemo(
    () => qr
      ? workOrders.filter(order => order.productSkuNumber?.toLowerCase() === qr.skuCode.toLowerCase())
      : workOrders,
    [qr, workOrders]
  );

  useEffect(() => {
    if (!canStocktake) return;
    void operationsApi.warehouses()
      .then(response => {
        const activeWarehouses = unwrap<Warehouse[]>(response.data, '載入倉庫失敗').filter(item => item.isActive);
        setWarehouses(activeWarehouses);
        if (activeWarehouses.length === 1) setWarehouseCode(activeWarehouses[0].warehouseCode);
      })
      .catch(loadError => setError(getApiErrorMessage(loadError, '載入倉庫失敗')));
  }, [canStocktake]);

  useEffect(() => {
    if (!canFqc) return;
    void operationsApi.workOrders()
      .then(response => {
        setWorkOrders(unwrap<WorkOrder[]>(response.data, '載入工單失敗'));
        setWorkOrdersLoaded(true);
      })
      .catch(loadError => setError(getApiErrorMessage(loadError, '載入工單失敗')));
  }, [canFqc]);

  const handleScan = useCallback((value: string) => {
    setRawQr(value);
    setError('');
    setNotice('');
    setBalance(null);
    setStocktakeResult(null);
    try {
      const parsed = parseLotQr(value);
      setQr(parsed);
      const matchingOrder = workOrders.find(
        order => order.productSkuNumber?.toLowerCase() === parsed.skuCode.toLowerCase()
      );
      if (matchingOrder) setWorkOrderId(matchingOrder.id);
    } catch (parseError) {
      setQr(null);
      setError(getApiErrorMessage(parseError, 'QR 格式錯誤'));
    }
  }, [workOrders]);

  const lookupBalance = async () => {
    if (!qr || !warehouseCode) {
      setError('請先掃描 QR 並選擇盤點倉庫');
      return;
    }
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const response = await operationsApi.stocktakeBalance(qr.skuCode, warehouseCode, qr.lotNumber);
      const nextBalance = unwrap<StocktakeBalance>(response.data, '讀取帳面庫存失敗');
      setBalance(nextBalance);
      setCountedQty(String(nextBalance.systemQty));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, '讀取帳面庫存失敗'));
    } finally {
      setBusy(false);
    }
  };

  const postStocktake = async () => {
    if (!qr || !balance || !warehouseCode) return;
    const actualQty = Number(countedQty);
    if (!Number.isFinite(actualQty) || actualQty < 0) {
      setError('實盤數必須是大於或等於零的數字');
      return;
    }

    setBusy(true);
    setError('');
    setNotice('');
    try {
      const response = await operationsApi.postStocktake({
        skuCode: qr.skuCode,
        warehouseCode,
        lotNumber: qr.lotNumber,
        expiryDate: qr.expiryDate,
        countedQty: actualQty,
        expectedSystemQty: balance.systemQty,
        notes: 'QR 盤點過帳',
      });
      const result = unwrap<StocktakeResult>(response.data, '盤點過帳失敗');
      setStocktakeResult(result);
      setBalance(current => current ? { ...current, systemQty: result.qtyAfter } : current);
      setNotice(`盤點已過帳，帳面 ${result.qtyBefore}、實盤 ${result.countedQty}、差異 ${result.qtyChange >= 0 ? '+' : ''}${result.qtyChange}${result.trxId ? `，異動單 ${result.trxId}` : ''}`);
    } catch (postError) {
      setError(getApiErrorMessage(postError, '盤點過帳失敗'));
    } finally {
      setBusy(false);
    }
  };

  const confirmStocktake = () => {
    if (!qr || !balance) return;
    const actualQty = Number(countedQty);
    if (!Number.isFinite(actualQty) || actualQty < 0) {
      setError('實盤數必須是大於或等於零的數字');
      return;
    }
    const delta = actualQty - balance.systemQty;
    const message = `${qr.skuCode} / ${qr.lotNumber}\n帳面數：${balance.systemQty}　實盤數：${actualQty}\n差異：${delta >= 0 ? '+' : ''}${delta}\n效期：${qr.expiryDate}\n確認後將立即過帳庫存調整。`;
    if (Platform.OS === 'web') {
      if (window.confirm(message)) void postStocktake();
      return;
    }
    Alert.alert('確認盤點過帳', message, [
      { text: '取消', style: 'cancel' },
      { text: '確認過帳', onPress: () => void postStocktake() },
    ]);
  };

  const createFqcInspection = async () => {
    if (!qr || !workOrderId || !inspectorName.trim()) {
      setError('請掃描 QR、選擇對應工單並填寫檢驗人員');
      return;
    }
    if ((Number(defectiveQty) > 0 || criticalDefectFound) && !defectCode.trim()) {
      setError('有抽樣不良或重大缺點時，請填寫主要不良代碼');
      return;
    }
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const response = await operationsApi.createFqcInspection({
        workOrderId,
        lotNumber: qr.lotNumber,
        expiryDate: qr.expiryDate,
        sampledQty: Number(sampledQty),
        defectiveQty: Number(defectiveQty),
        criticalDefectFound,
        defectCode: defectCode.trim() || undefined,
        inspectorName: inspectorName.trim(),
      });
      const created = unwrap<{ inspectionNumber: string; inspectionStatus: string }>(
        response.data,
        '建立 FQC 待判定批次失敗'
      );
      setNotice(`已建立 ${created.inspectionNumber}，狀態：${created.inspectionStatus}。請由授權人員另行判定，不會自動放行。`);
      setSampledQty('1');
      setDefectiveQty('0');
      setCriticalDefectFound(false);
      setDefectCode('');
    } catch (createError) {
      setError(getApiErrorMessage(createError, '建立 FQC 待判定批次失敗'));
    } finally {
      setBusy(false);
    }
  };

  const parseManualQr = () => handleScan(rawQr);

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.title}>掃描作業</Text>
      <Text style={styles.description}>QR 格式：料號|批號|YYYY-MM-DD（日期為效期）</Text>

      <LotQrCamera onScan={handleScan} />
      <View style={styles.row}>
        <TextInput
          style={[styles.input, styles.flex]}
          value={rawQr}
          onChangeText={setRawQr}
          placeholder="或輸入 QR 內容"
          autoCapitalize="none"
        />
        <TouchableOpacity style={styles.secondaryButton} onPress={parseManualQr}>
          <Text style={styles.secondaryText}>解析</Text>
        </TouchableOpacity>
      </View>

      {qr && (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>掃描結果</Text>
          <Text style={styles.value}>料號：{qr.skuCode}</Text>
          <Text style={styles.value}>批號：{qr.lotNumber}</Text>
          <Text style={styles.value}>效期：{qr.expiryDate}</Text>
        </View>
      )}

      {(canStocktake && canFqc) && (
        <View style={styles.row}>
          <TouchableOpacity
            style={[styles.secondaryButton, workflow === 'stocktake' && styles.selectedButton]}
            onPress={() => setWorkflow('stocktake')}
          >
            <Text style={[styles.secondaryText, workflow === 'stocktake' && styles.selectedText]}>庫存盤點</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.secondaryButton, workflow === 'fqc' && styles.selectedButton]}
            onPress={() => setWorkflow('fqc')}
          >
            <Text style={[styles.secondaryText, workflow === 'fqc' && styles.selectedText]}>成品 FQC</Text>
          </TouchableOpacity>
        </View>
      )}

      {workflow === 'stocktake' && canStocktake && (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>庫存盤點</Text>
          <Text style={styles.label}>盤點倉庫</Text>
          <View style={styles.choiceList}>
            {warehouses.map(warehouse => (
              <TouchableOpacity
                key={warehouse.id}
                style={[styles.choice, warehouseCode === warehouse.warehouseCode && styles.choiceSelected]}
                onPress={() => { setWarehouseCode(warehouse.warehouseCode); setBalance(null); setStocktakeResult(null); }}
              >
                <Text style={styles.value}>{warehouse.warehouseCode} - {warehouse.warehouseName}</Text>
              </TouchableOpacity>
            ))}
          </View>
          <TouchableOpacity style={styles.primaryButton} onPress={() => void lookupBalance()} disabled={busy || !qr || !warehouseCode}>
            <Text style={styles.primaryText}>{busy ? '讀取中…' : '讀取系統帳面數'}</Text>
          </TouchableOpacity>
          {balance && (
            <>
              <Text style={styles.value}>{balance.skuName}　帳面數：{balance.systemQty}</Text>
              <Text style={styles.label}>實盤數</Text>
              <TextInput style={styles.input} value={countedQty} onChangeText={setCountedQty} keyboardType="decimal-pad" />
              <Text style={styles.value}>差異：{Number(countedQty || 0) - balance.systemQty}</Text>
              {balance.expiryDate && balance.expiryDate.slice(0, 10) !== qr?.expiryDate && (
                <Text style={styles.warning}>原批號效期為 {balance.expiryDate.slice(0, 10)}；過帳將更新為掃描效期。</Text>
              )}
              <TouchableOpacity style={styles.primaryButton} onPress={confirmStocktake} disabled={busy}>
                <Text style={styles.primaryText}>{busy ? '過帳中…' : '確認差異並過帳'}</Text>
              </TouchableOpacity>
            </>
          )}
          {stocktakeResult && <Text style={styles.success}>已過帳差異 {stocktakeResult.qtyChange}，帳面更新為 {stocktakeResult.qtyAfter}。</Text>}
        </View>
      )}

      {workflow === 'fqc' && canFqc && (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>建立成品 FQC 待判定批次</Text>
          <Text style={styles.description}>掃描只帶入批號與效期；檢驗結果需人工填寫，且不會自動判定合格。</Text>
          <Text style={styles.label}>成品工單</Text>
          <View style={styles.choiceList}>
            {visibleWorkOrders.map(order => (
              <TouchableOpacity
                key={order.id}
                style={[styles.choice, workOrderId === order.id && styles.choiceSelected]}
                onPress={() => setWorkOrderId(order.id)}
              >
                <Text style={styles.value}>{order.woNumber} - {order.productSkuNumber} ({order.status})</Text>
              </TouchableOpacity>
            ))}
          </View>
          {qr && workOrdersLoaded && visibleWorkOrders.length === 0 && <Text style={styles.warning}>找不到 QR 料號對應的工單。</Text>}
          <Text style={styles.label}>抽樣數量</Text>
          <TextInput style={styles.input} value={sampledQty} onChangeText={setSampledQty} keyboardType="decimal-pad" />
          <Text style={styles.label}>抽樣不良數</Text>
          <TextInput style={styles.input} value={defectiveQty} onChangeText={setDefectiveQty} keyboardType="decimal-pad" />
          <TouchableOpacity style={styles.checkRow} onPress={() => setCriticalDefectFound(value => !value)}>
            <Text style={styles.value}>{criticalDefectFound ? '☑' : '☐'} 發現重大缺點</Text>
          </TouchableOpacity>
          {(Number(defectiveQty) > 0 || criticalDefectFound) && (
            <>
              <Text style={styles.label}>主要不良代碼</Text>
              <TextInput style={styles.input} value={defectCode} onChangeText={setDefectCode} maxLength={50} />
            </>
          )}
          <Text style={styles.label}>檢驗人員</Text>
          <TextInput style={styles.input} value={inspectorName} onChangeText={setInspectorName} maxLength={100} />
          <TouchableOpacity style={styles.primaryButton} onPress={() => void createFqcInspection()} disabled={busy || !qr || !workOrderId}>
            {busy ? <ActivityIndicator color={COLORS.white} /> : <Text style={styles.primaryText}>建立待判定 FQC</Text>}
          </TouchableOpacity>
        </View>
      )}

      {error ? <Text style={styles.error} accessibilityLiveRegion="assertive">{error}</Text> : null}
      {notice ? <Text style={styles.success} accessibilityLiveRegion="polite">{notice}</Text> : null}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: COLORS.background },
  content: { padding: 16, gap: 12 },
  title: { color: COLORS.dark, fontSize: 22, fontWeight: '700' },
  description: { color: COLORS.gray, fontSize: 13 },
  row: { flexDirection: 'row', gap: 8, alignItems: 'center' },
  flex: { flex: 1 },
  card: { backgroundColor: COLORS.white, borderRadius: 12, padding: 16, gap: 10, elevation: 1 },
  cardTitle: { color: COLORS.dark, fontSize: 17, fontWeight: '700' },
  value: { color: COLORS.dark, fontSize: 14 },
  label: { color: COLORS.gray, fontSize: 13, fontWeight: '600', marginTop: 4 },
  input: { minHeight: 44, borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8, paddingHorizontal: 10, backgroundColor: COLORS.white, color: COLORS.dark },
  choiceList: { gap: 6 },
  choice: { padding: 10, borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8 },
  choiceSelected: { borderColor: COLORS.primary, backgroundColor: '#EFF6FF' },
  primaryButton: { minHeight: 44, borderRadius: 8, justifyContent: 'center', alignItems: 'center', backgroundColor: COLORS.primary, paddingHorizontal: 12 },
  primaryText: { color: COLORS.white, fontWeight: '700' },
  secondaryButton: { minHeight: 40, borderRadius: 8, justifyContent: 'center', alignItems: 'center', backgroundColor: COLORS.lightGray, paddingHorizontal: 12 },
  secondaryText: { color: COLORS.dark, fontWeight: '600' },
  selectedButton: { backgroundColor: COLORS.primary },
  selectedText: { color: COLORS.white },
  checkRow: { paddingVertical: 8 },
  error: { color: COLORS.danger, padding: 10, backgroundColor: '#FEF2F2', borderRadius: 8 },
  success: { color: '#047857', padding: 10, backgroundColor: '#ECFDF5', borderRadius: 8 },
  warning: { color: '#B45309', fontSize: 13 },
});
