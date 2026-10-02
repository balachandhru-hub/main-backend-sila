import { FormEvent, useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ArrowUpRight, Eye, EyeOff, LockKeyhole, Mail } from 'lucide-react';
import { useLocation } from 'wouter';
import {
  getGetCloudSessionQueryKey,
  getGetCurrentUserQueryKey,
  useCloudLogin,
} from '@workspace/api-client-react';
import { CloudMark } from '@/components/cloud-mark';
import { SilaPoweredMark } from '@/components/branding';
import { customerBasePath, withCustomerBase } from '@/lib/tenant-route';

function errorMessage(error: unknown) {
  if (error && typeof error === 'object' && 'data' in error) {
    const data = (error as { data?: unknown }).data;
    if (data && typeof data === 'object' && 'message' in data) {
      return String((data as { message: unknown }).message);
    }
  }
  if (error instanceof Error) return error.message;
  return 'We could not verify those details. Try again.';
}

function safeReturnPath() {
  const returnTo = new URLSearchParams(window.location.search).get('returnTo');
  if (!returnTo || !returnTo.startsWith('/') || returnTo.startsWith('//')) return '/dashboard';

  try {
    const target = new URL(returnTo, window.location.origin);
    if (target.origin !== window.location.origin) return '/dashboard';
    const base = customerBasePath();
    let pathname = target.pathname;
    if (base && (pathname === base || pathname.startsWith(`${base}/`))) {
      pathname = pathname.slice(base.length) || '/';
    }
    if (pathname === '/login' || pathname.endsWith('/login')) return '/dashboard';
    return `${pathname}${target.search}${target.hash}`;
  } catch {
    return '/dashboard';
  }
}

function isSessionExpiredRedirect() {
  return new URLSearchParams(window.location.search).get('sessionExpired') === 'true';
}

type PublicTenant = {
  tenantCode?: string;
  customerName?: string;
  environment?: string;
  testEnvironment?: boolean;
  loginWelcomeText?: string;
};

export default function Login() {
  const [, setLocation] = useLocation();
  const queryClient = useQueryClient();
  const login = useCloudLogin({ request: { credentials: 'include' } });
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [formError, setFormError] = useState('');
  const [tenant, setTenant] = useState<PublicTenant | null>(null);
  const [launchPending, setLaunchPending] = useState(false);
  const sessionExpired = isSessionExpiredRedirect();

  useEffect(() => {
    document.title = 'Sign in · SILA ME Cloud';
    void fetch('/api/public/tenant', { credentials: 'include' })
      .then((response) => (response.ok ? response.json() : null))
      .then((payload: PublicTenant | null) => {
        if (payload) {
          setTenant(payload);
          document.title = `Sign in · ${payload.customerName || 'SILA ME'}`;
        }
      })
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code') || params.get('launch');
    if (!code) return;
    setLaunchPending(true);
    void fetch('/api/auth/launch', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code }),
    }).then(async (response) => {
      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Launch authorization was rejected.' })) as { message?: string };
        setFormError(body.message || 'Launch authorization was rejected.');
        setLaunchPending(false);
        return;
      }
      const session = await response.json() as { user: unknown };
      queryClient.setQueryData(getGetCloudSessionQueryKey(), session);
      queryClient.setQueryData(getGetCurrentUserQueryKey(), session.user);
      window.history.replaceState({}, '', withCustomerBase('/dashboard'));
      setLocation('/dashboard');
    }).catch(() => {
      setFormError('Launch authorization was rejected.');
      setLaunchPending(false);
    });
  }, [queryClient, setLocation]);

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError('');

    if (!email.trim() || !password) {
      setFormError('Enter your email and password to continue.');
      return;
    }

    login.mutate(
      { data: { email: email.trim(), password } },
      {
        onSuccess: (session) => {
          queryClient.setQueryData(getGetCloudSessionQueryKey(), session);
          queryClient.setQueryData(getGetCurrentUserQueryKey(), session.user);
          setLocation(safeReturnPath());
        },
        onError: (error) => setFormError(errorMessage(error)),
      },
    );
  };

  return (
    <main className="auth-page">
      <section className="auth-panel">
        <div className="auth-panel__inner">
          <div className="auth-mobile-mark"><CloudMark /></div>
          <div className="auth-panel__heading">
            <p className="eyebrow">
              <span className="eyebrow__mark" />
              Secure enterprise access
            </p>
            <h2>SILA ME Cloud</h2>
            <p>Menu Engineering &amp; Operations</p>
          </div>

          <form className="auth-form" onSubmit={submit} noValidate>
            {launchPending ? (
              <div className="form-alert form-alert--info" role="status">
                <span className="form-alert__line" />
                <p>Opening a SILA Platform support session…</p>
              </div>
            ) : null}
            {sessionExpired ? (
              <div className="form-alert form-alert--info" role="status" data-testid="alert-session-expired">
                <span className="form-alert__line" />
                <p>Your Cloud session expired. Sign in again to continue.</p>
              </div>
            ) : null}
            {formError ? (
              <div className="form-alert" role="alert" data-testid="alert-login-error">
                <span className="form-alert__line" />
                <p>{formError}</p>
              </div>
            ) : null}

            <label className="field" htmlFor="email">
                <span className="field__label">Email</span>
              <span className="field__control">
                <Mail size={17} aria-hidden="true" />
                <input
                  id="email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="you@company.com"
                  data-testid="input-email"
                  aria-invalid={Boolean(formError && !email)}
                />
              </span>
            </label>

            <label className="field" htmlFor="password">
                <span className="field__label">Password</span>
              <span className="field__control">
                <LockKeyhole size={17} aria-hidden="true" />
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  placeholder="Enter your password"
                  data-testid="input-password"
                  aria-invalid={Boolean(formError && !password)}
                />
                <button
                  className="field__action"
                  type="button"
                  onClick={() => setShowPassword((visible) => !visible)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  data-testid="button-toggle-password"
                >
                  {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
                </button>
              </span>
            </label>

            <button
              className="submit-button"
              type="submit"
              disabled={login.isPending || launchPending}
              data-testid="button-submit-login"
            >
               <span>{login.isPending ? 'SIGNING IN' : 'SIGN IN'}</span>
              <ArrowUpRight size={18} aria-hidden="true" />
            </button>
          </form>

          <p className="auth-panel__footnote">
            Access is limited to invited SILA ME teams. If you need an account, contact your workspace administrator.
          </p>
          <div style={{ marginTop: 24 }}>
            <SilaPoweredMark />
          </div>
        </div>
        <span className="panel-corner panel-corner--top" aria-hidden="true" />
        <span className="panel-corner panel-corner--bottom" aria-hidden="true" />
      </section>
    </main>
  );
}