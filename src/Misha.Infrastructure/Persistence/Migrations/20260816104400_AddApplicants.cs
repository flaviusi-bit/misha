using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[Migration("20260816104400_AddApplicants")]
public partial class AddApplicants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "applicants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_applicants", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_applicants_ExternalReference",
            table: "applicants",
            column: "ExternalReference",
            unique: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ApplicantId",
            table: "applications",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("INSERT INTO applicants (\"Id\", \"ExternalReference\", \"CreatedAtUtc\") SELECT gen_random_uuid(), \"ApplicantReference\", MIN(\"CreatedAtUtc\") FROM applications GROUP BY \"ApplicantReference\"");
        migrationBuilder.Sql("UPDATE applications a SET \"ApplicantId\" = p.\"Id\" FROM applicants p WHERE p.\"ExternalReference\" = a.\"ApplicantReference\"");

        migrationBuilder.AlterColumn<Guid>(
            name: "ApplicantId",
            table: "applications",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_applications_ApplicantId",
            table: "applications",
            column: "ApplicantId");

        migrationBuilder.AddForeignKey(
            name: "FK_applications_applicants_ApplicantId",
            table: "applications",
            column: "ApplicantId",
            principalTable: "applicants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_applications_applicants_ApplicantId", table: "applications");
        migrationBuilder.DropIndex(name: "IX_applications_ApplicantId", table: "applications");
        migrationBuilder.DropColumn(name: "ApplicantId", table: "applications");
        migrationBuilder.DropTable(name: "applicants");
    }
}
