import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { employeesApi } from '../services/api';
import { COLORS } from '../constants/config';
import { Employee } from '../types';

export const ProfileScreen: React.FC = () => {
  const { user, logout } = useAuth();
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadEmployeeData();
  }, []);

  const loadEmployeeData = async () => {
    try {
      if (user?.employeeId) {
        const response = await employeesApi.get(user.employeeId);
        setEmployee(response.data?.data);
      }
    } catch (error) {
      console.error('Failed to load employee data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = () => {
    Alert.alert(
      '登出',
      '確定要登出嗎？',
      [
        { text: '取消', style: 'cancel' },
        { text: '確定', onPress: logout },
      ]
    );
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '—';
    return dateStr.split('T')[0];
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={COLORS.primary} />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.contentContainer}>
      <View style={styles.header}>
        <View style={styles.avatarContainer}>
          <Text style={styles.avatarText}>
            {employee?.chineseName?.charAt(0) || user?.username?.charAt(0) || '?'}
          </Text>
        </View>
        <Text style={styles.name}>
          {employee?.chineseName || user?.employeeName || user?.username || '員工'}
        </Text>
        <Text style={styles.employeeNo}>
          員工編號：{employee?.employeeNo || user?.userId || '—'}
        </Text>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>組織資訊</Text>
        
        <View style={styles.infoCard}>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>部門</Text>
            <Text style={styles.infoValue}>{employee?.departmentName || '—'}</Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>職位</Text>
            <Text style={styles.infoValue}>{employee?.jobTitleName || '—'}</Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>到職日</Text>
            <Text style={styles.infoValue}>{formatDate(employee?.hireDate)}</Text>
          </View>
        </View>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>聯絡資訊</Text>
        
        <View style={styles.infoCard}>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>手機</Text>
            <Text style={styles.infoValue}>{employee?.mobile || '—'}</Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Email</Text>
            <Text style={styles.infoValue}>{employee?.email || user?.email || '—'}</Text>
          </View>
        </View>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>社福資訊</Text>
        
        <View style={styles.infoCard}>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>勞保投保等級</Text>
            <Text style={styles.infoValue}>—</Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>健保投保等級</Text>
            <Text style={styles.infoValue}>—</Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>勞退提繳率</Text>
            <Text style={styles.infoValue}>6%</Text>
          </View>
        </View>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>帳戶設定</Text>
        
        <TouchableOpacity style={styles.menuItem}>
          <Text style={styles.menuText}>🔔 通知設定</Text>
          <Text style={styles.menuArrow}>›</Text>
        </TouchableOpacity>
        
        <TouchableOpacity style={styles.menuItem}>
          <Text style={styles.menuText}>🔐 隱私權設定</Text>
          <Text style={styles.menuArrow}>›</Text>
        </TouchableOpacity>
        
        <TouchableOpacity style={styles.menuItem}>
          <Text style={styles.menuText}>❓ 幫助與支援</Text>
          <Text style={styles.menuArrow}>›</Text>
        </TouchableOpacity>
        
        <TouchableOpacity style={styles.menuItem}>
          <Text style={styles.menuText}>📋 版本資訊</Text>
          <Text style={styles.menuValue}>v1.0.0</Text>
        </TouchableOpacity>
      </View>

      <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
        <Text style={styles.logoutButtonText}>登出</Text>
      </TouchableOpacity>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.background,
  },
  contentContainer: {
    padding: 16,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    alignItems: 'center',
    paddingVertical: 24,
    backgroundColor: '#FFF',
    borderRadius: 16,
    marginBottom: 16,
  },
  avatarContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: COLORS.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 12,
  },
  avatarText: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#FFF',
  },
  name: {
    fontSize: 24,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  employeeNo: {
    fontSize: 14,
    color: COLORS.gray,
    marginTop: 4,
  },
  section: {
    marginBottom: 16,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
    marginBottom: 12,
  },
  infoCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 16,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  infoLabel: {
    fontSize: 14,
    color: COLORS.gray,
  },
  infoValue: {
    fontSize: 14,
    fontWeight: '500',
    color: COLORS.dark,
  },
  menuItem: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 16,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  menuText: {
    fontSize: 16,
    color: COLORS.dark,
  },
  menuArrow: {
    fontSize: 20,
    color: COLORS.gray,
  },
  menuValue: {
    fontSize: 14,
    color: COLORS.gray,
  },
  logoutButton: {
    backgroundColor: COLORS.danger,
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    marginTop: 20,
    marginBottom: 40,
  },
  logoutButtonText: {
    color: '#FFF',
    fontSize: 16,
    fontWeight: 'bold',
  },
});