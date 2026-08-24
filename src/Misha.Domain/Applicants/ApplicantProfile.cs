namespace Misha.Domain.Applicants;

public sealed record ApplicantProfile(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Nationality,
    string? CountryOfBirth,
    string? PlaceOfBirth,
    string? Gender,
    string? Email,
    string? PhoneNumber);
