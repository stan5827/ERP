# API 文件

本目錄收錄 ERP 系統之 API 規格與介面文件。

## 內容

| 文件 | 說明 |
|------|------|
| [overview.md](overview.md) | API 設計規範、版本控管、回應格式、認證授權 |
| [endpoints.md](endpoints.md) | 核心模組 API 端點清單 |

## 快速連結

- [Swagger UI（開發環境）](http://localhost:5100/swagger)
- [健康檢查](http://localhost:5100/health)
- [指標監控](http://localhost:5100/metrics)

## API 設計原則

1. **RESTful 架構**：前後端分離，標準 JSON 交換。
2. **版本控管**：Base Path 強制包含版本號 `/api/v1/...`。
3. **統一回應格式**：`ApiResponse` 包裝 `{ success, message, data }`。
4. **認證授權**：JWT Bearer Token + RBAC 原則。
5. **日誌與監控**：Serilog 集中式日誌、Prometheus 指標收集。

