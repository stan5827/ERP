export type ProcessQualityView = 'fqc' | 'records' | 'templates';

export interface ProcessQualityViewPermissions {
  qualityView: boolean;
  engineeringView: boolean;
  inventoryView: boolean;
}

export function getProcessQualityViews(
  permissions: ProcessQualityViewPermissions,
): ProcessQualityView[] {
  if (!permissions.qualityView)
    return [];

  const views: ProcessQualityView[] = ['fqc', 'records'];
  if (permissions.engineeringView && permissions.inventoryView)
    views.push('templates');
  return views;
}
