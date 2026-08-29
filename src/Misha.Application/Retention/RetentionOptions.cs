namespace Misha.Application.Retention;

public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    public bool Enabled { get; set; }
    public bool DryRun { get; set; } = true;
    public int DocumentRetentionDays { get; set; }
    public int ApplicantRetentionDays { get; set; }
    public int BatchSize { get; set; } = 100;
}
