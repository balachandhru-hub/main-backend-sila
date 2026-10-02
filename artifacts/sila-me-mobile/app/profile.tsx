import { useRouter } from 'expo-router';
import React, { useEffect } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { SilaBottomAction, SilaCard, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useAuth } from '@/providers/AuthProvider';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function ProfileScreen() {
  const colors = useColors();
  const router = useRouter();
  const { user, logout } = useAuth();
  const { organization, unit } = useStoreScope();

  useEffect(() => {
    if (!user) router.replace('/login');
  }, [router, user]);

  if (!user) return null;

  return (
    <SilaScreen title="Profile" eyebrow="ACCOUNT" fallback="/more">
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>{user.displayName}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>{user.email}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Application Mobile</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Organization {organization?.name ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Operating unit {unit?.name ?? '—'}</Text>
        <View style={{ marginTop: 10 }}><SilaStatusChip label={user.status} tone="success" /></View>
      </SilaCard>
      <View style={{ gap: 10, marginTop: 12 }}>
        <SilaBottomAction
          testID="mobile-logout-button"
          label="Log out"
          onPress={() => {
            logout();
            router.replace('/login');
          }}
        />
      </View>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 18 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 13 },
});
