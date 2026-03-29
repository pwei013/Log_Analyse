using Microsoft.AspNetCore.Mvc;

namespace LogAnalyse.WebApi.Controllers;

/// <summary>
/// 控制器基类
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
public class BaseController : ControllerBase
{
}
