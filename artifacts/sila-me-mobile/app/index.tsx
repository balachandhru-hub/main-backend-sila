import { Redirect } from 'expo-router';
import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { useColors } from '@/hooks/useColors';
import { useAuth } from '@/providers/AuthProvider';

export default function IndexScreen() {
  const { user, isLoading } = useAuth();
  const colors = useColors();

  if (isLoading) {
    return (
      <View style={[styles.loading, { backgroundColor: colors.background }]}>
        <ActivityIndicator color={colors.accent} />
      </View>
    );
  }

  return <Redirect href={user ? '/home' : '/login'} />;
}

const styles = StyleSheet.create({
  loading: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
});