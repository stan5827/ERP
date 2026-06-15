export type ThemeName = 'modern-dark' | 'professional-light' | 'high-contrast' | 'corporate-blue' | 'forest-green' | 'royal-purple' | 'sunset-orange' | 'slate-gray' | 'custom';

export interface Theme {
  name: ThemeName;
  displayName: string;
  description: string;
  isDefault: boolean;
  colors: ThemeColors;
}

export interface ThemeColors {
  primary: string;
  accent: string;
  background: string;
  surface: string;
  text: string;
  border: string;
}

export const themes: Record<ThemeName, Theme> = {
  'modern-dark': {
    name: 'modern-dark',
    displayName: '現代深色',
    description: '目前使用的深色玻璃風格',
    isDefault: true,
    colors: {
      primary: '#2d6ef5',
      accent: '#2d6ef5',
      background: '#04080f',
      surface: '#0d1829',
      text: '#e8eef8',
      border: '#1a3050',
    },
  },
  'professional-light': {
    name: 'professional-light',
    displayName: '淺色專業',
    description: '專業的淺色配色',
    isDefault: false,
    colors: {
      primary: '#1d5cd4',
      accent: '#1d5cd4',
      background: '#f5f7fa',
      surface: '#ffffff',
      text: '#1a202c',
      border: '#e2e8f0',
    },
  },
  'high-contrast': {
    name: 'high-contrast',
    displayName: '高對比',
    description: '高對比度黑白配色',
    isDefault: false,
    colors: {
      primary: '#0066ff',
      accent: '#ffff00',
      background: '#000000',
      surface: '#1a1a1a',
      text: '#ffffff',
      border: '#333333',
    },
  },
  'corporate-blue': {
    name: 'corporate-blue',
    displayName: '企業藍',
    description: '穩重專業的企業藍色主題',
    isDefault: false,
    colors: {
      primary: '#1e3a8a',
      accent: '#3b82f6',
      background: '#0f172a',
      surface: '#1e293b',
      text: '#f8fafc',
      border: '#334155',
    },
  },
  'forest-green': {
    name: 'forest-green',
    displayName: '森林綠',
    description: '自然清新的森林綠色主題',
    isDefault: false,
    colors: {
      primary: '#059669',
      accent: '#10b981',
      background: '#064e3b',
      surface: '#065f46',
      text: '#f0fdf4',
      border: '#6b7280',
    },
  },
  'royal-purple': {
    name: 'royal-purple',
    displayName: '皇家紫',
    description: '尊貴典雅的皇家紫色主題',
    isDefault: false,
    colors: {
      primary: '#7c3aed',
      accent: '#a78bfa',
      background: '#1e1b4b',
      surface: '#2d1b69',
      text: '#faf5ff',
      border: '#5b21b6',
    },
  },
  'sunset-orange': {
    name: 'sunset-orange',
    displayName: '夕陽橙',
    description: '活力熱情的夕陽橙色主題',
    isDefault: false,
    colors: {
      primary: '#ea580c',
      accent: '#fb923c',
      background: '#431407',
      surface: '#581c0c',
      text: '#fff7ed',
      border: '#9c4221',
    },
  },
  'slate-gray': {
    name: 'slate-gray',
    displayName: '石板灰',
    description: '現代簡約的石板灰色主題',
    isDefault: false,
    colors: {
      primary: '#475569',
      accent: '#94a3b8',
      background: '#0f172a',
      surface: '#1e293b',
      text: '#f1f5f9',
      border: '#64748b',
    },
  },
  'custom': {
    name: 'custom',
    displayName: '自訂',
    description: '自訂主題顏色',
    isDefault: false,
    colors: {
      primary: '#2d6ef5',
      accent: '#2d6ef5',
      background: '#04080f',
      surface: '#0d1829',
      text: '#e8eef8',
      border: '#1a3050',
    },
  },
};

export function getTheme(name: ThemeName): Theme {
  return themes[name] || themes['modern-dark'];
}

export function applyTheme(theme: Theme | ThemeColors): void {
  const root = document.documentElement;
  const colors = 'colors' in theme ? theme.colors : theme;
  
  // Map theme colors to actual CSS variables used in UI
  root.style.setProperty('--cobalt-600', colors.primary);
  root.style.setProperty('--cobalt-500', colors.primary);
  root.style.setProperty('--cobalt-400', colors.primary);
  root.style.setProperty('--cobalt-300', colors.primary);
  root.style.setProperty('--cobalt-200', adjustColor(colors.primary, 30));
  root.style.setProperty('--cobalt-100', adjustColor(colors.primary, 50));
  root.style.setProperty('--cobalt-glow', colors.primary + '26');
  root.style.setProperty('--cobalt-glow-strong', colors.primary + '59');

  root.style.setProperty('--slate-100', colors.text);
  root.style.setProperty('--slate-200', adjustColor(colors.text, -20));
  root.style.setProperty('--slate-300', adjustColor(colors.text, -40));
  root.style.setProperty('--slate-400', adjustColor(colors.text, -60));
  root.style.setProperty('--slate-500', adjustColor(colors.text, -70));
  root.style.setProperty('--slate-600', adjustColor(colors.text, -80));

  root.style.setProperty('--navy-950', colors.background);
  root.style.setProperty('--navy-900', adjustColor(colors.background, 5));
  root.style.setProperty('--navy-850', adjustColor(colors.background, 10));
  root.style.setProperty('--navy-800', colors.surface);
  root.style.setProperty('--navy-750', adjustColor(colors.surface, 5));
  root.style.setProperty('--navy-700', adjustColor(colors.surface, 10));
  root.style.setProperty('--navy-600', adjustColor(colors.surface, 20));
  root.style.setProperty('--navy-500', adjustColor(colors.surface, 30));

  root.style.setProperty('--glass-bg', colors.surface + 'cc');
  root.style.setProperty('--glass-border', colors.border);
  root.style.setProperty('--glass-border-hover', colors.border);

  // Set custom theme variables
  root.style.setProperty('--theme-primary', colors.primary);
  root.style.setProperty('--theme-accent', colors.accent);
  root.style.setProperty('--theme-background', colors.background);
  root.style.setProperty('--theme-surface', colors.surface);
  root.style.setProperty('--theme-text', colors.text);
  root.style.setProperty('--theme-border', colors.border);
}

function adjustColor(hex: string, percent: number): string {
  const num = parseInt(hex.replace('#', ''), 16);
  const amt = Math.round(2.55 * percent);
  const R = (num >> 16) + amt;
  const G = (num >> 8 & 0x00FF) + amt;
  const B = (num & 0x0000FF) + amt;
  return '#' + (0x1000000 + 
    (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 + 
    (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 + 
    (B < 255 ? B < 1 ? 0 : B : 255)
  ).toString(16).slice(1);
}
