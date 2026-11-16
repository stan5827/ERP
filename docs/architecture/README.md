# 架構文件

本目錄收錄 ERP 系統之系統架構文件。

## 內容

| 文件 | 說明 |
|------|------|
| [overview.md](overview.md) | 系統架構總覽（多層架構、依賴關係） |
| [ARCHITECTURE_REVIEW.md](ARCHITECTURE_REVIEW.md) | 架構診斷報告（原根目錄） |
| [system-audit-improvement-plan.md](system-audit-improvement-plan.md) | 附加文件與目前程式交叉核對、分階段改善方案 |
| [ai-agent-erp-integration-plan.md](ai-agent-erp-integration-plan.md) | ProMES ERP AI Agent 隱私防護與分階段修改方案 |
| [employee-identity-access-plan.md](employee-identity-access-plan.md) | ERP 使用者、員工身分、角色、權限樹及授權治理規劃 |

## 系統層次總覽

```
+-------------------------------------------------------------+
|  Presentation Layer (Controllers)                           |
|  -> 僅負責 HTTP 請求/回應                                  |
|  -> 依賴 Interfaces                                        |
+-------------------------------------------------------------+
                            |
                            v
+-------------------------------------------------------------+
|  Application Layer (Services)                              |
|  -> 業務邏輯核心                                            |
|  -> 依賴 Domain Entities                                    |
|  -> 實現接口隔離                                            |
+-------------------------------------------------------------+
                            |
                            v
+-------------------------------------------------------------+
|  Domain Layer (Entities)                                   |
|  -> 領域模型                                                |
|  -> 純業務規則                                              |
|  -> 無外部依賴                                              |
+-------------------------------------------------------------+
                            |
                            v
+-------------------------------------------------------------+
|  Infrastructure Layer                                      |
|  -> DbContext / Repository                                  |
|  -> 外部服務集成                                            |
|  -> 依賴倒轉實現                                            |
+-------------------------------------------------------------+
```
