import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Crypto from 'expo-crypto';
import { APP_CONFIG, DEFAULT_TIMEOUT } from '../constants/config';

const ENCRYPTION_KEY_NAME = 'erp_encryption_key';

const getEncryptionKey = async (): Promise<string> => {
  let key = await AsyncStorage.getItem(ENCRYPTION_KEY_NAME);
  if (!key) {
    key = Crypto.randomUUID() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    await AsyncStorage.setItem(ENCRYPTION_KEY_NAME, key);
  }
  return key;
};

const encrypt = async (data: string): Promise<string> => {
  const key = await getEncryptionKey();
  const encrypted = await Crypto.digestStringAsync(
    Crypto.CryptoDigestAlgorithm.SHA256,
    key + data
  );
  return encrypted;
};

const decrypt = async (encryptedData: string, originalData: string): Promise<boolean> => {
  const key = await getEncryptionKey();
  const check = await Crypto.digestStringAsync(
    Crypto.CryptoDigestAlgorithm.SHA256,
    key + originalData
  );
  return check === encryptedData;
};

export const storageService = {
  async setToken(token: string): Promise<void> {
    const encryptedToken = await encrypt(token);
    await AsyncStorage.setItem(APP_CONFIG.TOKEN_KEY, encryptedToken);
  },

  async getToken(): Promise<string | null> {
    const encryptedToken = await AsyncStorage.getItem(APP_CONFIG.TOKEN_KEY);
    if (!encryptedToken) return null;
    
    try {
      const tokens = await AsyncStorage.getItem('erp_token_raw');
      if (tokens) {
        const isValid = await decrypt(encryptedToken, tokens);
        if (isValid) return tokens;
      }
    } catch (e) {
      console.error('Token decode error:', e);
    }
    return null;
  },

  async removeToken(): Promise<void> {
    await AsyncStorage.removeItem(APP_CONFIG.TOKEN_KEY);
    await AsyncStorage.removeItem('erp_token_raw');
  },

  async setUser(user: object): Promise<void> {
    await AsyncStorage.setItem(APP_CONFIG.USER_KEY, JSON.stringify(user));
  },

  async getUser(): Promise<object | null> {
    const userData = await AsyncStorage.getItem(APP_CONFIG.USER_KEY);
    return userData ? JSON.parse(userData) : null;
  },

  async removeUser(): Promise<void> {
    await AsyncStorage.removeItem(APP_CONFIG.USER_KEY);
  },

  async clearAll(): Promise<void> {
    await this.removeToken();
    await this.removeUser();
  },
};

export const offlineStorage = {
  async saveOfflineData(key: string, data: unknown): Promise<void> {
    const offlineData = {
      data,
      timestamp: Date.now(),
      synced: false,
    };
    await AsyncStorage.setItem(`${APP_CONFIG.OFFLINE_KEY}_${key}`, JSON.stringify(offlineData));
  },

  async getOfflineData(key: string): Promise<{ data: unknown; timestamp: number } | null> {
    const cached = await AsyncStorage.getItem(`${APP_CONFIG.OFFLINE_KEY}_${key}`);
    if (!cached) return null;
    
    try {
      const parsed = JSON.parse(cached);
      return { data: parsed.data, timestamp: parsed.timestamp };
    } catch {
      return null;
    }
  },

  async getUnsyncedData(): Promise<Array<{ key: string; data: unknown; timestamp: number }>> {
    const keys = await AsyncStorage.getAllKeys();
    const unsynced = [];
    
    for (const key of keys) {
      if (key.startsWith(APP_CONFIG.OFFLINE_KEY)) {
        const data = await AsyncStorage.getItem(key);
        if (data) {
          const parsed = JSON.parse(data);
          if (!parsed.synced) {
            const cleanKey = key.replace(`${APP_CONFIG.OFFLINE_KEY}_`, '');
            unsynced.push({ key: cleanKey, data: parsed.data, timestamp: parsed.timestamp });
          }
        }
      }
    }
    
    return unsynced;
  },

  async markSynced(key: string): Promise<void> {
    const cached = await AsyncStorage.getItem(`${APP_CONFIG.OFFLINE_KEY}_${key}`);
    if (cached) {
      const parsed = JSON.parse(cached);
      parsed.synced = true;
      await AsyncStorage.setItem(`${APP_CONFIG.OFFLINE_KEY}_${key}`, JSON.stringify(parsed));
    }
  },

  async clearOfflineData(key?: string): Promise<void> {
    if (key) {
      await AsyncStorage.removeItem(`${APP_CONFIG.OFFLINE_KEY}_${key}`);
    } else {
      const keys = await AsyncStorage.getAllKeys();
      for (const k of keys) {
        if (k.startsWith(APP_CONFIG.OFFLINE_KEY)) {
          await AsyncStorage.removeItem(k);
        }
      }
    }
  },
};

export default { storageService, offlineStorage };
