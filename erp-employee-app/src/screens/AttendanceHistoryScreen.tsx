import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { attendanceApi } from '../services/api';
import { COLORS } from '../constants/config';
import { AttendanceRecord } from '../types';

export const AttendanceHistoryScreen: React.FC = () => {
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    loadRecords();
  }, []);

  const loadRecords = async () => {
    try {
      const response = await attendanceApi.list({ pageSize: '30' });
      setRecords(response.data?.data || []);
    } catch (error) {
      console.error('Failed to load records:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleRefresh = () => {
    setRefreshing(true);
    loadRecords();
  };

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return `${date.getMonth() + 1}/${date.getDate()} ${['日', '一', '二', '三', '四', '五', '六'][date.getDay()]}`;
  };

  const formatAttendanceTime = (time?: string) => {
    if (!time) return '—';
    return /^\d{2}:\d{2}/.test(time)
      ? time.slice(0, 5)
      : new Date(time).toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit' });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'normal': return COLORS.success;
      case 'late': return COLORS.warning;
      case 'absent': return COLORS.danger;
      default: return COLORS.gray;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'normal': return '正常';
      case 'late': return '遲到';
      case 'absent': return '缺勤';
      default: return '待審';
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={COLORS.primary} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>打卡歷史</Text>
      </View>

      <FlatList
        data={records}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
        }
        renderItem={({ item }) => (
          <View style={styles.recordCard}>
            <View style={styles.recordHeader}>
              <Text style={styles.recordDate}>{formatDate(item.date)}</Text>
              <View style={[styles.statusBadge, { backgroundColor: getStatusColor(item.status) + '20' }]}>
                <Text style={[styles.statusText, { color: getStatusColor(item.status) }]}>
                  {getStatusText(item.status)}
                </Text>
              </View>
            </View>

            <View style={styles.recordRow}>
              <Text style={styles.recordLabel}>上班</Text>
              <Text style={styles.recordValue}>{formatAttendanceTime(item.clockIn)}</Text>
            </View>

            <View style={styles.recordRow}>
              <Text style={styles.recordLabel}>下班</Text>
              <Text style={styles.recordValue}>{formatAttendanceTime(item.clockOut)}</Text>
            </View>

            {item.lateMinutes && item.lateMinutes > 0 && (
              <View style={styles.recordRow}>
                <Text style={styles.recordLabel}>遲到</Text>
                <Text style={[styles.recordValue, { color: COLORS.warning }]}>
                  {item.lateMinutes} 分鐘
                </Text>
              </View>
            )}
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>尚無打卡記錄</Text>
          </View>
        }
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.background,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    padding: 16,
    backgroundColor: '#FFF',
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  listContent: {
    padding: 16,
  },
  recordCard: {
    backgroundColor: '#FFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  recordHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  recordDate: {
    fontSize: 16,
    fontWeight: '600',
    color: COLORS.dark,
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
  recordRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
  },
  recordLabel: {
    fontSize: 14,
    color: COLORS.gray,
  },
  recordValue: {
    fontSize: 14,
    fontWeight: '500',
    color: COLORS.dark,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingTop: 60,
  },
  emptyText: {
    fontSize: 16,
    color: COLORS.gray,
  },
});