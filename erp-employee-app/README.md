# ERP Employee App

## Environment Setup

Copy `.env.example` to `.env` and configure:

```bash
cp .env.example .env
```

### Configuration Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `EXPO_PUBLIC_API_URL` | API server URL | `http://10.0.2.2:5100/api` |

### Android Emulator
Use `http://10.0.2.2:5100/api` to connect to localhost

### iOS Simulator  
Use `http://localhost:5100/api`

### Real Device
Use your computer's IP address, e.g., `http://192.168.1.100:5100/api`

## Features

- [x] Employee Attendance (打卡)
- [x] Leave Requests (請假)
- [x] Leave Balance Display (請假餘額)
- [x] Announcements (公告)
- [x] Payroll View (薪資查詢)
- [x] Profile (個人資料)
- [x] Approval Workflow (審核) - For supervisors
- [x] Push Notifications (推播)
- [x] Offline Mode (離線模式)
- [x] Encrypted Token Storage (加密儲存)

## Install

```bash
npm install
```

## Run

```bash
npm start
# or
expo start
```

## Build APK

```bash
npx expo prebuild
cd android
./gradlew assembleRelease
```