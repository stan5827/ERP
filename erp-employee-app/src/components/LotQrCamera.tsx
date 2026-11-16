import React, { useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import { COLORS } from '../constants/config';

interface LotQrCameraProps {
  onScan: (value: string) => void;
}

export const LotQrCamera: React.FC<LotQrCameraProps> = ({ onScan }) => {
  const [permission, requestPermission] = useCameraPermissions();
  const [active, setActive] = useState(false);

  if (!permission) {
    return <ActivityIndicator color={COLORS.primary} />;
  }

  if (!permission.granted) {
    return (
      <TouchableOpacity style={styles.button} onPress={() => void requestPermission()}>
        <Text style={styles.buttonText}>允許相機權限以掃描 QR</Text>
      </TouchableOpacity>
    );
  }

  if (!active) {
    return (
      <TouchableOpacity style={styles.button} onPress={() => setActive(true)}>
        <Text style={styles.buttonText}>開啟相機掃描 QR</Text>
      </TouchableOpacity>
    );
  }

  return (
    <View style={styles.container}>
      <CameraView
        style={styles.camera}
        facing="back"
        barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
        onBarcodeScanned={({ data }) => {
          setActive(false);
          onScan(data);
        }}
      />
      <TouchableOpacity style={styles.button} onPress={() => setActive(false)}>
        <Text style={styles.buttonText}>停止相機</Text>
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: { gap: 8 },
  camera: { height: 260, width: '100%', overflow: 'hidden', borderRadius: 8 },
  button: { minHeight: 44, justifyContent: 'center', alignItems: 'center', paddingHorizontal: 12, borderRadius: 8, backgroundColor: COLORS.primary },
  buttonText: { color: COLORS.white, fontWeight: '700' },
});
