import { useContext } from 'react';
import { RoleContext } from './RoleContextStore';

export function useRole() {
  const context = useContext(RoleContext);
  if (!context)
    throw new Error('useRole must be used within RoleProvider');
  return context;
}

export function usePermission() {
  const context = useContext(RoleContext);
  if (!context)
    throw new Error('usePermission must be used within RoleProvider');
  return {
    hasModule: context.hasModule,
    hasPermission: context.hasPermission,
    currentRole: context.currentRole,
    permissions: context.permissions,
    moduleAccess: context.moduleAccess
  };
}
