import React, { useState, useEffect } from 'react';
import { Plus, Download, Search, Edit2, Eye, Check, X, Calendar, Clock, DollarSign } from 'lucide-react';
import { useEmployees } from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import type { FormTab, FormField } from '../components/FormModal';
import { employeesApi, attendanceApi, leaveRequestsApi, payrollApi, departmentsApi, jobTitlesApi, jobGradesApi } from '../api/api';
import type { AttendanceRecordDto, LeaveRequestDto, PayrollDto } from '../api/api';

type TabType = 'employees' | 'attendance' | 'leave' | 'payroll';

/* ── 統計卡片（動態計算）── */
function getHRStats(employees: any[], attendanceRecords: any[], leaveRequests: any[]) {
  const today = new Date().toISOString().slice(0, 10);
  const activeEmployees = employees.filter((e: any) => e.status === 'active').length;
  const todayAttendance = attendanceRecords.filter((a: any) => a.date?.slice(0, 10) === today).length;
  const pendingLeave = leaveRequests.filter((l: any) => l.status === 'pending').length;
  const abnormal = attendanceRecords.filter((a: any) => a.status === 'abnormal' || a.lateMinutes > 0).length;
  return [
    { label: '在職員工', value: activeEmployees, color: 'var(--cobalt-400)' },
    { label: '今日出勤', value: todayAttendance, color: 'var(--emerald-400)' },
    { label: '待審請假', value: pendingLeave, color: 'var(--amber-400)' },
    { label: '出勤異常', value: abnormal, color: 'var(--crimson-400)' },
  ];
}

const statusConfig: Record<string, { label: string; cls: string }> = {
  active: { label: '在職', cls: 'active' },
  leave: { label: '留職停薪', cls: 'warning' },
  inactive: { label: '離職', cls: 'neutral' },
};

const attendanceStatusConfig: Record<string, { label: string; cls: string }> = {
  normal: { label: '正常', cls: 'active' },
  late: { label: '遲到', cls: 'warning' },
  absent: { label: '缺勤', cls: 'danger' },
  early_leave: { label: '早退', cls: 'warning' },
  pending: { label: '待審', cls: 'neutral' },
};

const leaveStatusConfig: Record<string, { label: string; cls: string }> = {
  pending: { label: '待審', cls: 'neutral' },
  approved: { label: '核准', cls: 'active' },
  rejected: { label: '退回', cls: 'danger' },
  cancelled: { label: '取消', cls: 'warning' },
};

const payrollStatusConfig: Record<string, { label: string; cls: string }> = {
  draft: { label: '草稿', cls: 'neutral' },
  approved: { label: '已核准', cls: 'active' },
  paid: { label: '已發放', cls: 'active' },
  void: { label: '作廢', cls: 'danger' },
};

const leaveTypes = [
  { value: 'annual', label: '特休/年假' },
  { value: 'personal', label: '事假' },
  { value: 'sick', label: '病假' },
  { value: 'maternity', label: '產假' },
  { value: 'marriage', label: '婚假' },
  { value: 'funeral', label: '喪假' },
  { value: 'official', label: '公假' },
];

function buildEmployeeTabs(
  deptOptions: { value: string; label: string }[],
  titleOptions: { value: string; label: string }[],
  gradeOptions: { value: string; label: string }[]
): FormTab[] {
  return [
    {
      label: '身分資料',
      fields: [
        { name: 'chineseName', label: '中文姓名', type: 'text', required: true, placeholder: '請輸入中文姓名', description: '員工的中文姓名，必須與身分證件相符，用於人事資料與薪資發放。' },
        { name: 'englishName', label: '英文姓名', type: 'text', placeholder: '如：John Wang', description: '護照上的英文姓名，用於國際出差、薪資單英文版等情境。' },
        { name: 'nationalId', label: '身分證字號', type: 'text', placeholder: 'A123456789', description: '中華民國國民的身分證字號，用於勞健保投保與所得稅申報。' },
        { name: 'passportNo', label: '護照號碼', type: 'text', placeholder: '外籍員工必填', description: '外籍或港澳人士的護照號碼，用於居留證申請與勞動部備查。' },
        { name: 'birthDate', label: '出生日期', type: 'date', description: '員工的出生年月日，用於年資計算與勞基法適用判定。' },
        { name: 'gender', label: '性別', type: 'select', options: [{ value: 'M', label: '男' }, { value: 'F', label: '女' }], description: '性別資料，用於勞保、健保與統計分析。' },
        { name: 'mobile', label: '手機', type: 'text', placeholder: '09xx-xxxxxx', description: '主要聯絡手機號碼，用於緊急聯繫與系統通知(如刷卡、請假通知)。' },
        { name: 'phone', label: '市話', type: 'text', placeholder: '02-xxxxxxxx', description: '住家或辦公室電話，緊急聯繫備用。' },
        { name: 'email', label: 'Email', type: 'text', placeholder: 'name@company.com', description: '公司或個人電子郵件，用於系統通知、薪資單寄送、會議邀請。' },
{ name: 'registeredAddress', label: '戶籍地址', type: 'text', placeholder: '同身分證登記地址', description: '戶籍登記地址，用於勞健保寄送與年度扣繳憑單寄發。' },
{ name: 'mailingAddress', label: '通訊地址', type: 'text', sameAs: 'registeredAddress', placeholder: '可收信地址', description: '實際收件地址，若與戶籍不同時填寫，優先用於信件寄送。' },
{ name: 'status', label: '狀態', type: 'select', options: [
  { value: 'active', label: '在職' },
  { value: 'leave', label: '留職停薪' },
  { value: 'inactive', label: '離職' }
], defaultValue: 'active', description: '員工目前的在職狀態。' },
],
    },
    {
      label: '緊急聯絡',
      fields: [
        { name: 'emergencyContactName', label: '緊急聯絡人', type: 'text', required: true, placeholder: '聯絡人姓名', description: '發生緊急事件時可聯繫的家屬或親友姓名。' },
        { name: 'emergencyContactRelation', label: '關係', type: 'select', options: [
          { value: '配偶', label: '配偶' }, { value: '父母', label: '父母' }, { value: '子女', label: '子女' },
          { value: '兄弟姊妹', label: '兄弟姊妹' }, { value: '朋友', label: '朋友' }, { value: '其他', label: '其他' },
        ], description: '與緊急聯絡人的關係，供緊急事件聯繫時參考。' },
        { name: 'emergencyContactPhone', label: '聯絡電話', type: 'text', required: true, placeholder: '09xx-xxxxxx', description: '緊急聯絡人的聯繫電話，請填寫可即時聯繫的號碼。' },
      ],
    },
    {
      label: '組織職位',
      fields: [
        { name: 'departmentId', label: '所屬部門', type: 'select', required: true, options: deptOptions, description: '員工所屬的行政部門，決定組織架構歸屬與成本中心歸屬。' },
        { name: 'jobTitleId', label: '職稱', type: 'select', required: true, options: titleOptions, description: '正式任用後的職稱，如工程師、課長、經理等，影響薪資與考核標準。' },
        { name: 'gradeId', label: '職等', type: 'select', required: true, options: gradeOptions, description: '薪資職等(如P1-P6或 Grade A-F)，決定薪資範圍與晉升路徑。' },
        { name: 'jobCategory', label: '職務類別', type: 'select', options: [
          { value: 'production', label: '生產部門' }, { value: 'admin', label: '管理部門' }, { value: 'service', label: '服務部門' },
        ], description: '部門的業務屬性，用於HR統計分析與相關法規適用。' },
        { name: 'hireDate', label: '到職日', type: 'date', required: true, description: '正式到公司報到的日期，作為年資計算、試用期起算與勞健保生效日依據。' },
        { name: 'probationEndDate', label: '試用期截止日', type: 'date', description: '試用期期滿日期，試用期一般為3-6個月，屆滿前需完成考核以決定是否正式任用。' },
      ],
    },
    {
      label: '薪資金融',
      fields: [
        { name: 'salaryType', label: '薪資計費方式', type: 'select', options: [
          { value: 'monthly', label: '月薪' }, { value: 'daily', label: '日薪' },
          { value: 'hourly', label: '時薪' }, { value: 'piece', label: '件計' },
        ], description: '薪資計算方式：月薪=固定月薪、日薪=按上班天數計算、時薪=按工時計算、件計=按產量計酬。' },
        { name: 'baseSalary', label: '底薪（本薪）', type: 'number', required: true, placeholder: '29500', min: 0, description: '基本工資，不含津貼與獎金。作為勞健保投保金額、資遣費、職業災害計算基準。' },
        { name: 'foodAllowance', label: '伙食津貼', type: 'number', placeholder: '0', min: 0, description: '每月固定伙食補助，如誤餐費或餐券補貼。' },
        { name: 'positionAllowance', label: '職務加給', type: 'number', placeholder: '0', min: 0, description: '因擔任特定職務而給予的加給，如主管加給、夜班津貼。' },
        { name: 'fullAttendanceBonus', label: '全勤獎金', type: 'number', placeholder: '0', min: 0, description: '全勤時發放的獎金金額，需符合公司全勤定義(如不遲到、未請假)。' },
        { name: 'bankCode', label: '銀行代碼', type: 'text', placeholder: '如 008', description: '發薪銀行的代碼(3碼)，如005=農業金庫、008=台灣銀行、012=台北富邦。' },
        { name: 'bankBranch', label: '銀行分行', type: 'text', placeholder: '如 台北分行', description: '銀行分行名稱，用於匯款作業確認。' },
        { name: 'bankAccount', label: '銀行帳號', type: 'text', placeholder: '帳號', description: '員工的薪資入帳帳號，請確認帳號正確避免匯款失敗。' },
      ],
    },
    {
      label: '法定保障',
      fields: [
        { name: 'laborInsuranceGrade', label: '勞保投保等級', type: 'number', placeholder: '0', min: 0, description: '勞工保險投保等級(1-20級)，決定每月勞保費金額。依月薪對照勞保局公告表投保。' },
        { name: 'healthInsuranceGrade', label: '健保投保等級', type: 'number', placeholder: '0', min: 0, description: '全民健康保險投保金額等級，決定每月健保費金額。' },
        { name: 'pensionContributionRate', label: '勞退提繳率（%）', type: 'number', placeholder: '6', min: 0, max: 100, step: 0.5, description: '勞工退休金提繳率，雇主最低須提繳6%，最高可提繳15%。' },
        { name: 'taxDependents', label: '所得稅扶養人數', type: 'number', placeholder: '0', min: 0, description: '年度所得稅申報時的免稅額人數，包括本人與符合條件的眷屬。' },
        { name: 'disabilityCategory', label: '身心障礙類別', type: 'text', placeholder: '無則免填', description: '若有身心障礙手冊請填寫，可享有相關稅務優惠與勞保優惠。' },
      ],
    },
    {
      label: '出勤學經歷',
      fields: [
        { name: 'shiftType', label: '班表類別', type: 'select', options: [
          { value: 'fixed', label: '固定班' }, { value: 'day', label: '常日班' },
          { value: 'rotating', label: '輪班制' },
        ], description: '員工的班別類型，決定刷卡時間與加班費計算方式。' },
        { name: 'overtimePermission', label: '加班權限', type: 'checkbox', placeholder: '開放線上申請加班', description: '勾選後員工可透過系統申請加班，未勾選則無法申請(適用責任制或管理職)。' },
        { name: 'cardNumber', label: '刷卡卡號', type: 'text', placeholder: '對應刷卡機 ID', description: '員工專屬的刷卡卡片號碼，用於考勤系統辨識出勤紀錄。' },
        { name: 'biometricId', label: '生物辨識 ID', type: 'text', placeholder: '人臉/指紋辨識 ID', description: '人臉辨識或指紋辨識系統的員工ID，用於智慧考勤。' },
        { name: 'highestEducation', label: '最高學歷', type: 'text', placeholder: '如：國立台灣大學 資訊工程系', description: '最高學歷的學校名稱與科系，供人事資料與教育訓練規劃參考。' },
        { name: 'major', label: '科系', type: 'text', placeholder: '選填', description: '大學或研究所的科系名稱，與工作性質相關時作為任用參考。' },
        { name: 'graduationYear', label: '畢業年度', description: '最高學歷的畢業年份，用於年資與經歷核對。', type: 'number', placeholder: '2020', min: 1950, max: 2030 },
        { name: 'foreignLanguages', label: '外語能力', type: 'text', placeholder: '如：英文流利、日文 N2', description: '語言能力檢定或自評程度，如多益800分、日文N1等。' },
        { name: 'skills', label: '專業技能', type: 'textarea', placeholder: '專業技能、證照、經歷...', description: '與工作相關的專業技能、專業證照、或過往工作經驗摘要。' },
      ],
    },
  ];
}

function buildEmployeeWizardFields(
  deptOptions: { value: string; label: string }[],
  titleOptions: { value: string; label: string }[],
  gradeOptions: { value: string; label: string }[]
): FormField[][] {
  return [
    [
      { name: 'chineseName', label: '中文姓名', type: 'text', required: true, placeholder: '請輸入中文姓名', description: '員工的中文姓名，必須與身分證件相符，用於人事資料與薪資發放。' },
      { name: 'englishName', label: '英文姓名', type: 'text', placeholder: '如：John Wang', description: '護照上的英文姓名，用於國際出差、薪資單英文版等情境。' },
      { name: 'nationalId', label: '身分證字號', type: 'text', placeholder: 'A123456789', description: '中華民國國民的身分證字號，用於勞健保投保與所得稅申報。' },
      { name: 'passportNo', label: '護照號碼', type: 'text', placeholder: '外籍員工必填', description: '外籍或港澳人士的護照號碼，用於居留證申請與勞動部備查。' },
      { name: 'birthDate', label: '出生日期', type: 'date', description: '員工的出生年月日，用於年資計算與勞基法適用判定。' },
      { name: 'gender', label: '性別', type: 'select', options: [{ value: 'M', label: '男' }, { value: 'F', label: '女' }], description: '性別資料，用於勞保、健保與統計分析。' },
      { name: 'mobile', label: '手機', type: 'text', placeholder: '09xx-xxxxxx', description: '主要聯絡手機號碼，用於緊急聯繫與系統通知(如刷卡、請假通知)。' },
      { name: 'phone', label: '市話', type: 'text', placeholder: '02-xxxxxxxx', description: '住家或辦公室電話，緊急聯繫備用。' },
      { name: 'email', label: 'Email', type: 'text', placeholder: 'name@company.com', description: '公司或個人電子郵件，用於系統通知、薪資單寄送、會議邀請。' },
      { name: 'registeredAddress', label: '戶籍地址', type: 'text', placeholder: '同身分證登記地址', description: '戶籍登記地址，用於勞健保寄送與年度扣繳憑單寄發。' },
{ name: 'mailingAddress', label: '通訊地址', type: 'text', sameAs: 'registeredAddress', placeholder: '可收信地址', description: '實際收件地址，若與戶籍不同時填寫，優先用於信件寄送。' },
{ name: 'status', label: '狀態', type: 'select', required: true, options: [
  { value: 'active', label: '在職' },
  { value: 'leave', label: '留職停薪' },
  { value: 'inactive', label: '離職' }
], defaultValue: 'active', description: '員工目前的在職狀態。' },
    ],
    [
      { name: 'emergencyContactName', label: '緊急聯絡人', type: 'text', required: true, placeholder: '聯絡人姓名', description: '發生緊急事件時可聯繫的家屬或親友姓名。' },
      { name: 'emergencyContactRelation', label: '關係', type: 'select', options: [
        { value: '配偶', label: '配偶' }, { value: '父母', label: '父母' }, { value: '子女', label: '子女' },
        { value: '兄弟姊妹', label: '兄弟姊妹' }, { value: '朋友', label: '朋友' }, { value: '其他', label: '其他' },
      ], description: '與緊急聯絡人的關係，供緊急事件聯繫時參考。' },
      { name: 'emergencyContactPhone', label: '聯絡電話', type: 'text', required: true, placeholder: '09xx-xxxxxx', description: '緊急聯絡人的聯繫電話，請填寫可即時聯繫的號碼。' },
    ],
    [
      { name: 'departmentId', label: '所屬部門', type: 'select', required: true, options: deptOptions, description: '員工所屬的行政部門，決定組織架構歸屬與成本中心歸屬。' },
      { name: 'jobTitleId', label: '職稱', type: 'select', required: true, options: titleOptions, description: '正式任用後的職稱，如工程師、課長、經理等，影響薪資與考核標準。' },
      { name: 'gradeId', label: '職等', type: 'select', required: true, options: gradeOptions, description: '薪資職等(如P1-P6或 Grade A-F)，決定薪資範圍與晉升路徑。' },
      { name: 'jobCategory', label: '職務類別', type: 'select', options: [
        { value: 'production', label: '生產部門' }, { value: 'admin', label: '管理部門' }, { value: 'service', label: '服務部門' },
      ], description: '部門的業務屬性，用於HR統計分析與相關法規適用。' },
      { name: 'hireDate', label: '到職日', type: 'date', required: true, description: '正式到公司報到的日期，作為年資計算、試用期起算與勞健保生效日依據。' },
      { name: 'probationEndDate', label: '試用期截止日', type: 'date', description: '試用期期滿日期，試用期一般為3-6個月，屆滿前需完成考核以決定是否正式任用。' },
    ],
    [
      { name: 'salaryType', label: '薪資計費方式', type: 'select', options: [
        { value: 'monthly', label: '月薪' }, { value: 'daily', label: '日薪' },
        { value: 'hourly', label: '時薪' }, { value: 'piece', label: '件計' },
      ], description: '薪資計算方式：月薪=固定月薪、日薪=按上班天數計算、時薪=按工時計算、件計=按產量計酬。' },
      { name: 'baseSalary', label: '底薪（本薪）', type: 'number', required: true, placeholder: '29500', min: 0, description: '基本工資，不含津貼與獎金。作為勞健保投保金額、資遣費、職業災害計算基準。' },
      { name: 'foodAllowance', label: '伙食津貼', type: 'number', placeholder: '0', min: 0, description: '每月固定伙食補助，如誤餐費或餐券補貼。' },
      { name: 'positionAllowance', label: '職務加給', type: 'number', placeholder: '0', min: 0, description: '因擔任特定職務而給予的加給，如主管加給、夜班津貼。' },
      { name: 'fullAttendanceBonus', label: '全勤獎金', type: 'number', placeholder: '0', min: 0, description: '全勤時發放的獎金金額，需符合公司全勤定義(如不遲到、未請假)。' },
      { name: 'bankCode', label: '銀行代碼', type: 'text', placeholder: '如 008', description: '發薪銀行的代碼(3碼)，如005=農業金庫、008=台灣銀行、012=台北富邦。' },
      { name: 'bankBranch', label: '銀行分行', type: 'text', placeholder: '如 台北分行', description: '銀行分行名稱，用於匯款作業確認。' },
      { name: 'bankAccount', label: '銀行帳號', type: 'text', placeholder: '帳號', description: '員工的薪資入帳帳號，請確認帳號正確避免匯款失敗。' },
    ],
    [
      { name: 'laborInsuranceGrade', label: '勞保投保等級', type: 'number', placeholder: '0', min: 0, description: '勞工保險投保等級(1-20級)，決定每月勞保費金額。依月薪對照勞保局公告表投保。' },
      { name: 'healthInsuranceGrade', label: '健保投保等級', type: 'number', placeholder: '0', min: 0, description: '全民健康保險投保金額等級，決定每月健保費金額。' },
      { name: 'pensionContributionRate', label: '勞退提繳率（%）', type: 'number', placeholder: '6', min: 0, max: 100, step: 0.5, description: '勞工退休金提繳率，雇主最低須提繳6%，最高可提繳15%。' },
      { name: 'taxDependents', label: '所得稅扶持人數', type: 'number', placeholder: '0', min: 0, description: '年度所得稅申報時的免稅額人數，包括本人與符合條件的眷屬。' },
      { name: 'disabilityCategory', label: '身心障礙類別', type: 'text', placeholder: '無則免填', description: '若有身心障礙手冊請填寫，可享有相關稅務優惠與勞保優惠。' },
    ],
    [
      { name: 'shiftType', label: '班表類別', type: 'select', options: [
        { value: 'fixed', label: '固定班' }, { value: 'day', label: '常日班' },
        { value: 'rotating', label: '輪班制' },
      ], description: '員工的班別類型，決定刷卡時間與加班費計算方式。' },
      { name: 'overtimePermission', label: '加班權限', type: 'checkbox', placeholder: '開放線上申請加班', description: '勾選後員工可透過系統申請加班，未勾選則無法申請(適用責任制或管理職)。' },
      { name: 'cardNumber', label: '刷卡卡號', type: 'text', placeholder: '對應刷卡機 ID', description: '員工專屬的刷卡卡片號碼，用於考勤系統辨識出勤紀錄。' },
      { name: 'biometricId', label: '生物辨識 ID', type: 'text', placeholder: '人臉/指紋辨識 ID', description: '人臉辨識或指紋辨識系統的員工ID，用於智慧考勤。' },
      { name: 'highestEducation', label: '最高學歷', type: 'text', placeholder: '如：國立台灣大學 資訊工程系', description: '最高學歷的學校名稱與科系，供人事資料與教育訓練規劃參考。' },
      { name: 'major', label: '科系', type: 'text', placeholder: '選填', description: '大學或研究所的科系名稱，與工作性質相關時作為任用參考。' },
      { name: 'graduationYear', label: '畢業年度', description: '最高學歷的畢業年份，用於年資與經歷核對。', type: 'number', placeholder: '2020', min: 1950, max: 2030 },
      { name: 'foreignLanguages', label: '外語能力', type: 'text', placeholder: '如：英文流利、日文 N2', description: '語言能力檢定或自評程度，如多益800分、日文N1等。' },
      { name: 'skills', label: '專業技能', type: 'textarea', placeholder: '專業技能、證照、經歷...', description: '與工作相關的專業技能、專業證照、或過往工作經驗摘要。' },
    ],
  ];
}

export const HR: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>('employees');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [showAdd, setShowAdd] = useState(false);
  const [editingEmployee, setEditingEmployee] = useState<any>(null);
  const [viewingEmployee, setViewingEmployee] = useState<any>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const [deptOptions, setDeptOptions] = useState<{ value: string; label: string }[]>([]);
  const [titleOptions, setTitleOptions] = useState<{ value: string; label: string }[]>([]);
  const [gradeOptions, setGradeOptions] = useState<{ value: string; label: string }[]>([]);
  const [terminatingEmployee, setTerminatingEmployee] = useState<any>(null);
  const [terminateDate, setTerminateDate] = useState('');
  const [terminateReason, setTerminateReason] = useState('');
  const [showAddLeave, setShowAddLeave] = useState(false);
  const [showAddAttendance, setShowAddAttendance] = useState(false);
  const [showAddPayroll, setShowAddPayroll] = useState(false);

  // Attendance state
  const [attendanceRecords, setAttendanceRecords] = useState<AttendanceRecordDto[]>([]);
  const [attendanceLoading, setAttendanceLoading] = useState(false);

  // Leave state
  const [leaveRequests, setLeaveRequests] = useState<LeaveRequestDto[]>([]);
  const [leaveLoading, setLeaveLoading] = useState(false);

  // Payroll state
  const [payrolls, setPayrolls] = useState<PayrollDto[]>([]);
  const [payrollLoading, setPayrollLoading] = useState(false);

  const { data: employeesResponse, isLoading } = useEmployees({
    ...(search && { search }),
    ...(statusFilter && { status: statusFilter }),
  }, refreshKey);

  const employees = employeesResponse?.data ?? [];

  // Load attendance records
  useEffect(() => {
    if (activeTab === 'attendance') {
      setAttendanceLoading(true);
      attendanceApi.list({ pageSize: '50' })
        .then(res => setAttendanceRecords(res.data?.data ?? []))
        .catch(console.error)
        .finally(() => setAttendanceLoading(false));
    }
  }, [activeTab, refreshKey]);

  // Load leave requests
  useEffect(() => {
    if (activeTab === 'leave') {
      setLeaveLoading(true);
      leaveRequestsApi.list({ pageSize: '50' })
        .then(res => setLeaveRequests(res.data?.data ?? []))
        .catch(console.error)
        .finally(() => setLeaveLoading(false));
    }
  }, [activeTab, refreshKey]);

  // Load payroll records
  useEffect(() => {
    if (activeTab === 'payroll') {
      setPayrollLoading(true);
      payrollApi.list({ pageSize: '50' })
        .then(res => setPayrolls(res.data?.data ?? []))
        .catch(console.error)
        .finally(() => setPayrollLoading(false));
    }
  }, [activeTab, refreshKey]);

  // Load departments, job titles, and job grades when modal opens
  useEffect(() => {
    if (showAdd || editingEmployee) {
      Promise.all([
        departmentsApi.flat(),
        jobTitlesApi.list(),
        jobGradesApi.list(),
      ]).then(([deptRes, titleRes, gradeRes]) => {
        if (deptRes.data?.data) {
          setDeptOptions(deptRes.data.data.map((d: { id: string; deptCode: string; deptName: string }) => ({
            value: d.id,
            label: `${d.deptCode} - ${d.deptName}`,
          })));
        }
        if (titleRes.data?.data) {
          setTitleOptions(titleRes.data.data.map((t: { id: string; titleCode: string; titleName: string }) => ({
            value: t.id,
            label: `${t.titleCode} - ${t.titleName}`,
          })));
        }
        if (gradeRes.data?.data) {
          setGradeOptions(gradeRes.data.data.map((g: { id: string; gradeCode: string; gradeName: string; level: number }) => ({
            value: g.id,
            label: `${g.gradeCode} - ${g.gradeName}`,
          })));
        }
      }).catch(err => console.error('Failed to load org data:', err));
    }
  }, [showAdd, editingEmployee]);

  const employeeTabs = buildEmployeeTabs(deptOptions, titleOptions, gradeOptions);
  const employeeWizardFields = buildEmployeeWizardFields(deptOptions, titleOptions, gradeOptions);

  const tabs = [
    { key: 'employees' as TabType, label: '員工資料', icon: <Calendar size={14} /> },
    { key: 'attendance' as TabType, label: '出勤管理', icon: <Clock size={14} /> },
    { key: 'leave' as TabType, label: '請假審批', icon: <Calendar size={14} /> },
    { key: 'payroll' as TabType, label: '薪資核算', icon: <DollarSign size={14} /> },
  ];

  const handleApproveLeave = async (id: string) => {
    try {
      await leaveRequestsApi.approve(id, 'approved');
      setRefreshKey(k => k + 1);
    } catch (err) {
      console.error('Failed to approve:', err);
    }
  };

  const handleRejectLeave = async (id: string) => {
    try {
      await leaveRequestsApi.approve(id, 'rejected', undefined, '退回');
      setRefreshKey(k => k + 1);
    } catch (err) {
      console.error('Failed to reject:', err);
    }
  };

  const handleApproveAttendance = async (id: string) => {
    try {
      await attendanceApi.approve(id, 'approved');
      setRefreshKey(k => k + 1);
    } catch (err) {
      console.error('Failed to approve:', err);
    }
  };

  const handleApprovePayroll = async (id: string) => {
    try {
      await payrollApi.updateStatus(id, 'approved');
      setRefreshKey(k => k + 1);
    } catch (err) {
      console.error('Failed to approve:', err);
    }
  };

  const renderTabContent = () => {
    switch (activeTab) {
      case 'employees':
        return (
          <>
            {/* 搜尋與篩選 */}
            <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
              <div style={{ position: 'relative', flex: 1 }}>
                <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
                <input type="text" placeholder="搜尋員工編號、姓名、部門..." value={search}
                  onChange={e => setSearch(e.target.value)}
                  style={{ width: '100%', padding: '8px 12px 8px 32px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', fontFamily: 'inherit' }} />
              </div>
              <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
                style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }}>
                <option value="">全部狀態</option>
                <option value="active">在職</option>
                <option value="leave">留職停薪</option>
                <option value="inactive">離職</option>
              </select>
              <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={13} />新增員工</button>
            </div>

            {/* 員工列表 */}
            <div className="glass-card" style={{ padding: '18px' }}>
              <div style={{ marginBottom: 14 }}>
                <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>員工名冊</h3>
                <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {employees.length} 筆員工資料</div>
              </div>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>員工編號</th><th>姓名</th><th>部門</th><th>職位</th><th>狀態</th><th>手機</th><th>入職日</th><th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {isLoading ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
                  ) : employees.length === 0 ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無員工資料</td></tr>
                  ) : (
                    employees.map(emp => {
                      const sc = statusConfig[emp.status] ?? { label: emp.status, cls: 'neutral' };
                      return (
                        <tr key={emp.id}>
                          <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{emp.employeeNo}</td>
                          <td style={{ fontWeight: 600, fontSize: 13 }}>{emp.chineseName}</td>
                          <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{emp.departmentName ?? '—'}</td>
                          <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{emp.jobTitleName ?? emp.positionTitle ?? '—'}</td>
                          <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                          <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{emp.mobile ?? '—'}</td>
                          <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{emp.hireDate?.slice(0, 10) ?? '—'}</td>
                          <td>
                            <div style={{ display: 'flex', gap: 4 }}>
                              <button className="icon-action-btn" title="查看" onClick={() => setViewingEmployee(emp)}><Eye size={14} /></button>
                              <button className="icon-action-btn" title="編輯" onClick={() => setEditingEmployee(emp)}><Edit2 size={14} /></button>
                              {emp.status === 'active' && (
                                <button className="icon-action-btn" title="離職" onClick={() => { setTerminatingEmployee(emp); setTerminateDate(new Date().toISOString().slice(0, 10)); setTerminateReason(''); }} style={{ color: 'var(--crimson-400)' }}><X size={14} /></button>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </>
        );

      case 'attendance':
        return (
          <>
            <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
              <div style={{ position: 'relative', flex: 1 }}>
                <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
                <input type="text" placeholder="搜尋員工編號、姓名..." value={search}
                  onChange={e => setSearch(e.target.value)}
                  style={{ width: '100%', padding: '8px 12px 8px 32px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', fontFamily: 'inherit' }} />
              </div>
              <input type="date" style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }} />
              <button className="btn btn-primary btn-sm" onClick={() => setShowAddAttendance(true)}><Plus size={13} />新增出勤</button>
            </div>

            <div className="glass-card" style={{ padding: '18px' }}>
              <div style={{ marginBottom: 14 }}>
                <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>出勤記錄</h3>
                <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {attendanceRecords.length} 筆出勤記錄</div>
              </div>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>日期</th><th>員工編號</th><th>姓名</th><th>班別</th><th>打卡時間</th><th>狀態</th><th>加班</th><th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {attendanceLoading ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
                  ) : attendanceRecords.length === 0 ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無出勤記錄</td></tr>
                  ) : (
                    attendanceRecords.map(record => {
                      const sc = attendanceStatusConfig[record.status] ?? { label: record.status, cls: 'neutral' };
                      const totalOt = record.ot1Hours + record.ot2Hours + record.ot3Hours;
                      return (
                        <tr key={record.id}>
                          <td className="mono" style={{ fontSize: 12 }}>{record.date?.slice(0, 10)}</td>
                          <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{record.employeeNo}</td>
                          <td style={{ fontWeight: 600, fontSize: 13 }}>{record.employeeName}</td>
                          <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{record.shiftType ?? '—'}</td>
                          <td className="mono" style={{ fontSize: 11 }}>
                            {record.clockIn?.slice(11, 16) ?? '—'} ~ {record.clockOut?.slice(11, 16) ?? '—'}
                          </td>
                          <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                          <td style={{ fontSize: 12 }}>{totalOt > 0 ? `${totalOt}h` : '—'}</td>
                          <td>
                            <div style={{ display: 'flex', gap: 4 }}>
                              {record.approvalStatus === 'pending' && (
                                <button className="icon-action-btn" title="核准" onClick={() => handleApproveAttendance(record.id)}><Check size={14} /></button>
                              )}
                              <button className="icon-action-btn" title="查看"><Eye size={14} /></button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </>
        );

      case 'leave':
        return (
          <>
            <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
              <div style={{ position: 'relative', flex: 1 }}>
                <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
                <input type="text" placeholder="搜尋請假單號、員工編號、姓名..." value={search}
                  onChange={e => setSearch(e.target.value)}
                  style={{ width: '100%', padding: '8px 12px 8px 32px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', fontFamily: 'inherit' }} />
              </div>
              <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
                style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }}>
                <option value="">全部狀態</option>
                <option value="pending">待審</option>
                <option value="approved">核准</option>
                <option value="rejected">退回</option>
              </select>
              <button className="btn btn-primary btn-sm" onClick={() => setShowAddLeave(true)}><Plus size={13} />新增請假</button>
            </div>

            <div className="glass-card" style={{ padding: '18px' }}>
              <div style={{ marginBottom: 14 }}>
                <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>請假申請</h3>
                <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {leaveRequests.length} 筆請假記錄</div>
              </div>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>單號</th><th>員工編號</th><th>姓名</th><th>假別</th><th>起訖日期</th><th>時數</th><th>狀態</th><th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {leaveLoading ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
                  ) : leaveRequests.length === 0 ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無請假記錄</td></tr>
                  ) : (
                    leaveRequests.map(record => {
                      const sc = leaveStatusConfig[record.status] ?? { label: record.status, cls: 'neutral' };
                      const lt = leaveTypes.find(l => l.value === record.leaveType);
                      return (
                        <tr key={record.id}>
                          <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{record.applicationNo}</td>
                          <td className="mono" style={{ fontSize: 12 }}>{record.employeeNo}</td>
                          <td style={{ fontWeight: 600, fontSize: 13 }}>{record.employeeName}</td>
                          <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{lt?.label ?? record.leaveType}</td>
                          <td className="mono" style={{ fontSize: 11 }}>{record.startDate.slice(0, 10)} ~ {record.endDate.slice(0, 10)}</td>
                          <td style={{ fontSize: 12 }}>{record.totalHours}h</td>
                          <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                          <td>
                            <div style={{ display: 'flex', gap: 4 }}>
                              {record.status === 'pending' && (
                                <>
                                  <button className="icon-action-btn" title="核准" onClick={() => handleApproveLeave(record.id)}><Check size={14} /></button>
                                  <button className="icon-action-btn" title="退回" onClick={() => handleRejectLeave(record.id)}><X size={14} /></button>
                                </>
                              )}
                              <button className="icon-action-btn" title="查看"><Eye size={14} /></button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </>
        );

      case 'payroll':
        return (
          <>
            <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
              <div style={{ position: 'relative', flex: 1 }}>
                <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
                <input type="text" placeholder="搜尋員工編號、姓名..." value={search}
                  onChange={e => setSearch(e.target.value)}
                  style={{ width: '100%', padding: '8px 12px 8px 32px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', fontFamily: 'inherit' }} />
              </div>
              <input type="month" style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }} />
              <button className="btn btn-primary btn-sm" onClick={() => setShowAddPayroll(true)}><Plus size={13} />新增薪資</button>
            </div>

            <div className="glass-card" style={{ padding: '18px' }}>
              <div style={{ marginBottom: 14 }}>
                <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>薪資核算</h3>
                <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {payrolls.length} 筆薪資記錄</div>
              </div>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>員工編號</th><th>姓名</th><th>薪資年月</th><th>基本薪資</th><th>應發薪資</th><th>實發薪資</th><th>狀態</th><th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {payrollLoading ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
                  ) : payrolls.length === 0 ? (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無薪資記錄</td></tr>
                  ) : (
                    payrolls.map(record => {
                      const sc = payrollStatusConfig[record.status] ?? { label: record.status, cls: 'neutral' };
                      return (
                        <tr key={record.id}>
                          <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{record.employeeNo}</td>
                          <td style={{ fontWeight: 600, fontSize: 13 }}>{record.employeeName}</td>
                          <td className="mono" style={{ fontSize: 12 }}>{record.periodStart.slice(0, 7)}</td>
                          <td style={{ fontSize: 12, textAlign: 'right' }}>{record.basicSalary.toLocaleString()}</td>
                          <td style={{ fontSize: 12, textAlign: 'right', color: 'var(--emerald-400)' }}>{record.grossPay.toLocaleString()}</td>
                          <td style={{ fontSize: 12, textAlign: 'right', fontWeight: 600, color: 'var(--cobalt-300)' }}>{record.netPay.toLocaleString()}</td>
                          <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                          <td>
                            <div style={{ display: 'flex', gap: 4 }}>
                              {record.status === 'draft' && (
                                <button className="icon-action-btn" title="核准" onClick={() => handleApprovePayroll(record.id)}><Check size={14} /></button>
                              )}
                              <button className="icon-action-btn" title="查看"><Eye size={14} /></button>
                              <button className="icon-action-btn" title="編輯"><Edit2 size={14} /></button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </>
        );
    }
  };

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">人力資源</h2>
          <p className="module-page-desc">員工資料 · 出勤管理 · 請假審批 · 薪資核算</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
        </div>
      </div>

      <div className="module-stat-grid">
        {getHRStats(employees, attendanceRecords, leaveRequests).map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div className="module-stat-label">{s.label}</div>
            <div className="module-stat-value kpi-value" style={{ color: s.color }}>{s.value}</div>
          </div>
        ))}
      </div>

      {/* Tab Navigation */}
      <div className="tab-nav">
        {tabs.map(tab => (
          <button
            key={tab.key}
            className={`tab-btn ${activeTab === tab.key ? 'active' : ''}`}
            onClick={() => setActiveTab(tab.key)}
          >
            {tab.icon}
            <span>{tab.label}</span>
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {renderTabContent()}

      {/* Employee Modals */}
      <FormModal open={showAdd} title="新增員工" width={620}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['身分資料', '緊急聯絡', '組織職位', '薪資金融', '法定保障', '出勤學經歷'],
          getStepFields: (stepIndex) => employeeWizardFields[stepIndex] || []
        }}
        onClose={() => setShowAdd(false)}
        onSubmit={async (data: Record<string, unknown>) => {
          if (data.nationalId && !/^[A-Z]\d{9}$/.test(String(data.nationalId))) {
            alert('身分證字號格式錯誤，應為 1 個大寫英文字母加上 9 位數字');
            return;
          }
          if (data.email && !String(data.email).includes('@')) {
            alert('Email 格式錯誤，需包含 @');
            return;
          }
          if (data.mobile && !/^09\d{8}$/.test(String(data.mobile))) {
            alert('手機號碼格式錯誤，應為 09 開頭的 10 位數字');
            return;
          }
          if (data.baseSalary != null) {
            const num = Number(data.baseSalary);
            if (isNaN(num) || num < 0) {
              alert('底薪不可為負數');
              return;
            }
          }
          if (data.probationEndDate && data.hireDate) {
            const probationEnd = new Date(data.probationEndDate as string);
            const hireDate = new Date(data.hireDate as string);
            if (probationEnd <= hireDate) {
              alert('試用期截止日必須晚於到職日');
              return;
            }
          }
          const cleaned: Record<string, unknown> = {};
          for (const [key, value] of Object.entries(data)) {
            cleaned[key] = value === '' ? null : value;
          }
          await employeesApi.create(cleaned);
          setShowAdd(false);
          setRefreshKey(k => k + 1);
        }}
      />

      <FormModal open={!!editingEmployee} title="編輯員工" tabs={employeeTabs} width={620}
        initialValues={editingEmployee ? (() => {
          const vals: Record<string, unknown> = { ...editingEmployee };
          const dateFields = ['birthDate', 'hireDate', 'probationEndDate'];
          dateFields.forEach(f => {
            if (vals[f] && typeof vals[f] === 'string') {
              vals[f] = (vals[f] as string).slice(0, 10);
            }
          });
          return vals;
        })() : undefined}
        onClose={() => setEditingEmployee(null)}
        onSubmit={async (data) => {
          if (data.nationalId && !/^[A-Z]\d{9}$/.test(String(data.nationalId))) {
            alert('身分證字號格式錯誤，應為 1 個大寫英文字母加上 9 位數字');
            return;
          }
          if (data.email && !String(data.email).includes('@')) {
            alert('Email 格式錯誤，需包含 @');
            return;
          }
          if (data.mobile && !/^09\d{8}$/.test(String(data.mobile))) {
            alert('手機號碼格式錯誤，應為 09 開頭的 10 位數字');
            return;
          }
          if (data.baseSalary != null && data.baseSalary !== '' && Number(data.baseSalary) < 0) {
            alert('底薪不可為負數');
            return;
          }
          if (data.probationEndDate && data.hireDate && new Date(String(data.probationEndDate)) <= new Date(String(data.hireDate))) {
            alert('試用期截止日必須晚於到職日');
            return;
          }
          const cleaned: Record<string, unknown> = {};
          for (const [key, value] of Object.entries(data)) {
            cleaned[key] = value === '' ? null : value;
          }
          try {
            await employeesApi.update(editingEmployee.id, cleaned);
            setEditingEmployee(null);
            setRefreshKey(k => k + 1);
          } catch (err: any) {
            alert('更新失敗: ' + (err.response?.data?.message || err.message));
          }
        }}
      />

      <FormModal open={!!viewingEmployee} title="員工資料" width={700}
        onClose={() => setViewingEmployee(null)}
      >
        {viewingEmployee && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, padding: 16 }}>
            <div style={{ gridColumn: '1 / -1', borderBottom: '1px solid var(--glass-border)', paddingBottom: 12, marginBottom: 8 }}>
              <h3 style={{ color: 'var(--slate-100)', fontSize: 16, fontWeight: 700 }}>{viewingEmployee.chineseName}</h3>
              <p style={{ color: 'var(--slate-500)', fontSize: 12 }}>{viewingEmployee.employeeNo}</p>
            </div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>部門</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.departmentName ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>職稱</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.jobTitleName ?? viewingEmployee.positionTitle ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>職等</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.gradeName ?? viewingEmployee.jobGrade ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>狀態</label><p style={{ color: 'var(--slate-200)' }}><span className={`status-badge status-${statusConfig[viewingEmployee.status]?.cls || 'neutral'}`}>{statusConfig[viewingEmployee.status]?.label || viewingEmployee.status}</span></p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>到職日</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.hireDate?.slice(0, 10) ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>Email</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.email ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>手機</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.mobile ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>底薪</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.baseSalary?.toLocaleString() ?? '—'}</p></div>
            <div><label style={{ fontSize: 11, color: 'var(--slate-500)' }}>職務類別</label><p style={{ color: 'var(--slate-200)' }}>{viewingEmployee.jobCategory === 'production' ? '生產部門' : viewingEmployee.jobCategory === 'admin' ? '管理部門' : viewingEmployee.jobCategory === 'service' ? '服務部門' : '—'}</p></div>
          </div>
        )}
      </FormModal>

      <FormModal open={!!terminatingEmployee} title="員工離職處理" width={420}
        onClose={() => setTerminatingEmployee(null)}
        onSubmit={async () => {
          if (!terminateDate) { alert('請選擇離職日期'); return; }
          try {
            await employeesApi.terminate(terminatingEmployee.id, terminateReason || '离职');
            setTerminatingEmployee(null);
            setRefreshKey(k => k + 1);
          } catch (err: any) {
            alert('操作失败: ' + (err.response?.data?.message || err.message));
          }
        }}
      >
        {terminatingEmployee && (
          <div style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div style={{ padding: 12, background: 'var(--slate-800)', borderRadius: 6 }}>
              <div style={{ fontSize: 12, color: 'var(--slate-500)' }}>員工</div>
              <div style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-100)' }}>{terminatingEmployee.chineseName}</div>
              <div style={{ fontSize: 12, color: 'var(--slate-500)' }}>{terminatingEmployee.employeeNo}</div>
            </div>
            <div>
              <label style={{ fontSize: 12, color: 'var(--slate-400)', display: 'block', marginBottom: 6 }}>離職日期 *</label>
              <input type="date" value={terminateDate} onChange={e => setTerminateDate(e.target.value)}
                style={{ width: '100%', padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none' }} />
            </div>
            <div>
              <label style={{ fontSize: 12, color: 'var(--slate-400)', display: 'block', marginBottom: 6 }}>離職原因</label>
              <textarea value={terminateReason} onChange={e => setTerminateReason(e.target.value)} placeholder="請輸入離職原因..."
                style={{ width: '100%', padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', minHeight: 80, resize: 'vertical' }} />
            </div>
            <div style={{ fontSize: 11, color: 'var(--amber-400)' }}>確認後員工狀態將變更為「離職」，並停止系統權限。</div>
          </div>
        )}
      </FormModal>

      <FormModal open={showAddLeave} title="新增請假" width={480}
        onClose={() => setShowAddLeave(false)}
        onSubmit={async (data: Record<string, unknown>) => {
          try {
            await leaveRequestsApi.create(data);
            setShowAddLeave(false);
            setRefreshKey(k => k + 1);
          } catch (err: any) {
            alert('建立失敗: ' + (err.response?.data?.message || err.message));
          }
        }}
        fields={[
          { name: 'employeeId', label: '員工', type: 'select', required: true, options: employees.map(e => ({ value: e.id, label: `${e.employeeNo} - ${e.chineseName}` })) },
          { name: 'leaveType', label: '假別', type: 'select', required: true, options: leaveTypes },
          { name: 'startDate', label: '開始日期', type: 'date', required: true },
          { name: 'endDate', label: '結束日期', type: 'date', required: true },
          { name: 'startTime', label: '開始時間', type: 'text', placeholder: '如：09:00' },
          { name: 'endTime', label: '結束時間', type: 'text', placeholder: '如：18:00' },
          { name: 'reason', label: '請假原因', type: 'textarea', placeholder: '請輸入請假原因...' },
        ]}
      />

      <FormModal open={showAddAttendance} title="新增出勤" width={480}
        onClose={() => setShowAddAttendance(false)}
        onSubmit={async (data: Record<string, unknown>) => {
          try {
            await attendanceApi.create(data);
            setShowAddAttendance(false);
            setRefreshKey(k => k + 1);
          } catch (err: any) {
            alert('建立失敗: ' + (err.response?.data?.message || err.message));
          }
        }}
        fields={[
          { name: 'employeeId', label: '員工', type: 'select', required: true, options: employees.map(e => ({ value: e.id, label: `${e.employeeNo} - ${e.chineseName}` })), description: '選擇員工' },
          { name: 'date', label: '出勤日期', type: 'date', required: true, description: '出勤的日期' },
          { name: 'shiftType', label: '班別類型', type: 'select', options: [{ value: 'fixed', label: '固定班' }, { value: 'day', label: '常日班' }, { value: 'rotating', label: '輪班制' }], description: '上班班別' },
          { name: 'clockIn', label: '上班打卡時間', type: 'time', placeholder: '如：09:00', description: '上班打卡時間' },
          { name: 'clockOut', label: '下班打卡時間', type: 'time', placeholder: '如：18:00', description: '下班打卡時間' },
          { name: 'status', label: '出勤狀態', type: 'select', required: true, options: [{ value: 'normal', label: '正常' }, { value: 'late', label: '遲到' }, { value: 'early_leave', label: '早退' }, { value: 'absent', label: '缺勤' }], description: '當天出勤狀態' },
          { name: 'lateMinutes', label: '遲到分鐘數', type: 'number', placeholder: '0', min: 0, description: '遲到的分鐘數' },
          { name: 'ot1Hours', label: '加班時數(一)', type: 'number', placeholder: '0', min: 0, description: '平日加班時數' },
          { name: 'ot2Hours', label: '加班時數(二)', type: 'number', placeholder: '0', min: 0, description: '休息日加班時數' },
          { name: 'ot3Hours', label: '加班時數(三)', type: 'number', placeholder: '0', min: 0, description: '國定假日加班時數' },
          { name: 'remarks', label: '備註', type: 'textarea', placeholder: '補充說明...' },
        ]}
      />

      <FormModal open={showAddPayroll} title="新增薪資" width={520}
        onClose={() => setShowAddPayroll(false)}
        onSubmit={async (data: Record<string, unknown>) => {
          try {
            await payrollApi.create(data);
            setShowAddPayroll(false);
            setRefreshKey(k => k + 1);
          } catch (err: any) {
            alert('建立失敗: ' + (err.response?.data?.message || err.message));
          }
        }}
        fields={[
          { name: 'employeeId', label: '員工', type: 'select', required: true, options: employees.map(e => ({ value: e.id, label: `${e.employeeNo} - ${e.chineseName}` })), description: '選擇要發放薪資的員工' },
          { name: 'periodStart', label: '薪資年月', type: 'month', required: true, description: '薪資所屬的年月' },
          { name: 'basicSalary', label: '基本薪資', type: 'number', required: true, placeholder: '29500', description: '員工的基本工資' },
          { name: 'overtimeHours', label: '加班時數', type: 'number', placeholder: '0', min: 0, description: '本月加班時數' },
          { name: 'overtimePay', label: '加班費', type: 'number', placeholder: '0', min: 0, description: '加班費金額' },
          { name: 'foodAllowance', label: '伙食津貼', type: 'number', placeholder: '0', min: 0, description: '伙食補助津貼' },
          { name: 'positionAllowance', label: '職務加給', type: 'number', placeholder: '0', min: 0, description: '職務加給津貼' },
          { name: 'fullAttendanceBonus', label: '全勤獎金', type: 'number', placeholder: '0', min: 0, description: '全勤獎金' },
          { name: 'leaveDeductions', label: '請假扣薪', type: 'number', placeholder: '0', min: 0, description: '請假時數扣薪' },
          { name: 'laborInsurance', label: '勞保扣款', type: 'number', placeholder: '0', min: 0, description: '勞工保險個人負擔金額' },
          { name: 'healthInsurance', label: '健保扣款', type: 'number', placeholder: '0', min: 0, description: '全民健康保險個人負擔金額' },
          { name: 'pension', label: '勞退提繳', type: 'number', placeholder: '0', min: 0, description: '勞工退休金個人負擔金額' },
          { name: 'incomeTax', label: '所得稅扣款', type: 'number', placeholder: '0', min: 0, description: '代扣所得稅金額' },
        ]}
      />

      <style>{`
        .module-page { display: flex; flex-direction: column; gap: 16px; }
        .module-page-header { display: flex; align-items: flex-start; justify-content: space-between; }
        .module-page-title { font-size: 20px; font-weight: 700; letter-spacing: -0.02em; color: var(--slate-100); }
        .module-page-desc { font-size: 12px; color: var(--slate-500); margin-top: 2px; font-family: var(--font-mono); }
        .module-page-actions { display: flex; gap: 8px; }
        .module-stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
        .module-stat-card { padding: 14px 16px; }
        .module-stat-label { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.07em; color: var(--slate-500); margin-bottom: 8px; font-family: var(--font-display); }
        .module-stat-value { font-size: 26px; font-weight: 700; letter-spacing: -0.04em; }
        .tab-nav { display: flex; gap: 4px; padding: 4px; background: var(--slate-800); border-radius: 8px; }
        .tab-btn { display: flex; align-items: center; gap: 6px; padding: 8px 16px; background: transparent; border: none; color: var(--slate-400); font-size: 13px; font-weight: 500; cursor: pointer; border-radius: 6px; transition: all 0.15s; font-family: inherit; }
        .tab-btn:hover { color: var(--slate-200); background: var(--slate-700); }
        .tab-btn.active { color: var(--slate-100); background: var(--cobalt-600); }
      `}</style>
    </div>
  );
};
