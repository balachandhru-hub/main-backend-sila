import { FormEvent, useEffect, useState } from 'react';
import { useLocation } from 'wouter';
import { CloudMark } from '@/components/cloud-mark';
import { platformFetch } from '@/lib/platform-api';

export default function PlatformLogin() {
  const [, setLocation] = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);

  useEffect(() => {
    document.title = 'Sign in · SILA Platform';
  }, []);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setPending(true);
    try {
      await platformFetch('/api/platform/auth/login', { method: 'POST', body: JSON.stringify({ email: email.trim(), password }) });
      setLocation('/organizations');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Sign-in failed.');
    } finally {
      setPending(false);
    }
  };

  return (
    <main className="auth-page">
      <section className="auth-panel">
        <div className="auth-panel__inner">
          <div className="auth-mobile-mark"><CloudMark /></div>
          <div className="auth-panel__heading">
            <p className="eyebrow"><span className="eyebrow__mark" />SILA CLOUD</p>
            <h2>Sign in</h2>
            <p>Organization management for SILA Super Admin and support.</p>
          </div>
          <form className="auth-form" onSubmit={(event) => void submit(event)} noValidate>
            {error ? <div className="form-alert" role="alert"><span className="form-alert__line" /><p>{error}</p></div> : null}
            <label className="field" htmlFor="platform-email">
              <span className="field__label">Email</span>
              <input id="platform-email" value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" />
            </label>
            <label className="field" htmlFor="platform-password">
              <span className="field__label">Password</span>
              <input id="platform-password" type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" />
            </label>
            <button className="sila-button" type="submit" disabled={pending}>{pending ? 'Signing in…' : 'Sign in'}</button>
          </form>
        </div>
      </section>
    </main>
  );
}
