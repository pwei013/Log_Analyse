using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace LogAnalyse.WebApi.Models.Response;

/// <summary>
/// 日志汇总查询响应
/// </summary>
public class LogSummaryResponse : BaseResponse
{
    /// <summary>
    /// 扫描文件数
    /// </summary>
    [JsonProperty("scannedFileCount")]
    [JsonPropertyName("scannedFileCount")]
    public int ScannedFileCount { get; set; }

    /// <summary>
    /// 总日志条数
    /// </summary>
    [JsonProperty("totalLogCount")]
    [JsonPropertyName("totalLogCount")]
    public int TotalLogCount { get; set; }

    /// <summary>
    /// 超时日志条数
    /// </summary>
    [JsonProperty("timeoutLogCount")]
    [JsonPropertyName("timeoutLogCount")]
    public int TimeoutLogCount { get; set; }

    /// <summary>
    /// 错误日志条数 (ResultCode != 200)
    /// </summary>
    [JsonProperty("errorLogCount")]
    [JsonPropertyName("errorLogCount")]
    public int ErrorLogCount { get; set; }

    /// <summary>
    /// 汇总结果
    /// </summary>
    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public List<LogSummaryItemResponse> Items { get; set; } = [];

    /// <summary>
    /// 时间分布统计
    /// </summary>
    [JsonProperty("timeDistribution")]
    [JsonPropertyName("timeDistribution")]
    public List<LogTimeDistributionItemResponse> TimeDistribution { get; set; } = [];
}

/// <summary>
/// 日志汇总项
/// </summary>
public class LogSummaryItemResponse
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    [JsonProperty("folderName")]
    [JsonPropertyName("folderName")]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 方法名称
    /// </summary>
    [JsonProperty("method")]
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 超时调用次数
    /// </summary>
    [JsonProperty("timeoutCount")]
    [JsonPropertyName("timeoutCount")]
    public int TimeoutCount { get; set; }

    /// <summary>
    /// 平均耗时，单位毫秒
    /// </summary>
    [JsonProperty("averageUsedTimeMs")]
    [JsonPropertyName("averageUsedTimeMs")]
    public double AverageUsedTimeMs { get; set; }

    /// <summary>
    /// 错误次数
    /// </summary>
    [JsonProperty("errorCount")]
    [JsonPropertyName("errorCount")]
    public int ErrorCount { get; set; }

    /// <summary>
    /// 最大耗时，单位毫秒
    /// </summary>
    [JsonProperty("maxUsedTimeMs")]
    [JsonPropertyName("maxUsedTimeMs")]
    public double MaxUsedTimeMs { get; set; }

    /// <summary>
    /// 最近调用时间
    /// </summary>
    [JsonProperty("lastCallTime")]
    [JsonPropertyName("lastCallTime")]
    public DateTime? LastCallTime { get; set; }
}

/// <summary>
/// 时间分布明细
/// </summary>
public class LogTimeDistributionItemResponse
{
    /// <summary>
    /// 时间点 (格式 yyyy-MM-dd HH:mm)
    /// </summary>
    [JsonProperty("time")]
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// 超时次数
    /// </summary>
    [JsonProperty("count")]
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
