using Backend.Data;
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class OperationalRestrictionService(
  AppDbContext appDbContext
) : IOperationalRestrictionService
{
  private readonly AppDbContext _appDbContext = appDbContext;

  /// <summary>
  /// Creates the first version of a new operational restriction as a draft.
  /// The creator is also recorded as the originator.
  /// </summary>
  public async Task<OperationalRestrictionDto> CreateRestrictionAsync(CreateRestrictionRequest request)
  {
    if (request.EndDate <= request.StartDate)
      throw new InvalidRestrictionPeriodException(request.StartDate, request.EndDate);

    var restriction = new OperationalRestriction
    {
      Id = Guid.NewGuid(),
      OriginatorUserId = request.CreatedBy,
      CreatedBy = request.CreatedBy,
      State = RestrictionState.Draft,
      StartDate = request.StartDate,
      EndDate = request.EndDate,
      CreatedAt = DateTime.UtcNow,
      OriginalOperationalRestrictionId = null
    };

    _appDbContext.Add(restriction);
    await _appDbContext.SaveChangesAsync();

    return OperationalRestrictionDto.FromDalEntity(restriction);
  }

  /// <summary>
  /// Makes a clone of a previous operational restriction with the fields from the request.
  /// </summary>
  private async Task<OperationalRestrictionDto> UpdateByIdAsync(UpdateByIdRequest request)
  {
    var originalRestriction = await _appDbContext.OperationalRestrictions
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == request.Id);

    if (originalRestriction == null)
      throw new OperationalRestrictionNotFoundException(request.Id);

    if (originalRestriction.OriginalOperationalRestrictionId == null)
    {
      originalRestriction.OriginalOperationalRestrictionId = request.Id;
    }
    
    if (request.State != null)
    {
      originalRestriction.State = request.State.Value;
    }
    if (request.StartDate != null)
    {
      originalRestriction.StartDate = request.StartDate.Value;
    }
    if (request.EndDate != null)
    {
      originalRestriction.EndDate = request.EndDate.Value;
    }

    originalRestriction.Id = Guid.NewGuid();
    originalRestriction.CreatedBy = request.EditedBy;

    _appDbContext.Add(originalRestriction);
    await _appDbContext.SaveChangesAsync();

    return OperationalRestrictionDto.FromDalEntity(originalRestriction);
  }
}

