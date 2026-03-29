# Log Analyse

> 一个面向 .NET 日志的可视化分析工具，用于发现慢接口与异常调用。
>
> A .NET log analysis dashboard for slow API detection and error diagnostics.

## 中文说明

### 功能特性

- 慢接口汇总：按 `文件夹 + Method` 统计超时次数、平均耗时、最大耗时
- 明细追踪：查看调用时间、耗时、请求参数、响应参数、结果码、日志文件与行号
- 趋势分析：按分钟聚合慢调用数量，展示慢接口时间分布
- 错误统计：统计时间范围内 `ResultCode != 200` 的调用数量
- 灵活目录：日志目录支持自定义绝对路径（留空默认 `..\Logs`）

### 支持日志格式

#### 1) JSON 行日志

支持提取字段：

- `method`
- `createTime`
- `used_time`
- `custom_field1` / `paramater`（请求）
- `custom_field2` / `result`（响应）

#### 2) 管道分隔日志

示例：

```text
2026-03-28 08:58:37.9829| Account | UserAccountConsume | INFO | {...} | ...
```

支持提取：

- `request.body.decrypt`（优先）或 `request.body`
- `response.body`
- `ElaspedTime`（兼容 `ElapsedTime`）
- `request.url`

请求参数会自动合并 `request.url`，示例：

```json
{
  "request.url": "/841/account/userAccountConsume",
  "request.body": {
    "userID": 1249
  }
}
```

### 技术栈

- .NET 10 Web API
- 原生前端 + Tailwind CSS
- ECharts（图表）
- Highlight.js（JSON 高亮）

### 快速开始

#### 1. 环境要求

- .NET SDK 10.x
- Windows / Linux / macOS

#### 2. 运行项目

```bash
cd LogAnalyse.WebApi
dotnet restore
dotnet run --urls http://localhost:5276
```

浏览器访问：

```text
http://localhost:5276
```

### 配置说明

配置文件：`LogAnalyse.WebApi/appsettings.json`

```json
"LogAnalysis": {
  "DefaultLogsRootPath": "..\\Logs",
  "DefaultThresholdMs": 200
}
```

- `DefaultLogsRootPath`：默认扫描目录
- `DefaultThresholdMs`：默认慢接口阈值（ms）

### API 说明

基础路由：`/LogAnalysis/[action]`

- `GET /LogAnalysis/GetDirectories`：获取可用目录
- `POST /LogAnalysis/GetSummary`：查询慢接口汇总与时间分布
- `POST /LogAnalysis/GetDetails`：查询慢接口明细（分页）

`GetSummary` 请求示例：

```json
{
  "logsRootPath": "D:\\Logs",
  "thresholdMs": 200,
  "startTime": "2026-03-28T00:00:00",
  "endTime": "2026-03-28T23:59:59"
}
```

`GetDetails` 请求示例：

```json
{
  "logsRootPath": "D:\\Logs",
  "thresholdMs": 200,
  "folderName": "Pipe",
  "method": "UserAccountConsume",
  "pageNumber": 1,
  "pageSize": 20
}
```

### 界面预览

你可以在仓库中放置截图后替换下方路径：

- `docs/screenshots/dashboard-overview.png`
- `docs/screenshots/summary-table.png`
- `docs/screenshots/detail-table.png`

```markdown
![Dashboard](docs/screenshots/dashboard-overview.png)
![Summary](docs/screenshots/summary-table.png)
![Detail](docs/screenshots/detail-table.png)
```

### Roadmap

- [ ] 增加 URL 维度慢接口排行
- [ ] 增加结果码筛选与聚合
- [ ] 增加 CSV/Excel 导出
- [ ] 增加按日/周趋势对比

### FAQ

#### 为什么不直接弹系统文件夹选择框并返回绝对路径？

浏览器安全策略限制网页直接读取本机绝对路径，当前采用目录输入方案。

#### 错误数如何统计？

错误数基于时间范围内全部日志，统计 `ResultCode != 200`（兼容 `ResultCode` / `resultCode`）。

## English

### Overview

Log Analyse is a .NET-based log analysis dashboard for:

- slow API discovery
- timeout distribution analysis
- error-rate inspection (`ResultCode != 200`)
- request/response payload tracing

### Quick Start

```bash
cd LogAnalyse.WebApi
dotnet restore
dotnet run --urls http://localhost:5276
```

Open:

```text
http://localhost:5276
```

### Supported Log Formats

- JSON-line logs
- Pipe-separated logs (`timestamp | module | method | level | json | ...`)

### API Endpoints

- `GET /LogAnalysis/GetDirectories`
- `POST /LogAnalysis/GetSummary`
- `POST /LogAnalysis/GetDetails`

### Contribution

Issues and PRs are welcome.  
If you plan major changes, please open an issue first for discussion.

## Project Structure

```text
Log_Analyse
├─ Logs
└─ LogAnalyse.WebApi
   ├─ Controllers
   ├─ Models
   ├─ Services
   └─ wwwroot
```

## License

Please add a `LICENSE` file before publishing (MIT / Apache-2.0 recommended).
