import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { leaveRequestsApi } from '../services/api';
import { COLORS, APP_CONFIG } from '../constants/config';
import { storageService, offlineStorage } from '../services/storage';

interface LeaveBalance {
  leaveType: string;
  available: number;
  used: number;
  total: number;
}

export const LeaveBalanceCard: React.FC = () => {
  const [balances, setBalances] = React.useState<LeaveBalance[]>([]);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    loadBalances();
  }, []);

  const loadBalances = async () => {
    try {
      const data = await leaveRequestsApi.balances();
      if (data && Array.isArray(data)) {
        setBalances(data);
      }
    } catch (error) {
      const cached = await offlineStorage.getOfflineData('leave_balances');
      if (cached) {
        setBalances(cached.data as LeaveBalance[]);
      }
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.container}>
        <Text style={styles.loading}>載入中...</Text>
      </View>
    );
  }

  const totalAvailable = balances.reduce((sum, b) => sum + b.available, 0);
  const totalUsed = balances.reduce((sum, b) => sum + b.used, 0);
  const total = balances.reduce((sum, b) => sum + b.total, 0);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>請假餘額</Text>
      
      <View style={styles.summaryRow}>
        <View style={styles.summaryItem}>
          <Text style={styles.summaryValue}>{totalAvailable}</Text>
          <Text style={styles.summaryLabel}>可用天數</Text>
        </View>
        <View style={styles.summaryItem}>
          <Text style={[styles.summaryValue, styles.usedValue]}>{totalUsed}</Text>
          <Text style={styles.summaryLabel}>已使用</Text>
        </View>
        <View style={styles.summaryItem}>
          <Text style={styles.summaryValue}>{total}</Text>
          <Text style={styles.summaryLabel}>總天數</Text>
        </View>
      </View>

      <View style={styles.breakdown}>
        {balances.map((balance, index) => (
          <View key={index} style={styles.breakdownItem}>
            <View style={styles.breakdownHeader}>
              <Text style={styles.leaveType}>{balance.leaveType}</Text>
              <Text style={styles.breakdownValue}>
                {balance.available} / {balance.total} 天
              </Text>
            </View>
            <View style={styles.progressBar}>
              <View 
                style={[
                  styles.progressFill, 
                  { width: `${(balance.used / balance.total) * 100}%` }
                ]} 
              />
            </View>
          </View>
        ))}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: COLORS.white,
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
    color: COLORS.dark,
    marginBottom: 16,
  },
  summaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 16,
  },
  summaryItem: {
    alignItems: 'center',
  },
  summaryValue: {
    fontSize: 24,
    fontWeight: '700',
    color: COLORS.primary,
  },
  usedValue: {
    color: COLORS.warning,
  },
  summaryLabel: {
    fontSize: 12,
    color: COLORS.gray,
    marginTop: 4,
  },
  breakdown: {
    borderTopWidth: 1,
    borderTopColor: COLORS.lightGray,
    paddingTop: 16,
  },
  breakdownItem: {
    marginBottom: 12,
  },
  breakdownHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 6,
  },
  leaveType: {
    fontSize: 14,
    color: COLORS.dark,
  },
  breakdownValue: {
    fontSize: 14,
    fontWeight: '600',
    color: COLORS.dark,
  },
  progressBar: {
    height: 8,
    backgroundColor: COLORS.lightGray,
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    backgroundColor: COLORS.primary,
    borderRadius: 4,
  },
  loading: {
    fontSize: 14,
    color: COLORS.gray,
    textAlign: 'center',
  },
});

export default LeaveBalanceCard;