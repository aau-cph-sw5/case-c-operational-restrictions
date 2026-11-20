using Backend.Authorization;
using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;

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
    // Guards check-and-add on the in-memory lists, so a double tap cannot create two signatures.
    private static readonly object _syncRoot = new();
    // Held from the first write to the commit of a signing action, so a status lookup or a retry
    // can only see a signature that is committed or has been discarded, never one in between.
    private static readonly SemaphoreSlim _signingGate = new(1, 1);

    public async Task<T> RunExclusiveAsync<T>(Func<Task<T>> signingAction)
    {
        await _signingGate.WaitAsync();
        try
        {
            return await signingAction();
        }
        finally
        {
            _signingGate.Release();
        }
    }

    /// <summary>
    /// Returns the committed signature of an earlier attempt with this key, or null if that attempt
    /// left no signature. Waits for any signing action in flight. Throws if the key was used for a
    /// different signing action than the expected one.
    /// </summary>
    public async Task<Signature?> FindReplayAsync(
        Guid attemptKey, Guid signerUserId, string? expectedRole = null)
    {
        await _signingGate.WaitAsync();
        try
        {
            return FindReplay(attemptKey, signerUserId, expectedRole);
        }
        finally
        {
            _signingGate.Release();
        }
    }

    public Signature? FindReplay(
        Guid attemptKey,
        Guid signerUserId,
        string? expectedRole = null,
        Guid? expectedRequiresSignatureId = null)
    {
        var existing = FindByAttemptKey(attemptKey, signerUserId);
        if (existing == null)
            return null;

        var matchesAction = (expectedRole == null || existing.SigneeRole == expectedRole)
            && (expectedRequiresSignatureId == null || existing.RequiresSignatureId == expectedRequiresSignatureId);

        if (!matchesAction)
            throw new AttemptKeyReusedException(attemptKey);

        return existing;
    }

    // The in-memory list is checked first, then the database, so the answer survives a restart
    // for signatures that were persisted with their restriction version.
    public Signature? FindByAttemptKey(Guid attemptKey, Guid signerUserId)
    {
        lock (_syncRoot)
        {
            var inMemory = _signatures.FirstOrDefault(
                signature => signature.AttemptKey == attemptKey && signature.SigneeId == signerUserId);
            if (inMemory != null)
                return inMemory;
        }

        return _appDbContext.Set<Signature>()
            .AsNoTracking()
            .Where(signature => signature.AttemptKey == attemptKey && signature.SigneeId == signerUserId)
            .OrderBy(signature => signature.SignedAt)
            .FirstOrDefault();
    }

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
    public Signature Sign(Guid requiresSignatureId, Guid signerUserId, SigningContext? signingContext = null)
    {
        var context = signingContext ?? SigningContext.Unknown;
        var requiredSignature = GetRequiresSignatureById(requiresSignatureId);

        if (requiredSignature.SigneeId != signerUserId)
            throw new NotRequiredSignerException(signerUserId, requiresSignatureId);

        // Role names are read before taking the lock.
        var roleNames = string.Join(", ", GetRoleNames(signerUserId));

        lock (_syncRoot)
        {
            // A retry of an attempt that already committed gets the same signature back.
            if (context.AttemptKey is { } attemptKey
                && FindReplay(attemptKey, signerUserId, expectedRequiresSignatureId: requiredSignature.Id) is { } replayed)
                return replayed;

            if (requiredSignature.RequiresSignatureStatus != RequiresSignatureStatus.PendingSignature)
                throw new RequiresSignatureNotSignableException(requiresSignatureId, requiredSignature.RequiresSignatureStatus.ToString());

            var signature = new Signature
            {
                Id = Guid.NewGuid(),
                RequiresSignatureId = requiredSignature.Id,
                OperationalRestrictionId = requiredSignature.OperationalRestrictionId,
                SigneeId = signerUserId,
                SigneeRole = roleNames,
                SignedAt = DateTime.UtcNow,
                DeviceClass = context.DeviceClass,
                UserAgent = context.UserAgent,
                AttemptKey = context.AttemptKey
            };
            //Dette skal smides i databasen selvfølgelig uden de virtuelle fields
            _signatures.Add(signature);

            //dette skal opdateres i databasen
            requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.CompletedSignature;

            return signature;
        }
    }

    public Signature SignOriginator(Guid operationalRestrictionId, Guid signerUserId, SigningContext? signingContext = null)
    {
        var context = signingContext ?? SigningContext.Unknown;

        lock (_syncRoot)
        {
            return SignOriginatorLocked(operationalRestrictionId, signerUserId, context);
        }
    }

    private static Signature SignOriginatorLocked(Guid operationalRestrictionId, Guid signerUserId, SigningContext context)
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
            SignedAt = DateTime.UtcNow,
            DeviceClass = context.DeviceClass,
            UserAgent = context.UserAgent,
            AttemptKey = context.AttemptKey
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

    public Signature SignControlRoomSupervisor(Guid operationalRestrictionId, Guid signerUserId, SigningContext? signingContext = null)
    {
        var context = signingContext ?? SigningContext.Unknown;
        var signer = _appDbContext.Users.SingleOrDefault(user => user.Id == signerUserId);

        if (signer == null || !GetRoleNames(signerUserId).Contains(RoleNames.ControlroomSupervisor))
            throw new NotControlRoomSupervisorException(signerUserId);

        lock (_syncRoot)
        {
            return SignControlRoomSupervisorLocked(operationalRestrictionId, signerUserId, context);
        }
    }

    private static Signature SignControlRoomSupervisorLocked(Guid operationalRestrictionId, Guid signerUserId, SigningContext context)
    {
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
            SignedAt = DateTime.UtcNow,
            DeviceClass = context.DeviceClass,
            UserAgent = context.UserAgent,
            AttemptKey = context.AttemptKey
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
