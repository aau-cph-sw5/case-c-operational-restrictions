using Backend.Models.Entities;

namespace Backend.Services;

public interface ISignatureService
{
    Signature SignOriginator(Guid operationalRestrictionId, Guid signerUserId);
    Signature? GetOriginatorSignature(Guid operationalRestrictionId);
    Signature SignDutyOperationsManager(Guid operationalRestrictionId, Guid signerUserId);
    Signature SignControlRoomSupervisor(Guid operationalRestrictionId, Guid signerUserId);
    Signature SignCancellation(Guid operationalRestrictionId, Guid signerUserId, string? requiredRole);
    List<Guid> GetAtWorkAndOffWorkUserIds();
    void DiscardUnpersistedSignature(Signature signature);
}
