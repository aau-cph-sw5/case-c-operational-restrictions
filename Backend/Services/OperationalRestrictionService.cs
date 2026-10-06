using Backend.Data;
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enum;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;



public class OperationalRestrictionService(
  AppDbContext appDbContext
) : IOperationalRestrictionService
{
  private readonly AppDbContext _appDbContext = appDbContext;

  /// <summary>
  /// Makes a clone of a previous operational restriction with the fields from the request.
  /// </summary>
  private async Task<OperationalRestrictionDto> UpdateById(UpdateByIdRequest request)
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
      originalRestriction.State = (RestrictionState)request.State;
    }
    if (request.StartDate != null)
    {
      originalRestriction.StartDate = (DateTime)request.StartDate;
    }
    if (request.EndDate != null)
    {
      originalRestriction.EndDate = (DateTime)request.EndDate;
    }

    originalRestriction.Id = Guid.NewGuid();
    originalRestriction.CreatedBy = (Guid)request.EditedBy;

    _appDbContext.Add(originalRestriction);
    await _appDbContext.SaveChangesAsync();

    return OperationalRestrictionDto.FromDalEntity(originalRestriction);
  }
}

public record UpdateByIdRequest(
  Guid Id,
  Guid EditedBy,
  RestrictionState? State,
  DateTime? StartDate,
  DateTime? EndDate
);