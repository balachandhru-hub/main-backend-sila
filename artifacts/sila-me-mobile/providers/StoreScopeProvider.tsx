import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import {
  getGetAccessContextQueryKey,
  useGetAccessContext,
  type Organization,
  type OrganizationUnit,
} from '@workspace/api-client-react';
import { useAuth } from '@/providers/AuthProvider';
import { mobileTenant } from '@/config/mobile-tenant';

type StoreContextValue = {
  organization: Organization | null;
  unit: OrganizationUnit | null;
  organizations: Organization[];
  units: OrganizationUnit[];
  permissions: Set<string>;
  hasPermission: (key: string) => boolean;
  setOperatingUnitId: (id: string) => void;
  isLoading: boolean;
  isOnline: boolean;
  firstName: string;
};

const StoreContext = createContext<StoreContextValue | null>(null);

export function StoreScopeProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const access = useGetAccessContext({
    query: { queryKey: getGetAccessContextQueryKey(), enabled: Boolean(user), retry: false },
  });
  const [isOnline, setIsOnline] = useState(typeof navigator === 'undefined' ? true : navigator.onLine);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    const on = () => setIsOnline(true);
    const off = () => setIsOnline(false);
    window.addEventListener('online', on);
    window.addEventListener('offline', off);
    return () => {
      window.removeEventListener('online', on);
      window.removeEventListener('offline', off);
    };
  }, []);

  const organizations = access.data?.organizations ?? [];
  const units = access.data?.units ?? [];
  const tenantCode = mobileTenant.tenantCode.toUpperCase();
  const organization =
    organizations.find((item) => item.code.toUpperCase() === tenantCode)
    ?? organizations.find((item) => item.name === mobileTenant.tenantName)
    ?? organizations[0]
    ?? null;
  const scopedUnits = organization
    ? units.filter((item) => item.organizationId === organization.id)
    : units;
  const unit = scopedUnits[0] ?? null;

  const permissions = useMemo(() => new Set((access.data?.permissions ?? []).map((item) => item.key)), [access.data?.permissions]);
  const firstName = (user?.displayName ?? 'Operator').split(' ')[0];

  const value = useMemo<StoreContextValue>(() => ({
    organization,
    unit,
    organizations,
    units,
    permissions,
    hasPermission: (key) => permissions.size === 0 || permissions.has(key),
    setOperatingUnitId: () => undefined,
    isLoading: Boolean(user) && access.isPending,
    isOnline,
    firstName,
  }), [access.isPending, firstName, isOnline, organization, organizations, permissions, unit, units, user]);

  return <StoreContext.Provider value={value}>{children}</StoreContext.Provider>;
}

export function useStoreScope() {
  const value = useContext(StoreContext);
  if (!value) throw new Error('useStoreScope must be used within StoreScopeProvider');
  return value;
}
