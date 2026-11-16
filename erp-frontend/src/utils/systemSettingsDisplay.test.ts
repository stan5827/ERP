import { describe, expect, it } from 'vitest';
import { getRoleDisplayName, matchesSystemUserSearch } from './systemSettingsDisplay';

describe('system settings display helpers', () => {
  it('matches users by Chinese employee name and employee number', () => {
    const user = { username: 'stan000', email: 'stan@example.com' };
    const employeeLabel = 'E013 - 李威';

    expect(matchesSystemUserSearch(user, employeeLabel, '李威')).toBe(true);
    expect(matchesSystemUserSearch(user, employeeLabel, 'E013')).toBe(true);
    expect(matchesSystemUserSearch(user, employeeLabel, '不存在')).toBe(false);
  });

  it('localizes known role names independent of spacing and separators', () => {
    const roleNames = { 'sales manager': '業務主管' };

    expect(getRoleDisplayName('Sales Manager', roleNames)).toBe('業務主管');
    expect(getRoleDisplayName('sales_manager', roleNames)).toBe('業務主管');
    expect(getRoleDisplayName('Custom Role', roleNames)).toBe('Custom Role');
  });
});
