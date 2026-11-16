import { describe, expect, it } from 'vitest';
import { parseLotQr } from './lotQr';

describe('parseLotQr', () => {
  it('parses the agreed SKU, lot, and expiry format', () => {
    expect(parseLotQr(' SKU-01 | LOT-001 | 2027-03-09 ')).toEqual({
      skuCode: 'SKU-01',
      lotNumber: 'LOT-001',
      expiryDate: '2027-03-09',
    });
  });

  it.each([
    '',
    'SKU-01|LOT-001',
    'SKU-01|LOT-001|2027-02-29',
    'SKU-01|LOT-001|2027/03/09',
    'SKU-01|LOT-001|2027-03-09|extra',
    'SKU-01||2027-03-09',
  ])('rejects invalid payload %s', value => {
    expect(() => parseLotQr(value)).toThrow();
  });
});
