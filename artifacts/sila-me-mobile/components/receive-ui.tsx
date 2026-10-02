/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * ReceiveShell layout is required for Invoice Review on RN-web (header inside ScrollView).
 */
import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import React from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { CustomerBrandImage, SilaPoweredFooter } from '@/components/branding';
import { useColors } from '@/hooks/useColors';

export function HeaderAction({
  icon,
  label,
  onPress,
  disabled = false,
  loading = false,
  testID,
  accessibilityLabel,
}: {
  icon: React.ComponentProps<typeof Feather>['name'];
  label: string;
  onPress: () => void;
  disabled?: boolean;
  loading?: boolean;
  testID?: string;
  accessibilityLabel: string;
}) {
  const colors = useColors();
  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      disabled={disabled}
      onPress={onPress}
      hitSlop={6}
      style={({ pressed }) => [
        styles.headerAction,
        {
          borderColor: colors.border,
          opacity: disabled ? 0.45 : pressed ? 0.72 : 1,
        },
      ]}
    >
      {loading ? (
        <ActivityIndicator size="small" color={colors.primary} />
      ) : (
        <Feather name={icon} size={16} color={colors.primary} />
      )}
      <Text numberOfLines={1} style={[styles.headerActionLabel, { color: colors.primary }]}>{label}</Text>
    </Pressable>
  );
}

export function ReceiveShell({
  title,
  eyebrow = 'RECEIVING',
  children,
  scroll = true,
  fallback = '/receive',
  onBack,
  onInfo,
  headerActions,
  footer,
}: {
  title: string;
  eyebrow?: string;
  children: React.ReactNode;
  scroll?: boolean;
  fallback?: string;
  onBack?: () => void;
  onInfo?: () => void;
  headerActions?: React.ReactNode;
  footer?: React.ReactNode;
}) {
  const colors = useColors();
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const { width } = useWindowDimensions();
  const compactTitle = Boolean(headerActions) && width < 420;
  const header = (
    <>
      <View style={styles.topbar}>
        <Pressable onPress={onBack ?? (() => router.canGoBack() ? router.back() : router.replace(fallback as never))} hitSlop={12} style={styles.back} accessibilityRole="button" accessibilityLabel="Back">
          <Feather name="arrow-left" size={20} color={colors.primary} />
        </Pressable>
        <CustomerBrandImage style={styles.topLogo} />
        <View style={{ width: 32 }} />
      </View>
      <Text style={[styles.eyebrow, { color: colors.primary }]}>{eyebrow}</Text>
      <View style={styles.titleRow}>
        <Text numberOfLines={1} style={[compactTitle ? styles.titleCompact : styles.title, { color: colors.foreground, flex: 1, minWidth: 0 }]}>{title}</Text>
        <View style={styles.headerActions}>
          {headerActions}
          {onInfo ? (
            <Pressable
              testID="ocr-info-button"
              accessibilityLabel="Invoice information"
              accessibilityRole="button"
              onPress={onInfo}
              hitSlop={12}
              style={styles.titleInfo}
            >
              <View style={[styles.infoBadge, { borderColor: colors.primary }]}>
                <Text style={[styles.infoBadgeText, { color: colors.primary }]}>i</Text>
              </View>
            </Pressable>
          ) : null}
        </View>
      </View>
    </>
  );
  if (footer) {
    return (
      <View style={{ flex: 1, height: '100%', backgroundColor: colors.background }}>
        <ScrollView
          style={{ flex: 1 }}
          contentContainerStyle={[styles.scroll, { paddingTop: insets.top + 16, paddingBottom: 16, flexGrow: 1 }]}
        >
          {header}
          {children}
        </ScrollView>
        <View style={[styles.footer, { paddingBottom: insets.bottom + 16, borderTopColor: colors.border, backgroundColor: colors.background }]}>
          {footer}
          <SilaPoweredFooter />
        </View>
      </View>
    );
  }
  const content = (
    <View style={[styles.inner, { paddingTop: insets.top + 16, paddingBottom: insets.bottom + 28 }]}>
      {header}
      {children}
      <SilaPoweredFooter />
    </View>
  );
  return scroll ? (
    <ScrollView style={{ backgroundColor: colors.background }} contentContainerStyle={styles.scroll}>
      {content}
    </ScrollView>
  ) : (
    <View style={{ flex: 1, backgroundColor: colors.background }}>{content}</View>
  );
}

export function Panel({ children }: { children: React.ReactNode }) {
  const colors = useColors();
  return <View style={[styles.panel, { backgroundColor: colors.card, borderColor: colors.border }]}>{children}</View>;
}

export function PrimaryButton({
  label,
  onPress,
  disabled = false,
  secondary = false,
  testID,
}: {
  label: string;
  onPress: () => void;
  disabled?: boolean;
  secondary?: boolean;
  testID?: string;
}) {
  const colors = useColors();
  return (
    <Pressable
      testID={testID}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.button,
        {
          backgroundColor: secondary ? colors.card : colors.primary,
          borderColor: colors.primary,
          opacity: disabled ? 0.45 : pressed ? 0.72 : 1,
        },
      ]}
    >
      <Text style={[styles.buttonText, { color: secondary ? colors.primary : colors.primaryForeground }]}>{label}</Text>
    </Pressable>
  );
}

export function StatusPill({ label, tone = 'neutral' }: { label: string; tone?: 'neutral' | 'success' | 'warning' | 'error' }) {
  const colors = useColors();
  const color = tone === 'success' ? colors.success : tone === 'warning' ? colors.warning : tone === 'error' ? colors.error : colors.primary;
  return (
    <View style={[styles.pill, { backgroundColor: `${color}18`, borderColor: `${color}48` }]}>
      <Text style={[styles.pillText, { color }]}>{label.replaceAll('_', ' ')}</Text>
    </View>
  );
}

export function formatQuantity(value?: number | null) {
  return value == null ? '—' : Number(value).toLocaleString(undefined, { maximumFractionDigits: 3 });
}

const styles = StyleSheet.create({
  scroll: { paddingHorizontal: 22 },
  inner: { minHeight: '100%' },
  topbar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 26 },
  back: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  topLogo: { width: 112, height: 28 },
  topbarLabel: { fontFamily: 'Inter_600SemiBold', fontSize: 11, letterSpacing: 1.6 },
  eyebrow: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.5 },
  titleRow: { marginTop: 8, flexDirection: 'row', alignItems: 'center', gap: 8 },
  title: { flexShrink: 1, fontFamily: 'Inter_700Bold', fontSize: 28, lineHeight: 34, letterSpacing: -0.7 },
  titleCompact: { flexShrink: 1, fontFamily: 'Inter_700Bold', fontSize: 20, lineHeight: 26, letterSpacing: -0.4 },
  headerActions: { flexDirection: 'row', alignItems: 'center', justifyContent: 'flex-end', gap: 6, flexShrink: 0, flexWrap: 'nowrap' },
  headerAction: { minHeight: 36, paddingHorizontal: 8, borderWidth: 1, borderRadius: 8, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 4 },
  headerActionLabel: { fontFamily: 'Inter_600SemiBold', fontSize: 11, letterSpacing: 0.2 },
  footer: { paddingHorizontal: 22, paddingTop: 12, borderTopWidth: 1, gap: 8 },
  titleInfo: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  infoBadge: { width: 26, height: 26, borderRadius: 13, borderWidth: 1.5, alignItems: 'center', justifyContent: 'center' },
  infoBadgeText: { fontFamily: 'Inter_700Bold', fontSize: 15, lineHeight: 18, textTransform: 'lowercase' },
  panel: { marginTop: 20, borderWidth: 1, borderRadius: 12, padding: 18 },
  button: { minHeight: 48, alignItems: 'center', justifyContent: 'center', borderRadius: 10, borderWidth: 1, paddingHorizontal: 18 },
  buttonText: { fontFamily: 'Inter_600SemiBold', fontSize: 14 },
  pill: { alignSelf: 'flex-start', borderWidth: 1, borderRadius: 100, paddingHorizontal: 10, paddingVertical: 5 },
  pillText: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 0.5, textTransform: 'uppercase' },
});