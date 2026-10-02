import React, { useEffect } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { KeyboardProvider } from 'react-native-keyboard-controller';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import {
  Inter_400Regular,
  Inter_500Medium,
  Inter_600SemiBold,
  Inter_700Bold,
  useFonts,
} from '@expo-google-fonts/inter';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { Platform } from 'react-native';
import { applyTenantContext, setBaseUrl } from '@workspace/api-client-react';
import { AuthProvider } from '@/providers/AuthProvider';
import { BrandingProvider } from '@/components/branding';
import { ScanSessionProvider } from '@/providers/ScanSessionProvider';
import { StoreScopeProvider } from '@/providers/StoreScopeProvider';
import { mobileTenant } from '@/config/mobile-tenant';

function resolveMobileApiBaseUrl(): string | null {
  const configured = process.env.EXPO_PUBLIC_SILA_ME_API_URL;
  if (Platform.OS === 'web' && typeof window !== 'undefined') {
    // Browser sessions must stay same-origin so login works when only the
    // Mobile/Cloud UI port is forwarded. Metro/Vite proxy /api to :8080.
    return window.location.origin;
  }
  return configured ?? null;
}

setBaseUrl(resolveMobileApiBaseUrl());
applyTenantContext(mobileTenant);

SplashScreen.preventAutoHideAsync();

const queryClient = new QueryClient();

function RootLayoutNav() {
  return (
    <Stack screenOptions={{ headerBackTitle: 'Back', headerShown: false }}>
      <Stack.Screen name="index" />
      <Stack.Screen name="login" />
      <Stack.Screen name="(tabs)" />
      <Stack.Screen name="search" />
      <Stack.Screen name="profile" />
      <Stack.Screen name="settings" />
      <Stack.Screen name="help" />
      <Stack.Screen name="about" />
      <Stack.Screen name="documents" />
      <Stack.Screen name="suppliers" />
      <Stack.Screen name="purchase-orders" />
    </Stack>
  );
}

export default function RootLayout() {
  const [fontsLoaded, fontError] = useFonts({
    Inter_400Regular,
    Inter_500Medium,
    Inter_600SemiBold,
    Inter_700Bold,
  });

  useEffect(() => {
    if (fontsLoaded || fontError) {
      SplashScreen.hideAsync();
    }
  }, [fontsLoaded, fontError]);

  if (!fontsLoaded && !fontError) return null;

  return (
    <SafeAreaProvider>
      <ErrorBoundary>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <StoreScopeProvider>
              <BrandingProvider>
                <ScanSessionProvider>
                  <GestureHandlerRootView style={{ flex: 1 }}>
                    <KeyboardProvider>
                      <RootLayoutNav />
                    </KeyboardProvider>
                  </GestureHandlerRootView>
                </ScanSessionProvider>
              </BrandingProvider>
            </StoreScopeProvider>
          </AuthProvider>
        </QueryClientProvider>
      </ErrorBoundary>
    </SafeAreaProvider>
  );
}
