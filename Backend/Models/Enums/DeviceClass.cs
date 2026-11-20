namespace Backend.Models.Enums;

// Coarse class of the device a signature was made from. Evidence only: it never
// changes who may sign or what is required to sign (ADR 0008, ADR 0010).
// Unknown must stay 0 so existing signature rows default to it.
public enum DeviceClass
{
    Unknown = 0,
    Phone = 1,
    Tablet = 2,
    Desktop = 3
}
