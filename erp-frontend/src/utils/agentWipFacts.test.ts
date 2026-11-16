import { describe, expect, it } from 'vitest';
import { toAgentWipFactPresentation } from './agentWipFacts';

describe('toAgentWipFactPresentation', () => {
  it('uses the server display value and keeps the UOM separate', () => {
    const presentation = toAgentWipFactPresentation({
      factId: 'wo/operation/30/good-quantity',
      value: Number('123456789.123456789'),
      displayValue: '123456789.123456789',
      sourceTable: 'WipReports',
      sourceField: 'CompletedQty',
      asOf: '2026-11-03T03:00:00Z',
      unit: 'KG',
      notApplicableReason: null,
      dataClassification: 'internal',
    });

    expect(presentation.value).toBe('123456789.123456789');
    expect(presentation.unit).toBe('KG');
    expect(presentation.factId).toBe('wo/operation/30/good-quantity');
    expect(presentation.source).toBe('WipReports.CompletedQty');
  });

  it('does not reformat numeric facts in the browser locale', () => {
    const presentation = toAgentWipFactPresentation({
      factId: 'wo/operation/30/sequence',
      value: 30,
      displayValue: '30',
      sourceTable: 'WorkOrderOperations',
      sourceField: 'OperationSeq',
      asOf: null,
      unit: null,
      notApplicableReason: null,
      dataClassification: 'internal',
    });

    expect(presentation.value).toBe('30');
    expect(presentation.unit).toBeNull();
  });

  it('renders null numeric values as not applicable without inventing a value', () => {
    const presentation = toAgentWipFactPresentation({
      factId: 'wo/operation/30/defect-rate-percent',
      value: null,
      displayValue: null,
      sourceTable: 'WipReports',
      sourceField: 'DefectRate',
      asOf: null,
      unit: '%',
      notApplicableReason: 'zero_denominator',
      dataClassification: 'internal',
    });

    expect(presentation.value).toBe('不適用');
    expect(presentation.unit).toBe('%');
    expect(presentation.notApplicableReason).toBe('zero_denominator');
  });
});
