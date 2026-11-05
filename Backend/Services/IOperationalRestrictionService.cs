using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Requests;

namespace Backend.Services;

public interface IOperationalRestrictionService
{

    Task<OperationalRestrictionDto> CreateRestrictionAsync(
          CreateRestrictionRequest data);

    Task<OperationalRestrictionDto> GetByIdAsync(Guid id, Guid currentUserId);
    Task<OperationalRestrictionDto> GetByIdAsync(Guid id);
    Task<List<OperationalRestrictionDto>> GetAllAsync(Guid currentUserId);

    Task<OperationalRestrictionDto> GetRestrictionByIdAsync(Guid id);
    Task<List<OperationalRestrictionDto>> GetAllAsync();
    Task<Signature> SignOriginatorAndAdvanceToPendingApprovalAsync(Guid id, Guid signerId);

    Task<List<OperationalRestrictionDto>> GetActiveRestrictionsAsync();

    Task<OperationalRestrictionDto> EditRestrictionAsync(
          Guid id,
          OperationalRestrictionDto updateRestrictionDto);

    Task<OperationalRestrictionDto> SubmitForApprovalAsync(
          Guid id,
          Guid actorId);
    Task<OperationalRestrictionDto> ApproveRestrictionAsync(
          Guid id,
          Guid approverId);

    Task<OperationalRestrictionDto> SubmitForControlRoomSupervisorApprovalAsync(
          Guid id,
          Guid actorId);

    Task<OperationalRestrictionDto> ActivateRestrictionAsync(
          Guid id,
          Guid approverId);

    Task<OperationalRestrictionDto> DenyRestrictionAsync(
          Guid id,
          Guid approverId,
          string reason);

    Task<OperationalRestrictionDto> ReturnForChangesAsync(
          Guid id,
          Guid reviewerId,
          string reason);

    Task<OperationalRestrictionDto> ResubmitRestrictionAsync(
          Guid id,
          Guid actorId);
}
