namespace Misha.Api;

public static class EtaVerificationUrl
{
    public static string? Create(
        string? publicBaseUrl,
        string etaNumber,
        string verificationToken)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl)
            || string.IsNullOrWhiteSpace(etaNumber)
            || string.IsNullOrWhiteSpace(verificationToken))
            return null;

        var baseUrl = publicBaseUrl.TrimEnd('/');
        var encodedEta = Uri.EscapeDataString(etaNumber.Trim());
        var encodedToken = Uri.EscapeDataString(verificationToken.Trim());

        // Keep the secret in the fragment so browsers do not send it to the server
        // in the initial GET request. The verification page can read it and submit
        // the token to POST /eta/verify.
        return $"{baseUrl}/eta/verify/{encodedEta}#token={encodedToken}";
    }
}
