# ERP 正式上線準備檢查清單 (Production Readiness Checklist)

本文件為 ERP 系統正式上線 (Go-Live) 前的最後驗證清單，涵蓋密鑰管理、資料庫遷移、日誌與監控等關鍵領域。

---

## 1. 密鑰管理 (Secrets Management)

### 1.1 敏感資訊清單

以下資訊**禁止**寫死在程式碼或明文設定檔中，必須透過雲端密鑰服務注入：

| 密鑰 | 說明 | 建議管理方式 |
|------|------|-------------|
| `DB_CONNECTION_STRING` | PostgreSQL 資料庫連線字串 | AWS Secrets Manager / Azure Key Vault / K8s Secrets |
| `JWT_SECRET_KEY` | JWT 簽章金鑰 (≥32 字元) | 雲端密鑰服務，定期輪替 |
| `LLM_API_KEY` | OpenAI / Anthropic API 金鑰 | 雲端密鑰服務 |
| `REDIS_CONNECTION_STRING` | Redis 快取連線 (含密碼) | 雲端密鑰服務 |
| `ADMIN_INITIAL_PASSWORD` | 系統管理員初始密碼 | 部署後立即變更 |
| `ALERTMANAGER_SLACK_WEBHOOK_URL` | Slack 告警 Webhook | 雲端密鑰服務 |
| `ALERTMANAGER_EMAIL_SMTP_PASSWORD` | SMTP 認證密碼 | 雲端密鑰服務 |

### 1.2 生產環境變數模板

參考 `deployment/.env.production.template`，使用方式：

```bash
# 複製模板
cp deployment/.env.production.template deployment/.env.production

# 填入實際密鑰 (透過雲端密鑰服務注入，禁止手動填入真實值後提交)
# 例如 AWS Secrets Manager：
aws secretsmanager create-secret --name erp/prod/db-connection --secret-string "Host=..."

# 例如 Azure Key Vault：
az keyvault secret set --vault-name erp-prod-kv --name DbConnectionString --value "Host=..."

# 例如 Kubernetes Secrets：
kubectl create secret generic erp-secrets \
  --from-literal=DB_CONNECTION_STRING="..." \
  --from-literal=JWT_SECRET_KEY="..."
```

### 1.3 密鑰檢查清單

- [ ] 所有敏感環境變數已從 `.env`、`appsettings.json` 等可提交檔案中移除
- [ ] `.env.production` 已加入 `.gitignore`
- [ ] 密鑰已存入雲端密鑰服務 (AWS Secrets Manager / Azure Key Vault / HashiCorp Vault)
- [ ] CI/CD 管線可從密鑰服務讀取並注入容器環境變數
- [ ] JWT_SECRET_KEY 長度 ≥ 32 字元且為隨機產生
- [ ] 密鑰輪替策略已制定 (建議每 90 天輪替一次)

---

## 2. 資料庫正式遷移計畫 (Production DB Migration)

### 2.1 遷移策略

ERP 後端使用 EF Core Migrations 管理資料庫結構。生產環境**禁止**在應用程式啟動時自動執行遷移，應以獨立步驟執行。

### 2.2 遷移命令

```bash
# 1. 建立備份 (遷移前必備)
pg_dump -h <db-host> -U erp_app -d ErpGeneral > backup_$(date +%Y%m%d_%H%M%S).sql

# 2. 檢查待套用的遷移
dotnet ef migrations list --project src/ErpBackend/ErpBackend.csproj

# 3. 產生遷移腳本 (檢查 SQL，不直接執行)
dotnet ef migrations script --project src/ErpBackend/ErpBackend.csproj --output deployment/migrations/production_migration.sql

# 4. 在生產資料庫上執行遷移
dotnet ef database update --project src/ErpBackend/ErpBackend.csproj --connection-string "<DB_CONNECTION_STRING>"

# 5. 驗證遷移結果
psql -h <db-host> -U erp_app -d ErpGeneral -c "\dt"
```

### 2.3 版本升級策略

| 版本類型 | 處理方式 | 風險等級 |
|----------|---------|---------|
| 資料庫結構變更 (新增欄位/表格) | EF Core Migrations，備份後執行 | 中 |
| 資料遷移 (資料格式轉換) | 編寫 SQL 腳本，測試環境驗證後執行 | 高 |
| 索引新增/修改 | 非鎖定建立索引 (CREATE INDEX CONCURRENTLY) | 低 |
| 應用程式升級 | 藍綠部署 (Blue-Green)，先部署新版本再切換流量 | 中 |

### 2.4 遷移檢查清單

- [ ] 生產資料庫備份已完成 (遷移前)
- [ ] 遷移腳本已在 staging 環境驗證通過
- [ ] 遷移執行時間已評估 (預計停機窗口)
- [ ] 回滾計畫已制定 (備份還原程序)
- [ ] 遷移後資料一致性已驗證 (記錄數、關聯完整性)
- [ ] 應用程式已重新部署至新資料庫結構

---

## 3. 日誌與監控持久化 (Logging & Alerting)

### 3.1 告警規則

現有告警規則位於 `deployment/prometheus/alerts.yml`，涵蓋：

| 告警名稱 | 嚴重度 | 觸發條件 | 通知對象 |
|----------|--------|----------|----------|
| ERP API Down | critical | up{job="erp-backend"} == 0 | team-devops-critical |
| ERP API High Error Rate | critical | 5xx 錯誤率 > 5% (5m) | team-devops-critical |
| ERP API High Latency | warning | P95 延遲 > 2s (5m) | team-backend |
| ERP Database Connection Pool Exhaustion | critical | 連線使用率 > 85% (5m) | team-devops-critical |
| ERP Database Connection Pool Saturation | critical | 連線使用率 > 95% (2m) | team-devops-critical |
| ERP High Memory Usage | warning | 記憶體使用率 > 85% (10m) | team-devops |
| ERP Disk Space Low | warning | 磁碟使用率 > 85% (10m) | team-devops |
| ERP Business Process Failure | critical | 業務失敗 > 5 次/10m | team-devops-critical |
| ERP Low Stock Warning | warning | 低庫存品項 > 20 (15m) | team-operations |
| ERP AI Agent Health Degraded | warning | 健康分數 < 0.5 (10m) | team-ai |
| ERP AI Agent High Error Rate | warning | AI 錯誤率 > 20% (5m) | team-ai |

### 3.2 Alertmanager 通知設定

位於 `deployment/alertmanager.yml`，支援：

- **Slack**：`#erp-alerts` (一般), `#erp-critical` (緊急), `#erp-backend`, `#erp-operations`, `#erp-ai`
- **Email**：devops@yourdomain.com, devops-critical@yourdomain.com, backend-team@yourdomain.com, operations@yourdomain.com, ai-team@yourdomain.com

### 3.3 告警檢查清單

- [ ] Alertmanager 容器已啟動並可從 Prometheus 連接
- [ ] Slack Webhook URL 已填入並測試通知
- [ ] SMTP 郵件伺服器已配置並測試發送
- [ ] 關鍵告警 (critical) 已設定重複通知 (repeat_interval: 4h)
- [ ] 抑制規則 (inhibit_rules) 已啟用 (critical 警告時不發送重複 warning)
- [ ] Grafana 儀表板已匯入並顯示關鍵指標

### 3.4 日誌保留政策

| 日誌類型 | 保留期限 | 儲存位置 |
|----------|---------|---------|
| 應用程式日誌 | 30 天 | 容器 stdout → 彙總至 ELK / CloudWatch |
| 資料庫查詢日誌 | 7 天 | PostgreSQL pg_log |
| 監控指標 (Prometheus) | 30 天 | prometheus_data Volume |
| 告警歷史 | 90 天 | alertmanager_data Volume |

---

## 4. 其他生產環境檢查

### 4.1 安全設定

- [ ] `ASPNETCORE_ENVIRONMENT=Production` (停用 Swagger)
- [ ] `JWT_SECRET_KEY` 已設定且長度 ≥ 32 字元
- [ ] CORS 允許的來源僅包含生產前端域名
- [ ] HTTPS 已啟用 (反向代理 / 負載平衡器終止 SSL)
- [ ] 非 root 使用者運行容器 (Dockerfile.backend 已設定 appuser)
- [ ] 健康檢查端點 `/health` 可正常回應

### 4.2 容器與部署

- [ ] `deployment/Dockerfile.backend` 使用多階段建置 (SDK → Runtime)
- [ ] `deployment/docker-compose.yml` 包含 postgres、redis、backend、frontend、prometheus、grafana、alertmanager
- [ ] 資料庫 Volume 已設定持久化 (postgres_data)
- [ ] Redis AOF 持久化已啟用
- [ ] k6 負載測試容器已加入 (load-test profile，需手動啟動)

### 4.3 CI/CD 管線

- [ ] `.github/workflows/ci.yml` 已設定：
  - build-and-test (Push/PR to main/master)：dotnet test (581 測試)
  - build-and-push (僅 Push to main/master)：建置並推送 Docker 映像檔至 GHCR
  - security-scan (僅 Push to main/master)：Trivy 漏洞掃描 (CRITICAL/HIGH)
- [ ] 測試結果 artefact 已設定上傳
- [ ] Docker Buildx 快取已設定 (cache-from/cache-to)

### 4.4 效能基準

- [ ] P95 延遲 < 500ms (核心 API)
- [ ] P99 延遲 < 2s
- [ ] 錯誤率 < 1%
- [ ] MRP 運算 P95 < 3000ms
- [ ] 庫存寫入 P95 < 1000ms

---

## 5. 上線執行步驟 (Go-Live)

1. **備份**：生產資料庫完整備份
2. **密鑰注入**：透過雲端密鑰服務注入所有生產環境變數
3. **資料庫遷移**：執行 EF Core Migrations，驗證結構正確
4. **部署後端**：`docker-compose -f deployment/docker-compose.yml up -d backend postgres redis alertmanager prometheus grafana`
5. **健康檢查**：`curl http://<backend-host>:8080/health` 回傳 healthy
6. **前端部署**：`docker-compose -f deployment/docker-compose.yml up -d frontend`
7. **驗證**：執行 E2E 業務流程測試 (SalesOrder → MRP → WorkOrder → WipReport → Invoice → CashReceipt → GL)
8. **監控確認**：Prometheus Targets 顯示 all healthy，Grafana 儀表板有資料
9. **告警測試**：手動觸發一個測試告警，確認 Slack/Email 通知正常
10. **切換流量**：將生產流量指向新部署的後端

---

## 6. 緊急聯絡與回滾

| 項目 | 內容 |
|------|------|
| 維運團隊 | devops@yourdomain.com |
| 緊急聯絡 | cto@yourdomain.com |
| Slack 通道 | #erp-critical |
| 回滾方式 | 還原資料庫備份 + 回滾 Docker 映像檔 tag |
| 預計 RTO | < 30 分鐘 |
| 預計 RPO | < 15 分鐘 (依備份頻率) |
