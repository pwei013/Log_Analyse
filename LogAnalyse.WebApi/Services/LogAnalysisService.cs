using LogAnalyse.WebApi.Models.Request;
using LogAnalyse.WebApi.Models.Response;
using System.Globalization;
using System.Text.Json;

namespace LogAnalyse.WebApi.Services;

/// <summary>
/// 日志分析服务
/// </summary>
public class LogAnalysisService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// 初始化日志分析服务
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="hostEnvironment">主机环境对象</param>
    public LogAnalysisService(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// 查询超时日志汇总
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>汇总结果</returns>
    public async Task<LogSummaryResponse> GetSummaryAsync(LogSummaryRequest request, CancellationToken cancellationToken)
    {
        var records = await ScanRecordsAsync(request, cancellationToken);
        var timeoutRecords = records.Where(x => x.UsedTimeMs > request.ThresholdMs).ToList();
        var errorRecords = records.Where(x => !string.IsNullOrEmpty(x.ResultCode) && x.ResultCode != "200").ToList();
        var groupedItems = timeoutRecords
            .GroupBy(x => new { x.FolderName, x.Method })
            .Select(group => new LogSummaryItemResponse
            {
                FolderName = group.Key.FolderName,
                Method = group.Key.Method,
                TimeoutCount = group.Count(),
                ErrorCount = group.Count(item => !string.IsNullOrEmpty(item.ResultCode) && item.ResultCode != "200"),
                AverageUsedTimeMs = Math.Round(group.Average(item => item.UsedTimeMs), 2),
                MaxUsedTimeMs = Math.Round(group.Max(item => item.UsedTimeMs), 2),
                LastCallTime = group.Max(item => item.CallTime)
            })
            .OrderByDescending(x => x.TimeoutCount)
            .ThenByDescending(x => x.MaxUsedTimeMs)
            .ThenBy(x => x.FolderName)
            .ThenBy(x => x.Method)
            .ToList();

        var timeDistribution = timeoutRecords
            .Where(x => x.CallTime.HasValue)
            .GroupBy(x => x.CallTime!.Value.ToString("yyyy-MM-dd HH:mm"))
            .Select(group => new LogTimeDistributionItemResponse
            {
                Time = group.Key,
                Count = group.Count()
            })
            .OrderBy(x => x.Time)
            .ToList();

        return new LogSummaryResponse
        {
            ScannedFileCount = records.Select(x => x.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalLogCount = records.Count,
            TimeoutLogCount = timeoutRecords.Count,
            ErrorLogCount = errorRecords.Count,
            Items = groupedItems,
            TimeDistribution = timeDistribution
        };
    }

    /// <summary>
    /// 查询超时日志明细
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>明细结果</returns>
    public async Task<LogDetailResponse> GetDetailsAsync(LogDetailRequest request, CancellationToken cancellationToken)
    {
        var records = await ScanRecordsAsync(request, cancellationToken);
        var filteredRecords = records
            .Where(x => x.UsedTimeMs > request.ThresholdMs)
            .Where(x => string.Equals(x.FolderName, request.FolderName, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.Equals(x.Method, request.Method, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CallTime ?? DateTime.MinValue)
            .ThenByDescending(x => x.UsedTimeMs)
            .ToList();

        var totalCount = filteredRecords.Count;
        var pageItems = filteredRecords
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LogDetailItemResponse
            {
                FolderName = x.FolderName,
                FilePath = x.FilePath,
                LineNumber = x.LineNumber,
                Method = x.Method,
                CallTime = x.CallTime,
                UsedTimeMs = Math.Round(x.UsedTimeMs, 2),
                RequestPayload = x.RequestPayload,
                ResponsePayload = x.ResponsePayload,
                ResultCode = x.ResultCode
            })
            .ToList();

        return new LogDetailResponse
        {
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Items = pageItems
        };
    }

    /// <summary>
    /// 获取可用的日志目录列表
    /// </summary>
    /// <returns>目录列表</returns>
    public List<string> GetAvailableDirectories()
    {
        var logsRoot = ResolveLogsRootPath(null);
        if (!Directory.Exists(logsRoot))
        {
            return [];
        }

        var directories = new List<string> { logsRoot };
        try
        {
            directories.AddRange(Directory.GetDirectories(logsRoot, "*", SearchOption.AllDirectories));
        }
        catch
        {
            // 忽略权限等异常
        }

        return directories;
    }

    private async Task<List<LogRecord>> ScanRecordsAsync(LogSummaryRequest request, CancellationToken cancellationToken)
    {
        var logsRoot = ResolveLogsRootPath(request.LogsRootPath);
        if (!Directory.Exists(logsRoot))
        {
            return [];
        }

        var files = Directory
            .EnumerateFiles(logsRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".log", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".info", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var records = new List<LogRecord>(capacity: Math.Max(files.Count * 10, 64));
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folderName = GetFolderName(logsRoot, Path.GetDirectoryName(file) ?? logsRoot);
            await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lineNumber = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var jsonLine = NormalizeJsonLine(line);
                if (string.IsNullOrWhiteSpace(jsonLine))
                {
                    continue;
                }

                if (!TryParseLine(jsonLine, file, folderName, lineNumber, out var record))
                {
                    continue;
                }

                if (request.StartTime.HasValue && record.CallTime.HasValue && record.CallTime.Value < request.StartTime.Value)
                {
                    continue;
                }

                if (request.EndTime.HasValue && record.CallTime.HasValue && record.CallTime.Value > request.EndTime.Value)
                {
                    continue;
                }

                records.Add(record);
            }
        }

        return records;
    }

    private string ResolveLogsRootPath(string? inputPath)
    {
        var configuredPath = _configuration["LogAnalysis:DefaultLogsRootPath"];
        var candidate = string.IsNullOrWhiteSpace(inputPath) ? configuredPath : inputPath;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = "..\\Logs";
        }

        if (Path.IsPathRooted(candidate))
        {
            return candidate;
        }

        var combinedPath = Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, candidate));
        return combinedPath;
    }

    private static bool TryParseLine(string line, string filePath, string folderName, int lineNumber, out LogRecord record)
    {
        record = default!;
        if (TryParseJsonLine(line, filePath, folderName, lineNumber, out record))
        {
            return true;
        }

        return TryParsePipeLine(line, filePath, folderName, lineNumber, out record);
    }

    private static bool TryParseJsonLine(string jsonLine, string filePath, string folderName, int lineNumber, out LogRecord record)
    {
        record = default!;
        try
        {
            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;
            var method = GetStringValue(root, "method");
            if (string.IsNullOrWhiteSpace(method))
            {
                return false;
            }

            var usedTimeMs = GetDoubleValue(root, "used_time");
            var callTime = GetDateTimeValue(root, "createTime");
            var requestPayload = ExtractRequestPayload(root);
            var requestUrl = GetStringValue(root, "request.url");
            requestPayload = MergeRequestPayloadWithUrl(requestPayload, requestUrl);
            var responsePayload = ExtractResponsePayload(root);
            var resultCode = ExtractResultCode(responsePayload);

            record = new LogRecord
            {
                FolderName = folderName,
                FilePath = filePath,
                LineNumber = lineNumber,
                Method = method,
                CallTime = callTime,
                UsedTimeMs = usedTimeMs,
                RequestPayload = requestPayload,
                ResponsePayload = responsePayload,
                ResultCode = resultCode
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParsePipeLine(string line, string filePath, string folderName, int lineNumber, out LogRecord record)
    {
        record = default!;
        var segments = line.Split('|');
        if (segments.Length < 5)
        {
            return false;
        }

        var callTimeSegment = segments[0].Trim();
        var methodSegment = segments[2].Trim();
        var detailJsonSegment = segments[4].Trim();
        if (string.IsNullOrWhiteSpace(methodSegment) || string.IsNullOrWhiteSpace(detailJsonSegment))
        {
            return false;
        }

        DateTime? callTime = null;
        if (DateTime.TryParse(callTimeSegment, out var parsedCallTime))
        {
            callTime = parsedCallTime;
        }

        try
        {
            using var document = JsonDocument.Parse(detailJsonSegment);
            var root = document.RootElement;
            var requestPayload = GetStringValue(root, "request.body.decrypt");
            if (string.IsNullOrWhiteSpace(requestPayload))
            {
                requestPayload = GetStringValue(root, "request.body");
            }
            var requestUrl = GetStringValue(root, "request.url");
            requestPayload = MergeRequestPayloadWithUrl(requestPayload, requestUrl);

            var responsePayload = GetStringValue(root, "response.body");
            var usedTimeMs = GetDoubleValue(root, "ElaspedTime");
            if (usedTimeMs <= 0)
            {
                usedTimeMs = GetDoubleValue(root, "ElapsedTime");
            }

            if (!callTime.HasValue)
            {
                callTime = GetDateTimeValue(root, "StartTime");
            }

            var resultCode = ExtractResultCode(responsePayload);
            if (string.IsNullOrWhiteSpace(resultCode))
            {
                resultCode = GetStringValue(root, "resultCode");
            }

            record = new LogRecord
            {
                FolderName = folderName,
                FilePath = filePath,
                LineNumber = lineNumber,
                Method = methodSegment,
                CallTime = callTime,
                UsedTimeMs = usedTimeMs,
                RequestPayload = requestPayload,
                ResponsePayload = responsePayload,
                ResultCode = resultCode
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string MergeRequestPayloadWithUrl(string requestPayload, string requestUrl)
    {
        if (string.IsNullOrWhiteSpace(requestUrl))
        {
            return requestPayload;
        }

        if (string.IsNullOrWhiteSpace(requestPayload))
        {
            return $"{{\"request.url\":{JsonSerializer.Serialize(requestUrl)}}}";
        }

        try
        {
            using var payloadDocument = JsonDocument.Parse(requestPayload);
            if (payloadDocument.RootElement.ValueKind == JsonValueKind.Object)
            {
                return $"{{\"request.url\":{JsonSerializer.Serialize(requestUrl)},\"request.body\":{requestPayload}}}";
            }
        }
        catch
        {
        }

        return $"{{\"request.url\":{JsonSerializer.Serialize(requestUrl)},\"request.body\":{JsonSerializer.Serialize(requestPayload)}}}";
    }

    private static string ExtractResultCode(string responsePayload)
    {
        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            return string.Empty;
        }

        try
        {
            // 尝试解析为JSON提取ResultCode或resultCode
            using var document = JsonDocument.Parse(responsePayload);
            var root = document.RootElement;
            if (root.TryGetProperty("ResultCode", out var codeElement) || root.TryGetProperty("resultCode", out codeElement))
            {
                return codeElement.ToString();
            }
        }
        catch
        {
            // 非合法JSON则忽略
        }

        return string.Empty;
    }

    private static string ExtractRequestPayload(JsonElement root)
    {
        if (root.TryGetProperty("custom_field1", out var customField1))
        {
            if (customField1.ValueKind == JsonValueKind.Object)
            {
                if (customField1.TryGetProperty("inputParams", out var inputParams))
                {
                    return inputParams.ToString();
                }

                return customField1.ToString();
            }

            if (customField1.ValueKind is JsonValueKind.String)
            {
                var value = customField1.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        if (root.TryGetProperty("paramater", out var parameterValue))
        {
            if (parameterValue.ValueKind == JsonValueKind.Object)
            {
                if (parameterValue.TryGetProperty("inputParams", out var inputParams))
                {
                    return inputParams.ToString();
                }

                return parameterValue.ToString();
            }

            return parameterValue.ToString();
        }

        return string.Empty;
    }

    private static string ExtractResponsePayload(JsonElement root)
    {
        if (root.TryGetProperty("custom_field2", out var customField2))
        {
            var value = customField2.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        if (root.TryGetProperty("result", out var result))
        {
            return result.ToString();
        }

        return string.Empty;
    }

    private static string NormalizeJsonLine(string line)
    {
        var normalized = line.Trim();
        while (normalized.StartsWith(",", StringComparison.Ordinal))
        {
            normalized = normalized[1..].TrimStart();
        }

        return normalized;
    }

    private static string GetFolderName(string logsRootPath, string directoryPath)
    {
        var relativePath = Path.GetRelativePath(logsRootPath, directoryPath);
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
        {
            return "ROOT";
        }

        return relativePath.Replace('\\', '/');
    }

    private static string GetStringValue(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return string.Empty;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static double GetDoubleValue(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return 0;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String when double.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue) => parsedValue,
            _ => 0
        };
    }

    private static DateTime? GetDateTimeValue(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), out var parsedDateTime))
        {
            return parsedDateTime;
        }

        return null;
    }

    private sealed class LogRecord
    {
        public required string FolderName { get; init; }

        public required string FilePath { get; init; }

        public required int LineNumber { get; init; }

        public required string Method { get; init; }

        public required DateTime? CallTime { get; init; }

        public required double UsedTimeMs { get; init; }

        public required string RequestPayload { get; init; }

        public required string ResponsePayload { get; init; }

        public required string ResultCode { get; init; }
    }
}
