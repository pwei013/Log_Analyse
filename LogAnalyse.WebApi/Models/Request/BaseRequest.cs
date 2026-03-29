using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace LogAnalyse.WebApi.Models.Request;

/// <summary>
/// 请求基类
/// </summary>
public class BaseRequest
{
    /// <summary>
    /// 请求追踪标识
    /// </summary>
    [JsonProperty("requestId")]
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }
}
