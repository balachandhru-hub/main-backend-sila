import { useMemo, type ReactNode } from 'react';
import { LockKeyhole } from 'lucide-react';
import { useAccessContext } from '@/components/sila-layout';
import { hasPermission } from '@/lib/permissions';

export { hasPermission };

export function PermissionGate({ permission, children, fallback = null }: { permission: string | string[]; children: ReactNode; fallback?: ReactNode }) {
  const { accessContext } = useAccessContext();
  const allowed = useMemo(() => hasPermission(accessContext?.permissions, permission), [accessContext?.permissions, permission]);
  return allowed ? <>{children}</> : <>{fallback}</>;
}

export function AccessDenied({ title = 'Access denied', description = 'Your current Cloud permission context does not include this workspace area.' }: { title?: string; description?: string }) {
  return (
    <main className="sila-access-denied" data-testid="page-access-denied">
      <div className="sila-access-denied__inner">
        <LockKeyhole size={28} color="var(--sila-blue)" />
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
    </main>
  );
}

export function RoutePermissionGuard({ permission, children }: { permission?: string | string[]; children: ReactNode }) {
  const { accessContext, accessPending } = useAccessContext();
  if (!permission || accessPending) return <>{children}</>;
  return hasPermission(accessContext?.permissions, permission) ? <>{children}</> : <AccessDenied />;
}