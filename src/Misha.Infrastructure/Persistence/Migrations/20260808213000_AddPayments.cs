using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MishaDbContext))]
[Migration("20260808213000_AddPayments")]
public partial class AddPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_payments_ApplicationId_CreatedAtUtc",
            table: "payments",
            columns: new[] { "ApplicationId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_payments_ProviderReference",
            table: "payments",
            column: "ProviderReference");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "payments");
    }
}
