// ===========================
// ERP 核心型別定義
// ===========================

export type ModuleId =
  | 'dashboard'
  | 'sales'
  | 'procurement'
  | 'inventory'
  | 'manufacturing'
  | 'hr'
  | 'finance'
  | 'assets'
  | 'budget'
  | 'quality'
  | 'rd'
  | 'esg'
  | 'ai'
  | 'agentflows'
  | 'collaboration'
  | 'calendar'
  | 'uom'
  | 'org'
  | 'costcenter'
  | 'substitute'
  | 'mrp'
  | 'aps'
  | 'engineering'
  | 'mps'
  | 'tpm'
  | 'barcode'
  | 'kpi'
  | 'supplierportal'
  | 'aischeduling'
  | 'ediexport'
  | 'aidecision'
  | 'profile'
  | 'suppliers'
  | 'rccp'
  | 'systemsettings'
  | 'actual_costing';

export interface NavItem {
  id: ModuleId;
  label: string;
  icon: string;
  badge?: number;
  children?: NavItem[];
}

export interface KPICard {
  id: string;
  title: string;
  value: string | number;
  unit?: string;
  trend?: number; // 百分比變化
  trendLabel?: string;
  status: 'positive' | 'negative' | 'neutral' | 'warning';
  icon: string;
  detail?: string;
}

export interface Alert {
  id: string;
  type: 'danger' | 'warning' | 'info' | 'success';
  title: string;
  message: string;
  time: string;
  module: string;
  read: boolean;
}

export interface WorkOrder {
  id: string;
  woNumber: string;
  productName: string;
  sku: string;
  qty: number;
  completedQty: number;
  status: 'pending' | 'released' | 'in_progress' | 'completed' | 'closed';
  priority: 'urgent' | 'high' | 'normal' | 'low';
  startDate: string;
  dueDate: string;
  workCenter: string;
  progress: number;
}

export interface SalesOrder {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  customerPoNumber?: string;
  orderDate: string;
  customerDueDate?: string;
  confirmedDeliveryDate?: string;
  expectedDeliveryDate?: string;
  paymentTerms?: string;
  tradeTerms?: string;
  incoterms?: string;
  currency: string;
  subtotal: number;
  taxAmount: number;
  totalAmount: number;
  freight?: number;
  advanceDepositAmount?: number;
  status: string;
  orderType?: string;
  productionGroupCode?: string;
  salesChannel?: string;
  hsCode?: string;
  shippingMethod?: string;
  salespersonId?: string;
  salespersonName?: string;
  departmentId?: string;
  departmentName?: string;
  orderRemarks?: string;
  creditChecked: boolean;
  items: SalesOrderItem[];
}

export interface SalesOrderItem {
  id: string;
  lineNumber: number;
  productId: string;
  productName?: string;
  skuId?: string;
  skuNumber?: string;
  customerItemCode?: string;
  quantity: number;
  unitPrice: number;
  discountRate: number;
  subtotal: number;
  deliveredQty: number;
  backorderQty: number;
  status: string;
  warehouseId?: string;
  warehouseName?: string;
  taxCode?: string;
  taxRate?: string;
  bomId?: string;
  bomVersion?: string;
  routingId?: string;
  workCenterId?: string;
  workCenterName?: string;
  needManufacturing: boolean;
  needOutsourcing: boolean;
  plannedStartDate?: string;
  plannedFinishDate?: string;
  lineRemarks?: string;
  productionStatus?: string;
}

export interface PurchaseOrder {
  id: string;
  poNumber: string;
  supplier: string;
  amount: number;
  status: 'pending' | 'confirmed' | 'partial' | 'received' | 'closed';
  orderDate: string;
  expectedDate: string;
}

export interface InventoryItem {
  id: string;
  sku: string;
  name: string;
  category: string;
  qty: number;
  unit: string;
  safetyStock: number;
  location: string;
  status: 'normal' | 'low' | 'critical' | 'overstock';
  lastUpdated: string;
}

export interface Employee {
  id: string;
  empNo: string;
  name: string;
  department: string;
  position: string;
  status: 'active' | 'leave' | 'inactive';
  attendanceToday: 'present' | 'absent' | 'late' | 'leave';
  hireDate: string;
}

export interface ChartDataPoint {
  label: string;
  value: number;
  value2?: number;
  value3?: number;
}

export interface Notification {
  id: string;
  type: 'system' | 'approval' | 'alert' | 'info';
  title: string;
  body: string;
  time: string;
  read: boolean;
  actionUrl?: string;
}

export interface User {
  id: string;
  name: string;
  role: string;
  department: string;
  avatar?: string;
  permissions: ModuleId[];
}

export interface QualityInspection {
  id: string;
  type: 'IQC' | 'IPQC' | 'OQC';
  lotNo: string;
  product: string;
  sampleSize: number;
  defectCount: number;
  result: 'pass' | 'fail' | 'conditional';
  inspector: string;
  inspectedAt: string;
}

// ========== AI Agent Monitoring ==========
export interface AgentHealthScore {
  agentType: string;
  score: number;
  status: 'healthy' | 'degraded' | 'critical';
  uptime: number;
  lastCheck: string;
}

export interface AgentMetricsSummary {
  totalRequests: number;
  successRate: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
  errorRate: number;
  tokenUsage24h: number;
  activeAgents: number;
  healthScores: AgentHealthScore[];
}

export interface AgentLatencyDataPoint {
  timestamp: string;
  p50: number;
  p95: number;
  p99: number;
}

export interface AgentErrorDataPoint {
  timestamp: string;
  count: number;
  rate: number;
}

export interface AgentQpsDataPoint {
  timestamp: string;
  qps: number;
}

export interface AgentFlowStep {
  id: string;
  name: string;
  status: 'pending' | 'running' | 'success' | 'failed' | 'skipped';
  startTime?: string;
  endTime?: string;
  duration?: number;
  output?: string;
  error?: string;
}

export interface AgentFlowExecution {
  id: string;
  flowType: 'anomaly_dispatch' | 'customer_inquiry' | 'inventory_replenishment';
  flowName: string;
  status: 'running' | 'success' | 'failed';
  triggerSource: string;
  startTime: string;
  endTime?: string;
  duration?: number;
  steps: AgentFlowStep[];
  businessImpact?: string;
}

export interface AgentTopologyNode {
  id: string;
  name: string;
  type: 'agent' | 'erp' | 'mes' | 'line' | 'sensor';
  status: 'online' | 'offline' | 'degraded';
  connections: string[];
}
