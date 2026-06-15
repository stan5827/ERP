import type { FormTab, FormField } from '../../components/FormModal';

/* ── 統計卡片（動態計算）── */
export function getHRStats(employees: any[], attendanceRecords: any[], leaveRequests: any[]) {
  const today = new Date().toISOString().slice(0, 10);
  const activeEmployees = employees.filter((e: any) => e.status?.toLowerCase() === 'active').length;
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

export const statusConfig: Record<string, { label: string; cls: string }> = {
  active: { label: '在職', cls: 'active' },
  leave: { label: '留職停薪', cls: 'warning' },
  terminated: { label: '已離職', cls: 'neutral' },
  inactive: { label: '停用', cls: 'neutral' },
  '': { label: '未知', cls: 'neutral' },
};

export const attendanceStatusConfig: Record<string, { label: string; cls: string }> = {
  normal: { label: '正常', cls: 'active' },
  late: { label: '遲到', cls: 'warning' },
  absent: { label: '缺勤', cls: 'danger' },
  early_leave: { label: '早退', cls: 'warning' },
  pending: { label: '待審', cls: 'neutral' },
};

export const leaveStatusConfig: Record<string, { label: string; cls: string }> = {
  pending: { label: '待審', cls: 'neutral' },
  approved: { label: '核准', cls: 'active' },
  rejected: { label: '退回', cls: 'danger' },
  cancelled: { label: '取消', cls: 'warning' },
};

export const payrollStatusConfig: Record<string, { label: string; cls: string }> = {
  draft: { label: '草稿', cls: 'neutral' },
  approved: { label: '已核准', cls: 'active' },
  paid: { label: '已發放', cls: 'active' },
  void: { label: '作廢', cls: 'danger' },
};

export const leaveTypes = [
  { value: 'annual', label: '特休/年假' },
  { value: 'personal', label: '事假' },
  { value: 'sick', label: '病假' },
  { value: 'maternity', label: '產假' },
  { value: 'marriage', label: '婚假' },
  { value: 'funeral', label: '喪假' },
  { value: 'official', label: '公假' },
];

export function buildEmployeeTabs(
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
          { value: 'terminated', label: '已離職' }
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
        { name: 'laborInsuranceGrade', label: '勞保投保等級', type: 'number', placeholder: '0', min: 0, description: '勞工保險投保等級(1-20級)，決定每月勞保費金額。雇主負擔70%、員工自負20%、政府補助10%。' },
        { name: 'healthInsuranceGrade', label: '健保投保等級', type: 'number', placeholder: '0', min: 0, description: '全民健康保險投保金額等級(1-13級)，決定每月健保費金額。雇主負擔60%、員工自負30%、政府補助10%。' },
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
        { name: 'overtimePermission', label: '加班權限', type: 'checkbox', placeholder: '開放線上申請加班', description: '勾選後員工可透過系統申請加班，未勾選則無法申請。' },
        { name: 'cardNumber', label: '刷卡卡號', type: 'text', placeholder: '對應刷卡機 ID', description: '員工專屬的刷卡卡片號碼，用於考勤系統辨識出勤紀錄。' },
        { name: 'biometricId', label: '生物辨識 ID', type: 'text', placeholder: '人臉/指紋辨識 ID', description: '人臉辨識或指紋辨識系統的員工ID。' },
        { name: 'highestEducation', label: '最高學歷', type: 'text', placeholder: '如：國立台灣大學 資訊工程系', description: '最高學歷的學校名稱與科系。' },
        { name: 'major', label: '科系', type: 'text', placeholder: '選填', description: '大學或研究所的科系名稱。' },
        { name: 'graduationYear', label: '畢業年度', type: 'number', placeholder: '2020', min: 1950, max: 2030, description: '最高學歷的畢業年份。' },
        { name: 'foreignLanguages', label: '外語能力', type: 'text', placeholder: '如：英文流利、日文 N2', description: '語言能力檢定或自評程度。' },
        { name: 'skills', label: '專業技能', type: 'textarea', placeholder: '專業技能、證照、經歷...', description: '與工作相關的專業技能、專業證照、或過往工作經驗摘要。' },
      ],
    },
  ];
}

export function buildEmployeeWizardFields(
  deptOptions: { value: string; label: string }[],
  titleOptions: { value: string; label: string }[],
  gradeOptions: { value: string; label: string }[]
): FormField[][] {
  const tabs = buildEmployeeTabs(deptOptions, titleOptions, gradeOptions);
  return tabs.map(t => t.fields);
}
