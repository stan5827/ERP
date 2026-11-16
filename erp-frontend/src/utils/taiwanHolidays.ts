export interface TaiwanHoliday {
  date: string;
  name: string;
  holidayType: 'national' | 'important';
}

const pad = (value: number) => String(value).padStart(2, '0');
const traditionalLunarMonths: Record<string, number> = {
  正月: 1,
  二月: 2,
  三月: 3,
  四月: 4,
  五月: 5,
  六月: 6,
  七月: 7,
  八月: 8,
  九月: 9,
  十月: 10,
  冬月: 11,
  臘月: 12,
};

export const toDateKey = (date: Date) =>
  `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

const getLunarDate = (date: Date) => {
  const formatter = new Intl.DateTimeFormat('zh-TW-u-ca-chinese', {
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
  });
  const parts = formatter.formatToParts(date);
  const monthPart = parts.find(part => part.type === 'month')?.value ?? '';
  const dayPart = parts.find(part => part.type === 'day')?.value ?? '0';
  const numericMonth = Number(monthPart.replace(/[^\d]/g, ''));
  const month = traditionalLunarMonths[monthPart] ?? numericMonth;
  const day = Number(dayPart.replace(/[^\d]/g, ''));

  return { month, day };
};

const getQingmingDate = (year: number) => {
  const day = Math.floor(5.242 + 0.2422 * (year - 2000) - Math.floor((year - 2000) / 4));
  return new Date(year, 3, day);
};

export const getTaiwanHolidays = (year: number): TaiwanHoliday[] => {
  const holidays: TaiwanHoliday[] = [
    { date: `${year}-01-01`, name: '元旦', holidayType: 'national' },
    { date: `${year}-02-28`, name: '和平紀念日', holidayType: 'national' },
    { date: `${year}-05-01`, name: '勞動節', holidayType: 'national' },
    { date: `${year}-10-10`, name: '國慶日', holidayType: 'national' },
    { date: `${year}-02-28`, name: '二二八紀念日', holidayType: 'important' },
    { date: `${year}-03-08`, name: '婦女節', holidayType: 'important' },
    { date: `${year}-10-25`, name: '光復節', holidayType: 'important' },
    { date: `${year}-10-28`, name: '教師節', holidayType: 'important' },
    { date: `${year}-12-25`, name: '行憲紀念日', holidayType: 'important' },
  ];

  holidays.push({
    date: toDateKey(getQingmingDate(year)),
    name: '清明節',
    holidayType: 'national',
  });

  const lunarDefinitions: Record<string, string> = {
    '1-1': '春節',
    '1-15': '元宵節',
    '2-2': '土地公誕',
    '2-19': '觀音誕',
    '3-23': '媽祖誕',
    '4-8': '佛誕',
    '5-5': '端午節',
    '6-6': '天貺節',
    '6-24': '關公誕',
    '7-7': '七夕',
    '7-15': '中元節',
    '7-30': '地藏節',
    '8-15': '中秋節',
    '9-9': '重陽節',
    '10-15': '下元節',
    '12-8': '臘八節',
    '12-16': '尾牙',
    '12-24': '送神日（小年夜）',
  };

  for (let month = 0; month < 12; month++) {
    for (let day = 1; day <= 31; day++) {
      const date = new Date(year, month, day);
      if (date.getMonth() !== month) break;

      const lunar = getLunarDate(date);
      const lunarName = lunarDefinitions[`${lunar.month}-${lunar.day}`];
      if (!lunarName) continue;

      const lunarHoliday: TaiwanHoliday = {
        date: toDateKey(date),
        name: lunarName,
        holidayType: ['春節', '端午節', '中秋節', '元宵節'].includes(lunarName) ? 'national' : 'important',
      };
      holidays.push(lunarHoliday);
    }
  }

  const lunarMonth12Days: Array<{ date: Date; day: number }> = [];
  for (let month = 0; month < 12; month++) {
    for (let day = 1; day <= 31; day++) {
      const date = new Date(year, month, day);
      if (date.getMonth() !== month) break;
      const lunar = getLunarDate(date);
      if (lunar.month === 12) lunarMonth12Days.push({ date, day: lunar.day });
    }
  }
  const lastLunarMonth12Day = lunarMonth12Days.reduce(
    (latest, item) => item.day > latest.day ? item : latest,
    lunarMonth12Days[0],
  );
  if (lastLunarMonth12Day) {
    holidays.push({
      date: toDateKey(lastLunarMonth12Day.date),
      name: '除夕',
      holidayType: 'national',
    });
  }

  const unique = new Map<string, TaiwanHoliday>();
  for (const holiday of holidays) {
    const key = `${holiday.date}-${holiday.name}`;
    if (!unique.has(key)) unique.set(key, holiday);
  }
  return [...unique.values()].sort((a, b) => a.date.localeCompare(b.date));
};
