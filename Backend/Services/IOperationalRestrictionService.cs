using Backend.Models.DTOs;
using Backend.Models.Requests;
using Backend.Models.Entities;

namespace Backend.Services;

public interface IOperationalRestrictionService{

  Task<OperationalRestrictionDto> CreateRestrictionAsync(
        CreateRestrictionRequest data);
  Task<List<OperationalRestrictionDto>> GetAllAsync();

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

  Task<OperationalRestrictionDto> GetRestrictionByIdAsync(Guid id);

  Task<List<OperationalRestriction>> GetActiveRestrictionsAsync();

}