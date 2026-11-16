# 資料庫文件

本目錄收錄 ERP 系統之資料庫設計文件。

## 內容

| 文件 | 說明 |
|------|------|
| [erd.md](erd.md) | 系統架構與資料庫設計（ERD & Data Model） |
| [conventions.md](conventions.md) | 資料表設計規範與審計軌跡 |

## 技術棧

- **資料庫**：PostgreSQL
- **ORM**：Entity Framework Core 8.0
- **提供者**：Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11

## 資料庫連線

透過環境變數 `DB_CONNECTION_STRING` 或 `appsettings.json` 的 `ConnectionStrings:ErpGeneral` 設定。

```bash
# 範例
export DB_CONNECTION_STRING="Host=localhost;Database=ErpGeneral;Username=postgres;Password=xxx"
```

## Migration

使用 EF Core Migrations 管理資料庫結構變更：

```bash
cd src/ErpBackend
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

