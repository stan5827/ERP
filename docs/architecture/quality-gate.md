# 品質門檻 (Quality Gate) 規範

> 文件版本: v1.0  
> 最後更新: 2026-04-16  
> 適用範圍: ERP 專案所有合併請求 (Pull Request) 與持續整合 (CI) 流程

---

## 1. 概述

Quality Gate 是 CI/CD Pipeline 中的自動化檢查關卡，用於確保每次合併代碼前，所有架構規範、程式碼品質與測試覆蓋率皆達到最低標準。

```
Developer Push → Build → Unit Tests → Architecture Tests → Coverage Check → Static Analysis → Gate Passed
```

---

## 2. 品質門檻細項

### 2.1 建置與編譯 (Gate 1)

| 檢查項目 | 門檻 | 違規處理 |
|----------|------|----------|
| 編譯成功 | 0 個 Error | Pipeline 中斷 |
| `.editorconfig` 風格檢查 | `EnforceCodeStyleInBuild = true` | 發出 Warning（Phase 4 改為 Error） |
| 程式碼分析層級 | `AnalysisLevel = latest` | 已於 `Directory.Build.props` 設定 |

### 2.2 單元測試 (Gate 2)

| 檢查項目 | 門檻 | 違規處理 |
|----------|------|----------|
| 測試通過率 | 100%（0 Failed） | Pipeline 中斷 |
| 測試涵蓋率 | **目前: ≥ 0%（基線）** → Phase 4 目標: ≥ 20% → Phase 5 目標: ≥ 60% | Pipeline 不中斷，但記錄趨勢 |
| 架構測試 (Architecture Tests) | 0 Failed | Pipeline 中斷 |
| 技術債測試 (Skipped Tests) | 需在 `ARCHITECTURE_REVIEW.md` 登記 | 需附 Phase 修復計畫 |

### 2.3 架構規則 (Gate 3) — 由 Architecture Tests 強制執行

| 規則 | 對應測試類別 | 說明 |
|------|-------------|------|
| 控制器不得直接依賴 `ErpGeneralDbContext` | `ControllerDependencyTests` | 強制使用服務層 |
| 模型不得依賴 Controller / Data / DTO | `ArchitectureTests` | 避免循環依賴 |
| 介面必須以 `I` 開頭 | `ArchitectureTests` | 命名慣例 |
| 服務類別必須實作對應介面 (`IXxxService`) | `ServiceInterfaceTests` | 依賴反轉原則 |
| 非同步方法須以 `Async` 結尾 | `ServiceInterfaceTests` | 非同步命名慣例 |
| DTO 命名慣例 | `ModuleStructureTests` (Skipped) | Phase 4 啟用 |
| 服務層不得依賴控制器層 | `ServiceInterfaceTests` (Skipped) | Phase 4 啟用 |

### 2.4 程式碼風格 (Gate 4)

| 檢查項目 | 工具 / 設定 | 當前狀態 |
|----------|-------------|----------|
| Tab / 縮排 | `.editorconfig` (indent_style = space, size = 4) | ✅ 已設定 |
| IDE 檔案排除 | `.gitignore` 含 `.vs/`, `.idea/`, `*.suo` | ✅ 已設定 |
| Nullable 參考型別 | `Nullable = enable` (專案層級) | ✅ 已設定 |
| 建置輸出排除 | `.gitignore` 含 `bin/`, `obj/`, `dist/` | ✅ 已設定 |

### 2.5 文件完整性 (Gate 5)

| 檢查項目 | 路徑 | 狀態 |
|----------|------|------|
| 文件總索引 | `docs/README.md` | ✅ |
| API 文件 | `docs/api/` | ✅ |
| 資料庫設計 | `docs/database/` | ✅ |
| 架構規範 | `docs/architecture/` | ✅ |

---

## 3. 涵蓋率目標路線圖

| Phase | 日期 | 目標涵蓋率 | 累積測試數 | 新增測試類型 |
|-------|------|-----------|-----------|-------------|
| Phase 2 | 2026-04 | 基線 (~1%) | 81 | Architecture Tests (21) + 既有 (60) |
| Phase 3 | 2026-05 | ≥ 5% | ~100 | 控制器單元測試 |
| Phase 4 | 2026-06 | ≥ 20% | ~300 | Service 層測試 |
| Phase 5 | 2026-08 | ≥ 60% | ~800 | Integration + E2E |
| 長期目標 | 2027-Q1 | ≥ 80% | ~1500 | 全涵蓋 |

---

## 4. CI/CD 執行步驟

```yaml
# 在 .github/workflows/ci.yml 中的 backend job 已包含以下步驟：
1. dotnet restore
2. dotnet build -c Release          # Gate 1: 編譯
3. dotnet test --filter "FullyQualifiedName~Architecture"   # Gate 3: 架構測試
4. dotnet test                      # Gate 2: 單元測試 + 涵蓋率
5. dotnet-coverage collect ...      # Gate 2: 涵蓋率收集
6. 上傳涵蓋率報告至 CodeCov
```

---

## 5. 違規處理流程

```
發現違規
   │
   ├── 編譯錯誤 / 測試失敗 → Pipeline 中斷 → 開發者修正 → 重新觸發
   │
   ├── 架構測試失敗 → Pipeline 中斷 → 
   │      1. 若為誤判 → 更新 Architecture Tests (白名單)
   │      2. 若為新違規 → 修正代碼或 Phase 登記
   │
   ├── 涵蓋率低於門檻 → Pipeline 不中斷，但標記 PR 為 "需補充測試"
   │      → 技術債追蹤
   │
   └── 程式碼風格警告 → Pipeline 不中斷
          → 合併前應修正，或登記為技術債
```

---

## 6. 技術債追蹤

| ID | 描述 | 嚴重度 | 登記於 | Phase |
|----|------|--------|--------|-------|
| SKIP-001 | DTO 命名不統一（部分未以 Dto 結尾） | 中 | `ModuleStructureTests.AllDtos_ShouldEndWithDtoSuffix` | Phase 4 |
| SKIP-002 | DTO 散布在非 `DTOs` 命名空間 | 中 | `ModuleStructureTests.Dtos_ShouldBeInDtosNamespace` | Phase 4 |
| SKIP-003 | 服務層混入非 Service 類型（LLM 模型等） | 低 | `ServiceInterfaceTests.ServiceNames_ShouldEndWithService` | Phase 4 |
| SKIP-004 | `InventoryService` 依賴 Controllers | 高 | `ServiceInterfaceTests.Services_ShouldNotDependOnPresentationLayer` | Phase 4 |
| SKIP-005 | `InMemoryCacheService` 未實作 `IXxxService` 介面 | 低 | `ServiceInterfaceTests.ServiceImplementations_ShouldImplementInterfaceByName` | Phase 4 |
| SKIP-006 | 8 個服務未實作介面（UomConversion, ApprovalWorkflow 等） | 中 | `ServiceInterfaceTests.ServiceImplementations_ShouldImplementInterfaceByName` | Phase 4 |
| SKIP-007 | 控制器未全部以 `Controller` 結尾 | 低 | `ArchitectureTests.ControllerClasses_ShouldEndWithController` | Phase 4 |
| SKIP-008 | 服務層依賴控制器層 | 高 | `ArchitectureTests.Services_ShouldNotDependOnControllers` | Phase 4 |
| DBWARN-001 | `Directory.Build.props` 設 `TreatWarningsAsErrors = false`（39 warnings） | 低 | 分散式 | Phase 4 |

---

## 7. 相關文件

- [架構原則與分層規範](./standards.md)
- [依賴規則矩陣](./dependency-rules.md)
- [Architecture Tests 原始碼](../../tests/ErpBackend.Tests/Architecture/)
- [CI/CD Pipeline 設定](../../.github/workflows/ci.yml)

