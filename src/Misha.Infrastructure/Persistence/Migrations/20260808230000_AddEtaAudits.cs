using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MishaDbContext))]
[Migration("20260808230000_AddEtaAudits")]
public partial class AddEtaAudits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "eta_audits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EtaId = table.Column<Guid>(type: "uuid", nullable: true),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                EtaNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ActorReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_eta_audits", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_eta_audits_ApplicationId_OccurredAtUtc",
            table: "eta_audits",
            columns: new[] { "ApplicationId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_eta_audits_EtaId_OccurredAtUtc",
            table: "eta_audits",
            columns: new[] { "EtaId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_eta_audits_EventType_OccurredAtUtc",
            table: "eta_audits",
            columns: new[] { "EventType", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "eta_audits");
    }
}
