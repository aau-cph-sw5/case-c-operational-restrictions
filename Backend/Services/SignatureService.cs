using Backend.Authorization;
using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SignatureService(
    AppDbContext appDbContext
)
{
    private readonly AppDbContext _appDbContext = appDbContext;
    //Midlertidigt mens der ikke er database
    private static readonly List<RequiresSignature> _requiredSignatures = [];
    //Midlertidigt mens der ikke er database
    private static readonly List<Signature> _signatures = [];

    //Midlertidigt mens der ikke er database
    public Guid GetSomeRandomRequiredSignatureId()
    {
        if (_requiredSignatures.Count == 0)
            CreateRequiredSignatures(Guid.NewGuid());

        return _requiredSignatures[Random.Shared.Next(_requiredSignatures.Count)].Id;
    }


    public List<RequiresSignature> CreateRequiredSignatures(Guid operationalRestrictionId)
    {
        var createdSignatures = _appDbContext.Users
            .Where(user => user.WorkingStatus != UserWorkingStatus.OnHoliday)
            .Select(user => new RequiresSignature
            {
                Id = Guid.NewGuid(),
                OperationalRestrictionId = operationalRestrictionId,
                SigneeId = user.Id,
                RequiresSignatureStatus = RequiresSignatureStatus.PendingSignature
            })
            .ToList();

        //her skal de tilføjes til db i fremtiden
        _requiredSignatures.AddRange(createdSignatures);

        return createdSignatures;
    }


    public RequiresSignature GetRequiresSignatureById(Guid requiresSignatureId)
    {
        //her skal de hentes fra db i fremtiden
        var requiredSignature = _requiredSignatures.FirstOrDefault(required => required.Id == requiresSignatureId);

        if (requiredSignature == null)
            throw new RequiresSignatureNotFoundException(requiresSignatureId);

        return requiredSignature;
    }


    /// <summary>
    /// Creates an immutable signature for the given required signature.
    /// The signer must be the user the signature is required from, and the required
    /// signature must still be unsigned and not invalidated by a newer restriction version.
    /// </summary>
    public Signature Sign(Guid requiresSignatureId, Guid signerUserId)
    {
        var requiredSignature = GetRequiresSignatureById(requiresSignatureId);

        if (requiredSignature.SigneeId != signerUserId)
            throw new NotRequiredSignerException(signerUserId, requiresSignatureId);

        if (requiredSignature.RequiresSignatureStatus != RequiresSignatureStatus.PendingSignature)
            throw new RequiresSignatureNotSignableException(requiresSignatureId, requiredSignature.RequiresSignatureStatus.ToString());

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = requiredSignature.OperationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = string.Join(", ", GetRoleNames(signerUserId)),
            SignedAt = DateTime.UtcNow
        };
        //Dette skal smides i databasen selvfølgelig uden de virtuelle fields
        _signatures.Add(signature);

        //dette skal opdateres i databasen
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;


        return signature;
    }

    public Signature SignOriginator(Guid operationalRestrictionId, Guid signerUserId)
    {
        var existingSignature = _signatures.FirstOrDefault(signature =>
            signature.OperationalRestrictionId == operationalRestrictionId
            && signature.SigneeId == signerUserId
            && signature.SigneeRole == RoleNames.Originator);

        if (existingSignature != null)
        {
            throw new RequiresSignatureNotSignableException(
                existingSignature.RequiresSignatureId,
                RequiresSignatureStatus.CompletedSignature.ToString());
        }

        var requiredSignature = new RequiresSignature
        {
            Id = Guid.NewGuid(),
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            RequiresSignatureStatus = RequiresSignatureStatus.PendingSignature
        };
        _requiredSignatures.Add(requiredSignature);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = RoleNames.Originator,
            SignedAt = DateTime.UtcNow
        };
        _signatures.Add(signature);
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;

        return signature;
    }

    public Signature? GetOriginatorSignature(Guid operationalRestrictionId)
    {
        return _signatures.FirstOrDefault(signature =>
            signature.OperationalRestrictionId == operationalRestrictionId
            && signature.SigneeRole == RoleNames.Originator);
    }

    // The signer's current role names, resolved from the database so the
    // recorded label matches the data-driven role assignments.
    private List<string> GetRoleNames(Guid userId)
    {
        return _appDbContext.UserRoleAssignments
            .Where(assignment => assignment.UserId == userId)
            .Join(_appDbContext.Roles,
                assignment => assignment.RoleId,
                role => role.Id,
                (assignment, role) => role.Name)
            .OrderBy(name => name)
            .ToList();
    }

    /// <summary>
    /// Creates the control room supervisor's signature for implementation (MET-C-009).
    /// Only a user with the ControlroomSupervisor role may sign, and each restriction
    /// can only be signed for implementation once.
    /// </summary>
    public Signature SignControlRoomSupervisor(Guid operationalRestrictionId, Guid signerUserId)
    {
        var signer = _appDbContext.Users.SingleOrDefault(user => user.Id == signerUserId);

        if (signer == null || signer.Role != UserRole.ControlroomSupervisor)
            throw new NotControlRoomSupervisorException(signerUserId);

        var existingSignature = _signatures.FirstOrDefault(signature =>
            signature.OperationalRestrictionId == operationalRestrictionId
            && signature.SigneeRole == UserRole.ControlroomSupervisor);

        if (existingSignature != null)
        {
            throw new RequiresSignatureNotSignableException(
                existingSignature.RequiresSignatureId,
                RequiresSignatureStatus.CompletedSignature.ToString());
        }

        var requiredSignature = new RequiresSignature
        {
            Id = Guid.NewGuid(),
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            RequiresSignatureStatus = RequiresSignatureStatus.PendingSignature
        };
        _requiredSignatures.Add(requiredSignature);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = UserRole.ControlroomSupervisor,
            SignedAt = DateTime.UtcNow
        };
        _signatures.Add(signature);
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;

        return signature;
    }

    public Signature? GetControlRoomSupervisorSignature(Guid operationalRestrictionId)
    {
        return _signatures.FirstOrDefault(signature =>
            signature.OperationalRestrictionId == operationalRestrictionId
            && signature.SigneeRole == UserRole.ControlroomSupervisor);
    }

    /// <summary>
    /// MET-C-009: determines the operators who must read and sign the
    /// restriction at the moment it comes into force, and creates a pending
    /// required signature for each of them. Operators on holiday are skipped.
    /// Returns the created required signatures; their signee ids are the
    /// assigned operators to record on the restriction version.
    /// </summary>
    public List<RequiresSignature> AssignOperatorsForImplementation(Guid operationalRestrictionId)
    {
        var operatorIds = _appDbContext.Users
            .Where(user => user.Role == UserRole.Operator
                && user.WorkingStatus != UserWorkingStatus.OnHoliday)
            .Select(user => user.Id)
            .ToList();

        var requiredSignatures = operatorIds
            .Select(operatorId => new RequiresSignature
            {
                Id = Guid.NewGuid(),
                OperationalRestrictionId = operationalRestrictionId,
                SigneeId = operatorId,
                RequiresSignatureStatus = RequiresSignatureStatus.PendingSignature
            })
            .ToList();

        //her skal de tilføjes til db i fremtiden
        _requiredSignatures.AddRange(requiredSignatures);

        return requiredSignatures;
    }

    internal static void DiscardUnpersistedRequiredSignatures(IEnumerable<Guid> requiresSignatureIds)
    {
        var ids = requiresSignatureIds.ToHashSet();
        _requiredSignatures.RemoveAll(required => ids.Contains(required.Id));
    }

    internal void DiscardUnpersistedSignature(Signature signature)
    {
        var requiredSignature = GetRequiresSignatureById(signature.RequiresSignatureId);

        if (!_signatures.Remove(signature) || !_requiredSignatures.Remove(requiredSignature))
        {
            throw new InvalidOperationException(
                $"Could not discard unpersisted signature {signature.Id}.");
        }
    }



    public void InvalidateRequiredSignatures(Guid operationalRestrictionId)
    {
        //I fremtiden skal den hente alle RequiredSignatures fra OperationalId i db, som skal invalideres
        foreach (var requiredSignature in _requiredSignatures.Where(required => required.OperationalRestrictionId == operationalRestrictionId))
        {
            requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.Invalid;
        }
    }


    public bool IsSignatureValid(Guid signatureId)
    {
        var signature = _signatures.FirstOrDefault(x => x.Id == signatureId);

        if (signature == null)
            throw new SignatureNotFoundException(signatureId);

        return GetRequiresSignatureById(signature.RequiresSignatureId).RequiresSignatureStatus == RequiresSignatureStatus.CompletedSignature;
    }
}
