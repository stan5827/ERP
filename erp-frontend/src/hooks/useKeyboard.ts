import { useEffect, useCallback } from 'react';

interface KeyboardShortcut {
  key: string;
  ctrl?: boolean;
  shift?: boolean;
  alt?: boolean;
  meta?: boolean;
  action: () => void;
  description?: string;
}

export const useKeyboardShortcuts = (shortcuts: KeyboardShortcut[]) => {
  const handleKeyDown = useCallback((event: KeyboardEvent) => {
    for (const shortcut of shortcuts) {
      const ctrlMatch = shortcut.ctrl ? (event.ctrlKey || event.metaKey) : !event.ctrlKey && !event.metaKey;
      const shiftMatch = shortcut.shift ? event.shiftKey : !event.shiftKey;
      const altMatch = shortcut.alt ? event.altKey : !event.altKey;
      
      if (
        event.key.toLowerCase() === shortcut.key.toLowerCase() &&
        ctrlMatch &&
        shiftMatch &&
        altMatch
      ) {
        event.preventDefault();
        shortcut.action();
        return;
      }
    }
  }, [shortcuts]);

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown]);
};

// Common keyboard shortcuts for ERP
export const useErpKeyboardShortcuts = (options: {
  onSave?: () => void;
  onSearch?: () => void;
  onNew?: () => void;
  onEscape?: () => void;
  onDelete?: () => void;
}) => {
  const shortcuts: KeyboardShortcut[] = [];

  if (options.onSave) {
    shortcuts.push({ key: 's', ctrl: true, action: options.onSave, description: '儲存' });
  }
  if (options.onSearch) {
    shortcuts.push({ key: 'k', ctrl: true, action: options.onSearch, description: '搜尋' });
  }
  if (options.onNew) {
    shortcuts.push({ key: 'n', ctrl: true, action: options.onNew, description: '新增' });
  }
  if (options.onEscape) {
    shortcuts.push({ key: 'Escape', action: options.onEscape, description: '取消' });
  }
  if (options.onDelete) {
    shortcuts.push({ key: 'Delete', action: options.onDelete, description: '刪除' });
  }

  useKeyboardShortcuts(shortcuts);
};

// Focus management
export const useFocusTrap = (isActive: boolean) => {
  useEffect(() => {
    if (!isActive) return;

    const focusableElements = document.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstElement = focusableElements[0] as HTMLElement;
    const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

    const handleTabKey = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return;

      if (e.shiftKey) {
        if (document.activeElement === firstElement) {
          e.preventDefault();
          lastElement?.focus();
        }
      } else {
        if (document.activeElement === lastElement) {
          e.preventDefault();
          firstElement?.focus();
        }
      }
    };

    document.addEventListener('keydown', handleTabKey);
    firstElement?.focus();

    return () => {
      document.removeEventListener('keydown', handleTabKey);
    };
  }, [isActive]);
};

// Auto-focus on mount
export const useAutoFocus = (condition: boolean = true) => {
  const ref = (element: HTMLElement | null) => {
    if (element && condition) {
      setTimeout(() => element.focus(), 100);
    }
  };
  return ref;
};

// List navigation (arrow keys)
export const useListNavigation = <T>(
  items: T[],
  onSelect: (item: T) => void,
  onEnter?: () => void
) => {
  const handleKeyDown = useCallback((e: React.KeyboardEvent, currentIndex: number) => {
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        return Math.min(currentIndex + 1, items.length - 1);
      case 'ArrowUp':
        e.preventDefault();
        return Math.max(currentIndex - 1, 0);
      case 'Enter':
        e.preventDefault();
        if (currentIndex >= 0 && currentIndex < items.length) {
          onSelect(items[currentIndex]);
          onEnter?.();
        }
        return currentIndex;
      case 'Home':
        e.preventDefault();
        return 0;
      case 'End':
        e.preventDefault();
        return items.length - 1;
      default:
        return currentIndex;
    }
  }, [items, onSelect, onEnter]);

  return { handleKeyDown };
};