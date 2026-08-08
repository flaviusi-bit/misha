namespace Misha.Application.Etas;

public interface IEtaCredentialSigner
{
    string KeyId { get; }
    string Algorithm { get; }

    string Sign(string etaNumber, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc);
}

public static class EtaCredentialPayload
{
    public static string Canonicalize(
        string etaNumber,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(etaNumber);

        return string.Join(
            "|",
            "misha-eta-v1",
            etaNumber.Trim(),
            issuedAtUtc.ToUniversalTime().ToString("O"),
            expiresAtUtc.ToUniversalTime().ToString("O"));
    }
}
