import { InMemoryWebStorage, UserManager, WebStorageStateStore } from 'oidc-client-ts';

const authority = import.meta.env.VITE_KEYCLOAK_AUTHORITY?.trim();
const clientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID?.trim();
const scope = import.meta.env.VITE_KEYCLOAK_SCOPE?.trim() || 'openid profile email';
const isAllowedAuthority = (() => {
  if (!authority)
    return false;
  try {
    const url = new URL(authority);
    return url.protocol === 'https:'
      || (url.protocol === 'http:' && ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname));
  } catch {
    return false;
  }
})();
const oidcSettings = authority && clientId && isAllowedAuthority
  ? {
      authority,
      client_id: clientId,
    }
  : null;

export const oidcConfigurationError =
  !authority || !clientId
    ? '尚未設定 Keycloak Authority 與 Portal Client ID。請設定 Portal 環境變數後重新啟動。'
    : !isAllowedAuthority
      ? 'Keycloak Authority 必須使用 HTTPS；僅允許 localhost 開發環境使用 HTTP。'
    : null;

export const userManager = oidcSettings
  ? new UserManager({
      ...oidcSettings,
      redirect_uri: `${window.location.origin}/auth/callback`,
      post_logout_redirect_uri: `${window.location.origin}/auth/logout-callback`,
      response_type: 'code',
      response_mode: 'query',
      scope,
      disablePKCE: false,
      automaticSilentRenew: false,
      monitorSession: false,
      loadUserInfo: false,
      filterProtocolClaims: true,
      stateStore: new WebStorageStateStore({ store: window.sessionStorage }),
      userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
    })
  : null;
