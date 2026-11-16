# 系統架構總覽 (System Architecture Overview)

## 1. 技術棧

| 層級 | 技術 |
|------|------|
| 前端 | React 19, TypeScript, Vite, Recharts, Three.js |
| 行動端 | React Native (Expo) — 員工 App |
| 後端 | ASP.NET Core 8.0 Web API |
| 資料庫 | PostgreSQL + EF Core 8.0 |
| 監控 | Prometheus + Grafana |
| 認證 | JWT Bearer + RBAC |

## 2. 專案結構

```
ERP/
├── Erp.sln                  # Solution 檔
├── src/
│   ├── ErpBackend/          # 後端 Web API
│   │   ├── Controllers/     # 76 個控制器
│   │   ├── Services/        # 38 個服務
│   │   ├── Models/          # ~240 個模型
│   │   ├── DTOs/            # ~90 個 DTO
│   │   ├── Data/            # DbContext / Migrations
│   │   ├── Middleware/      # 中介軟體
│   │   ├── Filters/         # Action Filter
│   │   └── Helpers/         # 輔助工具
│   ├── erp-frontend/        # 前端 SPA
│   ├── erp-employee-app/    # 行動端 App (Expo)
│   └── docs/                # 專案文件
├── deployment/              # Docker / 監控部署
│   ├── docker-compose.yml   # Prometheus + Grafana
│   ├── prometheus/          # 監控設定
│   └── grafana/             # 儀表板
├── tools/scripts/           # 文件產生工具
└── tests/                   # 測試專案
```

## 3. 部署拓撲

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│  前端 SPA    │ ───> │  後端 API    │ ───> │  PostgreSQL  │
│  (Vite)     │      │ (ASP.NET 8)  │      │   (EF Core)  │
└─────────────┘      └──────┬───────┘      └─────────────┘
                            │
                            v
                   ┌─────────────────┐
                   │ Prometheus      │──> Grafana
                   │ (metrics)       │    (dashboards)
                   └─────────────────┘
```

## 4. 環境設定

後端透過環境變數設定：

| 變數 | 說明 |
|------|------|
| `DB_CONNECTION_STRING` | PostgreSQL 連線字串 |
| `JWT_SECRET_KEY` | JWT 密鑰（至少 32 字元） |
| `LLM_PROVIDER` | AI Provider（openai/anthropic） |
| `OPENAI_API_KEY` | OpenAI API Key |
| `ANTHROPIC_API_KEY` | Anthropic API Key |
| `ADMIN_INITIAL_PASSWORD` | 初始管理員密碼 |

