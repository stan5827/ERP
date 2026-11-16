import { createContext } from 'react';

export interface ModuleAccess {
  moduleId: string;
  permissions: string[];
}

export interface RoleContextType {
  currentRole: string | null;
  permissions: string[];
  moduleAccess: ModuleAccess[];
  isLoading: boolean;
  hasModule: (moduleId: string) => boolean;
  hasPermission: (moduleId: string, action: string) => boolean;
  refreshPermissions: () => Promise<void>;
}

export const RoleContext = createContext<RoleContextType | undefined>(undefined);
