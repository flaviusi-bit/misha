using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

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
            """);

        migrationBuilder.Sql("""
            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS status varchar(32);

            UPDATE manual_review_cases
            SET status = 'Pending'
            WHERE status IS NULL;

            ALTER TABLE manual_review_cases
                ALTER COLUMN status SET NOT NULL;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS trigger varchar(100);

            UPDATE manual_review_cases
            SET trigger = 'Legacy'
            WHERE trigger IS NULL;

            ALTER TABLE manual_review_cases
                ALTER COLUMN trigger SET NOT NULL;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS reason varchar(2000);

            UPDATE manual_review_cases
            SET reason = 'Legacy manual review case'
            WHERE reason IS NULL;

            ALTER TABLE manual_review_cases
                ALTER COLUMN reason SET NOT NULL;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE manual_review_cases
                ADD COLUMN IF NOT EXISTS created_at_utc timestamp with time zone;

            UPDATE manual_review_cases
            SET created_at_utc = CURRENT_TIMESTAMP
            WHERE created_at_utc IS NULL;

            ALTER TABLE manual_review_cases
                ALTER COLUMN created_at_utc SET NOT NULL;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_manual_review_cases_status_created
                ON manual_review_cases (status, created_at_utc);

            CREATE INDEX IF NOT EXISTS ix_manual_review_cases_application
                ON manual_review_cases (application_id, created_at_utc);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_manual_review_cases_open_application
                ON manual_review_cases (application_id)
                WHERE status IN ('Pending', 'InProgress');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs a schema that may pre-date EF ownership of the table.
        // Do not drop the table or destroy existing manual-review data on rollback.
    }
}
