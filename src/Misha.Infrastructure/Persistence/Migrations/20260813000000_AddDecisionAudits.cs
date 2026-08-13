using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[Migration("20260813000000_AddDecisionAudits")]
public partial class AddDecisionAudits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS decision_audits (
                "Id" uuid NOT NULL,
                "ApplicationId" uuid NOT NULL,
                "PolicyVersion" character varying(50) NOT NULL,
                "PolicyDecision" character varying(32) NOT NULL,
                "Decision" character varying(32) NOT NULL,
                "ReasonsJson" jsonb NOT NULL,
                "ActorReference" character varying(200) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_decision_audits" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_decision_audits_ApplicationId_CreatedAtUtc"
            ON decision_audits ("ApplicationId", "CreatedAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep the audit table on rollback to avoid destructive data loss.
    }
}
