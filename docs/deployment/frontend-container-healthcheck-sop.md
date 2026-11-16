# 前端容器健康檢查診斷與排查 SOP

## 目的與適用範圍

本 SOP 用於診斷以 Nginx 提供 React/Vite SPA 靜態檔案的 Docker 前端容器回報 `unhealthy` 時的原因。目標是保留現場、在容器內重現探測，並依證據進行最小修復；瀏覽器可開啟頁面不代表 Docker health check 一定成功。

適用於本機、測試及 Staging 容器。執行操作前確認容器名稱、Compose project 和埠映射，避免誤操作正式環境。

## 核心原則

1. **先保留現場**：在重啟、移除容器或執行 Compose `down` 前，保存 health 狀態、健康檢查設定及容器日誌。
2. **容器內重現**：在目標容器執行與 Docker health check 相同的命令，確認工具、URL 與 HTTP 回應。
3. **對準同一服務**：確認瀏覽器請求的主機埠確實映射至被檢查的容器。
4. **依證據修復**：不為了讓狀態變成 `healthy` 而盲目放寬 timeout、增加重試次數或新增健康路由。
5. **保護診斷資料**：日誌與診斷輸出可能包含個資或敏感資訊。只存放於受控位置，分享前檢查並遮蔽機密；不要匯出完整環境變數。

## 作業前準備

先取得目標容器名稱或 ID：

```powershell
docker ps -a --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
```

若使用 Docker Compose，請以明確的 project 與 Compose 檔案查詢容器：

```powershell
docker compose -p <compose-project> -f <compose-file> ps -a
```

將以下範例中的 `<container>`、`<compose-project>`、`<compose-file>` 替換為實際值。Staging 專案應使用專屬 project 名稱，避免對其他環境執行命令。

## 診斷流程

### 步驟一：保存健康狀態與容器日誌

在重啟或清理容器前，先輸出 health state、實際健康檢查設定及最近日誌：

```powershell
docker inspect --format '{{json .State.Health}}' <container> |
    Out-File -Encoding utf8 .\frontend-health-state.json

docker inspect --format '{{json .Config.Healthcheck}}' <container> |
    Out-File -Encoding utf8 .\frontend-healthcheck-config.json

docker logs --timestamps --tail 200 <container> 2>&1 |
    Out-File -Encoding utf8 .\frontend-container.log
```

檢查 `frontend-health-state.json` 中：

- `Status`：目前為 `healthy`、`unhealthy` 或 `starting`。
- `FailingStreak`：連續失敗次數。
- `Log`：最近探測的 `Start`、`End`、`ExitCode` 與 `Output`。

並確認 `frontend-healthcheck-config.json` 顯示的命令、間隔、timeout、start period 和重試次數，與預期的映像設定一致。不要使用會輸出 `.Config.Env` 的完整 `docker inspect` 格式。

### 步驟二：確認目標容器與外部埠映射

檢查容器名稱、映像及埠映射：

```powershell
docker inspect --format '{{.Name}} {{.Config.Image}} {{json .NetworkSettings.Ports}}' <container>
```

將映射的主機埠與瀏覽器使用的 URL 比對。若主機埠、Compose project 或映像不符，先確認瀏覽器是否連到另一個容器或本機服務，不要以外部頁面可用作為該容器健康的證據。

### 步驟三：在容器內重現健康檢查

使用非互動方式，直接執行與 Docker health check 相同的命令。若目前 Dockerfile 使用 `wget -qO- http://127.0.0.1/`：

```powershell
docker exec <container> sh -c 'command -v wget && wget -qO- http://127.0.0.1/ >/dev/null'
```

以 `$LASTEXITCODE` 判斷指令結果。需要觀察 HTTP 回應時，可執行：

```powershell
docker exec <container> sh -c 'wget -S -O /dev/null http://127.0.0.1/'
```

如 `wget` 不存在或該映像的 `wget` 不支援相關參數，先確認健康檢查實際使用的完整命令及其 exit code；不要只因為基礎映像通常包含該工具就假定它可用。

### 步驟四：檢查 Nginx 與靜態檔案

容器存活且可以執行命令時，檢查 Nginx 設定及入口檔：

```powershell
docker exec <container> nginx -t
docker exec <container> sh -c 'test -s /usr/share/nginx/html/index.html'
```

將 HTTP 回應與 Nginx 設定對照，確認 `root` 指向實際建置產物目錄、根路徑 `/` 可回應成功，以及 SPA 路由 fallback 是否符合目前設定。若容器已停止，改為檢查映像建置輸出和 Dockerfile，不要在容器啟動失敗時忽略該訊號。

## 依證據採取修復

| 證據 | 優先檢查 | 修復方向 |
|---|---|---|
| health log 顯示 `wget: not found` 或命令參數錯誤 | 映像內容與實際 Healthcheck 命令 | 改用映像內已存在且驗證過的探測方式；若需安裝工具，確認增加的套件與映像維護策略相容。 |
| 探測連線被拒絕或回應非 2xx | Nginx 進程、監聽埠、Nginx error log | 修復 Nginx 啟動、監聽或設定問題；確認探測 URL 指向容器內的正確服務。 |
| `/` 回傳 404 或入口檔缺失 | `index.html` 建置產物、Dockerfile 的 COPY 路徑、Nginx `root` | 修正建置或複製路徑，讓有效產物位於 Nginx 預期位置。 |
| 靜態檔可用但 SPA 深層路由失敗 | Nginx `try_files` 等 fallback 設定 | 依 SPA 路由需求修正 fallback，並同時驗證 `/`、靜態資源與一個深層路由。 |
| 有重現的延遲或 timeout 證據 | health log 的耗時、主機資源、啟動時間 | 先排除資源或啟動問題；只有證據顯示探測時間門檻不足時，才調整 timeout 或 start period。 |
| 容器內探測成功但 Docker 狀態仍不健康 | `.Config.Healthcheck`、探測時間點、容器狀態 | 比對實際執行命令與手動命令，查看最新 health log；確認是否檢查了錯誤容器或舊映像。 |

一次只作必要修改，並記錄修改前後的 health check 設定與證據。

## 修復後驗收

1. 重新建置指定前端映像並啟動隔離的 Staging Compose project。
2. 使用 `docker inspect` 確認同一目標容器的 health `Status` 轉為 `healthy`，且最新 `Log` 中 `ExitCode` 為 `0`。
3. 在容器內再次執行健康檢查命令，確認 exit code 為 `0`；使用瀏覽器對照相同的主機埠驗證首頁、靜態資源及 SPA 深層路由。
4. 如有反向代理，另驗證一項必要 API 請求，確認前端對後端的代理路徑可用。
5. 保存驗收結果與映像識別資訊後，才清理 Staging。清理時明確指定 Staging Compose project；移除資料 volume 前再次確認其為隔離測試資料。

若狀態仍為 `unhealthy`，不要以瀏覽器 smoke test 通過取代 health check 驗收；保留新一輪 health log 與容器日誌，再依本 SOP 回到診斷流程。

## 診斷紀錄建議欄位

- 日期、執行者、環境及 Compose project
- 容器名稱、映像 tag/digest、埠映射
- health status、failing streak、最近探測 exit code/output
- 實際健康檢查命令與容器內手動重現結果
- Nginx 設定檢查、首頁及靜態檔案回應
- 根因、最小修改、修改後驗收結果
- 診斷輸出存放位置及敏感資料遮蔽確認
