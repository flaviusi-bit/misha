using Misha.Api;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class EtaVerificationPageTests
{
    [Fact]
    public void Verification_page_reads_the_token_from_the_fragment_without_embedding_a_token_value()
    {
        const string nonce = "test-nonce";

        var html = EtaVerificationPage.Create(nonce);

        Assert.Contains("eTA verification", html);
        Assert.Contains("nonce=\"test-nonce\"", html);
        Assert.Contains("location.hash", html);
        Assert.Contains("verificationToken: token", html);
        Assert.DoesNotContain("__NONCE__", html);
        Assert.DoesNotContain("?token=", html);
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
