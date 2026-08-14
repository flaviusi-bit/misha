using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MishaDbContext))]
[Migration("20260814000000_AddApplicationIdempotency")]
public partial class AddApplicationIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "applications",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_applications_IdempotencyKey",
            table: "applications",
            column: "IdempotencyKey",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_applications_IdempotencyKey",
            table: "applications");

        migrationBuilder.DropColumn(
            name: "IdempotencyKey",
            table: "applications");
    }
}
