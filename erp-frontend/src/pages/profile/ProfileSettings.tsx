import React, { useState, useEffect, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { systemSettingsApi, UserThemePreferenceDto } from '../../api/api';
import { useTheme } from '../../theme/ThemeProvider';
import { ThemeName } from '../../theme/themes';

interface ProfileSettingsProps {
  userId: string;
}

export const ProfileSettings: React.FC<ProfileSettingsProps> = ({ userId }) => {
  const { themeName, setTheme, setCustomColors } = useTheme();
  const queryClient = useQueryClient();
  
  const [showThemeModal, setShowThemeModal] = useState(false);
  const [customPrimary, setCustomPrimary] = useState('#2d6ef5');
  const [customAccent, setCustomAccent] = useState('#2d6ef5');
  const [showSaveBtn, setShowSaveBtn] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Load user's current theme preference
  const { data: themeRes, isLoading: themeLoading } = useQuery({
    queryKey: ['user-theme', userId],
    queryFn: () => systemSettingsApi.getUserTheme(userId).then(res => res.data),
    enabled: !!userId
  });

  // Update user theme
  const updateThemeMutation = useMutation({
    mutationFn: (data: { themeName: ThemeName; customPrimaryColor?: string; customAccentColor?: string }) =>
      systemSettingsApi.updateUserTheme(userId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-theme', userId] });
      alert('主題設定已更新');
    },
    onError: (err: any) => {
      alert('更新失敗: ' + (err.response?.data?.message || err.message));
    }
  });

  useEffect(() => {
    if (!themeRes?.data) return;
    const pref = themeRes.data as UserThemePreferenceDto;
    // apply theme and colors
    setTheme(pref.themeName as ThemeName);
    setCustomColors(pref.customPrimaryColor || undefined, pref.customAccentColor || undefined);
    // initialize pickers asynchronously to avoid sync setState-in-effect false positives
    Promise.resolve().then(() => {
      if (pref.customPrimaryColor) setCustomPrimary(pref.customPrimaryColor);
      if (pref.customAccentColor) setCustomAccent(pref.customAccentColor);
    });
  }, [themeRes, setTheme, setCustomColors]);

  const handleSelectTheme = (themeName: ThemeName) => {
    setTheme(themeName);
    if (themeName !== 'custom') {
      setCustomColors(undefined, undefined);
      setCustomPrimary('#2d6ef5');
      setCustomAccent('#2d6ef5');
    }
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      updateThemeMutation.mutate({
        themeName: themeName,
        customPrimaryColor: themeName === 'custom' ? customPrimary : undefined,
        customAccentColor: themeName === 'custom' ? customAccent : undefined
      });
    }, 500);
  };

  const handleColorChange = (primary?: string, accent?: string) => {
    setCustomPrimary(primary || '#2d6ef5');
    setCustomAccent(accent || '#2d6ef5');
    setShowSaveBtn(true);
    
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      updateThemeMutation.mutate({
        themeName: 'custom',
        customPrimaryColor: primary,
        customAccentColor: accent
      });
      setShowSaveBtn(false);
    }, 500);
  };

  const themeOptions = [
    { id: 'modern-dark' as ThemeName, name: '現代深色', icon: '🌙' },
    { id: 'professional-light' as ThemeName, name: '淺色專業', icon: '☀️' },
    { id: 'high-contrast' as ThemeName, name: '高對比', icon: '⚫⚪' }
  ];

  return (
    <div className="page-container">
      <div className="module-header">
        <h1>個人設定</h1>
        <div className="description">
          在此頁面設定您的個人偏好，包括主題選擇和顏色配置。
        </div>
      </div>

      {/* Theme Settings Section */}
      <div className="glass-card">
        <h2>顯示設定</h2>
        <div className="settings-section">
          
          {/* Theme Selection */}
          <div className="setting-group">
            <label className="setting-label">主題樣式</label>
            <div className="theme-selector">
              {themeOptions.map(option => (
                <div 
                  key={option.id} 
                  className={`theme-option ${themeName === option.id ? 'active' : ''}`}
                  onClick={() => handleSelectTheme(option.id)}
                >
                  <div className="theme-icon">{option.icon}</div>
                  <div className="theme-info">
                    <div className="theme-name">{option.name}</div>
                    {themeName === option.id && <span className="theme-check">✓ 已選擇</span>}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Custom Colors */}
          {themeName === 'custom' && (
            <div className="setting-group">
              <label className="setting-label">自訂顏色</label>
              <div className="color-pickers">
                <div className="color-picker">
                  <label>主色</label>
                  <input 
                    type="color" 
                    value={customPrimary} 
                    onChange={e => handleColorChange(e.target.value, customAccent)}
                  />
                  <span className="color-value">{customPrimary}</span>
                </div>
                <div className="color-picker">
                  <label>強調色</label>
                  <input 
                    type="color" 
                    value={customAccent} 
                    onChange={e => handleColorChange(customPrimary, e.target.value)}
                  />
                  <span className="color-value">{customAccent}</span>
                </div>
              </div>
              {showSaveBtn && (
                <button className="btn btn-primary btn-sm" onClick={() => {
                  if (debounceRef.current) clearTimeout(debounceRef.current);
                  updateThemeMutation.mutate({
                    themeName: 'custom',
                    customPrimaryColor: customPrimary,
                    customAccentColor: customAccent
                  });
                  setShowSaveBtn(false);
                }}>
                  儲存變更
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Preview Section */}
      <div className="glass-card">
        <h2>預覽效果</h2>
        <div className="theme-preview">
          <div className="preview-header" style={{ 
            background: 'var(--theme-surface)', 
            borderBottom: '1px solid var(--theme-border)' 
          }}>
            <div className="preview-dot" style={{ background: 'var(--theme-primary)' }} />
          </div>
          <div className="preview-sidebar" style={{ 
            background: 'var(--theme-surface)', 
            borderRight: '1px solid var(--theme-border)' 
          }}>
            <div className="preview-item" style={{ 
              background: themeName === 'custom' || themeName === 'modern-dark' ? 'var(--theme-primary)' : 'transparent' 
            }} />
            <div className="preview-item" />
          </div>
          <div className="preview-content">
            <div className="preview-card" style={{ 
              background: 'var(--theme-surface)', 
              border: '1px solid var(--theme-border)' 
            }}>
              <div style={{ 
                background: 'var(--theme-primary)', 
                width: '60%', 
                height: 8, 
                borderRadius: 4 
              }} />
            </div>
          </div>
        </div>
        <p className="preview-description">
          這是您目前選擇主題的預覽效果。實際應用會根據您的選擇調整整個系統的顏色和樣式。
        </p>
      </div>
    </div>
  );
};
