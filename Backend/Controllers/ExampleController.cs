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

    /// <summary>
    /// Handles an example request and processes it through the service layer.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/example
    ///
    /// This endpoint demonstrates the basic MVCS pattern: Controller receives request,
    /// validates it (if needed), and delegates to the Service layer for business logic.
    /// </remarks>
    /// <response code="200">Returns when the example request was processed successfully</response>
    [HttpPost]
    public IActionResult Post()
    {
        _exampleService.HandleExampleRequest();
        return Ok();
    }
}
