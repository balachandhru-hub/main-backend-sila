import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import React, { useMemo } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { getGetGrnsQueryKey, getHealthCheckQueryKey, useGetGrns, useHealthCheck } from '@workspace/api-client-react';
import {
  SilaActionCard,
  SilaEmptyState,
  SilaOfflineBanner,
  SilaStatusChip,
  SilaSummaryCard,
} from '@/components/sila/ui';
import { CustomerBrandImage, SilaPoweredFooter } from '@/components/branding';
import { greetingForNow } from '@/services/ops/greeting';
import { HOME_QUICK_ACTIONS } from '@/services/ops/nav';
import { useColors } from '@/hooks/useColors';
import { useAuth } from '@/providers/AuthProvider';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { mobileTenant } from '@/config/mobile-tenant';

export default function HomeScreen() {
  const colors = useColors();
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const { user } = useAuth();
  const { organization, firstName, hasPermission } = useStoreScope();
  const health = useHealthCheck({ query: { queryKey: getHealthCheckQueryKey(), refetchInterval: 30_000 } });
  const grns = useGetGrns({ query: { queryKey: getGetGrnsQueryKey(), enabled: Boolean(user) && hasPermission('VIEW_GRN'), retry: false } });

  const pendingGrns = useMemo(
    () => (grns.data ?? []).filter((item) => item.status === 'FAILED' || item.status === 'UNKNOWN' || item.status === 'POSTING').length,
    [grns.data],
  );
  const attention = useMemo(
    () => (grns.data ?? []).filter((item) => item.status === 'FAILED' || item.status === 'UNKNOWN').slice(0, 5),
    [grns.data],
  );

  if (!user) return null;
  const initials = user.displayName.split(' ').map((part) => part[0]).join('').slice(0, 2).toUpperCase();
  const dash = (value: number | null | undefined, loading: boolean) => (loading ? '…' : value == null ? '—' : String(value));

  return (
    <ScrollView
      style={{ backgroundColor: colors.background }}
      contentContainerStyle={[styles.content, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 28 }]}
      testID="home-screen"
    >
      <View style={[styles.header, { borderBottomColor: colors.border }]}>
        <CustomerBrandImage style={styles.logo} />
        <View style={styles.headerRight}>
          <Pressable accessibilityRole="button" accessibilityLabel="Profile" onPress={() => router.push('/profile')} style={[styles.avatar, { backgroundColor: colors.primaryLight }]}>
            <Text style={[styles.avatarText, { color: colors.primary }]}>{initials}</Text>
          </Pressable>
        </View>
      </View>

      <SilaOfflineBanner />

      <View style={styles.welcome}>
        <Text style={[styles.greeting, { color: colors.foreground }]}>{greetingForNow()}, {firstName}</Text>
        <Text style={[styles.orgName, { color: colors.foreground }]}>{organization?.name ?? mobileTenant.tenantName}</Text>
        <Text style={[styles.storeName, { color: colors.mutedForeground }]}>
          {mobileTenant.environment === 'TEST' ? 'TEST' : ''}
        </Text>
      </View>

      <Text style={[styles.section, { color: colors.mutedForeground }]}>QUICK ACTIONS</Text>
      <View style={styles.actions}>
        {HOME_QUICK_ACTIONS.map((action) => (
          <SilaActionCard
            key={action.testID}
            testID={action.testID}
            icon={action.icon as keyof typeof Feather.glyphMap}
            label={action.label}
            onPress={() => router.push(action.href as never)}
          />
        ))}
      </View>

      <Text style={[styles.section, { color: colors.mutedForeground, marginTop: 22 }]}>TODAY</Text>
      <View style={styles.actions}>
        <SilaSummaryCard label="Pending Receipts" value="—" />
        <SilaSummaryCard label="Pending GRNs" value={dash(grns.isPending ? null : pendingGrns, grns.isPending)} />
        <SilaSummaryCard label="Approvals" value="—" />
        <SilaSummaryCard label="Stock Count Tasks" value="—" />
        <SilaSummaryCard label="Pending ITO" value="—" />
        <SilaSummaryCard label="Low Stock Items" value="—" />
      </View>

      <Text style={[styles.section, { color: colors.mutedForeground, marginTop: 22 }]}>ATTENTION REQUIRED</Text>
      {attention.length === 0 ? (
        <SilaEmptyState title="Nothing requires your attention." description={health.data?.status === 'ok' ? undefined : 'Backend status will refresh automatically.'} />
      ) : (
        attention.map((item) => (
          <Pressable key={item.id} onPress={() => router.push({ pathname: '/receive/grn-result', params: { grnId: item.id } })} style={[styles.attention, { borderColor: colors.border }]}>
            <View style={{ flex: 1 }}>
              <Text style={[styles.attentionTitle, { color: colors.foreground }]}>{item.grnNumber}</Text>
              <Text style={[styles.attentionMeta, { color: colors.mutedForeground }]}>{item.supplierName} · {item.purchaseOrderNumber}</Text>
            </View>
            <SilaStatusChip label={item.status === 'FAILED' ? 'FAILED' : 'CRITICAL'} tone={item.status === 'FAILED' ? 'failed' : 'critical'} />
          </Pressable>
        ))
      )}

      <Pressable accessibilityRole="button" accessibilityLabel="Global search" onPress={() => router.push('/search')} style={[styles.searchEntry, { borderColor: colors.border }]}>
        <Feather name="search" size={16} color={colors.primary} />
        <Text style={{ color: colors.mutedForeground, fontFamily: 'Inter_400Regular' }}>Search suppliers, POs, GRNs…</Text>
      </Pressable>
      <SilaPoweredFooter />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: { paddingHorizontal: 20 },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingBottom: 14, borderBottomWidth: 1, marginBottom: 16 },
  logo: { width: 132, height: 32 },
  headerRight: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  avatar: { width: 40, height: 40, borderRadius: 20, alignItems: 'center', justifyContent: 'center' },
  avatarText: { fontFamily: 'Inter_700Bold', fontSize: 12 },
  welcome: { marginBottom: 18 },
  greeting: { fontFamily: 'Inter_700Bold', fontSize: 24, letterSpacing: -0.4 },
  orgName: { marginTop: 8, fontFamily: 'Inter_700Bold', fontSize: 16 },
  storeName: { marginTop: 2, fontFamily: 'Inter_400Regular', fontSize: 13 },
  section: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.2, marginBottom: 10 },
  actions: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  attention: { borderWidth: 1, borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', gap: 12 },
  attentionTitle: { fontFamily: 'Inter_700Bold', fontSize: 14 },
  attentionMeta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12 },
  searchEntry: { marginTop: 20, minHeight: 44, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12, flexDirection: 'row', alignItems: 'center', gap: 8 },
});
