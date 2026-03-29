using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace LogAnalyse.WebApi.Models.Response;

/// <summary>
/// 日志明细查询响应
/// </summary>
public class LogDetailResponse : BaseResponse
{
    /// <summary>
    /// 总条数
    /// </summary>
    [JsonProperty("totalCount")]
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    [JsonProperty("pageNumber")]
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    /// <summary>
    /// 每页条数
    /// </summary>
    [JsonProperty("pageSize")]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// 明细列表
    /// </summary>
    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public List<LogDetailItemResponse> Items { get; set; } = [];
}

/// <summary>
/// 日志明细项
/// </summary>
public class LogDetailItemResponse
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    [JsonProperty("folderName")]
    [JsonPropertyName("folderName")]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    [JsonProperty("filePath")]
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件行号
    /// </summary>
    [JsonProperty("lineNumber")]
    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }

    /// <summary>
    /// 方法名称
    /// </summary>
    [JsonProperty("method")]
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 调用时间
    /// </summary>
    [JsonProperty("callTime")]
    [JsonPropertyName("callTime")]
    public DateTime? CallTime { get; set; }

    /// <summary>
    /// 耗时，单位毫秒
    /// </summary>
    [JsonProperty("usedTimeMs")]
    [JsonPropertyName("usedTimeMs")]
    public double UsedTimeMs { get; set; }

    /// <summary>
    /// 请求负载
    /// </summary>
    [JsonProperty("requestPayload")]
    [JsonPropertyName("requestPayload")]
    public string RequestPayload { get; set; } = string.Empty;

    /// <summary>
    /// 响应负载
    /// </summary>
    [JsonProperty("responsePayload")]
    [JsonPropertyName("responsePayload")]
    public string ResponsePayload { get; set; } = string.Empty;

    /// <summary>
    /// 结果码
    /// </summary>
    [JsonProperty("resultCode")]
    [JsonPropertyName("resultCode")]
    public string ResultCode { get; set; } = string.Empty;
}
