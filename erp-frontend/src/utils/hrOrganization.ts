export interface DepartmentTypeSource {
  deptType?: string;
}

export interface JobTitleDepartmentSource {
  id: string;
  titleCode: string;
  titleName: string;
  applicableDeptType?: string;
}

function normalizeDepartmentType(value?: string): string {
  const normalized = value?.trim().toLowerCase() ?? '';
  if (normalized === 'finance' || normalized === 'accounting' || normalized === '財務' || normalized === '會計') {
    return 'admin';
  }
  return normalized;
}

export function getApplicableJobTitles<T extends JobTitleDepartmentSource>(
  titles: T[],
  department?: DepartmentTypeSource
): T[] {
  if (!department) return [];

  const departmentType = normalizeDepartmentType(department.deptType);
  return titles.filter(title => {
    const applicableDepartmentType = normalizeDepartmentType(title.applicableDeptType);
    return !applicableDepartmentType || applicableDepartmentType === departmentType;
  });
}
