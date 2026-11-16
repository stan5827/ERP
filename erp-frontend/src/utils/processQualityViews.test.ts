import { describe, expect, it } from 'vitest';
import { getProcessQualityViews } from './processQualityViews';

describe('getProcessQualityViews', () => {
  it('does not expose process-quality views without quality view permission', () => {
    expect(getProcessQualityViews({
      qualityView: false,
      engineeringView: true,
      inventoryView: true,
    })).toEqual([]);
  });

  it('keeps FQC and inspection records available to quality inspectors without master-data permissions', () => {
    expect(getProcessQualityViews({
      qualityView: true,
      engineeringView: false,
      inventoryView: false,
    })).toEqual(['fqc', 'records']);
  });

  it('exposes template management only when its engineering and inventory lookups are available', () => {
    expect(getProcessQualityViews({
      qualityView: true,
      engineeringView: true,
      inventoryView: true,
    })).toEqual(['fqc', 'records', 'templates']);
    expect(getProcessQualityViews({
      qualityView: true,
      engineeringView: true,
      inventoryView: false,
    })).toEqual(['fqc', 'records']);
  });
});
