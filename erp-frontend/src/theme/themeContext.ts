import { createContext, useContext } from 'react';
import type { ThemeName, Theme } from './themes';

export interface ThemeContextType {
  themeName: ThemeName;
  theme: Theme;
  customPrimaryColor: string | null;
  customAccentColor: string | null;
  setTheme: (name: ThemeName) => void;
  setCustomColors: (primary?: string, accent?: string) => void;
  isLoading: boolean;
}

export const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
}
