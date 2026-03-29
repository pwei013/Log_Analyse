using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace LogAnalyse.WebApi.Models.Response;

/// <summary>
/// 响应基类
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// 结果编码，200 表示成功
    /// </summary>
    [JsonProperty("resultCode")]
    [JsonPropertyName("resultCode")]
    public string ResultCode { get; set; } = "200";

    /// <summary>
    /// 结果说明
    /// </summary>
    [JsonProperty("resultMsg")]
    [JsonPropertyName("resultMsg")]
    public string ResultMsg { get; set; } = "OK";
}
