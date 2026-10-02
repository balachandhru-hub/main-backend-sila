import { useEffect, useState } from 'react';
import { Link } from 'wouter';
import { platformFetch } from '@/lib/platform-api';

export default function PlatformSimpleList({ path, title }: { path: string; title: string }) {
  const [rows, setRows] = useState<unknown>(null);
  const [error, setError] = useState('');
  useEffect(() => {
    void platformFetch<unknown>(path).then(setRows).catch((caught: Error) => setError(caught.message));
  }, [path]);
  return (
    <div className="sila-app" style={{ minHeight: '100vh', background: '#fff', padding: 32 }}>
      <Link href="/organizations">← Organizations</Link>
      <h1 style={{ color: 'var(--sila-ink)' }}>{title}</h1>
      {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : <pre style={{ background: '#f8fafc', border: '1px solid var(--sila-line)', padding: 16 }}>{JSON.stringify(rows, null, 2)}</pre>}
    </div>
  );
}
