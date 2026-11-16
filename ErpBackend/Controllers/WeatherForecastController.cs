using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class WeatherForecastController : BaseController
{
    public WeatherForecastController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "WeatherForecastController is currently being repaired." });
}

