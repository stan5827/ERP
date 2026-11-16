import { describe, expect, it } from 'vitest';
import { calculateApLineNetAmount } from '../src/pages/ApSubmissionEditor';

describe('AP submission amount calculation', () => {
  it('rounds line amounts to cents using midpoint-to-even rules', () => {
    expect(calculateApLineNetAmount('1', '1.005')).toBe(1);
    expect(calculateApLineNetAmount('1', '1.015')).toBe(1.02);
  });

  it('calculates quantity and unit price without floating-point cent drift', () => {
    expect(calculateApLineNetAmount('0.1', '0.1')).toBe(0.01);
    expect(calculateApLineNetAmount('3', '0.335')).toBe(1);
    expect(calculateApLineNetAmount('.5', '2.00000')).toBe(1);
  });
});
