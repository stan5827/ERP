import React, { useState, useEffect, ReactNode } from 'react';
import { ThemeName, themes, applyTheme } from './themes';
import { systemSettingsApi, UserThemePreferenceDto } from '../api/api';
import { ThemeContext } from './themeContext';

interface ThemeProviderProps {
  children: ReactNode;
  userId?: string;
}

export function ThemeProvider({ children, userId }: ThemeProviderProps) {
  const [themeName, setThemeName] = useState<ThemeName>('modern-dark');
  const [customPrimaryColor, setCustomPrimaryColor] = useState<string | null>(null);
  const [customAccentColor, setCustomAccentColor] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const theme = themes[themeName] || themes['modern-dark'];

  useEffect(() => {
    async function loadTheme() {
      if (userId) {
        try {
          const res = await systemSettingsApi.getUserTheme(userId);
          if (res.data?.data) {
            const pref = res.data.data as UserThemePreferenceDto;
            setThemeName(pref.themeName as ThemeName);
            setCustomPrimaryColor(pref.customPrimaryColor || null);
            setCustomAccentColor(pref.customAccentColor || null);
          }
        } catch {
          console.log('No user theme found, using default');
        }
      }
      setIsLoading(false);
    }
    loadTheme();
  }, [userId]);

  useEffect(() => {
    const colors = {
      primary: customPrimaryColor || theme.colors.primary,
      accent: customAccentColor || theme.colors.accent,
      background: theme.colors.background,
      surface: theme.colors.surface,
      text: theme.colors.text,
      border: theme.colors.border,
    };
    applyTheme(colors);
    
    document.body.setAttribute('data-theme', themeName);
  }, [themeName, theme, customPrimaryColor, customAccentColor]);

  const setTheme = (name: ThemeName) => {
    setThemeName(name);
    if (name !== 'custom') {
      setCustomPrimaryColor(null);
      setCustomAccentColor(null);
    }
    if (userId) {
      systemSettingsApi.updateUserTheme(userId, {
        themeName: name,
        customPrimaryColor: customPrimaryColor || undefined,
        customAccentColor: customAccentColor || undefined,
      }).catch(console.error);
    }
  };

  const setCustomColors = (primary?: string, accent?: string) => {
    setCustomPrimaryColor(primary || null);
    setCustomAccentColor(accent || null);
    if (userId && primary) {
      systemSettingsApi.updateUserTheme(userId, {
        themeName: 'custom',
        customPrimaryColor: primary,
        customAccentColor: accent,
      }).catch(console.error);
    }
  };

  return (
    <ThemeContext.Provider value={{
      themeName,
      theme,
      customPrimaryColor,
      customAccentColor,
      setTheme,
      setCustomColors,
      isLoading,
    }}>
      {children}
    </ThemeContext.Provider>
  );
}
