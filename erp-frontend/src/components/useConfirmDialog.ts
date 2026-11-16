import { useCallback, useState } from 'react';

export const useConfirmDialog = () => {
  const [state, setState] = useState<{
    isOpen: boolean;
    title: string;
    message: string;
    onConfirm: (() => void) | null;
    confirmText: string;
    cancelText: string;
    confirmVariant: 'primary' | 'danger' | 'warning';
  }>({
    isOpen: false,
    title: '',
    message: '',
    onConfirm: null,
    confirmText: '確認',
    cancelText: '取消',
    confirmVariant: 'primary',
  });

  const confirm = useCallback((
    title: string,
    message: string,
    onConfirm: () => void,
    options?: {
      confirmText?: string;
      cancelText?: string;
      confirmVariant?: 'primary' | 'danger' | 'warning';
    }
  ) => {
    setState({
      isOpen: true,
      title,
      message,
      onConfirm,
      confirmText: options?.confirmText ?? '確認',
      cancelText: options?.cancelText ?? '取消',
      confirmVariant: options?.confirmVariant ?? 'primary',
    });
  }, []);

  const close = useCallback(() => {
    setState(prev => ({ ...prev, isOpen: false }));
  }, []);

  return {
    ...state,
    confirm,
    close,
  };
};
