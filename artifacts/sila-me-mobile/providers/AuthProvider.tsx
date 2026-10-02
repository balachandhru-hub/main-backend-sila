import * as SecureStore from 'expo-secure-store';
import {
  getGetCurrentUserQueryKey,
  setAuthTokenGetter,
  useGetCurrentUser,
  useMobileLogin,
  useMobileLogout,
} from '@workspace/api-client-react';
import type { CurrentUser } from '@workspace/api-client-react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { Platform } from 'react-native';

export const MOBILE_TOKEN_KEY = 'sila_me_mobile_session';

type AuthContextValue = {
  user: CurrentUser | null;
  isLoading: boolean;
  isLoggingIn: boolean;
  errorMessage: string | null;
  login: (email: string, password: string) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const storage = {
  getItem: (key: string) =>
    Platform.OS === 'web' ? AsyncStorage.getItem(key) : SecureStore.getItemAsync(key),
  setItem: (key: string, value: string) =>
    Platform.OS === 'web' ? AsyncStorage.setItem(key, value) : SecureStore.setItemAsync(key, value),
  deleteItem: (key: string) =>
    Platform.OS === 'web' ? AsyncStorage.removeItem(key) : SecureStore.deleteItemAsync(key),
};

setAuthTokenGetter(() => storage.getItem(MOBILE_TOKEN_KEY));

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isBooting, setIsBooting] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const currentUserQuery = useGetCurrentUser({
    query: {
      enabled: Boolean(token),
      queryKey: getGetCurrentUserQueryKey(),
      retry: false,
    },
  });
  const loginMutation = useMobileLogin();
  const logoutMutation = useMobileLogout();

  useEffect(() => {
    let cancelled = false;
    storage.getItem(MOBILE_TOKEN_KEY).then((storedToken) => {
      if (!cancelled) {
        setToken(storedToken);
        setIsBooting(false);
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (currentUserQuery.data) {
      setUser(currentUserQuery.data);
      setErrorMessage(null);
    }
  }, [currentUserQuery.data]);

  useEffect(() => {
    if (currentUserQuery.isError) {
      storage.deleteItem(MOBILE_TOKEN_KEY);
      setToken(null);
      setUser(null);
      setIsBooting(false);
    }
  }, [currentUserQuery.isError]);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isLoading: isBooting || (Boolean(token) && currentUserQuery.isPending),
    isLoggingIn: loginMutation.isPending,
    errorMessage,
    login: (email, password) => {
      setErrorMessage(null);
      loginMutation.mutate(
        { data: { email: email.trim(), password } },
        {
          onSuccess: async (response) => {
            await storage.setItem(MOBILE_TOKEN_KEY, response.token);
            setToken(response.token);
            setUser(response.user);
          },
          onError: (error) => {
            if (error instanceof TypeError || (error instanceof Error && /failed to fetch|network request failed/i.test(error.message))) {
              setErrorMessage('Cannot reach the SILA ME API. Confirm the API is running and port 8080 is forwarded.');
              return;
            }
            const status = typeof error === 'object' && error !== null && 'status' in error
              ? Number((error as { status: number }).status)
              : undefined;
            if (status === 401) {
              setErrorMessage('We could not sign you in. Check your details and try again.');
              return;
            }
            setErrorMessage('We could not sign you in. Check your details and try again.');
          },
        },
      );
    },
    logout: () => {
      logoutMutation.mutate(undefined, {
        onSettled: async () => {
          await storage.deleteItem(MOBILE_TOKEN_KEY);
          setToken(null);
          setUser(null);
        },
      });
    },
  }), [currentUserQuery.isPending, errorMessage, isBooting, loginMutation, logoutMutation, token, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error('useAuth must be used inside AuthProvider');
  }
  return value;
}