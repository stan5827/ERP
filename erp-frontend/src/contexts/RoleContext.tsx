import React, { useState, useEffect, useRef, useCallback, ReactNode } from 'react';
import { systemSettingsApi } from '../api/api';
import { RoleContext, type ModuleAccess } from './RoleContextStore';

interface InitialRoleState {
  currentRole: string | null;
  permissions: string[];
  moduleAccess: ModuleAccess[];
  isLoading: boolean;
}

const roleAccessCacheKey = 'erp_role_access';

const isModuleAccessList = (value: unknown): value is ModuleAccess[] =>
  Array.isArray(value) && value.every(item =>
    item !== null
    && typeof item === 'object'
    && typeof item.moduleId === 'string'
    && Array.isArray(item.permissions)
    && item.permissions.every((permission: unknown) => typeof permission === 'string')
  );

const getModuleAccessFromLoginSnapshot = (permissions: unknown): ModuleAccess[] => {
  if (!Array.isArray(permissions) || !permissions.every(permission => typeof permission === 'string'))
    return [];

  const modulePermissions = new Map<string, Set<string>>();
  for (const permission of permissions) {
    const [moduleId, resourceId, resourceAction] = permission.split(':');
    if (!moduleId || moduleId === 'admin.*')
      continue;

    const scopedModuleId = resourceAction ? `${moduleId}:${resourceId}` : moduleId;
    const action = resourceAction ?? resourceId;
    const actions = modulePermissions.get(scopedModuleId) ?? new Set<string>();
    if (resourceAction === 'page') {
      actions.add('view');
    } else if (!resourceAction && (!action || action === 'page')) {
      for (const moduleAction of ['view', 'create', 'edit', 'delete', 'approve', 'export', 'execute'])
        actions.add(moduleAction);
    } else {
      actions.add(action);
    }
    modulePermissions.set(scopedModuleId, actions);
    if (resourceAction && !modulePermissions.has(moduleId))
      modulePermissions.set(moduleId, new Set(['view']));
    else if (resourceAction)
      modulePermissions.get(moduleId)?.add('view');
  }

  return Array.from(modulePermissions, ([moduleId, actions]) => ({
    moduleId,
    permissions: Array.from(actions),
  }));
};

const readInitialRoleState = (): InitialRoleState => {
  const stored = localStorage.getItem('erp_user');
  const token = localStorage.getItem('erp_token');
  if (!stored || !token) {
    sessionStorage.removeItem(roleAccessCacheKey);
    return { currentRole: null, permissions: [], moduleAccess: [], isLoading: false };
  }

  const user = JSON.parse(stored);
  const currentRole = (user.roles?.[0] || 'guest').toLowerCase();
  if (user.mustChangePassword)
    return { currentRole, permissions: [], moduleAccess: [], isLoading: false };

  const loginSnapshot = getModuleAccessFromLoginSnapshot(user.permissions);
  try {
    const rawCache = sessionStorage.getItem(roleAccessCacheKey);
    if (rawCache) {
      const cache: unknown = JSON.parse(rawCache);
      if (cache !== null
        && typeof cache === 'object'
        && 'userId' in cache
        && cache.userId === user.userId
        && 'modules' in cache
        && isModuleAccessList(cache.modules)) {
        const modules = cache.modules.length > 0 ? cache.modules : loginSnapshot;
        return {
          currentRole,
          moduleAccess: modules,
          permissions: modules.flatMap(module => module.permissions),
          isLoading: false,
        };
      }

      sessionStorage.removeItem(roleAccessCacheKey);
    }
  } catch (error) {
    console.error('[RoleContext] Could not restore cached permissions:', error);
    sessionStorage.removeItem(roleAccessCacheKey);
  }

  return {
    currentRole,
    permissions: loginSnapshot.flatMap(module => module.permissions),
    moduleAccess: loginSnapshot,
    isLoading: loginSnapshot.length === 0,
  };
};

export function RoleProvider({ children }: { children: ReactNode }) {
  const [initialState] = useState(readInitialRoleState);
  const [currentRole, setCurrentRole] = useState<string | null>(initialState.currentRole);
  const [permissions, setPermissions] = useState<string[]>(initialState.permissions);
  const [moduleAccess, setModuleAccess] = useState<ModuleAccess[]>(initialState.moduleAccess);
  const [isLoading, setIsLoading] = useState(initialState.isLoading);
  const refreshInFlight = useRef<Promise<void> | null>(null);

  const refreshPermissions = useCallback(async () => {
    if (refreshInFlight.current)
      return refreshInFlight.current;

    const refresh = (async () => {
      const stored = localStorage.getItem('erp_user');
      if (!stored || !localStorage.getItem('erp_token')) {
        sessionStorage.removeItem(roleAccessCacheKey);
        setCurrentRole(null);
        setModuleAccess([]);
        setPermissions([]);
        setIsLoading(false);
        return;
      }

      try {
        const user = JSON.parse(stored);
        if (user.mustChangePassword) {
          setCurrentRole((user.roles?.[0] || 'guest').toLowerCase());
          sessionStorage.removeItem(roleAccessCacheKey);
          setModuleAccess([]);
          setPermissions([]);
          return;
        }

        setCurrentRole((user.roles?.[0] || 'guest').toLowerCase());
        const response = await systemSettingsApi.getCurrentUserModules();
        const body = response.data as unknown;
        const modules = Array.isArray(body)
          ? body
          : body !== null && typeof body === 'object' && 'data' in body
            ? body.data
            : undefined;
        if (!isModuleAccessList(modules))
          throw new Error('Permission API returned an invalid module access response.');

        try {
          sessionStorage.setItem(roleAccessCacheKey, JSON.stringify({
            userId: user.userId,
            modules,
          }));
        } catch (error) {
          console.error('[RoleContext] Could not cache user permissions:', error);
        }
        setModuleAccess(modules);
        setPermissions(modules.flatMap(module => module.permissions));
      } catch (error) {
        if (!localStorage.getItem('erp_token')) {
          sessionStorage.removeItem(roleAccessCacheKey);
          setCurrentRole(null);
          setModuleAccess([]);
          setPermissions([]);
        }
        console.error('[RoleContext] Could not refresh user permissions; keeping the last successful permissions:', error);
      } finally {
        setIsLoading(false);
      }
    })();

    refreshInFlight.current = refresh;
    try {
      await refresh;
    } finally {
      if (refreshInFlight.current === refresh)
        refreshInFlight.current = null;
    }
  }, []);

  useEffect(() => {
    void refreshPermissions();
    const intervalId = window.setInterval(() => void refreshPermissions(), 30000);
    const handleFocus = () => void refreshPermissions();
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible')
        void refreshPermissions();
    };
    window.addEventListener('focus', handleFocus);
    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      window.clearInterval(intervalId);
      window.removeEventListener('focus', handleFocus);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [refreshPermissions]);

  const hasModule = (moduleId: string): boolean =>
    moduleAccess.some(module => module.moduleId === moduleId);

  const hasPermission = (moduleId: string, action: string): boolean =>
    moduleAccess.find(module => module.moduleId === moduleId)?.permissions.includes(action) ?? false;

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
