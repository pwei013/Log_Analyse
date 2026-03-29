using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace LogAnalyse.WebApi.Models.Request;

/// <summary>
/// 日志汇总查询请求
/// </summary>
public class LogSummaryRequest : BaseRequest
{
    /// <summary>
    /// 日志根目录，默认扫描项目根目录下的 Logs
    /// </summary>
    [JsonProperty("logsRootPath")]
    [JsonPropertyName("logsRootPath")]
    public string? LogsRootPath { get; set; }

    /// <summary>
    /// 超时阈值，单位毫秒
    /// </summary>
    [JsonProperty("thresholdMs")]
    [JsonPropertyName("thresholdMs")]
    public double ThresholdMs { get; set; } = 200;

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonProperty("startTime")]
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonProperty("endTime")]
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }
}
