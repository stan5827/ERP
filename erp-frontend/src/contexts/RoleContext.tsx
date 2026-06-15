import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { systemSettingsApi } from '../api/api';

interface RoleContextType {
  currentRole: string | null;
  permissions: string[];
  moduleAccess: ModuleAccess[];
  isLoading: boolean;
  hasModule: (moduleId: string) => boolean;
  hasPermission: (moduleId: string, action: string) => boolean;
  refreshPermissions: () => Promise<void>;
}

interface ModuleAccess {
  moduleId: string;
  permissions: string[];
}

const RoleContext = createContext<RoleContextType | undefined>(undefined);

export function RoleProvider({ children }: { children: ReactNode }) {
  const [currentRole, setCurrentRole] = useState<string | null>(null);
  const [permissions, setPermissions] = useState<string[]>([]);
  const [moduleAccess, setModuleAccess] = useState<ModuleAccess[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchModuleAccess = async (role: string) => {
    console.log('[RoleContext] fetchModuleAccess called with role:', role);
    console.log('[RoleContext] token in localStorage:', !!localStorage.getItem('erp_token'));
    try {
      console.log('[RoleContext] Calling API with role:', role);
      const res = await systemSettingsApi.getRoleModules(role);
      console.log('[RoleContext] API response:', res);
      console.log('[RoleContext] res.data:', res.data);
      console.log('[RoleContext] res.data.data:', res.data?.data);
      if (res.data?.data) {
        setModuleAccess(res.data.data);
        const allPerms = res.data.data.flatMap(m => m.permissions);
        console.log('[RoleContext] allPerms:', allPerms);
        setPermissions(allPerms);
      }
    } catch (err) {
      console.error('[RoleContext] Failed to fetch module access:', err);
      setModuleAccess([]);
    }
  };

  const refreshPermissions = async () => {
    console.log('[RoleContext] refreshPermissions called');
    const stored = localStorage.getItem('erp_user');
    if (stored) {
      const user = JSON.parse(stored);
      console.log('[RoleContext] user from localStorage:', user);
      console.log('[RoleContext] user.roles:', user.roles);
      const role = (user.roles?.[0] || 'guest').toLowerCase();
      console.log('[RoleContext] role:', role);
      setCurrentRole(role);
      await fetchModuleAccess(role);
    }
    setIsLoading(false);
  };

  useEffect(() => {
    refreshPermissions();
  }, []);

  const hasModule = (moduleId: string): boolean => {
    return moduleAccess.some(m => m.moduleId === moduleId);
  };

  const hasPermission = (moduleId: string, action: string): boolean => {
    const module = moduleAccess.find(m => m.moduleId === moduleId);
    return module?.permissions.includes(action) || false;
  };

  return (
    <RoleContext.Provider value={{
      currentRole,
      permissions,
      moduleAccess,
      isLoading,
      hasModule,
      hasPermission,
      refreshPermissions
    }}>
      {children}
    </RoleContext.Provider>
  );
}

export function useRole() {
  const context = useContext(RoleContext);
  if (!context) {
    throw new Error('useRole must be used within RoleProvider');
  }
  return context;
}

export function usePermission() {
  const context = useContext(RoleContext);
  if (!context) {
    throw new Error('usePermission must be used within RoleProvider');
  }
  return {
    hasModule: context.hasModule,
    hasPermission: context.hasPermission,
    currentRole: context.currentRole,
    permissions: context.permissions,
    moduleAccess: context.moduleAccess
  };
}