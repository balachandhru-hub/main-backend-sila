import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import React, { useState } from 'react';
import {
  ActivityIndicator,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { CustomerBrandImage, SilaPoweredFooter } from '@/components/branding';

export function SilaScreen({
  title,
  eyebrow,
  children,
  scroll = true,
  fallback,
  onBack,
  right,
  stickyFooter,
  showBack = true,
}: {
  title: string;
  eyebrow?: string;
  children: React.ReactNode;
  scroll?: boolean;
  fallback?: string;
  onBack?: () => void;
  right?: React.ReactNode;
  stickyFooter?: React.ReactNode;
  showBack?: boolean;
}) {
  const colors = useColors();
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const body = (
    <View style={[styles.inner, { paddingTop: insets.top + 12, paddingBottom: insets.bottom + (stickyFooter ? 100 : 24) }]}>
      <View style={styles.topbar}>
        {showBack ? (
          <Pressable
            accessibilityLabel="Back"
            accessibilityRole="button"
            onPress={onBack ?? (() => (router.canGoBack() ? router.back() : fallback ? router.replace(fallback as never) : undefined))}
            hitSlop={12}
            style={styles.iconHit}
          >
            <Feather name="arrow-left" size={20} color={colors.primary} />
          </Pressable>
        ) : (
          <View style={styles.iconHit} />
        )}
        <CustomerBrandImage style={styles.topLogo} />
        <View style={styles.iconHit}>{right}</View>
      </View>
      {eyebrow ? <Text style={[styles.eyebrow, { color: colors.primary }]}>{eyebrow}</Text> : null}
      <Text style={[styles.title, { color: colors.foreground }]}>{title}</Text>
      {children}
      <SilaPoweredFooter />
    </View>
  );
  return (
    <View style={{ flex: 1, backgroundColor: colors.background }}>
      {scroll ? <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">{body}</ScrollView> : body}
      {stickyFooter ? (
        <View style={[styles.sticky, { paddingBottom: insets.bottom + 12, borderTopColor: colors.border, backgroundColor: colors.background }]}>
          {stickyFooter}
        </View>
      ) : null}
    </View>
  );
}

export function SilaSearchBar({
  value,
  onChangeText,
  placeholder,
  autoFocus,
}: {
  value: string;
  onChangeText: (value: string) => void;
  placeholder: string;
  autoFocus?: boolean;
}) {
  const colors = useColors();
  return (
    <View style={[styles.search, { borderColor: colors.border, backgroundColor: colors.card }]}>
      <Feather name="search" size={16} color={colors.mutedForeground} />
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={colors.mutedForeground}
        autoCapitalize="none"
        autoCorrect={false}
        autoFocus={autoFocus}
        style={[styles.searchInput, { color: colors.foreground }]}
        accessibilityLabel={placeholder}
      />
    </View>
  );
}

export function SilaActionCard({
  icon,
  label,
  onPress,
  testID,
}: {
  icon: keyof typeof Feather.glyphMap;
  label: string;
  onPress: () => void;
  testID?: string;
}) {
  const colors = useColors();
  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={label}
      onPress={onPress}
      style={({ pressed }) => [styles.actionCard, { borderColor: colors.border, backgroundColor: colors.card, opacity: pressed ? 0.75 : 1 }]}
    >
      <View style={[styles.actionIcon, { backgroundColor: colors.primaryLight }]}>
        <Feather name={icon} size={18} color={colors.primary} />
      </View>
      <Text style={[styles.actionLabel, { color: colors.foreground }]}>{label}</Text>
    </Pressable>
  );
}

export function SilaSummaryCard({ label, value }: { label: string; value: string }) {
  const colors = useColors();
  return (
    <View style={[styles.summaryCard, { borderColor: colors.border, backgroundColor: colors.card }]}>
      <Text style={[styles.summaryValue, { color: colors.foreground }]}>{value}</Text>
      <Text style={[styles.summaryLabel, { color: colors.mutedForeground }]}>{label}</Text>
    </View>
  );
}

export function SilaStatusChip({
  label,
  tone = 'neutral',
}: {
  label: string;
  tone?: 'neutral' | 'success' | 'warning' | 'error' | 'critical' | 'pending' | 'review' | 'failed';
}) {
  const colors = useColors();
  const color =
    tone === 'success' ? colors.success
      : tone === 'warning' || tone === 'pending' || tone === 'review' ? colors.warning
        : tone === 'error' || tone === 'critical' || tone === 'failed' ? colors.error
          : colors.primary;
  return (
    <View style={[styles.chip, { backgroundColor: `${color}14`, borderColor: `${color}40` }]}>
      <Text style={[styles.chipText, { color }]}>{label}</Text>
    </View>
  );
}

export function SilaEmptyState({ title, description }: { title: string; description?: string }) {
  const colors = useColors();
  return (
    <View style={styles.stateBox}>
      <Text style={[styles.stateTitle, { color: colors.foreground }]}>{title}</Text>
      {description ? <Text style={[styles.stateCopy, { color: colors.mutedForeground }]}>{description}</Text> : null}
    </View>
  );
}

export function SilaErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  const colors = useColors();
  return (
    <View style={styles.stateBox}>
      <Text style={[styles.stateTitle, { color: colors.error }]}>{message}</Text>
      {onRetry ? (
        <Pressable accessibilityRole="button" accessibilityLabel="Retry" onPress={onRetry} style={{ marginTop: 12 }}>
          <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold' }}>Retry</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

export function SilaLoadingState({ label = 'Loading…' }: { label?: string }) {
  const colors = useColors();
  return (
    <View style={[styles.stateBox, { alignItems: 'center' }]}>
      <ActivityIndicator color={colors.primary} />
      <Text style={[styles.stateCopy, { color: colors.mutedForeground, marginTop: 10 }]}>{label}</Text>
    </View>
  );
}

export function SilaOfflineBanner() {
  const colors = useColors();
  const { isOnline } = useStoreScope();
  if (isOnline) return null;
  return (
    <View style={[styles.offline, { backgroundColor: colors.warningSurface ?? '#FFF8E8', borderColor: colors.warning }]}>
      <SilaStatusChip label="OFFLINE" tone="warning" />
      <Text style={[styles.offlineText, { color: colors.foreground }]}>You are offline. Drafts can be kept, but posting requires a connection.</Text>
    </View>
  );
}

export function SilaNotConnected({ message }: { message: string }) {
  const colors = useColors();
  return (
    <View style={[styles.notConnected, { borderColor: colors.border, backgroundColor: colors.secondary }]}>
      <Text style={[styles.stateCopy, { color: colors.mutedForeground }]}>{message}</Text>
    </View>
  );
}

export function SilaCard({ children, onPress }: { children: React.ReactNode; onPress?: () => void }) {
  const colors = useColors();
  const body = <View style={[styles.card, { borderColor: colors.border, backgroundColor: colors.card }]}>{children}</View>;
  if (!onPress) return body;
  return (
    <Pressable accessibilityRole="button" onPress={onPress} style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}>
      {body}
    </Pressable>
  );
}

export function SilaQuantityInput({
  label,
  value,
  onChangeText,
}: {
  label: string;
  value: string;
  onChangeText: (value: string) => void;
}) {
  const colors = useColors();
  return (
    <View style={styles.qtyField}>
      <Text style={[styles.qtyLabel, { color: colors.mutedForeground }]}>{label}</Text>
      <TextInput
        value={value}
        onChangeText={onChangeText}
        keyboardType="decimal-pad"
        accessibilityLabel={label}
        style={[styles.qtyInput, { borderColor: colors.border, color: colors.foreground }]}
      />
    </View>
  );
}

export function SilaBottomAction({
  label,
  onPress,
  disabled,
  secondary,
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
      accessibilityRole="button"
      accessibilityLabel={label}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.bottomAction,
        {
          backgroundColor: secondary ? colors.card : colors.primary,
          borderColor: colors.primary,
          opacity: disabled ? 0.45 : pressed ? 0.75 : 1,
        },
      ]}
    >
      <Text style={{ color: secondary ? colors.primary : colors.primaryForeground, fontFamily: 'Inter_600SemiBold', fontSize: 14 }}>{label}</Text>
    </Pressable>
  );
}

export function SilaConfirmDialog({
  visible,
  title,
  message,
  confirmLabel,
  cancelLabel = 'Keep editing',
  onConfirm,
  onCancel,
}: {
  visible: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const colors = useColors();
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <View style={styles.modalOverlay}>
        <View style={[styles.modalCard, { backgroundColor: colors.background, borderColor: colors.border }]}>
          <Text style={[styles.stateTitle, { color: colors.foreground }]}>{title}</Text>
          <Text style={[styles.stateCopy, { color: colors.mutedForeground, marginTop: 8 }]}>{message}</Text>
          <View style={{ marginTop: 18, gap: 10 }}>
            <SilaBottomAction label={cancelLabel} onPress={onCancel} secondary />
            <SilaBottomAction label={confirmLabel} onPress={onConfirm} />
          </View>
        </View>
      </View>
    </Modal>
  );
}

export function useDiscardGuard() {
  const [dirty, setDirty] = useState(false);
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState<(() => void) | null>(null);
  const requestLeave = (leave: () => void) => {
    if (!dirty) {
      leave();
      return;
    }
    setPending(() => leave);
    setOpen(true);
  };
  return {
    dirty,
    setDirty,
    dialog: (
      <SilaConfirmDialog
        visible={open}
        title="Discard this draft?"
        message="Unsaved changes will be lost."
        confirmLabel="Discard"
        onCancel={() => { setOpen(false); setPending(null); }}
        onConfirm={() => { setOpen(false); pending?.(); setPending(null); setDirty(false); }}
      />
    ),
    requestLeave,
  };
}

export { greetingForNow } from '@/services/ops/greeting';

const styles = StyleSheet.create({
  scroll: { paddingHorizontal: 20 },
  inner: { minHeight: '100%' },
  topbar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 18 },
  iconHit: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  topLogo: { width: 112, height: 28 },
  topbarLabel: { fontFamily: 'Inter_600SemiBold', fontSize: 11, letterSpacing: 1.4 },
  eyebrow: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.4 },
  title: { marginTop: 6, marginBottom: 14, fontFamily: 'Inter_700Bold', fontSize: 26, lineHeight: 32, letterSpacing: -0.5 },
  search: { minHeight: 44, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12, flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 14 },
  searchInput: { flex: 1, fontFamily: 'Inter_400Regular', fontSize: 14, minHeight: 44 },
  actionCard: { width: '31%', minHeight: 92, borderWidth: 1, borderRadius: 10, padding: 10, alignItems: 'flex-start', justifyContent: 'space-between' },
  actionIcon: { width: 34, height: 34, borderRadius: 17, alignItems: 'center', justifyContent: 'center' },
  actionLabel: { fontFamily: 'Inter_600SemiBold', fontSize: 11, lineHeight: 15 },
  summaryCard: { width: '31%', minHeight: 78, borderWidth: 1, borderRadius: 10, padding: 12 },
  summaryValue: { fontFamily: 'Inter_700Bold', fontSize: 18 },
  summaryLabel: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 11, lineHeight: 15 },
  chip: { alignSelf: 'flex-start', borderWidth: 1, borderRadius: 100, paddingHorizontal: 10, paddingVertical: 5 },
  chipText: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 0.4, textTransform: 'uppercase' },
  stateBox: { marginTop: 18 },
  stateTitle: { fontFamily: 'Inter_700Bold', fontSize: 16 },
  stateCopy: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
  offline: { borderWidth: 1, borderRadius: 10, padding: 12, marginBottom: 12, gap: 8 },
  offlineText: { fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 17 },
  notConnected: { borderWidth: 1, borderRadius: 10, padding: 14, marginTop: 12 },
  card: { borderWidth: 1, borderRadius: 12, padding: 14, marginBottom: 10 },
  qtyField: { flex: 1, gap: 6 },
  qtyLabel: { fontFamily: 'Inter_600SemiBold', fontSize: 9, letterSpacing: 0.7 },
  qtyInput: { minHeight: 44, borderWidth: 1, borderRadius: 8, paddingHorizontal: 10, fontFamily: 'Inter_600SemiBold', fontSize: 15 },
  sticky: { position: 'absolute', left: 0, right: 0, bottom: 0, borderTopWidth: 1, paddingHorizontal: 20, paddingTop: 12 },
  bottomAction: { minHeight: 48, borderWidth: 1, borderRadius: 10, alignItems: 'center', justifyContent: 'center' },
  modalOverlay: { flex: 1, backgroundColor: 'rgba(16,42,58,0.35)', justifyContent: 'center', padding: 24 },
  modalCard: { borderWidth: 1, borderRadius: 14, padding: 18 },
});
