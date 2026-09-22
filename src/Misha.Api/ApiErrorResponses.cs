namespace Misha.Api;

internal static class ApiErrorResponses
{
    internal const string OperationConflict = "The requested operation could not be completed.";
    internal const string OperationRejected = "The requested operation was rejected.";

    internal static IResult Conflict() =>
        Results.Conflict(new { error = OperationConflict });

    internal static IResult BadRequest() =>
        Results.BadRequest(new { error = OperationRejected });
}
