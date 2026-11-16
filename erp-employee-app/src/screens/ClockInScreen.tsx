import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  RefreshControl,
  Alert,
  ActivityIndicator,
  Platform,
} from 'react-native';
import * as Location from 'expo-location';
import { attendanceApi, getApiErrorMessage } from '../services/api';
import { COLORS } from '../constants/config';

interface TodayAttendance {
  id?: string;
  date?: string;
  clockIn?: string;
  clockOut?: string;
  status: string;
}

const showAlert = (title: string, message: string) => {
  if (Platform.OS === 'web') {
    window.alert(`${title}\n${message}`);
    return;
  }

  Alert.alert(title, message);
};

export const ClockInScreen: React.FC = () => {
  const [currentTime, setCurrentTime] = useState(new Date());
  const [todayAttendance, setTodayAttendance] = useState<TodayAttendance | null>(null);
  const [loading, setLoading] = useState(true);
  const [clockingIn, setClockingIn] = useState(false);
  const [location, setLocation] = useState<Location.LocationObject | null>(null);

  useEffect(() => {
    loadTodayAttendance();
    const timer = setInterval(() => setCurrentTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const loadTodayAttendance = async () => {
    try {
      const today = toLocalDateString(new Date());
      const response = await attendanceApi.list({ startDate: today, endDate: today });
      const records = response.data?.data ?? [];
      if (records.length > 0) {
        setTodayAttendance(records[0]);
      } else {
        setTodayAttendance({ status: 'not_clocked_in' });
      }
    } catch (error) {
      console.error('Failed to load attendance:', error);
    } finally {
      setLoading(false);
    }
  };

  const getCurrentLocation = async () => {
    try {
      const { status } = await Location.requestForegroundPermissionsAsync();
      if (status !== 'granted') {
        showAlert('權限不足', '請在 Chrome 網址列左側的網站設定中允許「位置」，再重新整理頁面。');
        return null;
      }
      
      const loc = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.High });
      setLocation(loc);
      return loc;
    } catch (error) {
      console.error('Location error:', error);
      showAlert('定位失敗', '無法取得您的位置，請確認瀏覽器已允許定位，並開啟裝置的位置服務。');
      return null;
    }
  };

  const handleClockIn = async () => {
    setClockingIn(true);
    try {
      const loc = await getCurrentLocation();
      if (!loc) return;

      await attendanceApi.clockIn({
        latitude: loc.coords.latitude,
        longitude: loc.coords.longitude,
      });

      showAlert('打卡成功', '上班打卡已完成');
      await loadTodayAttendance();
    } catch (error: unknown) {
      showAlert('打卡失敗', getApiErrorMessage(error, '請稍後再試'));
    } finally {
      setClockingIn(false);
    }
  };

  const handleClockOut = async () => {
    setClockingIn(true);
    try {
      const loc = await getCurrentLocation();
      if (!loc) return;

      await attendanceApi.clockOut({
        latitude: loc.coords.latitude,
        longitude: loc.coords.longitude,
      });

      showAlert('打卡成功', '下班打卡已完成');
      await loadTodayAttendance();
    } catch (error: unknown) {
      showAlert('打卡失敗', getApiErrorMessage(error, '請稍後再試'));
    } finally {
      setClockingIn(false);
    }
  };

  const getWeekday = (date: Date) => {
    const weekdays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
    return weekdays[date.getDay()];
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  };

  const formatAttendanceTime = (time?: string) => {
    if (!time) return '—';
    return /^\d{2}:\d{2}/.test(time)
      ? time.slice(0, 5)
      : new Date(time).toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit' });
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={COLORS.primary} />
      </View>
    );
  }

  const isClockedIn = todayAttendance?.clockIn && !todayAttendance?.clockOut;
  const isAllDone = todayAttendance?.clockIn && todayAttendance?.clockOut;

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.contentContainer}
      refreshControl={
        <RefreshControl refreshing={loading} onRefresh={loadTodayAttendance} />
      }
    >
      <View style={styles.header}>
        <Text style={styles.headerTitle}>出勤打卡</Text>
        <Text style={styles.headerSubtitle}>Employee App</Text>
      </View>

      <View style={styles.dateCard}>
        <Text style={styles.dateText}>
          {currentTime.getFullYear()}年{currentTime.getMonth() + 1}月{currentTime.getDate()}日
        </Text>
        <Text style={styles.weekdayText}>{getWeekday(currentTime)}</Text>
      </View>

      <View style={styles.clockCard}>
        <Text style={styles.clockTime}>{formatTime(currentTime)}</Text>
        <Text style={styles.clockLabel}>現在時間</Text>

        <View style={styles.buttonContainer}>
          <TouchableOpacity
            style={[
              styles.clockButton,
              isClockedIn && styles.clockButtonDisabled,
              isAllDone && styles.clockButtonDisabled,
            ]}
            onPress={handleClockIn}
            disabled={!!isClockedIn || !!isAllDone || clockingIn}
          >
            {clockingIn ? (
              <ActivityIndicator color="#FFF" />
            ) : (
              <Text style={styles.clockButtonText}>🟢 上班打卡</Text>
            )}
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.clockButton,
              styles.clockOutButton,
              !isClockedIn && styles.clockButtonDisabled,
            ]}
            onPress={handleClockOut}
            disabled={!isClockedIn || clockingIn}
          >
            {clockingIn ? (
              <ActivityIndicator color="#FFF" />
            ) : (
              <Text style={styles.clockButtonText}>🔴 下班打卡</Text>
            )}
          </TouchableOpacity>
        </View>
      </View>

      {todayAttendance && (
        <View style={styles.statusCard}>
          <Text style={styles.statusTitle}>今日出勤狀態</Text>
          
          <View style={styles.statusRow}>
            <Text style={styles.statusLabel}>上班時間</Text>
            <Text style={styles.statusValue}>
              {formatAttendanceTime(todayAttendance.clockIn)}
            </Text>
          </View>
          
          <View style={styles.statusRow}>
            <Text style={styles.statusLabel}>下班時間</Text>
            <Text style={styles.statusValue}>
              {formatAttendanceTime(todayAttendance.clockOut)}
            </Text>
          </View>
          
          <View style={styles.statusRow}>
            <Text style={styles.statusLabel}>狀態</Text>
            <View style={[styles.statusBadge, getStatusBadgeStyle(todayAttendance.status)]}>
              <Text style={styles.statusBadgeText}>{getStatusText(todayAttendance.status)}</Text>
            </View>
          </View>
        </View>
      )}

      <View style={styles.infoCard}>
        <Text style={styles.infoTitle}>📌 打卡說明</Text>
        <Text style={styles.infoText}>• 上班打卡時間：08:00 - 10:00</Text>
        <Text style={styles.infoText}>• 下班打卡時間：17:00 - 22:00</Text>
        <Text style={styles.infoText}>• 請在上班時間內完成打卡</Text>
        <Text style={styles.infoText}>• 如需補卡請洽人資單位</Text>
      </View>
    </ScrollView>
  );
};

const toLocalDateString = (date: Date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getStatusBadgeStyle = (status: string) => {
  switch (status) {
    case 'normal':
      return { backgroundColor: COLORS.success + '20' };
    case 'late':
      return { backgroundColor: COLORS.warning + '20' };
    case 'absent':
      return { backgroundColor: COLORS.danger + '20' };
    default:
      return { backgroundColor: COLORS.gray + '20' };
  }
};

const getStatusText = (status: string) => {
  switch (status) {
    case 'normal':
      return '正常';
    case 'late':
      return '遲到';
    case 'absent':
      return '未出勤';
    default:
      return '未打卡';
  }
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
  dateCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
    alignItems: 'center',
  },
  dateText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: COLORS.dark,
  },
  weekdayText: {
    fontSize: 16,
    color: COLORS.gray,
    marginTop: 4,
  },
  clockCard: {
    backgroundColor: COLORS.primary,
    borderRadius: 20,
    padding: 24,
    marginBottom: 16,
    alignItems: 'center',
  },
  clockTime: {
    fontSize: 48,
    fontWeight: 'bold',
    color: '#FFF',
    fontVariant: ['tabular-nums'],
  },
  clockLabel: {
    fontSize: 14,
    color: 'rgba(255,255,255,0.8)',
    marginTop: 4,
    marginBottom: 20,
  },
  buttonContainer: {
    width: '100%',
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 16,
  },
  clockButton: {
    backgroundColor: 'rgba(255,255,255,0.2)',
    borderWidth: 2,
    borderColor: '#FFF',
    borderRadius: 30,
    paddingVertical: 14,
    paddingHorizontal: 24,
    minWidth: 130,
    alignItems: 'center',
  },
  clockOutButton: {
    backgroundColor: 'rgba(255,255,255,0.1)',
  },
  clockButtonDisabled: {
    opacity: 0.5,
  },
  clockButtonText: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#FFF',
  },
  statusCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
  },
  statusTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
    marginBottom: 16,
  },
  statusRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  statusLabel: {
    fontSize: 14,
    color: COLORS.gray,
  },
  statusValue: {
    fontSize: 14,
    fontWeight: '600',
    color: COLORS.dark,
  },
  statusBadge: {
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
  },
  statusBadgeText: {
    fontSize: 12,
    fontWeight: '600',
    color: COLORS.dark,
  },
  infoCard: {
    backgroundColor: '#FFF',
    borderRadius: 16,
    padding: 20,
    marginBottom: 16,
  },
  infoTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: COLORS.dark,
    marginBottom: 12,
  },
  infoText: {
    fontSize: 14,
    color: COLORS.gray,
    marginBottom: 8,
  },
});