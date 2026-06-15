import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TextInput,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { leaveRequestsApi } from '../services/api';
import { COLORS } from '../constants/config';
import { LEAVE_TYPES } from '../types';

import { useNavigation } from '@react-navigation/native';

export const LeaveRequestScreen: React.FC = () => {
  const navigation = useNavigation();
  const { user } = useAuth();
  const [selectedType, setSelectedType] = useState<string>('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (!selectedType) {
      Alert.alert('錯誤', '請選擇請假類型');
      return;
    }

    if (!startDate || !endDate) {
      Alert.alert('錯誤', '請選擇請假日期');
      return;
    }

    if (new Date(startDate) > new Date(endDate)) {
      Alert.alert('錯誤', '結束日期必須晚於開始日期');
      return;
    }

    setLoading(true);
    try {
      await leaveRequestsApi.create({
        employeeId: user?.employeeId,
        leaveType: selectedType,
        startDate,
        endDate,
        reason,
      });

      Alert.alert('成功', '請假申請已送出', [
        { text: '確定', onPress: () => navigation.goBack() },
      ]);
    } catch (error: any) {
      Alert.alert('錯誤', error.message || '提交失敗，請稍後再試');
    } finally {
      setLoading(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.contentContainer}>
      <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
          <Text style={styles.backButton}>← 返回</Text>
        </TouchableOpacity>
        <Text style={styles.headerTitle}>申請請假</Text>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>請假類型</Text>
        <View style={styles.typeGrid}>
          {LEAVE_TYPES.filter(t => t.value !== 'official').map((type) => (
            <TouchableOpacity
              key={type.value}
              style={[
                styles.typeCard,
                selectedType === type.value && styles.typeCardSelected,
              ]}
              onPress={() => setSelectedType(type.value)}
            >
              <Text
                style={[
                  styles.typeLabel,
                  selectedType === type.value && styles.typeLabelSelected,
                ]}
              >
                {type.label}
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>開始日期</Text>
        <TextInput
          style={styles.input}
          value={startDate}
          onChangeText={setStartDate}
          placeholder="YYYY-MM-DD"
          placeholderTextColor={COLORS.gray}
        />
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>結束日期</Text>
        <TextInput
          style={styles.input}
          value={endDate}
          onChangeText={setEndDate}
          placeholder="YYYY-MM-DD"
          placeholderTextColor={COLORS.gray}
        />
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>請假原因 (選填)</Text>
        <TextInput
          style={[styles.input, styles.textArea]}
          value={reason}
          onChangeText={setReason}
          placeholder="請輸入請假原因..."
          placeholderTextColor={COLORS.gray}
          multiline
          numberOfLines={4}
          textAlignVertical="top"
        />
      </View>

      <TouchableOpacity
        style={[styles.submitButton, loading && styles.submitButtonDisabled]}
        onPress={handleSubmit}
        disabled={loading}
      >
        {loading ? (
          <ActivityIndicator color="#FFF" />
        ) : (
          <Text style={styles.submitButtonText}>送出申請</Text>
        )}
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
  header: {
    marginBottom: 20,
  },
  backButton: {
    fontSize: 16,
    color: COLORS.primary,
    marginBottom: 12,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  section: {
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: COLORS.dark,
    marginBottom: 12,
  },
  typeGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  typeCard: {
    backgroundColor: '#FFF',
    borderWidth: 2,
    borderColor: '#E5E7EB',
    borderRadius: 12,
    paddingVertical: 12,
    paddingHorizontal: 16,
    minWidth: '45%',
    alignItems: 'center',
  },
  typeCardSelected: {
    borderColor: COLORS.primary,
    backgroundColor: COLORS.primary + '10',
  },
  typeLabel: {
    fontSize: 14,
    fontWeight: '500',
    color: COLORS.dark,
  },
  typeLabelSelected: {
    color: COLORS.primary,
  },
  input: {
    backgroundColor: '#FFF',
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 16,
    color: COLORS.dark,
  },
  textArea: {
    minHeight: 100,
    paddingTop: 14,
  },
  submitButton: {
    backgroundColor: COLORS.primary,
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    marginTop: 20,
  },
  submitButtonDisabled: {
    opacity: 0.7,
  },
  submitButtonText: {
    color: '#FFF',
    fontSize: 16,
    fontWeight: 'bold',
  },
});