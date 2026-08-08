using Misha.Api;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class EtaVerificationPageTests
{
    [Fact]
    public void Verification_page_does_not_embed_the_verification_token()
    {
        const string nonce = "test-nonce";

        var html = EtaVerificationPage.Create(nonce);

        Assert.Contains("eTA verification", html);
        Assert.Contains("nonce=\"test-nonce\"", html);
        Assert.Contains("location.hash", html);
        Assert.DoesNotContain("verificationToken", html);
    }

    [Fact]
    public void Verification_page_nonce_is_generated_in_url_safe_form()
    {
        var nonce = EtaVerificationPage.CreateNonce();

        Assert.NotEmpty(nonce);
        Assert.DoesNotContain("+", nonce);
        Assert.DoesNotContain("/", nonce);
        Assert.DoesNotContain("=", nonce);
    }
}
