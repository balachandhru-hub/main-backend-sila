import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { Image, StyleSheet, Text, View } from 'react-native';
import {
  getGetAccessContextQueryKey,
  getOrganizationLogo,
  useGetAccessContext,
} from '@workspace/api-client-react';
import { useAuth } from '@/providers/AuthProvider';
import { useColors } from '@/hooks/useColors';

const silaLogo = require('../assets/images/sila-logo.png');

type BrandingValue = {
  customerLogoUri: string | null;
  organizationName: string | null;
  displayLogo: number | { uri: string };
};

const BrandingContext = createContext<BrandingValue>({
  customerLogoUri: null,
  organizationName: null,
  displayLogo: silaLogo,
});

export function BrandingProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const access = useGetAccessContext({
    query: { queryKey: getGetAccessContextQueryKey(), enabled: Boolean(user), retry: false },
  });
  const organization = access.data?.organizations[0] ?? null;
  const [customerLogoUri, setCustomerLogoUri] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    let objectUrl: string | null = null;
    if (!organization?.id || !organization.logoUrl) {
      setCustomerLogoUri(null);
      return;
    }
    void getOrganizationLogo(organization.id)
      .then(async (blob) => {
        if (!active) return;
        if (typeof FileReader !== 'undefined') {
          const reader = new FileReader();
          const dataUrl = await new Promise<string>((resolve, reject) => {
            reader.onloadend = () => resolve(String(reader.result ?? ''));
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(blob);
          });
          if (active) setCustomerLogoUri(dataUrl);
          return;
        }
        objectUrl = URL.createObjectURL(blob);
        setCustomerLogoUri(objectUrl);
      })
      .catch(() => {
        if (active) setCustomerLogoUri(null);
      });
    return () => {
      active = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [organization?.id, organization?.logoUrl]);

  const value = useMemo<BrandingValue>(() => ({
    customerLogoUri,
    organizationName: organization?.name ?? null,
    displayLogo: customerLogoUri ? { uri: customerLogoUri } : silaLogo,
  }), [customerLogoUri, organization?.name]);

  return <BrandingContext.Provider value={value}>{children}</BrandingContext.Provider>;
}

export function useBranding() {
  return useContext(BrandingContext);
}

export function CustomerBrandImage({
  style,
  accessibilityLabel,
}: {
  style?: object;
  accessibilityLabel?: string;
}) {
  const branding = useBranding();
  return (
    <Image
      source={branding.displayLogo}
      style={style}
      resizeMode="contain"
      accessibilityLabel={accessibilityLabel ?? (branding.customerLogoUri ? `${branding.organizationName ?? 'Customer'} logo` : 'SILA ME logo')}
    />
  );
}

export function SilaPoweredFooter() {
  const colors = useColors();
  return (
    <View style={[styles.footer, { borderTopColor: colors.border }]} accessibilityLabel="Powered by SILA">
      <Text style={[styles.powered, { color: colors.mutedForeground }]}>POWERED BY</Text>
      <Image source={silaLogo} style={styles.sila} resizeMode="contain" accessibilityLabel="SILA logo" />
    </View>
  );
}

const styles = StyleSheet.create({
  footer: {
    marginTop: 28,
    paddingTop: 16,
    borderTopWidth: 1,
    alignItems: 'center',
    gap: 8,
  },
  powered: {
    fontFamily: 'Inter_600SemiBold',
    fontSize: 10,
    letterSpacing: 1.1,
  },
  sila: {
    width: 96,
    height: 24,
  },
});
