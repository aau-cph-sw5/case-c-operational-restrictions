using Backend.Authorization;
using Backend.Data;
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SignatureService(
    AppDbContext appDbContext
) : ISignatureService
{
    private readonly AppDbContext _appDbContext = appDbContext;

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

        _appDbContext.RequiresSignatures.AddRange(createdSignatures);
        _appDbContext.SaveChanges();

        return createdSignatures;
    }


    public RequiresSignature GetRequiresSignatureById(Guid requiresSignatureId)
    {
        var requiredSignature = _appDbContext.RequiresSignatures
            .FirstOrDefault(required => required.Id == requiresSignatureId);

        if (requiredSignature == null)
            throw new RequiresSignatureNotFoundException(requiresSignatureId);

        return requiredSignature;
    }

    /// <summary>
    /// All required signatures where the user is the signee and has not signed yet.
    /// </summary>
    public async Task<List<RequiredSignatureDto>> GetPendingRequiredSignaturesAsync(Guid userId)
    {
        var pending = await _appDbContext.RequiresSignatures
            .AsNoTracking()
            .Include(required => required.OperationalRestriction)
            .Where(required => required.SigneeId == userId
                               && required.RequiresSignatureStatus == RequiresSignatureStatus.PendingSignature)
            .OrderBy(required => required.CreatedAt)
            .ToListAsync();

        return pending.Select(RequiredSignatureDto.FromDalEntity).ToList();
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
        _appDbContext.Signatures.Add(signature);
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;
        _appDbContext.SaveChanges();

        return signature;
    }

    public Signature SignOriginator(Guid operationalRestrictionId, Guid signerUserId)
    {
        var existingSignature = _appDbContext.Signatures.FirstOrDefault(signature =>
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
        _appDbContext.RequiresSignatures.Add(requiredSignature);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = RoleNames.Originator,
            SignedAt = DateTime.UtcNow
        };
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;
        _appDbContext.SaveChanges();

        return signature;
    }

    public Signature SignDutyOperationsManager(Guid operationalRestrictionId, Guid signerUserId)
    {
        if (!GetRoleNames(signerUserId).Contains(RoleNames.DutyOperationsManager))
            throw new NotDutyOperationsManagerException(signerUserId);


        var alreadySigned = _appDbContext.Signatures
            .Where(signature => signature.OperationalRestrictionId == operationalRestrictionId)
            .Select(signature => signature.SigneeRole)
            .AsEnumerable()
            .Any(role => role.Split(", ").Contains(RoleNames.DutyOperationsManager));

        if (alreadySigned)
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
        _appDbContext.RequiresSignatures.Add(requiredSignature);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = string.Join(", ", GetRoleNames(signerUserId)),
            SignedAt = DateTime.UtcNow
        };
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;
        _appDbContext.SaveChanges();

        return signature;
    }

    public Signature? GetOriginatorSignature(Guid operationalRestrictionId)
    {
        return _appDbContext.Signatures.FirstOrDefault(signature =>
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

        var existingSignature = _appDbContext.Signatures.FirstOrDefault(
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
        _appDbContext.RequiresSignatures.Add(requiredSignature);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = RoleNames.ControlroomSupervisor,
            RequiresSignatureId = requiredSignature.Id,
            SignedAt = DateTime.UtcNow
        };
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;
        _appDbContext.SaveChanges();

        return signature;
    }

    public Signature? GetControlRoomSupervisorSignature(Guid operationalRestrictionId)
    {
        return _appDbContext.Signatures.FirstOrDefault(signature =>
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

        // The signature may still be tracked as Added after the failed transition;
        // detach it so the SaveChanges below does not insert it anyway.
        _appDbContext.Entry(signature).State = EntityState.Detached;

        _appDbContext.RequiresSignatures.Remove(requiredSignature);
        _appDbContext.SaveChanges();
    }



    public void InvalidateRequiredSignatures(Guid operationalRestrictionId)
    {
        var requiredSignatures = _appDbContext.RequiresSignatures
            .Where(required => required.OperationalRestrictionId == operationalRestrictionId)
            .ToList();

        foreach (var requiredSignature in requiredSignatures)
        {
            requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.Invalid;
        }

        _appDbContext.SaveChanges();
    }


    public bool IsSignatureValid(Guid signatureId)
    {
        var signature = _appDbContext.Signatures.FirstOrDefault(x => x.Id == signatureId);

        if (signature == null)
            throw new SignatureNotFoundException(signatureId);

        return GetRequiresSignatureById(signature.RequiresSignatureId).RequiresSignatureStatus == RequiresSignatureStatus.CompletedSignature;
    }
}
