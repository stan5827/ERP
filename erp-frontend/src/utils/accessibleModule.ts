import type { ModuleAccess } from '../contexts/RoleContextStore';
import type { ModuleId } from '../types';

const defaultModuleOrder: ModuleId[] = [
  'dashboard',
  'kpi',
  'systemsettings',
  'uom',
  'engineering',
  'org',
  'sales',
  'customers',
  'mps',
  'rccp',
  'mrp',
  'procurement',
  'suppliers',
  'supplierportal',
  'inventory',
  'barcode',
  'manufacturing',
  'aps',
  'tpm',
  'finance',
  'budget',
  'actual_costing',
  'costcenter',
  'hr',
  'quality',
  'ai',
  'agentflows',
  'aidecision',
  'aischeduling',
  'assets',
  'rd',
  'esg',
  'collaboration',
  'calendar',
  'ediexport',
];

const permissionModuleForPage: Partial<Record<ModuleId, string>> = {
  customers: 'sales',
};

export function resolveAccessibleModule(
  activeModule: ModuleId,
  moduleAccess: ModuleAccess[],
  isAdmin: boolean,
): ModuleId | null {
  if (isAdmin || activeModule === 'profile')
    return activeModule;

  const hasAccess = (moduleId: ModuleId) => {
    const permissionModuleId = permissionModuleForPage[moduleId] ?? moduleId;
    if (moduleId === 'customers') {
      const hasScopedSalesAccess = moduleAccess.some(access => access.moduleId.startsWith('sales:'));
      if (hasScopedSalesAccess)
        return moduleAccess.some(access =>
          access.moduleId === 'sales:customers' && access.permissions.length > 0
        );
    }

    return moduleAccess.some(module =>
      module.moduleId === permissionModuleId && module.permissions.length > 0
    );
  };

  if (hasAccess(activeModule))
    return activeModule;

  return defaultModuleOrder.find(hasAccess) ?? null;
}
