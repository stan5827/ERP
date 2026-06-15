import React, { useState, useEffect } from 'react';
import { apsApi, mrpApi } from '../api/api';
import type { ProductionScheduleDto, ScheduleTaskDto, CapacityLoadDto, GanttTaskDto, MrpPlannedOrderDto } from '../api/api';
import { Play, RefreshCw, AlertTriangle, Settings, Package, Calendar, ArrowLeft } from 'lucide-react';
import './ApsScheduling.css';

interface ApsSchedulingProps {
  onNavigate?: (module: string) => void;
}

export const ApsScheduling: React.FC<ApsSchedulingProps> = ({ onNavigate }) => {
  const [schedules, setSchedules] = useState<ProductionScheduleDto[]>([]);
  const [selectedSchedule, setSelectedSchedule] = useState<ProductionScheduleDto | null>(null);
  const [capacityLoads, setCapacityLoads] = useState<CapacityLoadDto[]>([]);
  const [constraints, setConstraints] = useState<any[]>([]);
  const [ganttData, setGanttData] = useState<GanttTaskDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [view, setView] = useState<'list' | 'gantt' | 'capacity' | 'mrp'>('list');
  const [showConstraintModal, setShowConstraintModal] = useState(false);
  const [selectedConstraint, setSelectedConstraint] = useState<any>(null);
  const [resolving, setResolving] = useState(false);
  const [mrpPlannedOrders, setMrpPlannedOrders] = useState<MrpPlannedOrderDto[]>([]);
  const [loadingMrpOrders, setLoadingMrpOrders] = useState(false);

  useEffect(() => {
    loadSchedules();
  }, []);

  const loadSchedules = async () => {
    setLoading(true);
    try {
      const response = await apsApi.schedules({ pageSize: 20 });
      if (response.data?.success) {
        setSchedules(response.data.data || []);
        if (response.data.data?.length && !selectedSchedule) {
          handleSelectSchedule(response.data.data[0]);
        }
      }
    } catch (error) {
      console.error('Failed to load schedules:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectSchedule = async (schedule: ProductionScheduleDto) => {
    setSelectedSchedule(schedule);
    try {
      const [loadRes, constraintRes] = await Promise.all([
        apsApi.capacityLoads(schedule.id),
        apsApi.constraints(schedule.id)
      ]);
      if (loadRes.data?.success) setCapacityLoads(loadRes.data.data || []);
      if (constraintRes.data?.success) setConstraints(constraintRes.data.data || []);
    } catch (error) {
      console.error('Failed to load schedule details:', error);
    }
  };

  const handleGenerateSchedule = async () => {
    setGenerating(true);
    try {
      const now = new Date();
      const horizonEnd = new Date(now.getTime() + 14 * 24 * 60 * 60 * 1000); // 14 days
      
      const response = await apsApi.generateSchedule({
        horizonStart: now.toISOString(),
        horizonEnd: horizonEnd.toISOString(),
        scheduleType: 'firm'
      });
      
      if (response.data?.success) {
        alert('排程生成完成！');
        loadSchedules();
      }
    } catch (error) {
      console.error('Failed to generate schedule:', error);
      alert('排程生成失敗');
    } finally {
      setGenerating(false);
    }
  };

  const handleOptimize = async () => {
    if (!selectedSchedule) return;
    
    try {
      const response = await apsApi.optimizeSchedule(selectedSchedule.id, {
        primaryGoal: 'meet_deadlines',
        respectPriority: true,
        allowRescheduling: true
      });
      
      if (response.data?.success) {
        alert('排程優化完成！');
        handleSelectSchedule(response.data.data!);
      }
    } catch (error) {
      console.error('Failed to optimize schedule:', error);
      alert('排程優化失敗');
    }
  };

  const [draggingTask, setDraggingTask] = useState<string | null>(null);

  const loadGanttData = () => {
    if (!selectedSchedule) return;
    
    const transformed: GanttTaskDto[] = selectedSchedule.tasks.map(t => ({
      id: t.id,
      taskName: `OP${t.operationSeq} (QTY: ${t.quantity})`,
      workOrderId: t.workOrderId,
      woNumber: t.workOrderId.substring(0, 8),
      workCenterId: t.workCenterId,
      workCenterName: t.workCenterName || 'Unknown Machine',
      start: t.plannedStart,
      end: t.plannedEnd,
      progress: t.status === 'completed' ? 100 : (t.status === 'active' ? 50 : 0),
      status: t.status,
      priority: t.priority,
      setupHours: t.setupHours,
      runHours: t.runHours,
      totalHours: t.totalHours,
      quantity: t.quantity
    }));

    setGanttData(transformed);
  };

  const getGroupedGanttTasks = () => {
    return ganttData.reduce((acc, task) => {
      const wc = task.workCenterName || 'Other';
      if (!acc[wc]) acc[wc] = [];
      acc[wc].push(task);
      return acc;
    }, {} as Record<string, GanttTaskDto[]>);
  };

  const generateTimelineDates = (start: string, end: string) => {
    const dates = [];
    let curr = new Date(start);
    const endD = new Date(end);
    curr.setHours(0,0,0,0);
    // ensure we add till end to show the full last day grid
    while (curr <= endD) {
      dates.push(new Date(curr));
      curr.setDate(curr.getDate() + 1);
    }
    return dates;
  };

  const loadMrpPlannedOrders = async () => {
    setLoadingMrpOrders(true);
    try {
      const runsResponse = await mrpApi.runs({ pageSize: 1 });
      if (runsResponse.data?.success && runsResponse.data.data?.length) {
        const latestRun = runsResponse.data.data[0];
        const ordersResponse = await mrpApi.plannedOrders(latestRun.id, { orderType: 'work_order', status: 'confirmed' });
        if (ordersResponse.data?.success) {
          setMrpPlannedOrders(ordersResponse.data.data || []);
        }
      }
    } catch (error) {
      console.error('Failed to load MRP planned orders:', error);
    } finally {
      setLoadingMrpOrders(false);
    }
  };

  const handleAcceptWorkOrder = async (orderId: string) => {
    try {
      const response = await apsApi.acceptWorkOrder(selectedSchedule!.id, { workOrderId: orderId });
      if (response.data?.success) {
        alert('已接受工單到排程！');
        handleSelectSchedule(selectedSchedule!);
      }
    } catch (error) {
      console.error('Failed to accept work order:', error);
      alert('接受工單失敗');
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'draft': return '草稿';
      case 'active': return '啟用';
      case 'optimized': return '已優化';
      case 'completed': return '已完成';
      default: return status;
    }
  };

  const getLoadStatusClass = (status: string) => {
    switch (status) {
      case 'overloaded': return 'danger';
      case 'underutilized': return 'warning';
      default: return 'success';
    }
  };

  const getLoadStatusColor = (percentage: number) => {
    if (percentage > 100) return '#EF4444';
    if (percentage > 85) return '#F59E0B';
    if (percentage < 60) return '#3B82F6';
    return '#10B981';
  };

  const handleResolveConstraint = async () => {
    if (!selectedSchedule || !selectedConstraint) return;
    
    setResolving(true);
    try {
      const response = await apsApi.resolveConstraint(
        selectedSchedule.id,
        selectedConstraint.id,
        { resolutionNotes: (document.getElementById('resolutionNotes') as HTMLTextAreaElement)?.value || '' }
      );
      
      if (response.data?.success) {
        alert('約束已解決！');
        setShowConstraintModal(false);
        setSelectedConstraint(null);
        handleSelectSchedule(selectedSchedule);
      }
    } catch (error) {
      console.error('Failed to resolve constraint:', error);
      alert('解決約束失敗');
    } finally {
      setResolving(false);
    }
  };

  const openConstraintModal = (constraint: any) => {
    setSelectedConstraint(constraint);
    setShowConstraintModal(true);
  };

  const handleDragStart = (e: React.DragEvent<HTMLDivElement>, taskId: string) => {
    e.dataTransfer.setData('taskId', taskId);
    e.dataTransfer.effectAllowed = 'move';
    setDraggingTask(taskId);
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDrop = async (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setDraggingTask(null);
    
    if (!selectedSchedule) return;
    const taskId = e.dataTransfer.getData('taskId');
    const task = ganttData.find(t => t.id === taskId);
    if (!task) return;

    // Calculate drop percentage relative to the timeline row
    const rect = e.currentTarget.getBoundingClientRect();
    const dropX = Math.max(0, e.clientX - rect.left);
    const dropPercentage = dropX / rect.width;

    const { gridStart, gridEnd } = getGridRange(selectedSchedule.horizonStart, selectedSchedule.horizonEnd);
    const totalDuration = gridEnd - gridStart;

    const newStartTime = gridStart + totalDuration * dropPercentage;
    const taskDuration = new Date(task.end).getTime() - new Date(task.start).getTime();
    const newEndTime = newStartTime + taskDuration;

    // Optimistic UI update
    const previousGanttData = [...ganttData];
    const newStartStr = new Date(newStartTime).toISOString();
    const newEndStr = new Date(newEndTime).toISOString();

    setGanttData(prev => prev.map(t => 
      t.id === taskId ? { ...t, start: newStartStr, end: newEndStr } : t
    ));

    try {
      const response = await apsApi.rescheduleTask(taskId, {
        newStart: newStartStr,
        newEnd: newEndStr
      });
      if (response.data && !response.data.success) {
        throw new Error('Server returned false');
      }
    } catch (error) {
      console.error('Failed to reschedule task:', error);
      alert('排程更新失敗，伺服器發生錯誤。');
      // Revert optimistic update
      setGanttData(previousGanttData);
    }
  };

  if (loading) {
    return (
      <div className="aps-scheduling">
        <div className="loading-container">
          <div className="spinner"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="aps-scheduling">
      <div className="page-header">
        <h1>APS 有限產能排程</h1>
        <div className="header-actions">
          <button 
            className="btn btn-primary" 
            onClick={handleGenerateSchedule}
            disabled={generating}
          >
            {generating ? <><RefreshCw size={18} className="spin" /> 生成中...</> : <><Play size={18} /> 生成排程</>}
          </button>
        </div>
      </div>

      <div className="aps-content">
        <div className="schedules-panel">
          <h3>排程列表</h3>
          <div className="schedules-list">
            {schedules.length === 0 ? (
              <div className="empty-state">
                <Calendar />
                <p>尚無排程記錄</p>
              </div>
            ) : (
              schedules.map(schedule => (
                <div 
                  key={schedule.id} 
                  className={`schedule-item ${selectedSchedule?.id === schedule.id ? 'selected' : ''}`}
                  onClick={() => handleSelectSchedule(schedule)}
                >
                  <div className="schedule-header">
                    <span className="schedule-number">{schedule.scheduleNumber}</span>
                    <span className={`status ${schedule.status}`}>
                      {getStatusLabel(schedule.status)}
                    </span>
                  </div>
                  <div className="schedule-details">
                    <span>任務：{schedule.totalTasks}</span>
                    <span>約束：{schedule.unresolvedConstraints}</span>
                    <span>期間：{new Date(schedule.horizonStart).toLocaleDateString()} ~ {new Date(schedule.horizonEnd).toLocaleDateString()}</span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="details-panel">
          {!selectedSchedule ? (
            <div className="empty-state">
              <Package />
              <p>請選擇一筆排程查看詳情</p>
            </div>
          ) : (
            <>
              <div className="panel-header">
                <h3>排程詳情</h3>
                <div className="view-tabs">
                  <button 
                    className={`tab ${view === 'list' ? 'active' : ''}`}
                    onClick={() => setView('list')}
                  >
                    任務列表
                  </button>
                  <button 
                    className={`tab ${view === 'capacity' ? 'active' : ''}`}
                    onClick={() => setView('capacity')}
                  >
                    產能負荷
                  </button>
                  <button 
                    className={`tab ${view === 'gantt' ? 'active' : ''}`}
                    onClick={() => { setView('gantt'); loadGanttData(); }}
                  >
                    甘特圖
                  </button>
                  <button 
                    className={`tab ${view === 'mrp' ? 'active' : ''}`}
                    onClick={() => { setView('mrp'); loadMrpPlannedOrders(); }}
                  >
                    MRP工單
                  </button>
                </div>
                <button className="btn btn-secondary" onClick={() => onNavigate?.('mrp')}>
                  <ArrowLeft size={16} /> MRP
                </button>
              </div>

              {view === 'list' && (
                <div className="tasks-table">
                  {selectedSchedule.tasks?.length === 0 ? (
                    <div className="empty-state">
                      <Package />
                      <p>無任務資料</p>
                    </div>
                  ) : (
                    <table>
                      <thead>
                        <tr>
                          <th>工單</th>
                          <th>工序</th>
                          <th>工作中心</th>
                          <th>計劃開始</th>
                          <th>MRP 最早開始</th>
                          <th>計劃結束</th>
                          <th>工時</th>
                          <th>數量</th>
                          <th>狀態</th>
                          <th>優先級</th>
                        </tr>
                      </thead>
                      <tbody>
                        {selectedSchedule.tasks.map(task => (
                          <tr key={task.id}>
                            <td>WO-xxx</td>
                            <td>OP{task.operationSeq}</td>
                            <td>{task.workCenterName}</td>
                            <td>{new Date(task.plannedStart).toLocaleString()}</td>
                            <td style={{ color: (task.mrpPlannedStart && new Date(task.plannedStart) < new Date(task.mrpPlannedStart)) ? '#EF4444' : 'inherit' }}>
                              {task.mrpPlannedStart ? new Date(task.mrpPlannedStart).toLocaleDateString() : '-'}
                            </td>
                            <td>{new Date(task.plannedEnd).toLocaleString()}</td>
                            <td>{task.totalHours.toFixed(1)}h</td>
                            <td>{task.quantity}</td>
                            <td>
                              <span className={`badge ${task.status === 'completed' ? 'success' : 'info'}`}>
                                {task.status}
                              </span>
                            </td>
                            <td>{task.priority}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              )}

              {view === 'capacity' && (
                <div className="capacity-grid">
                  {capacityLoads.map(load => (
                    <div key={load.id} className="capacity-card">
                      <div className="capacity-header">
                        <h4>{load.workCenterName}</h4>
                        <span className="date">{new Date(load.loadDate).toLocaleDateString()}</span>
                      </div>
                      <div className="capacity-gauge">
                        <div 
                          className="gauge-fill"
                          style={{ 
                            width: `${Math.min(load.loadPercentage, 100)}%`,
                            backgroundColor: getLoadStatusColor(load.loadPercentage)
                          }}
                        />
                        <span className="gauge-value">{load.loadPercentage.toFixed(1)}%</span>
                      </div>
                      <div className="capacity-details">
                        <div className="detail">
                          <span className="label">可用工時</span>
                          <span className="value">{load.availableHours.toFixed(1)}h</span>
                        </div>
                        <div className="detail">
                          <span className="label">已排程</span>
                          <span className="value">{load.scheduledHours.toFixed(1)}h</span>
                        </div>
                        <div className="detail">
                          <span className="label">狀態</span>
                          <span className={`status ${getLoadStatusClass(load.loadStatus)}`}>
                            {load.loadStatus === 'overloaded' ? '過載' : 
                             load.loadStatus === 'underutilized' ? '低負載' : '正常'}
                          </span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {view === 'gantt' && selectedSchedule && (
                <div className="gantt-container">
                  <div className="gantt-header">
                    <div className="gantt-header-title">工作中心</div>
                    <div className="gantt-timeline-header">
                      {generateTimelineDates(selectedSchedule.horizonStart, selectedSchedule.horizonEnd).map((d, i) => (
                        <div key={i} className="gantt-date-tick">
                          {d.getMonth() + 1}/{d.getDate()}
                        </div>
                      ))}
                    </div>
                  </div>
                  <div className="gantt-body">
                    <div className="gantt-timeline-grid">
                      {generateTimelineDates(selectedSchedule.horizonStart, selectedSchedule.horizonEnd).map((d, i) => (
                        <div key={i} className="gantt-grid-col" />
                      ))}
                    </div>
                    {Object.keys(getGroupedGanttTasks()).length === 0 ? (
                      <div className="empty-state" style={{ gridColumn: '1 / -1' }}>
                        <Package />
                        <p>無任務資料，請先指派或產生排程</p>
                      </div>
                    ) : (
                      Object.entries(getGroupedGanttTasks()).map(([wc, tasks]) => (
                        <div key={wc} className="gantt-row-group">
                          <div className="gantt-row-title">{wc}</div>
                          <div 
                            className="gantt-row-tasks"
                            onDragOver={handleDragOver}
                            onDrop={handleDrop}
                          >
                            {tasks.map(task => (
                              <div 
                                key={task.id} 
                                draggable
                                onDragStart={(e) => handleDragStart(e, task.id)}
                                onDragEnd={() => setDraggingTask(null)}
                                className={`gantt-task ${task.status} ${draggingTask === task.id ? 'dragging' : ''}`}
                                style={{
                                  left: `${calculateGanttPosition(task.start, selectedSchedule.horizonStart, selectedSchedule.horizonEnd)}%`,
                                  width: `${calculateGanttWidth(task.start, task.end, selectedSchedule.horizonStart, selectedSchedule.horizonEnd)}%`,
                                  cursor: 'move',
                                  opacity: draggingTask === task.id ? 0.6 : 1
                                }}
                                title={`工單: ${task.woNumber}\n任務: ${task.taskName}\n開始: ${new Date(task.start).toLocaleString()}\n結束: ${new Date(task.end).toLocaleString()}`}
                              >
                                <div className="task-content">
                                  <span className="task-name">{task.woNumber}</span>
                                </div>
                                <div 
                                  className="task-progress"
                                  style={{ width: `${task.progress}%` }}
                                />
                              </div>
                            ))}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              )}

              {view === 'mrp' && (
                <div className="mrp-orders-section">
                  <h3>MRP 確認的工單 ({mrpPlannedOrders.length})</h3>
                  {loadingMrpOrders ? (
                    <div className="loading-container">
                      <div className="spinner"></div>
                    </div>
                  ) : mrpPlannedOrders.length === 0 ? (
                    <div className="empty-state">
                      <Package />
                      <p>無待排程的 MRP 工單</p>
                      <p style={{ fontSize: '12px', color: 'var(--slate-500)' }}>請先在 MRP 中確認計劃訂單並選擇產生工單</p>
                    </div>
                  ) : (
                    <table>
                      <thead>
                        <tr>
                          <th>料號</th>
                          <th>品名</th>
                          <th>數量</th>
                          <th>計劃開始</th>
                          <th>計劃結束</th>
                          <th>操作</th>
                        </tr>
                      </thead>
                      <tbody>
                        {mrpPlannedOrders.map(order => (
                          <tr key={order.id}>
                            <td>{order.skuNumber}</td>
                            <td>{order.skuName}</td>
                            <td>{order.orderQty.toLocaleString()}</td>
                            <td>{new Date(order.plannedStartDate).toLocaleDateString()}</td>
                            <td>{new Date(order.plannedEndDate).toLocaleDateString()}</td>
                            <td>
                              <button 
                                className="btn btn-sm btn-primary"
                                onClick={() => handleAcceptWorkOrder(order.id)}
                              >
                                加入排程
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              )}

              {constraints.length > 0 && (
                <div className="constraints-section">
                  <h3>
                    <AlertTriangle size={18} /> 未解決約束 ({constraints.length})
                  </h3>
                  <div className="constraints-list">
                    {constraints.map(constraint => (
                      <div 
                        key={constraint.id} 
                        className={`constraint-item ${constraint.severity}`}
                        onClick={() => openConstraintModal(constraint)}
                      >
                        <div className="constraint-header">
                          <span className={`severity ${constraint.severity}`}>
                            {constraint.severity === 'critical' ? '嚴重' : 
                             constraint.severity === 'error' ? '錯誤' : '警告'}
                          </span>
                          <span className="type">{constraint.constraintType}</span>
                        </div>
                        <p>{constraint.description}</p>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
            )}
        </div>
      </div>

      {showConstraintModal && selectedConstraint && (
        <div className="modal-overlay" onClick={() => setShowConstraintModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>約束詳情</h3>
              <button className="close-btn" onClick={() => setShowConstraintModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>約束類型</label>
                <span>{selectedConstraint.constraintType}</span>
              </div>
              <div className="form-group">
                <label>嚴重程度</label>
                <span className={`severity ${selectedConstraint.severity}`}>
                  {selectedConstraint.severity === 'critical' ? '嚴重' : 
                   selectedConstraint.severity === 'error' ? '錯誤' : '警告'}
                </span>
              </div>
              <div className="form-group">
                <label>描述</label>
                <p>{selectedConstraint.description}</p>
              </div>
              {selectedConstraint.affectedTask && (
                <div className="form-group">
                  <label>影響任務</label>
                  <p>{selectedConstraint.affectedTask}</p>
                </div>
              )}
              {selectedConstraint.resourceName && (
                <div className="form-group">
                  <label>影響資源</label>
                  <p>{selectedConstraint.resourceName}</p>
                </div>
              )}
              <div className="form-group">
                <label>解決說明</label>
                <textarea 
                  id="resolutionNotes"
                  rows={4} 
                  placeholder="請輸入解決說明..."
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setShowConstraintModal(false)}>
                取消
              </button>
              <button 
                className="btn btn-primary" 
                onClick={handleResolveConstraint}
                disabled={resolving}
              >
                {resolving ? '處理中...' : '標記為已解決'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// Helper functions for Gantt chart
function getGridRange(horizonStart: string, horizonEnd: string) {
  const start = new Date(horizonStart);
  start.setHours(0,0,0,0);
  const end = new Date(horizonEnd);
  end.setHours(23,59,59,999);
  return { gridStart: start.getTime(), gridEnd: end.getTime() };
}

function calculateGanttPosition(start: string, horizonStart: string, horizonEnd: string): number {
  const startDate = new Date(start).getTime();
  const { gridStart, gridEnd } = getGridRange(horizonStart, horizonEnd);
  const totalDuration = gridEnd - gridStart;
  
  if (totalDuration <= 0) return 0;
  return Math.max(0, Math.min(100, ((startDate - gridStart) / totalDuration) * 100));
}

function calculateGanttWidth(start: string, end: string, horizonStart: string, horizonEnd: string): number {
  const startTime = new Date(start).getTime();
  const endTime = new Date(end).getTime();
  const { gridStart, gridEnd } = getGridRange(horizonStart, horizonEnd);
  const totalDuration = gridEnd - gridStart;
  const taskDuration = endTime - startTime;
  
  if (totalDuration <= 0) return 0;
  return Math.max(0.5, Math.min(100, (taskDuration / totalDuration) * 100));
}