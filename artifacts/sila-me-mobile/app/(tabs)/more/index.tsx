import { useRouter } from 'expo-router';
import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaCard, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { MORE_LINKS } from '@/services/ops/nav';

const links = MORE_LINKS;

export default function MoreLanding() {
  const colors = useColors();
  const router = useRouter();
  const { hasPermission } = useStoreScope();
  const visible = links.filter((link) => {
    if (link.href === '/suppliers') return hasPermission('VIEW_SUPPLIER') || hasPermission('VIEW_PURCHASE_ORDER');
    if (link.href === '/purchase-orders') return hasPermission('VIEW_PURCHASE_ORDER');
    if (link.href === '/receive/history') return hasPermission('VIEW_GRN') || hasPermission('VIEW_GRN_HISTORY');
    if (link.href === '/inventory/movements' || link.href === '/inventory/batches' || link.href === '/inventory/ito/history') {
      return hasPermission('VIEW_INVENTORY');
    }
    return true;
  });
  return (
    <SilaScreen title="More" eyebrow="SILA ME" showBack={false}>
      {visible.map((link) => (
        <SilaCard key={link.href} onPress={() => router.push(link.href as never)}>
          <Text style={[styles.title, { color: colors.foreground }]}>{link.title}</Text>
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
});
