import { useEffect, useRef, useState } from 'react';
import { BrowserQRCodeReader } from '@zxing/browser';

interface LotQrCameraProps {
  onScan: (value: string) => void;
}

export function LotQrCamera({ onScan }: LotQrCameraProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [active, setActive] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!active || !videoRef.current) return;

    let controls: { stop: () => void } | undefined;
    let cancelled = false;
    const reader = new BrowserQRCodeReader();

    void reader.decodeFromVideoDevice(undefined, videoRef.current, (result, _error, scannerControls) => {
      controls = scannerControls;
      if (cancelled) {
        scannerControls.stop();
        return;
      }
      if (!result) return;
      cancelled = true;
      scannerControls.stop();
      setActive(false);
      setError('');
      onScan(result.getText());
    }).catch(scanError => {
      if (!cancelled) {
        setError(scanError instanceof Error ? scanError.message : '無法啟動相機掃描');
        setActive(false);
      }
    });

    return () => {
      cancelled = true;
      controls?.stop();
    };
  }, [active, onScan]);

  return (
    <div style={{ display: 'grid', gap: 8 }}>
      {!active ? (
        <button type="button" className="btn btn-ghost btn-sm" onClick={() => { setError(''); setActive(true); }}>
          開啟相機掃描 QR
        </button>
      ) : (
        <div style={{ display: 'grid', gap: 8 }}>
          <video ref={videoRef} aria-label="QR 相機畫面" style={{ width: '100%', maxWidth: 420, background: '#111', borderRadius: 8 }} />
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => setActive(false)}>停止相機</button>
        </div>
      )}
      {error && <div role="alert" style={{ color: 'var(--crimson-300)', fontSize: 12 }}>{error}</div>}
    </div>
  );
}
