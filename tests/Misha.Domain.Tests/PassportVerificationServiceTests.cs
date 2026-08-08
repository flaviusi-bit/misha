using Misha.Application.Documents;
using Misha.Domain.Documents;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PassportVerificationServiceTests
{
    [Fact]
    public async Task Unavailable_provider_fails_closed()
    {
        var applicationId = Guid.NewGuid();
        var passport = CreatePassport(applicationId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        var repository = new InMemoryPassportRepository(passport);
        var provider = new StubPassportVerificationProvider(
            new PassportVerificationResult(PassportVerificationDecision.UnableToVerify, ErrorMessage: "Provider unavailable."));
        var service = new PassportVerificationService(repository, provider);

        var result = await service.VerifyAsync(applicationId, CancellationToken.None);

        Assert.Equal(PassportVerificationDecision.UnableToVerify, result.Decision);
        Assert.Equal("Provider unavailable.", result.ErrorMessage);
    }

    [Fact]
    public async Task Expired_passport_is_rejected_without_calling_provider()
    {
        var applicationId = Guid.NewGuid();
        var passport = CreatePassport(applicationId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        var repository = new InMemoryPassportRepository(passport);
        var provider = new StubPassportVerificationProvider(
            new PassportVerificationResult(PassportVerificationDecision.Verified));
        var service = new PassportVerificationService(repository, provider);

        var result = await service.VerifyAsync(applicationId, CancellationToken.None);

        Assert.Equal(PassportVerificationDecision.Rejected, result.Decision);
        Assert.Equal("Passport is expired.", result.ErrorMessage);
        Assert.False(provider.Called);
    }

    [Fact]
    public async Task Provider_result_is_returned_for_valid_passport()
    {
        var applicationId = Guid.NewGuid();
        var passport = CreatePassport(applicationId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        var repository = new InMemoryPassportRepository(passport);
        var provider = new StubPassportVerificationProvider(
            new PassportVerificationResult(
                PassportVerificationDecision.Verified,
                Reference: "VERIFY-123"));
        var service = new PassportVerificationService(repository, provider);

        var result = await service.VerifyAsync(applicationId, CancellationToken.None);

        Assert.Equal(PassportVerificationDecision.Verified, result.Decision);
        Assert.Equal("VERIFY-123", result.Reference);
        Assert.True(provider.Called);
    }

    private static PassportDocument CreatePassport(Guid applicationId, DateOnly expiryDate) =>
        PassportDocument.Create(
            applicationId,
            "AB123456",
            "RO",
            "Popescu",
            "Ion",
            new DateOnly(1985, 4, 12),
            "RO",
            expiryDate);

    private sealed class InMemoryPassportRepository(PassportDocument passport) : IPassportRepository
    {
        public Task<PassportDocument?> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken) =>
            Task.FromResult<PassportDocument?>(passport.ApplicationId == applicationId ? passport : null);

        public Task AddAsync(PassportDocument passport, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubPassportVerificationProvider(PassportVerificationResult result) : IPassportVerificationProvider
    {
        public bool Called { get; private set; }
        public string Name => "stub";

        public Task<PassportVerificationResult> VerifyAsync(
            PassportDocument passport,
            CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(result);
        }
    }
}
