using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Backend.Controllers;
using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Enum;
using Backend.Models.Requests;
using Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests;

public class RestrictionControllerTests
{
    private readonly OperationalRestrictionService _service;

    public RestrictionControllerTests()
    {
        var appDbContext = TestDatabase.CreateSeededContext();
        _service = new OperationalRestrictionService(appDbContext, new SignatureService(appDbContext));
    }

    [Fact]
    public async Task CreateReturns201WithLoggedInUserAsOriginator()
    {
        // CreatedBy in the body is ignored; the originator always comes from the JWT.
        var request = new CreateRestrictionRequest(DbSeeder.LarsId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        var result = await CreateControllerFor(DbSeeder.AnnaId).Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var restriction = Assert.IsType<OperationalRestrictionDto>(created.Value);
        Assert.Equal(DbSeeder.AnnaId, restriction.OriginatorUserId);
        Assert.Equal(RestrictionStatus.Draft, restriction.Status);
    }

    [Fact]
    public async Task CreateWithInvalidPeriodReturns400()
    {
        var request = new CreateRestrictionRequest(Guid.Empty, DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1));

        var result = await CreateControllerFor(DbSeeder.AnnaId).Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByIdOfOwnDraftReturns200()
    {
        var draft = await CreateDraftAsync(DbSeeder.AnnaId);

        var result = await CreateControllerFor(DbSeeder.AnnaId).GetById(draft.Id);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByIdOfOtherUsersDraftReturns404()
    {
        var draft = await CreateDraftAsync(DbSeeder.AnnaId);

        var result = await CreateControllerFor(DbSeeder.LarsId).GetById(draft.Id);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAllDoesNotReturnOtherUsersDrafts()
    {
        await CreateDraftAsync(DbSeeder.AnnaId);

        var result = await CreateControllerFor(DbSeeder.LarsId).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var restrictions = Assert.IsType<List<OperationalRestrictionDto>>(ok.Value);
        Assert.DoesNotContain(restrictions, x => x.Status == RestrictionStatus.Draft && x.OriginatorUserId != DbSeeder.LarsId);
    }

    private RestrictionController CreateControllerFor(Guid userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            "Test"));

        return new RestrictionController(_service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    private Task<OperationalRestrictionDto> CreateDraftAsync(Guid createdBy)
    {
        var request = new CreateRestrictionRequest(createdBy, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        return _service.CreateRestrictionAsync(request);
    }
}
