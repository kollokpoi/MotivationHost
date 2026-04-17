using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Motivation.Controllers.MobileApi
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SystemController : Controller
    {
        private readonly ILogger<EmployeesController> _logger;

        public SystemController(ILogger<EmployeesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task Get(string systemName, int userId, int deviceId, int pushToken)
        {
            try
            {
                var appData = new
                {
                    app = new
                    {
                        last_version = "1.0.0",
                        update_title = string.Empty,
                        update_details = string.Empty,
                        update_url = string.Empty,
                        system_name = systemName,
                        user_id = userId,
                        device_id = deviceId,
                        push_token = pushToken,
                    },
                };
                var json = JsonConvert.SerializeObject(appData);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying get system data:\n {e}";
                _logger.LogError(exceptionString);
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }
    }
}
