import { describe, expect, it } from 'vitest';
import { getApplicableJobTitles } from './hrOrganization';

describe('HR organization title options', () => {
  it('filters titles by department type and includes titles applicable to all departments', () => {
    const titles = [
      { id: 'production-title', titleCode: 'OP', titleName: '作業員', applicableDeptType: 'production' },
      { id: 'admin-title', titleCode: 'HR', titleName: '人資專員', applicableDeptType: 'admin' },
      { id: 'generic-title', titleCode: 'MGR', titleName: '經理' },
    ];

    expect(getApplicableJobTitles(titles, { deptType: 'Production' }).map(title => title.id))
      .toEqual(['production-title', 'generic-title']);
  });

  it('returns no department-specific titles until a department is selected', () => {
    expect(getApplicableJobTitles([
      { id: 'generic-title', titleCode: 'MGR', titleName: '經理' },
    ])).toEqual([]);
  });
});
