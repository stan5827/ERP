import { useState } from 'react';
import type { FormEvent } from 'react';
import type { SupplierApSubmissionDraftRequest } from '../api/types';

export interface ApSubmissionLineFormValue {
  goodsReceiptItemId: string;
  skuNumber: string | null;
  skuName: string | null;
  unit: string;
  acceptedQuantity: number;
  availableQuantity: number;
  quantity: string;
  unitPrice: string;
  taxAmount: string;
}

export interface ApSubmissionFormValue {
  supplierInvoiceNumber: string;
  supplierInvoiceDate: string;
  currency: string;
  purchaseOrderId: string;
  purchaseOrderNumber: string;
  goodsReceiptId: string;
  goodsReceiptNumber: string;
  lines: ApSubmissionLineFormValue[];
}

function decimalToScaled(value: string, precision: number) {
  const match = /^(\d*)(?:\.(\d*))?$/.exec(value.trim());
  if (!match || (!match[1] && !match[2]))
    return null;
  const fraction = match[2] ?? '';
  if (fraction.slice(precision).replace(/0/g, '').length > 0)
    return null;
  const scale = 10n ** BigInt(precision);
  const normalizedFraction = fraction.slice(0, precision).padEnd(precision, '0');
  return BigInt(match[1] || '0') * scale + BigInt(normalizedFraction || '0');
}

function scaledToCents(value: bigint, precision: number) {
  const divisor = 10n ** BigInt(precision - 2);
  const quotient = value / divisor;
  const remainder = value % divisor;
  const midpoint = divisor / 2n;
  const rounded = remainder > midpoint || (remainder === midpoint && quotient % 2n !== 0n)
    ? quotient + 1n
    : quotient;
  return Number(rounded);
}

function lineNetCents(quantityValue: string, unitPriceValue: string) {
  const quantity = decimalToScaled(quantityValue, 2);
  const unitPrice = decimalToScaled(unitPriceValue.trim() || '0', 4);
  if (quantity === null || unitPrice === null)
    return 0;
  return scaledToCents(quantity * unitPrice, 6);
}

function taxCents(value: string) {
  const scaled = decimalToScaled(value.trim() || '0', 2);
  return scaled === null ? 0 : Number(scaled);
}

export function calculateApLineNetAmount(quantity: string, unitPrice: string) {
  return lineNetCents(quantity, unitPrice) / 100;
}

function hasPrecision(value: number, digits: number) {
  return Number.isFinite(value)
    && Math.abs(value * (10 ** digits) - Math.round(value * (10 ** digits))) < 0.000001;
}

function toRequest(value: ApSubmissionFormValue): SupplierApSubmissionDraftRequest {
  const lines = value.lines
    .map(line => {
      const quantity = Number(line.quantity);
      const unitPrice = Number(line.unitPrice);
      const netCents = lineNetCents(line.quantity, line.unitPrice);
      const lineTaxCents = taxCents(line.taxAmount);
      const netAmount = netCents / 100;
      const normalizedTaxAmount = lineTaxCents / 100;
      return {
        goodsReceiptItemId: line.goodsReceiptItemId,
        invoicedQuantity: quantity,
        unitPrice,
        netAmount,
        taxAmount: normalizedTaxAmount,
        totalAmount: (netCents + lineTaxCents) / 100,
      };
    })
    .filter(line => line.invoicedQuantity > 0);
  const invoiceAmount = lines.reduce((total, line) => total + Math.round(line.netAmount * 100), 0) / 100;
  const taxAmount = lines.reduce((total, line) => total + Math.round(line.taxAmount * 100), 0) / 100;

  return {
    supplierInvoiceNumber: value.supplierInvoiceNumber.trim(),
    supplierInvoiceDate: value.supplierInvoiceDate,
    currency: value.currency,
    invoiceAmount,
    taxAmount,
    totalAmount: (Math.round(invoiceAmount * 100) + Math.round(taxAmount * 100)) / 100,
    purchaseOrderId: value.purchaseOrderId,
    goodsReceiptId: value.goodsReceiptId,
    lines,
  };
}

function validate(value: ApSubmissionFormValue) {
  const errors: string[] = [];
  if (!value.supplierInvoiceNumber.trim())
    errors.push('請輸入供應商發票號碼。');
  if (value.supplierInvoiceNumber.trim().length > 50)
    errors.push('發票號碼不可超過 50 個字元。');
  if (!value.supplierInvoiceDate)
    errors.push('請選擇發票日期。');
  if (!value.purchaseOrderId || !value.goodsReceiptId)
    errors.push('請選取有效的採購單與收貨單來源。');

  const selectedLines = value.lines.filter(line => Number(line.quantity) > 0);
  if (selectedLines.length === 0)
    errors.push('至少輸入一筆大於零的請款數量。');

  for (const [index, line] of selectedLines.entries()) {
    const quantity = Number(line.quantity);
    const unitPrice = Number(line.unitPrice);
    const taxAmount = Number(line.taxAmount);
    if (!hasPrecision(quantity, 2)
      || decimalToScaled(line.quantity, 2) === null
      || !Number.isSafeInteger(lineNetCents(line.quantity, line.unitPrice))
      || quantity <= 0
      || quantity > line.availableQuantity)
      errors.push(`第 ${index + 1} 筆數量需大於 0、最多 2 位小數，且不可超過可請款數量。`);
    if (!hasPrecision(unitPrice, 4)
      || decimalToScaled(line.unitPrice.trim() || '0', 4) === null
      || unitPrice < 0)
      errors.push(`第 ${index + 1} 筆單價需為非負數且最多 4 位小數。`);
    if (!hasPrecision(taxAmount, 2)
      || decimalToScaled(line.taxAmount.trim() || '0', 2) === null
      || !Number.isSafeInteger(taxCents(line.taxAmount))
      || taxAmount < 0)
      errors.push(`第 ${index + 1} 筆稅額需為非負數且最多 2 位小數。`);
  }

  return errors;
}

function lineAmounts(line: ApSubmissionLineFormValue) {
  const netCents = lineNetCents(line.quantity, line.unitPrice);
  const taxValue = taxCents(line.taxAmount);
  return { net: netCents / 100, tax: taxValue / 100, total: (netCents + taxValue) / 100 };
}

export function createApSubmissionFormValue(
  purchaseOrderId: string,
  purchaseOrderNumber: string,
  goodsReceiptId: string,
  goodsReceiptNumber: string,
  currency: string,
  lines: Array<{
    goodsReceiptItemId: string;
    skuNumber: string | null;
    skuName: string | null;
    unit: string;
    acceptedQuantity: number;
    availableQuantity: number;
    unitPrice: number;
    invoicedQuantity: number;
    taxAmount: number;
  }>,
  invoice?: {
    supplierInvoiceNumber: string;
    supplierInvoiceDate: string;
  },
): ApSubmissionFormValue {
  return {
    supplierInvoiceNumber: invoice?.supplierInvoiceNumber ?? '',
    supplierInvoiceDate: invoice?.supplierInvoiceDate ?? '',
    currency,
    purchaseOrderId,
    purchaseOrderNumber,
    goodsReceiptId,
    goodsReceiptNumber,
    lines: lines.map(line => ({
      goodsReceiptItemId: line.goodsReceiptItemId,
      skuNumber: line.skuNumber,
      skuName: line.skuName,
      unit: line.unit,
      acceptedQuantity: line.acceptedQuantity,
      availableQuantity: line.availableQuantity,
      quantity: line.invoicedQuantity ? String(line.invoicedQuantity) : '',
      unitPrice: String(line.unitPrice),
      taxAmount: line.taxAmount ? String(line.taxAmount) : '0',
    })),
  };
}

export function ApSubmissionEditor({
  initialValue,
  disabled,
  saving,
  submitLabel,
  onSubmit,
}: {
  initialValue: ApSubmissionFormValue;
  disabled: boolean;
  saving: boolean;
  submitLabel: string;
  onSubmit: (request: SupplierApSubmissionDraftRequest) => Promise<void>;
}) {
  const [value, setValue] = useState(initialValue);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const netTotal = value.lines.reduce(
    (sum, line) => sum + lineNetCents(line.quantity, line.unitPrice),
    0,
  ) / 100;
  const taxTotal = value.lines.reduce((sum, line) => sum + taxCents(line.taxAmount), 0) / 100;
  const total = (Math.round(netTotal * 100) + Math.round(taxTotal * 100)) / 100;

  function updateLine(index: number, key: 'quantity' | 'unitPrice' | 'taxAmount', nextValue: string) {
    setValue(current => ({
      ...current,
      lines: current.lines.map((line, lineIndex) =>
        lineIndex === index ? { ...line, [key]: nextValue } : line),
    }));
    setValidationErrors([]);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const errors = validate(value);
    setValidationErrors(errors);
    if (errors.length > 0)
      return;
    await onSubmit(toRequest(value));
  }

  return (
    <form onSubmit={event => void submit(event)}>
      <section className="panel">
        <div className="panel-heading">
          <div><span className="eyebrow">SUPPLIER INVOICE</span><h2>發票資料</h2></div>
          <span className="status-pill status-muted">{value.currency || '未選幣別'}</span>
        </div>
        <div className="form-grid">
          <label className="field">
            供應商發票號碼 <em>必填</em>
            <input
              maxLength={50}
              value={value.supplierInvoiceNumber}
              onChange={event => setValue(current => ({ ...current, supplierInvoiceNumber: event.target.value }))}
              disabled={disabled || saving}
              autoComplete="off"
            />
          </label>
          <label className="field">
            發票日期 <em>必填</em>
            <input
              type="date"
              value={value.supplierInvoiceDate}
              onChange={event => setValue(current => ({ ...current, supplierInvoiceDate: event.target.value }))}
              disabled={disabled || saving}
            />
          </label>
          <div className="field">
            採購單／收貨單
            <div className="ap-source-fact">
              <strong>{value.purchaseOrderNumber} / {value.goodsReceiptNumber}</strong>
              <small>來源建立後固定；幣別依採購單。</small>
            </div>
          </div>
        </div>
      </section>

      <section className="panel">
        <div className="panel-heading">
          <div><span className="eyebrow">INVOICE LINES</span><h2>請款明細</h2><p>未請款品項數量留空；金額按明細計算，稅額請依實際發票輸入。</p></div>
        </div>
        <div className="table-wrap">
          <table className="data-table ap-lines-table">
            <thead><tr><th>品項</th><th>可請款數量</th><th>請款數量</th><th>單價</th><th>未稅金額</th><th>稅額</th><th>含稅金額</th></tr></thead>
            <tbody>
              {value.lines.map((line, index) => {
                const amounts = lineAmounts(line);
                return (
                  <tr key={line.goodsReceiptItemId}>
                    <td><strong>{line.skuNumber || '未提供料號'}</strong><small className="table-subtext">{line.skuName || '未提供品名'}</small></td>
                    <td>{line.availableQuantity.toLocaleString('zh-TW')} {line.unit}</td>
                    <td><input aria-label={`${line.skuNumber || '品項'}請款數量`} type="number" min="0" max={line.availableQuantity} step="0.01" value={line.quantity} onChange={event => updateLine(index, 'quantity', event.target.value)} disabled={disabled || saving} /></td>
                    <td><input aria-label={`${line.skuNumber || '品項'}單價`} type="number" min="0" step="0.0001" value={line.unitPrice} onChange={event => updateLine(index, 'unitPrice', event.target.value)} disabled={disabled || saving} /></td>
                    <td>{amounts.net.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                    <td><input aria-label={`${line.skuNumber || '品項'}稅額`} type="number" min="0" step="0.01" value={line.taxAmount} onChange={event => updateLine(index, 'taxAmount', event.target.value)} disabled={disabled || saving || Number(line.quantity) <= 0} /></td>
                    <td>{amounts.total.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div className="totals-row ap-totals">
          <span>未稅金額<strong>{value.currency} {netTotal.toLocaleString('zh-TW', { minimumFractionDigits: 2 })}</strong></span>
          <span>稅額<strong>{value.currency} {taxTotal.toLocaleString('zh-TW', { minimumFractionDigits: 2 })}</strong></span>
          <span className="total-emphasis">含稅總額<strong>{value.currency} {total.toLocaleString('zh-TW', { minimumFractionDigits: 2 })}</strong></span>
        </div>
        {validationErrors.length > 0 && (
          <div className="notice notice-error ap-validation" role="alert">
            <ul>{validationErrors.map(error => <li key={error}>{error}</li>)}</ul>
          </div>
        )}
        <div className="notice notice-info ap-info">
          發票總額由明細加總；系統不自動推算稅率或放寬金額差異。後端會再次檢查來源、發票唯一性及金額。
        </div>
        <div className="form-actions">
          <span>請確認發票資料及 PO／收貨來源正確後再儲存。</span>
          <button type="submit" className="button button-primary" disabled={disabled || saving}>
            {saving ? '正在儲存…' : submitLabel}
          </button>
        </div>
      </section>
    </form>
  );
}
