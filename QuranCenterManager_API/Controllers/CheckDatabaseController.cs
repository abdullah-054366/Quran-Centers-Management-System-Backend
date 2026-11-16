using Microsoft.AspNetCore.Mvc;
using Business_Layer;
using Microsoft.AspNetCore.Authorization;
namespace QuranCenterManager_API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "PlatformAdmin,Admin")]
    public class CheckDatabaseController : Controller
    {
        [HttpGet("check")]
        public IActionResult CheckDatabase()
        {
            try
            {
                bool connected = DatabaseCheck.TestDatabase();

                if (connected)
                     
                    return Ok(new { connected = true, message = "تم الاتصال بقاعدة البيانات." });

                return StatusCode(503, new { connected = false, message = "تعذر الاتصال بقاعدة البيانات." });
            }
            catch
            {
                return StatusCode(503, new { connected = false, message = "حدث خطأ أثناء الاتصال بقاعدة البيانات." });
            }
        }
    }
}
