import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, FlatList, TouchableOpacity, RefreshControl } from 'react-native';
import { approvalApi } from '../services/api';
import { COLORS } from '../constants/config';

interface ApprovalItem {
  id: string;
  type: string;
  requesterName: string;
  requestDate: string;
  description: string;
  status: string;
}

export const ApprovalScreen: React.FC = () => {
  const [approvals, setApprovals] = useState<ApprovalItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    loadApprovals();
  }, []);

  const loadApprovals = async () => {
    try {
      const data = await approvalApi.listPending();
      if (data && Array.isArray(data)) {
        setApprovals(data);
      }
    } catch (error) {
      console.error('Load approvals error:', error);
    } finally {
      setLoading(false);
    }
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadApprovals();
    setRefreshing(false);
  };

  const handleApprove = async (id: string) => {
    try {
      await approvalApi.approve(id, { status: 'approved' });
      setApprovals(prev => prev.filter(item => item.id !== id));
    } catch (error) {
      console.error('Approve error:', error);
    }
  };

  const handleReject = async (id: string) => {
    try {
      await approvalApi.reject(id, 'Rejected from mobile app');
      setApprovals(prev => prev.filter(item => item.id !== id));
    } catch (error) {
      console.error('Reject error:', error);
    }
  };

  const renderItem = ({ item }: { item: ApprovalItem }) => (
    <View style={styles.card}>
      <View style={styles.cardHeader}>
        <Text style={styles.cardType}>{item.type}</Text>
        <Text style={styles.cardDate}>{item.requestDate}</Text>
      </View>
      <Text style={styles.cardRequester}>{item.requesterName}</Text>
      <Text style={styles.cardDescription}>{item.description}</Text>
      <View style={styles.cardActions}>
        <TouchableOpacity
          style={[styles.actionButton, styles.rejectButton]}
          onPress={() => handleReject(item.id)}
        >
          <Text style={styles.rejectButtonText}>拒絕</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.actionButton, styles.approveButton]}
          onPress={() => handleApprove(item.id)}
        >
          <Text style={styles.approveButtonText}>核准</Text>
        </TouchableOpacity>
      </View>
    </View>
  );

  if (loading) {
    return (
      <View style={styles.container}>
        <Text style={styles.loadingText}>載入中...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>待審核事項</Text>
        <Text style={styles.headerCount}>{approvals.length}</Text>
      </View>

      <FlatList
        data={approvals}
        keyExtractor={(item) => item.id}
        renderItem={renderItem}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
        ListEmptyComponent={
          <Text style={styles.emptyText}>目前沒有待審核的事項</Text>
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
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    backgroundColor: COLORS.white,
    borderBottomWidth: 1,
    borderBottomColor: COLORS.lightGray,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: COLORS.dark,
  },
  headerCount: {
    fontSize: 14,
    fontWeight: '600',
    color: COLORS.white,
    backgroundColor: COLORS.primary,
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 12,
    marginLeft: 8,
    overflow: 'hidden',
  },
  listContent: {
    padding: 16,
  },
  card: {
    backgroundColor: COLORS.white,
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  cardType: {
    fontSize: 14,
    fontWeight: '600',
    color: COLORS.primary,
  },
  cardDate: {
    fontSize: 12,
    color: COLORS.gray,
  },
  cardRequester: {
    fontSize: 16,
    fontWeight: '500',
    color: COLORS.dark,
    marginBottom: 4,
  },
  cardDescription: {
    fontSize: 14,
    color: COLORS.gray,
    marginBottom: 12,
  },
  cardActions: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
  },
  actionButton: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 8,
    marginLeft: 8,
  },
  rejectButton: {
    backgroundColor: COLORS.danger,
  },
  rejectButtonText: {
    color: COLORS.white,
    fontWeight: '600',
  },
  approveButton: {
    backgroundColor: COLORS.success,
  },
  approveButtonText: {
    color: COLORS.white,
    fontWeight: '600',
  },
  loadingText: {
    fontSize: 16,
    color: COLORS.gray,
    textAlign: 'center',
    marginTop: 40,
  },
  emptyText: {
    fontSize: 14,
    color: COLORS.gray,
    textAlign: 'center',
    marginTop: 40,
  },
});

export default ApprovalScreen;