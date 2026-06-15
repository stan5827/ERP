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
import { announcementApi } from '../services/api';
import { COLORS } from '../constants/config';
import { Announcement } from '../types';

export const AnnouncementScreen: React.FC = () => {
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');

  const categories = [
    { value: 'all', label: '全部' },
    { value: 'general', label: '一般' },
    { value: 'policy', label: '政策' },
    { value: 'hr', label: '人事' },
    { value: 'training', label: '訓練' },
    { value: 'it', label: '資訊' },
    { value: 'urgent', label: '緊急' },
  ];

  useEffect(() => {
    loadAnnouncements();
  }, []);

  const loadAnnouncements = async () => {
    try {
      const response = await announcementApi.list({ pageSize: '50' });
      setAnnouncements(response.data?.data || []);
    } catch (error) {
      console.error('Failed to load announcements:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleRefresh = () => {
    setRefreshing(true);
    loadAnnouncements();
  };

  const handleMarkRead = async (id: string) => {
    try {
      await announcementApi.markRead(id);
      setAnnouncements(prev =>
        prev.map(a => (a.id === id ? { ...a, read: true } : a))
      );
    } catch (error) {
      console.error('Failed to mark as read:', error);
    }
  };

  const filteredAnnouncements = announcements.filter(a =>
    selectedCategory === 'all' ? true : a.category === selectedCategory
  );

  const getCategoryLabel = (category: string) => {
    const found = categories.find(c => c.value === category);
    return found?.label || category;
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'urgent': return COLORS.danger;
      case 'policy': return COLORS.primary;
      case 'hr': return COLORS.success;
      case 'training': return COLORS.warning;
      case 'it': return '#8B5CF6';
      default: return COLORS.gray;
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
        <Text style={styles.headerTitle}>公佈欄</Text>
        <Text style={styles.headerSubtitle}>Announcements</Text>
      </View>

      <View style={styles.filterContainer}>
        <FlatList
          horizontal
          data={categories}
          keyExtractor={(item) => item.value}
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.filterList}
          renderItem={({ item }) => (
            <TouchableOpacity
              style={[
                styles.filterChip,
                selectedCategory === item.value && styles.filterChipActive,
              ]}
              onPress={() => setSelectedCategory(item.value)}
            >
              <Text
                style={[
                  styles.filterChipText,
                  selectedCategory === item.value && styles.filterChipTextActive,
                ]}
              >
                {item.label}
              </Text>
            </TouchableOpacity>
          )}
        />
      </View>

      <FlatList
        data={filteredAnnouncements}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
        }
        renderItem={({ item }) => (
          <TouchableOpacity
            style={[styles.announcementCard, !item.read && styles.unreadCard]}
            onPress={() => {
              if (!item.read) {
                handleMarkRead(item.id);
              }
            }}
          >
            <View style={styles.cardHeader}>
              <View style={styles.cardHeaderLeft}>
                {item.isPinned && <Text style={styles.pinnedIcon}>📌</Text>}
                {item.isUrgent && (
                  <View style={styles.urgentBadge}>
                    <Text style={styles.urgentText}>緊急</Text>
                  </View>
                )}
              </View>
              <View
                style={[
                  styles.categoryBadge,
                  { backgroundColor: getCategoryColor(item.category) + '20' },
                ]}
              >
                <Text
                  style={[
                    styles.categoryText,
                    { color: getCategoryColor(item.category) },
                  ]}
                >
                  {getCategoryLabel(item.category)}
                </Text>
              </View>
            </View>

            <Text style={styles.announcementTitle}>{item.title}</Text>
            
            {item.content && (
              <Text style={styles.announcementContent} numberOfLines={2}>
                {item.content}
              </Text>
            )}

            <View style={styles.cardFooter}>
              <Text style={styles.publishDate}>
                發布日期：{item.publishDate?.split('T')[0]}
              </Text>
              {!item.read && <View style={styles.unreadDot} />}
            </View>
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>尚無公告</Text>
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
  filterContainer: {
    backgroundColor: '#FFF',
    paddingBottom: 12,
  },
  filterList: {
    paddingHorizontal: 16,
    gap: 8,
  },
  filterChip: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
    backgroundColor: '#F3F4F6',
    marginRight: 8,
  },
  filterChipActive: {
    backgroundColor: COLORS.primary,
  },
  filterChipText: {
    fontSize: 14,
    color: COLORS.gray,
  },
  filterChipTextActive: {
    color: '#FFF',
    fontWeight: '600',
  },
  listContent: {
    padding: 16,
  },
  announcementCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 16,
    marginBottom: 12,
  },
  unreadCard: {
    borderLeftWidth: 4,
    borderLeftColor: COLORS.primary,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  cardHeaderLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  pinnedIcon: {
    fontSize: 14,
  },
  urgentBadge: {
    backgroundColor: COLORS.danger + '20',
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 8,
  },
  urgentText: {
    fontSize: 10,
    fontWeight: '600',
    color: COLORS.danger,
  },
  categoryBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  categoryText: {
    fontSize: 12,
    fontWeight: '600',
  },
  announcementTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
    marginBottom: 8,
  },
  announcementContent: {
    fontSize: 14,
    color: COLORS.gray,
    marginBottom: 12,
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  publishDate: {
    fontSize: 12,
    color: COLORS.gray,
  },
  unreadDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    backgroundColor: COLORS.primary,
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