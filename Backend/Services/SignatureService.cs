using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enum;

namespace Backend.Services;

public class SignatureService
{
    private sealed record SignatureUser(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        UserRole Role,
        UserWorkingStatus WorkingStatus);

    private static readonly List<SignatureUser> _users =
    [
        new SignatureUser(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Anna Jensen",
            "anna.jensen@example.com",
            "seed-password-hash-1",
            UserRole.DutyOperationsManager,
            UserWorkingStatus.AtWork),
        new SignatureUser(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Lars Nielsen",
            "lars.nielsen@example.com",
            "seed-password-hash-2",
            UserRole.Operator,
            UserWorkingStatus.AtWork),
        new SignatureUser(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Sofie Hansen",
            "sofie.hansen@example.com",
            "seed-password-hash-3",
            UserRole.Operator,
            UserWorkingStatus.OffWork),
        new SignatureUser(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Mikkel Pedersen",
            "mikkel.pedersen@example.com",
            "seed-password-hash-4",
            UserRole.ControlroomSupervisor,
            UserWorkingStatus.OnHoliday),
        new SignatureUser(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Camilla Andersen",
            "camilla.andersen@example.com",
            "seed-password-hash-5",
            UserRole.Betriebsleiter,
            UserWorkingStatus.AtWork)
    ];

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
        var createdSignatures = _users
            .Where(user => user.WorkingStatus != UserWorkingStatus.OnHoliday)
            .Select(user => new RequiresSignature
            {
                Id = Guid.NewGuid(),
                OperationalRestrictionId = operationalRestrictionId,
                SigneeId = user.Id,
                RequiresSignatureStatus = RequiresSignatureStatus.Unsigned
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

        if (requiredSignature.RequiresSignatureStatus != RequiresSignatureStatus.Unsigned)
            throw new RequiresSignatureNotSignableException(requiresSignatureId, requiredSignature.RequiresSignatureStatus.ToString());

        //I fremtiden skal den bruge GetUserById
        var signee = _users.Single(user => user.Id == signerUserId);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = requiredSignature.OperationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = signee.Role,
            SignedAt = DateTime.UtcNow
        };
        //Dette skal smides i databasen selvfølgelig uden de virtuelle fields
        _signatures.Add(signature);

        //dette skal opdateres i databasen
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.Signed;


        return signature;
    }

    public Signature SignOriginator(Guid operationalRestrictionId, Guid signerUserId)
    {
        var existingSignature = _signatures.FirstOrDefault(signature =>
            signature.OperationalRestrictionId == operationalRestrictionId
            && signature.SigneeId == signerUserId
            && signature.SigneeRole == UserRole.Originator);

        if (existingSignature != null)
        {
            throw new RequiresSignatureNotSignableException(
                existingSignature.RequiresSignatureId,
                RequiresSignatureStatus.Signed.ToString());
        }

        var requiredSignature = new RequiresSignature
        {
            Id = Guid.NewGuid(),
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            RequiresSignatureStatus = RequiresSignatureStatus.Unsigned
        };
        _requiredSignatures.Add(requiredSignature);

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            RequiresSignatureId = requiredSignature.Id,
            OperationalRestrictionId = operationalRestrictionId,
            SigneeId = signerUserId,
            SigneeRole = UserRole.Originator,
            SignedAt = DateTime.UtcNow
        };
        _signatures.Add(signature);
        requiredSignature.RequiresSignatureStatus = RequiresSignatureStatus.Signed;

        return signature;
    }

    public Signature? GetOriginatorSignature(Guid operationalRestrictionId)
    {
        return _signatures.FirstOrDefault(signature =>
            signature.OperationalRestrictionId == operationalRestrictionId
            && signature.SigneeRole == UserRole.Originator);
    }

    internal void DiscardUnpersistedOriginatorSignature(Signature signature)
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

        return GetRequiresSignatureById(signature.RequiresSignatureId).RequiresSignatureStatus == RequiresSignatureStatus.Signed;
    }
}
