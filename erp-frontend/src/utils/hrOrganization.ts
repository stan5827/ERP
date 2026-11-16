export interface DepartmentTypeSource {
  deptType?: string;
}

export interface JobTitleDepartmentSource {
  id: string;
  titleCode: string;
  titleName: string;
  applicableDeptType?: string;
}

export function getApplicableJobTitles<T extends JobTitleDepartmentSource>(
  titles: T[],
  department?: DepartmentTypeSource
): T[] {
  if (!department) return [];

  const departmentType = department.deptType?.trim().toLowerCase();
  return titles.filter(title => {
    const applicableDepartmentType = title.applicableDeptType?.trim().toLowerCase();
    return !applicableDepartmentType || applicableDepartmentType === departmentType;
  });
}
