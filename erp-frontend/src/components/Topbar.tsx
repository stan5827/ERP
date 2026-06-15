import React, { useState } from 'react';
import {
  Search, Bell, ChevronDown, User, Settings, LogOut, Maximize2, Moon, Sun
} from 'lucide-react';
import type { User as UserType, Notification } from '../types';
import { useTheme } from '../theme/ThemeProvider';
import './Topbar.css';

interface TopbarProps {
  user: UserType;
  notifications: Notification[];
  pageTitle: string;
  pageBreadcrumb?: string;
  onLogout?: () => void;
}

export const Topbar: React.FC<TopbarProps> = ({
  user, notifications, pageTitle, pageBreadcrumb, onLogout
}) => {
  const [showNotifications, setShowNotifications] = useState(false);
  const [showProfile, setShowProfile] = useState(false);
  const [searchFocused, setSearchFocused] = useState(false);
  const [showThemePicker, setShowThemePicker] = useState(false);
  const { themeName, setTheme } = useTheme();

  const unreadCount = notifications.filter(n => !n.read).length;

  const typeColors: Record<string, string> = {
    approval: 'var(--amber-400)',
    alert: 'var(--crimson-400)',
    system: 'var(--cobalt-400)',
    info: 'var(--emerald-400)',
  };

  const themeOptions = [
    { id: 'modern-dark', name: '現代深色', icon: Moon },
    { id: 'professional-light', name: '淺色專業', icon: Sun },
    { id: 'high-contrast', name: '高對比', icon: Moon },
    { id: 'corporate-blue', name: '企業藍', icon: '💼' },
    { id: 'forest-green', name: '森林綠', icon: '🌲' },
    { id: 'royal-purple', name: '皇家紫', icon: '👑' },
    { id: 'sunset-orange', name: '夕陽橙', icon: '🌅' },
    { id: 'slate-gray', name: '石板灰', icon: '🪨' }
  ];

  const handleThemeSelect = (themeId: any) => {
    setTheme(themeId);
    setShowThemePicker(false);
  };

  return (
    <header className="topbar">
      {/* 左側：麵包屑 */}
      <div className="topbar-left">
        <div className="topbar-breadcrumb">
          {pageBreadcrumb && (
            <>
              <span className="breadcrumb-parent">{pageBreadcrumb}</span>
              <span className="breadcrumb-sep">/</span>
            </>
          )}
          <span className="breadcrumb-current">{pageTitle}</span>
        </div>
        <TopbarClock />
      </div>

      {/* 中間：搜尋框 */}
      <div className={`topbar-search ${searchFocused ? 'focused' : ''}`}>
        <Search size={14} className="search-icon" />
        <input
          type="text"
          placeholder="搜尋工單、訂單、品號..."
          className="search-input"
          onFocus={() => setSearchFocused(true)}
          onBlur={() => setSearchFocused(false)}
        />
        <span className="search-shortcut">⌘K</span>
      </div>

      {/* 右側：工具列 */}
      <div className="topbar-right">
        <button className="topbar-icon-btn" title="全螢幕">
          <Maximize2 size={15} />
        </button>

        {/* 主題切換 */}
        <div className="topbar-dropdown-wrap">
          <button
            className="topbar-icon-btn theme-btn"
            onClick={() => { setShowThemePicker(!showThemePicker); setShowNotifications(false); setShowProfile(false); }}
          >
            {themeName === 'modern-dark' ? <Moon size={15} /> : <Sun size={15} />}
          </button>

          {showThemePicker && (
            <div className="topbar-dropdown theme-panel">
              <div className="dropdown-header">
                <span>切換主題</span>
              </div>
              <div className="theme-list">
                {themeOptions.map(option => (
                  <div 
                    key={option.id} 
                    className={`theme-option ${themeName === option.id ? 'active' : ''}`}
                    onClick={() => handleThemeSelect(option.id)}
                  >
                    <div className="theme-icon">
                      <option.icon size={14} />
                    </div>
                    <div className="theme-info">
                      <div className="theme-name">{option.name}</div>
                      {themeName === option.id && <span className="theme-check">✓</span>}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* 通知 */}
        <div className="topbar-dropdown-wrap">
          <button
            className="topbar-icon-btn notif-btn"
            onClick={() => { setShowNotifications(!showNotifications); setShowProfile(false); setShowThemePicker(false); }}
          >
            <Bell size={15} />
            {unreadCount > 0 && <span className="notif-badge">{unreadCount}</span>}
          </button>

          {showNotifications && (
            <div className="topbar-dropdown notif-panel">
              <div className="dropdown-header">
                <span>系統通知</span>
                <span className="dropdown-count">{unreadCount} 則未讀</span>
              </div>
              <div className="notif-list">
                {notifications.map(n => (
                  <div key={n.id} className={`notif-item ${!n.read ? 'unread' : ''}`}>
                    <div className="notif-dot" style={{ background: typeColors[n.type] }} />
                    <div className="notif-content">
                      <div className="notif-title">{n.title}</div>
                      <div className="notif-body">{n.body}</div>
                      <div className="notif-time">{n.time}</div>
                    </div>
                  </div>
                ))}
              </div>
              <div className="dropdown-footer">
                <button className="btn btn-ghost btn-sm">查看全部通知</button>
              </div>
            </div>
          )}
        </div>

        {/* 個人資料 */}
        <div className="topbar-dropdown-wrap">
          <button
            className="topbar-profile-btn"
            onClick={() => { setShowProfile(!showProfile); setShowNotifications(false); setShowThemePicker(false); }}
          >
            <div className="avatar">{user.name.charAt(0)}</div>
            <div className="profile-info">
              <div className="profile-name">{user.name}</div>
              <div className="profile-role">{user.role}</div>
            </div>
            <ChevronDown size={12} className={`chevron ${showProfile ? 'rotated' : ''}`} />
          </button>

          {showProfile && (
            <div className="topbar-dropdown profile-panel">
              <div className="profile-header">
                <div className="avatar-lg">{user.name.charAt(0)}</div>
                <div>
                  <div className="profile-name-lg">{user.name}</div>
                  <div className="profile-dept">{user.department}</div>
                </div>
              </div>
              <div className="divider" style={{ margin: '8px 0' }} />
              <button className="dropdown-item">
                <User size={14} />
                <span>個人資料</span>
              </button>
              <button className="dropdown-item">
                <Settings size={14} />
                <span>帳號設定</span>
              </button>
              <div className="divider" style={{ margin: '8px 0' }} />
              <button className="dropdown-item danger" onClick={onLogout}>
                <LogOut size={14} />
                <span>登出系統</span>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* 覆蓋層 */}
      {(showNotifications || showProfile || showThemePicker) && (
        <div
          className="topbar-overlay"
          onClick={() => { setShowNotifications(false); setShowProfile(false); setShowThemePicker(false); }}
        />
      )}
    </header>
  );
};

// 即時時鐘元件
const TopbarClock: React.FC = () => {
  const [time, setTime] = React.useState(new Date());

  React.useEffect(() => {
    const timer = setInterval(() => setTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const pad = (n: number) => n.toString().padStart(2, '0');

  return (
    <div className="topbar-clock">
      {`${time.getFullYear()}/${pad(time.getMonth()+1)}/${pad(time.getDate())} ${pad(time.getHours())}:${pad(time.getMinutes())}:${pad(time.getSeconds())}`}
    </div>
  );
};
