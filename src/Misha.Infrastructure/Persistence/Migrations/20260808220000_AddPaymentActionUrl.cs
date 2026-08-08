using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misha.Infrastructure.Persistence.Migrations;

public partial class AddPaymentActionUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActionUrl",
            table: "payments",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ActionUrl",
            table: "payments");
    }
}
