import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useLocation } from 'wouter';
import { getGetCloudSessionQueryKey, getGetCurrentUserQueryKey } from '@workspace/api-client-react';
import { withCustomerBase } from '@/lib/tenant-route';

export default function PlatformLaunch() {
  const [, setLocation] = useLocation();
  const queryClient = useQueryClient();
  const [error, setError] = useState('');

  useEffect(() => {
    document.title = 'Opening customer application · SILA Cloud';
    const code = new URLSearchParams(window.location.search).get('code')
      || new URLSearchParams(window.location.search).get('launch');
    if (!code) {
      setError('Launch authorization is missing.');
      return;
    }

    void fetch('/api/auth/launch', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code }),
    }).then(async (response) => {
      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Launch authorization was rejected.' })) as { message?: string };
        setError(body.message || 'Launch authorization was rejected.');
        return;
      }
      const session = await response.json() as { user: unknown };
      queryClient.setQueryData(getGetCloudSessionQueryKey(), session);
      queryClient.setQueryData(getGetCurrentUserQueryKey(), session.user);
      window.history.replaceState({}, '', withCustomerBase('/dashboard'));
      setLocation('/dashboard');
    }).catch(() => setError('Launch authorization was rejected.'));
  }, [queryClient, setLocation]);

  return (
    <main className="auth-page">
      <section className="auth-panel">
        <div className="auth-panel__inner">
          <p className="eyebrow"><span className="eyebrow__mark" />SILA Cloud</p>
          <h2>{error ? 'Launch rejected' : 'Opening customer application'}</h2>
          <p>{error || 'Exchanging a one-time SILA Platform launch code. Credentials are not requested again.'}</p>
        </div>
      </section>
    </main>
  );
}
