namespace Backend.Tests;

// SignatureService keeps required signatures and signatures in static lists
// shared across the whole test process, so tests that exercise signing must
// not run in parallel with each other.
[CollectionDefinition("SignatureService")]
public class SignatureTestFixture;
