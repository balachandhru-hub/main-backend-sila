import { Link } from 'wouter';
import { useEffect, useState } from 'react';
import { getOrganizationLogo } from '@workspace/api-client-react';
import silaLogoUrl from '@/assets/sila-logo.png';
import { isSuperAdmin } from '@/lib/branding';

export { isSuperAdmin };
export { silaLogoUrl };

const request = { credentials: 'include' as const };

export function useCustomerLogoUrl(organizationId?: string | null, logoPath?: string | null) {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    let created: string | null = null;
    if (!organizationId || !logoPath) {
      setObjectUrl(null);
      return;
    }
    void getOrganizationLogo(organizationId, request)
      .then((blob) => {
        if (!active) return;
        created = URL.createObjectURL(blob);
        setObjectUrl(created);
      })
      .catch(() => {
        if (active) setObjectUrl(null);
      });
    return () => {
      active = false;
      if (created) URL.revokeObjectURL(created);
    };
  }, [organizationId, logoPath]);

  return objectUrl;
}

export function CustomerBrandMark({
  organizationId,
  logoUrl,
  organizationName,
  compact = false,
}: {
  organizationId?: string | null;
  logoUrl?: string | null;
  organizationName?: string | null;
  compact?: boolean;
}) {
  const customerLogo = useCustomerLogoUrl(organizationId, logoUrl);
  const src = customerLogo ?? silaLogoUrl;
  const label = customerLogo ? `${organizationName ?? 'Customer'} logo` : 'SILA ME';

  return (
    <Link
      href="/dashboard"
      className={`cloud-mark ${compact ? 'cloud-mark--compact' : ''}`}
      data-testid="link-customer-brand"
      aria-label={`${label} home`}
    >
      <img src={src} alt={label} className="cloud-mark__image" />
    </Link>
  );
}

export function SilaPoweredMark({ compact = false }: { compact?: boolean }) {
  return (
    <div className={`sila-powered ${compact ? 'sila-powered--compact' : ''}`} data-testid="sila-powered-mark">
      <span className="sila-powered__label">Powered by</span>
      <img src={silaLogoUrl} alt="SILA" className="sila-powered__image" />
    </div>
  );
}
