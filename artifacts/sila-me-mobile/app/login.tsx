import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Image,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useColors } from '@/hooks/useColors';
import { useAuth } from '@/providers/AuthProvider';
import { SilaPoweredFooter } from '@/components/branding';
import { mobileTenant } from '@/config/mobile-tenant';

export default function LoginScreen() {
  const colors = useColors();
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const { login, isLoggingIn, errorMessage, user } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [validationMessage, setValidationMessage] = useState<string | null>(null);

  useEffect(() => {
    if (user) router.replace('/home');
  }, [router, user]);

  const submit = () => {
    if (!email.trim() || !password) {
      setValidationMessage('Enter your email and password to continue.');
      return;
    }
    setValidationMessage(null);
    login(email, password);
  };

  const message = validationMessage ?? errorMessage;
  return (
    <KeyboardAvoidingView
      style={[styles.flex, { backgroundColor: colors.background }]}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView
        contentContainerStyle={[
          styles.scroll,
          {
            backgroundColor: colors.background,
            paddingTop: insets.top + 28,
            paddingBottom: insets.bottom + 28,
          },
        ]}
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.brandRow}>
          <Image
            source={require('../assets/images/sila-logo.png')}
            style={styles.brandLogo}
            resizeMode="contain"
            accessibilityLabel="SILA logo"
          />
        </View>

        <View style={styles.copy}>
          <Text style={[styles.eyebrow, { color: colors.mutedForeground }]}>SILA ME</Text>
          <Text style={[styles.title, { color: colors.foreground }]}>{mobileTenant.tenantName}</Text>
          <Text style={[styles.subtitle, { color: colors.mutedForeground }]}>{mobileTenant.environment}</Text>
        </View>

        <View style={[styles.formCard, { backgroundColor: colors.card, borderColor: colors.border }]}>
          <Text style={[styles.formTitle, { color: colors.foreground }]}>Sign in to SILA ME</Text>
          <Text style={[styles.formHint, { color: colors.mutedForeground }]}>
            Use your Mobile workspace credentials.
          </Text>

          <Text style={[styles.label, { color: colors.foreground }]}>EMAIL</Text>
          <TextInput
            testID="mobile-email-input"
            autoCapitalize="none"
            autoComplete="email"
            autoCorrect={false}
            keyboardType="email-address"
            placeholder="you@company.com"
            placeholderTextColor={colors.mutedForeground}
            style={[
              styles.input,
              { color: colors.foreground, borderColor: colors.input, backgroundColor: colors.card },
            ]}
            value={email}
            onChangeText={setEmail}
          />

          <Text style={[styles.label, { color: colors.foreground }]}>PASSWORD</Text>
          <View style={styles.passwordWrap}>
            <TextInput
              testID="mobile-password-input"
              autoCapitalize="none"
              autoComplete="password"
              placeholder="Your password"
              placeholderTextColor={colors.mutedForeground}
              secureTextEntry={!showPassword}
              style={[
                styles.input,
                styles.passwordInput,
                { color: colors.foreground, borderColor: colors.input, backgroundColor: colors.card },
              ]}
              value={password}
              onChangeText={setPassword}
            />
            <Pressable
              accessibilityLabel={showPassword ? 'Hide password' : 'Show password'}
              hitSlop={10}
              onPress={() => setShowPassword((visible) => !visible)}
              style={styles.eyeButton}
            >
              <Feather name={showPassword ? 'eye-off' : 'eye'} size={18} color={colors.primary} />
            </Pressable>
          </View>

          {message ? (
            <View style={[styles.error, { backgroundColor: `${colors.destructive}18`, borderColor: `${colors.destructive}55` }]}>
              <Feather name="alert-circle" size={16} color={colors.destructive} />
              <Text style={[styles.errorText, { color: colors.destructive }]}>{message}</Text>
            </View>
          ) : null}

          <Pressable
            testID="mobile-login-button"
            accessibilityRole="button"
            disabled={isLoggingIn}
            onPress={submit}
            style={({ pressed }) => [
              styles.submit,
              { backgroundColor: colors.primary, opacity: pressed || isLoggingIn ? 0.78 : 1 },
            ]}
          >
            {isLoggingIn ? <ActivityIndicator color={colors.primaryForeground} /> : <Text style={[styles.submitText, { color: colors.primaryForeground }]}>LOGIN</Text>}
          </Pressable>
        </View>

        <SilaPoweredFooter />
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  scroll: { flexGrow: 1, paddingHorizontal: 24, justifyContent: 'center' },
  brandRow: { alignItems: 'center' },
  brandLogo: { width: 190, height: 46 },
  copy: { marginTop: 62, marginBottom: 28 },
  eyebrow: { fontFamily: 'Inter_600SemiBold', fontSize: 11, letterSpacing: 1.7 },
  title: { marginTop: 12, fontFamily: 'Inter_700Bold', fontSize: 36, letterSpacing: -1.4, lineHeight: 42 },
  subtitle: { marginTop: 10, fontFamily: 'Inter_400Regular', fontSize: 15, lineHeight: 23 },
  formCard: { borderWidth: 1, borderRadius: 16, padding: 20 },
  formTitle: { fontFamily: 'Inter_700Bold', fontSize: 20 },
  formHint: { marginTop: 6, marginBottom: 26, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
  label: { marginBottom: 8, fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.4 },
  input: { height: 50, borderWidth: 1, borderRadius: 10, paddingHorizontal: 14, fontFamily: 'Inter_400Regular', fontSize: 15, marginBottom: 18 },
  passwordWrap: { position: 'relative' },
  passwordInput: { paddingRight: 45, marginBottom: 0 },
  eyeButton: { position: 'absolute', right: 14, top: 16 },
  error: { flexDirection: 'row', alignItems: 'flex-start', gap: 8, borderWidth: 1, borderRadius: 10, padding: 11, marginTop: 14 },
  errorText: { flex: 1, fontFamily: 'Inter_500Medium', fontSize: 12, lineHeight: 18 },
  submit: { height: 52, alignItems: 'center', justifyContent: 'center', borderRadius: 10, marginTop: 18 },
  submitText: { fontFamily: 'Inter_700Bold', fontSize: 14, letterSpacing: 0.3 },
  footer: { flexDirection: 'row', alignItems: 'center', gap: 10, marginTop: 26 },
  footerLine: { width: 28, height: 1 },
  footerText: { fontFamily: 'Inter_500Medium', fontSize: 9, letterSpacing: 1.2 },
});