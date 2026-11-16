import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import type { User } from 'oidc-client-ts';
import { oidcConfigurationError, userManager } from './oidc';

interface AuthContextValue {
  user: User | null;
  isLoading: boolean;
  configurationError: string | null;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(Boolean(userManager));
  const [initializationError, setInitializationError] = useState<string | null>(null);

  useEffect(() => {
    const manager = userManager;
    if (!manager) {
      setIsLoading(false);
      return;
    }

    let active = true;
    const syncUser = (nextUser: User | null) => {
      if (active)
        setUser(nextUser && !nextUser.expired ? nextUser : null);
    };
    const onUserLoaded = (nextUser: User) => syncUser(nextUser);
    const onUserUnloaded = () => syncUser(null);
    const onAccessTokenExpired = () => syncUser(null);

    manager.events.addUserLoaded(onUserLoaded);
    manager.events.addUserUnloaded(onUserUnloaded);
    manager.events.addAccessTokenExpired(onAccessTokenExpired);

    void manager.getUser()
      .then(syncUser)
      .catch(() => {
        if (active)
          setInitializationError('無法讀取此分頁的登入狀態，請重新登入。');
        syncUser(null);
      })
      .finally(() => {
        if (active)
          setIsLoading(false);
      });

    return () => {
      active = false;
      manager.events.removeUserLoaded(onUserLoaded);
      manager.events.removeUserUnloaded(onUserUnloaded);
      manager.events.removeAccessTokenExpired(onAccessTokenExpired);
    };
  }, []);

  const signIn = useCallback(async () => {
    if (!userManager)
      throw new Error(oidcConfigurationError ?? 'Keycloak 尚未設定。');
    await userManager.signinRedirect();
  }, []);

  const signOut = useCallback(async () => {
    if (!userManager)
      throw new Error(oidcConfigurationError ?? 'Keycloak 尚未設定。');
    await userManager.signoutRedirect();
  }, []);

  const value = useMemo(() => ({
    user,
    isLoading,
    configurationError: oidcConfigurationError || initializationError,
    signIn,
    signOut,
  }), [user, isLoading, initializationError, signIn, signOut]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value)
    throw new Error('useAuth 必須在 AuthProvider 內使用。');
  return value;
}
