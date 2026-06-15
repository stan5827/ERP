import { useState, useMemo, useCallback, useEffect } from 'react';
import React from 'react';
import {
  CalendarDays, ChevronLeft, ChevronRight, Plus, Download, Filter,
  Clock, MapPin, Users, Flag, Wrench, BookOpen, FolderOpen, Sun, CheckCircle2, Factory, Loader2, X, AlertTriangle,
  Edit2, Trash2, Calendar as CalendarIcon
} from 'lucide-react';
import { useCalendarRange, useCalendarCategories, useCreateCalendarEvent, useUpdateCalendarEvent, useDeleteCalendarEvent, useMeetingRooms, useCreateCalendarCategory, useCreateMeetingRoom } from '../hooks/useApi';
import { calendarApi } from '../api/api';
import type { CalendarEventDto, CalendarCategoryDto } from '../api/api';

type ViewMode = 'month' | 'week' | 'day';

const categoryIconMap: Record<string, React.ReactNode> = {
  meeting: <Users size={12} />,
  maintenance: <Wrench size={12} />,
  holiday: <Sun size={12} />,
  task: <CheckCircle2 size={12} />,
  workorder: <Factory size={12} />,
  milestone: <Flag size={12} />,
  downtime: <Wrench size={12} />,
  training: <BookOpen size={12} />,
  leave: <CalendarIcon size={12} />,
  // visit: <VisitIcon size={12} />, // VisitIcon not available in lucide-react
  project: <FolderOpen size={12} />,
};

function formatTime(dateStr: string) {
  const d = new Date(dateStr);
  return d.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false });
}

function isSameDay(d1: Date, d2: Date) {
  return d1.getFullYear() === d2.getFullYear() && d1.getMonth() === d2.getMonth() && d1.getDate() === d2.getDate();
}

function getMonthDays(year: number, month: number): (Date | null)[] {
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const days: (Date | null)[] = [];
  // Padding for start of month (0=Sun)
  const startPad = firstDay.getDay();
  for (let i = 0; i < startPad; i++) days.push(null);
  for (let d = 1; d <= lastDay.getDate(); d++) days.push(new Date(year, month, d));
  // Padding for end
  while (days.length % 7 !== 0) days.push(null);
  return days;
}

function getWeekDays(date: Date): Date[] {
  const day = date.getDay();
  const start = new Date(date);
  start.setDate(start.getDate() - day);
  const days: Date[] = [];
  for (let i = 0; i < 7; i++) {
    const d = new Date(start);
    d.setDate(d.getDate() + i);
    days.push(d);
  }
  return days;
}

function getEventsForDay(events: CalendarEventDto[], day: Date): CalendarEventDto[] {
  return events.filter(e => {
    const start = new Date(e.startDate);
    const end = new Date(e.endDate);
    const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 0, 0, 0);
    const dayEnd = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59);
    return start <= dayEnd && end >= dayStart;
  });
}

function getDateRange(viewMode: ViewMode, currentDate: Date) {
  if (viewMode === 'month') {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    const start = new Date(year, month, 1);
    const end = new Date(year, month + 1, 0);
    // extend to full weeks
    const startDay = start.getDay();
    start.setDate(start.getDate() - startDay);
    const endDay = end.getDay();
    end.setDate(end.getDate() + (6 - endDay));
    return { start, end };
  } else if (viewMode === 'week') {
    const days = getWeekDays(currentDate);
    return { start: days[0], end: days[6] };
  } else {
    return { start: currentDate, end: currentDate };
  }
}

export const Calendar: React.FC = () => {
  const [viewMode, setViewMode] = useState<ViewMode>('month');
  const [currentDate, setCurrentDate] = useState(new Date());
  const [selectedEvent, setSelectedEvent] = useState<CalendarEventDto | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingEvent, setEditingEvent] = useState<CalendarEventDto | null>(null);
  const [activeCategories, setActiveCategories] = useState<Set<string>>(new Set());
  const [showAddCategory, setShowAddCategory] = useState(false);
  const [showAddRoom, setShowAddRoom] = useState(false);
  const [newCategory, setNewCategory] = useState({ name: '', code: '', color: '#3B82F6' });
  const [newRoom, setNewRoom] = useState({ roomName: '', location: '', capacity: 8 });
  const [form, setForm] = useState<Record<string, unknown>>({
    title: '', description: '', categoryCode: 'meeting', location: '',
    startDate: new Date().toISOString().slice(0, 16),
    endDate: new Date(Date.now() + 3600000).toISOString().slice(0, 16),
    isAllDay: false, visibility: 'department', meetingRoomId: '', participants: '', recurrenceRule: '',
  });

  const today = new Date();
  const range = getDateRange(viewMode, currentDate);
  const startStr = range.start.toISOString().slice(0, 10);
  const endStr = range.end.toISOString().slice(0, 10);

  const { data: rangeData, isLoading } = useCalendarRange(startStr, endStr);
  const { data: categoriesData } = useCalendarCategories();
  const { data: meetingRoomsData } = useMeetingRooms();

  const events = useMemo(() => {
    const raw = (rangeData?.data as CalendarEventDto[]) || [];
    if (activeCategories.size === 0) return raw;
    return raw.filter(e => !e.categoryCode || activeCategories.has(e.categoryCode));
  }, [rangeData, activeCategories]);

  const categories = (categoriesData?.data as CalendarCategoryDto[]) || [];
  const meetingRooms = meetingRoomsData?.data || [];

  // Sync form state when modal opens
  useEffect(() => {
    if (showForm && editingEvent) {
      setForm({
        title: editingEvent.title,
        description: editingEvent.description || '',
        categoryCode: editingEvent.categoryCode || 'meeting',
        location: editingEvent.location || '',
        startDate: editingEvent.startDate.slice(0, 16),
        endDate: editingEvent.endDate.slice(0, 16),
        isAllDay: editingEvent.isAllDay,
        visibility: editingEvent.visibility,
        meetingRoomId: editingEvent.meetingRoomId || '',
        participants: editingEvent.participants || '',
        recurrenceRule: editingEvent.recurrenceRule || '',
      });
    } else if (showForm) {
      setForm({
        title: '', description: '', categoryCode: 'meeting', location: '',
        startDate: new Date().toISOString().slice(0, 16),
        endDate: new Date(Date.now() + 3600000).toISOString().slice(0, 16),
        isAllDay: false, visibility: 'department', meetingRoomId: '', participants: '', recurrenceRule: '',
      });
    }
    if (showForm) {
      setShowAddCategory(false);
      setShowAddRoom(false);
      setNewCategory({ name: '', code: '', color: '#3B82F6' });
      setNewRoom({ roomName: '', location: '', capacity: 8 });
    }
  }, [showForm, editingEvent]);

  const navigate = useCallback((dir: number) => {
    setCurrentDate(prev => {
      const d = new Date(prev);
      if (viewMode === 'month') d.setMonth(d.getMonth() + dir);
      else if (viewMode === 'week') d.setDate(d.getDate() + dir * 7);
      else d.setDate(d.getDate() + dir);
      return d;
    });
  }, [viewMode]);

  const goToToday = () => setCurrentDate(new Date());

  const toggleCategory = (code: string) => {
    setActiveCategories(prev => {
      const next = new Set(prev);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      return next;
    });
  };

  const headerTitle = useMemo(() => {
    const y = currentDate.getFullYear();
    const m = currentDate.getMonth() + 1;
    const d = currentDate.getDate();
    if (viewMode === 'month') return `${y} 年 ${m} 月`;
    if (viewMode === 'week') {
      const weekDays = getWeekDays(currentDate);
      const ws = weekDays[0];
      const we = weekDays[6];
      return `${ws.getMonth() + 1}/${ws.getDate()} - ${we.getMonth() + 1}/${we.getDate()}`;
    }
    return `${y}/${m}/${d}`;
  }, [currentDate, viewMode]);

  const handleExportIcs = () => {
    const url = calendarApi.exportIcs(startStr, endStr);
    window.open(url, '_blank');
  };

  // ========== Month View ==========
  const renderMonthView = () => {
    const days = getMonthDays(currentDate.getFullYear(), currentDate.getMonth());
    const weekDays = ['日', '一', '二', '三', '四', '五', '六'];
    return (
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: 1, background: 'rgba(255,255,255,0.03)', borderRadius: 8, overflow: 'hidden' }}>
        {weekDays.map((wd, i) => (
          <div key={i} style={{ padding: '8px 6px', textAlign: 'center', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', background: 'rgba(255,255,255,0.04)' }}>
            {wd}
          </div>
        ))}
        {days.map((day, idx) => {
          if (!day) return <div key={idx} style={{ minHeight: 90, background: 'rgba(0,0,0,0.2)' }} />;
          const dayEvents = getEventsForDay(events, day);
          const isToday = isSameDay(day, today);
          return (
            <div
              key={idx}
              style={{
                minHeight: 90, padding: '4px 6px', cursor: 'pointer',
                background: isToday ? 'rgba(59,130,246,0.08)' : 'rgba(255,255,255,0.02)',
                border: isToday ? '1px solid rgba(59,130,246,0.3)' : '1px solid transparent',
                borderRadius: 2, transition: 'background 0.15s',
              }}
              onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.05)')}
              onMouseLeave={e => (e.currentTarget.style.background = isToday ? 'rgba(59,130,246,0.08)' : 'rgba(255,255,255,0.02)')}
              onClick={() => { setCurrentDate(day); setViewMode('day'); }}
            >
              <div style={{ fontSize: 12, fontWeight: isToday ? 700 : 400, color: isToday ? 'var(--cobalt-400)' : 'var(--slate-300)', marginBottom: 3 }}>
                {day.getDate()}
              </div>
              {dayEvents.slice(0, 3).map((evt, ei) => (
                <div
                  key={ei}
                  onClick={e => { e.stopPropagation(); setSelectedEvent(evt); }}
                  style={{
                    fontSize: 10, padding: '1px 4px', marginBottom: 1, borderRadius: 3, cursor: 'pointer',
                    background: (evt.color || evt.categoryColor || '#3B82F6') + '22',
                    borderLeft: `2px solid ${evt.color || evt.categoryColor || '#3B82F6'}`,
                    color: 'var(--slate-200)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                  }}
                >
                  {evt.isAllDay ? '' : formatTime(evt.startDate) + ' '}{evt.title.length > 12 ? evt.title.slice(0, 12) + '…' : evt.title}
                </div>
              ))}
              {dayEvents.length > 3 && (
                <div style={{ fontSize: 10, color: 'var(--slate-500)', paddingLeft: 4 }}>+{dayEvents.length - 3} 更多</div>
              )}
            </div>
          );
        })}
      </div>
    );
  };

  // ========== Week View ==========
  const renderWeekView = () => {
    const weekDays = getWeekDays(currentDate);
    const weekDayLabels = ['週日', '週一', '週二', '週三', '週四', '週五', '週六'];
    const hours = Array.from({ length: 24 }, (_, i) => i);

    return (
      <div style={{ display: 'grid', gridTemplateColumns: '60px repeat(7, 1fr)', gap: 1, background: 'rgba(255,255,255,0.03)', borderRadius: 8, overflow: 'hidden' }}>
        {/* Header */}
        <div style={{ background: 'rgba(255,255,255,0.04)' }} />
        {weekDays.map((day, i) => {
          const isToday = isSameDay(day, today);
          return (
            <div key={i} style={{ padding: '8px 4px', textAlign: 'center', fontSize: 12, fontWeight: 600, background: 'rgba(255,255,255,0.04)', color: isToday ? 'var(--cobalt-400)' : 'var(--slate-400)' }}>
              {weekDayLabels[i]}<br />{day.getMonth() + 1}/{day.getDate()}
            </div>
          );
        })}
        {/* Hours */}
        {hours.map(hour => (
          <React.Fragment key={hour}>
            <div style={{ padding: '4px 6px', fontSize: 10, color: 'var(--slate-500)', textAlign: 'right', borderTop: '1px solid rgba(255,255,255,0.04)' }}>
              {String(hour).padStart(2, '0')}:00
            </div>
            {weekDays.map((day, di) => {
              const hourEvents = events.filter(e => {
                if (e.isAllDay) return false;
                const start = new Date(e.startDate);
                const end = new Date(e.endDate);
                const cellStart = new Date(day.getFullYear(), day.getMonth(), day.getDate(), hour, 0, 0);
                const cellEnd = new Date(day.getFullYear(), day.getMonth(), day.getDate(), hour, 59, 59);
                return start <= cellEnd && end >= cellStart;
              });
              return (
                <div
                  key={di}
                  style={{
                    minHeight: 36, borderTop: '1px solid rgba(255,255,255,0.04)',
                    padding: '1px 2px', position: 'relative',
                  }}
                >
                  {hourEvents.map((evt, ei) => (
                    <div
                      key={ei}
                      onClick={() => setSelectedEvent(evt)}
                      style={{
                        fontSize: 9, padding: '1px 3px', marginBottom: 1, borderRadius: 2, cursor: 'pointer',
                        background: (evt.color || evt.categoryColor || '#3B82F6') + '33',
                        borderLeft: `2px solid ${evt.color || evt.categoryColor || '#3B82F6'}`,
                        color: 'var(--slate-200)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                      }}
                    >
                      {formatTime(evt.startDate)} {evt.title.length > 8 ? evt.title.slice(0, 8) + '…' : evt.title}
                    </div>
                  ))}
                </div>
              );
            })}
          </React.Fragment>
        ))}
      </div>
    );
  };

  // ========== Day View ==========
  const renderDayView = () => {
    const dayEvents = getEventsForDay(events, currentDate);
    const allDayEvents = dayEvents.filter(e => e.isAllDay);
    const timedEvents = dayEvents.filter(e => !e.isAllDay);
    const hours = Array.from({ length: 24 }, (_, i) => i);

    return (
      <div style={{ display: 'grid', gridTemplateColumns: '60px 1fr', gap: 1, background: 'rgba(255,255,255,0.03)', borderRadius: 8, overflow: 'hidden' }}>
        {/* All-day events */}
        {allDayEvents.length > 0 && (
          <>
            <div style={{ padding: '6px 8px', fontSize: 10, color: 'var(--slate-500)', background: 'rgba(255,255,255,0.04)' }}>全天</div>
            <div style={{ padding: '4px', display: 'flex', flexWrap: 'wrap', gap: 4, background: 'rgba(255,255,255,0.04)' }}>
              {allDayEvents.map((evt, i) => (
                <div
                  key={i}
                  onClick={() => setSelectedEvent(evt)}
                  style={{
                    fontSize: 11, padding: '2px 8px', borderRadius: 4, cursor: 'pointer',
                    background: (evt.color || evt.categoryColor || '#3B82F6') + '22',
                    borderLeft: `3px solid ${evt.color || evt.categoryColor || '#3B82F6'}`,
                    color: 'var(--slate-200)',
                  }}
                >
                  {categoryIconMap[evt.categoryCode || '']}{evt.title}
                </div>
              ))}
            </div>
          </>
        )}
        {/* Hourly */}
        {hours.map(hour => {
          const hourEvents = timedEvents.filter(e => {
            const start = new Date(e.startDate);
            return start.getHours() === hour || (new Date(e.startDate) <= new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate(), hour, 59, 59) && new Date(e.endDate) >= new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate(), hour, 0, 0));
          });
          return (
            <React.Fragment key={hour}>
              <div style={{ padding: '6px 8px', fontSize: 11, color: 'var(--slate-500)', textAlign: 'right', borderTop: '1px solid rgba(255,255,255,0.04)' }}>
                {String(hour).padStart(2, '0')}:00
              </div>
              <div style={{ minHeight: 42, borderTop: '1px solid rgba(255,255,255,0.04)', padding: '2px 6px' }}>
                {hourEvents.map((evt, ei) => (
                  <div
                    key={ei}
                    onClick={() => setSelectedEvent(evt)}
                    style={{
                      fontSize: 12, padding: '4px 10px', marginBottom: 3, borderRadius: 6, cursor: 'pointer',
                      background: (evt.color || evt.categoryColor || '#3B82F6') + '18',
                      borderLeft: `3px solid ${evt.color || evt.categoryColor || '#3B82F6'}`,
                      color: 'var(--slate-200)', display: 'flex', alignItems: 'center', gap: 6,
                    }}
                  >
                    <span style={{ fontSize: 10, color: 'var(--slate-400)', minWidth: 42 }}>{formatTime(evt.startDate)}</span>
                    <span>{evt.title}</span>
                    {evt.meetingRoomName && <span style={{ fontSize: 10, color: 'var(--slate-500)' }}>📍 {evt.meetingRoomName}</span>}
                  </div>
                ))}
              </div>
            </React.Fragment>
          );
        })}
      </div>
    );
  };

  // ========== Event Detail Panel ==========
  const renderEventDetail = () => {
    if (!selectedEvent) return null;
    const evt = selectedEvent;
    const color = evt.color || evt.categoryColor || '#3B82F6';
     return (
       <div style={{
         position: 'fixed', top: 0, right: 0, width: 380, height: '100vh', zIndex: 100,
         background: 'var(--navy-900)', borderLeft: '1px solid rgba(255,255,255,0.08)',
         boxShadow: '-4px 0 24px rgba(0,0,0,0.4)', overflowY: 'auto', padding: 0,
       }}>
        <div style={{ padding: '20px 24px', borderBottom: `3px solid ${color}`, background: 'rgba(255,255,255,0.03)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <span style={{
              fontSize: 10, padding: '2px 8px', borderRadius: 4,
              background: color + '22', color, fontWeight: 600,
            }}>
              {evt.categoryName || evt.categoryCode || evt.eventType}
            </span>
            <button onClick={() => setSelectedEvent(null)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-400)' }}>
              <X size={18} />
            </button>
          </div>
          <h3 style={{ fontSize: 18, fontWeight: 700, color: 'var(--slate-100)', margin: 0 }}>{evt.title}</h3>
        </div>
        <div style={{ padding: '16px 24px' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'var(--slate-300)' }}>
              <Clock size={14} />
              <span>
                {evt.isAllDay
                  ? `${new Date(evt.startDate).toLocaleDateString('zh-TW')} (全天)`
                  : `${new Date(evt.startDate).toLocaleString('zh-TW')} - ${new Date(evt.endDate).toLocaleString('zh-TW')}`
                }
              </span>
            </div>
            {evt.location && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'var(--slate-300)' }}>
                <MapPin size={14} /><span>{evt.location}</span>
              </div>
            )}
            {evt.meetingRoomName && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'var(--slate-300)' }}>
                <Users size={14} /><span>會議室: {evt.meetingRoomName}</span>
              </div>
            )}
            {evt.description && (
              <div style={{ fontSize: 13, color: 'var(--slate-300)', lineHeight: 1.6, background: 'rgba(255,255,255,0.03)', padding: 10, borderRadius: 6 }}>
                {evt.description}
              </div>
            )}
            {evt.sourceModule && evt.sourceModule !== 'manual' && (
              <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>
                來源: {evt.sourceModule} {evt.sourceId && `(${evt.sourceId.slice(0, 8)}…)`}
              </div>
            )}
            <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
              {evt.sourceModule === 'manual' && (
                <>
                  <button
                    className="btn btn-primary"
                    style={{ fontSize: 12, padding: '6px 14px' }}
                    onClick={() => { setEditingEvent(evt); setSelectedEvent(null); setShowForm(true); }}
                  >
                    <Edit2 size={13} style={{ marginRight: 4 }} />編輯
                  </button>
                  <button
                    className="btn"
                    style={{ fontSize: 12, padding: '6px 14px', color: 'var(--crimson-400)', borderColor: 'var(--crimson-400)' }}
                    onClick={() => {
                      if (confirm('確定刪除此事件？')) {
                        deleteEvent.mutate(evt.id, { onSuccess: () => setSelectedEvent(null) });
                      }
                    }}
                  >
                    <Trash2 size={13} style={{ marginRight: 4 }} />刪除
                  </button>
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    );
  };

  // ========== Event Form Modal ==========
  const createEvent = useCreateCalendarEvent();
  const updateEvent = useUpdateCalendarEvent();
  const deleteEvent = useDeleteCalendarEvent();
  const createCategory = useCreateCalendarCategory();
  const createRoom = useCreateMeetingRoom();

  const renderEventForm = () => {
    if (!showForm) return null;
    const isEdit = !!editingEvent;

    const handleSubmit = async (e: React.FormEvent) => {
      e.preventDefault();
      const startDateStr = form.startDate as string;
      const endDateStr = form.endDate as string;
      if (startDateStr && endDateStr && new Date(endDateStr) <= new Date(startDateStr)) {
        alert('結束時間必須晚於開始時間');
        return;
      }
      const payload = {
        ...form,
        eventType: 'company',
        status: 'confirmed',
        startDate: new Date(form.startDate as string).toISOString(),
        endDate: new Date(form.endDate as string).toISOString(),
        meetingRoomId: form.meetingRoomId || undefined,
      };
      if (isEdit && editingEvent) {
        await updateEvent.mutateAsync({ id: editingEvent.id, data: payload });
      } else {
        await createEvent.mutateAsync(payload);
      }
      setShowForm(false);
      setEditingEvent(null);
    };

    const fieldStyle: React.CSSProperties = {
      width: '100%', padding: '8px 12px', borderRadius: 6,
      border: '1px solid rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.05)',
      color: 'var(--slate-200)', fontSize: 13,
    };

    return (
      <div style={{ position: 'fixed', inset: 0, zIndex: 200, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div className="glass-card" style={{ width: 520, maxHeight: '85vh', overflowY: 'auto', padding: 0 }}>
          <div style={{ padding: '16px 24px', borderBottom: '1px solid rgba(255,255,255,0.08)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, fontSize: 16, fontWeight: 700 }}>{isEdit ? '編輯事件' : '新增事件'}</h3>
            <button onClick={() => { setShowForm(false); setEditingEvent(null); }} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-400)' }}>
              <X size={18} />
            </button>
          </div>
          <form onSubmit={handleSubmit} style={{ padding: '16px 24px', display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>標題 *</label>
              <input style={fieldStyle} value={form.title as string} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} required placeholder="輸入事件標題" />
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div>
                <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>分類</label>
                <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                  <select style={{ ...fieldStyle, flex: 1 }} value={form.categoryCode as string} onChange={e => setForm(f => ({ ...f, categoryCode: e.target.value }))}>
                    {categories.map(c => <option key={c.code} value={c.code}>{c.name}</option>)}
                  </select>
                  <button type="button" onClick={() => { setShowAddCategory(!showAddCategory); setShowAddRoom(false); }}
                    style={{ width: 28, height: 32, borderRadius: 6, border: '1px solid rgba(59,130,246,0.3)', background: 'rgba(59,130,246,0.1)', color: 'var(--cobalt-400)', cursor: 'pointer', fontSize: 16, fontWeight: 700, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}
                    title="新增分類">＋</button>
                </div>
                {showAddCategory && (
                  <div style={{ marginTop: 8, padding: 10, borderRadius: 6, background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.06)', display: 'flex', flexDirection: 'column', gap: 8 }}>
                    <input style={{ ...fieldStyle, fontSize: 12 }} placeholder="分類名稱（例：技術討論）" value={newCategory.name}
                      onChange={e => { const name = e.target.value; setNewCategory(n => ({ ...n, name, code: name.toLowerCase().replace(/[^a-z0-9\u4e00-\u9fff]/g, '').slice(0, 20) || n.code })); }} />
                    <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                      <input style={{ ...fieldStyle, fontSize: 12, flex: 1 }} placeholder="代碼（自動產生）" value={newCategory.code}
                        onChange={e => setNewCategory(n => ({ ...n, code: e.target.value }))} />
                      <input type="color" value={newCategory.color} onChange={e => setNewCategory(n => ({ ...n, color: e.target.value }))}
                        style={{ width: 32, height: 32, border: 'none', borderRadius: 4, cursor: 'pointer', background: 'none' }} />
                    </div>
                    <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                      <button type="button" className="btn" style={{ fontSize: 11, padding: '4px 10px' }}
                        onClick={() => { setShowAddCategory(false); setNewCategory({ name: '', code: '', color: '#3B82F6' }); }}>取消</button>
                      <button type="button" className="btn btn-primary" style={{ fontSize: 11, padding: '4px 10px' }}
                        disabled={!newCategory.name || createCategory.isPending}
                        onClick={async () => {
                          const res = await createCategory.mutateAsync({ code: newCategory.code || newCategory.name, name: newCategory.name, color: newCategory.color, sortOrder: categories.length + 1 });
                          if ((res as { data?: { code?: string } })?.data?.code) {
                            setForm(f => ({ ...f, categoryCode: (res as { data: { code: string } }).data.code }));
                          }
                          setShowAddCategory(false);
                          setNewCategory({ name: '', code: '', color: '#3B82F6' });
                        }}>{createCategory.isPending ? '儲存中...' : '儲存'}</button>
                    </div>
                  </div>
                )}
              </div>
              <div>
                <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>可見範圍</label>
                <select style={fieldStyle} value={form.visibility as string} onChange={e => setForm(f => ({ ...f, visibility: e.target.value }))}>
                  <option value="public">公開</option>
                  <option value="department">部門內</option>
                  <option value="private">僅自己</option>
                </select>
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div>
                <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>開始時間 *</label>
                <input type="datetime-local" style={fieldStyle} value={form.startDate as string} onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))} required />
              </div>
              <div>
                <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>結束時間 *</label>
                <input type="datetime-local" style={fieldStyle} value={form.endDate as string} onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))} required />
              </div>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input type="checkbox" id="isAllDay" checked={form.isAllDay as boolean} onChange={e => setForm(f => ({ ...f, isAllDay: e.target.checked }))} />
              <label htmlFor="isAllDay" style={{ fontSize: 13, color: 'var(--slate-300)' }}>全天事件</label>
            </div>
            <div>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>地點</label>
              <input style={fieldStyle} value={form.location as string} onChange={e => setForm(f => ({ ...f, location: e.target.value }))} placeholder="輸入地點" />
            </div>
            <div>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>會議室</label>
              <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                <select style={{ ...fieldStyle, flex: 1 }} value={form.meetingRoomId as string} onChange={e => setForm(f => ({ ...f, meetingRoomId: e.target.value }))}>
                  <option value="">不指定會議室</option>
                  {(meetingRooms as Array<{ id: string; roomName: string; capacity: number }>).map(r => (
                    <option key={r.id} value={r.id}>{r.roomName} ({r.capacity}人)</option>
                  ))}
                </select>
                <button type="button" onClick={() => { setShowAddRoom(!showAddRoom); setShowAddCategory(false); }}
                  style={{ width: 28, height: 32, borderRadius: 6, border: '1px solid rgba(59,130,246,0.3)', background: 'rgba(59,130,246,0.1)', color: 'var(--cobalt-400)', cursor: 'pointer', fontSize: 16, fontWeight: 700, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}
                  title="新增會議室">＋</button>
              </div>
              {showAddRoom && (
                <div style={{ marginTop: 8, padding: 10, borderRadius: 6, background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.06)', display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <input style={{ ...fieldStyle, fontSize: 12 }} placeholder="會議室名稱（例：會議室 D）" value={newRoom.roomName}
                    onChange={e => setNewRoom(r => ({ ...r, roomName: e.target.value }))} />
                  <div style={{ display: 'flex', gap: 6 }}>
                    <input style={{ ...fieldStyle, fontSize: 12, flex: 1 }} placeholder="位置（例：4F）" value={newRoom.location}
                      onChange={e => setNewRoom(r => ({ ...r, location: e.target.value }))} />
                    <input type="number" min={1} max={100} style={{ ...fieldStyle, fontSize: 12, width: 80 }} placeholder="容量"
                      value={newRoom.capacity} onChange={e => setNewRoom(r => ({ ...r, capacity: parseInt(e.target.value) || 1 }))} />
                  </div>
                  <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                    <button type="button" className="btn" style={{ fontSize: 11, padding: '4px 10px' }}
                      onClick={() => { setShowAddRoom(false); setNewRoom({ roomName: '', location: '', capacity: 8 }); }}>取消</button>
                    <button type="button" className="btn btn-primary" style={{ fontSize: 11, padding: '4px 10px' }}
                      disabled={!newRoom.roomName || createRoom.isPending}
                      onClick={async () => {
                        const res = await createRoom.mutateAsync({ roomName: newRoom.roomName, location: newRoom.location || undefined, capacity: newRoom.capacity });
                        if ((res as { data?: { id?: string } })?.data?.id) {
                          setForm(f => ({ ...f, meetingRoomId: (res as { data: { id: string } }).data.id }));
                        }
                        setShowAddRoom(false);
                        setNewRoom({ roomName: '', location: '', capacity: 8 });
                      }}>{createRoom.isPending ? '儲存中...' : '儲存'}</button>
                  </div>
                </div>
              )}
            </div>
            <div>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>重複規則</label>
              <select style={fieldStyle} value={form.recurrenceRule as string} onChange={e => setForm(f => ({ ...f, recurrenceRule: e.target.value }))}>
                <option value="">不重複</option>
                <option value="FREQ=DAILY">每日</option>
                <option value="FREQ=WEEKLY">每週</option>
                <option value="FREQ=MONTHLY">每月</option>
              </select>
            </div>
            <div>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>參與者</label>
              <input style={fieldStyle} value={form.participants as string} onChange={e => setForm(f => ({ ...f, participants: e.target.value }))} placeholder="輸入參與者" />
            </div>
            <div>
              <label style={{ display: 'block', fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 4 }}>描述</label>
              <textarea style={{ ...fieldStyle, minHeight: 60, resize: 'vertical' as const }} value={form.description as string} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="輸入事件描述" />
            </div>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', paddingTop: 8 }}>
              <button type="button" className="btn" style={{ fontSize: 12, padding: '8px 16px' }} onClick={() => { setShowForm(false); setEditingEvent(null); }}>取消</button>
              <button type="submit" className="btn btn-primary" style={{ fontSize: 12, padding: '8px 16px' }} disabled={createEvent.isPending || updateEvent.isPending}>
                {(createEvent.isPending || updateEvent.isPending) ? <Loader2 size={14} className="spin" /> : (isEdit ? '更新' : '建立')}
              </button>
            </div>
          </form>
        </div>
      </div>
    );
  };

  // ========== Main Render ==========
  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <CalendarDays size={22} style={{ color: 'var(--cobalt-400)' }} />
          <h2 style={{ margin: 0, fontSize: 20, fontWeight: 700 }}>公司行事曆</h2>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-primary" style={{ fontSize: 12, padding: '7px 14px' }} onClick={() => { setEditingEvent(null); setShowForm(true); }}>
            <Plus size={14} style={{ marginRight: 4 }} />新增事件
          </button>
          <button className="btn" style={{ fontSize: 12, padding: '7px 14px' }} onClick={handleExportIcs}>
            <Download size={14} style={{ marginRight: 4 }} />匯出 ICS
          </button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '220px 1fr', gap: 20 }}>
        {/* Left Sidebar */}
        <div>
          {/* Navigation */}
          <div className="glass-card" style={{ padding: 16, marginBottom: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
              <button onClick={() => navigate(-1)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-300)' }}><ChevronLeft size={18} /></button>
              <span style={{ fontSize: 14, fontWeight: 600, color: 'var(--slate-200)' }}>{headerTitle}</span>
              <button onClick={() => navigate(1)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-300)' }}><ChevronRight size={18} /></button>
            </div>
            <button className="btn" style={{ width: '100%', fontSize: 11, padding: '5px 0' }} onClick={goToToday}>今天</button>
          </div>

          {/* View Mode Switcher */}
          <div className="glass-card" style={{ padding: 12, marginBottom: 16 }}>
            {(['month', 'week', 'day'] as ViewMode[]).map(vm => (
              <button
                key={vm}
                onClick={() => setViewMode(vm)}
                style={{
                  display: 'block', width: '100%', textAlign: 'left', padding: '6px 10px', marginBottom: 2, borderRadius: 6,
                  background: viewMode === vm ? 'rgba(59,130,246,0.15)' : 'transparent',
                  color: viewMode === vm ? 'var(--cobalt-300)' : 'var(--slate-400)',
                  border: 'none', cursor: 'pointer', fontSize: 12, fontWeight: viewMode === vm ? 600 : 400,
                }}
              >
                {vm === 'month' ? '📅 月視圖' : vm === 'week' ? '📆 週視圖' : '📋 日視圖'}
              </button>
            ))}
          </div>

          {/* Category Filter */}
          <div className="glass-card" style={{ padding: 12 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 10, fontSize: 12, fontWeight: 600, color: 'var(--slate-400)' }}>
              <Filter size={13} />分類篩選
            </div>
            {categories.map(c => (
              <label key={c.code} style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6, cursor: 'pointer', fontSize: 12, color: 'var(--slate-300)' }}>
                <input
                  type="checkbox"
                  checked={activeCategories.size === 0 || activeCategories.has(c.code)}
                  onChange={() => toggleCategory(c.code)}
                  style={{ accentColor: c.color }}
                />
                <span style={{ width: 8, height: 8, borderRadius: '50%', background: c.color, display: 'inline-block' }} />
                {categoryIconMap[c.code]} {c.name}
              </label>
            ))}
            {activeCategories.size > 0 && (
              <button
                onClick={() => setActiveCategories(new Set())}
                style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 11, color: 'var(--cobalt-400)', marginTop: 4 }}
              >
                重置篩選
              </button>
            )}
          </div>
        </div>

        {/* Main Calendar Area */}
        <div>
          {isLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 300, color: 'var(--slate-400)' }}>
              <Loader2 size={24} className="spin" style={{ marginRight: 8 }} /> 載入行事曆...
            </div>
          ) : (
            <>
              {viewMode === 'month' && renderMonthView()}
              {viewMode === 'week' && renderWeekView()}
              {viewMode === 'day' && renderDayView()}
            </>
          )}

          {/* Today's Events Summary */}
          {viewMode === 'month' && (() => {
            const todayEvents = getEventsForDay(events, today);
            if (todayEvents.length === 0) return null;
            return (
              <div className="glass-card" style={{ marginTop: 16, padding: 16 }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-300)', marginBottom: 10 }}>
                  今日事件 ({todayEvents.length})
                </div>
                {todayEvents.map((evt, i) => {
                  const color = evt.color || evt.categoryColor || '#3B82F6';
                  return (
                    <div
                      key={i}
                      onClick={() => setSelectedEvent(evt)}
                      style={{
                        display: 'flex', alignItems: 'center', gap: 10, padding: '8px 12px', marginBottom: 4, borderRadius: 6,
                        cursor: 'pointer', background: 'rgba(255,255,255,0.02)',
                        borderLeft: `3px solid ${color}`,
                      }}
                    >
                      <span style={{
                        fontSize: 10, padding: '1px 6px', borderRadius: 3,
                        background: color + '22', color, fontWeight: 600, minWidth: 36, textAlign: 'center',
                      }}>
                        {evt.isAllDay ? '全天' : formatTime(evt.startDate)}
                      </span>
                      <span style={{ fontSize: 13, color: 'var(--slate-200)' }}>{evt.title}</span>
                    </div>
                  );
                })}
              </div>
            );
          })()}
        </div>
      </div>

      {/* Event Detail Panel */}
      {renderEventDetail()}

      {/* Event Form Modal */}
      {renderEventForm()}

      {/* Conflict Warning Badge */}
      {events.some(e => e.sourceModule === 'holiday') && (
        <div style={{
          position: 'fixed', bottom: 20, right: 20, zIndex: 50,
          background: 'rgba(249,115,22,0.15)', border: '1px solid rgba(249,115,22,0.3)',
          borderRadius: 8, padding: '8px 14px', display: 'flex', alignItems: 'center', gap: 8,
          fontSize: 12, color: 'var(--orange-300)',
        }}>
          <AlertTriangle size={14} />
          此範圍內含有 {events.filter(e => e.sourceModule === 'holiday').length} 個假日
        </div>
      )}
    </div>
  );
};
