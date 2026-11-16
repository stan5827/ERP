const pptxgen = require("pptxgenjs");
const fs = require("fs");

const pres = new pptxgen();
pres.layout = "LAYOUT_16x9";
pres.author = "ProMES ERP Team";
pres.title = "ERP AI Agent 監控系統";

// ── Color palette (Midnight Executive) ──
const C = {
  navy: "1E2761",
  cobalt: "2D6EF5",
  cobaltLight: "4B83F7",
  ice: "CADCFC",
  white: "FFFFFF",
  slate: "64748B",
  dark: "0F172A",
  emerald: "10B981",
  crimson: "C71F3D",
  amber: "F59E0B",
  lightBg: "F8FAFC",
};

// ── Helpers ──
function addDarkSlide() {
  const slide = pres.addSlide();
  slide.background = { color: C.navy };
  return slide;
}
function addLightSlide() {
  const slide = pres.addSlide();
  slide.background = { color: C.lightBg };
  return slide;
}
function addPageNumber(slide, num) {
  slide.addText(`${num}`, { x: 9.2, y: 5.2, w: 0.5, h: 0.3, fontSize: 10, color: C.slate, align: "right" });
}

// ══════════════════════════════════════════════
// Slide 1: Title (Dark)
// ══════════════════════════════════════════════
const s1 = addDarkSlide();
s1.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: 0.06, fill: { color: C.cobalt } });
s1.addText("ERP AI Agent", { x: 0.8, y: 1.5, w: 8.4, h: 1, fontSize: 48, fontFace: "Arial", color: C.white, bold: true });
s1.addText("監控系統方案簡報", { x: 0.8, y: 2.3, w: 8.4, h: 0.6, fontSize: 28, fontFace: "Arial", color: C.ice });
s1.addText("ProMES ERP v1.0  |  製造業資源規劃系統", { x: 0.8, y: 3.4, w: 8.4, h: 0.4, fontSize: 16, fontFace: "Arial", color: C.slate });
s1.addText("2026 年 3 月", { x: 0.8, y: 4.6, w: 4, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.slate });

// ══════════════════════════════════════════════
// Slide 2: Agenda (Light)
// ══════════════════════════════════════════════
const s2 = addLightSlide();
s2.addText("議程", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.navy, bold: true });
const agendaItems = [
  { num: "01", title: "系統架構與監控指標", desc: "技術堆疊與 Prometheus 整合" },
  { num: "02", title: "即時監控儀表板", desc: "React 內建儀表板 + Grafana" },
  { num: "03", title: "Agent Flow 自動化", desc: "產線異常、客服詢價、庫存補貨" },
  { num: "04", title: "部署與效益分析", desc: "ROI 評估與導入時程" },
];
agendaItems.forEach((item, i) => {
  const y = 1.5 + i * 0.9;
  s2.addShape(pres.shapes.RECTANGLE, { x: 0.8, y, w: 0.6, h: 0.6, fill: { color: C.cobalt }, rectRadius: 0.05 });
  s2.addText(item.num, { x: 0.8, y, w: 0.6, h: 0.6, fontSize: 18, fontFace: "Arial", color: C.white, bold: true, align: "center", valign: "middle" });
  s2.addText(item.title, { x: 1.7, y: y - 0.02, w: 7, h: 0.35, fontSize: 18, fontFace: "Arial", color: C.dark, bold: true });
  s2.addText(item.desc, { x: 1.7, y: y + 0.28, w: 7, h: 0.25, fontSize: 13, fontFace: "Arial", color: C.slate });
});
addPageNumber(s2, 2);

// ══════════════════════════════════════════════
// Slide 3: System Architecture (Light)
// ══════════════════════════════════════════════
const s3 = addLightSlide();
s3.addText("系統架構", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.navy, bold: true });

// Architecture boxes
const layers = [
  { label: "前端監控", items: ["React 19 + Recharts", "即時儀表板", "Agent Flow 追蹤"], y: 1.5, color: C.cobalt },
  { label: "後端服務", items: ["ASP.NET Core 8", "Prometheus 指標暴露", "Agent Metrics API"], y: 2.6, color: C.emerald },
  { label: "監控堆疊", items: ["Prometheus 指標收集", "Grafana 視覺化", "即時警報通知"], y: 3.7, color: C.amber },
];
layers.forEach(layer => {
  s3.addShape(pres.shapes.RECTANGLE, { x: 0.8, y: layer.y, w: 2.2, h: 0.85, fill: { color: layer.color }, rectRadius: 0.05 });
  s3.addText(layer.label, { x: 0.8, y: layer.y, w: 2.2, h: 0.85, fontSize: 14, fontFace: "Arial", color: C.white, bold: true, align: "center", valign: "middle" });
  layer.items.forEach((item, i) => {
    s3.addText(`• ${item}`, { x: 3.3, y: layer.y + i * 0.28, w: 6, h: 0.28, fontSize: 13, fontFace: "Arial", color: C.dark });
  });
});
// Arrows between layers
[2.25, 3.35].forEach(y => {
  s3.addShape(pres.shapes.RECTANGLE, { x: 1.85, y, w: 0.1, h: 0.3, fill: { color: C.slate } });
});
addPageNumber(s3, 3);

// ══════════════════════════════════════════════
// Slide 4: Monitoring Metrics (Light)
// ══════════════════════════════════════════════
const s4 = addLightSlide();
s4.addText("監控指標定義", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.navy, bold: true });

const metricsTable = [
  [{ text: "指標名稱", options: { bold: true, color: C.white, fill: { color: C.navy }, fontSize: 12 } },
   { text: "類型", options: { bold: true, color: C.white, fill: { color: C.navy }, fontSize: 12 } },
   { text: "說明", options: { bold: true, color: C.white, fill: { color: C.navy }, fontSize: 12 } }],
  ["erp_api_requests_total", "Counter", "API 請求總數"],
  ["erp_ai_agent_latency_seconds", "Histogram", "AI Agent 處理延遲"],
  ["erp_ai_agent_errors_total", "Counter", "AI Agent 錯誤數"],
  ["erp_ai_agent_health_score", "Gauge", "Agent 健康分數 (0-1)"],
  ["erp_work_orders_active", "Gauge", "活躍工單數"],
  ["erp_agent_flow_executions_total", "Counter", "Agent Flow 執行次數"],
];
s4.addTable(metricsTable, {
  x: 0.8, y: 1.3, w: 8.4, colW: [3.2, 1.2, 4],
  border: { pt: 0.5, color: "E2E8F0" },
  rowH: [0.35, 0.35, 0.35, 0.35, 0.35, 0.35, 0.35],
  fontSize: 11, fontFace: "Arial",
});
addPageNumber(s4, 4);

// ══════════════════════════════════════════════
// Slide 5: Dashboard Overview (Dark)
// ══════════════════════════════════════════════
const s5 = addDarkSlide();
s5.addText("即時監控儀表板", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.white, bold: true });

const kpiCards = [
  { label: "可用率", value: "99.23%", color: C.emerald },
  { label: "平均延遲", value: "156ms", color: C.cobalt },
  { label: "Token 消耗", value: "284.5K", color: C.amber },
  { label: "活躍 Agent", value: "3", color: C.emerald },
];
kpiCards.forEach((kpi, i) => {
  const x = 0.8 + i * 2.2;
  s5.addShape(pres.shapes.RECTANGLE, { x, y: 1.4, w: 2, h: 1.2, fill: { color: C.navy }, line: { color: kpi.color, width: 1.5 } });
  s5.addText(kpi.value, { x, y: 1.5, w: 2, h: 0.7, fontSize: 28, fontFace: "Arial", color: kpi.color, bold: true, align: "center", valign: "middle" });
  s5.addText(kpi.label, { x, y: 2.1, w: 2, h: 0.4, fontSize: 12, fontFace: "Arial", color: C.slate, align: "center" });
});

s5.addText("• 延遲趨勢 (P50/P95/P99) 即時折線圖", { x: 0.8, y: 3.0, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.ice });
s5.addText("• API QPS 區域圖 + 錯誤率長條圖", { x: 0.8, y: 3.4, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.ice });
s5.addText("• Agent 健康分數進度條 + 系統拓撲圖", { x: 0.8, y: 3.8, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.ice });
s5.addText("• Agent Flow 執行紀錄與狀態分佈", { x: 0.8, y: 4.2, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.ice });
addPageNumber(s5, 5);

// ══════════════════════════════════════════════
// Slide 6: Grafana Integration (Light)
// ══════════════════════════════════════════════
const s6 = addLightSlide();
s6.addText("Grafana 監控整合", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.navy, bold: true });

s6.addText("快速啟動", { x: 0.8, y: 1.3, w: 4, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.cobalt, bold: true });
s6.addShape(pres.shapes.RECTANGLE, { x: 0.8, y: 1.8, w: 4, h: 0.5, fill: { color: C.navy } });
s6.addText("cd monitoring && docker-compose up -d", { x: 0.9, y: 1.8, w: 3.8, h: 0.5, fontSize: 13, fontFace: "Consolas", color: C.emerald, valign: "middle" });

s6.addText("服務位址", { x: 0.8, y: 2.6, w: 4, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.cobalt, bold: true });
s6.addText("• Prometheus: http://localhost:9090\n• Grafana: http://localhost:3000\n• 登入: admin / admin123", {
  x: 0.8, y: 3.1, w: 4, h: 1.2, fontSize: 13, fontFace: "Arial", color: C.dark, lineSpacingMultiple: 1.4
});

s6.addText("Grafana 面板", { x: 5.5, y: 1.3, w: 4, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.cobalt, bold: true });
const panels = ["Agent 健康總覽", "延遲趨勢圖", "ERP API QPS", "錯誤率報警", "Agent Flow 統計", "Token 消耗追蹤"];
panels.forEach((p, i) => {
  s6.addShape(pres.shapes.RECTANGLE, { x: 5.5, y: 1.9 + i * 0.45, w: 0.08, h: 0.25, fill: { color: C.cobalt } });
  s6.addText(p, { x: 5.75, y: 1.9 + i * 0.45, w: 3.5, h: 0.25, fontSize: 13, fontFace: "Arial", color: C.dark });
});
addPageNumber(s6, 6);

// ══════════════════════════════════════════════
// Slide 7: Agent Flow - Anomaly Dispatch (Light)
// ══════════════════════════════════════════════
const s7 = addLightSlide();
s7.addText("Agent Flow: 產線異常派工", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 28, fontFace: "Arial", color: C.navy, bold: true });
s7.addShape(pres.shapes.RECTANGLE, { x: 0.8, y: 1.1, w: 8.4, h: 0.04, fill: { color: C.crimson } });

const steps1 = ["AI 異常偵測", "建立維修單", "查詢維修人員", "派工回寫 ERP", "LINE 推播"];
steps1.forEach((step, i) => {
  const x = 0.8 + i * 1.7;
  s7.addShape(pres.shapes.RECTANGLE, { x, y: 1.5, w: 1.5, h: 0.9, fill: { color: C.white }, line: { color: C.crimson, width: 1 } });
  s7.addShape(pres.shapes.RECTANGLE, { x, y: 1.5, w: 1.5, h: 0.06, fill: { color: C.crimson } });
  s7.addText(`${i + 1}`, { x, y: 1.6, w: 1.5, h: 0.35, fontSize: 18, fontFace: "Arial", color: C.crimson, bold: true, align: "center" });
  s7.addText(step, { x, y: 1.95, w: 1.5, h: 0.4, fontSize: 11, fontFace: "Arial", color: C.dark, align: "center" });
  if (i < 4) s7.addText("→", { x: x + 1.5, y: 1.7, w: 0.2, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.slate, align: "center" });
});

s7.addText("觸發條件: IoT 感測器 / AI Vision 檢測產線異常", { x: 0.8, y: 2.8, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.dark, bold: true });
s7.addText([
  { text: "效益: ", options: { bold: true, color: C.emerald } },
  { text: "減少停機時間 25 分鐘/次 | 維修單建立時間從 15 分鐘縮短至 8 秒", options: { color: C.dark } },
], { x: 0.8, y: 3.2, w: 8, h: 0.4, fontSize: 14, fontFace: "Arial" });
s7.addText([
  { text: "API 串接: ", options: { bold: true, color: C.cobalt } },
  { text: "POST /api/work-orders → GET /api/employees → PATCH /api/work-orders/{id}/assign → POST /api/digital/line-notify", options: { color: C.slate, fontSize: 11 } },
], { x: 0.8, y: 3.7, w: 8.4, h: 0.4, fontSize: 12, fontFace: "Arial" });
addPageNumber(s7, 7);

// ══════════════════════════════════════════════
// Slide 8: Agent Flow - Customer Inquiry (Dark)
// ══════════════════════════════════════════════
const s8 = addDarkSlide();
s8.addText("Agent Flow: 客服詢價自動化", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 28, fontFace: "Arial", color: C.white, bold: true });
s8.addShape(pres.shapes.RECTANGLE, { x: 0.8, y: 1.1, w: 8.4, h: 0.04, fill: { color: C.cobalt } });

const steps2 = ["NLP 解析", "查庫存", "比對交期", "生成報價", "回傳 LINE"];
steps2.forEach((step, i) => {
  const x = 0.8 + i * 1.7;
  s8.addShape(pres.shapes.RECTANGLE, { x, y: 1.5, w: 1.5, h: 0.9, fill: { color: C.navy }, line: { color: C.cobalt, width: 1 } });
  s8.addShape(pres.shapes.RECTANGLE, { x, y: 1.5, w: 1.5, h: 0.06, fill: { color: C.cobalt } });
  s8.addText(`${i + 1}`, { x, y: 1.6, w: 1.5, h: 0.35, fontSize: 18, fontFace: "Arial", color: C.cobalt, bold: true, align: "center" });
  s8.addText(step, { x, y: 1.95, w: 1.5, h: 0.4, fontSize: 11, fontFace: "Arial", color: C.ice, align: "center" });
  if (i < 4) s8.addText("→", { x: x + 1.5, y: 1.7, w: 0.2, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.slate, align: "center" });
});

s8.addText("觸發條件: LINE / 官網詢價訊息進入", { x: 0.8, y: 2.8, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.ice, bold: true });
s8.addText("效益: 回應時間從 4 小時縮短至 7 秒 | 24/7 全自動回覆", { x: 0.8, y: 3.2, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.slate });
s8.addText("API: NLP → /api/inventory → /api/suppliers → /api/quotes → LINE 回覆", { x: 0.8, y: 3.7, w: 8.4, h: 0.3, fontSize: 12, fontFace: "Arial", color: C.slate });
addPageNumber(s8, 8);

// ══════════════════════════════════════════════
// Slide 9: Agent Flow - Inventory Replenishment (Light)
// ══════════════════════════════════════════════
const s9 = addLightSlide();
s9.addText("Agent Flow: 庫存預警補貨", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 28, fontFace: "Arial", color: C.navy, bold: true });
s9.addShape(pres.shapes.RECTANGLE, { x: 0.8, y: 1.1, w: 8.4, h: 0.04, fill: { color: C.emerald } });

const steps3 = ["掃描低庫存", "AI 預測", "比對價格", "生成 PR", "審批流程"];
steps3.forEach((step, i) => {
  const x = 0.8 + i * 1.7;
  s9.addShape(pres.shapes.RECTANGLE, { x, y: 1.5, w: 1.5, h: 0.9, fill: { color: C.white }, line: { color: C.emerald, width: 1 } });
  s9.addShape(pres.shapes.RECTANGLE, { x, y: 1.5, w: 1.5, h: 0.06, fill: { color: C.emerald } });
  s9.addText(`${i + 1}`, { x, y: 1.6, w: 1.5, h: 0.35, fontSize: 18, fontFace: "Arial", color: C.emerald, bold: true, align: "center" });
  s9.addText(step, { x, y: 1.95, w: 1.5, h: 0.4, fontSize: 11, fontFace: "Arial", color: C.dark, align: "center" });
  if (i < 4) s9.addText("→", { x: x + 1.5, y: 1.7, w: 0.2, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.slate, align: "center" });
});

s9.addText("觸發條件: 定時排程 (每小時) / 庫存低於安全水位", { x: 0.8, y: 2.8, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.dark, bold: true });
s9.addText([
  { text: "效益: ", options: { bold: true, color: C.emerald } },
  { text: "避免缺料停線 | 採購申請建立時間從 30 分鐘縮短至 12 秒", options: { color: C.dark } },
], { x: 0.8, y: 3.2, w: 8, h: 0.4, fontSize: 14, fontFace: "Arial" });
s9.addText("API: /api/inventory/low-stock → AI → /api/suppliers → /api/purchase-requisitions → /api/approval-workflow", { x: 0.8, y: 3.7, w: 8.4, h: 0.3, fontSize: 11, fontFace: "Arial", color: C.slate });
addPageNumber(s9, 9);

// ══════════════════════════════════════════════
// Slide 10: ROI & Benefits (Dark)
// ══════════════════════════════════════════════
const s10 = addDarkSlide();
s10.addText("效益分析", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.white, bold: true });

const benefits = [
  { label: "效率提升", value: "30%+", desc: "自動化流程減少人工抄錄錯誤", color: C.emerald },
  { label: "停機減少", value: "25 min/次", desc: "異常偵測到派工 < 10 秒", color: C.cobalt },
  { label: "回應速度", value: "7 秒", desc: "客服詢價 24/7 即時回覆", color: C.amber },
];
benefits.forEach((b, i) => {
  const y = 1.5 + i * 1.2;
  s10.addShape(pres.shapes.RECTANGLE, { x: 0.8, y, w: 8.4, h: 0.95, fill: { color: C.navy }, line: { color: b.color, width: 1 } });
  s10.addText(b.value, { x: 1.0, y, w: 2.2, h: 0.95, fontSize: 28, fontFace: "Arial", color: b.color, bold: true, valign: "middle" });
  s10.addText(b.label, { x: 3.5, y: y + 0.05, w: 5, h: 0.4, fontSize: 16, fontFace: "Arial", color: C.white, bold: true });
  s10.addText(b.desc, { x: 3.5, y: y + 0.45, w: 5, h: 0.35, fontSize: 13, fontFace: "Arial", color: C.slate });
});
addPageNumber(s10, 10);

// ══════════════════════════════════════════════
// Slide 11: Deployment Timeline (Light)
// ══════════════════════════════════════════════
const s11 = addLightSlide();
s11.addText("導入時程", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.navy, bold: true });

const phases = [
  { phase: "Phase 1", title: "後端指標暴露", weeks: "第 1 週", color: C.cobalt },
  { phase: "Phase 2", title: "React 監控頁面", weeks: "第 2 週", color: C.emerald },
  { phase: "Phase 3", title: "Grafana 配置", weeks: "第 2-3 週", color: C.amber },
  { phase: "Phase 4", title: "Agent Flow 實作", weeks: "第 3-4 週", color: C.cobalt },
  { phase: "Phase 5", title: "測試與上線", weeks: "第 5 週", color: C.emerald },
];
phases.forEach((p, i) => {
  const y = 1.5 + i * 0.7;
  s11.addShape(pres.shapes.RECTANGLE, { x: 0.8, y, w: 1.5, h: 0.5, fill: { color: p.color }, rectRadius: 0.05 });
  s11.addText(p.phase, { x: 0.8, y, w: 1.5, h: 0.5, fontSize: 11, fontFace: "Arial", color: C.white, bold: true, align: "center", valign: "middle" });
  s11.addText(p.title, { x: 2.6, y, w: 3, h: 0.5, fontSize: 14, fontFace: "Arial", color: C.dark, bold: true, valign: "middle" });
  s11.addText(p.weeks, { x: 6, y, w: 2, h: 0.5, fontSize: 13, fontFace: "Arial", color: C.slate, valign: "middle" });
});
s11.addText("總時程: 5 週 | 首批試點: 品管與客服場景", { x: 0.8, y: 5.0, w: 8, h: 0.3, fontSize: 14, fontFace: "Arial", color: C.cobalt, bold: true });
addPageNumber(s11, 11);

// ══════════════════════════════════════════════
// Slide 12: Summary & Next Steps (Dark)
// ══════════════════════════════════════════════
const s12 = addDarkSlide();
s12.addText("總結與下一步", { x: 0.8, y: 0.4, w: 8, h: 0.7, fontSize: 32, fontFace: "Arial", color: C.white, bold: true });
s12.addShape(pres.shapes.RECTANGLE, { x: 0, y: 1.1, w: 10, h: 0.04, fill: { color: C.cobalt } });

s12.addText("本方案提供完整的 ERP AI Agent 監控與自動化解決方案:", { x: 0.8, y: 1.4, w: 8, h: 0.4, fontSize: 16, fontFace: "Arial", color: C.ice });

const summaryItems = [
  "即時監控儀表板 (React + Grafana 雙軌方案)",
  "三大 Agent Flow 自動化場景覆蓋製造業痛點",
  "完整 Prometheus 指標體系與告警機制",
  "5 週快速導入，從試點到全面上線",
];
summaryItems.forEach((item, i) => {
  s12.addShape(pres.shapes.RECTANGLE, { x: 0.8, y: 2.1 + i * 0.55, w: 0.08, h: 0.3, fill: { color: C.emerald } });
  s12.addText(item, { x: 1.1, y: 2.1 + i * 0.55, w: 8, h: 0.3, fontSize: 15, fontFace: "Arial", color: C.ice });
});

s12.addText("下一步: 確認試點場景 → 啟動 Phase 1 開發", { x: 0.8, y: 4.6, w: 8, h: 0.4, fontSize: 18, fontFace: "Arial", color: C.cobalt, bold: true });
addPageNumber(s12, 12);

// ── Generate ──
pres.writeFile({ fileName: "D:/ERP/docs/ERP_AI_Agent_監控系統簡報.pptx" }).then(() => {
  console.log("PPT created successfully!");
}).catch(err => {
  console.error("Error:", err);
});
