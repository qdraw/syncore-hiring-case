using Microsoft.AspNetCore.Mvc;

namespace Cubing.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Index() => Redirect("/scalar");
}
