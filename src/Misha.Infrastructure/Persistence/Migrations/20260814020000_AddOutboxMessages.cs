using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MishaDbContext))]
[Migration("20260814020000_AddOutboxMessages")]
public partial class AddOutboxMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                Payload = table.Column<string>(type: "jsonb", nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_outbox_messages", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_AggregateId_OccurredAtUtc",
            table: "outbox_messages",
            columns: new[] { "AggregateId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_PublishedAtUtc_OccurredAtUtc",
            table: "outbox_messages",
            columns: new[] { "PublishedAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "outbox_messages");
}
