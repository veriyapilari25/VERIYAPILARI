using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VERIYAPILARI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrgChartController : ControllerBase
    {
    }
} 