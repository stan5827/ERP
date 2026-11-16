import React, { useCallback, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { leaveRequestsApi } from '../services/api';
import { COLORS } from '../constants/config';
import { LeaveRequest, LeaveBalance, LEAVE_TYPES } from '../types';

import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

type RootStackParamList = {
  LeaveMain: undefined;
  LeaveRequest: undefined;
};

export const LeaveScreen: React.FC = () => {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const [leaveRequests, setLeaveRequests] = useState<LeaveRequest[]>([]);
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState<'requests' | 'balances'>('balances');

  const loadData = useCallback(async () => {
    try {
      const [requestsRes, balancesRes] = await Promise.all([
        leaveRequestsApi.list({ pageSize: '20' }),
        leaveRequestsApi.balances(),
      ]);
      
      setLeaveRequests(requestsRes.data?.data || []);
      setBalances(balancesRes.data?.data || []);
    } catch (error) {
      console.error('Failed to load leave data:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      void loadData();
    }, [loadData])
  );

  const handleRefresh = () => {
    setRefreshing(true);
    loadData();
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'approved': return COLORS.success;
      case 'pending': return COLORS.warning;
      case 'rejected': return COLORS.danger;
      default: return COLORS.gray;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'approved': return '已核准';
      case 'pending': return '待審';
      case 'rejected': return '已退回';
      default: return status;
    }
  };

  const getLeaveTypeLabel = (type: string) => {
    const found = LEAVE_TYPES.find(t => t.value === type);
    return found?.label || type;
  };

  const formatDate = (dateStr: string) => {
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
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.contentContainer}
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
      }
    >
      <View style={styles.header}>
        <Text style={styles.headerTitle}>請假</Text>
        <Text style={styles.headerSubtitle}>Leave Management</Text>
      </View>

      <View style={styles.tabContainer}>
        <TouchableOpacity
          style={[styles.tab, activeTab === 'balances' && styles.tabActive]}
          onPress={() => setActiveTab('balances')}
        >
          <Text style={[styles.tabText, activeTab === 'balances' && styles.tabTextActive]}>
            假期餘額
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.tab, activeTab === 'requests' && styles.tabActive]}
          onPress={() => setActiveTab('requests')}
        >
          <Text style={[styles.tabText, activeTab === 'requests' && styles.tabTextActive]}>
            請假紀錄
          </Text>
        </TouchableOpacity>
      </View>

      {activeTab === 'balances' ? (
        <View style={styles.balancesContainer}>
          {balances.length === 0 ? (
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>尚無假期資料</Text>
            </View>
          ) : (
            balances.map((balance, index) => {
              const usedPercent = balance.totalDays > 0
                ? Math.min(100, Math.round((balance.usedDays / balance.totalDays) * 100))
                : 0;
              return (
                <View key={index} style={styles.balanceCard}>
                  <View style={styles.balanceHeader}>
                    <Text style={styles.leaveTypeLabel}>{getLeaveTypeLabel(balance.leaveType)}</Text>
                    <Text style={styles.balanceYear}>{balance.year} 年度</Text>
                  </View>

                  <View style={styles.balanceRow}>
                    <View style={styles.balanceItem}>
                      <Text style={styles.balanceValue}>{balance.totalDays}</Text>
                      <Text style={styles.balanceLabel}>總天數</Text>
                    </View>
                    <View style={styles.balanceItem}>
                      <Text style={[styles.balanceValue, { color: COLORS.danger }]}>{balance.usedDays}</Text>
                      <Text style={styles.balanceLabel}>已使用</Text>
                    </View>
                    <View style={styles.balanceItem}>
                      <Text style={[styles.balanceValue, { color: COLORS.success }]}>{balance.remainingDays}</Text>
                      <Text style={styles.balanceLabel}>剩餘</Text>
                    </View>
                  </View>

                  <View style={styles.progressContainer}>
                    <View style={styles.progressBar}>
                      <View
                        style={[
                          styles.progressFill,
                          { width: `${usedPercent}%` },
                        ]}
                      />
                    </View>
                    <Text style={styles.progressText}>
                      已使用 {usedPercent}%
                    </Text>
                  </View>
                </View>
              );
            })
          )}
        </View>
      ) : (
        <View style={styles.requestsContainer}>
          {leaveRequests.length === 0 ? (
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>尚無請假紀錄</Text>
            </View>
          ) : (
            leaveRequests.map((request, index) => (
              <View key={index} style={styles.requestCard}>
                <View style={styles.requestHeader}>
                  <View>
                    <Text style={styles.requestType}>{getLeaveTypeLabel(request.leaveType)}</Text>
                    <Text style={styles.requestNo}>單號：{request.applicationNo}</Text>
                  </View>
                  <View style={[styles.statusBadge, { backgroundColor: getStatusColor(request.status) + '20' }]}>
                    <Text style={[styles.statusText, { color: getStatusColor(request.status) }]}>
                      {getStatusText(request.status)}
                    </Text>
                  </View>
                </View>
                
                <View style={styles.requestRow}>
                  <Text style={styles.requestLabel}>期間</Text>
                  <Text style={styles.requestValue}>
                    {formatDate(request.startDate)} ~ {formatDate(request.endDate)}
                  </Text>
                </View>
                
                <View style={styles.requestRow}>
                  <Text style={styles.requestLabel}>時數</Text>
                  <Text style={styles.requestValue}>{request.totalHours} 小時</Text>
                </View>
                
                {request.reason && (
                  <View style={styles.requestRow}>
                    <Text style={styles.requestLabel}>原因</Text>
                    <Text style={styles.requestValue}>{request.reason}</Text>
                  </View>
                )}
              </View>
            ))
          )}
        </View>
      )}

      <TouchableOpacity
        style={styles.applyButton}
        onPress={() => navigation.navigate('LeaveRequest')}
      >
        <Text style={styles.applyButtonText}>+ 申請請假</Text>
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
    marginBottom: 20,
  },
  headerTitle: {
    fontSize: 28,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  headerSubtitle: {
    fontSize: 14,
    color: COLORS.gray,
    marginTop: 4,
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: '#FFF',
    borderRadius: 12,
    padding: 4,
    marginBottom: 16,
  },
  tab: {
    flex: 1,
    paddingVertical: 12,
    alignItems: 'center',
    borderRadius: 10,
  },
  tabActive: {
    backgroundColor: COLORS.primary,
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
    color: COLORS.gray,
  },
  tabTextActive: {
    color: '#FFF',
  },
  balancesContainer: {
    gap: 12,
  },
  balanceCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 16,
  },
  balanceHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  leaveTypeLabel: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  balanceYear: {
    fontSize: 12,
    color: COLORS.gray,
  },
  balanceRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginBottom: 16,
  },
  balanceItem: {
    alignItems: 'center',
  },
  balanceValue: {
    fontSize: 24,
    fontWeight: 'bold',
    color: COLORS.primary,
  },
  balanceLabel: {
    fontSize: 12,
    color: COLORS.gray,
    marginTop: 4,
  },
  progressContainer: {
    marginTop: 8,
  },
  progressBar: {
    height: 8,
    backgroundColor: '#E5E7EB',
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    backgroundColor: COLORS.primary,
    borderRadius: 4,
  },
  progressText: {
    fontSize: 12,
    color: COLORS.gray,
    marginTop: 8,
    textAlign: 'right',
  },
  requestsContainer: {
    gap: 12,
  },
  requestCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 16,
  },
  requestHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  requestType: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  requestNo: {
    fontSize: 12,
    color: COLORS.gray,
    marginTop: 4,
  },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
  },
  requestRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
  },
  requestLabel: {
    fontSize: 14,
    color: COLORS.gray,
  },
  requestValue: {
    fontSize: 14,
    fontWeight: '500',
    color: COLORS.dark,
  },
  emptyContainer: {
    padding: 40,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 16,
    color: COLORS.gray,
  },
  applyButton: {
    backgroundColor: COLORS.primary,
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    marginTop: 20,
  },
  applyButtonText: {
    color: '#FFF',
    fontSize: 16,
    fontWeight: 'bold',
  },
});