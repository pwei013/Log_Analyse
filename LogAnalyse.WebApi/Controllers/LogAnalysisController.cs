using LogAnalyse.WebApi.Models.Request;
using LogAnalyse.WebApi.Models.Response;
using LogAnalyse.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LogAnalyse.WebApi.Controllers;

/// <summary>
/// 日志分析控制器
/// </summary>
public class LogAnalysisController : BaseController
{
    private readonly LogAnalysisService _logAnalysisService;

    /// <summary>
    /// 初始化日志分析控制器
    /// </summary>
    /// <param name="logAnalysisService">日志分析服务</param>
    public LogAnalysisController(LogAnalysisService logAnalysisService)
    {
        _logAnalysisService = logAnalysisService;
    }

    /// <summary>
    /// 获取可用的日志目录列表
    /// </summary>
    /// <returns>目录列表</returns>
    [HttpGet]
    public ActionResult<List<string>> GetDirectories()
    {
        return _logAnalysisService.GetAvailableDirectories();
    }

    /// <summary>
    /// 查询超时日志汇总
    /// </summary>
    /// <param name="request">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>汇总结果</returns>
    [HttpPost]
    public async Task<LogSummaryResponse> GetSummary([FromBody] LogSummaryRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return new LogSummaryResponse
            {
                ResultCode = "400",
                ResultMsg = "请求参数不能为空"
            };
        }

        if (request.ThresholdMs < 0)
        {
            return new LogSummaryResponse
            {
                ResultCode = "400",
                ResultMsg = "超时阈值不能小于0"
            };
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime.Value > request.EndTime.Value)
        {
            return new LogSummaryResponse
            {
                ResultCode = "400",
                ResultMsg = "开始时间不能晚于结束时间"
            };
        }

        return await _logAnalysisService.GetSummaryAsync(request, cancellationToken);
    }

    /// <summary>
    /// 查询超时日志明细
    /// </summary>
    /// <param name="request">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>明细结果</returns>
    [HttpPost]
    public async Task<LogDetailResponse> GetDetails([FromBody] LogDetailRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return new LogDetailResponse
            {
                ResultCode = "400",
                ResultMsg = "请求参数不能为空"
            };
        }

        if (string.IsNullOrWhiteSpace(request.FolderName))
        {
            return new LogDetailResponse
            {
                ResultCode = "400",
                ResultMsg = "文件夹名称不能为空"
            };
        }

        if (string.IsNullOrWhiteSpace(request.Method))
        {
            return new LogDetailResponse
            {
                ResultCode = "400",
                ResultMsg = "方法名称不能为空"
            };
        }

        if (request.ThresholdMs < 0)
        {
            return new LogDetailResponse
            {
                ResultCode = "400",
                ResultMsg = "超时阈值不能小于0"
            };
        }

        if (request.PageNumber < 1 || request.PageSize < 1)
        {
            return new LogDetailResponse
            {
                ResultCode = "400",
                ResultMsg = "分页参数不合法"
            };
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime.Value > request.EndTime.Value)
        {
            return new LogDetailResponse
            {
                ResultCode = "400",
                ResultMsg = "开始时间不能晚于结束时间"
            };
        }

        return await _logAnalysisService.GetDetailsAsync(request, cancellationToken);
    }
}
