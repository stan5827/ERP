# ERP AI Agent 監控系統

## 快速啟動

### 1. 啟動監控服務

```bash
cd monitoring
docker-compose up -d
```

### 2. 開啟服務

| 服務 | 位址 | 帳號 |
|------|------|------|
| Prometheus | http://localhost:9090 | - |
| Grafana | http://localhost:3000 | admin / admin123 |

### 3. 確認指標暴露

啟動 ERP 後端後，確認 `/metrics` 端點可存取：

```bash
curl http://localhost:5100/metrics
```

## 監控指標

| 指標名稱 | 類型 | 說明 |
|----------|------|------|
| `erp_api_requests_total` | Counter | API 請求總數 |
| `erp_api_request_duration_seconds` | Histogram | API 回應延遲 |
| `erp_ai_agent_latency_seconds` | Histogram | AI Agent 處理延遲 |
| `erp_ai_agent_errors_total` | Counter | AI Agent 錯誤數 |
| `erp_ai_agent_tokens_total` | Counter | Token 消耗量 |
| `erp_ai_agent_health_score` | Gauge | Agent 健康分數 |
| `erp_work_orders_active` | Gauge | 活躍工單數 |
| `erp_inventory_low_stock_count` | Gauge | 低庫存品項數 |
| `erp_agent_flow_executions_total` | Counter | Agent Flow 執行次數 |
| `erp_agent_flow_duration_seconds` | Histogram | Agent Flow 執行耗時 |

## Grafana 儀表板

匯入後自動包含以下面板：

1. **Agent 健康總覽** — 健康分數、可用率、延遲、QPS
2. **延遲趨勢** — P50/P95/P99 延遲折線圖
3. **ERP API 效能** — QPS 按端點分佈、請求狀態圓餅圖
4. **Agent 效能** — Agent 延遲、Flow 執行統計、Token 消耗

## 常見問題

### Prometheus 無法抓取 ERP 指標

確認 `prometheus.yml` 中的 target 位址與 ERP 後端實際運行位址一致。Docker 環境下使用 `host.docker.internal` 存取宿主機服務。

### Grafana 顯示無資料

1. 確認 Prometheus 已成功抓取指標 (Status → Targets)
2. 確認 ERP 後端已執行且 `/metrics` 端點可存取
3. 檢查 Grafana 資料源連線 (Connections → Data sources)
