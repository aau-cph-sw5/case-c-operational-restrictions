using Backend.Services;

using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// CONTROLLER (MVCS):
// Handles incoming HTTP requests, route binding, and HTTP status codes
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _exampleService;

    public ExampleController(IExampleService exampleService)
    {
        _exampleService = exampleService;
    }

    [HttpPost]
    public IActionResult Post()
    {
        _exampleService.HandleExampleRequest();
        return Ok();
    }
}
