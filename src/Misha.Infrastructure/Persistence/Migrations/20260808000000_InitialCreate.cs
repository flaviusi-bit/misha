using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "applications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicantReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ProcessingStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RefusalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_applications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "document_artifacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_artifacts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "passport_documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IssuingCountry = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Surname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                GivenNames = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                Nationality = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_passport_documents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "watchlist_checks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                MatchReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CheckedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_watchlist_checks", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_applications_ApplicantReference",
            table: "applications",
            column: "ApplicantReference");

        migrationBuilder.CreateIndex(
            name: "IX_applications_Status",
            table: "applications",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_document_artifacts_ApplicationId_CreatedAtUtc",
            table: "document_artifacts",
            columns: new[] { "ApplicationId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_document_artifacts_Sha256",
            table: "document_artifacts",
            column: "Sha256");

        migrationBuilder.CreateIndex(
            name: "IX_passport_documents_ApplicationId",
            table: "passport_documents",
            column: "ApplicationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_passport_documents_DocumentNumber",
            table: "passport_documents",
            column: "DocumentNumber");

        migrationBuilder.CreateIndex(
            name: "IX_watchlist_checks_ApplicationId_CreatedAtUtc",
            table: "watchlist_checks",
            columns: new[] { "ApplicationId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "document_artifacts");
        migrationBuilder.DropTable(name: "passport_documents");
        migrationBuilder.DropTable(name: "watchlist_checks");
        migrationBuilder.DropTable(name: "applications");
    }
}
