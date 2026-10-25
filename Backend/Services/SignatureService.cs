using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enum;

namespace Backend.Services;

public class SignatureService(
    AppDbContext appDbContext
)
{
    private readonly AppDbContext _appDbContext = appDbContext;
    //Temporary before db
    private static readonly List<User> _users =
    [
        new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Anna Jensen",
            Email = "anna.jensen@example.com",
            PasswordHash = "seed-password-hash-1",
            Role = UserRole.DutyOperationsManager,
            WorkingStatus = UserWorkingStatus.AtWork,
            InCharge = false
        },
        new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Lars Nielsen",
            Email = "lars.nielsen@example.com",
            PasswordHash = "seed-password-hash-2",
            Role = UserRole.Operator,
            WorkingStatus = UserWorkingStatus.AtWork,
            InCharge = false
        },
        new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Sofie Hansen",
            Email = "sofie.hansen@example.com",
            PasswordHash = "seed-password-hash-3",
            Role = UserRole.Operator,
            WorkingStatus = UserWorkingStatus.OffWork,
            InCharge = false
        },
        new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Mikkel Pedersen",
            Email = "mikkel.pedersen@example.com",
            PasswordHash = "seed-password-hash-4",
            Role = UserRole.ControlroomSupervisor,
            WorkingStatus = UserWorkingStatus.OnHoliday,
            InCharge = false
        },
        new User
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "Camilla Andersen",
            Email = "camilla.andersen@example.com",
            PasswordHash = "seed-password-hash-5",
            Role = UserRole.Betriebsleiter,
            WorkingStatus = UserWorkingStatus.AtWork,
            InCharge = true
        }
    ];

    //Midlertidigt mens der ikke er database
    private static List<RequiresSignature> _requiredSignatures = [];
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
