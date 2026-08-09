namespace Misha.Application.FastLane;

public sealed record FastLanePackage(
    string Version,
    string EtaNumber,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string SigningKeyId,
    string SigningAlgorithm,
    string Signature,
    string PublicKeyPem);
