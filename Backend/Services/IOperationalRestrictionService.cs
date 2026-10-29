using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Requests;

namespace Backend.Services;

public interface IOperationalRestrictionService
{

    Task<OperationalRestrictionDto> CreateRestrictionAsync(
          CreateRestrictionRequest data);
    Task<List<OperationalRestrictionDto>> GetAllAsync();

    Task<OperationalRestriction> EditRestrictionAsync(
          Guid id,
          OperationalRestrictionDto updateRestrictionDto);

    Task<OperationalRestriction> SubmitForApprovalAsync(
          Guid id,
          Guid actorId);
    Task<OperationalRestriction> ApproveRestrictionAsync(
          Guid id,
          Guid approverId);

    Task<OperationalRestriction> SubmitForControlRoomSupervisorApprovalAsync(
          Guid id,
          Guid actorId);

    Task<OperationalRestriction> ActivateRestrictionAsync(
          Guid id,
          Guid approverId);

    Task<OperationalRestriction> DenyRestrictionAsync(
          Guid id,
          Guid approverId,
          string reason);

    Task<OperationalRestriction> ReturnForChangesAsync(
          Guid id,
          Guid reviewerId,
          string reason);

    Task<OperationalRestriction> ResubmitRestrictionAsync(
          Guid id,
          Guid actorId);

    Task<OperationalRestrictionDto> GetRestrictionByIdAsync(Guid id);

    Task<List<OperationalRestriction>> GetActiveRestrictionsAsync();

}
