using Backend.Models.DTOs;
using Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignatureCompletenessController : ApiControllerBase
{
    private readonly SignatureCompletenessService _completenessService;

    public SignatureCompletenessController(SignatureCompletenessService completenessService)
    {
        _completenessService = completenessService;
    }

    /// <summary>
    /// Signature completeness across the CCR population: every operator on shift against
    /// every restriction in force, with every unsigned combination identifiable, the
    /// count of outstanding signatures, and the time the report was computed.
    /// </summary>
    /// <response code="200">Returns the signature completeness report</response>
    [HttpGet]
    public async Task<ActionResult<SignatureCompletenessReportDto>> Get()
    {
        var report = await _completenessService.GetReportAsync();

        return Ok(report);
    }
}
