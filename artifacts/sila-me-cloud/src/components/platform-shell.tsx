import { Link } from 'wouter';
import type { ReactNode } from 'react';

export function PlatformShell({ children, title, action }: { children: ReactNode; title: string; action?: ReactNode }) {
  return (
    <div className="sila-app" style={{ minHeight: '100vh', background: '#fff', color: 'var(--sila-ink)' }}>
      <header className="sila-header" style={{ borderBottom: '1px solid #c5d8e8', background: '#fff' }}>
        <div className="sila-header__left">
          <strong style={{ color: 'var(--sila-ink)', fontSize: 16, letterSpacing: '.04em' }}>SILA CLOUD</strong>
          <nav style={{ display: 'flex', gap: 18, marginLeft: 28, fontSize: 14 }}>
            <Link href="/organizations">Organizations</Link>
            <Link href="/platform/users">Platform users</Link>
            <Link href="/platform/audit">Audit</Link>
            <Link href="/platform/settings">Settings</Link>
          </nav>
        </div>
      </header>
      <main style={{ padding: '28px 32px', maxWidth: 1280, margin: '0 auto' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 16, marginBottom: 20 }}>
          <h1 style={{ margin: 0, fontSize: 28, color: 'var(--sila-ink)' }}>{title}</h1>
          {action}
        </div>
        {children}
      </main>
    </div>
  );
}
