
using Backend.Authorization;
using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SignatureService(
    AppDbContext appDbContext,
    INotificationService notificationService
)
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly INotificationService _notificationService = notificationService;

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


    // The users who must read and sign a restriction when it becomes active:
    // everyone at work or off work at that moment (i.e. not on holiday).
    public List<Guid> GetAtWorkAndOffWorkUserIds()
    {
        return _appDbContext.Users
            .Where(user => user.WorkingStatus == UserWorkingStatus.AtWork
                        || user.WorkingStatus == UserWorkingStatus.OffWork)
            .Select(user => user.Id)
            .ToList();
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

        foreach (var requiredSignature in createdSignatures)
        {
            _notificationService.AddNotification(
                new CreateNotificationRequest(
                    RecipientUserId: requiredSignature.SigneeId,
                    Type: NotificationType.SignatureRequired,
                    RequiresAction: true,
                    OperationalRestrictionId: requiredSignature.OperationalRestrictionId
                )
            );
        }

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

    public Signature SignDutyOperationsManager(Guid operationalRestrictionId, Guid signerUserId)
    {
        if (!GetRoleNames(signerUserId).Contains(RoleNames.DutyOperationsManager))
            throw new NotDutyOperationsManagerException(signerUserId);

        if (_signatures.Any(signature =>
                signature.OperationalRestrictionId == operationalRestrictionId
                && signature.SigneeRole.Split(", ")
                    .Contains(RoleNames.DutyOperationsManager)))
        {
            throw new InvalidOperationException(
                "A duty operations manager has already signed this restriction.");
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
            SigneeRole = string.Join(", ", GetRoleNames(signerUserId)),
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

    /// <summary>
    /// Creates the control room supervisor's signature for implementation.
    /// Only a user with the ControlroomSupervisor role may sign, and each restriction.
    /// Can only be signed once.
    /// </summary>

    public Signature SignControlRoomSupervisor(Guid operationalRestrictionId, Guid signerUserId)
    {
        var signer = _appDbContext.Users.SingleOrDefault(user => user.Id == signerUserId);

        if (signer == null || !GetRoleNames(signerUserId).Contains(RoleNames.ControlroomSupervisor))
            throw new NotControlRoomSupervisorException(signerUserId);

        var existingSignature = _signatures.FirstOrDefault(
            signature => signature.OperationalRestrictionId == operationalRestrictionId
                         && signature.SigneeRole == RoleNames.ControlroomSupervisor);

        if (existingSignature != null)
            throw new InvalidOperationException("Signature already exists.");

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
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = RoleNames.ControlroomSupervisor,
            RequiresSignatureId = requiredSignature.Id,
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
            && signature.SigneeRole == RoleNames.ControlroomSupervisor
        );
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

    public void DiscardUnpersistedSignature(Signature signature)
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
