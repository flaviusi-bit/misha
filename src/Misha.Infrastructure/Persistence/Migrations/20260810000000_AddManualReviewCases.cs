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
                id uuid NOT NULL,
                application_id uuid NOT NULL,
                status varchar(32),
                trigger varchar(100),
                reason varchar(2000),
                created_at_utc timestamp with time zone,
                assigned_to_actor_reference varchar(200),
                assigned_at_utc timestamp with time zone,
                resolution varchar(32),
                resolution_reason varchar(2000),
                resolved_by_actor_reference varchar(200),
                resolved_at_utc timestamp with time zone,
                CONSTRAINT pk_manual_review_cases PRIMARY KEY (id),
                CONSTRAINT fk_manual_review_cases_applications FOREIGN KEY (application_id)
                    REFERENCES applications (id)
                    ON DELETE RESTRICT
            );

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS status varchar(32);
            UPDATE manual_review_cases SET status = 'Pending' WHERE status IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN status SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS trigger varchar(100);
            UPDATE manual_review_cases SET trigger = 'Legacy' WHERE trigger IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN trigger SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS reason varchar(2000);
            UPDATE manual_review_cases SET reason = 'Legacy manual review case' WHERE reason IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN reason SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS created_at_utc timestamp with time zone;
            UPDATE manual_review_cases SET created_at_utc = CURRENT_TIMESTAMP WHERE created_at_utc IS NULL;
            ALTER TABLE manual_review_cases ALTER COLUMN created_at_utc SET NOT NULL;

            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS assigned_to_actor_reference varchar(200),
                ADD COLUMN IF NOT EXISTS assigned_at_utc timestamp with time zone,
                ADD COLUMN IF NOT EXISTS resolution varchar(32),
                ADD COLUMN IF NOT EXISTS resolution_reason varchar(2000),
                ADD COLUMN IF NOT EXISTS resolved_by_actor_reference varchar(200),
                ADD COLUMN IF NOT EXISTS resolved_at_utc timestamp with time zone;

            CREATE INDEX IF NOT EXISTS ix_manual_review_cases_status_created
                ON manual_review_cases (status, created_at_utc);
            CREATE INDEX IF NOT EXISTS ix_manual_review_cases_application
                ON manual_review_cases (application_id, created_at_utc);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_manual_review_cases_open_application
                ON manual_review_cases (application_id)
                WHERE status IN ('Pending', 'InProgress');
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS notifications (
                id uuid NOT NULL,
                application_id uuid NOT NULL,
                recipient_reference varchar(200) NOT NULL,
                channel varchar(32) NOT NULL,
                template varchar(100) NOT NULL,
                payload text NOT NULL,
                status varchar(32) NOT NULL,
                attempts integer NOT NULL DEFAULT 0,
                created_at_utc timestamp with time zone NOT NULL,
                sent_at_utc timestamp with time zone,
                last_attempt_at_utc timestamp with time zone,
                last_error varchar(2000),
                CONSTRAINT pk_notifications PRIMARY KEY (id),
                CONSTRAINT fk_notifications_applications FOREIGN KEY (application_id)
                    REFERENCES applications (id)
                    ON DELETE RESTRICT
            );

            ALTER TABLE notifications
                ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS sent_at_utc timestamp with time zone,
                ADD COLUMN IF NOT EXISTS last_attempt_at_utc timestamp with time zone,
                ADD COLUMN IF NOT EXISTS last_error varchar(2000);

            CREATE INDEX IF NOT EXISTS ix_notifications_status_created
                ON notifications (status, created_at_utc);
            CREATE INDEX IF NOT EXISTS ix_notifications_application_created
                ON notifications (application_id, created_at_utc);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs schemas that pre-date EF ownership of these tables.
        // Do not drop existing data on rollback.
    }
}
