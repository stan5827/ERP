# ERP 系統文件總覽

本目錄彙整 ERP 系統之技術與專案文件，提供團隊統一的知識入口。

> 全專案 Markdown 文件分類索引請見[文件總覽](./文件總覽.md)。

## 目錄結構

```
docs/
├── README.md              # 本文件（文件索引）
├── api/                   # API 規格與介面文件
│   ├── README.md          # API 概覽
│   ├── overview.md        # API 設計規範與相容性管理
│   └── endpoints.md       # 核心模組 API 端點
├── database/              # 資料庫設計文件
│   ├── README.md          # 資料庫概覽
│   ├── erd.md             # ERD 與資料模型
│   └── conventions.md     # 資料表設計規範
├── deployment/            # 部署與維運作業程序
│   ├── frontend-container-healthcheck-sop.md  # 前端容器健康檢查 SOP
│   └── erp-uat-checklist.md  # ERP 使用者驗收檢核清單
└── architecture/          # 系統架構文件
    ├── README.md          # 架構概覽
    └── ARCHITECTURE_REVIEW.md  # 架構診斷報告（原根目錄）
```

## 文件對應來源

| 本目錄 | 原始文件（src/docs） |
|--------|----------------------|
| `docs/api/` | `05_API_Specifications.md` |
| `docs/database/` | `04_System_Architecture_and_ERD.md` |
| `docs/architecture/` | `ARCHITECTURE_REVIEW.md`、`ERP 軟體架構圖.md` |

## 核心文件連結

### 專案管理
- [01_Project_Charter_and_Plan](../src/docs/01_Project_Charter_and_Plan.md)
- [07_Go_Live_and_Migration_Plan](../src/docs/07_Go_Live_and_Migration_Plan.md)
- [08_Refactoring_and_Optimization_Roadmap](../src/docs/08_Refactoring_and_Optimization_Roadmap.md)

### 需求與藍圖
- [02_Business_Blueprint](../src/docs/02_Business_Blueprint.md)
- [03_Functional_Requirement_Document](../src/docs/03_Functional_Requirement_Document.md)
- [ERP系統業務流程](../src/docs/ERP系統業務流程.md)

### 技術規格
- [05_API_Specifications](../src/docs/05_API_Specifications.md)
- [04_System_Architecture_and_ERD](../src/docs/04_System_Architecture_and_ERD.md)
- [06_Test_Plan_and_Cases](../src/docs/06_Test_Plan_and_Cases.md)

### 部門操作手冊
- [各部門ERP系統操作手冊](../src/docs/各部門ERP系統操作手冊.md)
- [ERP_前端操作手冊](../src/docs/ERP_前端操作手冊.md)
- [BOM操作手冊](../src/docs/BOM操作手冊.md)
- [庫存管理執行流程](../src/docs/庫存管理執行流程.md)

### 部署與維運
- [前端容器健康檢查診斷與排查 SOP](./deployment/frontend-container-healthcheck-sop.md)
- [ERP 使用者驗收（UAT）檢核清單](./deployment/erp-uat-checklist.md)

## 工具與腳本

- [tools/scripts](../tools/scripts/) - 文件產生腳本（generate-docx / generate-pptx）
