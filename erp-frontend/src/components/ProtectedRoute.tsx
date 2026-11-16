import React, { ReactNode } from 'react';
import { useRole } from '../contexts/useRole';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredRole?: string | string[];
  requiredModule?: string;
  requiredPermission?: { module: string; action: string };
}

export function ProtectedRoute({ 
  children, 
  requiredRole, 
  requiredModule,
  requiredPermission 
}: ProtectedRouteProps) {
  const { currentRole, hasModule, hasPermission, isLoading } = useRole();

  if (isLoading) {
    return null;
  }

  if (requiredRole) {
    const roles = Array.isArray(requiredRole) ? requiredRole : [requiredRole];
    if (!currentRole || !roles.includes(currentRole)) {
      return null;
    }
  }

  if (requiredModule) {
    if (!hasModule(requiredModule)) {
      return null;
    }
  }

  if (requiredPermission) {
    if (!hasPermission(requiredPermission.module, requiredPermission.action)) {
      return null;
    }
  }

  return <>{children}</>;
}