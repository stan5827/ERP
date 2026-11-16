import { describe, expect, it } from 'vitest';
import { getTaiwanHolidays } from './taiwanHolidays';

describe('getTaiwanHolidays', () => {
  it('maps lunar festivals to the correct Gregorian dates', () => {
    const holidays = getTaiwanHolidays(2026);

    expect(holidays).toContainEqual({
      date: '2026-02-17',
      name: '春節',
      holidayType: 'national',
    });
    expect(holidays).toContainEqual({
      date: '2026-02-11',
      name: '送神日（小年夜）',
      holidayType: 'important',
    });
    expect(holidays).toContainEqual({
      date: '2026-03-03',
      name: '元宵節',
      holidayType: 'national',
    });
    expect(holidays).toContainEqual({
      date: '2026-10-18',
      name: '重陽節',
      holidayType: 'important',
    });
  });
});
