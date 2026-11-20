namespace Backend.Models.DTOs;

// Answer to "what happened to my signing attempt?" after a lost connection.
// A signature either exists (Signed, with the signature) or it does not (NotSigned).
public class SigningAttemptStatusDto
{
    public const string Signed = "Signed";
    public const string NotSigned = "NotSigned";

    public Guid AttemptKey { get; set; }
    public string Status { get; set; } = NotSigned;
    public SignatureDto? Signature { get; set; }
}
