import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Text, View, StyleSheet } from 'react-native';

import { useAuth } from '../context/AuthContext';
import { LoginScreen } from '../screens/LoginScreen';
import { ClockInScreen } from '../screens/ClockInScreen';
import { AttendanceHistoryScreen } from '../screens/AttendanceHistoryScreen';
import { LeaveScreen } from '../screens/LeaveScreen';
import { LeaveRequestScreen } from '../screens/LeaveRequestScreen';
import { AnnouncementScreen } from '../screens/AnnouncementScreen';
import { SalaryScreen } from '../screens/SalaryScreen';
import { ProfileScreen } from '../screens/ProfileScreen';
import { OperationsScreen } from '../screens/OperationsScreen';
import { COLORS } from '../constants/config';

const Tab = createBottomTabNavigator();
const Stack = createNativeStackNavigator();

const TabIcon = ({ name, focused }: { name: string; focused: boolean }) => {
  const icons: Record<string, string> = {
    '打卡': '🕐',
    '請假': '📅',
    '公告': '📢',
    '薪資': '💰',
    '我的': '👤',
    '作業': '📦',
  };
  
  return (
    <View style={styles.tabIconContainer}>
      <Text style={[styles.tabIcon, focused && styles.tabIconFocused]}>
        {icons[name] || '●'}
      </Text>
    </View>
  );
};

const ClockInStack = () => (
  <Stack.Navigator
    screenOptions={{
      headerShown: false,
      contentStyle: { backgroundColor: COLORS.background },
    }}
  >
    <Stack.Screen name="ClockInMain" component={ClockInScreen} />
    <Stack.Screen name="AttendanceHistory" component={AttendanceHistoryScreen} />
  </Stack.Navigator>
);

const LeaveStack = () => (
  <Stack.Navigator
    screenOptions={{
      headerShown: false,
      contentStyle: { backgroundColor: COLORS.background },
    }}
  >
    <Stack.Screen name="LeaveMain" component={LeaveScreen} />
    <Stack.Screen name="LeaveRequest" component={LeaveRequestScreen} />
  </Stack.Navigator>
);

const MainTabs: React.FC<{ permissions: string[] }> = ({ permissions }) => {
  const canUseOperations =
    (permissions.includes('inventory:view') && permissions.includes('inventory:execute')) ||
    (permissions.includes('quality:create') && permissions.includes('manufacturing:view'));

  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarIcon: ({ focused }) => <TabIcon name={route.name} focused={focused} />,
        tabBarActiveTintColor: COLORS.primary,
        tabBarInactiveTintColor: COLORS.gray,
        tabBarStyle: styles.tabBar,
        tabBarLabelStyle: styles.tabBarLabel,
      })}
    >
      <Tab.Screen name="打卡" component={ClockInStack} />
      <Tab.Screen name="請假" component={LeaveStack} />
      <Tab.Screen name="公告" component={AnnouncementScreen} />
      <Tab.Screen name="薪資" component={SalaryScreen} />
      <Tab.Screen name="我的" component={ProfileScreen} />
      {canUseOperations && <Tab.Screen name="作業" component={OperationsScreen} />}
    </Tab.Navigator>
  );
};

export const AppNavigator: React.FC = () => {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return (
      <View style={styles.loadingContainer}>
        <Text style={styles.loadingText}>載入中...</Text>
      </View>
    );
  }

  return (
    <NavigationContainer>
      {isAuthenticated ? <MainTabs permissions={user?.permissions ?? []} /> : <LoginScreen />}
    </NavigationContainer>
  );
};

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: COLORS.background,
  },
  loadingText: {
    fontSize: 16,
    color: COLORS.gray,
  },
  tabBar: {
    backgroundColor: '#FFF',
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    paddingTop: 8,
    paddingBottom: 8,
    height: 70,
  },
  tabBarLabel: {
    fontSize: 12,
    fontWeight: '500',
    marginTop: 4,
  },
  tabIconContainer: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  tabIcon: {
    fontSize: 24,
    opacity: 0.6,
  },
  tabIconFocused: {
    opacity: 1,
  },
});