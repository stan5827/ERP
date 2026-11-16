import { AlertCircle, LoaderCircle } from 'lucide-react';

export function LoadingScreen({ title, detail }: { title: string; detail?: string }) {
  return (
    <main className="feedback-page">
      <div className="feedback-card">
        <LoaderCircle className="feedback-icon is-spinning" size={30} aria-hidden="true" />
        <h1>{title}</h1>
        {detail && <p>{detail}</p>}
      </div>
    </main>
  );
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div className="notice notice-error" role="alert">
      <AlertCircle size={19} aria-hidden="true" />
      <span>{message}</span>
      {onRetry && <button className="button button-quiet" onClick={onRetry}>重試</button>}
    </div>
  );
}

export function EmptyState({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="empty-state">
      <div className="empty-state-mark" aria-hidden="true">—</div>
      <h2>{title}</h2>
      <p>{detail}</p>
    </div>
  );
}
