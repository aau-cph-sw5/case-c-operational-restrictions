using Backend.Models.DTOs;
using Backend.Models.Requests;
using Backend.Models.Entities;

namespace Backend.Services;

public interface IOperationalRestrictionService
{
  Task<OperationalRestrictionDto> CreateRestrictionAsync(CreateRestrictionRequest request);

  Task<OperationalRestrictionDto> GetByIdAsync(Guid id);
  Task<List<OperationalRestrictionDto>> GetAllAsync();
}
public interface IOperationalRestrictionServices{

  Task<OperationalRestriction> CreateRestrictionAsync(
        OperationalRestrictionDto data);

  Task<OperationalRestriction> EditRestrictionAsync(
        Guid id,
        OperationalRestrictionDto updateRestrictionDto);

  Task<OperationalRestriction> SubmitForApprovalAsync(
        Guid id,
        OperationalRestrictionDto data);
  Task<OperationalRestriction> ApproveRestrictionAsync(
        Guid id,
        Guid approverId,
        OperationalRestrictionDto data);

  Task<OperationalRestriction> SubmitForControlRoomSupervisorApprovalAsync(
        Guid id,
        OperationalRestrictionDto data);

  Task<OperationalRestriction> ActivateRestrictionAsync(
        Guid id,
        Guid approverId,
        OperationalRestrictionDto data);

  Task<OperationalRestriction> DenyRestrictionAsync(
        Guid id,
        Guid approverId,
        string reason,
        OperationalRestrictionDto data);

  Task<OperationalRestriction> ArchiveRestrictionAsync(Guid id);

  Task<OperationalRestriction> AssignOperatorAsync(
        Guid id,
        Guid operatorId);

  Task<OperationalRestriction> ReturnForChangesAsync(
        Guid id,
        OperationalRestrictionDto data);

  Task<OperationalRestriction> ResubmitRestrictionAsync(
        Guid id,
        OperationalRestrictionDto data);

  Task<OperationalRestriction?> GetRestrictionByIdAsync(Guid id);

  Task<List<OperationalRestriction>> GetActiveRestrictionsAsync();

}