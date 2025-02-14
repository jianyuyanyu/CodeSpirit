using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Amis.Controllers
{
    public abstract class AmisApiControllerBase : ControllerBase
    {
        [HttpOptions]
        public IActionResult Options() => Ok();
    }
}
