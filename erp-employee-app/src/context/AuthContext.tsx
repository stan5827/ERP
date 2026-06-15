import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { API_BASE_URL, APP_CONFIG } from '../constants/config';

interface User {
  userId: string;
  username: string;
  email?: string;
  token: string;
  expiresAt: string;
  roles: string[];
  permissions: string[];
  employeeId?: string;
  employeeName?: string;
}

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadStoredUser();
  }, []);

  const loadStoredUser = async () => {
    try {
      const token = await AsyncStorage.getItem(APP_CONFIG.TOKEN_KEY);
      const userData = await AsyncStorage.getItem(APP_CONFIG.USER_KEY);
      
      if (token && userData) {
        const parsedUser = JSON.parse(userData);
        const expiresAt = new Date(parsedUser.expiresAt);
        const now = new Date();
        
        if (expiresAt > now) {
          setUser(parsedUser);
        } else {
          await logout();
        }
      }
    } catch (error) {
      console.error('Failed to load stored user:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (username: string, password: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });

      const data = await response.json();

      if (data.success && data.data) {
        const userData = {
          ...data.data,
          employeeId: data.data.employeeId,
          employeeName: data.data.employeeName,
        };
        
        await AsyncStorage.setItem(APP_CONFIG.TOKEN_KEY, data.data.token);
        await AsyncStorage.setItem(APP_CONFIG.USER_KEY, JSON.stringify(userData));
        
        setUser(userData);
      } else {
        throw new Error(data.message || '登入失敗');
      }
    } catch (error: any) {
      throw new Error(error.message || '網路錯誤，請稍後再試');
    }
  };

  const logout = async () => {
    try {
      await AsyncStorage.removeItem(APP_CONFIG.TOKEN_KEY);
      await AsyncStorage.removeItem(APP_CONFIG.USER_KEY);
      setUser(null);
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};