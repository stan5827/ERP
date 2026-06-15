import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  Alert,
} from 'react-native';
import * as FileSystem from 'expo-file-system';
import * as Sharing from 'expo-sharing';
import { useAuth } from '../context/AuthContext';
import { payrollApi } from '../services/api';
import { COLORS } from '../constants/config';
import { PayrollRecord } from '../types';

export const SalaryScreen: React.FC = () => {
  const { user } = useAuth();
  const [payrolls, setPayrolls] = useState<PayrollRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedPayroll, setSelectedPayroll] = useState<PayrollRecord | null>(null);

  useEffect(() => {
    loadPayrolls();
  }, []);

  const loadPayrolls = async () => {
    try {
      const response = await payrollApi.list({ pageSize: '12' });
      setPayrolls(response.data?.data || []);
    } catch (error) {
      console.error('Failed to load payrolls:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleRefresh = () => {
    setRefreshing(true);
    loadPayrolls();
  };

  const formatCurrency = (amount: number) => {
    return amount.toLocaleString('zh-TW', { minimumFractionDigits: 0 });
  };

  const formatMonth = (dateStr: string) => {
    const date = new Date(dateStr);
    return `${date.getFullYear()}年${date.getMonth() + 1}月`;
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'paid': return COLORS.success;
      case 'approved': return COLORS.primary;
      case 'draft': return COLORS.gray;
      default: return COLORS.gray;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'paid': return '已發放';
      case 'approved': return '待發放';
      case 'draft': return '核算中';
      default: return status;
    }
  };

  const toggleDetail = (payroll: PayrollRecord) => {
    setSelectedPayroll(selectedPayroll?.id === payroll.id ? null : payroll);
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
        <Text style={styles.headerTitle}>薪資查詢</Text>
        <Text style={styles.headerSubtitle}>Salary Information</Text>
      </View>

      <FlatList
        data={payrolls}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
        }
        renderItem={({ item }) => (
          <View style={styles.payrollCard}>
            <TouchableOpacity onPress={() => toggleDetail(item)}>
              <View style={styles.cardHeader}>
                <View>
                  <Text style={styles.periodText}>{formatMonth(item.periodStart)}</Text>
                  <Text style={styles.employeeName}>{user?.employeeName || '員工'}</Text>
                </View>
                <View style={styles.amountContainer}>
                  <Text style={styles.netPayText}>NT$ {formatCurrency(item.netPay)}</Text>
                  <View style={[styles.statusBadge, { backgroundColor: getStatusColor(item.status) + '20' }]}>
                    <Text style={[styles.statusText, { color: getStatusColor(item.status) }]}>
                      {getStatusText(item.status)}
                    </Text>
                  </View>
                </View>
              </View>
            </TouchableOpacity>

            {selectedPayroll?.id === item.id && (
              <View style={styles.detailContainer}>
                <View style={styles.detailSection}>
                  <Text style={styles.detailTitle}>💰 收入項目</Text>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>基本薪資</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.basicSalary)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>伙食津貼</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.foodAllowance)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>職務加給</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.positionAllowance)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>加班費</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.overtimePay)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>獎金</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.bonus)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>全勤獎金</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.attendanceBonus)}</Text>
                  </View>
                  <View style={[styles.detailRow, styles.totalRow]}>
                    <Text style={styles.totalLabel}>應發薪資</Text>
                    <Text style={[styles.totalValue, { color: COLORS.success }]}>
                      {formatCurrency(item.grossPay)}
                    </Text>
                  </View>
                </View>

                <View style={styles.detailSection}>
                  <Text style={styles.detailTitle}>📉 扣款項目</Text>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>勞保扣款</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.laborInsuranceDeduction)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>健保扣款</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.healthInsuranceDeduction)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>勞退提繳</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.pensionDeduction)}</Text>
                  </View>
                  <View style={styles.detailRow}>
                    <Text style={styles.detailLabel}>所得稅</Text>
                    <Text style={styles.detailValue}>{formatCurrency(item.withholdingTax)}</Text>
                  </View>
                  <View style={[styles.detailRow, styles.totalRow]}>
                    <Text style={styles.totalLabel}>總扣款</Text>
                    <Text style={[styles.totalValue, { color: COLORS.danger }]}>
                      -{formatCurrency(item.totalDeductions)}
                    </Text>
                  </View>
                </View>

                <View style={styles.summaryRow}>
                  <Text style={styles.summaryLabel}>實發薪資</Text>
                  <Text style={styles.summaryValue}>NT$ {formatCurrency(item.netPay)}</Text>
                </View>

                {item.paymentDate && (
                  <Text style={styles.paymentDate}>
                    發放日期：{item.paymentDate}
                  </Text>
                )}
              </View>
            )}
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>尚無薪資記錄</Text>
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
    fontSize: 28,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  headerSubtitle: {
    fontSize: 14,
    color: COLORS.gray,
    marginTop: 4,
  },
  listContent: {
    padding: 16,
  },
  payrollCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    marginBottom: 12,
    overflow: 'hidden',
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
  },
  periodText: {
    fontSize: 18,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  employeeName: {
    fontSize: 14,
    color: COLORS.gray,
    marginTop: 4,
  },
  amountContainer: {
    alignItems: 'flex-end',
  },
  netPayText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: COLORS.success,
  },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    marginTop: 4,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
  },
  detailContainer: {
    backgroundColor: '#F9FAFB',
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  detailSection: {
    marginBottom: 16,
  },
  detailTitle: {
    fontSize: 14,
    fontWeight: 'bold',
    color: COLORS.dark,
    marginBottom: 12,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 6,
  },
  detailLabel: {
    fontSize: 14,
    color: COLORS.gray,
  },
  detailValue: {
    fontSize: 14,
    fontWeight: '500',
    color: COLORS.dark,
  },
  totalRow: {
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    marginTop: 8,
    paddingTop: 8,
  },
  totalLabel: {
    fontSize: 14,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  totalValue: {
    fontSize: 14,
    fontWeight: 'bold',
  },
  summaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderTopWidth: 2,
    borderTopColor: COLORS.primary,
    marginTop: 8,
  },
  summaryLabel: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  summaryValue: {
    fontSize: 20,
    fontWeight: 'bold',
    color: COLORS.primary,
  },
  paymentDate: {
    fontSize: 12,
    color: COLORS.gray,
    marginTop: 8,
    textAlign: 'right',
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