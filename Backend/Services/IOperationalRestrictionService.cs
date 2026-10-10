using Backend.Models.DTOs;
using Backend.Models.Requests;

namespace Backend.Services;

public interface IOperationalRestrictionService
{
  Task<OperationalRestrictionDto> CreateRestrictionAsync(CreateRestrictionRequest request);
}
