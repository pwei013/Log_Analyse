using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace LogAnalyse.WebApi.Models.Request;

/// <summary>
/// 日志明细查询请求
/// </summary>
public class LogDetailRequest : LogSummaryRequest
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
    /// 页码
    /// </summary>
    [JsonProperty("pageNumber")]
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    [JsonProperty("pageSize")]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}
