import { Feather } from '@expo/vector-icons';
import { Tabs } from 'expo-router';
import React from 'react';
import { useColors } from '@/hooks/useColors';

export default function TabsLayout() {
  const colors = useColors();
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.mutedForeground,
        tabBarStyle: {
          backgroundColor: colors.background,
          borderTopColor: colors.border,
          height: 64,
          paddingBottom: 8,
          paddingTop: 8,
        },
        tabBarLabelStyle: { fontFamily: 'Inter_600SemiBold', fontSize: 10 },
      }}
    >
      <Tabs.Screen
        name="home"
        options={{
          title: 'Home',
          tabBarIcon: ({ color, size }) => <Feather name="home" color={color} size={size} />,
          tabBarAccessibilityLabel: 'Home',
        }}
      />
      <Tabs.Screen
        name="receive"
        options={{
          title: 'Receive',
          tabBarIcon: ({ color, size }) => <Feather name="inbox" color={color} size={size} />,
          tabBarAccessibilityLabel: 'Receive',
        }}
      />
      <Tabs.Screen
        name="inventory"
        options={{
          title: 'Inventory',
          tabBarIcon: ({ color, size }) => <Feather name="package" color={color} size={size} />,
          tabBarAccessibilityLabel: 'Inventory',
        }}
      />
      <Tabs.Screen
        name="tasks"
        options={{
          title: 'Tasks',
          tabBarIcon: ({ color, size }) => <Feather name="check-square" color={color} size={size} />,
          tabBarAccessibilityLabel: 'Tasks',
        }}
      />
      <Tabs.Screen
        name="more"
        options={{
          title: 'More',
          tabBarIcon: ({ color, size }) => <Feather name="menu" color={color} size={size} />,
          tabBarAccessibilityLabel: 'More',
        }}
      />
    </Tabs>
  );
}
