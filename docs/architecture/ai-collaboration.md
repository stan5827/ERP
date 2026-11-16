# AI 協作模式與 MCP 能力評估

> 文件版本: v1.1
> 最後更新: 2026-11-02
> 此文件說明本專案如何與 AI 工具（Claude Code、GitHub Copilot、Cursor 等）協作，以及 MCP（Model Context Protocol）的評估。開發者 AI 協作與 ERP 使用者執行期 Agent 是不同信任邊界；面向 ERP 使用者的 Agent 架構、隱私防護與修改階段，請參考 [ProMES ERP AI Agent 隱私防護與分階段修改方案](./ai-agent-erp-integration-plan.md)。

---

## 1. AI 協作架構

### 1.1 協作原則

```
開發者 → 提出任務（Issue / PR Description）
    │
    ▼
AI Agent ↴
    ├── 讀取 AGENTS.md（專案規範）
    ├── 讀取 .github/copilot-instructions.md（Copilot 規範）
    ├── 讀取 docs/architecture/*（架構規則）
    ├── 搜尋相關原始碼（search_files + read_file）
    ├── 制定計畫
    ├── 執行修改
    └── 驗證（dotnet build + test）
    │
    ▼
開發者 ← 審查 PR
```

### 1.2 已就緒的 AI 協作基礎設施

| 設施 | 路徑 | 用途 |
|------|------|------|
| AGENTS.md | `AGENTS.md` | AI Agent 作業規範（鐵律、流程、命名） |
| Copilot Instructions | `.github/copilot-instructions.md` | GitHub Copilot Chat/Agent 提示詞 |
| Architecture Tests | `tests/ErpBackend.Tests/Architecture/` | 自動驗證 AI 生成的程式碼符合架構 |
| CI Pipeline | `.github/workflows/ci.yml` | AI 生成後自動驗證 |
| Quality Gate | `docs/architecture/quality-gate.md` | 合併門檻 |
| TODO.md | `TODO.md` | 執行狀態追蹤 |
| Dependency Rules | `docs/architecture/dependency-rules.md` | 依賴矩陣供 AI 參考 |
| Module Map | `docs/architecture/module-map.md` | 模組邊界與關聯 |

---

## 2. AI 可協助的任務類型

| 任務 | 適合 AI | 備註 |
|------|---------|------|
| 新增 CRUD Controller | ✅ | 需按規範先建立 Service/Interface |
| 抽取 Servic + Interface（從既有 Controller） | ✅ | 重構模式明確 |
| 新增 DTO 與映射 | ✅ | 命名慣例明確 |
| 新增單元測試 | ✅ | xUnit 測試模式固定 |
| 修正編譯錯誤 | ✅ | 錯誤訊息明確 |
| 建立文件（Markdown） | ✅ | 路徑與格式已規範 |
| 大型架構重構（服務拆分） | ⚠️ 部分 | 需人工審查 |
| 複雜業務邏輯設計 | ❌ | 需領域專家 |
| 資料庫遷移（Migration） | ❌ | 高風險操作 |

---

## 3. MCP Server 評估

MCP（Model Context Protocol）可讓 AI Agent 存取外部工具與資料源。以下為本專案的評估：

### 3.1 建議導入的 MCP Server

| MCP Server | 優先級 | 用途 | 實作難度 |
|------------|--------|------|---------|
| **GitHub MCP Server** | 🔴 高 | Agent 可直接建立 PR、Issue、搜尋程式碼 | 低（官方提供） |
| **ERP Tool Gateway / MCP Adapter** | 🟡 中 | 以具使用者權限的 ERP Service/API 提供固定工具 | 中（先完成身分委派與工具治理） |
| **File System MCP Server** | 🟢 低 | Agent 可建立/讀取專案文件（已可透過工具達成） | 低（已內建） |
| **Docker MCP Server** | 🟢 低 | Agent 可直接操作 Docker 容器 | 低 |

### 3.2 不建議導入的 MCP Server

| MCP Server | 原因 |
|------------|------|
| **Shell Execution MCP** | 安全風險（Agent 已有 execute_command 能力） |
| **Email / Slack MCP** | 非 ERP 專案核心需求 |
| **Browser Automation MCP** | 前端測試另有 Playwright/Cypress |

### 3.3 導入 MCP 的效益分析

| 情境 | 無 MCP | 有 MCP（GitHub + ERP Tool Gateway） |
|------|--------|----------------------|
| 查詢 ERP 業務資料 | 透過現有 ERP API | 透過受控工具呼叫既有 ERP Service/API |
| 建立 PR | 手動操作 | Agent 可自動建立 |
| 查詢 issue 狀態 | 手動至 GitHub 檢視 | Agent 可搜尋並摘要 |
| 比對資料庫與模型 | 人工比對核准的 schema 資訊 | 在不授予 Agent 直接資料庫存取的前提下，比對核准匯出的 schema 與程式模型 |

### 3.4 建議實施步驟

```
Phase 5.3a: 導入 GitHub MCP（當週可完成）
  1. 確認開發環境已設定 GitHub CLI (gh)
  2. 啟用 gh MCP Server
  3. 測試：請 AI 建立 Issue Review Comment

Phase 5.3b: 評估 ERP Tool Gateway / MCP Adapter
  1. 以 ERP JWT 使用者身分委派工具呼叫
  2. 僅暴露固定、具型別的業務查詢工具
  3. 沿用既有 Service、RBAC、資料範圍與稽核
  4. 不提供 Agent 直接存取 PostgreSQL 或任意 SQL 的能力
  5. 寫入工具須另行設計預覽、明確確認、冪等及既有簽核保護
```

---

## 4. AI 生成的程式碼品質要求

| 項目 | 標準 | 驗證方式 |
|------|------|---------|
| 編譯 | 0 errors | `dotnet build` |
| 測試 | 0 failed | `dotnet test` |
| 架構測試 | 0 failed（non-skipped） | `dotnet test --filter Architecture` |
| 命名慣例 | 依 AGENTS.md | Architecture Tests |
| 依賴規則 | 依 dependency-rules.md | Architecture Tests |
| 程式碼風格 | 依 .editorconfig | Directory.Build.props 強制執行 |

---

## 5. 給未來開發者 / AI Agent 的備註

- **已經有架構測試保護**：不應隨意 `[Skip]`，需有合理理由
- **AGENTS.md 是最高優先級**：與 AGENTS.md 衝突的操作應先修正 AGENTS.md
- **Phase 0-5 狀態**：見 `TODO.md`，若為 Phase 4+ 任務，請同步更新 TODO.md
- **PR 描述**：應包含「修改了哪些檔案」、「為什麼」、「測試結果」
