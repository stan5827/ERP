export interface LotQrData {
  skuCode: string;
  lotNumber: string;
  expiryDate: string;
}

export function parseLotQr(rawValue: string): LotQrData {
  const parts = rawValue.trim().split('|').map(part => part.trim());
  if (parts.length !== 3 || parts.some(part => !part)) {
    throw new Error('QR 格式錯誤，應為「料號|批號|YYYY-MM-DD」');
  }

  const [skuCode, lotNumber, expiryDate] = parts;
  if (skuCode.length > 50 || lotNumber.length > 50) {
    throw new Error('QR 料號或批號超過允許長度');
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(expiryDate)) {
    throw new Error('QR 效期格式錯誤，請使用 YYYY-MM-DD');
  }

  const [year, month, day] = expiryDate.split('-').map(Number);
  const parsedDate = new Date(Date.UTC(year, month - 1, day));
  if (
    parsedDate.getUTCFullYear() !== year ||
    parsedDate.getUTCMonth() !== month - 1 ||
    parsedDate.getUTCDate() !== day
  ) {
    throw new Error('QR 效期不是有效日期');
  }

  return { skuCode, lotNumber, expiryDate };
}
