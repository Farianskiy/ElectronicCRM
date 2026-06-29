using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { message = "TestController works!", time = DateTimeOffset.UtcNow });

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id) =>
        Ok(new { id, message = $"Item {id} found" });
}