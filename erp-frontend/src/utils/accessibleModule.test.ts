import { describe, expect, it } from 'vitest';
import { resolveAccessibleModule } from './accessibleModule';

describe('resolveAccessibleModule', () => {
  it('keeps the active module when the user has access', () => {
    expect(resolveAccessibleModule(
      'inventory',
      [{ moduleId: 'inventory', permissions: ['view'] }],
      false,
    )).toBe('inventory');
  });

  it('sends a user without dashboard access to the first granted module', () => {
    expect(resolveAccessibleModule(
      'dashboard',
      [{ moduleId: 'inventory', permissions: ['view'] }],
      false,
    )).toBe('inventory');
  });

  it('allows the customer page through the sales permission', () => {
    expect(resolveAccessibleModule(
      'customers',
      [{ moduleId: 'sales', permissions: ['view'] }],
      false,
    )).toBe('customers');
  });

  it('keeps transfer-only sales access away from customer management', () => {
    const access = [
      { moduleId: 'sales', permissions: ['view'] },
      { moduleId: 'sales:workordertransfer', permissions: ['view', 'execute'] },
    ];

    expect(resolveAccessibleModule('sales', access, false)).toBe('sales');
    expect(resolveAccessibleModule('customers', access, false)).toBe('sales');
  });

  it('keeps dashboard as the administrator home without module grants', () => {
    expect(resolveAccessibleModule('dashboard', [], true)).toBe('dashboard');
  });

  it('returns no page when the user has no module grants', () => {
    expect(resolveAccessibleModule('dashboard', [], false)).toBeNull();
  });
});
