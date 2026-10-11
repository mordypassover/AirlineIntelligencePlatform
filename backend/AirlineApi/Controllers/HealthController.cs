using Microsoft.AspNetCore.Mvc;
[ApiController]public class HealthController:ControllerBase{[HttpGet("health")]public IActionResult Get()=>Ok(new{status="healthy"});}
