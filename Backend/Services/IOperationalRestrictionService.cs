using Backend.Models.DTOs;
using Backend.Models.Requests;

namespace Backend.Services;

public interface IOperationalRestrictionService
{
    Task<OperationalRestrictionDto> CreateRestrictionAsync(CreateRestrictionRequest request);

    Task<OperationalRestrictionDto> GetByIdAsync(Guid id, Guid currentUserId);
    Task<List<OperationalRestrictionDto>> GetAllAsync(Guid currentUserId);
}
