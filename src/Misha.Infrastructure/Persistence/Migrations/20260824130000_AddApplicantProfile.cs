using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[Migration("20260824130000_AddApplicantProfile")]
public partial class AddApplicantProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "FirstName", table: "applicants", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "LastName", table: "applicants", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "DateOfBirth", table: "applicants", type: "date", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Nationality", table: "applicants", type: "character varying(3)", maxLength: 3, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CountryOfBirth", table: "applicants", type: "character varying(3)", maxLength: 3, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PlaceOfBirth", table: "applicants", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Gender", table: "applicants", type: "character varying(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Email", table: "applicants", type: "character varying(320)", maxLength: 320, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PhoneNumber", table: "applicants", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "UpdatedAtUtc", table: "applicants", type: "timestamp with time zone", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FirstName", table: "applicants"); migrationBuilder.DropColumn(name: "LastName", table: "applicants"); migrationBuilder.DropColumn(name: "DateOfBirth", table: "applicants"); migrationBuilder.DropColumn(name: "Nationality", table: "applicants"); migrationBuilder.DropColumn(name: "CountryOfBirth", table: "applicants"); migrationBuilder.DropColumn(name: "PlaceOfBirth", table: "applicants"); migrationBuilder.DropColumn(name: "Gender", table: "applicants"); migrationBuilder.DropColumn(name: "Email", table: "applicants"); migrationBuilder.DropColumn(name: "PhoneNumber", table: "applicants"); migrationBuilder.DropColumn(name: "UpdatedAtUtc", table: "applicants");
    }
}

[Migration("20260830000000_AddTenantIsolation")]
public partial class AddTenantIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("TenantId", "applicants", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>("TenantId", "applications", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.DropIndex("IX_applicants_ExternalReference", "applicants");
        migrationBuilder.DropIndex("IX_applications_IdempotencyKey", "applications");
        migrationBuilder.CreateIndex("IX_applicants_TenantId_ExternalReference", "applicants", new[] { "TenantId", "ExternalReference" }, unique: true);
        migrationBuilder.CreateIndex("IX_applications_TenantId", "applications", "TenantId");
        migrationBuilder.CreateIndex("IX_applications_TenantId_IdempotencyKey", "applications", new[] { "TenantId", "IdempotencyKey" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_applicants_TenantId_ExternalReference", "applicants");
        migrationBuilder.DropIndex("IX_applications_TenantId", "applications");
        migrationBuilder.DropIndex("IX_applications_TenantId_IdempotencyKey", "applications");
        migrationBuilder.CreateIndex("IX_applicants_ExternalReference", "applicants", "ExternalReference", unique: true);
        migrationBuilder.CreateIndex("IX_applications_IdempotencyKey", "applications", "IdempotencyKey", unique: true);
        migrationBuilder.DropColumn("TenantId", "applicants");
        migrationBuilder.DropColumn("TenantId", "applications");
    }
}
