using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class WeatherForecastController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public WeatherForecastController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "WeatherForecastController is currently being repaired." });
}

