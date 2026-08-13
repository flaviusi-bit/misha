using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MishaDbContext))]
[Migration("20260810000000_AddManualReviewCases")]
public partial class AddManualReviewCases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS manual_review_cases (
                "Id" uuid NOT NULL,
                "ApplicationId" uuid NOT NULL,
                "Status" varchar(32),
                "Trigger" varchar(100),
                "Reason" varchar(2000),
                "CreatedAtUtc" timestamp with time zone,
                "AssignedToActorReference" varchar(200),
                "AssignedAtUtc" timestamp with time zone,
                "Resolution" varchar(32),
                "ResolutionReason" varchar(2000),
                "ResolvedByActorReference" varchar(200),
                "ResolvedAtUtc" timestamp with time zone,
                CONSTRAINT "PK_manual_review_cases" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_manual_review_cases_applications" FOREIGN KEY ("ApplicationId")
                    REFERENCES applications ("Id")
                    ON DELETE RESTRICT
            );

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS "Status" varchar(32);
            UPDATE manual_review_cases SET "Status" = 'Pending' WHERE "Status" IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN "Status" SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS "Trigger" varchar(100);
            UPDATE manual_review_cases SET "Trigger" = 'Legacy' WHERE "Trigger" IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN "Trigger" SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS "Reason" varchar(2000);
            UPDATE manual_review_cases SET "Reason" = 'Legacy manual review case' WHERE "Reason" IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN "Reason" SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp with time zone;
            UPDATE manual_review_cases SET "CreatedAtUtc" = CURRENT_TIMESTAMP WHERE "CreatedAtUtc" IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN "CreatedAtUtc" SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS "AssignedToActorReference" varchar(200),
                ADD COLUMN IF NOT EXISTS "AssignedAtUtc" timestamp with time zone,
                ADD COLUMN IF NOT EXISTS "Resolution" varchar(32),
                ADD COLUMN IF NOT EXISTS "ResolutionReason" varchar(2000),
                ADD COLUMN IF NOT EXISTS "ResolvedByActorReference" varchar(200),
                ADD COLUMN IF NOT EXISTS "ResolvedAtUtc" timestamp with time zone;

            CREATE INDEX IF NOT EXISTS "IX_manual_review_cases_Status_CreatedAtUtc"
                ON manual_review_cases ("Status", "CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_manual_review_cases_ApplicationId_CreatedAtUtc"
                ON manual_review_cases ("ApplicationId", "CreatedAtUtc");
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_manual_review_cases_open_application"
                ON manual_review_cases ("ApplicationId")
                WHERE "Status" IN ('Pending', 'InProgress');
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS notifications (
                "Id" uuid NOT NULL,
                "ApplicationId" uuid NOT NULL,
                "RecipientReference" varchar(200) NOT NULL,
                "Channel" varchar(32) NOT NULL,
                "Template" varchar(100) NOT NULL,
                "Payload" text NOT NULL,
                "Status" varchar(32) NOT NULL,
                "Attempts" integer NOT NULL DEFAULT 0,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "SentAtUtc" timestamp with time zone,
                "LastAttemptAtUtc" timestamp with time zone,
                "LastError" varchar(2000),
                CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_notifications_applications" FOREIGN KEY ("ApplicationId")
                    REFERENCES applications ("Id")
                    ON DELETE RESTRICT
            );

            ALTER TABLE notifications
                ADD COLUMN IF NOT EXISTS "Attempts" integer NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS "SentAtUtc" timestamp with time zone,
                ADD COLUMN IF NOT EXISTS "LastAttemptAtUtc" timestamp with time zone,
                ADD COLUMN IF NOT EXISTS "LastError" varchar(2000);

            CREATE INDEX IF NOT EXISTS "IX_notifications_Status_CreatedAtUtc"
                ON notifications ("Status", "CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_notifications_ApplicationId_CreatedAtUtc"
                ON notifications ("ApplicationId", "CreatedAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs schemas that pre-date EF ownership of these tables.
        // Do not drop existing data on rollback.
    }
}
